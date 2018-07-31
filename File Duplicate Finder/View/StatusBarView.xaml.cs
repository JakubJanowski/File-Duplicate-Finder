using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class StatusBarView: UserControl {
        internal StatusBarViewModel ViewModel { get; } = new StatusBarViewModel();

        public bool ShowProgress { get => ViewModel.ShowProgress; set => ViewModel.ShowProgress = value; }
        public double Progress { get => ViewModel.Progress; set => ViewModel.Progress = value; }
        public string State { get => ViewModel.State; set => ViewModel.State = value; }
        public string StateInfo { get => ViewModel.StateInfo; set => ViewModel.StateInfo = value; }

        public StatusBarView() {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
