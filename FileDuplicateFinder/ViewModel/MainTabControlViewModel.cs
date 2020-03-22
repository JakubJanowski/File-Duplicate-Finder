using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using FileDuplicateFinder.Enums;
using FileDuplicateFinder.Models;
using FileDuplicateFinder.Services;
using FileDuplicateFinder.Utilities;
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

        private bool showPrimaryOnlyTabs = true;
        private const int pathChildPosition = 1;
        private volatile bool stopTask = false;

        private readonly ApplicationState state;
        private readonly DirectoryPickerViewModel directoryPickerViewModel;
        private readonly MainWindowViewModel mainWindowViewModel;
        private readonly StatusBarViewModel statusBarViewModel;

        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
        }

        public bool ShowBasePaths {
            get => state.ShowBasePaths;
        }

        public bool ShowPrimaryOnlyTabs {
            get => showPrimaryOnlyTabs;
            set {
                if (showPrimaryOnlyTabs != value) {
                    showPrimaryOnlyTabs = value;
                    OnPropertyChanged(nameof(ShowPrimaryOnlyTabs));
                }
            }
        }

        internal void OnUpdateGUIEnabled() {
            DuplicatedFilesTabViewModel.OnUpdateGUIEnabled();
            EmptyDirectoriesTabControlViewModel.OnUpdateGUIEnabled();
            EmptyFilesTabControlViewModel.OnUpdateGUIEnabled();
            PrimaryOnlyEmptyDirectoriesTabViewModel.OnUpdateGUIEnabled();
            PrimaryOnlyEmptyFilesTabViewModel.OnUpdateGUIEnabled();
            OnPropertyChanged(nameof(IsGUIEnabled));
        }

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

            FileManager.RemoveProgressUpdated += RemoveProgerssUpdatedHandler;
        }

        public DelegateCommand<object> OpenDirectoryPrimaryCommand { get; private set; }
        public void OpenDirectoryPrimary(object sender) {
            /// set this once per search maybe
            ///string DirectoryPickerViewModel.PrimaryDirectory = DirectoryPickerViewModel.PrimaryDirectory;
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            string fullPath = path;
            if (!ShowBasePaths)
                fullPath = directoryPickerViewModel.PrimaryDirectory + path;

            try {
                Process.Start(fullPath);
            } catch (Win32Exception) {
                FileManager.emptyDirectoriesPrimary.Remove(path);
                CommonUtilities.Log("Directory \"" + fullPath + "\" no longer exists.");
            }
        }

        public DelegateCommand<object> OpenDirectorySecondaryCommand { get; private set; }
        public void OpenDirectorySecondary(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            string fullPath = path;
            if (!ShowBasePaths)
                fullPath = directoryPickerViewModel.SecondaryDirectory + path;

            try {
                Process.Start(fullPath);
            } catch (Win32Exception) {
                FileManager.emptyDirectoriesSecondary.Remove(path);
                CommonUtilities.Log("Directory \"" + fullPath + "\" no longer exists.");
            }
        }

        public DelegateCommand<object> OpenFileDirectoryPrimaryCommand { get; private set; }

        public void OpenFileDirectoryPrimary(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            string fullPath = path;
            if (!ShowBasePaths)
                fullPath = directoryPickerViewModel.PrimaryDirectory + path;

            if (File.Exists(fullPath))
                Process.Start("explorer.exe", "/select, \"" + fullPath + "\"");
            else {
                FileManager.RemovePrimaryFileWithPath(path);
                CommonUtilities.Log("File \"" + fullPath + "\" no longer exists.");
            }
        }

        public DelegateCommand<object> OpenFileDirectorySecondaryCommand { get; private set; }
        public void OpenFileDirectorySecondary(object sender) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            string fullPath = path;
            if (!ShowBasePaths)
                fullPath = directoryPickerViewModel.SecondaryDirectory + path;

            if (File.Exists(fullPath))
                Process.Start("explorer.exe", "/select, \"" + fullPath + "\"");
            else {
                FileManager.RemoveSecondaryFileWithPath(path);
                CommonUtilities.Log("File \"" + fullPath + "\" no longer exists.");
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
            mainWindowViewModel.IsGUIEnabled = false;
            new Thread(() => {
                if (ShowBasePaths)
                    action();
                else
                    action(directory);
                CommonUtilities.BeginInvoke(() => {
                    FinishProgress("Done");
                    mainWindowViewModel.IsGUIEnabled = true;
                });
            }).Start();
        }

        private void RemoveProgerssUpdatedHandler(RemoveProgress progress) {
            CommonUtilities.BeginInvoke(() => {
                switch (progress.State) {
                    case RemoveProgressState.StartingRemoval:
                        statusBarViewModel.StateInfo = "0 / " + progress.MaxProgress;
                        statusBarViewModel.MaxProgress = progress.MaxProgress;
                        statusBarViewModel.Progress = 0;
                        break;
                    case RemoveProgressState.Removing:
                        statusBarViewModel.StateInfo = progress.Progress + " / " + progress.MaxProgress;
                        statusBarViewModel.Progress = progress.Progress;
                        break;
                }
            });
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
            } else {
                statusBarViewModel.State = state;
            }
        }
    }
}
