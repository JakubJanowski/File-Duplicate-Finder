using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FileDuplicateFinder {
    internal static class FileManager {
        public static string tmpDirectory;
        private const int megaByte = 1048576;
        private volatile static bool abortTask = false;
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
        public static List<Tuple<string, string>> storedFiles = new List<Tuple<string, string>>();

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

        public static void AbortTask() {
            abortTask = true;
        }

        public static void FindDuplicatedFiles(string directory, bool showBasePaths) {
            dispatcher.Invoke(() => {
                stateTextBlock.Text = "Processing directory";
                progressBar.Value = 2;
            });
            List<string> localEmptyDirectories = new List<string>();
            List<string> localEmptyFiles = new List<string>();
            ProcessDirectory(directory, primaryFiles, localEmptyDirectories, localEmptyFiles, directory);
            if (abortTask) {
                abortTask = false;
                return;
            }
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
                        if (abortTask)
                            throw new TaskAbortedException();
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
                catch (TaskAbortedException) {
                    abortTask = false;
                    return;
                }
                sorted = true;
            }

            dispatcher.Invoke(() => {
                stateTextBlock.Text = "Searching for duplicates...";
                progressBar.Value = 11;
                progressTextBlock.Visibility = Visibility.Visible;
                Utility.SetProgress(0, primaryFiles.Count);
            });

            int index = 0;
            int otherIndex;
            long length;
            long otherLength;
            int groupIndex;
            List<List<FileEntry>> localList = new List<List<FileEntry>>();
            List<List<FileEntry>> localListCopy;
            for (; index < primaryFiles.Count - 1; index++) {
                if (abortTask) {
                    abortTask = false;
                    return;
                }
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
                            if (abortTask) {
                                AbortFindDuplicateFilesOneDirectory(showBasePaths, localList, directory);
                                abortTask = false;
                                return;
                            }
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
                    localListCopy = new List<List<FileEntry>>(localList);
                    dispatcher.Invoke(() => {
                        duplicatedFilesPrimaryOnly.AddRange(localListCopy);
                        duplicatedFilesPrimaryOnlyListView.Items.Refresh();
                    });
                    localList.Clear();
                }

                dispatcher.Invoke(() => {
                    progressBar.Value = (otherIndex * 89) / primaryFiles.Count + 11;
                    Utility.SetProgress(otherIndex, primaryFiles.Count);
                });
            }
        }
        
        private static void AbortFindDuplicateFilesOneDirectory(bool showBasePaths, List<List<FileEntry>> localList, string directory) {
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

        public static void FindDuplicatedFiles(string primaryDirectory, string secondaryDirectory, bool showBasePaths) {
            dispatcher.Invoke(() => {
                stateTextBlock.Text = "Processing primary directory";
                progressBar.Value = 1;
            });
            List<string> localEmptyDirectoriesPrimary = new List<string>();
            List<string> localEmptyFilesPrimary = new List<string>();
            ProcessDirectory(primaryDirectory, primaryFiles, localEmptyDirectoriesPrimary, localEmptyFilesPrimary, primaryDirectory);
            if (abortTask) {
                abortTask = false;
                return;
            }
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
            if (abortTask) {
                abortTask = false;
                return;
            }
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
                        if (abortTask) {
                            throw new TaskAbortedException();
                        }
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
                catch (TaskAbortedException) {
                    abortTask = false;
                    return;
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
                        if (abortTask) {
                            throw new TaskAbortedException();
                        }
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
                catch (TaskAbortedException) {
                    abortTask = false;
                    return;
                }
                sorted = true;
            }

            dispatcher.Invoke(() => {
                stateTextBlock.Text = "Searching for duplicates...";
                progressBar.Value = 11;
                progressTextBlock.Visibility = Visibility.Visible;
                Utility.SetProgress(0, primaryFiles.Count + secondaryFiles.Count);
            });

            int indexPrimary = 0;
            int indexSecondary = 0;
            long lengthPrimary, lengthSecondary;
            List<Tuple<List<FileEntry>, List<FileEntry>>> localList = new List<Tuple<List<FileEntry>, List<FileEntry>>>();
            List<Tuple<List<FileEntry>, List<FileEntry>>> localListCopy;
            while (indexPrimary < primaryFiles.Count && indexSecondary < secondaryFiles.Count) {
                if (abortTask) {
                    abortTask = false;
                    return;
                }
                if (primaryFiles.Count < secondaryFiles.Count) {
                    dispatcher.Invoke(() => {
                        progressBar.Value = (indexPrimary * 89) / primaryFiles.Count + 11;
                        Utility.SetProgress(indexPrimary + indexSecondary, primaryFiles.Count + secondaryFiles.Count);
                    });
                }
                else {
                    dispatcher.Invoke(() => {
                        progressBar.Value = (indexSecondary * 89) / secondaryFiles.Count + 11;
                        Utility.SetProgress(indexPrimary + indexSecondary, primaryFiles.Count + secondaryFiles.Count);
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
                        if (abortTask) {
                            AbortFindDuplicateFiles(showBasePaths, localList, primaryDirectory, secondaryDirectory);
                            abortTask = false;
                            return;
                        }
                        tmpLength = GetFileLength(primaryDirectory + primaryFiles[indexPrimary]);
                        if (tmpLength < 0)
                            continue;
                        else if (tmpLength != commonLength)
                            break;

                        for (indexSecondary = indexSecondaryStart; indexSecondary < secondaryFiles.Count; indexSecondary++) {
                            if (abortTask) {
                                AbortFindDuplicateFiles(showBasePaths, localList, primaryDirectory, secondaryDirectory);
                                abortTask = false;
                                return;
                            }
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
                    localListCopy = new List<Tuple<List<FileEntry>, List<FileEntry>>>(localList);
                    dispatcher.Invoke(() => {
                        duplicatedFiles.AddRange(localListCopy);
                        duplicatedFilesListView.Items.Refresh();
                    });
                    localList.Clear();
                }
                else if (lengthPrimary < lengthSecondary)
                    indexPrimary++;
                else
                    indexSecondary++;
            }
        }

        private static void AbortFindDuplicateFiles(bool showBasePaths, List<Tuple<List<FileEntry>, List<FileEntry>>> localList, string primaryDirectory, string secondaryDirectory) {
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

        private static void ProcessDirectory(string targetDirectory, List<string> fileList, List<string> emptyDirectories, List<string> emptyFiles, string originalDirectory) {
            if (abortTask)
                return;
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
                if (abortTask) {
                    fileStreamPrimary.Close();
                    fileStreamSecondary.Close();
                    return false;
                }
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

        internal static void EmptyDirectoriesPrimaryIgnoreFile(string path) {
            emptyDirectoriesPrimary.Remove(path);
            emptyDirectoriesPrimaryListView.Items.Refresh();
        }

        internal static void EmptyFilesPrimaryIgnoreFile(string path) {
            emptyFilesPrimary.Remove(path);
            emptyFilesPrimaryListView.Items.Refresh();
        }

        internal static void EmptyDirectoriesSecondaryIgnoreFile(string path) {
            emptyDirectoriesSecondary.Remove(path);
            emptyDirectoriesSecondaryListView.Items.Refresh();
        }

        internal static void EmptyFilesSecondaryIgnoreFile(string path) {
            emptyFilesSecondary.Remove(path);
            emptyFilesSecondaryListView.Items.Refresh();
        }

        internal static void DuplicatedFilesPrimaryIgnoreFile(string path) {
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int j = 0; j < duplicatedFiles[i].Item1.Count; j++) {
                    if (abortTask) {
                        abortTask = false;
                        return;
                    }
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

        internal static void DuplicatedFilesSecondaryIgnoreFile(string path) {
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int j = 0; j < duplicatedFiles[i].Item2.Count; j++) {
                    if (abortTask) {
                        abortTask = false;
                        return;
                    }
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

        internal static void DuplicatedFilesPrimaryOnlyIgnoreFile(string path) {
            for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++) {
                for (int j = 0; j < duplicatedFilesPrimaryOnly[i].Count; j++) {
                    if (abortTask) {
                        abortTask = false;
                        return;
                    }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="baseDirectory"></param>
        /// <returns><see langword="true"/> if restoring the file will be possible, <see langword="false"/> otherwise</returns>
        internal static bool RemoveFile(string path, string baseDirectory, bool backupFiles, bool askLarge) {
            bool canRestoreFiles = false;
            try {
                if (File.GetAttributes(path).HasFlag(FileAttributes.Directory)) {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    if (backupFiles) {
                        storedFiles.Add(Tuple.Create(path, ""));
                        canRestoreFiles = true;
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
                            canRestoreFiles = true;
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
                Utility.LogFromNonGUIThread("File " + path + " no longer exists.");
            }
            catch (UnauthorizedAccessException) {
                Utility.LogFromNonGUIThread("Access denied for " + path);
            }

            return canRestoreFiles;
        }

        internal static void ClearDirectory(string path) {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            foreach (FileInfo file in directoryInfo.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                dir.Delete(true);
        }


        // Sorting functions

        internal static void SortAlphabetically() {
            duplicatedFiles.Sort((a, b) => a.Item1[0].Path.CompareTo(b.Item1[0].Path));
        }

        internal static void SortBySize(string baseDirectory = "") {
            bool sorted = false;
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
        }

        internal static void SortAlphabeticallyPrimaryOnly() {
            duplicatedFilesPrimaryOnly.Sort((a, b) => a[0].Path.CompareTo(b[0].Path));
        }

        internal static void SortBySizePrimaryOnly(string baseDirectory = "") {
            bool sorted = false;
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
        }

        internal static void RemoveAllEmptyDirectoriesPrimary(string primaryDirectory) {
            for (int i = 0; i < emptyDirectoriesPrimary.Count; i++) {
                if (abortTask) {
                    AbortRemoveAllEmptyDirectoriesPrimary(i);
                    abortTask = false;
                    return;
                }
                RemoveFile(emptyDirectoriesPrimary[i], primaryDirectory, false, false);
                dispatcher.Invoke(() => {
                    progressBar.Value = i * 100f / emptyDirectoriesPrimary.Count;
                    Utility.SetProgress(i, emptyDirectoriesPrimary.Count);
                });
            }
            emptyDirectoriesPrimary.Clear();
        }

        private static void AbortRemoveAllEmptyDirectoriesPrimary(int i) {
            emptyDirectoriesPrimary.RemoveRange(0, i);
        }

        internal static void RemoveAllEmptyDirectoriesSecondary(string secondaryDirectory) {
            for (int i = 0; i < emptyDirectoriesSecondary.Count; i++) {
                if (abortTask) {
                    AbortRemoveAllEmptyDirectoriesSecondary(i);
                    abortTask = false;
                    return;
                }
                RemoveFile(emptyDirectoriesSecondary[i], secondaryDirectory, false, false);
                dispatcher.Invoke(() => {
                    progressBar.Value = i * 100f / emptyDirectoriesSecondary.Count;
                    Utility.SetProgress(i, emptyDirectoriesSecondary.Count);
                });
            }
            emptyDirectoriesSecondary.Clear();
        }

        private static void AbortRemoveAllEmptyDirectoriesSecondary(int i) {
            emptyDirectoriesSecondary.RemoveRange(0, i);
        }

        internal static void RemoveAllEmptyFilesPrimary(string primaryDirectory) {
            for (int i = 0; i < emptyFilesPrimary.Count; i++) {
                if (abortTask) {
                    AbortRemoveAllEmptyFilesPrimary(i);
                    abortTask = false;
                    return;
                }
                RemoveFile(emptyFilesPrimary[i], primaryDirectory, false, false);
                dispatcher.Invoke(() => {
                    progressBar.Value = i * 100f / emptyFilesPrimary.Count;
                    Utility.SetProgress(i, emptyFilesPrimary.Count);
                });
            }
            emptyFilesPrimary.Clear();
        }

        private static void AbortRemoveAllEmptyFilesPrimary(int i) {
            emptyFilesPrimary.RemoveRange(0, i);
        }

        internal static void RemoveAllEmptyFilesSecondary(string secondaryDirectory) {
            for (int i = 0; i < emptyFilesSecondary.Count; i++) {
                if (abortTask) {
                    AbortRemoveAllEmptyFilesSecondary(i);
                    abortTask = false;
                    return;
                }
                RemoveFile(emptyFilesSecondary[i], secondaryDirectory, false, false);
                dispatcher.Invoke(() => {
                    progressBar.Value = i * 100f / emptyFilesSecondary.Count;
                    Utility.SetProgress(i, emptyFilesSecondary.Count);
                });
            }
            emptyFilesSecondary.Clear();
        }

        private static void AbortRemoveAllEmptyFilesSecondary(int i) {
            emptyFilesSecondary.RemoveRange(0, i);
        }

        internal static void RemoveAllPrimary(string primaryDirectory) {
            int count = duplicatedFiles.Count;
            for (int i = 0; i < duplicatedFiles.Count;) {
                for (int j = 0; j < duplicatedFiles[i].Item1.Count; j++) {
                    if (abortTask) {
                        AbortRemoveAllPrimary(i);
                        abortTask = false;
                        return;
                    }
                    RemoveFile(duplicatedFiles[i].Item1[j].Path, primaryDirectory, false, false);
                }
                if (duplicatedFiles[i].Item2.Count <= 1) {
                    duplicatedFiles.RemoveAt(i);
                }
                else {
                    duplicatedFiles[i].Item1.Clear();
                    i++;
                }
                dispatcher.Invoke(() => {
                    int progress = i + count - duplicatedFiles.Count;
                    progressBar.Value = progress * 100f / count;
                    Utility.SetProgress(progress, duplicatedFiles.Count);
                });
            }
        }

        private static void AbortRemoveAllPrimary(int i) {
            if (duplicatedFiles[i].Item2.Count <= 1)
                duplicatedFiles.RemoveAt(i);
            else
                duplicatedFiles[i].Item1.Clear();
        }

        internal static void RemoveAllSecondary(string secondaryDirectory) {
            int count = duplicatedFiles.Count;
            for (int i = 0; i < duplicatedFiles.Count;) {
                for (int j = 0; j < duplicatedFiles[i].Item2.Count; j++) {
                    if (abortTask) {
                        AbortRemoveAllSecondary(i);
                        abortTask = false;
                        return;
                    }
                    RemoveFile(duplicatedFiles[i].Item2[j].Path, secondaryDirectory, false, false);
                }
                if (duplicatedFiles[i].Item1.Count <= 1) {
                    duplicatedFiles.RemoveAt(i);
                }
                else {
                    duplicatedFiles[i].Item2.Clear();
                    i++;
                }
                dispatcher.Invoke(() => {
                    int progress = i + count - duplicatedFiles.Count;
                    progressBar.Value = progress * 100f / count;
                    Utility.SetProgress(progress, duplicatedFiles.Count);
                });
            }
        }

        private static void AbortRemoveAllSecondary(int i) {
            if (duplicatedFiles[i].Item1.Count <= 1)
                duplicatedFiles.RemoveAt(i);
            else
                duplicatedFiles[i].Item2.Clear();
        }
    }
}
