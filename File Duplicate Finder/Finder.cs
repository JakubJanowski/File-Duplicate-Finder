using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FileDuplicateFinder {
    internal static class Finder {
        public const int megaByte = 1048576;//private
        private static byte[] bufferPrimary = new byte[megaByte];
        private static byte[] bufferSecondary = new byte[megaByte];

        public static List<string> primaryFiles = new List<string>();
        public static List<string> secondaryFiles = new List<string>();
        public static List<Tuple<List<FileEntry>, List<FileEntry>>> duplicatedFiles = new List<Tuple<List<FileEntry>, List<FileEntry>>>();
        public static List<List<FileEntry>> duplicatedFilesPrimaryOnly = new List<List<FileEntry>>();
        public static Dictionary<int, int> duplicateIndexingPrimary = new Dictionary<int, int>();
        public static Dictionary<int, int> duplicateIndexingSecondary = new Dictionary<int, int>();
        public static List<string> emptyDirectoriesPrimary = new List<string>();
        public static List<string> emptyDirectoriesSecondary = new List<string>();
        public static List<string> emptyFilesPrimary = new List<string>();
        public static List<string> emptyFilesSecondary = new List<string>();

        internal static Dispatcher dispatcher;
        internal static TextBlock stateTextBlock;
        internal static ProgressBar progressBar;
        internal static ListView emptyDirectoriesPrimaryOnlyListView;
        internal static ListView emptyFilesPrimaryOnlyListView;
        internal static TextBlock progressTextBlock;
        internal static ListView duplicatedFilesPrimaryOnlyListView;
        internal static ListView emptyDirectoriesPrimaryListView;
        internal static ListView emptyFilesPrimaryListView;
        internal static ListView emptyDirectoriesSecondaryListView;
        internal static ListView emptyFilesSecondaryListView;
        internal static ListView duplicatedFilesListView;

        public static void FindDuplicatedFiles(string directory, bool showBasePaths) {
            dispatcher.Invoke(() => {
                stateTextBlock.Text = "Processing directory";
                progressBar.Value = 2;
            });
            List<string> localEmptyDirectories = new List<string>();
            List<string> localEmptyFiles = new List<string>();
            ProcessDirectory(directory, primaryFiles, localEmptyDirectories, localEmptyFiles, directory);
            dispatcher.Invoke(() => {
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
                        long aLength = GetFileLength(directory + a);
                        if (aLength < 0) {
                            primaryFiles.Remove(a);
                            throw new SortException();
                        }
                        long bLength = GetFileLength(directory + b);
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
                            if (GetFileLength(directory + primaryFiles[i]) < 0) {
                                primaryFiles.RemoveAt(i);
                                i--;
                            }
                        }
                        continue;
                    }
                }
                sorted = true;
            }

            dispatcher.Invoke(() => {
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
                length = GetFileLength(directory + primaryFiles[index]);
                if (length < 0)
                    continue;
                otherLength = GetFileLength(directory + primaryFiles[otherIndex]);
                while (otherLength < 0) {
                    otherIndex++;
                    if (otherIndex == primaryFiles.Count)
                        return; // all files checked
                    otherLength = GetFileLength(directory + primaryFiles[otherIndex]);
                }
                if (length == otherLength) {
                    do {
                        for (otherIndex = index + 1, groupIndex = -1; otherIndex < primaryFiles.Count; otherIndex++) {
                            long tmpLength = GetFileLength(directory + primaryFiles[otherIndex]);
                            if (tmpLength < 0)
                                continue;
                            else if (tmpLength != length)
                                break;

                            if (groupIndex != -1) {
                                if (CompareFileContent(directory + primaryFiles[index], directory + primaryFiles[otherIndex], length)) {
                                    localList[groupIndex].Add(new FileEntry(primaryFiles[otherIndex], Utility.PrettyPrintSize(length)));
                                    duplicateIndexingPrimary[otherIndex] = groupIndex;
                                }
                            }
                            else if (!duplicateIndexingPrimary.ContainsKey(index)) {
                                if (CompareFileContent(directory + primaryFiles[index], directory + primaryFiles[otherIndex], length)) {
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
                                localList[i][j].Path = directory + localList[i][j].Path;
                    dispatcher.Invoke(() => {
                        duplicatedFilesPrimaryOnly.AddRange(localList);
                        duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                        localList.Clear();
                    });
                }

                dispatcher.Invoke(() => {
                    progressBar.Value = (otherIndex * 89) / primaryFiles.Count + 11;
                    SetProgress(otherIndex, primaryFiles.Count);
                });
            }
        }

        public static void FindDuplicatedFiles(string primaryDirectory, string secondaryDirectory, bool showBasePaths) {
            dispatcher.Invoke(() => {
                stateTextBlock.Text = "Processing primary directory";
                progressBar.Value = 1;
            });
            List<string> localEmptyDirectoriesPrimary = new List<string>();
            List<string> localEmptyFilesPrimary = new List<string>();
            ProcessDirectory(primaryDirectory, primaryFiles, localEmptyDirectoriesPrimary, localEmptyFilesPrimary, primaryDirectory);
            dispatcher.Invoke(() => {
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
            dispatcher.Invoke(() => {
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

            dispatcher.Invoke(() => {
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

            dispatcher.Invoke(() => {
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
                    dispatcher.Invoke(() => {
                        progressBar.Value = (indexPrimary * 89) / primaryFiles.Count + 11;
                        SetProgress(indexPrimary + indexSecondary, primaryFiles.Count + secondaryFiles.Count);
                    });
                }
                else {
                    dispatcher.Invoke(() => {
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
                    dispatcher.Invoke(() => {
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
        }

        private static void ProcessDirectory(string targetDirectory, List<string> fileList, List<string> emptyDirectories, List<string> emptyFiles, string originalDirectory) {
            string[] files;
            try {
                files = Directory.GetFiles(targetDirectory);
            }
            catch (UnauthorizedAccessException) {
                Utility.LogFromNonGUIThread("Access to path \"" + targetDirectory + "\" was denied.");
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

        public static long GetFileLength(string path) {//private
            try {
                return new FileInfo(path).Length;
            }
            catch (FileNotFoundException) {
                Utility.LogFromNonGUIThread("Could not find file \"" + path + "\". It has been probably deleted just now.");
                return -1;
            }
        }

        private static bool CompareFileContent(string filePrimary, string fileSecondary, long fileLength) {
            FileStream fileStreamPrimary;
            FileStream fileStreamSecondary;
            try {
                fileStreamPrimary = File.OpenRead(filePrimary);
            }
            catch (IOException) {
                Utility.LogFromNonGUIThread("Could not access file " + filePrimary + " because it is being used by another process.");
                return false;
            }
            try {
                fileStreamSecondary = File.OpenRead(fileSecondary);
            }
            catch (IOException) {
                Utility.LogFromNonGUIThread("Could not access file " + fileSecondary + " because it is being used by another process.");
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

        private static void SetProgress(int done, int outOf) {
            progressTextBlock.Text = done + " / " + outOf;
        }
    }
}
