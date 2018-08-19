using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class StatusBarView: UserControl {
        internal StatusBarViewModel ViewModel { get; } = new StatusBarViewModel();

        public StatusBarView() {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
