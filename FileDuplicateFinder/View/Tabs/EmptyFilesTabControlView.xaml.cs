using FileDuplicateFinder.Services;
using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class EmptyFilesTabControlView: UserControl {

        public EmptyFilesTabControlView() {
            InitializeComponent();

            emptyFilesPrimaryListView.ItemsSource = FileManager.emptyFilesPrimary;
            emptyFilesSecondaryListView.ItemsSource = FileManager.emptyFilesSecondary;
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as EmptyFilesTabControlViewModel).ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as EmptyFilesTabControlViewModel).HideButtons(sender);
        }
    }
}
