using Prism.Commands;

namespace FileDuplicateFinder.ViewModel {
    class EmptyDirectoriesTabControlViewModel: ObjectBase {
        private const int pathChildPosition = 1;
        private bool isGUIEnabled = true;

        private MainTabControlViewModel mainTabControlViewModel;

        internal MainTabControlViewModel MainTabControlViewModel {
            private get => mainTabControlViewModel;
            set {
                mainTabControlViewModel = value;
                OpenDirectoryPrimaryCommand = new DelegateCommand<object>(value.OpenDirectoryPrimary);
                OpenDirectorySecondaryCommand = new DelegateCommand<object>(value.OpenDirectorySecondary);
            }
        }

        internal DirectoryPickerViewModel DirectoryPickerViewModel { private get; set; }

        public EmptyDirectoriesTabControlViewModel() {
            EmptyDirectoriesPrimaryIgnoreCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryIgnore);
            EmptyDirectoriesPrimaryRemoveAllCommand = new DelegateCommand<object>(RemoveAllEmptyDirectoriesPrimary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            EmptyDirectoriesPrimaryRemoveCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryRemove);
            EmptyDirectoriesSecondaryIgnoreCommand = new DelegateCommand<object>(EmptyDirectoriesSecondaryIgnore);
            EmptyDirectoriesSecondaryRemoveAllCommand = new DelegateCommand<object>(RemoveAllEmptyDirectoriesSecondary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            EmptyDirectoriesSecondaryRemoveCommand = new DelegateCommand<object>(EmptyDirectoriesSecondaryRemove);
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
        public DelegateCommand<object> OpenDirectorySecondaryCommand { get; private set; }

        //// truncate method names PrimaryIgnoreFileCommand etc.

        public DelegateCommand<object> EmptyDirectoriesPrimaryIgnoreCommand { get; private set; }
        public void EmptyDirectoriesPrimaryIgnore(object sender) {
            MainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyDirectoriesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyDirectoriesPrimaryRemoveAllCommand { get; private set; }
        public void RemoveAllEmptyDirectoriesPrimary(object obj) {
            MainTabControlViewModel.RemoveAllTemplate(DirectoryPickerViewModel.PrimaryDirectory, FileManager.RemoveAllEmptyDirectoriesPrimary);
        }

        public DelegateCommand<object> EmptyDirectoriesPrimaryRemoveCommand { get; private set; }
        public void EmptyDirectoriesPrimaryRemove(object sender) {
            MainTabControlViewModel.RemoveFileTemplate(sender, DirectoryPickerViewModel.PrimaryDirectory, FileManager.EmptyDirectoriesPrimaryIgnoreFile);
        }


        public DelegateCommand<object> EmptyDirectoriesSecondaryIgnoreCommand { get; private set; }
        public void EmptyDirectoriesSecondaryIgnore(object sender) {
            MainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyDirectoriesSecondaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyDirectoriesSecondaryRemoveAllCommand { get; private set; }
        public void RemoveAllEmptyDirectoriesSecondary(object obj) {
            MainTabControlViewModel.RemoveAllTemplate(DirectoryPickerViewModel.SecondaryDirectory, FileManager.RemoveAllEmptyDirectoriesSecondary);
        }

        public DelegateCommand<object> EmptyDirectoriesSecondaryRemoveCommand { get; private set; }
        public void EmptyDirectoriesSecondaryRemove(object sender) {
            MainTabControlViewModel.RemoveFileTemplate(sender, DirectoryPickerViewModel.SecondaryDirectory, FileManager.EmptyDirectoriesSecondaryIgnoreFile);
        }
    }
}
