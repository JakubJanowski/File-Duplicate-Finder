using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class StatusBarView: UserControl {
        private StatusBarViewModel ViewModel { get => DataContext as StatusBarViewModel; }

        public StatusBarView() {
            InitializeComponent();
        }
    }
}
