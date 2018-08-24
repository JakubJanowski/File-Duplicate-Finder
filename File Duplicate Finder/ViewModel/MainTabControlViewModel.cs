using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Prism.Commands;

namespace FileDuplicateFinder.ViewModel {
    class MainTabControlViewModel: ObjectBase {
        private const int pathChildPosition = 1;
        private volatile bool stopTask = false;

        private bool isGUIEnabled = true;
        private bool primaryOnly = false;
        private bool showBasePaths = false;

        private DirectoryPickerViewModel directoryPickerViewModel;
        private MainWindowViewModel mainWindowViewModel;

        private DuplicatedFilesTabViewModel duplicatedFilesTabViewModel;
        private EmptyDirectoriesTabControlViewModel emptyDirectoriesTabControlViewModel;
        private EmptyFilesTabControlViewModel emptyFilesTabControlViewModel;
        private PrimaryOnlyDuplicatedFilesTabViewModel primaryOnlyDuplicatedFilesTabViewModel;
        private PrimaryOnlyEmptyDirectoriesTabViewModel primaryOnlyEmptyDirectoriesTabViewModel;
        private PrimaryOnlyEmptyFilesTabViewModel primaryOnlyEmptyFilesTabViewModel;

        internal DirectoryPickerViewModel DirectoryPickerViewModel {
            private get => directoryPickerViewModel;
            set {
                directoryPickerViewModel = value;
                DuplicatedFilesTabViewModel.DirectoryPickerViewModel = value;
                EmptyDirectoriesTabControlViewModel.DirectoryPickerViewModel = value;
                EmptyFilesTabControlViewModel.DirectoryPickerViewModel = value;
                PrimaryOnlyDuplicatedFilesTabViewModel.DirectoryPickerViewModel = value;
                PrimaryOnlyEmptyDirectoriesTabViewModel.DirectoryPickerViewModel = value;
                PrimaryOnlyEmptyFilesTabViewModel.DirectoryPickerViewModel = value;
            }
        }

        internal DuplicatedFilesTabViewModel DuplicatedFilesTabViewModel {
            private get => duplicatedFilesTabViewModel;
            set {
                duplicatedFilesTabViewModel = value;
                value.MainTabControlViewModel = this;
            }
        }

        internal EmptyDirectoriesTabControlViewModel EmptyDirectoriesTabControlViewModel {
            private get => emptyDirectoriesTabControlViewModel;
            set {
                emptyDirectoriesTabControlViewModel = value;
                value.MainTabControlViewModel = this;
            }
        }

        internal EmptyFilesTabControlViewModel EmptyFilesTabControlViewModel {
            private get => emptyFilesTabControlViewModel;
            set {
                emptyFilesTabControlViewModel = value;
                value.MainTabControlViewModel = this;
            }
        }

        internal PrimaryOnlyDuplicatedFilesTabViewModel PrimaryOnlyDuplicatedFilesTabViewModel {
            private get => primaryOnlyDuplicatedFilesTabViewModel;
            set {
                primaryOnlyDuplicatedFilesTabViewModel = value;
                value.MainTabControlViewModel = this;
            }
        }

        internal PrimaryOnlyEmptyDirectoriesTabViewModel PrimaryOnlyEmptyDirectoriesTabViewModel {
            private get => primaryOnlyEmptyDirectoriesTabViewModel;
            set {
                primaryOnlyEmptyDirectoriesTabViewModel = value;
                value.MainTabControlViewModel = this;
            }
        }

        internal PrimaryOnlyEmptyFilesTabViewModel PrimaryOnlyEmptyFilesTabViewModel {
            private get => primaryOnlyEmptyFilesTabViewModel;
            set {
                primaryOnlyEmptyFilesTabViewModel = value;
                value.MainTabControlViewModel = this;
            }
        }

        internal MainWindowViewModel MainWindowViewModel {
            private get => mainWindowViewModel;
            set {
                mainWindowViewModel = value;
                duplicatedFilesTabViewModel.MainWindowViewModel = value;
                primaryOnlyDuplicatedFilesTabViewModel.MainWindowViewModel = value;
            }
        }
        internal StatusBarViewModel StatusBarViewModel { private get; set; }

        public MainTabControlViewModel() {}

        public bool IsGUIEnabled {
            get => isGUIEnabled;
            set {
                if (isGUIEnabled != value) {
                    isGUIEnabled = value;
                    DuplicatedFilesTabViewModel.IsGUIEnabled = value;
                    EmptyDirectoriesTabControlViewModel.IsGUIEnabled = value;
                    EmptyFilesTabControlViewModel.IsGUIEnabled = value;
                    PrimaryOnlyEmptyDirectoriesTabViewModel.IsGUIEnabled = value;
                    PrimaryOnlyEmptyFilesTabViewModel.IsGUIEnabled = value;
                    OnPropertyChanged("IsGUIEnabled");
                }
            }
        }

        public bool PrimaryOnly {
            get => primaryOnly;
            set {
                if (primaryOnly != value) {
                    primaryOnly = value;
                    OnPropertyChanged("PrimaryOnly");
                }
            }
        }

        public bool ShowBasePaths {
            get => showBasePaths;
            set {
                if (showBasePaths != value) {
                    showBasePaths = value;
                    DuplicatedFilesTabViewModel.ShowBasePaths = value;
                    PrimaryOnlyDuplicatedFilesTabViewModel.ShowBasePaths = value;
                    OnPropertyChanged("ShowBasePaths");
                }
            }
        }

        internal void LockGUI() {
            IsGUIEnabled = false;
        }

        internal void UnlockGUI() {
            IsGUIEnabled = true;
        }

