using FileDuplicateFinder.Services;
using FileDuplicateFinder.Utilities;
using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileDuplicateFinder.View {
    public partial class EmptyFilesTabControlView: UserControl {
        private EmptyFilesTabControlViewModel ViewModel { get => DataContext as EmptyFilesTabControlViewModel; }

        public EmptyFilesTabControlView() {
            InitializeComponent();

            emptyFilesPrimaryListView.ItemsSource = FileManager.emptyFilesPrimary;
            emptyFilesSecondaryListView.ItemsSource = FileManager.emptyFilesSecondary;
        }

        public void ShowButtons(object sender, MouseEventArgs e) {
            ViewUtilities.ShowFileListItemButtons(sender as Border);
        }

        public void HideButtons(object sender, MouseEventArgs e) {
            ViewUtilities.HideFileListItemButtons(sender as Border);
        }
    }
}
