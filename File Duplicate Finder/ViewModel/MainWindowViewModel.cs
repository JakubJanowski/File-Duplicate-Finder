using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using FileDuplicateFinder.Models;
using Prism.Commands;

namespace FileDuplicateFinder.ViewModel {
    class MainWindowViewModel: ObjectBase {
        public DirectoryPickerViewModel DirectoryPickerViewModel { get; private set; }
        public MainTabControlViewModel MainTabControlViewModel { get; private set; }
        public StatusBarViewModel StatusBarViewModel { private get; set; }

        internal string tmpDirectory;

        private volatile bool stopTask = false;
        private string primaryDirectory;
        private string secondaryDirectory;

        private ApplicationState state = new ApplicationState();
        private bool isRestorePossible = false;
        private bool showBasePaths = false;
        private bool backupFiles = true;
        private bool askLarge = true;

        public bool SortBySize { get; set; } = true;
        public bool SortBySizePrimaryOnly { get; set; } = true;

        /// make isGUIEnabled common model
        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
            set {
                if (state.IsGUIEnabled != value) {
                    state.IsGUIEnabled = value;
                    DirectoryPickerViewModel.OnUpdateGUIEnabled();
                    MainTabControlViewModel.OnUpdateGUIEnabled();
                    OnPropertyChanged("IsGUIEnabled");
                }
            }
        }

        public bool IsRestorePossible {
            get => isRestorePossible;
            set {
                if (isRestorePossible != value) {
                    isRestorePossible = value;
                    OnPropertyChanged("IsRestorePossible");
                }
            }
        }

        ///get this from settings 
        public bool ShowBasePaths {
            get => showBasePaths;
            set {
                if (showBasePaths != value) {
                    showBasePaths = value;
                    if (value)
                        ShowFileBasePaths();
                    else
                        HideFileBasePaths();
                    MainTabControlViewModel.ShowBasePaths = value;
                    OnPropertyChanged("ShowBasePaths");
                }
            }
        }

        ///get this from settings 
        public bool BackupFiles {
            get => backupFiles;
            set {
                if (backupFiles != value) {
                    backupFiles = value;
                    if (!value)
                        AskLarge = false;
                    OnPropertyChanged("BackupFiles");
                }
            }
        }

        ///get this from settings 
        public bool AskLarge {
            get => askLarge;
            set {
                if (askLarge != value) {
                    askLarge = value;
                    if (value)
                        BackupFiles = true;
                    OnPropertyChanged("AskLarge");
                }
            }
        }

