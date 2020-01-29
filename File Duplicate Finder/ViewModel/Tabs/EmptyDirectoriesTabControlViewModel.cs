using FileDuplicateFinder.Models;
using Prism.Commands;
using System;

namespace FileDuplicateFinder.ViewModel {
    class EmptyDirectoriesTabControlViewModel: ObjectBase {
        private readonly ApplicationState state;
        private readonly DirectoryPickerViewModel directoryPickerViewModel;
        private readonly MainTabControlViewModel mainTabControlViewModel;

        public EmptyDirectoriesTabControlViewModel(ApplicationState state, DirectoryPickerViewModel directoryPickerViewModel, MainTabControlViewModel mainTabControlViewModel) {
            EmptyDirectoriesPrimaryIgnoreCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryIgnore);
            EmptyDirectoriesPrimaryRemoveAllCommand = new DelegateCommand<object>(RemoveAllEmptyDirectoriesPrimary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            EmptyDirectoriesPrimaryRemoveCommand = new DelegateCommand<object>(EmptyDirectoriesPrimaryRemove);
            EmptyDirectoriesSecondaryIgnoreCommand = new DelegateCommand<object>(EmptyDirectoriesSecondaryIgnore);
            EmptyDirectoriesSecondaryRemoveAllCommand = new DelegateCommand<object>(RemoveAllEmptyDirectoriesSecondary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            EmptyDirectoriesSecondaryRemoveCommand = new DelegateCommand<object>(EmptyDirectoriesSecondaryRemove);
            OpenDirectoryPrimaryCommand = new DelegateCommand<object>(mainTabControlViewModel.OpenDirectoryPrimary);
            OpenDirectorySecondaryCommand = new DelegateCommand<object>(mainTabControlViewModel.OpenDirectorySecondary);

            this.state = state;
            this.directoryPickerViewModel = directoryPickerViewModel;
            this.mainTabControlViewModel = mainTabControlViewModel;
        }

        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
        }

        public void ShowButtons(object sender) {
            mainTabControlViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender) {
            mainTabControlViewModel.HideButtons(sender);
        }

        internal void OnUpdateGUIEnabled() => OnPropertyChanged(nameof(IsGUIEnabled));

        public DelegateCommand<object> OpenDirectoryPrimaryCommand { get; private set; }
        public DelegateCommand<object> OpenDirectorySecondaryCommand { get; private set; }

        //// truncate method names PrimaryIgnoreFileCommand etc.

        public DelegateCommand<object> EmptyDirectoriesPrimaryIgnoreCommand { get; private set; }
        public void EmptyDirectoriesPrimaryIgnore(object sender) {
            mainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyDirectoriesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyDirectoriesPrimaryRemoveAllCommand { get; private set; }
        public void RemoveAllEmptyDirectoriesPrimary(object obj) {
            mainTabControlViewModel.RemoveAllTemplate(directoryPickerViewModel.PrimaryDirectory, FileManager.RemoveAllEmptyDirectoriesPrimary);
        }

        public DelegateCommand<object> EmptyDirectoriesPrimaryRemoveCommand { get; private set; }
        public void EmptyDirectoriesPrimaryRemove(object sender) {
            mainTabControlViewModel.RemoveFileTemplate(sender, directoryPickerViewModel.PrimaryDirectory, FileManager.EmptyDirectoriesPrimaryIgnoreFile);
        }


        public DelegateCommand<object> EmptyDirectoriesSecondaryIgnoreCommand { get; private set; }
        public void EmptyDirectoriesSecondaryIgnore(object sender) {
            mainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyDirectoriesSecondaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyDirectoriesSecondaryRemoveAllCommand { get; private set; }
        public void RemoveAllEmptyDirectoriesSecondary(object obj) {
            mainTabControlViewModel.RemoveAllTemplate(directoryPickerViewModel.SecondaryDirectory, FileManager.RemoveAllEmptyDirectoriesSecondary);
        }

        public DelegateCommand<object> EmptyDirectoriesSecondaryRemoveCommand { get; private set; }
        public void EmptyDirectoriesSecondaryRemove(object sender) {
            mainTabControlViewModel.RemoveFileTemplate(sender, directoryPickerViewModel.SecondaryDirectory, FileManager.EmptyDirectoriesSecondaryIgnoreFile);
        }
    }
}
