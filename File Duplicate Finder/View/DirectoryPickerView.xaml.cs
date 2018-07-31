using FileDuplicateFinder.ViewModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class DirectoryPickerView: UserControl {
        internal DirectoryPickerViewModel ViewModel { get; } = new DirectoryPickerViewModel();
        private StatusBarView statusBarView;
        private MainTabControlView mainTabControlView;

        internal StatusBarView StatusBarView {
            private get => statusBarView;
            set {
                statusBarView = value;
                ViewModel.StatusBarViewModel = value.ViewModel;
            }
        }

        internal MainTabControlView MainTabControlView {
            private get => mainTabControlView;
            set {
                mainTabControlView = value;
                ViewModel.MainTabControlViewModel = value.ViewModel;
            }
        }

        public string PrimaryDirectory { get => ViewModel.PrimaryDirectory; }
        public string SecondaryDirectory { get => ViewModel.SecondaryDirectory; }
        public bool PrimaryOnly { get => ViewModel.PrimaryOnly; }

        public DirectoryPickerView() {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void PrimaryDirectoryDialog(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
                ViewModel.PrimaryDirectory = dialog.FileName;

            primaryDirectoryTextBox.Focus();
            primaryDirectoryTextBox.CaretIndex = primaryDirectoryTextBox.Text.Length;
        }

        private void SecondaryDirectoryDialog(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
                ViewModel.SecondaryDirectory = dialog.FileName;

            secondaryDirectoryTextBox.Focus();
            secondaryDirectoryTextBox.CaretIndex = secondaryDirectoryTextBox.Text.Length;
        }

        public void LockGUI() {
            ViewModel.IsGUIEnabled = false;
        }

        public void UnlockGUI() {
            ViewModel.IsGUIEnabled = true;
        }
    }
}
