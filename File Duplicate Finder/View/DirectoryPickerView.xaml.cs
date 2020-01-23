using FileDuplicateFinder.ViewModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class DirectoryPickerView: UserControl {
        private StatusBarView statusBarView;
        private MainTabControlView mainTabControlView;

        public string PrimaryDirectory { get => (DataContext as DirectoryPickerViewModel).PrimaryDirectory; }
        public string SecondaryDirectory { get => (DataContext as DirectoryPickerViewModel).SecondaryDirectory; }
        public bool PrimaryOnly { get => (DataContext as DirectoryPickerViewModel).PrimaryOnly; }

        public DirectoryPickerView() {
            InitializeComponent();
        }

        private void PrimaryDirectoryDialog(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
                (DataContext as DirectoryPickerViewModel).PrimaryDirectory = dialog.FileName;

            primaryDirectoryTextBox.Focus();
            primaryDirectoryTextBox.CaretIndex = primaryDirectoryTextBox.Text.Length;
        }

        private void SecondaryDirectoryDialog(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
                (DataContext as DirectoryPickerViewModel).SecondaryDirectory = dialog.FileName;

            secondaryDirectoryTextBox.Focus();
            secondaryDirectoryTextBox.CaretIndex = secondaryDirectoryTextBox.Text.Length;
        }
    }
}