        public MainWindowViewModel() {
            FindDuplicatedFilesCommand = new DelegateCommand<object>(FindDuplicatedFiles, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            RestoreFilesCommand = new DelegateCommand<object>(RestoreFiles, (o) => IsRestorePossible).ObservesProperty(() => IsRestorePossible);
            StopTaskCommand = new DelegateCommand<object>(StopTask, (o) => !IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            DirectoryPickerViewModel = new DirectoryPickerViewModel(state);
            MainTabControlViewModel = new MainTabControlViewModel(state);
            DirectoryPickerViewModel.MainTabControlViewModel = MainTabControlViewModel;
            MainTabControlViewModel.DirectoryPickerViewModel = DirectoryPickerViewModel;
            //MainTabControlViewModel.StatusBarViewModel = StatusBarViewModel;///
            MainTabControlViewModel.MainWindowViewModel = this;
        }

       

        private void ShowFileBasePaths() {
            /// in different thread maybe? then also in view + pass specific listview to update first (sender)
            if (DirectoryPickerViewModel.PrimaryOnly) {
                for (int i = 0; i < FileManager.duplicatedFilesPrimaryOnly.Count; i++)
                    for (int p = 0; p < FileManager.duplicatedFilesPrimaryOnly[i].Count; p++)
                        FileManager.duplicatedFilesPrimaryOnly[i][p].Path = primaryDirectory + FileManager.duplicatedFilesPrimaryOnly[i][p].Path;
                for (int i = 0; i < FileManager.emptyDirectoriesPrimary.Count; i++)
                    FileManager.emptyDirectoriesPrimary[i].Path = primaryDirectory + FileManager.emptyDirectoriesPrimary[i].Path;
                for (int i = 0; i < FileManager.emptyFilesPrimary.Count; i++)
                    FileManager.emptyFilesPrimary[i].Path = primaryDirectory + FileManager.emptyFilesPrimary[i].Path;

                FileManager.duplicatedFilesPrimaryOnly.Refresh();
                FileManager.emptyDirectoriesPrimary.Refresh();
                FileManager.emptyFilesPrimary.Refresh();
            } else {
                for (int i = 0; i < FileManager.duplicatedFiles.Count; i++) {
                    for (int p = 0; p < FileManager.duplicatedFiles[i].Item1.Count; p++)
                        FileManager.duplicatedFiles[i].Item1[p].Path = primaryDirectory + FileManager.duplicatedFiles[i].Item1[p].Path;
                    for (int s = 0; s < FileManager.duplicatedFiles[i].Item2.Count; s++)
                        FileManager.duplicatedFiles[i].Item2[s].Path = secondaryDirectory + FileManager.duplicatedFiles[i].Item2[s].Path;
                }
                for (int i = 0; i < FileManager.emptyDirectoriesPrimary.Count; i++)
                    FileManager.emptyDirectoriesPrimary[i].Path = primaryDirectory + FileManager.emptyDirectoriesPrimary[i].Path;
                for (int i = 0; i < FileManager.emptyFilesPrimary.Count; i++)
                    FileManager.emptyFilesPrimary[i].Path = primaryDirectory + FileManager.emptyFilesPrimary[i].Path;
                for (int i = 0; i < FileManager.emptyDirectoriesSecondary.Count; i++)
                    FileManager.emptyDirectoriesSecondary[i].Path = secondaryDirectory + FileManager.emptyDirectoriesSecondary[i].Path;
                for (int i = 0; i < FileManager.emptyFilesSecondary.Count; i++)
                    FileManager.emptyFilesSecondary[i].Path = secondaryDirectory + FileManager.emptyFilesSecondary[i].Path;

                FileManager.duplicatedFiles.Refresh();
                FileManager.emptyDirectoriesPrimary.Refresh();
                FileManager.emptyFilesPrimary.Refresh();
                FileManager.emptyDirectoriesSecondary.Refresh();
                FileManager.emptyFilesSecondary.Refresh();
            }
        }

        private void HideFileBasePaths() {
            if (DirectoryPickerViewModel.PrimaryOnly) {
                for (int i = 0; i < FileManager.duplicatedFilesPrimaryOnly.Count; i++)
                    for (int p = 0; p < FileManager.duplicatedFilesPrimaryOnly[i].Count; p++)
                        FileManager.duplicatedFilesPrimaryOnly[i][p].Path = new string(FileManager.duplicatedFilesPrimaryOnly[i][p].Path.Skip(primaryDirectory.Length).ToArray());
                for (int i = 0; i < FileManager.emptyDirectoriesPrimary.Count; i++)
                    FileManager.emptyDirectoriesPrimary[i].Path = new string(FileManager.emptyDirectoriesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());
                for (int i = 0; i < FileManager.emptyFilesPrimary.Count; i++)
                    FileManager.emptyFilesPrimary[i].Path = new string(FileManager.emptyFilesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());

                FileManager.duplicatedFilesPrimaryOnly.Refresh();
                FileManager.emptyDirectoriesPrimary.Refresh();
                FileManager.emptyFilesPrimary.Refresh();
            } else {
                for (int i = 0; i < FileManager.duplicatedFiles.Count; i++) {
                    for (int p = 0; p < FileManager.duplicatedFiles[i].Item1.Count; p++)
                        FileManager.duplicatedFiles[i].Item1[p].Path = new string(FileManager.duplicatedFiles[i].Item1[p].Path.Skip(primaryDirectory.Length).ToArray());
                    for (int s = 0; s < FileManager.duplicatedFiles[i].Item2.Count; s++)
                        FileManager.duplicatedFiles[i].Item2[s].Path = new string(FileManager.duplicatedFiles[i].Item2[s].Path.Skip(secondaryDirectory.Length).ToArray());
                }
                for (int i = 0; i < FileManager.emptyDirectoriesPrimary.Count; i++)
                    FileManager.emptyDirectoriesPrimary[i].Path = new string(FileManager.emptyDirectoriesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());
                for (int i = 0; i < FileManager.emptyFilesPrimary.Count; i++)
                    FileManager.emptyFilesPrimary[i].Path = new string(FileManager.emptyFilesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());
                for (int i = 0; i < FileManager.emptyDirectoriesSecondary.Count; i++)
                    FileManager.emptyDirectoriesSecondary[i].Path = new string(FileManager.emptyDirectoriesSecondary[i].Path.Skip(secondaryDirectory.Length).ToArray());
                for (int i = 0; i < FileManager.emptyFilesSecondary.Count; i++)
                    FileManager.emptyFilesSecondary[i].Path = new string(FileManager.emptyFilesSecondary[i].Path.Skip(secondaryDirectory.Length).ToArray());

                FileManager.duplicatedFiles.Refresh();
                FileManager.emptyDirectoriesPrimary.Refresh();
                FileManager.emptyFilesPrimary.Refresh();
                FileManager.emptyDirectoriesSecondary.Refresh();
                FileManager.emptyFilesSecondary.Refresh();
            }
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
            //// chk empty or null string please specify directory to search in

            try {
                Directory.GetAccessControl(primaryDirectory);
            } catch (UnauthorizedAccessException) {
                error = true;
                Utility.LogFromNonGUIThread("Primary directory is not accessible.");
            } catch {
                error = true;
                if (!Directory.Exists(primaryDirectory))
                    Utility.LogFromNonGUIThread("Primary directory does not exist.");
                else
                    Utility.LogFromNonGUIThread("Unknown error in primary directory.");
            }

            if (error) {
                Utility.BeginInvoke((Action)(() => {
                    StatusBarViewModel.State = "Failed";
                    StatusBarViewModel.ShowProgress = false;
                    UnlockGUI();
                }));
                return;
            }

            FileManager.FindDuplicatedFiles(primaryDirectory, ShowBasePaths);

            Utility.BeginInvoke((Action)(() => {
                if (!SortBySizePrimaryOnly)
                    FileManager.duplicatedFilesPrimaryOnly.Sort(Comparer<ObservableRangeCollection<FileEntry>>.Create((a, b) => a[0].Path.CompareTo(b[0].Path)));
                FinalizeDuplicateFinding();
            }));
        }


        private void FindDuplicatedFilesInBoth() {
            bool error = false;

            if (primaryDirectory.ToUpperInvariant() == secondaryDirectory.ToUpperInvariant()) {
                error = true;
                Utility.LogFromNonGUIThread("Primary and secondary directories must be different.");
            }

            Utility.CheckDirectories(primaryDirectory, secondaryDirectory, ref error);
            if (primaryDirectory.IsSubDirectoryOf(secondaryDirectory)) {
                error = true;
                Utility.LogFromNonGUIThread("Primary directory cannot be a subdirectory of secondary directory.");
            }
            if (secondaryDirectory.IsSubDirectoryOf(primaryDirectory)) {
                error = true;
                Utility.LogFromNonGUIThread("Secondary directory cannot be a subdirectory of primary directory.");
            }

            if (error) {
                Utility.BeginInvoke((Action)(() => {
                    StatusBarViewModel.State = "Failed";
                    StatusBarViewModel.ShowProgress = false;
                    UnlockGUI();
                }));
                return;
            }

            FileManager.FindDuplicatedFiles(primaryDirectory, secondaryDirectory, ShowBasePaths);

            Utility.BeginInvoke((Action)(() => {
                if (!SortBySize)
                    FileManager.duplicatedFiles.Sort(Comparer<Tuple<ObservableRangeCollection<FileEntry>, ObservableRangeCollection<FileEntry>>>.Create((a, b) => a.Item1[0].Path.CompareTo(b.Item1[0].Path)));
                FinalizeDuplicateFinding();
            }));
        }

        private void FinalizeDuplicateFinding() {
            if (stopTask) {
                stopTask = false;
                StatusBarViewModel.State = "Stopped";
            } else
                StatusBarViewModel.State = "Done";
            StatusBarViewModel.ShowProgress = false;
            StatusBarViewModel.StateInfo = "";
            UnlockGUI();
        }

        public void RemoveFile(string path, string baseDirectory) {
            if (!ShowBasePaths)
                path = baseDirectory + path;
            /// try catch log here
            if (FileManager.RemoveFile(path, backupFiles, askLarge))
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
            ///LockGUI();
            IsGUIEnabled = false;
            StatusBarViewModel.State = "Initializing";
            StatusBarViewModel.Progress = 0;
            StatusBarViewModel.IsIndeterminate = false;
            StatusBarViewModel.ShowProgress = true;
            FileManager.primaryFiles.Clear();
            FileManager.secondaryFiles.Clear();

            //logListView.Items.Clear();
            /// should be through MainTabControl
            Utility.logListView.Items.Clear();

            FileManager.emptyDirectoriesPrimary.Clear();
            FileManager.emptyFilesPrimary.Clear();
            FileManager.emptyDirectoriesSecondary.Clear();
            FileManager.emptyFilesSecondary.Clear();
            FileManager.duplicatedFiles.Clear();
            FileManager.duplicatedFilesPrimaryOnly.Clear();

            primaryDirectory = DirectoryPickerViewModel.PrimaryDirectory;
            secondaryDirectory = DirectoryPickerViewModel.SecondaryDirectory;
            FileManager.ClearDirectory(tmpDirectory);
            FileManager.storedFiles.Clear();
            IsRestorePossible = false;
        }

        ///make private
        public void LockGUI() {
            IsGUIEnabled = false;
        }

        ///make private
        public void UnlockGUI() {
            IsGUIEnabled = true;
        }

        internal void WindowClosing() {
            StopTask();
            FileManager.ClearDirectory(tmpDirectory);
        }
    }
}
