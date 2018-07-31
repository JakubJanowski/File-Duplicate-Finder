using FileDuplicateFinder.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FileDuplicateFinder.View {
    public partial class MainTabControlView: UserControl {
        internal MainTabControlViewModel ViewModel { get; } = new MainTabControlViewModel();

        private DirectoryPickerView directoryPickerView;
        private StatusBarView statusBarView;

        internal DirectoryPickerView DirectoryPickerView {
            private get => directoryPickerView;
            set {
                directoryPickerView = value;
                ViewModel.DirectoryPickerViewModel = value.ViewModel;
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
            ///ViewModel.mainTabControlView = this;
            DataContext = ViewModel;

            Utility.logListView = logListView;
            Utility.logTabItem = logTabItem;


            //logListView.ItemsSource too
            emptyDirectoriesPrimaryListView.ItemsSource = FileManager.emptyDirectoriesPrimary;
            emptyFilesPrimaryListView.ItemsSource = FileManager.emptyFilesPrimary;
            emptyDirectoriesSecondaryListView.ItemsSource = FileManager.emptyDirectoriesSecondary;
            emptyFilesSecondaryListView.ItemsSource = FileManager.emptyFilesSecondary;
            duplicatedFilesListView.ItemsSource = FileManager.duplicatedFiles;
            emptyDirectoriesPrimaryOnlyListView.ItemsSource = FileManager.emptyDirectoriesPrimary;
            emptyFilesPrimaryOnlyListView.ItemsSource = FileManager.emptyFilesPrimary;
            duplicatedFilesPrimaryOnlyListView.ItemsSource = FileManager.duplicatedFilesPrimaryOnly;

            //this or stick to refresh and no use for observable
            BindingOperations.EnableCollectionSynchronization(FileManager.emptyDirectoriesPrimary, new object());
            BindingOperations.EnableCollectionSynchronization(FileManager.emptyFilesPrimary, new object());
            BindingOperations.EnableCollectionSynchronization(FileManager.emptyDirectoriesSecondary, new object());
            BindingOperations.EnableCollectionSynchronization(FileManager.emptyFilesSecondary, new object());
            BindingOperations.EnableCollectionSynchronization(FileManager.duplicatedFiles, new object());
            BindingOperations.EnableCollectionSynchronization(FileManager.emptyDirectoriesPrimary, new object());
            BindingOperations.EnableCollectionSynchronization(FileManager.emptyFilesPrimary, new object());
            BindingOperations.EnableCollectionSynchronization(FileManager.duplicatedFilesPrimaryOnly, new object());

            FileManager.emptyDirectoriesPrimaryOnlyListView = emptyDirectoriesPrimaryOnlyListView;
            FileManager.emptyFilesPrimaryOnlyListView = emptyFilesPrimaryOnlyListView;
            FileManager.duplicatedFilesPrimaryOnlyListView = duplicatedFilesPrimaryOnlyListView;
            FileManager.emptyDirectoriesPrimaryListView = emptyDirectoriesPrimaryListView;
            FileManager.emptyFilesPrimaryListView = emptyFilesPrimaryListView;
            FileManager.emptyDirectoriesSecondaryListView = emptyDirectoriesSecondaryListView;
            FileManager.emptyFilesSecondaryListView = emptyFilesSecondaryListView;
            FileManager.duplicatedFilesListView = duplicatedFilesListView;
        }

        internal void LockGUI() {
            ViewModel.IsGUIEnabled = false;
        }

        internal void UnlockGUI() {
            ViewModel.IsGUIEnabled = true;
        }

        // push these as commands to other classes  and make private

        private void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.ShowButtons(sender, e);
        }

        private void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            ViewModel.HideButtons(sender, e);
        }


        // do something with these
        private void IdenticalSubpathChecked(object sender, RoutedEventArgs e) {

        }

        private void IdenticalSubpathUnchecked(object sender, RoutedEventArgs e) {

        }
    }
}
