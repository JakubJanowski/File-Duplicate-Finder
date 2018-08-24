using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class PrimaryOnlyEmptyFilesTabView: UserControl {
        internal PrimaryOnlyEmptyFilesTabViewModel ViewModel { get; } = new PrimaryOnlyEmptyFilesTabViewModel();

        public PrimaryOnlyEmptyFilesTabView() {
            InitializeComponent();
            DataContext = ViewModel;

            emptyFilesPrimaryOnlyListView.ItemsSource = FileManager.emptyFilesPrimary;
        }

        public void Refresh() {
            emptyFilesPrimaryOnlyListView.Items.Refresh();
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.HideButtons(sender);
        }
    }
}
