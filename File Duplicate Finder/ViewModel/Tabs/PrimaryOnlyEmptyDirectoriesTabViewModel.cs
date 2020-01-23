using FileDuplicateFinder.Models;
using Prism.Commands;
using System;

namespace FileDuplicateFinder.ViewModel {
    class PrimaryOnlyEmptyDirectoriesTabViewModel: ObjectBase {
        private MainTabControlViewModel mainTabControlViewModel;
        private readonly ApplicationState state;

        internal MainTabControlViewModel MainTabControlViewModel {
            private get => mainTabControlViewModel;
            set {
                mainTabControlViewModel = value;
                OpenDirectoryPrimaryCommand = new DelegateCommand<object>(value.OpenDirectoryPrimary);
            }
        }

        internal DirectoryPickerViewModel DirectoryPickerViewModel { private get; set; }

        public PrimaryOnlyEmptyDirectoriesTabViewModel(ApplicationState state) {
            this.state = state;
            EmptyDirectoriesPrimaryRemoveFileCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryRemoveFile);
            EmptyDirectoriesPrimaryIgnoreFileCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryIgnoreFile);
            RemoveAllEmptyDirectoriesPrimaryCommand = new DelegateCommand<object>(RemoveAllEmptyDirectoriesPrimary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
        }

        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
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

        internal void OnUpdateGUIEnabled() => OnPropertyChanged("IsGUIEnabled");

        public DelegateCommand<object> RemoveAllEmptyDirectoriesPrimaryCommand { get; private set; }
        public void RemoveAllEmptyDirectoriesPrimary(object obj) {
            MainTabControlViewModel.RemoveAllTemplate(DirectoryPickerViewModel.PrimaryDirectory, FileManager.RemoveAllEmptyDirectoriesPrimary);
        }
    }
}
