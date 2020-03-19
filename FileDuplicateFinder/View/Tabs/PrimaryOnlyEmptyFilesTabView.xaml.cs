using FileDuplicateFinder.Services;
using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class PrimaryOnlyEmptyFilesTabView: UserControl {
        public PrimaryOnlyEmptyFilesTabView() {
            InitializeComponent();

            emptyFilesPrimaryOnlyListView.ItemsSource = FileManager.emptyFilesPrimary;
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as PrimaryOnlyEmptyFilesTabViewModel).ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as PrimaryOnlyEmptyFilesTabViewModel).HideButtons(sender);
        }
    }
}
