using Prism.Commands;

namespace FileDuplicateFinder.ViewModel {
    class PrimaryOnlyEmptyDirectoriesTabViewModel: ObjectBase {
        private const int pathChildPosition = 1;
        private bool isGUIEnabled = true;

        private MainTabControlViewModel mainTabControlViewModel;

        internal MainTabControlViewModel MainTabControlViewModel {
            private get => mainTabControlViewModel;
            set {
                mainTabControlViewModel = value;
                OpenDirectoryPrimaryCommand = new DelegateCommand<object>(value.OpenDirectoryPrimary);
            }
        }

        internal DirectoryPickerViewModel DirectoryPickerViewModel { private get; set; }

        public PrimaryOnlyEmptyDirectoriesTabViewModel() {
            EmptyDirectoriesPrimaryRemoveFileCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryRemoveFile);
            EmptyDirectoriesPrimaryIgnoreFileCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryIgnoreFile);
            RemoveAllEmptyDirectoriesPrimaryCommand = new DelegateCommand<object>(RemoveAllEmptyDirectoriesPrimary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
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

        public DelegateCommand<object> OpenDirectoryPrimaryCommand { get; private set; }

        public DelegateCommand<object> EmptyDirectoriesPrimaryRemoveFileCommand { get; private set; }
        public void EmptyDirectoriesPrimaryRemoveFile(object sender) {
            MainTabControlViewModel.RemoveFileTemplate(sender, DirectoryPickerViewModel.PrimaryDirectory, FileManager.EmptyDirectoriesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyDirectoriesPrimaryIgnoreFileCommand { get; private set; }
        public void EmptyDirectoriesPrimaryIgnoreFile(object sender) {
            MainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyDirectoriesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> RemoveAllEmptyDirectoriesPrimaryCommand { get; private set; }
        public void RemoveAllEmptyDirectoriesPrimary(object obj) {
            MainTabControlViewModel.RemoveAllTemplate(DirectoryPickerViewModel.PrimaryDirectory, FileManager.RemoveAllEmptyDirectoriesPrimary);
        }
    }
}
