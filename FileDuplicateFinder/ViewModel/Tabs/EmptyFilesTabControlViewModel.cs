using FileDuplicateFinder.Models;
using FileDuplicateFinder.Services;
using Prism.Commands;

namespace FileDuplicateFinder.ViewModel {
    class EmptyFilesTabControlViewModel: ObjectBase {
        private readonly ApplicationState state;
        private readonly DirectoryPickerViewModel directoryPickerViewModel;
        private readonly MainTabControlViewModel mainTabControlViewModel;

        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
        }

        public EmptyFilesTabControlViewModel(ApplicationState state, DirectoryPickerViewModel directoryPickerViewModel, MainTabControlViewModel mainTabControlViewModel) {
            EmptyFilesPrimaryRemoveAllCommand = new DelegateCommand<object>(RemoveAllEmptyFilesPrimary, (o) => IsGUIEnabled && FileManager.emptyFilesPrimary.Count > 0).ObservesProperty(() => IsGUIEnabled);
            EmptyFilesPrimaryRemoveFileCommand = new DelegateCommand<object>(EmptyFilesPrimaryRemoveFile);
            EmptyFilesPrimaryIgnoreFileCommand = new DelegateCommand<object>(EmptyFilesPrimaryIgnoreFile);
            EmptyFilesSecondaryRemoveAllCommand = new DelegateCommand<object>(RemoveAllEmptyFilesSecondary, (o) => IsGUIEnabled && FileManager.emptyFilesSecondary.Count > 0).ObservesProperty(() => IsGUIEnabled);
            EmptyFilesSecondaryRemoveFileCommand = new DelegateCommand<object>(EmptyFilesSecondaryRemoveFile);
            EmptyFilesSecondaryIgnoreFileCommand = new DelegateCommand<object>(EmptyFilesSecondaryIgnoreFile);
            OpenFileDirectoryPrimaryCommand = new DelegateCommand<object>(mainTabControlViewModel.OpenFileDirectoryPrimary);
            OpenFileDirectorySecondaryCommand = new DelegateCommand<object>(mainTabControlViewModel.OpenFileDirectorySecondary);

            this.state = state;
            this.directoryPickerViewModel = directoryPickerViewModel;
            this.mainTabControlViewModel = mainTabControlViewModel;
        }

        internal void OnUpdateGUIEnabled() => OnPropertyChanged(nameof(IsGUIEnabled));


        public DelegateCommand<object> OpenFileDirectoryPrimaryCommand { get; private set; }

        public DelegateCommand<object> OpenFileDirectorySecondaryCommand { get; private set; }

        public DelegateCommand<object> EmptyFilesPrimaryRemoveAllCommand { get; private set; }
        public void RemoveAllEmptyFilesPrimary(object obj) {
            mainTabControlViewModel.RemoveAllTemplate(directoryPickerViewModel.PrimaryDirectory, FileManager.RemoveAllEmptyFilesPrimary);
        }

        public DelegateCommand<object> EmptyFilesPrimaryRemoveFileCommand { get; private set; }
        public void EmptyFilesPrimaryRemoveFile(object sender) {
            mainTabControlViewModel.RemoveFileTemplate(sender, directoryPickerViewModel.PrimaryDirectory, FileManager.EmptyFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyFilesPrimaryIgnoreFileCommand { get; private set; }
        public void EmptyFilesPrimaryIgnoreFile(object sender) {
            mainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyFilesSecondaryRemoveAllCommand { get; private set; }
        public void RemoveAllEmptyFilesSecondary(object obj) {
            //// no need to store DirectoryPickerViewModel reference, just pass enum value secondary / primary
            mainTabControlViewModel.RemoveAllTemplate(directoryPickerViewModel.SecondaryDirectory, FileManager.RemoveAllEmptyFilesSecondary);
        }

        public DelegateCommand<object> EmptyFilesSecondaryRemoveFileCommand { get; private set; }
        public void EmptyFilesSecondaryRemoveFile(object sender) {
            mainTabControlViewModel.RemoveFileTemplate(sender, directoryPickerViewModel.SecondaryDirectory, FileManager.EmptyFilesSecondaryIgnoreFile);
        }
        
        public DelegateCommand<object> EmptyFilesSecondaryIgnoreFileCommand { get; private set; }
        public void EmptyFilesSecondaryIgnoreFile(object sender) {
            mainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyFilesSecondaryIgnoreFile);
        }
    }
}
