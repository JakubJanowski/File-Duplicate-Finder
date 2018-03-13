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
// disablegui buttons [...]

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
        private long fileLengthOnError = -1;
        byte[] bufferPrimary = new byte[megaByte];
        byte[] bufferSecondary = new byte[megaByte];
        List<string> primaryFiles = new List<string>();
        List<string> secondaryFiles = new List<string>();
        List<Tuple<List<FileEntry>, List<FileEntry>>> duplicatedFiles = new List<Tuple<List<FileEntry>, List<FileEntry>>>();
        List<List<FileEntry>> duplicatedFilesPrimaryOnly = new List<List<FileEntry>>();
        Dictionary<int, int> duplicateIndexingPrimary = new Dictionary<int, int>();
        Dictionary<int, int> duplicateIndexingSecondary = new Dictionary<int, int>();
        List<string> emptyDirectoriesPrimary = new List<string>();
        List<string> emptyDirectoriesSecondary = new List<string>();
        List<string> emptyFilesPrimary = new List<string>();
        List<string> emptyFilesSecondary = new List<string>();
        List<Tuple<string, string>> storedFiles = new List<Tuple<string, string>>();

        public MainWindow() {
            InitializeComponent();
            Utility.logListView = logListView;
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
            fileLengthOnError = -1;
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
                        stateTextBlock.Text = "Sorting files by size";
                        progressBar.Value = 5;
                        localEmptyDirectories.Clear();
                        localEmptyFiles.Clear();
                    });

                    bool sorted = false;
                    while (!sorted) {
                        try {
                            primaryFiles.Sort((a, b) => {
                                long aLength = GetFileLength(primaryDirectory + a);
                                if (aLength < 0) {
                                    primaryFiles.Remove(a);
                                    throw new SortException();
                                }
                                long bLength = GetFileLength(primaryDirectory + b);
                                if (bLength < 0) {
                                    primaryFiles.Remove(b);
                                    throw new SortException();
                                }
                                return aLength.CompareTo(bLength);
                            });
                        }
                        catch (InvalidOperationException ex) {
                            if (ex.InnerException is SortException) {
                                for (int i = 0; i < primaryFiles.Count; i++) {
                                    if (GetFileLength(primaryDirectory + primaryFiles[i]) < 0) {
                                        primaryFiles.RemoveAt(i);
                                        i--;
                                    }
                                }
                                continue;
                            }
                        }
                        sorted = true;
                    }

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
                    List<List<FileEntry>> localList = new List<List<FileEntry>>();
                    for (; index < primaryFiles.Count - 1; index++) {
                        otherIndex = index + 1;
                        length = GetFileLength(primaryDirectory + primaryFiles[index]);
                        if (length < 0)
                            continue;
                        otherLength = GetFileLength(primaryDirectory + primaryFiles[otherIndex]);
                        while (otherLength < 0) {
                            otherIndex++;
                            if (otherIndex == primaryFiles.Count)
                                goto EndOfSearch; // all files checked -> exit loop
                            otherLength = GetFileLength(primaryDirectory + primaryFiles[otherIndex]);
                        }
                        if (length == otherLength) {
                            do {
                                for (otherIndex = index + 1, groupIndex = -1; otherIndex < primaryFiles.Count; otherIndex++) {
                                    long tmpLength = GetFileLength(primaryDirectory + primaryFiles[otherIndex]);
                                    if (tmpLength < 0)
                                        continue;
                                    else if (tmpLength != length)
                                        break;

                                    if (groupIndex != -1) {
                                        if (CompareFileContent(primaryDirectory + primaryFiles[index], primaryDirectory + primaryFiles[otherIndex], length)) {
                                            localList[groupIndex].Add(new FileEntry(primaryFiles[otherIndex], Utility.PrettyPrintSize(length)));
                                            duplicateIndexingPrimary[otherIndex] = groupIndex;
                                        }
                                    }
                                    else if (!duplicateIndexingPrimary.ContainsKey(index)) {
                                        if (CompareFileContent(primaryDirectory + primaryFiles[index], primaryDirectory + primaryFiles[otherIndex], length)) {
                                            groupIndex = localList.Count;
                                            duplicateIndexingPrimary[otherIndex] = localList.Count;
                                            List<FileEntry> list = new List<FileEntry>();
                                            list.Add(new FileEntry(primaryFiles[index], Utility.PrettyPrintSize(length)));
                                            list.Add(new FileEntry(primaryFiles[otherIndex], Utility.PrettyPrintSize(length)));
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
                                        localList[i][j].Path = primaryDirectory + localList[i][j].Path;
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

                    EndOfSearch:

                    if (!sortBySizePrimaryOnly)
                        duplicatedFilesPrimaryOnly.Sort((a, b) => a[0].Path.CompareTo(b[0].Path));

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
                        Dispatcher.Invoke(() => Utility.Log("Primary and secondary directories must be different."));
                    }

                    Utility.CheckDirectories(primaryDirectory, secondaryDirectory, ref error, Dispatcher);

                    if (primaryDirectory.IsSubDirectoryOf(secondaryDirectory)) {
                        error = true;
                        Dispatcher.Invoke(() => Utility.Log("Primary directory cannot be a subdirectory of secondary directory."));
                    }
                    if (secondaryDirectory.IsSubDirectoryOf(primaryDirectory)) {
                        error = true;
                        Dispatcher.Invoke(() => Utility.Log("Secondary directory cannot be a subdirectory of primary directory."));
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

                    bool sorted = false;
                    while (!sorted) {
                        try {
                            primaryFiles.Sort((a, b) => {
                                long aLength = GetFileLength(primaryDirectory + a);
                                if (aLength < 0) {
                                    primaryFiles.Remove(a);
                                    throw new SortException();
                                }
                                long bLength = GetFileLength(primaryDirectory + b);
                                if (bLength < 0) {
                                    primaryFiles.Remove(b);
                                    throw new SortException();
                                }
                                return aLength.CompareTo(bLength);
                            });
                        }
                        catch (InvalidOperationException ex) {
                            if (ex.InnerException is SortException) {
                                for (int i = 0; i < primaryFiles.Count; i++) {
                                    if (GetFileLength(primaryDirectory + primaryFiles[i]) < 0) {
                                        primaryFiles.RemoveAt(i);
                                        i--;
                                    }
                                }
                                continue;
                            }
                        }
                        sorted = true;
                    }

                    Dispatcher.Invoke(() => {
                        stateTextBlock.Text = "Sorting files by size in secondary directory";
                        progressBar.Value = 9;
                    });

                    sorted = false;
                    while (!sorted) {
                        try {
                            secondaryFiles.Sort((a, b) => {
                                long aLength = GetFileLength(secondaryDirectory + a);
                                if (aLength < 0) {
                                    secondaryFiles.Remove(a);
                                    throw new SortException();
                                }
                                long bLength = GetFileLength(secondaryDirectory + b);
                                if (bLength < 0) {
                                    secondaryFiles.Remove(b);
                                    throw new SortException();
                                }
                                return aLength.CompareTo(bLength);
                            });
                        }
                        catch (InvalidOperationException ex) {
                            if (ex.InnerException is SortException) {
                                for (int i = 0; i < secondaryFiles.Count; i++) {
                                    if (GetFileLength(secondaryDirectory + secondaryFiles[i]) < 0) {
                                        secondaryFiles.RemoveAt(i);
                                        i--;
                                    }
                                }
                                continue;
                            }
                        }
                        sorted = true;
                    }

                    Dispatcher.Invoke(() => {
                        stateTextBlock.Text = "Searching for duplicates...";
                        progressBar.Value = 11;
                        progressTextBlock.Visibility = Visibility.Visible;
                        SetProgress(0, primaryFiles.Count + secondaryFiles.Count);
                    });

                    int indexPrimary = 0;
                    int indexSecondary = 0;
                    long lengthPrimary, lengthSecondary;
                    List<Tuple<List<FileEntry>, List<FileEntry>>> localList = new List<Tuple<List<FileEntry>, List<FileEntry>>>();
                    while (indexPrimary < primaryFiles.Count && indexSecondary < secondaryFiles.Count) {
                        if (primaryFiles.Count < secondaryFiles.Count) {
                            Dispatcher.Invoke(() => {
                                progressBar.Value = (indexPrimary * 89) / primaryFiles.Count + 11;
                                SetProgress(indexPrimary + indexSecondary, primaryFiles.Count + secondaryFiles.Count);
                            });
                        }
                        else {
                            Dispatcher.Invoke(() => {
                                progressBar.Value = (indexSecondary * 89) / secondaryFiles.Count + 11;
                                SetProgress(indexPrimary + indexSecondary, primaryFiles.Count + secondaryFiles.Count);
                            });
                        }

                        lengthPrimary = GetFileLength(primaryDirectory + primaryFiles[indexPrimary]);
                        if (lengthPrimary < 0) {
                            indexPrimary++;
                            continue;
                        }
                        lengthSecondary = GetFileLength(secondaryDirectory + secondaryFiles[indexSecondary]);
                        if (lengthSecondary < 0) {
                            indexSecondary++;
                            continue;
                        }

                        if (lengthPrimary == lengthSecondary) {
                            int indexSecondaryStart = indexSecondary;
                            long commonLength = lengthPrimary;
                            long tmpLength;
                            for (; indexPrimary < primaryFiles.Count; indexPrimary++) {
                                tmpLength = GetFileLength(primaryDirectory + primaryFiles[indexPrimary]);
                                if (tmpLength < 0)
                                    continue;
                                else if (tmpLength != commonLength)
                                    break;

                                for (indexSecondary = indexSecondaryStart; indexSecondary < secondaryFiles.Count; indexSecondary++) {
                                    tmpLength = GetFileLength(secondaryDirectory + secondaryFiles[indexSecondary]);
                                    if (tmpLength < 0)
                                        continue;
                                    else if (tmpLength != commonLength)
                                        break;

                                    if (duplicateIndexingPrimary.ContainsKey(indexPrimary) && duplicateIndexingSecondary.ContainsKey(indexSecondary))
                                        continue; // if it was included in the indexing before
                                    if (CompareFileContent(primaryDirectory + primaryFiles[indexPrimary], secondaryDirectory + secondaryFiles[indexSecondary], commonLength)) {
                                        if (duplicateIndexingPrimary.ContainsKey(indexPrimary)) {
                                            localList[duplicateIndexingPrimary[indexPrimary]].Item2.Add(new FileEntry(secondaryFiles[indexSecondary], Utility.PrettyPrintSize(commonLength)));
                                            duplicateIndexingSecondary[indexSecondary] = duplicateIndexingPrimary[indexPrimary];
                                        }
                                        else if (duplicateIndexingSecondary.ContainsKey(indexSecondary)) {
                                            localList[duplicateIndexingSecondary[indexSecondary]].Item1.Add(new FileEntry(primaryFiles[indexPrimary], Utility.PrettyPrintSize(commonLength)));
                                            duplicateIndexingPrimary[indexPrimary] = duplicateIndexingSecondary[indexSecondary];
                                        }
                                        else {
                                            duplicateIndexingPrimary[indexPrimary] = localList.Count;
                                            duplicateIndexingSecondary[indexSecondary] = localList.Count;
                                            Tuple<List<FileEntry>, List<FileEntry>> tuple = Tuple.Create(new List<FileEntry>(), new List<FileEntry>());
                                            tuple.Item1.Add(new FileEntry(primaryFiles[indexPrimary], Utility.PrettyPrintSize(commonLength)));
                                            tuple.Item2.Add(new FileEntry(secondaryFiles[indexSecondary], Utility.PrettyPrintSize(commonLength)));
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
                                        localList[i].Item1[j].Path = primaryDirectory + localList[i].Item1[j].Path;
                                    for (int j = 0; j < localList[i].Item2.Count; j++)
                                        localList[i].Item2[j].Path = secondaryDirectory + localList[i].Item2[j].Path;
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
                    }

                    if (!sortBySize)
                        duplicatedFiles.Sort((a, b) => a.Item1[0].Path.CompareTo(b.Item1[0].Path));

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

        private long GetFileLength(string path) {
            try {
                return new FileInfo(path).Length;
            }
            catch (FileNotFoundException) {
                Dispatcher.Invoke(() => Utility.Log("Could not find file \"" + path + "\". It has been probably deleted just now."));
                fileLengthOnError--;
                return fileLengthOnError;
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
                Dispatcher.Invoke(() => Utility.Log("Could not access file " + filePrimary + " because it is being used by another process."));
                return false;
            }
            try {
                fileStreamSecondary = File.OpenRead(fileSecondary);
            }
            catch (IOException) {
                Dispatcher.Invoke(() => Utility.Log("Could not access file " + fileSecondary + " because it is being used by another process."));
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
            string[] files;
            try {
                files = Directory.GetFiles(targetDirectory);
            }
            catch (UnauthorizedAccessException) {
                Dispatcher.Invoke(() => Utility.Log("Access to path \"" + targetDirectory + "\" was denied."));
                return;
            }
            if (files.Length == 0) {
                if (Directory.GetDirectories(targetDirectory).Length == 0)
                    emptyDirectories.Add(new string(targetDirectory.Skip(originalDirectory.Length).ToArray()));
            }
            else {
                foreach (var file in files) {
                    long fileLength = GetFileLength(file);
                    if (fileLength == 0)
                        emptyFiles.Add(new string(file.Skip(originalDirectory.Length).ToArray()));
                    else if (fileLength > 0)
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
            try {
                Process.Start(path);
            }
            catch (Win32Exception) {
                emptyDirectoriesPrimary.Remove(((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text);
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
                emptyDirectoriesSecondary.Remove(((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text);
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
                if (!emptyFilesPrimary.Remove(path)) {
                    for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++) {
                        int index = duplicatedFilesPrimaryOnly[i].FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            duplicatedFilesPrimaryOnly[i].RemoveAt(index);
                            if (duplicatedFilesPrimaryOnly[i].Count <= 1)
                                duplicatedFilesPrimaryOnly.RemoveAt(i);
                            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                            goto OpenFileDirectoryPrimaryFound;
                        }
                    }
                    for (int i = 0; i < duplicatedFiles.Count; i++) {
                        int index = duplicatedFiles[i].Item1.FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            duplicatedFiles[i].Item1.RemoveAt(index);
                            if (duplicatedFiles[i].Item2.Count == 0 && duplicatedFiles[i].Item1.Count <= 1)
                                duplicatedFiles.RemoveAt(i);
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
                if (!emptyFilesSecondary.Remove(path)) {
                    for (int i = 0; i < duplicatedFiles.Count; i++) {
                        int index = duplicatedFiles[i].Item2.FindIndex(f => f.Path == path);
                        if (index >= 0) {
                            duplicatedFiles[i].Item2.RemoveAt(index);
                            if (duplicatedFiles[i].Item2.Count == 0 && duplicatedFiles[i].Item1.Count <= 1)
                                duplicatedFiles.RemoveAt(i);
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
                    if (duplicatedFiles[i].Item1[j].Path.Equals(path)) {
                        duplicatedFiles[i].Item1.RemoveAt(j);
                        if (duplicatedFiles[i].Item1.Count + duplicatedFiles[i].Item2.Count <= 1)
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
                    if (duplicatedFiles[i].Item2[j].Path.Equals(path)) {
                        duplicatedFiles[i].Item2.RemoveAt(j);
                        if (duplicatedFiles[i].Item1.Count + duplicatedFiles[i].Item2.Count <= 1)
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
                    if (duplicatedFilesPrimaryOnly[i][j].Path.Equals(path)) {
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
                Dispatcher.Invoke(() => Utility.Log("File " + path + " no longer exists."));
            }
            catch (UnauthorizedAccessException) {
                Dispatcher.Invoke(() => Utility.Log("Access denied for " + path));
            }
        }

        private void ShowBasePathsChecked(object sender, RoutedEventArgs e) {
            showBasePaths = true;
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int p = 0; p < duplicatedFiles[i].Item1.Count; p++)
                    duplicatedFiles[i].Item1[p].Path = primaryDirectory + duplicatedFiles[i].Item1[p].Path;
                for (int s = 0; s < duplicatedFiles[i].Item2.Count; s++)
                    duplicatedFiles[i].Item2[s].Path = secondaryDirectory + duplicatedFiles[i].Item2[s].Path;
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
                    duplicatedFilesPrimaryOnly[i][p].Path = primaryDirectory + duplicatedFilesPrimaryOnly[i][p].Path;
            duplicatedFilesPrimaryOnlyListView.Items.Refresh();
        }

        private void ShowBasePathsUnchecked(object sender, RoutedEventArgs e) {
            showBasePaths = false;
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int p = 0; p < duplicatedFiles[i].Item1.Count; p++)
                    duplicatedFiles[i].Item1[p].Path = new string(duplicatedFiles[i].Item1[p].Path.Skip(primaryDirectory.Length).ToArray());
                for (int s = 0; s < duplicatedFiles[i].Item2.Count; s++)
                    duplicatedFiles[i].Item2[s].Path = new string(duplicatedFiles[i].Item2[s].Path.Skip(secondaryDirectory.Length).ToArray());
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
                    duplicatedFilesPrimaryOnly[i][p].Path = new string(duplicatedFilesPrimaryOnly[i][p].Path.Skip(primaryDirectory.Length).ToArray());
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
            duplicatedFiles.Sort((a, b) => a.Item1[0].Path.CompareTo(b.Item1[0].Path));
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
                    duplicatedFiles.Sort((a, b) => {
                        long aLength = GetFileLength(baseDirectory + a.Item1[0].Path);
                        if (aLength < 0) {
                            a.Item1.RemoveAt(0);
                            throw new SortException();
                        }
                        long bLength = GetFileLength(baseDirectory + b.Item1[0].Path);
                        if (bLength < 0) {
                            b.Item1.RemoveAt(0);
                            throw new SortException();
                        }
                        return aLength.CompareTo(bLength);
                    });
                }
                catch (InvalidOperationException ex) {
                    if (ex.InnerException is SortException) {
                        for (int i = 0; i < duplicatedFiles.Count; i++) {
                            for (int j = 0; j < duplicatedFiles[i].Item1.Count; j++) {
                                if (GetFileLength(baseDirectory + duplicatedFiles[i].Item1[j].Path) < 0) {
                                    duplicatedFiles[i].Item1.RemoveAt(j);
                                    j--;
                                }
                            }
                            for (int j = 0; j < duplicatedFiles[i].Item2.Count; j++) {
                                if (GetFileLength(baseDirectory + duplicatedFiles[i].Item2[j].Path) < 0) {
                                    duplicatedFiles[i].Item2.RemoveAt(j);
                                    j--;
                                }
                            }
                            if (duplicatedFiles[i].Item1.Count + duplicatedFiles[i].Item2.Count <= 1) {
                                duplicatedFiles.RemoveAt(i);
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
            duplicatedFilesPrimaryOnly.Sort((a, b) => a[0].Path.CompareTo(b[0].Path));
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
                    duplicatedFilesPrimaryOnly.Sort((a, b) => {
                        long aLength = GetFileLength(baseDirectory + a[0].Path);
                        if (aLength < 0) {
                            a.RemoveAt(0);
                            if (a.Count == 0)
                                duplicatedFilesPrimaryOnly.Remove(a);
                            throw new SortException();
                        }
                        long bLength = GetFileLength(baseDirectory + b[0].Path);
                        if (bLength < 0) {
                            b.RemoveAt(0);
                            if (b.Count == 0)
                                duplicatedFilesPrimaryOnly.Remove(b);
                            throw new SortException();
                        }
                        return aLength.CompareTo(bLength);
                    });
                }
                catch (InvalidOperationException ex) {
                    if (ex.InnerException is SortException) {
                        for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++) {
                            for (int j = 0; j < duplicatedFilesPrimaryOnly[i].Count; j++) {
                                if (GetFileLength(baseDirectory + duplicatedFilesPrimaryOnly[i][j].Path) < 0) {
                                    duplicatedFilesPrimaryOnly[i].RemoveAt(j);
                                    j--;
                                }
                            }
                            if (duplicatedFilesPrimaryOnly[i].Count <= 1) {
                                duplicatedFilesPrimaryOnly.RemoveAt(i);
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
                int count = duplicatedFiles.Count;
                for (int i = 0; i < duplicatedFiles.Count;) {
                    for (int j = 0; j < duplicatedFiles[i].Item1.Count; j++)
                        RemoveFile(duplicatedFiles[i].Item1[j].Path, primaryDirectory);
                    if (duplicatedFiles[i].Item2.Count <= 1) {
                        duplicatedFiles.RemoveAt(i);
                    }
                    else {
                        duplicatedFiles[i].Item1.Clear();
                        i++;
                    }
                    Dispatcher.Invoke(() => {
                        int progress = i + count - duplicatedFiles.Count;
                        progressBar.Value = progress * 100f / count;
                        SetProgress(progress, duplicatedFiles.Count);
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
                int count = duplicatedFiles.Count;
                for (int i = 0; i < duplicatedFiles.Count;) {
                    for (int j = 0; j < duplicatedFiles[i].Item2.Count; j++)
                        RemoveFile(duplicatedFiles[i].Item2[j].Path, secondaryDirectory);
                    if (duplicatedFiles[i].Item1.Count <= 1) {
                        duplicatedFiles.RemoveAt(i);
                    }
                    else {
                        duplicatedFiles[i].Item2.Clear();
                        i++;
                    }
                    Dispatcher.Invoke(() => {
                        int progress = i + count - duplicatedFiles.Count;
                        progressBar.Value = progress * 100f / count;
                        SetProgress(progress, duplicatedFiles.Count);
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
            SetProgress(0, duplicatedFilesPrimaryOnly.Count);
            LockGUI();
            stateTextBlock.Text = "Removing files...";
            new Thread(() => {
                int count = duplicatedFilesPrimaryOnly.Count;
                for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++) {
                    for (int j = 0; j < duplicatedFilesPrimaryOnly[i].Count; j++)
                        RemoveFile(duplicatedFilesPrimaryOnly[i][j].Path, primaryDirectory);
                    Dispatcher.Invoke(() => {
                        int progress = i + count - duplicatedFilesPrimaryOnly.Count;
                        progressBar.Value = progress * 100f / count;
                        SetProgress(progress, duplicatedFilesPrimaryOnly.Count);
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
