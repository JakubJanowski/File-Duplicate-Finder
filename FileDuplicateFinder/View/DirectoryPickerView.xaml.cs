using FileDuplicateFinder.ViewModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace FileDuplicateFinder.View {
    public partial class DirectoryPickerView: UserControl {
        private DirectoryPickerViewModel ViewModel { get => DataContext as DirectoryPickerViewModel;  }

        public DirectoryPickerView() {
            InitializeComponent();
        }

        private void PrimaryDirectoryDialog(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
                ViewModel.PrimaryDirectory = dialog.FileName;

            primaryDirectoryTextBox.Focus();
            primaryDirectoryTextBox.CaretIndex = primaryDirectoryTextBox.Text.Length;
        }

        private void SecondaryDirectoryDialog(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
                ViewModel.SecondaryDirectory = dialog.FileName;

            secondaryDirectoryTextBox.Focus();
            secondaryDirectoryTextBox.CaretIndex = secondaryDirectoryTextBox.Text.Length;
        }
    }
}
