using System;

namespace FileDuplicateFinder.ViewModel {
    class DirectoryPickerViewModel: ObjectBase {
        internal MainTabControlViewModel MainTabControlViewModel { private get; set; }
        internal StatusBarViewModel StatusBarViewModel { private get; set; }

        private bool isGUIEnabled = true;
        private bool primaryOnly = false;
        private string primaryDirectory = "";
        private string secondaryDirectory = "";

        public bool IsGUIEnabled {
            get => isGUIEnabled;
            set {
                if (isGUIEnabled != value) {
                    isGUIEnabled = value;
                    OnPropertyChanged("IsGUIEnabled");
                }
            }
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
        
        public void LockGUI() {
            IsGUIEnabled = false;
        }

        public void UnlockGUI() {
            IsGUIEnabled = true;
        }
    }
}
