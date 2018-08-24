using FileDuplicateFinder.ViewModel;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class MainTabControlView: UserControl {
        internal MainTabControlViewModel ViewModel { get; } = new MainTabControlViewModel();

        private DirectoryPickerView directoryPickerView;
        private MainWindowView mainWindowView;
        private StatusBarView statusBarView;

        internal DirectoryPickerView DirectoryPickerView {
            private get => directoryPickerView;
            set {
                directoryPickerView = value;
                ViewModel.DirectoryPickerViewModel = value.ViewModel;
            }
        }

        internal MainWindowView MainWindowView {
            private get => mainWindowView;
            set {
                mainWindowView = value;
                ViewModel.MainWindowViewModel = value.ViewModel;
            }
        }

        internal StatusBarView StatusBarView {
            private get => statusBarView;
            set {
                statusBarView = value;
                ViewModel.StatusBarViewModel = value.ViewModel;
            }
        }
        
        public MainTabControlView() {
            InitializeComponent();
            DataContext = ViewModel;

            ViewModel.DuplicatedFilesTabViewModel = duplicatedFilesTabView.ViewModel;
            ViewModel.EmptyDirectoriesTabControlViewModel = emptyDirectoriesTabControlView.ViewModel;
            ViewModel.EmptyFilesTabControlViewModel = emptyFilesTabControlView.ViewModel;
            ViewModel.PrimaryOnlyDuplicatedFilesTabViewModel = primaryOnlyDuplicatedFilesTabView.ViewModel;
            ViewModel.PrimaryOnlyEmptyDirectoriesTabViewModel = primaryOnlyEmptyDirectoriesTabView.ViewModel;
            ViewModel.PrimaryOnlyEmptyFilesTabViewModel = primaryOnlyEmptyFilesTabView.ViewModel;

            Utility.logListView = logListView;
            Utility.logTabItem = logTabItem;

            //logListView.ItemsSource too
        }

        internal void RefreshListViews() {
            if (directoryPickerView.PrimaryOnly) {
                primaryOnlyDuplicatedFilesTabView.Refresh();
                primaryOnlyEmptyDirectoriesTabView.Refresh();
                primaryOnlyEmptyFilesTabView.Refresh();
            }
            else {
                duplicatedFilesTabView.Refresh();
                emptyDirectoriesTabControlView.Refresh();
                emptyFilesTabControlView.Refresh();
            }
        }
        
        private void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.ShowButtons(sender);
        }

        private void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.HideButtons(sender);
        }
    }
}
