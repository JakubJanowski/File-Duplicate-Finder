using FileDuplicateFinder.Models;
using Prism.Commands;
using System;

namespace FileDuplicateFinder.ViewModel {
    class EmptyFilesTabControlViewModel: ObjectBase {
        private const int pathChildPosition = 1;
        private bool isGUIEnabled = true;

        private MainTabControlViewModel mainTabControlViewModel;
        private readonly ApplicationState state;

        internal MainTabControlViewModel MainTabControlViewModel {
            private get => mainTabControlViewModel;
            set {
                mainTabControlViewModel = value;
                OpenFileDirectoryPrimaryCommand = new DelegateCommand<object>(value.OpenFileDirectoryPrimary);
                OpenFileDirectorySecondaryCommand = new DelegateCommand<object>(value.OpenFileDirectorySecondary);
            }
        }

        internal DirectoryPickerViewModel DirectoryPickerViewModel { private get; set; }

        public EmptyFilesTabControlViewModel(ApplicationState state) {
            this.state = state;
            EmptyFilesPrimaryRemoveAllCommand = new DelegateCommand<object>(RemoveAllEmptyFilesPrimary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            EmptyFilesPrimaryRemoveFileCommand = new DelegateCommand<object>(EmptyFilesPrimaryRemoveFile);
            EmptyFilesPrimaryIgnoreFileCommand = new DelegateCommand<object>(EmptyFilesPrimaryIgnoreFile);
            EmptyFilesSecondaryRemoveAllCommand = new DelegateCommand<object>(RemoveAllEmptyFilesSecondary, (o) => IsGUIEnabled).ObservesProperty(() => IsGUIEnabled);
            EmptyFilesSecondaryRemoveFileCommand = new DelegateCommand<object>(EmptyFilesSecondaryRemoveFile);
            EmptyFilesSecondaryIgnoreFileCommand = new DelegateCommand<object>(EmptyFilesSecondaryIgnoreFile);
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

        public DelegateCommand<object> OpenFileDirectoryPrimaryCommand { get; private set; }

        internal void OnUpdateGUIEnabled() => OnPropertyChanged("IsGUIEnabled");

        public DelegateCommand<object> OpenFileDirectorySecondaryCommand { get; private set; }


        public DelegateCommand<object> EmptyFilesPrimaryRemoveAllCommand { get; private set; }
        public void RemoveAllEmptyFilesPrimary(object obj) {
            MainTabControlViewModel.RemoveAllTemplate(DirectoryPickerViewModel.PrimaryDirectory, FileManager.RemoveAllEmptyFilesPrimary);
        }

        public DelegateCommand<object> EmptyFilesPrimaryRemoveFileCommand { get; private set; }
        public void EmptyFilesPrimaryRemoveFile(object sender) {
            MainTabControlViewModel.RemoveFileTemplate(sender, DirectoryPickerViewModel.PrimaryDirectory, FileManager.EmptyFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyFilesPrimaryIgnoreFileCommand { get; private set; }
        public void EmptyFilesPrimaryIgnoreFile(object sender) {
            MainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyFilesPrimaryIgnoreFile);
        }

        public DelegateCommand<object> EmptyFilesSecondaryRemoveAllCommand { get; private set; }
        public void RemoveAllEmptyFilesSecondary(object obj) {
            //// no need to store DirectoryPickerViewModel reference, just pass enum value secondary / primary
            MainTabControlViewModel.RemoveAllTemplate(DirectoryPickerViewModel.SecondaryDirectory, FileManager.RemoveAllEmptyFilesSecondary);
        }

        public DelegateCommand<object> EmptyFilesSecondaryRemoveFileCommand { get; private set; }
        public void EmptyFilesSecondaryRemoveFile(object sender) {
            MainTabControlViewModel.RemoveFileTemplate(sender, DirectoryPickerViewModel.SecondaryDirectory, FileManager.EmptyFilesSecondaryIgnoreFile);
        }
        
        public DelegateCommand<object> EmptyFilesSecondaryIgnoreFileCommand { get; private set; }
        public void EmptyFilesSecondaryIgnoreFile(object sender) {
            MainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.EmptyFilesSecondaryIgnoreFile);
        }
    }
}
