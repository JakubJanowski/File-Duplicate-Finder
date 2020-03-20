using FileDuplicateFinder.Models;
using FileDuplicateFinder.Utilities;

namespace FileDuplicateFinder.ViewModel {
    class DirectoryPickerViewModel: ObjectBase {
        private readonly ApplicationState state;
        private readonly StatusBarViewModel statusBarViewModel;

        private string primaryDirectory = "";
        private string secondaryDirectory = "";

        public bool IsGUIEnabled {
            get => state.IsGUIEnabled;
        }

        public bool PrimaryOnly {
            get => state.PrimaryOnly;
            set {
                if (state.PrimaryOnly != value) {
                    state.PrimaryOnly = value;
                    statusBarViewModel.State = "Ready";
                    OnPropertyChanged(nameof(PrimaryOnly));
                }
            }
        }

        public string PrimaryDirectory {
            get => primaryDirectory;
            set {
                if (primaryDirectory != value) {
                    primaryDirectory = FileUtilities.NormalizeDirectoryPath(value);
                    statusBarViewModel.State = "Ready";
                    OnPropertyChanged(nameof(PrimaryDirectory));
                }
            }
        }

        public string SecondaryDirectory {
            get => secondaryDirectory;
            set {
                if (secondaryDirectory != value) {
                    secondaryDirectory = FileUtilities.NormalizeDirectoryPath(value);
                    statusBarViewModel.State = "Ready";
                    OnPropertyChanged(nameof(SecondaryDirectory));
                }
            }
        }

        public DirectoryPickerViewModel(ApplicationState state, StatusBarViewModel statusBarViewModel) {
            this.state = state;
            this.statusBarViewModel = statusBarViewModel;
        }

        internal void OnUpdateGUIEnabled() => OnPropertyChanged(nameof(IsGUIEnabled));
    }
}
