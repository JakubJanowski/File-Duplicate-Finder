using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class MainTabControlView: UserControl {
        private MainTabControlViewModel ViewModel { get => DataContext as MainTabControlViewModel; }

        public MainTabControlView() {
            InitializeComponent();

            Utilities.logListView = logListView;
            Utilities.logTabItem = logTabItem;

            //logListView.ItemsSource too
        }
    }
}
