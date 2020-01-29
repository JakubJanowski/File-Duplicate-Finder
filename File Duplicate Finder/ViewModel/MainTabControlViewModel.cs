using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FileDuplicateFinder.Models;
using Prism.Commands;

namespace FileDuplicateFinder.ViewModel {

    class MainTabControlViewModelParameters {
        internal ApplicationState State { get; set; }
        internal DirectoryPickerViewModel DirectoryPickerViewModel { get; set; }
        internal MainWindowViewModel MainWindowViewModel { get; set; }
        internal StatusBarViewModel StatusBarViewModel { get; set; }
    }

    class MainTabControlViewModel: ObjectBase {
        public DuplicatedFilesTabViewModel DuplicatedFilesTabViewModel { get; }
        public EmptyDirectoriesTabControlViewModel EmptyDirectoriesTabControlViewModel { get; }
        public EmptyFilesTabControlViewModel EmptyFilesTabControlViewModel { get; }
        public PrimaryOnlyDuplicatedFilesTabViewModel PrimaryOnlyDuplicatedFilesTabViewModel { get; }
        public PrimaryOnlyEmptyDirectoriesTabViewModel PrimaryOnlyEmptyDirectoriesTabViewModel { get; }
        public PrimaryOnlyEmptyFilesTabViewModel PrimaryOnlyEmptyFilesTabViewModel { get; }

        private const int pathChildPosition = 1;
        private volatile bool stopTask = false;

        private readonly ApplicationState state;
        private readonly DirectoryPickerViewModel directoryPickerViewModel;
        private readonly MainWindowViewModel mainWindowViewModel;
        private readonly StatusBarViewModel statusBarViewModel;



        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
        }

        public bool PrimaryOnly {
            get => state.PrimaryOnly;
        }

        public bool ShowBasePaths {
            get => state.ShowBasePaths;
        }

        internal void OnUpdateGUIEnabled() {
            DuplicatedFilesTabViewModel.OnUpdateGUIEnabled();
            EmptyDirectoriesTabControlViewModel.OnUpdateGUIEnabled();
            EmptyFilesTabControlViewModel.OnUpdateGUIEnabled();
            PrimaryOnlyEmptyDirectoriesTabViewModel.OnUpdateGUIEnabled();
            PrimaryOnlyEmptyFilesTabViewModel.OnUpdateGUIEnabled();
            OnPropertyChanged(nameof(IsGUIEnabled));
        }

        internal void OnUpdatePrimaryOnly() => OnPropertyChanged(nameof(PrimaryOnly));

        internal void OnUpdateShowBasePaths() {
            DuplicatedFilesTabViewModel.OnUpdateShowBasePaths();
            PrimaryOnlyDuplicatedFilesTabViewModel.OnUpdateShowBasePaths();
            OnPropertyChanged(nameof(ShowBasePaths));
        }

        public MainTabControlViewModel(MainTabControlViewModelParameters parameters) {
            state = parameters.State;
            directoryPickerViewModel = parameters.DirectoryPickerViewModel;
            mainWindowViewModel = parameters.MainWindowViewModel;
            statusBarViewModel = parameters.StatusBarViewModel;

            DuplicatedFilesTabViewModel = new DuplicatedFilesTabViewModel(state, directoryPickerViewModel, this, mainWindowViewModel);
            EmptyDirectoriesTabControlViewModel = new EmptyDirectoriesTabControlViewModel(state, directoryPickerViewModel, this);
            EmptyFilesTabControlViewModel = new EmptyFilesTabControlViewModel(state, directoryPickerViewModel, this);
            PrimaryOnlyDuplicatedFilesTabViewModel = new PrimaryOnlyDuplicatedFilesTabViewModel(state, directoryPickerViewModel, this, mainWindowViewModel);
            PrimaryOnlyEmptyDirectoriesTabViewModel = new PrimaryOnlyEmptyDirectoriesTabViewModel(state, directoryPickerViewModel, this);
            PrimaryOnlyEmptyFilesTabViewModel = new PrimaryOnlyEmptyFilesTabViewModel(state, directoryPickerViewModel, this);
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
            if (!ShowBasePaths)
                path = directoryPickerViewModel.PrimaryDirectory + path;
            try {
                Process.Start(path);
            } catch (Win32Exception) {
                FileManager.emptyDirectoriesPrimary.Remove(((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text);
                Utility.Log("Directory \"" + path + "\" no longer exists.");
            }
        }

        public DelegateCommand<object> OpenDirectorySecondaryCommand { get; private set; }
        public void OpenDirectorySecondary(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            if (!ShowBasePaths)
                path = directoryPickerViewModel.SecondaryDirectory + path;
            try {
                Process.Start(path);
            } catch (Win32Exception) {
                FileManager.emptyDirectoriesSecondary.Remove(((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text);
                Utility.Log("Directory \"" + path + "\" no longer exists.");
            }
        }

        public DelegateCommand<object> OpenFileDirectoryPrimaryCommand { get; private set; }

        public void OpenFileDirectoryPrimary(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            if (!ShowBasePaths)
                path = directoryPickerViewModel.PrimaryDirectory + path;
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
                    OpenFileDirectoryPrimaryFound:
                    ;
                }
                Utility.Log("File \"" + path + "\" no longer exists.");
            }
        }

        public DelegateCommand<object> OpenFileDirectorySecondaryCommand { get; private set; }
        public void OpenFileDirectorySecondary(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            if (!ShowBasePaths)
                path = directoryPickerViewModel.SecondaryDirectory + path;
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
            /// temporary solution use parent.lockgui
            mainWindowViewModel.LockGUI();
            new Thread(() => {
                if (ShowBasePaths)
                    action();
                else
                    action(directory);
                Utility.BeginInvoke(() => {
                    FinishProgress("Done");
                    /// temporary solution
                    mainWindowViewModel.UnlockGUI();
                });
            }).Start();
        }

        public void StopTask() {
            stopTask = true;
        }

        private void RemoveFile(string path, string baseDirectory) {
            /// temporary solution
            mainWindowViewModel.RemoveFile(path, baseDirectory);
        }

        private void InitRemoveAllProgress(string state) {
            statusBarViewModel.Progress = 0;
            statusBarViewModel.State = state;
            statusBarViewModel.IsIndeterminate = false;
            statusBarViewModel.ShowProgress = true;
        }

        private void FinishProgress(string state) {
            statusBarViewModel.ShowProgress = false;
            statusBarViewModel.StateInfo = "";
            if (stopTask) {
                stopTask = false;
                statusBarViewModel.State = "Stopped";
            } else
                statusBarViewModel.State = state;
        }
    }
}
