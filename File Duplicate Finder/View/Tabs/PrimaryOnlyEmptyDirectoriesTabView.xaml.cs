using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class PrimaryOnlyEmptyDirectoriesTabView: UserControl {
        public PrimaryOnlyEmptyDirectoriesTabView() {
            InitializeComponent();

            emptyDirectoriesPrimaryOnlyListView.ItemsSource = FileManager.emptyDirectoriesPrimary;
        }

        public void Refresh() {
            emptyDirectoriesPrimaryOnlyListView.Items.Refresh();
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as PrimaryOnlyEmptyDirectoriesTabViewModel).ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as PrimaryOnlyEmptyDirectoriesTabViewModel).HideButtons(sender);
        }
    }
}
