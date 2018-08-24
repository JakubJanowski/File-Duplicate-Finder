using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class PrimaryOnlyDuplicatedFilesTabView: UserControl {
        internal PrimaryOnlyDuplicatedFilesTabViewModel ViewModel { get; } = new PrimaryOnlyDuplicatedFilesTabViewModel();

        public PrimaryOnlyDuplicatedFilesTabView() {
            InitializeComponent();
            DataContext = ViewModel;

            duplicatedFilesPrimaryOnlyListView.ItemsSource = FileManager.duplicatedFilesPrimaryOnly;
        }

        public void Refresh() {
            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.HideButtons(sender);
        }
    }
}
