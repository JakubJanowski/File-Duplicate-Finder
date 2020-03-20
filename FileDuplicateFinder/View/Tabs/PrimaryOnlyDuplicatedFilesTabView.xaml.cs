using FileDuplicateFinder.Services;
using FileDuplicateFinder.Utilities;
using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileDuplicateFinder.View {
    public partial class PrimaryOnlyDuplicatedFilesTabView: UserControl {
        private PrimaryOnlyDuplicatedFilesTabViewModel ViewModel { get => DataContext as PrimaryOnlyDuplicatedFilesTabViewModel; }

        public PrimaryOnlyDuplicatedFilesTabView() {
            InitializeComponent();

            duplicatedFilesPrimaryOnlyListView.ItemsSource = FileManager.duplicatedFilesPrimaryOnly;
        }

        public void ShowButtons(object sender, MouseEventArgs e) {
            ViewUtilities.ShowFileListItemButtons(sender as Border);
        }

        public void HideButtons(object sender, MouseEventArgs e) {
            ViewUtilities.HideFileListItemButtons(sender as Border);
        }
    }
}
