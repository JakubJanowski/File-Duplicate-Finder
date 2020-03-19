using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using FileDuplicateFinder.Models;
using FileDuplicateFinder.Services;
using Prism.Commands;

namespace FileDuplicateFinder.ViewModel {
    internal class MainWindowViewModel: ObjectBase {
        public DirectoryPickerViewModel DirectoryPickerViewModel { get; }
        public MainTabControlViewModel MainTabControlViewModel { get; }
        public StatusBarViewModel StatusBarViewModel { get; }

        /// add to settings only and make modifiable
        private readonly string tmpDirectory = AppDomain.CurrentDomain.BaseDirectory + "tmp/";

        private volatile bool stopTask = false;
        private string primaryDirectory;
        private string secondaryDirectory;

        private readonly ApplicationState state = new ApplicationState();
        private bool isRestorePossible = false;

        public bool SortBySize { get; set; } = true;
        public bool SortBySizePrimaryOnly { get; set; } = true;

        public bool AskLarge {
            get => state.AskLarge;
            set {
                if (state.AskLarge != value) {
                    state.AskLarge = value;
                    if (value)
                        BackupFiles = true;
                    OnPropertyChanged(nameof(AskLarge));
                }
            }
        }

        public bool BackupFiles {
            get => state.BackupFiles;
            set {
                if (state.BackupFiles != value) {
                    state.BackupFiles = value;
                    if (!value)
                        AskLarge = false;
                    OnPropertyChanged(nameof(BackupFiles));
                }
            }
        }

        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
            set {
                if (state.IsGUIEnabled != value) {
                    state.IsGUIEnabled = value;
                    DirectoryPickerViewModel.OnUpdateGUIEnabled();
                    MainTabControlViewModel.OnUpdateGUIEnabled();
                    OnPropertyChanged(nameof(IsGUIEnabled));
                }
            }
        }

        public bool IsRestorePossible {
            get => isRestorePossible;
            set {
                if (isRestorePossible != value) {
                    isRestorePossible = value;
                    OnPropertyChanged(nameof(IsRestorePossible));
                }
            }
        }

        public bool ShowBasePaths {
            get => state.ShowBasePaths;
            set {
                if (state.ShowBasePaths != value) {
                    state.ShowBasePaths = value;
                    if (value)
                        ShowFileBasePaths();
                    else
                        HideFileBasePaths();
                    MainTabControlViewModel.OnUpdateShowBasePaths();
                    OnPropertyChanged(nameof(ShowBasePaths));
                }
            }
        }

