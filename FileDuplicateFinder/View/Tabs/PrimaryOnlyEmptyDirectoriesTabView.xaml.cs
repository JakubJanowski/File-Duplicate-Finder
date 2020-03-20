using FileDuplicateFinder.Services;
using FileDuplicateFinder.Utilities;
using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileDuplicateFinder.View {
    public partial class PrimaryOnlyEmptyDirectoriesTabView: UserControl {
        private PrimaryOnlyEmptyDirectoriesTabViewModel ViewModel { get => DataContext as PrimaryOnlyEmptyDirectoriesTabViewModel; }

        public PrimaryOnlyEmptyDirectoriesTabView() {
            InitializeComponent();

            emptyDirectoriesPrimaryOnlyListView.ItemsSource = FileManager.emptyDirectoriesPrimary;
        }

        public void ShowButtons(object sender, MouseEventArgs e) {
            ViewUtilities.ShowFileListItemButtons(sender as Border);
        }

        public void HideButtons(object sender, MouseEventArgs e) {
            ViewUtilities.HideFileListItemButtons(sender as Border);
        }
    }
}
