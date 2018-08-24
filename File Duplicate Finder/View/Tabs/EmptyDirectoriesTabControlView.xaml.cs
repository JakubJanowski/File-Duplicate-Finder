using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class EmptyDirectoriesTabControlView: UserControl {
        internal EmptyDirectoriesTabControlViewModel ViewModel { get; } = new EmptyDirectoriesTabControlViewModel();

        public EmptyDirectoriesTabControlView() {
            InitializeComponent();
            DataContext = ViewModel;

            emptyDirectoriesPrimaryListView.ItemsSource = FileManager.emptyDirectoriesPrimary;
            emptyDirectoriesSecondaryListView.ItemsSource = FileManager.emptyDirectoriesSecondary;
        }

        public void Refresh() {
            emptyDirectoriesPrimaryListView.Items.Refresh();
            emptyDirectoriesSecondaryListView.Items.Refresh();
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.HideButtons(sender);
        }
    }
}
