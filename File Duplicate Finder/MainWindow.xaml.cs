//TODO duplicate finding in one directory
//recognize identical subpath
//open in explorer button
// show file size checkbox
// add ignore button

using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace File_Duplicate_Finder {
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
        const int megaByte = 1048576;
        byte[] bufferPrimary = new byte[megaByte];
        byte[] bufferSecondary = new byte[megaByte];
        List<string> primaryFiles = new List<string>();
        List<string> secondaryFiles = new List<string>();
        List<Tuple<List<string>, List<string>>> duplicatedFiles = new List<Tuple<List<string>, List<string>>>();
        List<List<string>> duplicatedFilesPrimaryOnly = new List<List<string>>();
        Dictionary<int, int> duplicateIndexingPrimary = new Dictionary<int, int>();
        Dictionary<int, int> duplicateIndexingSecondary = new Dictionary<int, int>();
        List<string> emptyDirectoriesPrimary = new List<string>();
        List<string> emptyDirectoriesSecondary = new List<string>();
        List<string> emptyFilesPrimary = new List<string>();
        List<string> emptyFilesSecondary = new List<string>();
        List<Tuple<string, string>> storedFiles = new List<Tuple<string, string>>();

        public MainWindow() {
            InitializeComponent();
            Utility.listView = logListView;
            keepFilesCheckBox.IsChecked = true;
            keepFilesCheckBox.IsEnabled = false;
            emptyDirectoriesPrimaryListView.ItemsSource = emptyDirectoriesPrimary;
            emptyFilesPrimaryListView.ItemsSource = emptyFilesPrimary;
            emptyDirectoriesSecondaryListView.ItemsSource = emptyDirectoriesSecondary;
            emptyFilesSecondaryListView.ItemsSource = emptyFilesSecondary;
            duplicatedFilesListView.ItemsSource = duplicatedFiles;
            emptyDirectoriesPrimaryOnlyListView.ItemsSource = emptyDirectoriesPrimary;
            emptyFilesPrimaryOnlyListView.ItemsSource = emptyFilesPrimary;
            duplicatedFilesPrimaryOnlyListView.ItemsSource = duplicatedFilesPrimaryOnly;
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
        }
        private void SecondaryDirectoryDialog(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok) {
                secondaryDirectoryTextBox.Text = dialog.FileName;
                stateTextBlock.Text = "Ready";
            }

            secondaryDirectoryTextBox.Focus();
        }

        private void FindDuplicatedFiles(object sender, RoutedEventArgs e) {
            if (primaryOnly) {
                new Thread(() => {
                    bool error = false;
                    Dispatcher.Invoke(() => {
                        LockGUI();
                        stateTextBlock.Text = "Initializing";
                        progressBar.Value = 0;
                        progressBar.Visibility = Visibility.Visible;
                        primaryFiles.Clear();
                        secondaryFiles.Clear();
                        logListView.Items.Clear();
                        emptyDirectoriesPrimary.Clear();
                        emptyFilesPrimary.Clear();
                        emptyDirectoriesSecondary.Clear();
                        emptyFilesSecondary.Clear();
                        duplicatedFiles.Clear();
                        duplicatedFilesPrimaryOnly.Clear();
                        duplicatedFilesListView.Items.Refresh();
                        duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                        primaryDirectory = primaryDirectoryTextBox.Text;
                        secondaryDirectory = secondaryDirectoryTextBox.Text;
                        DirectoryInfo directoryInfo = new DirectoryInfo(tmpDirectory);
                        foreach (FileInfo file in directoryInfo.GetFiles())
                            file.Delete();
                        foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                            dir.Delete(true);
                        restoreButton.IsEnabled = false;
                    });

                    primaryDirectory = Utility.NormalizePath(primaryDirectory);

                    try {
                        Directory.GetAccessControl(primaryDirectory);
                    }
                    catch (UnauthorizedAccessException) {
                        error = true;
                        Dispatcher.Invoke(() => logListView.Items.Add("Primary directory is not accessible."));
                    }
                    catch {
                        error = true;
                        if (!Directory.Exists(primaryDirectory))
                            Dispatcher.Invoke(() => logListView.Items.Add("Primary directory does not exist."));
                        else
                            Dispatcher.Invoke(() => logListView.Items.Add("Unknown error in primary directory."));
                    }

                    if (error) {
                        Dispatcher.Invoke(() => {
                            stateTextBlock.Text = "Failed";
                            progressBar.Visibility = Visibility.Hidden;
                            UnlockGUI();
                        });
                        return;
                    }

                    Dispatcher.Invoke(() => {
                        stateTextBlock.Text = "Processing directory";
                        progressBar.Value = 2;
                    });
                    ProcessDirectory(primaryDirectory, primaryFiles, emptyDirectoriesPrimary, emptyFilesPrimary, primaryDirectory);
                    Dispatcher.Invoke(() => {
                        emptyDirectoriesPrimaryOnlyListView.Items.Refresh();
                        emptyFilesPrimaryOnlyListView.Items.Refresh();
                        stateTextBlock.Text = "Sorting files by size in primary directory";
                        progressBar.Value = 5;
                    });
                    primaryFiles.Sort((a, b) => new FileInfo(primaryDirectory + a).Length.CompareTo(new FileInfo(primaryDirectory + b).Length));
                    Dispatcher.Invoke(() => {
                        stateTextBlock.Text = "Searching for duplicates...";
                        progressBar.Value = 11;
                    });

                    int index = 0;
                    int otherIndex;
                    long length;
                    long otherLength;
                    int groupIndex;
                    for (; index < primaryFiles.Count - 1; index++) {
                        otherIndex = index + 1;
                        length = new FileInfo(primaryDirectory + primaryFiles[index]).Length;
                        otherLength = new FileInfo(primaryDirectory + primaryFiles[otherIndex]).Length;
                        if (length == otherLength) {
                            do {
                                for (otherIndex = index + 1, groupIndex = -1; otherIndex < primaryFiles.Count && new FileInfo(primaryDirectory + primaryFiles[otherIndex]).Length == length; otherIndex++) {
                                    if(groupIndex != -1) {
                                        if (CompareFileContent(primaryDirectory + primaryFiles[index], primaryDirectory + primaryFiles[otherIndex], length)) {
                                            duplicatedFilesPrimaryOnly[groupIndex].Add(primaryFiles[otherIndex]);
                                            duplicateIndexingPrimary[otherIndex] = groupIndex;
                                        }
                                    }
                                    else if (!duplicateIndexingPrimary.ContainsKey(index)) {
                                        if (CompareFileContent(primaryDirectory + primaryFiles[index], primaryDirectory + primaryFiles[otherIndex], length)) {
                                            groupIndex = duplicatedFilesPrimaryOnly.Count;
                                            duplicateIndexingPrimary[otherIndex] = duplicatedFilesPrimaryOnly.Count;
                                            List<string> list = new List<string>();
                                            list.Add(primaryFiles[index]);
                                            list.Add(primaryFiles[otherIndex]);
                                            duplicatedFilesPrimaryOnly.Add(list);
                                        }
                                    }
                                }
                                index++;
                            } while (index < otherIndex - 1);
                            duplicateIndexingPrimary.Clear();
                            Dispatcher.Invoke(() => duplicatedFilesPrimaryOnlyListView.Items.Refresh());
                        }
                        
                        Dispatcher.Invoke(() => {
                            progressBar.Value = (otherIndex * 89) / primaryFiles.Count + 11;
                        });
                    }

                    if (!sortBySizePrimaryOnly)
                        duplicatedFilesPrimaryOnly.Sort((a, b) => a[0].CompareTo(b[0]));

                    Dispatcher.Invoke(() => {
                        duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                        stateTextBlock.Text = "Done";
                        progressBar.Visibility = Visibility.Hidden;
                        UnlockGUI();
                    });
                }).Start();
            }
            else {
                new Thread(() => {
                    bool error = false;
                    Dispatcher.Invoke(() => {
                        LockGUI();
                        stateTextBlock.Text = "Initializing";
                        progressBar.Value = 0;
                        progressBar.Visibility = Visibility.Visible;
                        primaryFiles.Clear();
                        secondaryFiles.Clear();
                        logListView.Items.Clear();
                        emptyDirectoriesPrimary.Clear();
                        emptyFilesPrimary.Clear();
                        emptyDirectoriesSecondary.Clear();
                        emptyFilesSecondary.Clear();
                        duplicatedFiles.Clear();
                        duplicatedFilesPrimaryOnly.Clear();
                        duplicatedFilesListView.Items.Refresh();
                        duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                        primaryDirectory = primaryDirectoryTextBox.Text;
                        secondaryDirectory = secondaryDirectoryTextBox.Text;
                        DirectoryInfo directoryInfo = new DirectoryInfo(tmpDirectory);
                        foreach (FileInfo file in directoryInfo.GetFiles())
                            file.Delete();
                        foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                            dir.Delete(true);
                        restoreButton.IsEnabled = false;
                    });

                    primaryDirectory = Utility.NormalizePath(primaryDirectory);
                    secondaryDirectory = Utility.NormalizePath(secondaryDirectory);

                    if (primaryDirectory.ToUpperInvariant() == secondaryDirectory.ToUpperInvariant()) {
                        error = true;
                        Dispatcher.Invoke(() => logListView.Items.Add("Primary and secondary directories must be different."));
                    }

                    Utility.CheckDirectories(primaryDirectory, secondaryDirectory, ref error, Dispatcher);

                    if (primaryDirectory.IsSubDirectoryOf(secondaryDirectory)) {
                        error = true;
                        Dispatcher.Invoke(() => logListView.Items.Add("Primary directory cannot be a subdirectory of secondary directory."));
                    }
                    if (secondaryDirectory.IsSubDirectoryOf(primaryDirectory)) {
                        error = true;
                        Dispatcher.Invoke(() => logListView.Items.Add("Secondary directory cannot be a subdirectory of primary directory."));
                    }

                    if (error) {
                        Dispatcher.Invoke(() => {
                            stateTextBlock.Text = "Failed";
                            progressBar.Visibility = Visibility.Hidden;
                            UnlockGUI();
                        });
                        return;
                    }

                    Dispatcher.Invoke(() => {
                        stateTextBlock.Text = "Processing primary directory";
                        progressBar.Value = 1;
                    });
                    ProcessDirectory(primaryDirectory, primaryFiles, emptyDirectoriesPrimary, emptyFilesPrimary, primaryDirectory);
                    Dispatcher.Invoke(() => {
                        emptyDirectoriesPrimaryListView.Items.Refresh();
                        emptyFilesPrimaryListView.Items.Refresh();
                        stateTextBlock.Text = "Processing secondary directory";
                        progressBar.Value = 4;
                    });
                    ProcessDirectory(secondaryDirectory, secondaryFiles, emptyDirectoriesSecondary, emptyFilesSecondary, secondaryDirectory);
                    Dispatcher.Invoke(() => {
                        emptyDirectoriesSecondaryListView.Items.Refresh();
                        emptyFilesSecondaryListView.Items.Refresh();
                        stateTextBlock.Text = "Sorting files by size in primary directory";
                        progressBar.Value = 7;
                    });
                    primaryFiles.Sort((a, b) => new FileInfo(primaryDirectory + a).Length.CompareTo(new FileInfo(primaryDirectory + b).Length));
                    Dispatcher.Invoke(() => {
                        stateTextBlock.Text = "Sorting files by size in secondary directory";
                        progressBar.Value = 9;
                    });
                    secondaryFiles.Sort((a, b) => new FileInfo(secondaryDirectory + a).Length.CompareTo(new FileInfo(secondaryDirectory + b).Length));
                    Dispatcher.Invoke(() => {
                        stateTextBlock.Text = "Searching for duplicates...";
                        progressBar.Value = 11;
                    });

                    int indexPrimary = 0;
                    int indexSecondary = 0;
                    long lengthPrimary, lengthSecondary;
                    while (indexPrimary < primaryFiles.Count && indexSecondary < secondaryFiles.Count) {
                        lengthPrimary = new FileInfo(primaryDirectory + primaryFiles[indexPrimary]).Length;
                        lengthSecondary = new FileInfo(secondaryDirectory + secondaryFiles[indexSecondary]).Length;
                        if (lengthPrimary == lengthSecondary) {
                            int indexSecondaryStart = indexSecondary;
                            long commonLength = lengthPrimary;
                            for (; indexPrimary < primaryFiles.Count && new FileInfo(primaryDirectory + primaryFiles[indexPrimary]).Length == commonLength; indexPrimary++) {
                                for (indexSecondary = indexSecondaryStart; indexSecondary < secondaryFiles.Count && new FileInfo(secondaryDirectory + secondaryFiles[indexSecondary]).Length == commonLength; indexSecondary++) {
                                    if (duplicateIndexingPrimary.ContainsKey(indexPrimary) && duplicateIndexingSecondary.ContainsKey(indexSecondary))
                                        continue; // if it was included in the indexing before
                                    if (CompareFileContent(primaryDirectory + primaryFiles[indexPrimary], secondaryDirectory + secondaryFiles[indexSecondary], commonLength)) {
                                        if (duplicateIndexingPrimary.ContainsKey(indexPrimary)) {
                                            duplicatedFiles[duplicateIndexingPrimary[indexPrimary]].Item2.Add(secondaryFiles[indexSecondary]);
                                            duplicateIndexingSecondary[indexSecondary] = duplicateIndexingPrimary[indexPrimary];
                                        }
                                        else if (duplicateIndexingSecondary.ContainsKey(indexSecondary)) {
                                            duplicatedFiles[duplicateIndexingSecondary[indexSecondary]].Item1.Add(primaryFiles[indexPrimary]);
                                            duplicateIndexingPrimary[indexPrimary] = duplicateIndexingSecondary[indexSecondary];
                                        }
                                        else {
                                            duplicateIndexingPrimary[indexPrimary] = duplicatedFiles.Count;
                                            duplicateIndexingSecondary[indexSecondary] = duplicatedFiles.Count;
                                            Tuple<List<string>, List<string>> tuple = Tuple.Create(new List<string>(), new List<string>());
                                            tuple.Item1.Add(primaryFiles[indexPrimary]);
                                            tuple.Item2.Add(secondaryFiles[indexSecondary]);
                                            duplicatedFiles.Add(tuple);
                                        }
                                    }
                                }
                            }
                            duplicateIndexingPrimary.Clear();
                            duplicateIndexingSecondary.Clear();
                            Dispatcher.Invoke(() => duplicatedFilesListView.Items.Refresh());
                        }
                        else if (lengthPrimary < lengthSecondary)
                            indexPrimary++;
                        else
                            indexSecondary++;

                        if (primaryFiles.Count < secondaryFiles.Count)
                            Dispatcher.Invoke(() => {
                                progressBar.Value = (indexPrimary * 89) / primaryFiles.Count + 11;
                            });
                        else
                            Dispatcher.Invoke(() => {
                                progressBar.Value = (indexSecondary * 89) / secondaryFiles.Count + 11;
                            });
                    }

                    if (!sortBySize)
                        duplicatedFiles.Sort((a, b) => a.Item1[0].CompareTo(b.Item1[0]));

                    Dispatcher.Invoke(() => {
                        duplicatedFilesListView.Items.Refresh();
                        stateTextBlock.Text = "Done";
                        progressBar.Visibility = Visibility.Hidden;
                        UnlockGUI();
                    });
                }).Start();
            }
        }

        // for big files first check random data fragment for equality
        // rather than comparing the whole file greedily
        private bool CompareFileContent(string filePrimary, string fileSecondary, long fileLength) {
            FileStream fileStreamPrimary = File.OpenRead(filePrimary);
            FileStream fileStreamSecondary = File.OpenRead(fileSecondary);
            while (fileLength > 0) {
                if (fileLength >= megaByte) {
                    fileStreamPrimary.Read(bufferPrimary, 0, megaByte);
                    fileStreamSecondary.Read(bufferSecondary, 0, megaByte);
                    if (!Enumerable.SequenceEqual(bufferPrimary, bufferSecondary)) {
                        fileStreamPrimary.Close();
                        fileStreamSecondary.Close();
                        return false;
                    }
                }
                else {
                    fileStreamPrimary.Read(bufferPrimary, 0, (int)fileLength);
                    fileStreamSecondary.Read(bufferSecondary, 0, (int)fileLength);
                    if (!Enumerable.SequenceEqual(Enumerable.Take(bufferPrimary, (int)fileLength), Enumerable.Take(bufferSecondary, (int)fileLength))) {
                        fileStreamPrimary.Close();
                        fileStreamSecondary.Close();
                        return false;
                    }
                }
                fileLength -= megaByte;
            }
            fileStreamPrimary.Close();
            fileStreamSecondary.Close();
            return true;
        }

        private void LockGUI() {
            removeByPathAndContentCheckBox.IsEnabled = false;
            removeByContentCheckBox.IsEnabled = false;
            removeEmptyDirectoriesFromPrimaryCheckBox.IsEnabled = false;
            removeEmptyDirectoriesFromSecondaryCheckBox.IsEnabled = false;
            keepFilesCheckBox.IsEnabled = false;
            findButton.IsEnabled = false;
            primaryDirectoryTextBox.IsEnabled = false;
            secondaryDirectoryTextBox.IsEnabled = false;
        }

        private void UnlockGUI() {
            removeByPathAndContentCheckBox.IsEnabled = true;
            removeByContentCheckBox.IsEnabled = true;
            removeEmptyDirectoriesFromPrimaryCheckBox.IsEnabled = true;
            removeEmptyDirectoriesFromSecondaryCheckBox.IsEnabled = true;
            keepFilesCheckBox.IsEnabled = true;
            findButton.IsEnabled = true;
            primaryDirectoryTextBox.IsEnabled = true;
            secondaryDirectoryTextBox.IsEnabled = true;
        }



        void ProcessDirectory(string targetDirectory, List<string> fileList, List<string> emptyDirectories, List<string> emptyFiles, string originalDirectory) {
            string[] files = Directory.GetFiles(targetDirectory);
            if (files.Length == 0) {
                if (Directory.GetDirectories(targetDirectory).Length == 0)
                    emptyDirectories.Add(new string(targetDirectory.Skip(originalDirectory.Length).ToArray()));
            }
            else {
                foreach (var file in files) {
                    if (new FileInfo(file).Length == 0)
                        emptyFiles.Add(new string(file.Skip(originalDirectory.Length).ToArray()));
                    else
                        fileList.Add(new string(file.Skip(originalDirectory.Length).ToArray()));
                }
            }
            // Recurse into subdirectories of this directory.
            foreach (var subdirectory in Directory.GetDirectories(targetDirectory))
                ProcessDirectory(subdirectory, fileList, emptyDirectories, emptyFiles, originalDirectory);
        }

        private void EmptyDirectoriesPrimaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, () => {
                emptyDirectoriesPrimary.Remove(path);
                emptyDirectoriesPrimaryListView.Items.Refresh();
            });
        }

        private void EmptyFilesPrimaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, () => {
                emptyFilesPrimary.Remove(path);
                emptyFilesPrimaryListView.Items.Refresh();
            });
        }

        private void EmptyDirectoriesSecondaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, () => {
                emptyDirectoriesSecondary.Remove(path);
                emptyDirectoriesSecondaryListView.Items.Refresh();
            });
        }

        private void EmptyFilesSecondaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, () => {
                emptyFilesSecondary.Remove(path);
                emptyFilesSecondaryListView.Items.Refresh();
            });
        }

        private void DuplicatedFilesPrimaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, () => {
                for (int i = 0; i < duplicatedFiles.Count; i++) {
                    for (int j = 0; j < duplicatedFiles[i].Item1.Count; j++) {
                        if (duplicatedFiles[i].Item1[j].Equals(path)) {
                            duplicatedFiles[i].Item1.RemoveAt(j);
                            if (duplicatedFiles[i].Item1.Count == 0 && duplicatedFiles[i].Item2.Count == 0)
                                duplicatedFiles.RemoveAt(i);
                            duplicatedFilesListView.Items.Refresh();
                            break;
                        }
                    }
                }
            });
        }

        private void DuplicatedFilesSecondaryRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, () => {
                for (int i = 0; i < duplicatedFiles.Count; i++) {
                    for (int j = 0; j < duplicatedFiles[i].Item2.Count; j++) {
                        if (duplicatedFiles[i].Item2[j].Equals(path)) {
                            duplicatedFiles[i].Item2.RemoveAt(j);
                            if (duplicatedFiles[i].Item1.Count == 0 && duplicatedFiles[i].Item2.Count == 0)
                                duplicatedFiles.RemoveAt(i);
                            duplicatedFilesListView.Items.Refresh();
                            break;
                        }
                    }
                }
            });
        }

        private void DuplicatedFilesPrimaryOnlyRemoveFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            RemoveFile(path, () => {
                for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++) {
                    for (int j = 0; j < duplicatedFilesPrimaryOnly[i].Count; j++) {
                        if (duplicatedFilesPrimaryOnly[i][j].Equals(path)) {
                            duplicatedFilesPrimaryOnly[i].RemoveAt(j);
                            if (duplicatedFilesPrimaryOnly[i].Count == 0)
                                duplicatedFilesPrimaryOnly.RemoveAt(i);
                            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                            break;
                        }
                    }
                }
            });
        }

        private void RemoveFile(string path, Action action) {
            if (!showBasePaths)
                path = primaryDirectory + path;
            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory)) {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                if (backupFiles) {
                    storedFiles.Add(Tuple.Create(path, ""));
                    restoreButton.IsEnabled = true;
                }
                action();
                directoryInfo.Delete();
            }
            else {  // file
                FileInfo fileInfo = new FileInfo(path);
                if (fileInfo.Length < 2 * megaByte) {   // don't copy large files
                    if (backupFiles) {
                        Guid guid = Guid.NewGuid();
                        fileInfo.MoveTo(tmpDirectory + guid.ToString());
                        storedFiles.Add(Tuple.Create(path, guid.ToString()));
                        restoreButton.IsEnabled = true;
                        action();
                    }
                    else {
                        action();
                        fileInfo.Delete();
                    }
                }
                else {
                    if (!askLarge || MessageBox.Show("You will permanently delete file " + path) == MessageBoxResult.OK) {
                        action();
                        fileInfo.Delete();
                    }
                }
            }
        }

        private void ShowBasePathsChecked(object sender, RoutedEventArgs e) {
            showBasePaths = true;
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int p = 0; p < duplicatedFiles[i].Item1.Count; p++)
                    duplicatedFiles[i].Item1[p] = primaryDirectory + duplicatedFiles[i].Item1[p];
                for (int s = 0; s < duplicatedFiles[i].Item2.Count; s++)
                    duplicatedFiles[i].Item2[s] = secondaryDirectory + duplicatedFiles[i].Item2[s];
            }
            duplicatedFilesListView.Items.Refresh();
            for (int i = 0; i < emptyDirectoriesPrimary.Count; i++)
                emptyDirectoriesPrimary[i] = primaryDirectory + emptyDirectoriesPrimary[i];
            emptyDirectoriesPrimaryListView.Items.Refresh();
            for (int i = 0; i < emptyFilesPrimary.Count; i++)
                emptyFilesPrimary[i] = primaryDirectory + emptyFilesPrimary[i];
            emptyFilesPrimaryListView.Items.Refresh();
            for (int i = 0; i < emptyDirectoriesSecondary.Count; i++)
                emptyDirectoriesSecondary[i] = secondaryDirectory + emptyDirectoriesSecondary[i];
            emptyDirectoriesSecondaryListView.Items.Refresh();
            for (int i = 0; i < emptyFilesSecondary.Count; i++)
                emptyFilesSecondary[i] = secondaryDirectory + emptyFilesSecondary[i];
            emptyFilesSecondaryListView.Items.Refresh();

            for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++)
                for (int p = 0; p < duplicatedFilesPrimaryOnly[i].Count; p++)
                    duplicatedFilesPrimaryOnly[i][p] = primaryDirectory + duplicatedFilesPrimaryOnly[i][p];
        }

        private void ShowBasePathsUnchecked(object sender, RoutedEventArgs e) {
            showBasePaths = false;
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int p = 0; p < duplicatedFiles[i].Item1.Count; p++)
                    duplicatedFiles[i].Item1[p] = new string(duplicatedFiles[i].Item1[p].Skip(primaryDirectory.Length).ToArray());
                for (int s = 0; s < duplicatedFiles[i].Item2.Count; s++)
                    duplicatedFiles[i].Item2[s] = new string(duplicatedFiles[i].Item2[s].Skip(secondaryDirectory.Length).ToArray());
            }
            duplicatedFilesListView.Items.Refresh();
            for (int i = 0; i < emptyDirectoriesPrimary.Count; i++)
                emptyDirectoriesPrimary[i] = new string(emptyDirectoriesPrimary[i].Skip(primaryDirectory.Length).ToArray());
            emptyDirectoriesPrimaryListView.Items.Refresh();
            for (int i = 0; i < emptyFilesPrimary.Count; i++)
                emptyFilesPrimary[i] = new string(emptyFilesPrimary[i].Skip(primaryDirectory.Length).ToArray());
            emptyFilesPrimaryListView.Items.Refresh();
            for (int i = 0; i < emptyDirectoriesSecondary.Count; i++)
                emptyDirectoriesSecondary[i] = new string(emptyDirectoriesSecondary[i].Skip(secondaryDirectory.Length).ToArray());
            emptyDirectoriesSecondaryListView.Items.Refresh();
            for (int i = 0; i < emptyFilesSecondary.Count; i++)
                emptyFilesSecondary[i] = new string(emptyFilesSecondary[i].Skip(secondaryDirectory.Length).ToArray());
            emptyFilesSecondaryListView.Items.Refresh();
            
            for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++)
                for (int p = 0; p < duplicatedFilesPrimaryOnly[i].Count; p++)
                    duplicatedFilesPrimaryOnly[i][p] = new string(duplicatedFilesPrimaryOnly[i][p].Skip(primaryDirectory.Length).ToArray());
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
            duplicatedFiles.Sort((a, b) => a.Item1[0].CompareTo(b.Item1[0]));
            duplicatedFilesListView.Items.Refresh();
        }

        private void SortBySize(object sender, RoutedEventArgs e) {
            sortBySize = true;
            if (showBasePaths)
                duplicatedFiles.Sort((a, b) => new FileInfo(a.Item1[0]).Length.CompareTo(new FileInfo(b.Item1[0]).Length));
            else
                duplicatedFiles.Sort((a, b) => new FileInfo(primaryDirectory + a.Item1[0]).Length.CompareTo(new FileInfo(primaryDirectory + b.Item1[0]).Length));
            duplicatedFilesListView?.Items.Refresh();
        }

        private void SortAlphabeticallyPrimaryOnly(object sender, RoutedEventArgs e) {
            sortBySizePrimaryOnly = false;
            duplicatedFilesPrimaryOnly.Sort((a, b) => a[0].CompareTo(b[0]));
            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
        }

        private void SortBySizePrimaryOnly(object sender, RoutedEventArgs e) {
            sortBySizePrimaryOnly = true;
            if (showBasePaths)
                duplicatedFilesPrimaryOnly.Sort((a, b) => new FileInfo(a[0]).Length.CompareTo(new FileInfo(b[0]).Length));
            else
                duplicatedFilesPrimaryOnly.Sort((a, b) => new FileInfo(primaryDirectory + a[0]).Length.CompareTo(new FileInfo(primaryDirectory + b[0]).Length));
            duplicatedFilesPrimaryOnlyListView?.Items.Refresh();
        }

        private void PrimaryOnlyChecked(object sender, RoutedEventArgs e) {
            primaryOnly = true;
            secondaryDirectoryTextBox.IsEnabled = false;
            tabControlBoth.Visibility = Visibility.Collapsed;
            tabControlPrimaryOnly.Visibility = Visibility.Visible;
        }

        private void PrimaryOnlyUnchecked(object sender, RoutedEventArgs e) {
            primaryOnly = false;
            secondaryDirectoryTextBox.IsEnabled = true;
            tabControlBoth.Visibility = Visibility.Visible;
            tabControlPrimaryOnly.Visibility = Visibility.Collapsed;
        }
    }
}
