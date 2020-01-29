using FileDuplicateFinder.Models;
using Prism.Commands;
using System;

namespace FileDuplicateFinder.ViewModel {
    class PrimaryOnlyEmptyDirectoriesTabViewModel: ObjectBase {
        private readonly ApplicationState state;
        private readonly DirectoryPickerViewModel directoryPickerViewModel;
        private readonly MainTabControlViewModel mainTabControlViewModel;

        public PrimaryOnlyEmptyDirectoriesTabViewModel(ApplicationState state, DirectoryPickerViewModel directoryPickerViewModel, MainTabControlViewModel mainTabControlViewModel) {
            EmptyDirectoriesPrimaryRemoveFileCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryRemoveFile);
            EmptyDirectoriesPrimaryIgnoreFileCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryIgnoreFile);
            OpenDirectoryPrimaryCommand = new DelegateCommand<object>(mainTabControlViewModel.OpenDirectoryPrimary);
            RemoveAllEmptyDirectoriesPrimaryCommand = new DelegateCommand<object>(RemoveAllEmptyDirectoriesPrimary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);

            this.state = state;
            this.directoryPickerViewModel = directoryPickerViewModel;
            this.mainTabControlViewModel = mainTabControlViewModel;
        }

        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
        }
        internal void OnUpdateGUIEnabled() => OnPropertyChanged(nameof(IsGUIEnabled));

        public void ShowButtons(object sender) {
            mainTabControlViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender) {
            mainTabControlViewModel.HideButtons(sender);
        }

        public DelegateCommand<object> OpenDirectoryPrimaryCommand { get; private set; }

        public DelegateCommand<object> EmptyDirectoriesPrimaryRemoveFileCommand { get; private set; }
        public void EmptyDirectoriesPrimaryRemoveFile(object sender) {
            mainTabControlViewModel.RemoveFileTemplate(sender, directoryPickerViewModel.PrimaryDirectory, FileManager.EmptyDirectoriesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyDirectoriesPrimaryIgnoreFileCommand { get; private set; }
        public void EmptyDirectoriesPrimaryIgnoreFile(object sender) {
            mainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyDirectoriesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> RemoveAllEmptyDirectoriesPrimaryCommand { get; private set; }
        public void RemoveAllEmptyDirectoriesPrimary(object obj) {
            mainTabControlViewModel.RemoveAllTemplate(directoryPickerViewModel.PrimaryDirectory, FileManager.RemoveAllEmptyDirectoriesPrimary);
        }
    }
}
