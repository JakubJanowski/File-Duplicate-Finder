using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class DuplicatedFilesTabView: UserControl {
        internal DuplicatedFilesTabViewModel ViewModel { get; } = new DuplicatedFilesTabViewModel();

        public DuplicatedFilesTabView() {
            InitializeComponent();
            DataContext = ViewModel;

            duplicatedFilesListView.ItemsSource = FileManager.duplicatedFiles;
        }

        public void Refresh() {
            duplicatedFilesListView.Items.Refresh();
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.HideButtons(sender);
        }
    }
}
