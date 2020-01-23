using FileDuplicateFinder.Models;
using Prism.Commands;
using System;

namespace FileDuplicateFinder.ViewModel {
    class PrimaryOnlyDuplicatedFilesTabViewModel: ObjectBase {
        /// fix method names (can skip primaryonly in names, etc)
        private bool showBasePaths = false;

        private MainTabControlViewModel mainTabControlViewModel;
        private readonly ApplicationState state;

        internal MainTabControlViewModel MainTabControlViewModel {
            private get => mainTabControlViewModel;
            set {
                mainTabControlViewModel = value;
                OpenFileDirectoryPrimaryCommand = new DelegateCommand<object>(value.OpenFileDirectoryPrimary);
            }
        }

        internal DirectoryPickerViewModel DirectoryPickerViewModel { private get; set; }
        internal MainWindowViewModel MainWindowViewModel { private get; set; }

        public PrimaryOnlyDuplicatedFilesTabViewModel(ApplicationState state) {
            this.state = state;
            DuplicatedFilesPrimaryOnlyRemoveFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryOnlyRemoveFile);
            DuplicatedFilesPrimaryOnlyIgnoreFileCommand = new DelegateCommand<object>(DuplicatedFilesPrimaryOnlyIgnoreFile);
            SortAlphabeticallyPrimaryOnlyCommand = new DelegateCommand<object>(SortAlphabeticallyPrimaryOnly);
            SortBySizePrimaryOnlyCommand = new DelegateCommand<object>(SortBySizePrimaryOnly);
        }


        public bool ShowBasePaths {
            get => showBasePaths;
            set {
                if (showBasePaths != value) {
                    showBasePaths = value;
                    OnPropertyChanged("ShowBasePaths");
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

        public DelegateCommand<object> DuplicatedFilesPrimaryOnlyRemoveFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryOnlyRemoveFile(object sender) {
            MainTabControlViewModel.RemoveFileTemplate(sender, DirectoryPickerViewModel.PrimaryDirectory, FileManager.DuplicatedFilesPrimaryOnlyIgnoreFile);
        }

        public DelegateCommand<object> DuplicatedFilesPrimaryOnlyIgnoreFileCommand { get; private set; }
        public void DuplicatedFilesPrimaryOnlyIgnoreFile(object sender) {
            MainTabControlViewModel.IgnoreFileTemplate(sender, FileManager.DuplicatedFilesPrimaryOnlyIgnoreFile);
        }

        public DelegateCommand<object> SortAlphabeticallyPrimaryOnlyCommand { get; private set; }
        public void SortAlphabeticallyPrimaryOnly(object obj) {
            ///
            MainWindowViewModel.SortBySizePrimaryOnly = false;
            FileManager.SortAlphabeticallyPrimaryOnly();
        }

        public DelegateCommand<object> SortBySizePrimaryOnlyCommand { get; private set; }
        public void SortBySizePrimaryOnly(object obj) {
            ///
            MainWindowViewModel.SortBySizePrimaryOnly = true;
            if (ShowBasePaths)
                FileManager.SortBySizePrimaryOnly();
            else
                FileManager.SortBySizePrimaryOnly(DirectoryPickerViewModel.PrimaryDirectory);
        }
    }
}
