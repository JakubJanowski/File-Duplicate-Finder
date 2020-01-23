using FileDuplicateFinder.Models;
using Prism.Commands;
using System;

namespace FileDuplicateFinder.ViewModel {
    class DuplicatedFilesTabViewModel: ObjectBase {
        //// fix method names (can skip primaryonly in names, etc)
        private bool showBasePaths = false;
        private bool isGUIEnabled = true;

        private MainTabControlViewModel mainTabControlViewModel;

        internal MainTabControlViewModel MainTabControlViewModel {
            private get => mainTabControlViewModel;
            set {
                mainTabControlViewModel = value;
                OpenFileDirectoryPrimaryCommand = new DelegateCommand<object>(value.OpenFileDirectoryPrimary);
                OpenFileDirectorySecondaryCommand = new DelegateCommand<object>(value.OpenFileDirectorySecondary);
            }
        }

        internal DirectoryPickerViewModel DirectoryPickerViewModel { private get; set; }
        internal MainWindowViewModel MainWindowViewModel { private get; set; }

        private ApplicationState state;

        public DuplicatedFilesTabViewModel(ApplicationState state) {
            this.state = state;
            DuplicatedFilesPrimaryRemoveFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryRemoveFile);
            DuplicatedFilesPrimaryIgnoreFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryIgnoreFile);
            DuplicatedFilesSecondaryRemoveFileCommand = new DelegateCommand<object>(DuplicatedFilesSecondaryRemoveFile);
            DuplicatedFilesSecondaryIgnoreFileCommand = new DelegateCommand<object>(DuplicatedFilesSecondaryIgnoreFile);
            RemoveAllPrimaryCommand = new DelegateCommand<object>(RemoveAllPrimary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            RemoveAllSecondaryCommand = new DelegateCommand<object>(RemoveAllSecondary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            SortAlphabeticallyCommand = new DelegateCommand<object>(SortAlphabetically);
            SortBySizeCommand = new DelegateCommand<object>(SortBySize);
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

        internal void OnUpdateGUIEnabled() => OnPropertyChanged("IsGUIEnabled");

        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
        }

        public void ShowButtons(object sender) {
            MainTabControlViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender) {
            MainTabControlViewModel.HideButtons(sender);
        }

        public DelegateCommand<object> OpenFileDirectoryPrimaryCommand { get; private set; }
        public DelegateCommand<object> OpenFileDirectorySecondaryCommand { get; private set; }
        
        ///check if every remove and ignore function works, also check if the oneliner functions can be put in DelegateCommand definition in constructors
        public DelegateCommand<object> DuplicatedFilesPrimaryIgnoreFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryIgnoreFile(object sender) {
            MainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.DuplicatedFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> DuplicatedFilesPrimaryRemoveFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryRemoveFile(object sender) {
            MainTabControlViewModel.RemoveFileTemplate(sender, DirectoryPickerViewModel.PrimaryDirectory, FileManager.DuplicatedFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> DuplicatedFilesSecondaryIgnoreFileCommand { get; private set; }
        public void DuplicatedFilesSecondaryIgnoreFile(object sender) {
            MainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.DuplicatedFilesSecondaryIgnoreFile);
        }

        public DelegateCommand<object> DuplicatedFilesSecondaryRemoveFileCommand { get; private set; }
        public void DuplicatedFilesSecondaryRemoveFile(object sender) {
            MainTabControlViewModel.RemoveFileTemplate(sender, DirectoryPickerViewModel.SecondaryDirectory, FileManager.DuplicatedFilesSecondaryIgnoreFile);
        }

        public DelegateCommand<object> RemoveAllPrimaryCommand { get; private set; }
        public void RemoveAllPrimary(object obj) {
            MainTabControlViewModel.RemoveAllTemplate(DirectoryPickerViewModel.PrimaryDirectory, FileManager.RemoveAllPrimary);
        }

        public DelegateCommand<object> RemoveAllSecondaryCommand { get; private set; }
        public void RemoveAllSecondary(object obj) {
            MainTabControlViewModel.RemoveAllTemplate(DirectoryPickerViewModel.SecondaryDirectory, FileManager.RemoveAllSecondary);
        }

        public DelegateCommand<object> SortAlphabeticallyCommand { get; private set; }
        public void SortAlphabetically(object obj) {
            ////
            MainWindowViewModel.SortBySize = false;
            FileManager.SortAlphabetically();
        }

        public DelegateCommand<object> SortBySizeCommand { get; private set; }
        public void SortBySize(object obj) {
            ////
            MainWindowViewModel.SortBySize = true;
            if (ShowBasePaths)
                FileManager.SortBySize();
            else
                FileManager.SortBySize(DirectoryPickerViewModel.PrimaryDirectory);
        }
    }
}
