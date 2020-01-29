using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class MainTabControlView: UserControl {
        private MainTabControlViewModel ViewModel { get => DataContext as MainTabControlViewModel; }

        public MainTabControlView() {
            InitializeComponent();

            Utility.logListView = logListView;
            Utility.logTabItem = logTabItem;

            //logListView.ItemsSource too
        }

        private void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.ShowButtons(sender);
        }

        private void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.HideButtons(sender);
        }
    }
}
