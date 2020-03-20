using FileDuplicateFinder.Services;
using FileDuplicateFinder.Utilities;
using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileDuplicateFinder.View {
    public partial class EmptyDirectoriesTabControlView: UserControl {
        private EmptyDirectoriesTabControlViewModel ViewModel { get => DataContext as EmptyDirectoriesTabControlViewModel; }

        public EmptyDirectoriesTabControlView() {
            InitializeComponent();

            emptyDirectoriesPrimaryListView.ItemsSource = FileManager.emptyDirectoriesPrimary;
            emptyDirectoriesSecondaryListView.ItemsSource = FileManager.emptyDirectoriesSecondary;
        }

        public void ShowButtons(object sender, MouseEventArgs e) {
            ViewUtilities.ShowFileListItemButtons(sender as Border);
        }

        public void HideButtons(object sender, MouseEventArgs e) {
            ViewUtilities.HideFileListItemButtons(sender as Border);
        }
    }
}
