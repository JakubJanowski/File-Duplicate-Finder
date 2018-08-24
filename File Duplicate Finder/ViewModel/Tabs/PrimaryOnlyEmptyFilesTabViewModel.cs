using Prism.Commands;

namespace FileDuplicateFinder.ViewModel {
    class PrimaryOnlyEmptyFilesTabViewModel: ObjectBase {
        private const int pathChildPosition = 1;
        private bool isGUIEnabled = true;

        private MainTabControlViewModel mainTabControlViewModel;

        internal MainTabControlViewModel MainTabControlViewModel {
            private get => mainTabControlViewModel;
            set {
                mainTabControlViewModel = value;
                OpenFileDirectoryPrimaryCommand = new DelegateCommand<object>(value.OpenFileDirectoryPrimary);
            }
        }

        internal DirectoryPickerViewModel DirectoryPickerViewModel { private get; set; }

        public PrimaryOnlyEmptyFilesTabViewModel() {
            EmptyFilesPrimaryRemoveFileCommand = new DelegateCommand<object>(EmptyFilesPrimaryRemoveFile);
            EmptyFilesPrimaryIgnoreFileCommand = new DelegateCommand<object>(EmptyFilesPrimaryIgnoreFile);
            RemoveAllEmptyFilesPrimaryCommand = new DelegateCommand<object>(RemoveAllEmptyFilesPrimary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
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

        public void ShowButtons(object sender) {
            MainTabControlViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender) {
            MainTabControlViewModel.HideButtons(sender);
        }

        public DelegateCommand<object> OpenFileDirectoryPrimaryCommand { get; private set; }
        
        public DelegateCommand<object> EmptyFilesPrimaryRemoveFileCommand { get; private set; }
        public void EmptyFilesPrimaryRemoveFile(object sender) {
            MainTabControlViewModel.RemoveFileTemplate(sender, DirectoryPickerViewModel.PrimaryDirectory, FileManager.EmptyFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyFilesPrimaryIgnoreFileCommand { get; private set; }
        public void EmptyFilesPrimaryIgnoreFile(object sender) {
            MainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> RemoveAllEmptyFilesPrimaryCommand { get; private set; }
        public void RemoveAllEmptyFilesPrimary(object obj) {
            MainTabControlViewModel.RemoveAllTemplate(DirectoryPickerViewModel.PrimaryDirectory, FileManager.RemoveAllEmptyFilesPrimary);
        }
    }
}
