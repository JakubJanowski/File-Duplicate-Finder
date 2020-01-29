using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class PrimaryOnlyDuplicatedFilesTabView: UserControl {
        public PrimaryOnlyDuplicatedFilesTabView() {
            InitializeComponent();

            duplicatedFilesPrimaryOnlyListView.ItemsSource = FileManager.duplicatedFilesPrimaryOnly;
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as PrimaryOnlyDuplicatedFilesTabViewModel).ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as PrimaryOnlyDuplicatedFilesTabViewModel).HideButtons(sender);
        }
    }
}