        public void ShowButtons(object sender) {
            Border border = sender as Border;
            Grid grid = border.Child as Grid;
            border.Background = Brushes.AliceBlue;
            foreach (var child in grid.Children)
                if (child is Button)
                    ((Button)child).Visibility = Visibility.Visible;
        }

        public void HideButtons(object sender) {
            Border border = sender as Border;
            Grid grid = border.Child as Grid;
            border.Background = Brushes.Transparent;
            foreach (var child in grid.Children)
                if (child is Button)
                    ((Button)child).Visibility = Visibility.Collapsed;
        }

        public DelegateCommand<object> OpenDirectoryPrimaryCommand { get; private set; }
        public void OpenDirectoryPrimary(object sender) {
            /// set this once per search maybe
            ///string DirectoryPickerViewModel.PrimaryDirectory = DirectoryPickerViewModel.PrimaryDirectory;
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            if (!showBasePaths)
                path = DirectoryPickerViewModel.PrimaryDirectory + path;
            try {
                Process.Start(path);
            }
            catch (Win32Exception) {
                FileManager.emptyDirectoriesPrimary.Remove(((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text);
                Utility.Log("Directory \"" + path + "\" no longer exists.");
            }
        }

        public DelegateCommand<object> OpenDirectorySecondaryCommand { get; private set; }
        public void OpenDirectorySecondary(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            if (!showBasePaths)
                path = DirectoryPickerViewModel.SecondaryDirectory + path;
            try {
                Process.Start(path);
            }
            catch (Win32Exception) {
                FileManager.emptyDirectoriesSecondary.Remove(((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text);
                Utility.Log("Directory \"" + path + "\" no longer exists.");
            }
        }

        public DelegateCommand<object> OpenFileDirectoryPrimaryCommand { get; private set; }
        public void OpenFileDirectoryPrimary(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            if (!showBasePaths)
                path = DirectoryPickerViewModel.PrimaryDirectory + path;
            if (File.Exists(path))
                Process.Start("explorer.exe", "/select, \"" + path + "\"");
            else {
                path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
                if (!FileManager.emptyFilesPrimary.Remove(path)) {
                    for (int i = 0; i < FileManager.duplicatedFilesPrimaryOnly.Count; i++) {
                        int index = FileManager.duplicatedFilesPrimaryOnly[i].FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            FileManager.duplicatedFilesPrimaryOnly[i].RemoveAt(index);
                            if (FileManager.duplicatedFilesPrimaryOnly[i].Count <= 1)
                                FileManager.duplicatedFilesPrimaryOnly.RemoveAt(i);
                            goto OpenFileDirectoryPrimaryFound;
                        }
                    }
                    for (int i = 0; i < FileManager.duplicatedFiles.Count; i++) {
                        int index = FileManager.duplicatedFiles[i].Item1.FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            FileManager.duplicatedFiles[i].Item1.RemoveAt(index);
                            if (FileManager.duplicatedFiles[i].Item2.Count == 0 && FileManager.duplicatedFiles[i].Item1.Count <= 1)
                                FileManager.duplicatedFiles.RemoveAt(i);
                            break;
                        }
                    }
                    OpenFileDirectoryPrimaryFound:;
                }
                Utility.Log("File \"" + path + "\" no longer exists.");
            }
        }

        public DelegateCommand<object> OpenFileDirectorySecondaryCommand { get; private set; }
        public void OpenFileDirectorySecondary(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            if (!showBasePaths)
                path = DirectoryPickerViewModel.SecondaryDirectory + path;
            if (File.Exists(path))
                Process.Start("explorer.exe", "/select, \"" + path + "\"");
            else {
                path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
                if (!FileManager.emptyFilesSecondary.Remove(path)) {
                    for (int i = 0; i < FileManager.duplicatedFiles.Count; i++) {
                        int index = FileManager.duplicatedFiles[i].Item2.FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            FileManager.duplicatedFiles[i].Item2.RemoveAt(index);
                            if (FileManager.duplicatedFiles[i].Item2.Count == 0 && FileManager.duplicatedFiles[i].Item1.Count <= 1)
                                FileManager.duplicatedFiles.RemoveAt(i);
                            break;
                        }
                    }
                }
                Utility.Log("File \"" + path + "\" no longer exists.");
            }
        }

        ///check which methods can be private in this class
        public void RemoveFileTemplate(object sender, string directory, Action<string> action) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, directory);
            action(path);
        }

        public void IgnoreFileTemplate(object sender, Action<string> action) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            action(path);
        }

        public delegate void RemoveAllAction(string baseDirectory = "");

        public void RemoveAllTemplate(string directory, RemoveAllAction action) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + directory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitRemoveAllProgress("Removing files...");
            /// temporary solution
            MainWindowViewModel.LockGUI();
            new Thread(() => {
                if (ShowBasePaths)
                    action();
                else
                    action(directory);
                Utility.BeginInvoke(() => {
                    FinishProgress("Done");
                    /// temporary solution
                    MainWindowViewModel.UnlockGUI();
                });
            }).Start();
        }

        public void StopTask() {
            stopTask = true;
        }

        private void RemoveFile(string path, string baseDirectory) {
            /// temporary solution
            MainWindowViewModel.RemoveFile(path, baseDirectory);
        }


        private void InitRemoveAllProgress(string state) {
            StatusBarViewModel.Progress = 0;
            StatusBarViewModel.State = state;
            StatusBarViewModel.IsIndeterminate = false;
            StatusBarViewModel.ShowProgress = true;
        }

        private void FinishProgress(string state) {
            StatusBarViewModel.ShowProgress = false;
            StatusBarViewModel.StateInfo = "";
            if (stopTask) {
                stopTask = false;
                StatusBarViewModel.State = "Stopped";
            }
            else
                StatusBarViewModel.State = state;
        }
    }
}
