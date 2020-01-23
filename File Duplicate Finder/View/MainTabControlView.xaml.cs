using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class MainTabControlView: UserControl {
        public MainTabControlView() {
            InitializeComponent();

            Utility.logListView = logListView;
            Utility.logTabItem = logTabItem;

            //logListView.ItemsSource too
        }

        internal void RefreshListViews() {
            ///from applcation state
            //if (directoryPickerView.PrimaryOnly) {
            if ((DataContext as MainTabControlViewModel).PrimaryOnly) {
                //primaryOnlyDuplicatedFilesTabView.Refresh();
                //primaryOnlyEmptyDirectoriesTabView.Refresh();
                //primaryOnlyEmptyFilesTabView.Refresh();
            } else {
                //duplicatedFilesTabView.Refresh();
                //emptyDirectoriesTabControlView.Refresh();
                //emptyFilesTabControlView.Refresh();
            }
        }

        private void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as MainTabControlViewModel).ShowButtons(sender);
        }

        private void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            (DataContext as MainTabControlViewModel).HideButtons(sender);
        }
    }
}
