using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class EmptyFilesTabControlView: UserControl {
        internal EmptyFilesTabControlViewModel ViewModel { get; } = new EmptyFilesTabControlViewModel();

        public EmptyFilesTabControlView() {
            InitializeComponent();
            DataContext = ViewModel;

            emptyFilesPrimaryListView.ItemsSource = FileManager.emptyFilesPrimary;
            emptyFilesSecondaryListView.ItemsSource = FileManager.emptyFilesSecondary;
        }

        public void Refresh() {
            emptyFilesPrimaryListView.Items.Refresh();
            emptyFilesSecondaryListView.Items.Refresh();
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.HideButtons(sender);
        }
    }
}