        public MainWindowViewModel() {
            FindDuplicatedFilesCommand = new DelegateCommand<object>(FindDuplicatedFiles, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            RestoreFilesCommand = new DelegateCommand<object>(RestoreFiles, (o) => IsRestorePossible).ObservesProperty(() => IsRestorePossible);
            StopTaskCommand = new DelegateCommand<object>(StopTask, (o) => !IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);

            StatusBarViewModel = new StatusBarViewModel();
            DirectoryPickerViewModel = new DirectoryPickerViewModel(state, StatusBarViewModel);
            MainTabControlViewModel = new MainTabControlViewModel(new MainTabControlViewModelParameters() {
                State = state,
                DirectoryPickerViewModel = DirectoryPickerViewModel,
                MainWindowViewModel = this,
                StatusBarViewModel = StatusBarViewModel
            });
            DirectoryPickerViewModel.Bind(nameof(DirectoryPickerViewModel.PrimaryOnly), () => MainTabControlViewModel.OnUpdatePrimaryOnly());

            Utilities.statusBarViewModel = StatusBarViewModel;
            FileManager.SearchProgressUpdated += ProgressUpdatedHandler;
            FileManager.tmpDirectory = tmpDirectory;
            Directory.CreateDirectory(tmpDirectory);
        }

        private void ProgressUpdatedHandler(DuplicateSearchProgress progress) {
            Utilities.BeginInvoke(() => {
                switch (progress.State) {
                    case DuplicateSearchProgressState.Processing:
                    case DuplicateSearchProgressState.Sorting:
                        StatusBarViewModel.State = progress.Description;
                        break;
                    case DuplicateSearchProgressState.StartingSearch:
                        StatusBarViewModel.State = progress.Description;
                        StatusBarViewModel.StateInfo = "0 / " + progress.MaxProgress;
                        StatusBarViewModel.IsIndeterminate = false;
                        StatusBarViewModel.MaxProgress = progress.MaxProgress;
                        StatusBarViewModel.Progress = 0;
                        break;
                    case DuplicateSearchProgressState.Searching:
                        StatusBarViewModel.StateInfo = progress.Progress + " / " + progress.MaxProgress;
                        StatusBarViewModel.Progress = progress.Progress;
                        break;
                }
            });
        }

        private void ShowFileBasePaths() {
            /// in different thread maybe? then also in view + pass specific listview to update first (sender)
            if (DirectoryPickerViewModel.PrimaryOnly)
                FileManager.ShowPrimaryBasePaths(primaryDirectory);
            else
                FileManager.ShowBasePaths(primaryDirectory, secondaryDirectory);
        }

        private void HideFileBasePaths() {
            if (DirectoryPickerViewModel.PrimaryOnly)
                FileManager.HidePrimaryBasePaths(primaryDirectory);
            else
                FileManager.HideBasePaths(primaryDirectory, secondaryDirectory);
        }

        public DelegateCommand<object> FindDuplicatedFilesCommand { get; private set; }
        private void FindDuplicatedFiles(object obj) {
            InitializeDuplicateFinding();

            if (DirectoryPickerViewModel.PrimaryOnly)
                new Thread(FindDuplicatedFilesInPrimaryOnly).Start();
            else
                new Thread(FindDuplicatedFilesInBoth).Start();
        }

        private void FindDuplicatedFilesInPrimaryOnly() {
            bool error = false;
            ////todo chk empty or null string please specify directory to search in or make field required and button unavailable until filled (add form validation)

            Utilities.CheckDirectory(primaryDirectory, SearchDirectoryType.Primary, ref error);

            if (error) {
                Utilities.BeginInvoke(() => {
                    StatusBarViewModel.State = "Failed";
                    StatusBarViewModel.ShowProgress = false;
                    IsGUIEnabled = true;
                });
                return;
            }

            FileManager.FindDuplicatedFiles(primaryDirectory, ShowBasePaths);

            Utilities.BeginInvoke(() => {
                if (!SortBySizePrimaryOnly)
                    FileManager.duplicatedFilesPrimaryOnly.Sort(Comparer<ObservableRangeCollection<FileEntry>>.Create((a, b) => string.Compare(a[0].Path, b[0].Path, StringComparison.InvariantCultureIgnoreCase))); // a[0].Path.CompareTo(b[0].Path)
                FinalizeDuplicateFinding();
            });
        }


        private void FindDuplicatedFilesInBoth() {
            bool error = false;

            if (primaryDirectory.ToUpperInvariant() == secondaryDirectory.ToUpperInvariant()) {
                error = true;
                Utilities.LogFromNonGUIThread("Primary and secondary directories must be different.");
            }

            Utilities.CheckDirectories(primaryDirectory, secondaryDirectory, ref error);
            if (primaryDirectory.IsSubdirectoryOf(secondaryDirectory)) {
                error = true;
                Utilities.LogFromNonGUIThread("Primary directory cannot be a subdirectory of secondary directory.");
            }
            if (secondaryDirectory.IsSubdirectoryOf(primaryDirectory)) {
                error = true;
                Utilities.LogFromNonGUIThread("Secondary directory cannot be a subdirectory of primary directory.");
            }

            if (error) {
                Utilities.BeginInvoke(() => {
                    StatusBarViewModel.State = "Failed";
                    StatusBarViewModel.ShowProgress = false;
                    IsGUIEnabled = true;
                });
                return;
            }

            FileManager.FindDuplicatedFiles(primaryDirectory, secondaryDirectory, ShowBasePaths);

            Utilities.BeginInvoke(() => {
                if (!SortBySize)
                    FileManager.duplicatedFiles.Sort(Comparer<Tuple<ObservableRangeCollection<FileEntry>, ObservableRangeCollection<FileEntry>>>.Create((a, b) => string.Compare(a.Item1[0].Path, b.Item1[0].Path, StringComparison.InvariantCultureIgnoreCase)));  // a.Item1[0].Path.CompareTo(b.Item1[0].Path)
                FinalizeDuplicateFinding();
            });
        }

        private void FinalizeDuplicateFinding() {
            if (stopTask) {
                stopTask = false;
                StatusBarViewModel.State = "Stopped";
            } else {
                StatusBarViewModel.State = "Done";
            }
            StatusBarViewModel.ShowProgress = false;
            StatusBarViewModel.StateInfo = "";
            IsGUIEnabled = true;
        }

        public void RemoveFile(string path, string baseDirectory) {
            if (!ShowBasePaths)
                path = baseDirectory + path;
            /// try catch log here
            if (FileManager.RemoveFile(path, BackupFiles, AskLarge))
                IsRestorePossible = true;
        }

        public DelegateCommand<object> RestoreFilesCommand { get; private set; }
        private void RestoreFiles(object obj) {
            RestoreFileDialog popup = new RestoreFileDialog(FileManager.storedFiles);
            popup.ShowDialog();
        }

        public DelegateCommand<object> StopTaskCommand { get; private set; }
        private void StopTask(object obj = null) {
            stopTask = true;
            MainTabControlViewModel.StopTask();
            FileManager.StopTask();
        }

        private void InitializeDuplicateFinding() {
            IsGUIEnabled = false;
            StatusBarViewModel.State = "Initializing";
            StatusBarViewModel.Progress = 0;
            StatusBarViewModel.IsIndeterminate = true;
            StatusBarViewModel.ShowProgress = true;
            FileManager.Initialize();
            FileManager.ClearTmpDirectory();

            //logListView.Items.Clear();
            /// should be through MainTabControl
            Utilities.logListView.Items.Clear();

            primaryDirectory = DirectoryPickerViewModel.PrimaryDirectory;
            secondaryDirectory = DirectoryPickerViewModel.SecondaryDirectory;
            IsRestorePossible = false;
        }

        internal void WindowClosing() {
            StopTask();
            FileManager.ClearDirectory(tmpDirectory);
        }
    }
}
