using FileDuplicateFinder.Models;
using FileDuplicateFinder.Services;
using Prism.Commands;

namespace FileDuplicateFinder.ViewModel {
    class PrimaryOnlyEmptyFilesTabViewModel: ObjectBase {
        private readonly ApplicationState state;
        private readonly DirectoryPickerViewModel directoryPickerViewModel;
        private readonly MainTabControlViewModel mainTabControlViewModel;

        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
        }

        public PrimaryOnlyEmptyFilesTabViewModel(ApplicationState state, DirectoryPickerViewModel directoryPickerViewModel, MainTabControlViewModel mainTabControlViewModel) {
            EmptyFilesPrimaryRemoveFileCommand = new DelegateCommand<object>(EmptyFilesPrimaryRemoveFile);
            EmptyFilesPrimaryIgnoreFileCommand = new DelegateCommand<object>(EmptyFilesPrimaryIgnoreFile);
            OpenFileDirectoryPrimaryCommand = new DelegateCommand<object>(mainTabControlViewModel.OpenFileDirectoryPrimary);
            RemoveAllEmptyFilesPrimaryCommand = new DelegateCommand<object>(RemoveAllEmptyFilesPrimary, (o) => IsGUIEnabled && FileManager.emptyFilesPrimary.Count > 0).ObservesProperty(() => IsGUIEnabled);

            this.state = state;
            this.directoryPickerViewModel = directoryPickerViewModel;
            this.mainTabControlViewModel = mainTabControlViewModel;
        }

        internal void OnUpdateGUIEnabled() => OnPropertyChanged(nameof(IsGUIEnabled));

        public void ShowButtons(object sender) {
            mainTabControlViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender) {
            mainTabControlViewModel.HideButtons(sender);
        }

        public DelegateCommand<object> OpenFileDirectoryPrimaryCommand { get; private set; }

        public DelegateCommand<object> EmptyFilesPrimaryRemoveFileCommand { get; private set; }
        public void EmptyFilesPrimaryRemoveFile(object sender) {
            mainTabControlViewModel.RemoveFileTemplate(sender, directoryPickerViewModel.PrimaryDirectory, FileManager.EmptyFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyFilesPrimaryIgnoreFileCommand { get; private set; }
        public void EmptyFilesPrimaryIgnoreFile(object sender) {
            mainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> RemoveAllEmptyFilesPrimaryCommand { get; private set; }
        public void RemoveAllEmptyFilesPrimary(object obj) {
            mainTabControlViewModel.RemoveAllTemplate(directoryPickerViewModel.PrimaryDirectory, FileManager.RemoveAllEmptyFilesPrimary);
        }
    }
}
