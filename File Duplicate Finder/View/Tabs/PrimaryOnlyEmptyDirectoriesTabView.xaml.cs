using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class PrimaryOnlyEmptyDirectoriesTabView: UserControl {
        internal PrimaryOnlyEmptyDirectoriesTabViewModel ViewModel { get; } = new PrimaryOnlyEmptyDirectoriesTabViewModel();

        public PrimaryOnlyEmptyDirectoriesTabView() {
            InitializeComponent();
            DataContext = ViewModel;

            emptyDirectoriesPrimaryOnlyListView.ItemsSource = FileManager.emptyDirectoriesPrimary;
        }

        public void Refresh() {
            emptyDirectoriesPrimaryOnlyListView.Items.Refresh();
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.HideButtons(sender);
        }
    }
}
