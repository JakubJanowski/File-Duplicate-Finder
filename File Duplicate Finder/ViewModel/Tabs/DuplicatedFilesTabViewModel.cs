using FileDuplicateFinder.Models;
using Prism.Commands;

namespace FileDuplicateFinder.ViewModel {
    class DuplicatedFilesTabViewModel: ObjectBase {
        //// todo fix method names (can skip primaryonly in names, etc)
        private readonly ApplicationState state;
        private readonly DirectoryPickerViewModel directoryPickerViewModel;
        private readonly MainTabControlViewModel mainTabControlViewModel;
        private readonly MainWindowViewModel mainWindowViewModel;


        public DuplicatedFilesTabViewModel(ApplicationState state, DirectoryPickerViewModel directoryPickerViewModel, MainTabControlViewModel mainTabControlViewModel, MainWindowViewModel mainWindowViewModel) {
            DuplicatedFilesPrimaryRemoveFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryRemoveFile);
            DuplicatedFilesPrimaryIgnoreFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryIgnoreFile);
            DuplicatedFilesSecondaryRemoveFileCommand = new DelegateCommand<object>(DuplicatedFilesSecondaryRemoveFile);
            DuplicatedFilesSecondaryIgnoreFileCommand = new DelegateCommand<object>(DuplicatedFilesSecondaryIgnoreFile);
            OpenFileDirectoryPrimaryCommand = new DelegateCommand<object>(mainTabControlViewModel.OpenFileDirectoryPrimary);
            OpenFileDirectorySecondaryCommand = new DelegateCommand<object>(mainTabControlViewModel.OpenFileDirectorySecondary);
            RemoveAllPrimaryCommand = new DelegateCommand<object>(RemoveAllPrimary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            RemoveAllSecondaryCommand = new DelegateCommand<object>(RemoveAllSecondary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            SortAlphabeticallyCommand = new DelegateCommand<object>(SortAlphabetically);
            SortBySizeCommand = new DelegateCommand<object>(SortBySize);

            this.state = state;
            this.directoryPickerViewModel = directoryPickerViewModel;
            this.mainTabControlViewModel = mainTabControlViewModel;
            this.mainWindowViewModel = mainWindowViewModel;
        }


        public bool ShowBasePaths {
            get => state.ShowBasePaths;
        }

        internal void OnUpdateGUIEnabled() => OnPropertyChanged(nameof(IsGUIEnabled));
        internal void OnUpdateShowBasePaths() => OnPropertyChanged(nameof(ShowBasePaths));

        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
        }

        public void ShowButtons(object sender) {
            mainTabControlViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender) {
            mainTabControlViewModel.HideButtons(sender);
        }

        public DelegateCommand<object> OpenFileDirectoryPrimaryCommand { get; private set; }
        public DelegateCommand<object> OpenFileDirectorySecondaryCommand { get; private set; }
        
        ///check if every remove and ignore function works, also check if the oneliner functions can be put in DelegateCommand definition in constructors
        public DelegateCommand<object> DuplicatedFilesPrimaryIgnoreFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryIgnoreFile(object sender) {
            mainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.DuplicatedFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> DuplicatedFilesPrimaryRemoveFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryRemoveFile(object sender) {
            mainTabControlViewModel.RemoveFileTemplate(sender, directoryPickerViewModel.PrimaryDirectory, FileManager.DuplicatedFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> DuplicatedFilesSecondaryIgnoreFileCommand { get; private set; }
        public void DuplicatedFilesSecondaryIgnoreFile(object sender) {
            mainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.DuplicatedFilesSecondaryIgnoreFile);
        }

        public DelegateCommand<object> DuplicatedFilesSecondaryRemoveFileCommand { get; private set; }
        public void DuplicatedFilesSecondaryRemoveFile(object sender) {
            mainTabControlViewModel.RemoveFileTemplate(sender, directoryPickerViewModel.SecondaryDirectory, FileManager.DuplicatedFilesSecondaryIgnoreFile);
        }

        public DelegateCommand<object> RemoveAllPrimaryCommand { get; private set; }
        public void RemoveAllPrimary(object obj) {
            mainTabControlViewModel.RemoveAllTemplate(directoryPickerViewModel.PrimaryDirectory, FileManager.RemoveAllPrimary);
        }

        public DelegateCommand<object> RemoveAllSecondaryCommand { get; private set; }
        public void RemoveAllSecondary(object obj) {
            mainTabControlViewModel.RemoveAllTemplate(directoryPickerViewModel.SecondaryDirectory, FileManager.RemoveAllSecondary);
        }

        public DelegateCommand<object> SortAlphabeticallyCommand { get; private set; }
        public void SortAlphabetically(object obj) {
            ////
            mainWindowViewModel.SortBySize = false;
            FileManager.SortAlphabetically();
        }

        public DelegateCommand<object> SortBySizeCommand { get; private set; }
        public void SortBySize(object obj) {
            ////
            mainWindowViewModel.SortBySize = true;
            if (ShowBasePaths)
                FileManager.SortBySize();
            else
                FileManager.SortBySize(directoryPickerViewModel.PrimaryDirectory);
        }
    }
}
