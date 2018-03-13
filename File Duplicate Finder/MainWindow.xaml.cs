//TODO
//recognize identical subpath
// show file size checkbox
// remove/ignore/show in explorer on restore list
//scroll propagate down to main list
//abort task
// clean up hidden checkboxes or add their functionality
// restructurize code
// switch visibility for tabitems after pressing find duplicates button not the checkbox for primaryonly
// clicking on buttons does not always work in listview when it is being constantly updated
// ignore group
// show number of results or some other statistics
// buttons on hover flicker on refresh
// scroll doesn't adjust size to listitem height but to listitemcount   VirtualizingPanel.ScrollUnit="Item" or pixel
// when deleting from appdata with noadmin rights it did nothing not even logged errors
//test primary only Touro/backup/kuba janowski old    and remove file from duplicated list
// checkbox remove from list when only one element left whit no other to compare to (do not delete elements from list after ignoring other for sure)

using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FileDuplicateFinder {
    public partial class MainWindow: Window {
        bool removeByPathAndContent = false;
        bool removeByContent = false;
        bool removeEmptyFoldersPrimary = false;
        bool removeEmptyFoldersSecondary = false;
        bool removeEmptyFilesPrimary = false;
        bool removeEmptyFilesSecondary = false;
        bool keepFiles = true;
        bool showBasePaths = false;
        bool backupFiles = true;
        bool askLarge = true;
        bool sortBySize = true;
        bool primaryOnly = false;
        bool sortBySizePrimaryOnly = true;
        string primaryDirectory;
        string secondaryDirectory;
        string tmpDirectory = AppDomain.CurrentDomain.BaseDirectory + "tmp/";
        List<Tuple<string, string>> storedFiles = new List<Tuple<string, string>>();

        public MainWindow() {
            InitializeComponent();
            Utility.logListView = logListView;
            Utility.logTabItem = logTabItem;
            Utility.dispatcher = Dispatcher;
            keepFilesCheckBox.IsChecked = true;
            keepFilesCheckBox.IsEnabled = false;
            emptyDirectoriesPrimaryListView.ItemsSource = Finder.emptyDirectoriesPrimary;
            emptyFilesPrimaryListView.ItemsSource = Finder.emptyFilesPrimary;
            emptyDirectoriesSecondaryListView.ItemsSource = Finder.emptyDirectoriesSecondary;
            emptyFilesSecondaryListView.ItemsSource = Finder.emptyFilesSecondary;
            duplicatedFilesListView.ItemsSource = Finder.duplicatedFiles;
            emptyDirectoriesPrimaryOnlyListView.ItemsSource = Finder.emptyDirectoriesPrimary;
            emptyFilesPrimaryOnlyListView.ItemsSource = Finder.emptyFilesPrimary;
            duplicatedFilesPrimaryOnlyListView.ItemsSource = Finder.duplicatedFilesPrimaryOnly;
            Finder.dispatcher = Dispatcher;
            Finder.stateTextBlock = stateTextBlock;
            Finder.progressBar = progressBar;
            Finder.progressTextBlock = progressTextBlock;
            Finder.emptyDirectoriesPrimaryOnlyListView = emptyDirectoriesPrimaryOnlyListView;
            Finder.emptyFilesPrimaryOnlyListView = emptyFilesPrimaryOnlyListView;
            Finder.duplicatedFilesPrimaryOnlyListView = duplicatedFilesPrimaryOnlyListView;
            Finder.emptyDirectoriesPrimaryListView = emptyDirectoriesPrimaryListView;
            Finder.emptyFilesPrimaryListView = emptyFilesPrimaryListView;
            Finder.emptyDirectoriesSecondaryListView = emptyDirectoriesSecondaryListView;
            Finder.emptyFilesSecondaryListView = emptyFilesSecondaryListView;
            Finder.duplicatedFilesListView = duplicatedFilesListView;

            Directory.CreateDirectory(tmpDirectory);
        }

        #region checkboxes
        private void RemoveByPathAndContentChecked(object sender, RoutedEventArgs e) {
            removeByPathAndContent = true;
            keepFilesCheckBox.IsChecked = false;
            keepFilesCheckBox.IsEnabled = true;
            stateTextBlock.Text = "Ready";
        }
        private void RemoveByPathAndContentUnchecked(object sender, RoutedEventArgs e) {
            removeByPathAndContent = false;
            removeByContentCheckBox.IsChecked = false;
            if (!removeByContent && !removeEmptyFoldersPrimary && !removeEmptyFoldersSecondary && !removeEmptyFilesPrimary && !removeEmptyFilesSecondary) {
                keepFilesCheckBox.IsChecked = true;
                keepFilesCheckBox.IsEnabled = false;
            }
            stateTextBlock.Text = "Ready";
        }
        private void RemoveByContentChecked(object sender, RoutedEventArgs e) {
            removeByContent = true;
            removeByPathAndContentCheckBox.IsChecked = true;
            keepFilesCheckBox.IsChecked = false;
            keepFilesCheckBox.IsEnabled = true;
            stateTextBlock.Text = "Ready";
        }
        private void RemoveByContentUnchecked(object sender, RoutedEventArgs e) {
            removeByContent = false;
            if (!removeByPathAndContent && !removeEmptyFoldersPrimary && !removeEmptyFoldersSecondary && !removeEmptyFilesPrimary && !removeEmptyFilesSecondary) {
                keepFilesCheckBox.IsChecked = true;
                keepFilesCheckBox.IsEnabled = false;
            }
            stateTextBlock.Text = "Ready";
        }
        private void RemoveEmptyDirectoriesFromPrimaryChecked(object sender, RoutedEventArgs e) {
            removeEmptyFoldersPrimary = true;
            keepFilesCheckBox.IsChecked = false;
            keepFilesCheckBox.IsEnabled = true;
            stateTextBlock.Text = "Ready";
        }
        private void RemoveEmptyDirectoriesFromPrimaryUnchecked(object sender, RoutedEventArgs e) {
            removeEmptyFoldersPrimary = false;
            if (!removeByPathAndContent && !removeByContent && !removeEmptyFoldersSecondary && !removeEmptyFilesPrimary && !removeEmptyFilesSecondary) {
                keepFilesCheckBox.IsChecked = true;
                keepFilesCheckBox.IsEnabled = false;
            }
            stateTextBlock.Text = "Ready";
        }
        private void RemoveEmptyDirectoriesFromSecondaryChecked(object sender, RoutedEventArgs e) {
            removeEmptyFoldersSecondary = true;
            keepFilesCheckBox.IsChecked = false;
            keepFilesCheckBox.IsEnabled = true;
            stateTextBlock.Text = "Ready";
        }
        private void RemoveEmptyDirectoriesFromSecondaryUnchecked(object sender, RoutedEventArgs e) {
            removeEmptyFoldersSecondary = false;
            if (!removeByPathAndContent && !removeByContent && !removeEmptyFoldersPrimary && !removeEmptyFilesPrimary && !removeEmptyFilesSecondary) {
                keepFilesCheckBox.IsChecked = true;
                keepFilesCheckBox.IsEnabled = false;
            }
            stateTextBlock.Text = "Ready";
        }
        private void RemoveEmptyFilesFromPrimaryChecked(object sender, RoutedEventArgs e) {
            removeEmptyFilesPrimary = true;
            keepFilesCheckBox.IsChecked = false;
            keepFilesCheckBox.IsEnabled = true;
            stateTextBlock.Text = "Ready";
        }
        private void RemoveEmptyFilesFromPrimaryUnchecked(object sender, RoutedEventArgs e) {
            removeEmptyFilesPrimary = false;
            if (!removeByPathAndContent && !removeByContent && !removeEmptyFoldersPrimary && !removeEmptyFoldersSecondary && !removeEmptyFilesSecondary) {
                keepFilesCheckBox.IsChecked = true;
                keepFilesCheckBox.IsEnabled = false;
            }
            stateTextBlock.Text = "Ready";
        }
        private void RemoveEmptyFilesFromSecondaryChecked(object sender, RoutedEventArgs e) {
            removeEmptyFilesSecondary = true;
            keepFilesCheckBox.IsChecked = false;
            keepFilesCheckBox.IsEnabled = true;
            stateTextBlock.Text = "Ready";
        }
        private void RemoveEmptyFilesFromSecondaryUnchecked(object sender, RoutedEventArgs e) {
            removeEmptyFilesSecondary = false;
            if (!removeByPathAndContent && !removeByContent && !removeEmptyFoldersPrimary && !removeEmptyFoldersSecondary && !removeEmptyFilesPrimary) {
                keepFilesCheckBox.IsChecked = true;
                keepFilesCheckBox.IsEnabled = false;
            }
            stateTextBlock.Text = "Ready";
        }
        private void KeepFilesChecked(object sender, RoutedEventArgs e) {
            keepFiles = true;
            removeByPathAndContentCheckBox.IsChecked = false;
            removeByContentCheckBox.IsChecked = false;
            removeEmptyDirectoriesFromPrimaryCheckBox.IsChecked = false;
            removeEmptyDirectoriesFromSecondaryCheckBox.IsChecked = false;
            removeEmptyFilesFromPrimaryCheckBox.IsChecked = false;
            removeEmptyFilesFromSecondaryCheckBox.IsChecked = false;
            stateTextBlock.Text = "Ready";
        }
        private void KeepFilesUnchecked(object sender, RoutedEventArgs e) {
            keepFiles = false;
            stateTextBlock.Text = "Ready";
        }
        #endregion

        private void PrimaryDirectoryDialog(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok) {
                primaryDirectoryTextBox.Text = dialog.FileName;
                stateTextBlock.Text = "Ready";
            }

            primaryDirectoryTextBox.Focus();
            primaryDirectoryTextBox.CaretIndex = primaryDirectoryTextBox.Text.Length;
        }

        private void SecondaryDirectoryDialog(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok) {
                secondaryDirectoryTextBox.Text = dialog.FileName;
                stateTextBlock.Text = "Ready";
            }

            secondaryDirectoryTextBox.Focus();
            secondaryDirectoryTextBox.CaretIndex = secondaryDirectoryTextBox.Text.Length;
        }

        private void FindDuplicatedFiles(object sender, RoutedEventArgs e) {
            InitializeDuplicateFinding();

            if (primaryOnly)
                new Thread(FindDuplicatedFilesInPrimaryOnly).Start();
            else
                new Thread(FindDuplicatedFilesInBoth).Start();
        }

        private void FindDuplicatedFilesInPrimaryOnly() {
            bool error = false;
            primaryDirectory = Utility.NormalizePath(primaryDirectory);

            try {
                Directory.GetAccessControl(primaryDirectory);
            }
            catch (UnauthorizedAccessException) {
                error = true;
                Dispatcher.Invoke(() => Utility.Log("Primary directory is not accessible."));
            }
            catch {
                error = true;
                if (!Directory.Exists(primaryDirectory)) {
                    Dispatcher.Invoke(() => Utility.Log("Primary directory does not exist."));
                }
                else {
                    Dispatcher.Invoke(() => Utility.Log("Unknown error in primary directory."));
                }
            }

            if (error) {
                Dispatcher.Invoke(() => {
                    stateTextBlock.Text = "Failed";
                    progressBar.Visibility = Visibility.Hidden;
                    UnlockGUI();
                });
                return;
            }

            Finder.FindDuplicatedFiles(primaryDirectory, showBasePaths);
            
            if (!sortBySizePrimaryOnly)
                Finder.duplicatedFilesPrimaryOnly.Sort((a, b) => a[0].Path.CompareTo(b[0].Path));

            Dispatcher.Invoke(() => {
                duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                FinalizeDuplicateFinding();
            });
        }

        private void FindDuplicatedFilesInBoth() {
            bool error = false;
            primaryDirectory = Utility.NormalizePath(primaryDirectory);
            secondaryDirectory = Utility.NormalizePath(secondaryDirectory);
            if (primaryDirectory.ToUpperInvariant() == secondaryDirectory.ToUpperInvariant()) {
                error = true;
                Utility.LogFromNonGUIThread("Primary and secondary directories must be different.");
            }

            Utility.CheckDirectories(primaryDirectory, secondaryDirectory, ref error);
            if (primaryDirectory.IsSubDirectoryOf(secondaryDirectory)) {
                error = true;
                Utility.LogFromNonGUIThread("Primary directory cannot be a subdirectory of secondary directory.");
            }
            if (secondaryDirectory.IsSubDirectoryOf(primaryDirectory)) {
                error = true;
                Utility.LogFromNonGUIThread("Secondary directory cannot be a subdirectory of primary directory.");
            }

            if (error) {
                Dispatcher.Invoke(() => {
                    stateTextBlock.Text = "Failed";
                    progressBar.Visibility = Visibility.Hidden;
                    UnlockGUI();
                });
                return;
            }

            Finder.FindDuplicatedFiles(primaryDirectory, secondaryDirectory, showBasePaths);


            if (!sortBySize)
                Finder.duplicatedFiles.Sort((a, b) => a.Item1[0].Path.CompareTo(b.Item1[0].Path));

            Dispatcher.Invoke(() => {
                duplicatedFilesListView.Items.Refresh();
                FinalizeDuplicateFinding();
            });
        }

        private void InitializeDuplicateFinding() {
            LockGUI();
            stateTextBlock.Text = "Initializing";
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Visible;
            Finder.primaryFiles.Clear();
            Finder.secondaryFiles.Clear();
            logListView.Items.Clear();
            Finder.emptyDirectoriesPrimary.Clear();
            Finder.emptyFilesPrimary.Clear();
            Finder.emptyDirectoriesSecondary.Clear();
            Finder.emptyFilesSecondary.Clear();
            Finder.duplicatedFiles.Clear();
            Finder.duplicatedFilesPrimaryOnly.Clear();
            duplicatedFilesListView.Items.Refresh();
            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
            primaryDirectory = primaryDirectoryTextBox.Text;
            secondaryDirectory = secondaryDirectoryTextBox.Text;
            ClearTmpDirectory();
            storedFiles.Clear();
            restoreButton.IsEnabled = false;
        }

        private void FinalizeDuplicateFinding() {
            stateTextBlock.Text = "Done";
            progressBar.Visibility = Visibility.Hidden;
            progressTextBlock.Visibility = Visibility.Hidden;
            UnlockGUI();
        }

        private void ClearTmpDirectory() {
            DirectoryInfo directoryInfo = new DirectoryInfo(tmpDirectory);
            foreach (FileInfo file in directoryInfo.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                dir.Delete(true);
        }

        void SetProgress(int done, int outOf) {
            progressTextBlock.Text = done + " / " + outOf;
        }

        private void LockGUI() {
            primaryDirectoryTextBox.IsEnabled = false;
            secondaryDirectoryTextBox.IsEnabled = false;
            primaryDirectoryDialogButton.IsEnabled = false;
            secondaryDirectoryDialogButton.IsEnabled = false;
            primaryOnlyCheckBox.IsEnabled = false;
            removeByPathAndContentCheckBox.IsEnabled = false;
            removeByContentCheckBox.IsEnabled = false;
            removeEmptyDirectoriesFromPrimaryCheckBox.IsEnabled = false;
            removeEmptyDirectoriesFromSecondaryCheckBox.IsEnabled = false;
            keepFilesCheckBox.IsEnabled = false;
            basePathsCheckBox.IsEnabled = false;
            findButton.IsEnabled = false;
            emptyDirectoriesPrimaryButton.IsEnabled = false;
            emptyDirectoriesSecondaryButton.IsEnabled = false;
            emptyFilesPrimaryButton.IsEnabled = false;
            emptyFilesSecondaryButton.IsEnabled = false;
            duplicatedFilesPrimaryButton.IsEnabled = false;
            duplicatedFilesSecondaryButton.IsEnabled = false;
            emptyFilesPrimaryOnlyButton.IsEnabled = false;
            emptyDirectoriesPrimaryOnlyButton.IsEnabled = false;
        }

        private void UnlockGUI() {
            primaryDirectoryTextBox.IsEnabled = true;
            secondaryDirectoryTextBox.IsEnabled = true;
            primaryDirectoryDialogButton.IsEnabled = true;
            secondaryDirectoryDialogButton.IsEnabled = true;
            primaryOnlyCheckBox.IsEnabled = true;
            removeByPathAndContentCheckBox.IsEnabled = true;
            removeByContentCheckBox.IsEnabled = true;
            removeEmptyDirectoriesFromPrimaryCheckBox.IsEnabled = true;
            removeEmptyDirectoriesFromSecondaryCheckBox.IsEnabled = true;
            keepFilesCheckBox.IsEnabled = true;
            findButton.IsEnabled = true;
            basePathsCheckBox.IsEnabled = true;
            emptyDirectoriesPrimaryButton.IsEnabled = true;
            emptyDirectoriesSecondaryButton.IsEnabled = true;
            emptyFilesPrimaryButton.IsEnabled = true;
            emptyFilesSecondaryButton.IsEnabled = true;
            duplicatedFilesPrimaryButton.IsEnabled = true;
            duplicatedFilesSecondaryButton.IsEnabled = true;
            emptyFilesPrimaryOnlyButton.IsEnabled = true;
            emptyDirectoriesPrimaryOnlyButton.IsEnabled = true;
        }

        private void OpenDirectoryPrimary(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            if (!showBasePaths)
                path = primaryDirectory + path;
            try {
                Process.Start(path);
            }
            catch (Win32Exception) {
                Finder.emptyDirectoriesPrimary.Remove(((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text);
                emptyDirectoriesPrimaryListView.Items.Refresh();
                emptyDirectoriesPrimaryOnlyListView.Items.Refresh();
                Utility.Log("Directory \"" + path + "\" no longer exists.");
            }
        }

        private void OpenDirectorySecondary(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            if (!showBasePaths)
                path = secondaryDirectory + path;
            try {
                Process.Start(path);
            }
            catch (Win32Exception) {
                Finder.emptyDirectoriesSecondary.Remove(((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text);
                emptyDirectoriesSecondaryListView.Items.Refresh();
                Utility.Log("Directory \"" + path + "\" no longer exists.");
            }
        }

        private void OpenFileDirectoryPrimary(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            if (!showBasePaths)
                path = primaryDirectory + path;
            if (File.Exists(path))
                Process.Start("explorer.exe", "/select, \"" + path + "\"");
            else {
                path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
                if (!Finder.emptyFilesPrimary.Remove(path)) {
                    for (int i = 0; i < Finder.duplicatedFilesPrimaryOnly.Count; i++) {
                        int index = Finder.duplicatedFilesPrimaryOnly[i].FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            Finder.duplicatedFilesPrimaryOnly[i].RemoveAt(index);
                            if (Finder.duplicatedFilesPrimaryOnly[i].Count <= 1)
                                Finder.duplicatedFilesPrimaryOnly.RemoveAt(i);
                            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                            goto OpenFileDirectoryPrimaryFound;
                        }
                    }
                    for (int i = 0; i < Finder.duplicatedFiles.Count; i++) {
                        int index = Finder.duplicatedFiles[i].Item1.FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            Finder.duplicatedFiles[i].Item1.RemoveAt(index);
                            if (Finder.duplicatedFiles[i].Item2.Count == 0 && Finder.duplicatedFiles[i].Item1.Count <= 1)
                                Finder.duplicatedFiles.RemoveAt(i);
                            duplicatedFilesListView.Items.Refresh();
                            break;
                        }
                    }
                    OpenFileDirectoryPrimaryFound:;
                }
                else {
                    emptyFilesPrimaryListView.Items.Refresh();
                    emptyFilesPrimaryOnlyListView.Items.Refresh();
                }
                Utility.Log("File \"" + path + "\" no longer exists.");
            }
        }

        private void OpenFileDirectorySecondary(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            if (!showBasePaths)
                path = secondaryDirectory + path;
            if (File.Exists(path))
                Process.Start("explorer.exe", "/select, \"" + path + "\"");
            else {
                path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
                if (!Finder.emptyFilesSecondary.Remove(path)) {
                    for (int i = 0; i < Finder.duplicatedFiles.Count; i++) {
                        int index = Finder.duplicatedFiles[i].Item2.FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            Finder.duplicatedFiles[i].Item2.RemoveAt(index);
                            if (Finder.duplicatedFiles[i].Item2.Count == 0 && Finder.duplicatedFiles[i].Item1.Count <= 1)
                                Finder.duplicatedFiles.RemoveAt(i);
                            duplicatedFilesListView.Items.Refresh();
                            break;
                        }
                    }
                }
                else
                    emptyFilesSecondaryListView.Items.Refresh();
                Utility.Log("File \"" + path + "\" no longer exists.");
            }
        }

        private void EmptyDirectoriesPrimaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, primaryDirectory);
            EmptyDirectoriesPrimaryIgnoreFile(path);
        }

        private void EmptyDirectoriesPrimaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            EmptyDirectoriesPrimaryIgnoreFile(path);
        }

        private void EmptyDirectoriesPrimaryIgnoreFile(string path) {
            Finder.emptyDirectoriesPrimary.Remove(path);
            emptyDirectoriesPrimaryListView.Items.Refresh();
        }

        private void EmptyFilesPrimaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, primaryDirectory);
            EmptyFilesPrimaryIgnoreFile(path);
        }

        private void EmptyFilesPrimaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            EmptyFilesPrimaryIgnoreFile(path);
        }

        private void EmptyFilesPrimaryIgnoreFile(string path) {
            Finder.emptyFilesPrimary.Remove(path);
            emptyFilesPrimaryListView.Items.Refresh();
        }

        private void EmptyDirectoriesSecondaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, secondaryDirectory);
            EmptyDirectoriesSecondaryIgnoreFile(path);
        }

        private void EmptyDirectoriesSecondaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            EmptyDirectoriesSecondaryIgnoreFile(path);
        }

        private void EmptyDirectoriesSecondaryIgnoreFile(string path) {
            Finder.emptyDirectoriesSecondary.Remove(path);
            emptyDirectoriesSecondaryListView.Items.Refresh();
        }

        private void EmptyFilesSecondaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, secondaryDirectory);
            EmptyFilesSecondaryIgnoreFile(path);
        }

        private void EmptyFilesSecondaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            EmptyFilesSecondaryIgnoreFile(path);
        }

        private void EmptyFilesSecondaryIgnoreFile(string path) {
            Finder.emptyFilesSecondary.Remove(path);
            emptyFilesSecondaryListView.Items.Refresh();
        }

        private void DuplicatedFilesPrimaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, primaryDirectory);
            DuplicatedFilesPrimaryIgnoreFile(path);
        }

        private void DuplicatedFilesPrimaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            DuplicatedFilesPrimaryIgnoreFile(path);
        }

        private void DuplicatedFilesPrimaryIgnoreFile(string path) {
            for (int i = 0; i < Finder.duplicatedFiles.Count; i++) {
                for (int j = 0; j < Finder.duplicatedFiles[i].Item1.Count; j++) {
                    if (Finder.duplicatedFiles[i].Item1[j].Path.Equals(path)) {
                        Finder.duplicatedFiles[i].Item1.RemoveAt(j);
                        if (Finder.duplicatedFiles[i].Item1.Count + Finder.duplicatedFiles[i].Item2.Count <= 1)
                            Finder.duplicatedFiles.RemoveAt(i);
                        duplicatedFilesListView.Items.Refresh();
                        break;
                    }
                }
            }
        }

        private void DuplicatedFilesSecondaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, secondaryDirectory);
            DuplicatedFilesSecondaryIgnoreFile(path);
        }

        private void DuplicatedFilesSecondaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            DuplicatedFilesSecondaryIgnoreFile(path);
        }

        private void DuplicatedFilesSecondaryIgnoreFile(string path) {
            for (int i = 0; i < Finder.duplicatedFiles.Count; i++) {
                for (int j = 0; j < Finder.duplicatedFiles[i].Item2.Count; j++) {
                    if (Finder.duplicatedFiles[i].Item2[j].Path.Equals(path)) {
                        Finder.duplicatedFiles[i].Item2.RemoveAt(j);
                        if (Finder.duplicatedFiles[i].Item1.Count + Finder.duplicatedFiles[i].Item2.Count <= 1)
                            Finder.duplicatedFiles.RemoveAt(i);
                        duplicatedFilesListView.Items.Refresh();
                        break;
                    }
                }
            }
        }

        private void DuplicatedFilesPrimaryOnlyRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, primaryDirectory);
            DuplicatedFilesPrimaryOnlyIgnoreFile(path);
        }

        private void DuplicatedFilesPrimaryOnlyIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            DuplicatedFilesPrimaryOnlyIgnoreFile(path);
        }

        private void DuplicatedFilesPrimaryOnlyIgnoreFile(string path) {
            for (int i = 0; i < Finder.duplicatedFilesPrimaryOnly.Count; i++) {
                for (int j = 0; j < Finder.duplicatedFilesPrimaryOnly[i].Count; j++) {
                    if (Finder.duplicatedFilesPrimaryOnly[i][j].Path.Equals(path)) {
                        Finder.duplicatedFilesPrimaryOnly[i].RemoveAt(j);
                        if (Finder.duplicatedFilesPrimaryOnly[i].Count == 0)
                            Finder.duplicatedFilesPrimaryOnly.RemoveAt(i);
                        duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                        break;
                    }
                }
            }
        }

        private void RemoveFile(string path, string baseDirectory) {
            if (!showBasePaths)
                path = baseDirectory + path;
            try {
                if (File.GetAttributes(path).HasFlag(FileAttributes.Directory)) {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    if (backupFiles) {
                        storedFiles.Add(Tuple.Create(path, ""));
                        restoreButton.IsEnabled = true;
                    }
                    directoryInfo.Delete();
                }
                else {  // file
                    FileInfo fileInfo = new FileInfo(path);
                    if (fileInfo.Length < 2 * Finder.megaByte) {   // don't move large files
                        if (backupFiles) {
                            Guid guid = Guid.NewGuid();
                            fileInfo.MoveTo(tmpDirectory + guid.ToString());
                            storedFiles.Add(Tuple.Create(path, guid.ToString()));
                            restoreButton.IsEnabled = true;
                        }
                        else {
                            fileInfo.Delete();
                        }
                    }
                    else {
                        if (!askLarge || MessageBox.Show("You will permanently delete file " + path) == MessageBoxResult.OK)
                            fileInfo.Delete();
                    }
                }
            }
            catch (IOException) {
                Dispatcher.Invoke(() => Utility.Log("File " + path + " no longer exists."));
            }
            catch (UnauthorizedAccessException) {
                Dispatcher.Invoke(() => Utility.Log("Access denied for " + path));
            }
        }

        private void ShowBasePathsChecked(object sender, RoutedEventArgs e) {
            showBasePaths = true;
            for (int i = 0; i < Finder.duplicatedFiles.Count; i++) {
                for (int p = 0; p < Finder.duplicatedFiles[i].Item1.Count; p++)
                    Finder.duplicatedFiles[i].Item1[p].Path = primaryDirectory + Finder.duplicatedFiles[i].Item1[p].Path;
                for (int s = 0; s < Finder.duplicatedFiles[i].Item2.Count; s++)
                    Finder.duplicatedFiles[i].Item2[s].Path = secondaryDirectory + Finder.duplicatedFiles[i].Item2[s].Path;
            }
            duplicatedFilesListView.Items.Refresh();
            for (int i = 0; i < Finder.emptyDirectoriesPrimary.Count; i++)
                Finder.emptyDirectoriesPrimary[i] = primaryDirectory + Finder.emptyDirectoriesPrimary[i];
            emptyDirectoriesPrimaryListView.Items.Refresh();
            for (int i = 0; i < Finder.emptyFilesPrimary.Count; i++)
                Finder.emptyFilesPrimary[i] = primaryDirectory + Finder.emptyFilesPrimary[i];
            emptyFilesPrimaryListView.Items.Refresh();
            for (int i = 0; i < Finder.emptyDirectoriesSecondary.Count; i++)
                Finder.emptyDirectoriesSecondary[i] = secondaryDirectory + Finder.emptyDirectoriesSecondary[i];
            emptyDirectoriesSecondaryListView.Items.Refresh();
            for (int i = 0; i < Finder.emptyFilesSecondary.Count; i++)
                Finder.emptyFilesSecondary[i] = secondaryDirectory + Finder.emptyFilesSecondary[i];
            emptyFilesSecondaryListView.Items.Refresh();
            for (int i = 0; i < Finder.duplicatedFilesPrimaryOnly.Count; i++)
                for (int p = 0; p < Finder.duplicatedFilesPrimaryOnly[i].Count; p++)
                    Finder.duplicatedFilesPrimaryOnly[i][p].Path = primaryDirectory + Finder.duplicatedFilesPrimaryOnly[i][p].Path;
            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
        }

        private void ShowBasePathsUnchecked(object sender, RoutedEventArgs e) {
            showBasePaths = false;
            for (int i = 0; i < Finder.duplicatedFiles.Count; i++) {
                for (int p = 0; p < Finder.duplicatedFiles[i].Item1.Count; p++)
                    Finder.duplicatedFiles[i].Item1[p].Path = new string(Finder.duplicatedFiles[i].Item1[p].Path.Skip(primaryDirectory.Length).ToArray());
                for (int s = 0; s < Finder.duplicatedFiles[i].Item2.Count; s++)
                    Finder.duplicatedFiles[i].Item2[s].Path = new string(Finder.duplicatedFiles[i].Item2[s].Path.Skip(secondaryDirectory.Length).ToArray());
            }
            duplicatedFilesListView.Items.Refresh();
            for (int i = 0; i < Finder.emptyDirectoriesPrimary.Count; i++)
                Finder.emptyDirectoriesPrimary[i] = new string(Finder.emptyDirectoriesPrimary[i].Skip(primaryDirectory.Length).ToArray());
            emptyDirectoriesPrimaryListView.Items.Refresh();
            for (int i = 0; i < Finder.emptyFilesPrimary.Count; i++)
                Finder.emptyFilesPrimary[i] = new string(Finder.emptyFilesPrimary[i].Skip(primaryDirectory.Length).ToArray());
            emptyFilesPrimaryListView.Items.Refresh();
            for (int i = 0; i < Finder.emptyDirectoriesSecondary.Count; i++)
                Finder.emptyDirectoriesSecondary[i] = new string(Finder.emptyDirectoriesSecondary[i].Skip(secondaryDirectory.Length).ToArray());
            emptyDirectoriesSecondaryListView.Items.Refresh();
            for (int i = 0; i < Finder.emptyFilesSecondary.Count; i++)
                Finder.emptyFilesSecondary[i] = new string(Finder.emptyFilesSecondary[i].Skip(secondaryDirectory.Length).ToArray());
            emptyFilesSecondaryListView.Items.Refresh();
            for (int i = 0; i < Finder.duplicatedFilesPrimaryOnly.Count; i++)
                for (int p = 0; p < Finder.duplicatedFilesPrimaryOnly[i].Count; p++)
                    Finder.duplicatedFilesPrimaryOnly[i][p].Path = new string(Finder.duplicatedFilesPrimaryOnly[i][p].Path.Skip(primaryDirectory.Length).ToArray());
            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
        }

        private void IdenticalSubpathChecked(object sender, RoutedEventArgs e) {

        }

        private void IdenticalSubpathUnchecked(object sender, RoutedEventArgs e) {

        }

        private void BackupFilesChecked(object sender, RoutedEventArgs e) {
            backupFiles = true;
        }

        private void BackupFilesUnchecked(object sender, RoutedEventArgs e) {
            backupFiles = false;
            askLargeCheckBox.IsChecked = false;
        }

        private void AskLargeChecked(object sender, RoutedEventArgs e) {
            askLarge = true;
            backupFilesCheckBox.IsChecked = true;
        }

        private void AskLargeUnchecked(object sender, RoutedEventArgs e) {
            askLarge = false;
        }

        private void RestoreFiles(object sender, RoutedEventArgs e) {
            RestoreFileDialog popup = new RestoreFileDialog(storedFiles);
            popup.ShowDialog();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            DirectoryInfo directoryInfo = new DirectoryInfo(tmpDirectory);
            foreach (FileInfo file in directoryInfo.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                dir.Delete(true);
        }

        private void SortAlphabetically(object sender, RoutedEventArgs e) {
            sortBySize = false;
            Finder.duplicatedFiles.Sort((a, b) => a.Item1[0].Path.CompareTo(b.Item1[0].Path));
            duplicatedFilesListView.Items.Refresh();
        }

        private void SortBySize(object sender, RoutedEventArgs e) {
            sortBySize = true;
            bool sorted = false;
            string baseDirectory = "";
            if (!showBasePaths)
                baseDirectory = primaryDirectory;
            while (!sorted) {
                try {
                    Finder.duplicatedFiles.Sort((a, b) => {
                        long aLength = Finder.GetFileLength(baseDirectory + a.Item1[0].Path);
                        if (aLength < 0) {
                            a.Item1.RemoveAt(0);
                            throw new SortException();
                        }
                        long bLength = Finder.GetFileLength(baseDirectory + b.Item1[0].Path);
                        if (bLength < 0) {
                            b.Item1.RemoveAt(0);
                            throw new SortException();
                        }
                        return aLength.CompareTo(bLength);
                    });
                }
                catch (InvalidOperationException ex) {
                    if (ex.InnerException is SortException) {
                        for (int i = 0; i < Finder.duplicatedFiles.Count; i++) {
                            for (int j = 0; j < Finder.duplicatedFiles[i].Item1.Count; j++) {
                                if (Finder.GetFileLength(baseDirectory + Finder.duplicatedFiles[i].Item1[j].Path) < 0) {
                                    Finder.duplicatedFiles[i].Item1.RemoveAt(j);
                                    j--;
                                }
                            }
                            for (int j = 0; j < Finder.duplicatedFiles[i].Item2.Count; j++) {
                                if (Finder.GetFileLength(baseDirectory + Finder.duplicatedFiles[i].Item2[j].Path) < 0) {
                                    Finder.duplicatedFiles[i].Item2.RemoveAt(j);
                                    j--;
                                }
                            }
                            if (Finder.duplicatedFiles[i].Item1.Count + Finder.duplicatedFiles[i].Item2.Count <= 1) {
                                Finder.duplicatedFiles.RemoveAt(i);
                                i--;
                            }
                        }
                        continue;
                    }
                }
                sorted = true;
            }
            duplicatedFilesListView?.Items.Refresh();
        }

        private void SortAlphabeticallyPrimaryOnly(object sender, RoutedEventArgs e) {
            sortBySizePrimaryOnly = false;
            Finder.duplicatedFilesPrimaryOnly.Sort((a, b) => a[0].Path.CompareTo(b[0].Path));
            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
        }

        private void SortBySizePrimaryOnly(object sender, RoutedEventArgs e) {
            sortBySizePrimaryOnly = true;
            bool sorted = false;
            string baseDirectory = "";
            if (!showBasePaths)
                baseDirectory = primaryDirectory;
            while (!sorted) {
                try {
                    Finder.duplicatedFilesPrimaryOnly.Sort((a, b) => {
                        long aLength = Finder.GetFileLength(baseDirectory + a[0].Path);
                        if (aLength < 0) {
                            a.RemoveAt(0);
                            if (a.Count == 0)
                                Finder.duplicatedFilesPrimaryOnly.Remove(a);
                            throw new SortException();
                        }
                        long bLength = Finder.GetFileLength(baseDirectory + b[0].Path);
                        if (bLength < 0) {
                            b.RemoveAt(0);
                            if (b.Count == 0)
                                Finder.duplicatedFilesPrimaryOnly.Remove(b);
                            throw new SortException();
                        }
                        return aLength.CompareTo(bLength);
                    });
                }
                catch (InvalidOperationException ex) {
                    if (ex.InnerException is SortException) {
                        for (int i = 0; i < Finder.duplicatedFilesPrimaryOnly.Count; i++) {
                            for (int j = 0; j < Finder.duplicatedFilesPrimaryOnly[i].Count; j++) {
                                if (Finder.GetFileLength(baseDirectory + Finder.duplicatedFilesPrimaryOnly[i][j].Path) < 0) {
                                    Finder.duplicatedFilesPrimaryOnly[i].RemoveAt(j);
                                    j--;
                                }
                            }
                            if (Finder.duplicatedFilesPrimaryOnly[i].Count <= 1) {
                                Finder.duplicatedFilesPrimaryOnly.RemoveAt(i);
                                i--;
                            }
                        }
                        continue;
                    }
                }
                sorted = true;
            }
            duplicatedFilesPrimaryOnlyListView?.Items.Refresh();
        }

        private void PrimaryOnlyChecked(object sender, RoutedEventArgs e) {
            primaryOnly = true;
            secondaryDirectoryTextBox.IsEnabled = false;
            emptyDirectoriesTabItem.Visibility = Visibility.Collapsed;
            emptyFilesTabItem.Visibility = Visibility.Collapsed;
            duplicatedFilesTabItem.Visibility = Visibility.Collapsed;
            emptyDirectoriesPrimaryOnlyTabItem.Visibility = Visibility.Visible;
            emptyFilesPrimaryOnlyTabItem.Visibility = Visibility.Visible;
            duplicatedFilesPrimaryOnlyTabItem.Visibility = Visibility.Visible;
        }

        private void PrimaryOnlyUnchecked(object sender, RoutedEventArgs e) {
            primaryOnly = false;
            secondaryDirectoryTextBox.IsEnabled = true;
            emptyDirectoriesTabItem.Visibility = Visibility.Visible;
            emptyFilesTabItem.Visibility = Visibility.Visible;
            duplicatedFilesTabItem.Visibility = Visibility.Visible;
            emptyDirectoriesPrimaryOnlyTabItem.Visibility = Visibility.Collapsed;
            emptyFilesPrimaryOnlyTabItem.Visibility = Visibility.Collapsed;
            duplicatedFilesPrimaryOnlyTabItem.Visibility = Visibility.Collapsed;
        }

        private void RemoveAllEmptyDirectoriesPrimary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + primaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            bool saveStateOfBackupFiles = backupFiles;
            bool saveStateOfAskLarge = askLarge;
            backupFiles = false;
            askLarge = false;
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Visible;
            progressTextBlock.Visibility = Visibility.Visible;
            SetProgress(0, Finder.emptyDirectoriesPrimary.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                for (int i = 0; i < Finder.emptyDirectoriesPrimary.Count; i++) {
                    RemoveFile(Finder.emptyDirectoriesPrimary[i], primaryDirectory);
                    Dispatcher.Invoke(() => {
                        progressBar.Value = i * 100f / Finder.emptyDirectoriesPrimary.Count;
                        SetProgress(i, Finder.emptyDirectoriesPrimary.Count);
                    });
                }
                Finder.emptyDirectoriesPrimary.Clear();
                backupFiles = saveStateOfBackupFiles;
                askLarge = saveStateOfAskLarge;
                Dispatcher.Invoke(() => {
                    progressBar.Visibility = Visibility.Hidden;
                    progressTextBlock.Visibility = Visibility.Hidden;
                    emptyDirectoriesPrimaryListView.Items.Refresh();
                    emptyDirectoriesPrimaryOnlyListView.Items.Refresh();
                    UnlockGUI();
                    stateTextBlock.Text = "Done";
                });
            }).Start();
        }

        private void RemoveAllEmptyDirectoriesSecondary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + secondaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            bool saveStateOfBackupFiles = backupFiles;
            bool saveStateOfAskLarge = askLarge;
            backupFiles = false;
            askLarge = false;
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Visible;
            progressTextBlock.Visibility = Visibility.Visible;
            SetProgress(0, Finder.emptyDirectoriesSecondary.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                for (int i = 0; i < Finder.emptyDirectoriesSecondary.Count; i++) {
                    RemoveFile(Finder.emptyDirectoriesSecondary[i], secondaryDirectory);
                    Dispatcher.Invoke(() => {
                        progressBar.Value = i * 100f / Finder.emptyDirectoriesSecondary.Count;
                        SetProgress(i, Finder.emptyDirectoriesSecondary.Count);
                    });
                }
                Finder.emptyDirectoriesSecondary.Clear();
                backupFiles = saveStateOfBackupFiles;
                askLarge = saveStateOfAskLarge;
                Dispatcher.Invoke(() => {
                    progressBar.Visibility = Visibility.Hidden;
                    progressTextBlock.Visibility = Visibility.Hidden;
                    emptyDirectoriesSecondaryListView.Items.Refresh();
                    UnlockGUI();
                    stateTextBlock.Text = "Done";
                });
            }).Start();
        }

        private void RemoveAllEmptyFilesPrimary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + primaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            bool saveStateOfBackupFiles = backupFiles;
            bool saveStateOfAskLarge = askLarge;
            backupFiles = false;
            askLarge = false;
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Visible;
            progressTextBlock.Visibility = Visibility.Visible;
            SetProgress(0, Finder.emptyFilesPrimary.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                for (int i = 0; i < Finder.emptyFilesPrimary.Count; i++) {
                    RemoveFile(Finder.emptyFilesPrimary[i], primaryDirectory);
                    Dispatcher.Invoke(() => {
                        progressBar.Value = i * 100f / Finder.emptyFilesPrimary.Count;
                        SetProgress(i, Finder.emptyFilesPrimary.Count);
                    });
                }
                Finder.emptyFilesPrimary.Clear();
                backupFiles = saveStateOfBackupFiles;
                askLarge = saveStateOfAskLarge;
                Dispatcher.Invoke(() => {
                    progressBar.Visibility = Visibility.Hidden;
                    progressTextBlock.Visibility = Visibility.Hidden;
                    emptyFilesPrimaryListView.Items.Refresh();
                    emptyFilesPrimaryOnlyListView.Items.Refresh();
                    UnlockGUI();
                    stateTextBlock.Text = "Done";
                });
            }).Start();
        }

        private void RemoveAllEmptyFilesSecondary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + secondaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            bool saveStateOfBackupFiles = backupFiles;
            bool saveStateOfAskLarge = askLarge;
            backupFiles = false;
            askLarge = false;
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Visible;
            progressTextBlock.Visibility = Visibility.Visible;
            SetProgress(0, Finder.emptyFilesSecondary.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                for (int i = 0; i < Finder.emptyFilesSecondary.Count; i++) {
                    RemoveFile(Finder.emptyFilesSecondary[i], secondaryDirectory);
                    Dispatcher.Invoke(() => {
                        progressBar.Value = i * 100f / Finder.emptyFilesSecondary.Count;
                        SetProgress(i, Finder.emptyFilesSecondary.Count);
                    });
                }
                Finder.emptyFilesSecondary.Clear();
                backupFiles = saveStateOfBackupFiles;
                askLarge = saveStateOfAskLarge;
                Dispatcher.Invoke(() => {
                    progressBar.Visibility = Visibility.Hidden;
                    progressTextBlock.Visibility = Visibility.Hidden;
                    emptyFilesSecondaryListView.Items.Refresh();
                    UnlockGUI();
                    stateTextBlock.Text = "Done";
                });
            }).Start();
        }

        private void RemoveAllPrimary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + primaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            bool saveStateOfBackupFiles = backupFiles;
            bool saveStateOfAskLarge = askLarge;
            backupFiles = false;
            askLarge = false;
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Visible;
            progressTextBlock.Visibility = Visibility.Visible;
            SetProgress(0, Finder.duplicatedFiles.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                int count = Finder.duplicatedFiles.Count;
                for (int i = 0; i < Finder.duplicatedFiles.Count;) {
                    for (int j = 0; j < Finder.duplicatedFiles[i].Item1.Count; j++)
                        RemoveFile(Finder.duplicatedFiles[i].Item1[j].Path, primaryDirectory);
                    if (Finder.duplicatedFiles[i].Item2.Count <= 1) {
                        Finder.duplicatedFiles.RemoveAt(i);
                    }
                    else {
                        Finder.duplicatedFiles[i].Item1.Clear();
                        i++;
                    }
                    Dispatcher.Invoke(() => {
                        int progress = i + count - Finder.duplicatedFiles.Count;
                        progressBar.Value = progress * 100f / count;
                        SetProgress(progress, Finder.duplicatedFiles.Count);
                    });
                }
                Dispatcher.Invoke(() => {
                    progressBar.Visibility = Visibility.Hidden;
                    progressTextBlock.Visibility = Visibility.Hidden;
                    backupFiles = saveStateOfBackupFiles;
                    askLarge = saveStateOfAskLarge;
                    duplicatedFilesListView.Items.Refresh();
                    UnlockGUI();
                    stateTextBlock.Text = "Done";
                });
            }).Start();
        }

        private void RemoveAllSecondary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + secondaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            bool saveStateOfBackupFiles = backupFiles;
            bool saveStateOfAskLarge = askLarge;
            backupFiles = false;
            askLarge = false;
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Visible;
            progressTextBlock.Visibility = Visibility.Visible;
            SetProgress(0, Finder.duplicatedFiles.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                int count = Finder.duplicatedFiles.Count;
                for (int i = 0; i < Finder.duplicatedFiles.Count;) {
                    for (int j = 0; j < Finder.duplicatedFiles[i].Item2.Count; j++)
                        RemoveFile(Finder.duplicatedFiles[i].Item2[j].Path, secondaryDirectory);
                    if (Finder.duplicatedFiles[i].Item1.Count <= 1) {
                        Finder.duplicatedFiles.RemoveAt(i);
                    }
                    else {
                        Finder.duplicatedFiles[i].Item2.Clear();
                        i++;
                    }
                    Dispatcher.Invoke(() => {
                        int progress = i + count - Finder.duplicatedFiles.Count;
                        progressBar.Value = progress * 100f / count;
                        SetProgress(progress, Finder.duplicatedFiles.Count);
                    });
                }
                Dispatcher.Invoke(() => {
                    progressBar.Visibility = Visibility.Hidden;
                    progressTextBlock.Visibility = Visibility.Hidden;
                    backupFiles = saveStateOfBackupFiles;
                    askLarge = saveStateOfAskLarge;
                    duplicatedFilesListView.Items.Refresh();
                    UnlockGUI();
                    stateTextBlock.Text = "Done";
                });
            }).Start();
        }

        [Obsolete]
        private void RemoveAllPrimarOnly(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + primaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            bool saveStateOfBackupFiles = backupFiles;
            bool saveStateOfAskLarge = askLarge;
            backupFiles = false;
            askLarge = false;
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Visible;
            progressTextBlock.Visibility = Visibility.Visible;
            SetProgress(0, Finder.duplicatedFilesPrimaryOnly.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                int count = Finder.duplicatedFilesPrimaryOnly.Count;
                for (int i = 0; i < Finder.duplicatedFilesPrimaryOnly.Count; i++) {
                    for (int j = 0; j < Finder.duplicatedFilesPrimaryOnly[i].Count; j++)
                        RemoveFile(Finder.duplicatedFilesPrimaryOnly[i][j].Path, primaryDirectory);
                    Dispatcher.Invoke(() => {
                        int progress = i + count - Finder.duplicatedFilesPrimaryOnly.Count;
                        progressBar.Value = progress * 100f / count;
                        SetProgress(progress, Finder.duplicatedFilesPrimaryOnly.Count);
                    });
                }
                Finder.duplicatedFilesPrimaryOnly.Clear();
                Dispatcher.Invoke(() => {
                    progressBar.Visibility = Visibility.Hidden;
                    progressTextBlock.Visibility = Visibility.Hidden;
                    backupFiles = saveStateOfBackupFiles;
                    askLarge = saveStateOfAskLarge;
                    duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                    UnlockGUI();
                    stateTextBlock.Text = "Done";
                });
            }).Start();
        }

        private void ShowButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            Border border = sender as Border;
            Grid grid = border.Child as Grid;
            border.Background = Brushes.AliceBlue;
            foreach (var child in grid.Children)
                if (child is Button)
                    ((Button)child).Visibility = Visibility.Visible;
        }

        private void HideButtons(object sender, System.Windows.Input.MouseEventArgs e) {
            Border border = sender as Border;
            Grid grid = border.Child as Grid;
            border.Background = Brushes.Transparent;
            foreach (var child in grid.Children)
                if (child is Button)
                    ((Button)child).Visibility = Visibility.Collapsed;
        }


        Thread currentTask;
        private void AbortTask(object sender, RoutedEventArgs e) {
            currentTask.Abort();//or interrupt to do something like finally block in try catch

            stateTextBlock.Text = "Aborted";
        }
    }
}
