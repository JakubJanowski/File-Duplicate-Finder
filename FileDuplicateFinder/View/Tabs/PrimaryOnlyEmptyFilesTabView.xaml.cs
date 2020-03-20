using FileDuplicateFinder.Services;
using FileDuplicateFinder.Utilities;
using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileDuplicateFinder.View {
    public partial class PrimaryOnlyEmptyFilesTabView: UserControl {
        private PrimaryOnlyEmptyFilesTabViewModel ViewModel { get => DataContext as PrimaryOnlyEmptyFilesTabViewModel; }

        public PrimaryOnlyEmptyFilesTabView() {
            InitializeComponent();

            emptyFilesPrimaryOnlyListView.ItemsSource = FileManager.emptyFilesPrimary;
        }

        public void ShowButtons(object sender, MouseEventArgs e) {
            ViewUtilities.ShowFileListItemButtons(sender as Border);
        }

        public void HideButtons(object sender, MouseEventArgs e) {
            ViewUtilities.HideFileListItemButtons(sender as Border);
        }
    }
}
