using FileDuplicateFinder.Services;
using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class EmptyDirectoriesTabControlView: UserControl {
        public EmptyDirectoriesTabControlView() {
            InitializeComponent();

            emptyDirectoriesPrimaryListView.ItemsSource = FileManager.emptyDirectoriesPrimary;
            emptyDirectoriesSecondaryListView.ItemsSource = FileManager.emptyDirectoriesSecondary;
        }

        public void Refresh() {
            emptyDirectoriesPrimaryListView.Items.Refresh();
            emptyDirectoriesSecondaryListView.Items.Refresh();
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as EmptyDirectoriesTabControlViewModel).ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as EmptyDirectoriesTabControlViewModel).HideButtons(sender);
        }
    }
}
