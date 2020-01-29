using FileDuplicateFinder.Models;
using Prism.Commands;

namespace FileDuplicateFinder.ViewModel {
    internal class PrimaryOnlyDuplicatedFilesTabViewModel: ObjectBase {
        private readonly ApplicationState state;
        private readonly DirectoryPickerViewModel directoryPickerViewModel;
        private readonly MainTabControlViewModel mainTabControlViewModel;
        private readonly MainWindowViewModel mainWindowViewModel;

        public PrimaryOnlyDuplicatedFilesTabViewModel(ApplicationState state, DirectoryPickerViewModel directoryPickerViewModel, MainTabControlViewModel mainTabControlViewModel, MainWindowViewModel mainWindowViewModel) {
            DuplicatedFilesPrimaryOnlyRemoveFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryOnlyRemoveFile);
            DuplicatedFilesPrimaryOnlyIgnoreFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryOnlyIgnoreFile);
            OpenFileDirectoryPrimaryCommand = new DelegateCommand<object>(mainTabControlViewModel.OpenFileDirectoryPrimary);
            SortAlphabeticallyPrimaryOnlyCommand = new DelegateCommand<object>(SortAlphabeticallyPrimaryOnly);
            SortBySizePrimaryOnlyCommand = new DelegateCommand<object>(SortBySizePrimaryOnly);

            this.state = state;
            this.directoryPickerViewModel = directoryPickerViewModel;
            this.mainTabControlViewModel = mainTabControlViewModel;
            this.mainWindowViewModel = mainWindowViewModel;
        }

        public bool ShowBasePaths {
            get => state.ShowBasePaths;
        }

        internal void OnUpdateShowBasePaths() => OnPropertyChanged(nameof(ShowBasePaths));

        public void ShowButtons(object sender) {
            mainTabControlViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender) {
            mainTabControlViewModel.HideButtons(sender);
        }

        public DelegateCommand<object> OpenFileDirectoryPrimaryCommand { get; private set; }

        public DelegateCommand<object> DuplicatedFilesPrimaryOnlyRemoveFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryOnlyRemoveFile(object sender) {
            mainTabControlViewModel.RemoveFileTemplate(sender, directoryPickerViewModel.PrimaryDirectory, FileManager.DuplicatedFilesPrimaryOnlyIgnoreFile);
        }

        public DelegateCommand<object> DuplicatedFilesPrimaryOnlyIgnoreFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryOnlyIgnoreFile(object sender) {
            mainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.DuplicatedFilesPrimaryOnlyIgnoreFile);
        }

        public DelegateCommand<object> SortAlphabeticallyPrimaryOnlyCommand { get; private set; }
        public void SortAlphabeticallyPrimaryOnly(object obj) {
            ///
            mainWindowViewModel.SortBySizePrimaryOnly = false;
            FileManager.SortAlphabeticallyPrimaryOnly();
        }

        public DelegateCommand<object> SortBySizePrimaryOnlyCommand { get; private set; }
        public void SortBySizePrimaryOnly(object obj) {
            ///
            mainWindowViewModel.SortBySizePrimaryOnly = true;
            if (ShowBasePaths)
                FileManager.SortBySizePrimaryOnly();
            else
                FileManager.SortBySizePrimaryOnly(directoryPickerViewModel.PrimaryDirectory);
        }
    }
}
