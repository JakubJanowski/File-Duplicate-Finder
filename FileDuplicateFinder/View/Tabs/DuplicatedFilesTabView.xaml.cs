using FileDuplicateFinder.Services;
using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class DuplicatedFilesTabView: UserControl {

        public DuplicatedFilesTabView() {
            InitializeComponent();

            duplicatedFilesListView.ItemsSource = FileManager.duplicatedFiles;
        }

        public void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as DuplicatedFilesTabViewModel).ShowButtons(sender);
        }

        public void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as DuplicatedFilesTabViewModel).HideButtons(sender);
        }
    }
}
