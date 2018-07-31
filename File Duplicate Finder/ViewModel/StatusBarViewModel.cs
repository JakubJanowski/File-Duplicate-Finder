namespace FileDuplicateFinder.ViewModel {
    class StatusBarViewModel: ObjectBase {
        private bool showProgress = false;
        private double progress = 0;
        private string state = "Ready";
        private string stateInfo = "";

        public bool ShowProgress {
            get => showProgress;
            set {
                if (showProgress != value) {
                    showProgress = value;
                    OnPropertyChanged("ShowProgress");
                }
            }
        }

        public double Progress {
            get => progress;
            set {
                if (progress != value) {
                    progress = value;
                    OnPropertyChanged("Progress");
                }
            }
        }

        public string State {
            get => state;
            set {
                if (state != value) {
                    state = value;
                    OnPropertyChanged("State");
                }
            }
        }

        public string StateInfo {
            get => stateInfo;
            set {
                if (stateInfo != value) {
                    stateInfo = value;
                    OnPropertyChanged("StateInfo");
                }
            }
        }
    }
}
