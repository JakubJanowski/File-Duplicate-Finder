using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FileDuplicateFinder.ViewModel {
    class MainTabControlViewModel: ObjectBase {
        private const int pathChildPosition = 1;
        private bool isGUIEnabled = true;

        private bool showBasePaths = false;
        private volatile bool stopTask = false;
        //tmp
        //public View.MainTabControlView mainTabControlView;
        private bool primaryOnly = false;

        internal DirectoryPickerViewModel DirectoryPickerViewModel { private get; set; }
        internal StatusBarViewModel StatusBarViewModel { private get; set; }

        public MainTabControlViewModel() {
            OpenDirectoryPrimaryCommand = new DelegateCommand<object>(OpenDirectoryPrimary);
            OpenDirectorySecondaryCommand = new DelegateCommand<object>(OpenDirectorySecondary);
            OpenFileDirectoryPrimaryCommand = new DelegateCommand<object>(OpenFileDirectoryPrimary);
            OpenFileDirectorySecondaryCommand = new DelegateCommand<object>(OpenFileDirectorySecondary);
            EmptyDirectoriesPrimaryRemoveFileCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryRemoveFile);
            EmptyDirectoriesPrimaryIgnoreFileCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryIgnoreFile);
            EmptyFilesPrimaryRemoveFileCommand = new DelegateCommand<object>(EmptyFilesPrimaryRemoveFile);
            EmptyFilesPrimaryIgnoreFileCommand = new DelegateCommand<object>(EmptyFilesPrimaryIgnoreFile);
            EmptyDirectoriesSecondaryRemoveFileCommand = new DelegateCommand<object>(EmptyDirectoriesSecondaryRemoveFile);
            EmptyDirectoriesSecondaryIgnoreFileCommand = new DelegateCommand<object>(EmptyDirectoriesSecondaryIgnoreFile);
            EmptyFilesSecondaryRemoveFileCommand = new DelegateCommand<object>(EmptyFilesSecondaryRemoveFile);
            EmptyFilesSecondaryIgnoreFileCommand = new DelegateCommand<object>(EmptyFilesSecondaryIgnoreFile);
            DuplicatedFilesPrimaryRemoveFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryRemoveFile);
            DuplicatedFilesPrimaryIgnoreFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryIgnoreFile);
            DuplicatedFilesSecondaryRemoveFileCommand = new DelegateCommand<object>(DuplicatedFilesSecondaryRemoveFile);
            DuplicatedFilesSecondaryIgnoreFileCommand = new DelegateCommand<object>(DuplicatedFilesSecondaryIgnoreFile);
            DuplicatedFilesPrimaryOnlyRemoveFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryOnlyRemoveFile);
            DuplicatedFilesPrimaryOnlyIgnoreFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryOnlyIgnoreFile);

            RemoveAllEmptyDirectoriesPrimaryCommand = new DelegateCommand<object>(RemoveAllEmptyDirectoriesPrimary, (o) => IsGUIEnabled);
            RemoveAllEmptyDirectoriesSecondaryCommand = new DelegateCommand<object>(RemoveAllEmptyDirectoriesSecondary, (o) => IsGUIEnabled);
            RemoveAllEmptyFilesPrimaryCommand = new DelegateCommand<object>(RemoveAllEmptyFilesPrimary, (o) => IsGUIEnabled);
            RemoveAllEmptyFilesSecondaryCommand = new DelegateCommand<object>(RemoveAllEmptyFilesSecondary, (o) => IsGUIEnabled);
            RemoveAllPrimaryCommand = new DelegateCommand<object>(RemoveAllPrimary, (o) => IsGUIEnabled);
            RemoveAllSecondaryCommand = new DelegateCommand<object>(RemoveAllSecondary, (o) => IsGUIEnabled);

            SortAlphabeticallyCommand = new DelegateCommand<object>(SortAlphabetically);
            SortBySizeCommand = new DelegateCommand<object>(SortBySize);
            SortAlphabeticallyPrimaryOnlyCommand = new DelegateCommand<object>(SortAlphabeticallyPrimaryOnly);
            SortBySizePrimaryOnlyCommand = new DelegateCommand<object>(SortBySizePrimaryOnly);

        }

        public bool IsGUIEnabled {
            get => isGUIEnabled;
            set {
                if (isGUIEnabled != value) {
                    isGUIEnabled = value;
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
                    OnPropertyChanged("ShowBasePaths");
                }
            }
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            Border border = sender as Border;
            Grid grid = border.Child as Grid;
            border.Background = Brushes.AliceBlue;
            foreach (var child in grid.Children)
                if (child is Button)
                    ((Button)child).Visibility = Visibility.Visible;
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            Border border = sender as Border;
            Grid grid = border.Child as Grid;
            border.Background = Brushes.Transparent;
            foreach (var child in grid.Children)
                if (child is Button)
                    ((Button)child).Visibility = Visibility.Collapsed;
        }

        public DelegateCommand<object> OpenDirectoryPrimaryCommand { get; private set; }
        public void OpenDirectoryPrimary(object sender) {
            // set this once per search maybe
            //string DirectoryPickerViewModel.PrimaryDirectory = DirectoryPickerViewModel.PrimaryDirectory;
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

        public DelegateCommand<object> EmptyDirectoriesPrimaryRemoveFileCommand { get; private set; }
        public void EmptyDirectoriesPrimaryRemoveFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, DirectoryPickerViewModel.PrimaryDirectory);
            FileManager.EmptyDirectoriesPrimaryIgnoreFile(path);
        }

        public DelegateCommand<object> EmptyDirectoriesPrimaryIgnoreFileCommand { get; private set; }
        public void EmptyDirectoriesPrimaryIgnoreFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.EmptyDirectoriesPrimaryIgnoreFile(path);
        }

        public DelegateCommand<object> EmptyFilesPrimaryRemoveFileCommand { get; private set; }
        public void EmptyFilesPrimaryRemoveFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, DirectoryPickerViewModel.PrimaryDirectory);
            FileManager.EmptyFilesPrimaryIgnoreFile(path);
        }

        public DelegateCommand<object> EmptyFilesPrimaryIgnoreFileCommand { get; private set; }
        public void EmptyFilesPrimaryIgnoreFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.EmptyFilesPrimaryIgnoreFile(path);
        }

        public DelegateCommand<object> EmptyDirectoriesSecondaryRemoveFileCommand { get; private set; }
        public void EmptyDirectoriesSecondaryRemoveFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, DirectoryPickerViewModel.SecondaryDirectory);
            FileManager.EmptyDirectoriesSecondaryIgnoreFile(path);
        }

        public DelegateCommand<object> EmptyDirectoriesSecondaryIgnoreFileCommand { get; private set; }
        public void EmptyDirectoriesSecondaryIgnoreFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.EmptyDirectoriesSecondaryIgnoreFile(path);
        }

        public DelegateCommand<object> EmptyFilesSecondaryRemoveFileCommand { get; private set; }
        public void EmptyFilesSecondaryRemoveFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, DirectoryPickerViewModel.SecondaryDirectory);
            FileManager.EmptyFilesSecondaryIgnoreFile(path);
        }

        public DelegateCommand<object> EmptyFilesSecondaryIgnoreFileCommand { get; private set; }
        public void EmptyFilesSecondaryIgnoreFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.EmptyFilesSecondaryIgnoreFile(path);
        }

        public DelegateCommand<object> DuplicatedFilesPrimaryRemoveFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryRemoveFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, DirectoryPickerViewModel.PrimaryDirectory);
            FileManager.DuplicatedFilesPrimaryIgnoreFile(path);
        }

        public DelegateCommand<object> DuplicatedFilesPrimaryIgnoreFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryIgnoreFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.DuplicatedFilesPrimaryIgnoreFile(path);
        }

        public DelegateCommand<object> DuplicatedFilesSecondaryRemoveFileCommand { get; private set; }
        public void DuplicatedFilesSecondaryRemoveFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, DirectoryPickerViewModel.SecondaryDirectory);
            FileManager.DuplicatedFilesSecondaryIgnoreFile(path);
        }

        public DelegateCommand<object> DuplicatedFilesSecondaryIgnoreFileCommand { get; private set; }
        public void DuplicatedFilesSecondaryIgnoreFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.DuplicatedFilesSecondaryIgnoreFile(path);
        }

        public DelegateCommand<object> DuplicatedFilesPrimaryOnlyRemoveFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryOnlyRemoveFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, DirectoryPickerViewModel.PrimaryDirectory);
            FileManager.DuplicatedFilesPrimaryOnlyIgnoreFile(path);
        }

        public DelegateCommand<object> DuplicatedFilesPrimaryOnlyIgnoreFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryOnlyIgnoreFile(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.DuplicatedFilesPrimaryOnlyIgnoreFile(path);
        }

        private void RemoveFile(string path, string baseDirectory) {
            /// temporary solution
            ((MainWindow)Application.Current.MainWindow).RemoveFile(path, baseDirectory);
        }



        public DelegateCommand<object> RemoveAllEmptyDirectoriesPrimaryCommand { get; private set; }
        public void RemoveAllEmptyDirectoriesPrimary(object obj) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + DirectoryPickerViewModel.PrimaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.emptyDirectoriesPrimary.Count);
            /// temporary solution
            ((MainWindow)Application.Current.MainWindow).LockGUI();
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllEmptyDirectoriesPrimary();
                else
                    FileManager.RemoveAllEmptyDirectoriesPrimary(DirectoryPickerViewModel.PrimaryDirectory);
                Utility.BeginInvokeFromNonGUIThread(() => {
                    FinishProgress("Done");
                    /// temporary solution
                    ((MainWindow)Application.Current.MainWindow).UnlockGUI();
                });
            }).Start();
        }

        public DelegateCommand<object> RemoveAllEmptyDirectoriesSecondaryCommand { get; private set; }
        public void RemoveAllEmptyDirectoriesSecondary(object obj) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + DirectoryPickerViewModel.SecondaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.emptyDirectoriesSecondary.Count);
            /// temporary solution
            ((MainWindow)Application.Current.MainWindow).LockGUI();
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllEmptyDirectoriesSecondary();
                else
                    FileManager.RemoveAllEmptyDirectoriesSecondary(DirectoryPickerViewModel.SecondaryDirectory);
                Utility.BeginInvokeFromNonGUIThread(() => {
                    FinishProgress("Done");
                    /// temporary solution
                    ((MainWindow)Application.Current.MainWindow).UnlockGUI();
                });
            }).Start();
        }

        public DelegateCommand<object> RemoveAllEmptyFilesPrimaryCommand { get; private set; }
        public void RemoveAllEmptyFilesPrimary(object obj) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + DirectoryPickerViewModel.PrimaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.emptyFilesPrimary.Count);
            /// temporary solution
            ((MainWindow)Application.Current.MainWindow).LockGUI();
            StatusBarViewModel.State = "Removing files...";
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllEmptyFilesPrimary();
                else
                    FileManager.RemoveAllEmptyFilesPrimary(DirectoryPickerViewModel.PrimaryDirectory);
                Utility.BeginInvokeFromNonGUIThread(() => {
                    FinishProgress("Done");
                    /// temporary solution
                    ((MainWindow)Application.Current.MainWindow).UnlockGUI();
                });
            }).Start();
        }

        public DelegateCommand<object> RemoveAllEmptyFilesSecondaryCommand { get; private set; }
        public void RemoveAllEmptyFilesSecondary(object obj) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + DirectoryPickerViewModel.SecondaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.emptyFilesSecondary.Count);
            /// temporary solution
            ((MainWindow)Application.Current.MainWindow).LockGUI();
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllEmptyFilesSecondary();
                else
                    FileManager.RemoveAllEmptyFilesSecondary(DirectoryPickerViewModel.SecondaryDirectory);
                Utility.BeginInvokeFromNonGUIThread(() => {
                    FinishProgress("Done");
                    /// temporary solution
                    ((MainWindow)Application.Current.MainWindow).UnlockGUI();
                });
            }).Start();
        }

        public DelegateCommand<object> RemoveAllPrimaryCommand { get; private set; }
        public void RemoveAllPrimary(object obj) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + DirectoryPickerViewModel.PrimaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.duplicatedFiles.Count);
            /// temporary solution
            ((MainWindow)Application.Current.MainWindow).LockGUI();
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllPrimary();
                else
                    FileManager.RemoveAllPrimary(DirectoryPickerViewModel.PrimaryDirectory);
                Utility.BeginInvokeFromNonGUIThread(() => {
                    FinishProgress("Done");
                    /// temporary solution
                    ((MainWindow)Application.Current.MainWindow).UnlockGUI();
                });
            }).Start();
        }

        public DelegateCommand<object> RemoveAllSecondaryCommand { get; private set; }
        public void RemoveAllSecondary(object obj) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + DirectoryPickerViewModel.SecondaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.duplicatedFiles.Count);
            /// temporary solution
            ((MainWindow)Application.Current.MainWindow).LockGUI();
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllSecondary();
                else
                    FileManager.RemoveAllSecondary(DirectoryPickerViewModel.SecondaryDirectory);
                Utility.BeginInvokeFromNonGUIThread(() => {  /// check if this could be changed to not use invoke at all, then this utility method can be erased
                    FinishProgress("Done");
                    /// temporary solution
                    ((MainWindow)Application.Current.MainWindow).UnlockGUI();
                });
            }).Start();
        }

        private void InitProgress(string state) {
            StatusBarViewModel.Progress = 0;
            StatusBarViewModel.ShowProgress = true;
            StatusBarViewModel.State = state;
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

        public void StopTask() {
            stopTask = true;
        }


        public DelegateCommand<object> SortAlphabeticallyCommand { get; private set; }
        public void SortAlphabetically(object obj) {
            ((MainWindow)Application.Current.MainWindow).SortBySize = false;
            FileManager.SortAlphabetically();
        }

        public DelegateCommand<object> SortBySizeCommand { get; private set; }
        public void SortBySize(object obj) {
            ///
            ((MainWindow)Application.Current.MainWindow).SortBySize = true;
            if (showBasePaths)
                FileManager.SortBySize();
            else
                FileManager.SortBySize(DirectoryPickerViewModel.PrimaryDirectory);
        }

        public DelegateCommand<object> SortAlphabeticallyPrimaryOnlyCommand { get; private set; }
        public void SortAlphabeticallyPrimaryOnly(object obj) {
            ///
            ((MainWindow)Application.Current.MainWindow).SortBySizePrimaryOnly = false;
            FileManager.SortAlphabeticallyPrimaryOnly();
        }

        public DelegateCommand<object> SortBySizePrimaryOnlyCommand { get; private set; }
        public void SortBySizePrimaryOnly(object obj) {
            ///
            ((MainWindow)Application.Current.MainWindow).SortBySizePrimaryOnly = true;
            if (showBasePaths)
                FileManager.SortBySizePrimaryOnly();
            else
                FileManager.SortBySizePrimaryOnly(DirectoryPickerViewModel.PrimaryDirectory);
        }
    }
}
