//TODO
//recognize identical subpath
// show file size checkbox
// remove/ignore/show in explorer on restore list
//scroll propagate down to main list
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
// checkbox remove from list when only one element left whit no other to compare to (do not delete elements from list after ignoring other for sure
//  add watermark text after removing files from list because they don't have a duplicate saying something like "Files that did not have a duplicate after your operation were removed from this list"
// config file with ignored directories in search and mayby open last checked
// start progressbar when started searching for duplicates, during initialization make progressbar shift green area constantly
// ikonki folderów
// dont store names as guids but as normal name but make sure the names don't collide easiest way - add guid at the end, can use some base64 number of files in this directory past max or etc
// do something with hiding list elements on remove all, the <= 1 is bad as default as it's scary to see all elements disappear, maybe add an information, make them collapse into subgroup or make a checkbox to make it an opitonal feature
// Features:
// look at files in archives too
// show files with match percentage above given treshold
// regex search names (add tooltip because it's hard to use - "If you want to search for files with some extension write *.jpg, see full guide online link")

using Microsoft.WindowsAPICodePack.Dialogs;
using System;
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
        private const int pathChildPosition = 1;
        private volatile bool abortTask = false;
        private bool showBasePaths = false;
        private bool backupFiles = true;
        private bool askLarge = true;
        private bool sortBySize = true;
        private bool primaryOnly = false;
        private bool sortBySizePrimaryOnly = true;
        private string primaryDirectory;
        private string secondaryDirectory;
        private string tmpDirectory = AppDomain.CurrentDomain.BaseDirectory + "tmp/";

        public MainWindow() {
            InitializeComponent();
            Utility.logListView = logListView;
            Utility.logTabItem = logTabItem;
            Utility.dispatcher = Dispatcher;
            Utility.progressTextBlock = progressTextBlock;
            emptyDirectoriesPrimaryListView.ItemsSource = FileManager.emptyDirectoriesPrimary;
            emptyFilesPrimaryListView.ItemsSource = FileManager.emptyFilesPrimary;
            emptyDirectoriesSecondaryListView.ItemsSource = FileManager.emptyDirectoriesSecondary;
            emptyFilesSecondaryListView.ItemsSource = FileManager.emptyFilesSecondary;
            duplicatedFilesListView.ItemsSource = FileManager.duplicatedFiles;
            emptyDirectoriesPrimaryOnlyListView.ItemsSource = FileManager.emptyDirectoriesPrimary;
            emptyFilesPrimaryOnlyListView.ItemsSource = FileManager.emptyFilesPrimary;
            duplicatedFilesPrimaryOnlyListView.ItemsSource = FileManager.duplicatedFilesPrimaryOnly;
            FileManager.tmpDirectory = tmpDirectory;
            FileManager.dispatcher = Dispatcher;
            FileManager.stateTextBlock = stateTextBlock;
            FileManager.progressBar = progressBar;
            FileManager.progressTextBlock = progressTextBlock;
            FileManager.emptyDirectoriesPrimaryOnlyListView = emptyDirectoriesPrimaryOnlyListView;
            FileManager.emptyFilesPrimaryOnlyListView = emptyFilesPrimaryOnlyListView;
            FileManager.duplicatedFilesPrimaryOnlyListView = duplicatedFilesPrimaryOnlyListView;
            FileManager.emptyDirectoriesPrimaryListView = emptyDirectoriesPrimaryListView;
            FileManager.emptyFilesPrimaryListView = emptyFilesPrimaryListView;
            FileManager.emptyDirectoriesSecondaryListView = emptyDirectoriesSecondaryListView;
            FileManager.emptyFilesSecondaryListView = emptyFilesSecondaryListView;
            FileManager.duplicatedFilesListView = duplicatedFilesListView;

            Directory.CreateDirectory(tmpDirectory);
        }

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

            FileManager.FindDuplicatedFiles(primaryDirectory, showBasePaths);

            if (!sortBySizePrimaryOnly)
                FileManager.duplicatedFilesPrimaryOnly.Sort((a, b) => a[0].Path.CompareTo(b[0].Path));

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

            FileManager.FindDuplicatedFiles(primaryDirectory, secondaryDirectory, showBasePaths);


            if (!sortBySize)
                FileManager.duplicatedFiles.Sort((a, b) => a.Item1[0].Path.CompareTo(b.Item1[0].Path));

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
            FileManager.primaryFiles.Clear();
            FileManager.secondaryFiles.Clear();
            logListView.Items.Clear();
            FileManager.emptyDirectoriesPrimary.Clear();
            FileManager.emptyFilesPrimary.Clear();
            FileManager.emptyDirectoriesSecondary.Clear();
            FileManager.emptyFilesSecondary.Clear();
            FileManager.duplicatedFiles.Clear();
            FileManager.duplicatedFilesPrimaryOnly.Clear();
            duplicatedFilesListView.Items.Refresh();
            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
            primaryDirectory = primaryDirectoryTextBox.Text;
            secondaryDirectory = secondaryDirectoryTextBox.Text;
            ClearTmpDirectory();
            FileManager.storedFiles.Clear();
            restoreButton.IsEnabled = false;
        }

        private void FinalizeDuplicateFinding() {
            if (abortTask) {
                abortTask = false;
                stateTextBlock.Text = "Aborted";
            }
            else
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

        private void LockGUI() {
            primaryDirectoryTextBox.IsEnabled = false;
            secondaryDirectoryTextBox.IsEnabled = false;
            primaryDirectoryDialogButton.IsEnabled = false;
            secondaryDirectoryDialogButton.IsEnabled = false;
            primaryOnlyCheckBox.IsEnabled = false;
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
            abortButton.IsEnabled = true;
        }

        private void UnlockGUI() {
            primaryDirectoryTextBox.IsEnabled = true;
            secondaryDirectoryTextBox.IsEnabled = true;
            primaryDirectoryDialogButton.IsEnabled = true;
            secondaryDirectoryDialogButton.IsEnabled = true;
            primaryOnlyCheckBox.IsEnabled = true;
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
            abortButton.IsEnabled = false;
        }

        private void OpenDirectoryPrimary(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            if (!showBasePaths)
                path = primaryDirectory + path;
            try {
                Process.Start(path);
            }
            catch (Win32Exception) {
                FileManager.emptyDirectoriesPrimary.Remove(((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text);
                emptyDirectoriesPrimaryListView.Items.Refresh();
                emptyDirectoriesPrimaryOnlyListView.Items.Refresh();
                Utility.Log("Directory \"" + path + "\" no longer exists.");
            }
        }

        private void OpenDirectorySecondary(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            if (!showBasePaths)
                path = secondaryDirectory + path;
            try {
                Process.Start(path);
            }
            catch (Win32Exception) {
                FileManager.emptyDirectoriesSecondary.Remove(((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text);
                emptyDirectoriesSecondaryListView.Items.Refresh();
                Utility.Log("Directory \"" + path + "\" no longer exists.");
            }
        }

        private void OpenFileDirectoryPrimary(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            if (!showBasePaths)
                path = primaryDirectory + path;
            if (File.Exists(path))
                Process.Start("explorer.exe", "/select, \"" + path + "\"");
            else {
                path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
                if (!FileManager.emptyFilesPrimary.Remove(path)) {
                    for (int i = 0; i < FileManager.duplicatedFilesPrimaryOnly.Count; i++) {
                        int index = FileManager.duplicatedFilesPrimaryOnly[i].FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            FileManager.duplicatedFilesPrimaryOnly[i].RemoveAt(index);
                            if (FileManager.duplicatedFilesPrimaryOnly[i].Count <= 1)
                                FileManager.duplicatedFilesPrimaryOnly.RemoveAt(i);
                            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                            goto OpenFileDirectoryPrimaryFound;
                        }
                    }
                    for (int i = 0; i < FileManager.duplicatedFiles.Count; i++) {
                        int index = FileManager.duplicatedFiles[i].Item1.FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            FileManager.duplicatedFiles[i].Item1.RemoveAt(index);
                            if (FileManager.duplicatedFiles[i].Item2.Count == 0 && FileManager.duplicatedFiles[i].Item1.Count <= 1)
                                FileManager.duplicatedFiles.RemoveAt(i);
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
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            if (!showBasePaths)
                path = secondaryDirectory + path;
            if (File.Exists(path))
                Process.Start("explorer.exe", "/select, \"" + path + "\"");
            else {
                path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
                if (!FileManager.emptyFilesSecondary.Remove(path)) {
                    for (int i = 0; i < FileManager.duplicatedFiles.Count; i++) {
                        int index = FileManager.duplicatedFiles[i].Item2.FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            FileManager.duplicatedFiles[i].Item2.RemoveAt(index);
                            if (FileManager.duplicatedFiles[i].Item2.Count == 0 && FileManager.duplicatedFiles[i].Item1.Count <= 1)
                                FileManager.duplicatedFiles.RemoveAt(i);
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
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, primaryDirectory);
            FileManager.EmptyDirectoriesPrimaryIgnoreFile(path);
        }

        private void EmptyDirectoriesPrimaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.EmptyDirectoriesPrimaryIgnoreFile(path);
        }

        private void EmptyFilesPrimaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, primaryDirectory);
            FileManager.EmptyFilesPrimaryIgnoreFile(path);
        }

        private void EmptyFilesPrimaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.EmptyFilesPrimaryIgnoreFile(path);
        }

        private void EmptyDirectoriesSecondaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, secondaryDirectory);
            FileManager.EmptyDirectoriesSecondaryIgnoreFile(path);
        }

        private void EmptyDirectoriesSecondaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.EmptyDirectoriesSecondaryIgnoreFile(path);
        }

        private void EmptyFilesSecondaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, secondaryDirectory);
            FileManager.EmptyFilesSecondaryIgnoreFile(path);
        }

        private void EmptyFilesSecondaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.EmptyFilesSecondaryIgnoreFile(path);
        }

        private void DuplicatedFilesPrimaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, primaryDirectory);
            FileManager.DuplicatedFilesPrimaryIgnoreFile(path);
        }

        private void DuplicatedFilesPrimaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.DuplicatedFilesPrimaryIgnoreFile(path);
        }

        private void DuplicatedFilesSecondaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, secondaryDirectory);
            FileManager.DuplicatedFilesSecondaryIgnoreFile(path);
        }

        private void DuplicatedFilesSecondaryIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.DuplicatedFilesSecondaryIgnoreFile(path);
        }

        private void DuplicatedFilesPrimaryOnlyRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            RemoveFile(path, primaryDirectory);
            FileManager.DuplicatedFilesPrimaryOnlyIgnoreFile(path);
        }

        private void DuplicatedFilesPrimaryOnlyIgnoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[pathChildPosition]).Text;
            FileManager.DuplicatedFilesPrimaryOnlyIgnoreFile(path);
        }

        private void RemoveFile(string path, string baseDirectory) {
            if (!showBasePaths)
                path = baseDirectory + path;
            // try catch log here
            if (FileManager.RemoveFile(path, backupFiles, askLarge))
                restoreButton.IsEnabled = true;
        }

        private void ShowBasePathsChecked(object sender, RoutedEventArgs e) {
            showBasePaths = true;
            for (int i = 0; i < FileManager.duplicatedFiles.Count; i++) {
                for (int p = 0; p < FileManager.duplicatedFiles[i].Item1.Count; p++)
                    FileManager.duplicatedFiles[i].Item1[p].Path = primaryDirectory + FileManager.duplicatedFiles[i].Item1[p].Path;
                for (int s = 0; s < FileManager.duplicatedFiles[i].Item2.Count; s++)
                    FileManager.duplicatedFiles[i].Item2[s].Path = secondaryDirectory + FileManager.duplicatedFiles[i].Item2[s].Path;
            }
            duplicatedFilesListView.Items.Refresh();
            for (int i = 0; i < FileManager.emptyDirectoriesPrimary.Count; i++)
                FileManager.emptyDirectoriesPrimary[i].Path = primaryDirectory + FileManager.emptyDirectoriesPrimary[i].Path;
            emptyDirectoriesPrimaryListView.Items.Refresh();
            for (int i = 0; i < FileManager.emptyFilesPrimary.Count; i++)
                FileManager.emptyFilesPrimary[i].Path = primaryDirectory + FileManager.emptyFilesPrimary[i].Path;
            emptyFilesPrimaryListView.Items.Refresh();
            for (int i = 0; i < FileManager.emptyDirectoriesSecondary.Count; i++)
                FileManager.emptyDirectoriesSecondary[i].Path = secondaryDirectory + FileManager.emptyDirectoriesSecondary[i].Path;
            emptyDirectoriesSecondaryListView.Items.Refresh();
            for (int i = 0; i < FileManager.emptyFilesSecondary.Count; i++)
                FileManager.emptyFilesSecondary[i].Path = secondaryDirectory + FileManager.emptyFilesSecondary[i].Path;
            emptyFilesSecondaryListView.Items.Refresh();
            for (int i = 0; i < FileManager.duplicatedFilesPrimaryOnly.Count; i++)
                for (int p = 0; p < FileManager.duplicatedFilesPrimaryOnly[i].Count; p++)
                    FileManager.duplicatedFilesPrimaryOnly[i][p].Path = primaryDirectory + FileManager.duplicatedFilesPrimaryOnly[i][p].Path;
            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
        }

        private void ShowBasePathsUnchecked(object sender, RoutedEventArgs e) {
            showBasePaths = false;
            for (int i = 0; i < FileManager.duplicatedFiles.Count; i++) {
                for (int p = 0; p < FileManager.duplicatedFiles[i].Item1.Count; p++)
                    FileManager.duplicatedFiles[i].Item1[p].Path = new string(FileManager.duplicatedFiles[i].Item1[p].Path.Skip(primaryDirectory.Length).ToArray());
                for (int s = 0; s < FileManager.duplicatedFiles[i].Item2.Count; s++)
                    FileManager.duplicatedFiles[i].Item2[s].Path = new string(FileManager.duplicatedFiles[i].Item2[s].Path.Skip(secondaryDirectory.Length).ToArray());
            }
            duplicatedFilesListView.Items.Refresh();
            for (int i = 0; i < FileManager.emptyDirectoriesPrimary.Count; i++)
                FileManager.emptyDirectoriesPrimary[i].Path = new string(FileManager.emptyDirectoriesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());
            emptyDirectoriesPrimaryListView.Items.Refresh();
            for (int i = 0; i < FileManager.emptyFilesPrimary.Count; i++)
                FileManager.emptyFilesPrimary[i].Path = new string(FileManager.emptyFilesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());
            emptyFilesPrimaryListView.Items.Refresh();
            for (int i = 0; i < FileManager.emptyDirectoriesSecondary.Count; i++)
                FileManager.emptyDirectoriesSecondary[i].Path = new string(FileManager.emptyDirectoriesSecondary[i].Path.Skip(secondaryDirectory.Length).ToArray());
            emptyDirectoriesSecondaryListView.Items.Refresh();
            for (int i = 0; i < FileManager.emptyFilesSecondary.Count; i++)
                FileManager.emptyFilesSecondary[i].Path = new string(FileManager.emptyFilesSecondary[i].Path.Skip(secondaryDirectory.Length).ToArray());
            emptyFilesSecondaryListView.Items.Refresh();
            for (int i = 0; i < FileManager.duplicatedFilesPrimaryOnly.Count; i++)
                for (int p = 0; p < FileManager.duplicatedFilesPrimaryOnly[i].Count; p++)
                    FileManager.duplicatedFilesPrimaryOnly[i][p].Path = new string(FileManager.duplicatedFilesPrimaryOnly[i][p].Path.Skip(primaryDirectory.Length).ToArray());
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
            RestoreFileDialog popup = new RestoreFileDialog(FileManager.storedFiles);
            popup.ShowDialog();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            FileManager.ClearDirectory(tmpDirectory);
        }

        private void SortAlphabeticallyButtonClick(object sender, RoutedEventArgs e) {
            sortBySize = false;
            FileManager.SortAlphabetically();
            duplicatedFilesListView.Items.Refresh();
        }

        private void SortBySizeButtonClick(object sender, RoutedEventArgs e) {
            sortBySize = true;
            if (showBasePaths)
                FileManager.SortBySize();
            else
                FileManager.SortBySize(primaryDirectory);
            duplicatedFilesListView?.Items.Refresh();
        }

        private void SortAlphabeticallyPrimaryOnlyButtonClick(object sender, RoutedEventArgs e) {
            sortBySizePrimaryOnly = false;
            FileManager.SortAlphabeticallyPrimaryOnly();
            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
        }

        private void SortBySizePrimaryOnlyButtonClick(object sender, RoutedEventArgs e) {
            sortBySizePrimaryOnly = true;
            if (showBasePaths)
                FileManager.SortBySizePrimaryOnly();
            else
                FileManager.SortBySizePrimaryOnly(primaryDirectory);
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
            stateTextBlock.Text = "Ready";
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
            stateTextBlock.Text = "Ready";
        }

        private void RemoveAllEmptyDirectoriesPrimary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + primaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.emptyDirectoriesPrimary.Count);
            LockGUI();
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllEmptyDirectoriesPrimary();
                else
                    FileManager.RemoveAllEmptyDirectoriesPrimary(primaryDirectory);
                Dispatcher.Invoke(() => {
                    emptyDirectoriesPrimaryListView.Items.Refresh();
                    emptyDirectoriesPrimaryOnlyListView.Items.Refresh();
                    FinishProgress("Done");
                    UnlockGUI();
                });
            }).Start();
        }

        private void RemoveAllEmptyDirectoriesSecondary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + secondaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.emptyDirectoriesSecondary.Count);
            LockGUI();
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllEmptyDirectoriesSecondary();
                else
                    FileManager.RemoveAllEmptyDirectoriesSecondary(secondaryDirectory);
                Dispatcher.Invoke(() => {
                    emptyDirectoriesSecondaryListView.Items.Refresh();
                    FinishProgress("Done");
                    UnlockGUI();
                });
            }).Start();
        }

        private void RemoveAllEmptyFilesPrimary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + primaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.emptyFilesPrimary.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllEmptyFilesPrimary();
                else
                    FileManager.RemoveAllEmptyFilesPrimary(primaryDirectory);
                Dispatcher.Invoke(() => {
                    emptyFilesPrimaryListView.Items.Refresh();
                    emptyFilesPrimaryOnlyListView.Items.Refresh();
                    FinishProgress("Done");
                    UnlockGUI();
                });
            }).Start();
        }

        private void RemoveAllEmptyFilesSecondary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + secondaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.emptyFilesSecondary.Count);
            LockGUI();
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllEmptyFilesSecondary();
                else
                    FileManager.RemoveAllEmptyFilesSecondary(secondaryDirectory);
                Dispatcher.Invoke(() => {
                    emptyFilesSecondaryListView.Items.Refresh();
                    FinishProgress("Done");
                    UnlockGUI();
                });
            }).Start();
        }

        private void RemoveAllPrimary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + primaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.duplicatedFiles.Count);
            LockGUI();
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllPrimary();
                else
                    FileManager.RemoveAllPrimary(primaryDirectory);
                Dispatcher.Invoke(() => {
                    duplicatedFilesListView.Items.Refresh();
                    FinishProgress("Done");
                    UnlockGUI();
                });
            }).Start();
        }

        private void RemoveAllSecondary(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Are you sure you want to delete all files from " + secondaryDirectory + "? Backup files will not be stored.", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            InitProgress("Removing files...");
            Utility.SetProgress(0, FileManager.duplicatedFiles.Count);
            LockGUI();
            new Thread(() => {
                if (showBasePaths)
                    FileManager.RemoveAllSecondary();
                else
                    FileManager.RemoveAllSecondary(secondaryDirectory);
                Dispatcher.Invoke(() => {
                    duplicatedFilesListView.Items.Refresh();
                    FinishProgress("Done");
                    UnlockGUI();
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

        private void FinishProgress(string state) {
            progressBar.Visibility = Visibility.Hidden;
            progressTextBlock.Visibility = Visibility.Hidden;
            if (abortTask) {
                abortTask = false;
                stateTextBlock.Text = "Aborted";
            }
            else
                stateTextBlock.Text = state;
        }

        private void InitProgress(string state) {
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Visible;
            progressTextBlock.Visibility = Visibility.Visible;
            stateTextBlock.Text = state;
        }

        private void AbortTask(object sender, RoutedEventArgs e) {
            abortTask = true;
            FileManager.AbortTask();
        }
    }
}
