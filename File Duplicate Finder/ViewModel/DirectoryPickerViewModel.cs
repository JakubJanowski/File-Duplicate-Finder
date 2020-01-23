using FileDuplicateFinder.Models;
using System;

namespace FileDuplicateFinder.ViewModel {
    class DirectoryPickerViewModel: ObjectBase {
        internal MainTabControlViewModel MainTabControlViewModel { private get; set; }
        internal StatusBarViewModel StatusBarViewModel { private get; set; }

        private bool primaryOnly = false;
        private string primaryDirectory = "";
        private string secondaryDirectory = "";
        private ApplicationState state;

        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
        }

        public bool PrimaryOnly {
            get => primaryOnly;
            set {
                if (primaryOnly != value) {
                    primaryOnly = value;
                    /// later change this in settingsVM and use it from settings in MainTabControlViewModel
                    MainTabControlViewModel.PrimaryOnly = value;
                    StatusBarViewModel.State = "Ready";
                    OnPropertyChanged("PrimaryOnly");
                }
            }
        }

        public string PrimaryDirectory {
            get => primaryDirectory;
            set {
                if (primaryDirectory != value) {
                    primaryDirectory = Utility.NormalizePath(value);
                    StatusBarViewModel.State = "Ready";
                    OnPropertyChanged("PrimaryDirectory");
                }
            }
        }

        internal void OnUpdateGUIEnabled() => OnPropertyChanged("IsGUIEnabled");

        public string SecondaryDirectory {
            get => secondaryDirectory;
            set {
                if (secondaryDirectory != value) {
                    secondaryDirectory = Utility.NormalizePath(value);
                    StatusBarViewModel.State = "Ready";
                    OnPropertyChanged("SecondaryDirectory");
                }
            }
        }

        public DirectoryPickerViewModel(ApplicationState state) {
            this.state = state;
        }
    }
}
