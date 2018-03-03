//TODO
//recognize identical subpath
// show file size checkbox
// remove/ignore/show in explorer on restore list
//scroll propagate down to main list
//abort task
// clean up hidden checkboxes or add their functionality
// restructurize code
//D:\Zdjęcia\Stare


using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Utility.logTabItem = logTabItem;
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
                        ClearTmpDirectory();
                        storedFiles.Clear();
                        restoreButton.IsEnabled = false;
                    });

                    primaryDirectory = Utility.NormalizePath(primaryDirectory);

                    try {
                        Directory.GetAccessControl(primaryDirectory);
                    }
                    catch (UnauthorizedAccessException) {
                        error = true;
                        Dispatcher.Invoke(() => {
                            logListView.Items.Add("Primary directory is not accessible.");
                            logTabItem.IsSelected = true;
                        });
                    }
                    catch {
                        error = true;
                        if (!Directory.Exists(primaryDirectory)) {
                            Dispatcher.Invoke(() => {
                                logListView.Items.Add("Primary directory does not exist.");
                                logTabItem.IsSelected = true;
                            });
                        }
                        else {
                            Dispatcher.Invoke(() => {
                                logListView.Items.Add("Unknown error in primary directory.");
                                logTabItem.IsSelected = true;
                            });
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

                    Dispatcher.Invoke(() => {
                        stateTextBlock.Text = "Processing directory";
                        progressBar.Value = 2;
                    });
                    List<string> localEmptyDirectories = new List<string>();
                    List<string> localEmptyFiles = new List<string>();
                    ProcessDirectory(primaryDirectory, primaryFiles, localEmptyDirectories, localEmptyFiles, primaryDirectory);
                    Dispatcher.Invoke(() => {
                        emptyDirectoriesPrimary.AddRange(localEmptyDirectories);
                        emptyDirectoriesPrimaryOnlyListView.Items.Refresh();
                        emptyFilesPrimary.AddRange(localEmptyFiles);
                        emptyFilesPrimaryOnlyListView.Items.Refresh();
                        stateTextBlock.Text = "Sorting files by size in primary directory";
                        progressBar.Value = 5;
                        localEmptyDirectories.Clear();
                        localEmptyFiles.Clear();
                    });
                    primaryFiles.Sort((a, b) => new FileInfo(primaryDirectory + a).Length.CompareTo(new FileInfo(primaryDirectory + b).Length));
                    Dispatcher.Invoke(() => {
                        stateTextBlock.Text = "Searching for duplicates...";
                        progressBar.Value = 11;
                        progressTextBlock.Visibility = Visibility.Visible;
                        SetProgress(0, primaryFiles.Count);
                    });

                    int index = 0;
                    int otherIndex;
                    long length;
                    long otherLength;
                    int groupIndex;
                    List<List<string>> localList = new List<List<string>>();
                    for (; index < primaryFiles.Count - 1; index++) {
                        otherIndex = index + 1;
                        length = new FileInfo(primaryDirectory + primaryFiles[index]).Length;
                        otherLength = new FileInfo(primaryDirectory + primaryFiles[otherIndex]).Length;
                        if (length == otherLength) {
                            do {
                                for (otherIndex = index + 1, groupIndex = -1; otherIndex < primaryFiles.Count && new FileInfo(primaryDirectory + primaryFiles[otherIndex]).Length == length; otherIndex++) {
                                    if (groupIndex != -1) {
                                        if (CompareFileContent(primaryDirectory + primaryFiles[index], primaryDirectory + primaryFiles[otherIndex], length)) {
                                            localList[groupIndex].Add(primaryFiles[otherIndex]);
                                            duplicateIndexingPrimary[otherIndex] = groupIndex;
                                        }
                                    }
                                    else if (!duplicateIndexingPrimary.ContainsKey(index)) {
                                        if (CompareFileContent(primaryDirectory + primaryFiles[index], primaryDirectory + primaryFiles[otherIndex], length)) {
                                            groupIndex = localList.Count;
                                            duplicateIndexingPrimary[otherIndex] = localList.Count;
                                            List<string> list = new List<string>();
                                            list.Add(primaryFiles[index]);
                                            list.Add(primaryFiles[otherIndex]);
                                            localList.Add(list);
                                        }
                                    }
                                }
                                index++;
                            } while (index < otherIndex - 1);
                            duplicateIndexingPrimary.Clear();
                            if (showBasePaths)
                                for (int i = 0; i < localList.Count; i++)
                                    for (int j = 0; j < localList[i].Count; j++)
                                        localList[i][j] = primaryDirectory + localList[i][j];
                            Dispatcher.Invoke(() => {
                                duplicatedFilesPrimaryOnly.AddRange(localList);
                                duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                                localList.Clear();
                            });
                        }

                        Dispatcher.Invoke(() => {
                            progressBar.Value = (otherIndex * 89) / primaryFiles.Count + 11;
                            SetProgress(otherIndex, primaryFiles.Count);
                        });
                    }

                    if (!sortBySizePrimaryOnly)
                        duplicatedFilesPrimaryOnly.Sort((a, b) => a[0].CompareTo(b[0]));

                    Dispatcher.Invoke(() => {
                        duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                        stateTextBlock.Text = "Done";
                        progressBar.Visibility = Visibility.Hidden;
                        progressTextBlock.Visibility = Visibility.Hidden;
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
                        ClearTmpDirectory();
                        storedFiles.Clear();
                        restoreButton.IsEnabled = false;
                    });

                    primaryDirectory = Utility.NormalizePath(primaryDirectory);
                    secondaryDirectory = Utility.NormalizePath(secondaryDirectory);

                    if (primaryDirectory.ToUpperInvariant() == secondaryDirectory.ToUpperInvariant()) {
                        error = true;
                        Dispatcher.Invoke(() => {
                            logListView.Items.Add("Primary and secondary directories must be different.");
                            logTabItem.IsSelected = true;
                        });
                    }

                    Utility.CheckDirectories(primaryDirectory, secondaryDirectory, ref error, Dispatcher);

                    if (primaryDirectory.IsSubDirectoryOf(secondaryDirectory)) {
                        error = true;
                        Dispatcher.Invoke(() => {
                            logListView.Items.Add("Primary directory cannot be a subdirectory of secondary directory.");
                            logTabItem.IsSelected = true;
                        });
                    }
                    if (secondaryDirectory.IsSubDirectoryOf(primaryDirectory)) {
                        error = true;
                        Dispatcher.Invoke(() => {
                            logListView.Items.Add("Secondary directory cannot be a subdirectory of primary directory.");
                            logTabItem.IsSelected = true;
                        });
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
                    List<string> localEmptyDirectoriesPrimary = new List<string>();
                    List<string> localEmptyFilesPrimary = new List<string>();
                    ProcessDirectory(primaryDirectory, primaryFiles, localEmptyDirectoriesPrimary, localEmptyFilesPrimary, primaryDirectory);
                    Dispatcher.Invoke(() => {
                        emptyDirectoriesPrimary.AddRange(localEmptyDirectoriesPrimary);
                        emptyDirectoriesPrimaryListView.Items.Refresh();
                        emptyFilesPrimary.AddRange(localEmptyFilesPrimary);
                        emptyFilesPrimaryListView.Items.Refresh();
                        stateTextBlock.Text = "Processing secondary directory";
                        progressBar.Value = 4;
                        localEmptyDirectoriesPrimary.Clear();
                        localEmptyFilesPrimary.Clear();
                    });
                    List<string> localEmptyDirectoriesSecondary = new List<string>();
                    List<string> localEmptyFilesSecondary = new List<string>();
                    ProcessDirectory(secondaryDirectory, secondaryFiles, localEmptyDirectoriesSecondary, localEmptyFilesSecondary, secondaryDirectory);
                    Dispatcher.Invoke(() => {
                        emptyDirectoriesSecondary.AddRange(localEmptyDirectoriesSecondary);
                        emptyDirectoriesSecondaryListView.Items.Refresh();
                        emptyFilesSecondary.AddRange(localEmptyFilesSecondary);
                        emptyFilesSecondaryListView.Items.Refresh();
                        stateTextBlock.Text = "Sorting files by size in primary directory";
                        progressBar.Value = 7;
                        localEmptyDirectoriesSecondary.Clear();
                        localEmptyFilesSecondary.Clear();
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
                        progressTextBlock.Visibility = Visibility.Visible;
                        SetProgress(0, primaryFiles.Count + secondaryFiles.Count);
                    });

                    int indexPrimary = 0;
                    int indexSecondary = 0;
                    long lengthPrimary, lengthSecondary;
                    List<Tuple<List<string>, List<string>>> localList = new List<Tuple<List<string>, List<string>>>();
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
                                            localList[duplicateIndexingPrimary[indexPrimary]].Item2.Add(secondaryFiles[indexSecondary]);
                                            duplicateIndexingSecondary[indexSecondary] = duplicateIndexingPrimary[indexPrimary];
                                        }
                                        else if (duplicateIndexingSecondary.ContainsKey(indexSecondary)) {
                                            localList[duplicateIndexingSecondary[indexSecondary]].Item1.Add(primaryFiles[indexPrimary]);
                                            duplicateIndexingPrimary[indexPrimary] = duplicateIndexingSecondary[indexSecondary];
                                        }
                                        else {
                                            duplicateIndexingPrimary[indexPrimary] = localList.Count;
                                            duplicateIndexingSecondary[indexSecondary] = localList.Count;
                                            Tuple<List<string>, List<string>> tuple = Tuple.Create(new List<string>(), new List<string>());
                                            tuple.Item1.Add(primaryFiles[indexPrimary]);
                                            tuple.Item2.Add(secondaryFiles[indexSecondary]);
                                            localList.Add(tuple);
                                        }
                                    }
                                }
                            }
                            duplicateIndexingPrimary.Clear();
                            duplicateIndexingSecondary.Clear();
                            if (showBasePaths) {
                                for (int i = 0; i < localList.Count; i++) {
                                    for (int j = 0; j < localList[i].Item1.Count; j++)
                                        localList[i].Item1[j] = primaryDirectory + localList[i].Item1[j];
                                    for (int j = 0; j < localList[i].Item2.Count; j++)
                                        localList[i].Item2[j] = secondaryDirectory + localList[i].Item2[j];
                                }
                            }
                            Dispatcher.Invoke(() => {
                                duplicatedFiles.AddRange(localList);
                                duplicatedFilesListView.Items.Refresh();
                                localList.Clear();
                            });
                        }
                        else if (lengthPrimary < lengthSecondary)
                            indexPrimary++;
                        else
                            indexSecondary++;

                        if (primaryFiles.Count < secondaryFiles.Count)
                            Dispatcher.Invoke(() => {
                                progressBar.Value = (indexPrimary * 89) / primaryFiles.Count + 11;
                                SetProgress(indexPrimary + indexSecondary, primaryFiles.Count + secondaryFiles.Count);
                            });
                        else
                            Dispatcher.Invoke(() => {
                                progressBar.Value = (indexSecondary * 89) / secondaryFiles.Count + 11;
                                SetProgress(indexPrimary + indexSecondary, primaryFiles.Count + secondaryFiles.Count);
                            });
                    }

                    if (!sortBySize)
                        duplicatedFiles.Sort((a, b) => a.Item1[0].CompareTo(b.Item1[0]));

                    Dispatcher.Invoke(() => {
                        duplicatedFilesListView.Items.Refresh();
                        stateTextBlock.Text = "Done";
                        progressBar.Visibility = Visibility.Hidden;
                        progressTextBlock.Visibility = Visibility.Hidden;
                        UnlockGUI();
                    });
                }).Start();
            }
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

        private bool CompareFileContent(string filePrimary, string fileSecondary, long fileLength) {
            FileStream fileStreamPrimary;
            FileStream fileStreamSecondary;
            try {
                fileStreamPrimary = File.OpenRead(filePrimary);
            }
            catch (IOException) {
                Dispatcher.Invoke(() => {
                    logListView.Items.Add("Could not access file " + filePrimary + " because it is being used by another process.");
                    logTabItem.IsSelected = true;
                });
                return false;
            }
            try {
                fileStreamSecondary = File.OpenRead(fileSecondary);
            }
            catch (IOException) {
                Dispatcher.Invoke(() => {
                    logListView.Items.Add("Could not access file " + fileSecondary + " because it is being used by another process.");
                    logTabItem.IsSelected = true;
                });
                return false;
            }
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
            primaryDirectoryTextBox.IsEnabled = false;
            secondaryDirectoryTextBox.IsEnabled = false;
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

        private void OpenDirectoryPrimary(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            if (!showBasePaths)
                path = primaryDirectory + path;
            Process.Start(path);
        }

        private void OpenDirectorySecondary(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            if (!showBasePaths)
                path = secondaryDirectory + path;
            Process.Start(path);
        }

        private void OpenFileDirectoryPrimary(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            if (!showBasePaths)
                path = primaryDirectory + path;
            Process.Start("explorer.exe", "/select, \"" + path + "\"");
        }

        private void OpenFileDirectorySecondary(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            if (!showBasePaths)
                path = secondaryDirectory + path;
            Process.Start("explorer.exe", "/select, \"" + path + "\"");
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
            emptyDirectoriesPrimary.Remove(path);
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
            emptyFilesPrimary.Remove(path);
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
            emptyDirectoriesSecondary.Remove(path);
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
            emptyFilesSecondary.Remove(path);
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
                    if (fileInfo.Length < 2 * megaByte) {   // don't move large files
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
                Dispatcher.Invoke(() => {
                    logListView.Items.Add("File " + path + " no longer exists.");
                    logTabItem.IsSelected = true;
                });
            }
            catch (UnauthorizedAccessException) {
                Dispatcher.Invoke(() => {
                    logListView.Items.Add("Access denied for " + path);
                    logTabItem.IsSelected = true;
                });
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
            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
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
            SetProgress(0, emptyDirectoriesPrimary.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                for (int i = 0; i < emptyDirectoriesPrimary.Count; i++) {
                    RemoveFile(emptyDirectoriesPrimary[i], primaryDirectory);
                    Dispatcher.Invoke(() => {
                        progressBar.Value = i * 100f / emptyDirectoriesPrimary.Count;
                        SetProgress(i, emptyDirectoriesPrimary.Count);
                    });
                }
                emptyDirectoriesPrimary.Clear();
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
            SetProgress(0, emptyDirectoriesSecondary.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                for (int i = 0; i < emptyDirectoriesSecondary.Count; i++) {
                    RemoveFile(emptyDirectoriesSecondary[i], secondaryDirectory);
                    Dispatcher.Invoke(() => {
                        progressBar.Value = i * 100f / emptyDirectoriesSecondary.Count;
                        SetProgress(i, emptyDirectoriesSecondary.Count);
                    });
                }
                emptyDirectoriesSecondary.Clear();
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
            SetProgress(0, emptyFilesPrimary.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                for (int i = 0; i < emptyFilesPrimary.Count; i++) {
                    RemoveFile(emptyFilesPrimary[i], primaryDirectory);
                    Dispatcher.Invoke(() => {
                        progressBar.Value = i * 100f / emptyFilesPrimary.Count;
                        SetProgress(i, emptyFilesPrimary.Count);
                    });
                }
                emptyFilesPrimary.Clear();
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
            SetProgress(0, emptyFilesSecondary.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                for (int i = 0; i < emptyFilesSecondary.Count; i++) {
                    RemoveFile(emptyFilesSecondary[i], secondaryDirectory);
                    Dispatcher.Invoke(() => {
                        progressBar.Value = i * 100f / emptyFilesSecondary.Count;
                        SetProgress(i, emptyFilesSecondary.Count);
                    });
                }
                emptyFilesSecondary.Clear();
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
            SetProgress(0, duplicatedFiles.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                for (int i = 0; i < duplicatedFiles.Count;) {
                    for (int j = 0; j < duplicatedFiles[i].Item1.Count; j++)
                        RemoveFile(duplicatedFiles[i].Item1[j], primaryDirectory);
                    if (duplicatedFiles[i].Item2.Count == 0) {
                        duplicatedFiles.RemoveAt(i);
                    }
                    else {
                        duplicatedFiles[i].Item1.Clear();
                        i++;
                    }
                    Dispatcher.Invoke(() => {
                        progressBar.Value = i * 100f / duplicatedFiles.Count;
                        SetProgress(i, duplicatedFiles.Count);
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
            SetProgress(0, duplicatedFiles.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                for (int i = 0; i < duplicatedFiles.Count;) {
                    for (int j = 0; j < duplicatedFiles[i].Item2.Count; j++)
                        RemoveFile(duplicatedFiles[i].Item2[j], secondaryDirectory);
                    if (duplicatedFiles[i].Item1.Count == 0) {
                        duplicatedFiles.RemoveAt(i);
                    }
                    else {
                        duplicatedFiles[i].Item2.Clear();
                        i++;
                    }
                    Dispatcher.Invoke(() => {
                        progressBar.Value = i * 100f / duplicatedFiles.Count;
                        SetProgress(i, duplicatedFiles.Count);
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
            SetProgress(0, duplicatedFiles.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                for (int i = 0; i < duplicatedFiles.Count; i++) {
                    for (int j = 0; j < duplicatedFiles[i].Item1.Count; j++)
                        RemoveFile(duplicatedFilesPrimaryOnly[i][j], primaryDirectory);
                    Dispatcher.Invoke(() => {
                        progressBar.Value = i * 100f / duplicatedFiles.Count;
                        SetProgress(i, duplicatedFiles.Count);
                    });
                }
                duplicatedFilesPrimaryOnly.Clear();
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


        Thread currentTask;
        private void AbortTask(object sender, RoutedEventArgs e) {
            currentTask.Abort();//or interrupt to do something like finally block in try catch

            stateTextBlock.Text = "Aborted";
        }
    }
}
