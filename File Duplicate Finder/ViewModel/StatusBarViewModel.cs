namespace FileDuplicateFinder.ViewModel {
    class StatusBarViewModel: ObjectBase {
        private bool showProgress = false;
        private bool isIndeterminate = false;
        private int progress = 0;
        private int maxProgress = 100;
        private string state = "Ready";
        private string stateInfo = "";

        public bool ShowProgress {
            get => showProgress;
            set {
                if (showProgress != value) {
                    showProgress = value;
                    OnPropertyChanged(nameof(ShowProgress));
                }
            }
        }

        public bool IsIndeterminate {
            get => isIndeterminate;
            set {
                if (isIndeterminate != value) {
                    isIndeterminate = value;
                    OnPropertyChanged(nameof(IsIndeterminate));
                }
            }
        }

        public int Progress {
            get => progress;
            set {
                if (progress != value) {
                    progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        public int MaxProgress {
            get => maxProgress;
            set {
                if (maxProgress != value) {
                    maxProgress = value;
                    OnPropertyChanged(nameof(MaxProgress));
                }
            }
        }

        public string State {
            get => state;
            set {
                if (state != value) {
                    state = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }

        public string StateInfo {
            get => stateInfo;
            set {
                if (stateInfo != value) {
                    stateInfo = value;
                    OnPropertyChanged(nameof(StateInfo));
                }
            }
        }
    }
}
