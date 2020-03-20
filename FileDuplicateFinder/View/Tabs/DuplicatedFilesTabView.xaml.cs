using FileDuplicateFinder.Services;
using FileDuplicateFinder.Utilities;
using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileDuplicateFinder.View {
    public partial class DuplicatedFilesTabView: UserControl {
        private DuplicatedFilesTabViewModel ViewModel { get => DataContext as DuplicatedFilesTabViewModel; }

        public DuplicatedFilesTabView() {
            InitializeComponent();

            duplicatedFilesListView.ItemsSource = FileManager.duplicatedFiles;
        }

        public void ShowButtons(object sender, MouseEventArgs e) {
            ViewUtilities.ShowFileListItemButtons(sender as Border);
        }

        public void HideButtons(object sender, MouseEventArgs e) {
            ViewUtilities.HideFileListItemButtons(sender as Border);
        }
    }
}
