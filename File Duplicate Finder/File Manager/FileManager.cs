using FileDuplicateFinder.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace FileDuplicateFinder {
    using FileEntryCollection = ObservableRangeCollection<FileEntry>;
    internal static class FileManager {
        public static string tmpDirectory;
        private const int megaByte = 1048576;
        private volatile static bool stopTask = false;
        private static readonly byte[] bufferPrimary = new byte[megaByte];
        private static readonly byte[] bufferSecondary = new byte[megaByte];

        // holds relative path to all files in primary directory
        public static List<string> primaryFiles = new List<string>();
        // holds relative path to all files in secondary directory
        public static List<string> secondaryFiles = new List<string>();
        public static ObservableSortableCollection<Tuple<FileEntryCollection, FileEntryCollection>> duplicatedFiles = new ObservableSortableCollection<Tuple<FileEntryCollection, FileEntryCollection>>();
        public static ObservableSortableCollection<FileEntryCollection> duplicatedFilesPrimaryOnly = new ObservableSortableCollection<FileEntryCollection>();
        public static Dictionary<int, int> duplicateIndexingPrimary = new Dictionary<int, int>();
        public static Dictionary<int, int> duplicateIndexingSecondary = new Dictionary<int, int>();
        public static FileEntryCollection emptyDirectoriesPrimary = new FileEntryCollection();
        public static FileEntryCollection emptyDirectoriesSecondary = new FileEntryCollection();
        public static FileEntryCollection emptyFilesPrimary = new FileEntryCollection();
        public static FileEntryCollection emptyFilesSecondary = new FileEntryCollection();
        // should I make storedFiles list ObservableRangeCollection in RestoreFileDialog class too?
        public static ObservableRangeCollection<Tuple<string, string>> storedFiles = new ObservableRangeCollection<Tuple<string, string>>();

        internal static StatusBarViewModel statusBarViewModel;
        internal static Dispatcher dispatcher;

        public static void StopTask() {
            stopTask = true;
        }

        /// <summary>
        /// Search for duplicated files in primary directory only
        /// </summary>
        /// <param name="directory">the base directory to start recursive search in</param>
        /// <param name="showBasePaths">flag whether absolute path or relative should be displayed</param>
        public static void FindDuplicatedFiles(string directory, bool showBasePaths) {
            SetProgressStatus("Processing directory", true);
            List<FileEntry> localEmptyDirectories = new List<FileEntry>();
            List<FileEntry> localEmptyFiles = new List<FileEntry>();

            ProcessDirectory(directory, primaryFiles, localEmptyDirectories, localEmptyFiles, directory, showBasePaths);

            if (stopTask) {
                stopTask = false;
                return;
            }


            dispatcher.BeginInvoke((Action)(() => {
                emptyDirectoriesPrimary.AddRange(localEmptyDirectories);
                emptyFilesPrimary.AddRange(localEmptyFiles);
                localEmptyDirectories.Clear();
                localEmptyFiles.Clear();
                statusBarViewModel.State = "Sorting files by size";
            }));

            bool sorted = false;
            while (!sorted) {
                try {
                    primaryFiles.Sort((a, b) => {
                        if (stopTask)
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
                    else if (ex.InnerException is TaskAbortedException) {
                        stopTask = false;
                        return;
                    }
                }
                sorted = true;
            }

            SetProgressStatus("Searching for duplicates...", 0, primaryFiles.Count);

            int index = 0;
            int otherIndex;
            long length;
            long otherLength;
            int groupIndex;
            List<FileEntryCollection> localList = new List<FileEntryCollection>();
            for (; index < primaryFiles.Count - 1; index++) {
                if (stopTask) {
                    stopTask = false;
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
                            if (stopTask) {
                                AbortFindDuplicateFilesOneDirectory(showBasePaths, localList, directory);
                                stopTask = false;
                                return;
                            }
                            long tmpLength = GetFileLength(directory + primaryFiles[otherIndex]);
                            if (tmpLength < 0)
                                continue;
                            else if (tmpLength != length)
                                break;

                            if (groupIndex != -1) {
                                if (CompareFileContent(directory + primaryFiles[index], directory + primaryFiles[otherIndex], length)) {
                                    string absolutePath = directory + primaryFiles[otherIndex];
                                    localList[groupIndex].Add(new FileEntry(primaryFiles[otherIndex], Utility.PrettyPrintSize(length), GetFileIcon(absolutePath)));
                                    duplicateIndexingPrimary[otherIndex] = groupIndex;
                                }
                            }
                            else if (!duplicateIndexingPrimary.ContainsKey(index)) {
                                if (CompareFileContent(directory + primaryFiles[index], directory + primaryFiles[otherIndex], length)) {
                                    string absolutePath = directory + primaryFiles[index];
                                    string absolutePathOther = directory + primaryFiles[otherIndex];
                                    groupIndex = localList.Count;
                                    duplicateIndexingPrimary[otherIndex] = localList.Count;
                                    FileEntryCollection list = new FileEntryCollection {
                                        new FileEntry(primaryFiles[index], Utility.PrettyPrintSize(length), GetFileIcon(absolutePath)),
                                        new FileEntry(primaryFiles[otherIndex], Utility.PrettyPrintSize(length), GetFileIcon(absolutePathOther))
                                    };

                                    ///
                                    Utility.BeginInvoke(() => BindingOperations.EnableCollectionSynchronization(list, new object()));

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
                    AddRangeToCollection(duplicatedFilesPrimaryOnly, new List<FileEntryCollection>(localList));
                    localList.Clear();
                }

                SetProgressStatus(otherIndex);
            }
            stopTask = false;
        }

        private static void AbortFindDuplicateFilesOneDirectory(bool showBasePaths, List<FileEntryCollection> localList, string directory) {
            duplicateIndexingPrimary.Clear();
            if (showBasePaths)
                for (int i = 0; i < localList.Count; i++)
                    for (int j = 0; j < localList[i].Count; j++)
                        localList[i][j].Path = directory + localList[i][j].Path;
            dispatcher.BeginInvoke((Action)(() => {
                duplicatedFilesPrimaryOnly.AddRange(localList);
                localList.Clear();
            }));
        }

        public static void FindDuplicatedFiles(string primaryDirectory, string secondaryDirectory, bool showBasePaths) {
            SetProgressStatus("Processing primary directory", true);
            List<FileEntry> localEmptyDirectoriesPrimary = new List<FileEntry>();
            List<FileEntry> localEmptyFilesPrimary = new List<FileEntry>();

            ProcessDirectory(primaryDirectory, primaryFiles, localEmptyDirectoriesPrimary, localEmptyFilesPrimary, primaryDirectory, showBasePaths);

            if (stopTask) {
                stopTask = false;
                return;
            }

            dispatcher.BeginInvoke((Action)(() => {
                emptyDirectoriesPrimary.AddRange(localEmptyDirectoriesPrimary);
                emptyFilesPrimary.AddRange(localEmptyFilesPrimary);
                statusBarViewModel.State = "Processing secondary directory";
                localEmptyDirectoriesPrimary.Clear();
                localEmptyFilesPrimary.Clear();
            }));
            List<FileEntry> localEmptyDirectoriesSecondary = new List<FileEntry>();
            List<FileEntry> localEmptyFilesSecondary = new List<FileEntry>();

            ProcessDirectory(secondaryDirectory, secondaryFiles, localEmptyDirectoriesSecondary, localEmptyFilesSecondary, secondaryDirectory, showBasePaths);

            if (stopTask) {
                stopTask = false;
                return;
            }

            dispatcher.BeginInvoke((Action)(() => {
                emptyDirectoriesSecondary.AddRange(localEmptyDirectoriesSecondary);
                emptyFilesSecondary.AddRange(localEmptyFilesSecondary);
                statusBarViewModel.State = "Sorting files by size in primary directory";
                localEmptyDirectoriesSecondary.Clear();
                localEmptyFilesSecondary.Clear();
            }));

            bool sorted = false;
            while (!sorted) {
                try {
                    primaryFiles.Sort((a, b) => {
                        if (stopTask)
                            throw new TaskAbortedException();
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
                    else if (ex.InnerException is TaskAbortedException) {
                        stopTask = false;
                        return;
                    }
                }
                sorted = true;
            }

            SetProgressStatus("Sorting files by size in secondary directory");

            sorted = false;
            while (!sorted) {
                try {
                    secondaryFiles.Sort((a, b) => {
                        if (stopTask)
                            throw new TaskAbortedException();
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
                    else if (ex.InnerException is TaskAbortedException) {
                        stopTask = false;
                        return;
                    }
                }
                sorted = true;
            }

            SetProgressStatus("Searching for duplicates...", 0, primaryFiles.Count + secondaryFiles.Count);

            int indexPrimary = 0;
            int indexSecondary = 0;
            long lengthPrimary, lengthSecondary;
            List<Tuple<FileEntryCollection, FileEntryCollection>> localList = new List<Tuple<FileEntryCollection, FileEntryCollection>>();
            while (indexPrimary < primaryFiles.Count && indexSecondary < secondaryFiles.Count) {
                if (stopTask) {
                    stopTask = false;
                    return;
                }

                SetProgressStatus(indexPrimary + indexSecondary);

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
                        if (stopTask) {
                            AbortFindDuplicateFiles(showBasePaths, localList, primaryDirectory, secondaryDirectory);
                            stopTask = false;
                            return;
                        }
                        tmpLength = GetFileLength(primaryDirectory + primaryFiles[indexPrimary]);
                        if (tmpLength < 0)
                            continue;
                        else if (tmpLength != commonLength)
                            break;

                        for (indexSecondary = indexSecondaryStart; indexSecondary < secondaryFiles.Count; indexSecondary++) {
                            if (stopTask) {
                                AbortFindDuplicateFiles(showBasePaths, localList, primaryDirectory, secondaryDirectory);
                                stopTask = false;
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
                                    string absolutePath = secondaryDirectory + secondaryFiles[indexSecondary];
                                    localList[duplicateIndexingPrimary[indexPrimary]].Item2.Add(new FileEntry(secondaryFiles[indexSecondary], Utility.PrettyPrintSize(commonLength), GetFileIcon(absolutePath)));
                                    duplicateIndexingSecondary[indexSecondary] = duplicateIndexingPrimary[indexPrimary];
                                }
                                else if (duplicateIndexingSecondary.ContainsKey(indexSecondary)) {
                                    string absolutePath = primaryDirectory + primaryFiles[indexPrimary];
                                    localList[duplicateIndexingSecondary[indexSecondary]].Item1.Add(new FileEntry(primaryFiles[indexPrimary], Utility.PrettyPrintSize(commonLength), GetFileIcon(absolutePath)));
                                    duplicateIndexingPrimary[indexPrimary] = duplicateIndexingSecondary[indexSecondary];
                                }
                                else {
                                    string absolutePathPrimary = primaryDirectory + primaryFiles[indexPrimary];
                                    string absolutePathSecondary = secondaryDirectory + secondaryFiles[indexSecondary];
                                    duplicateIndexingPrimary[indexPrimary] = localList.Count;
                                    duplicateIndexingSecondary[indexSecondary] = localList.Count;
                                    Tuple<FileEntryCollection, FileEntryCollection> tuple = Tuple.Create(new FileEntryCollection(), new FileEntryCollection());

                                    ///
                                    Utility.BeginInvoke(() => {
                                        BindingOperations.EnableCollectionSynchronization(tuple.Item1, new object());
                                        BindingOperations.EnableCollectionSynchronization(tuple.Item2, new object());
                                    });

                                    tuple.Item1.Add(new FileEntry(primaryFiles[indexPrimary], Utility.PrettyPrintSize(commonLength), GetFileIcon(absolutePathPrimary)));
                                    tuple.Item2.Add(new FileEntry(secondaryFiles[indexSecondary], Utility.PrettyPrintSize(commonLength), GetFileIcon(absolutePathSecondary)));
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
                    AddRangeToCollection(duplicatedFiles, new List<Tuple<FileEntryCollection, FileEntryCollection>>(localList));
                    localList.Clear();
                }
                else if (lengthPrimary < lengthSecondary)
                    indexPrimary++;
                else
                    indexSecondary++;
            }
            stopTask = false;
        }

        private static void AbortFindDuplicateFiles(bool showBasePaths, List<Tuple<FileEntryCollection, FileEntryCollection>> localList, string primaryDirectory, string secondaryDirectory) {
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
            dispatcher.BeginInvoke((Action)(() => {
                duplicatedFiles.AddRange(localList);
                localList.Clear();
            }));
        }

        private static void ProcessDirectory(string targetDirectory, List<string> fileList, List<FileEntry> emptyDirectories, List<FileEntry> emptyFiles, string originalDirectory, bool showBasePaths) {
            if (stopTask)
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
                if (Directory.GetDirectories(targetDirectory).Length == 0) {
                    if (showBasePaths)
                        emptyDirectories.Add(new FileEntry(targetDirectory, GetFolderIcon(targetDirectory)));
                    else
                        emptyDirectories.Add(new FileEntry(new string(targetDirectory.Skip(originalDirectory.Length).ToArray()), GetFolderIcon(targetDirectory)));
                }
            }
            else {
                foreach (var file in files) {
                    long fileLength = GetFileLength(file);
                    string relativePath = new string(file.Skip(originalDirectory.Length).ToArray());
                    if (fileLength == 0) {
                        if (showBasePaths)
                            emptyFiles.Add(new FileEntry(file, GetFileIcon(file)));
                        else
                            emptyFiles.Add(new FileEntry(relativePath, GetFileIcon(file)));
                    }
                    else if (fileLength > 0) {
                        fileList.Add(relativePath);
                    }
                }
            }
            // Recurse into subdirectories of this directory.
            foreach (var subdirectory in Directory.GetDirectories(targetDirectory))
                ProcessDirectory(subdirectory, fileList, emptyDirectories, emptyFiles, originalDirectory, showBasePaths);
        }

        private static long GetFileLength(string path) {
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
                if (stopTask) {
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
        }

        internal static void EmptyFilesPrimaryIgnoreFile(string path) {
            emptyFilesPrimary.Remove(path);
        }

        internal static void EmptyDirectoriesSecondaryIgnoreFile(string path) {
            emptyDirectoriesSecondary.Remove(path);
        }

        internal static void EmptyFilesSecondaryIgnoreFile(string path) {
            emptyFilesSecondary.Remove(path);
        }

        internal static void DuplicatedFilesPrimaryIgnoreFile(string path) {
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int j = 0; j < duplicatedFiles[i].Item1.Count; j++) {
                    if (stopTask) {
                        stopTask = false;
                        return;
                    }
                    if (duplicatedFiles[i].Item1[j].Path.Equals(path)) {
                        duplicatedFiles[i].Item1.RemoveAt(j);
                        if (duplicatedFiles[i].Item1.Count + duplicatedFiles[i].Item2.Count <= 1)
                            duplicatedFiles.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        internal static void DuplicatedFilesSecondaryIgnoreFile(string path) {
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int j = 0; j < duplicatedFiles[i].Item2.Count; j++) {
                    if (stopTask) {
                        stopTask = false;
                        return;
                    }
                    if (duplicatedFiles[i].Item2[j].Path.Equals(path)) {
                        duplicatedFiles[i].Item2.RemoveAt(j);
                        if (duplicatedFiles[i].Item1.Count + duplicatedFiles[i].Item2.Count <= 1)
                            duplicatedFiles.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        internal static void DuplicatedFilesPrimaryOnlyIgnoreFile(string path) {
            for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++) {
                for (int j = 0; j < duplicatedFilesPrimaryOnly[i].Count; j++) {
                    if (stopTask) {
                        stopTask = false;
                        return;
                    }
                    if (duplicatedFilesPrimaryOnly[i][j].Path.Equals(path)) {
                        duplicatedFilesPrimaryOnly[i].RemoveAt(j);
                        if (duplicatedFilesPrimaryOnly[i].Count == 0)
                            duplicatedFilesPrimaryOnly.RemoveAt(i);
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
        internal static bool RemoveFile(string path, bool backupFiles = false, bool askLarge = false) {
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


        #region Sorting functions
        internal static void SortAlphabetically() {
            duplicatedFiles.Sort(Comparer<Tuple<FileEntryCollection, FileEntryCollection>>.Create((a, b) => a.Item1[0].Path.CompareTo(b.Item1[0].Path)));
        }

        /// could do simpler sort as follows:
        /// duplicatedFiles.Sort(Comparer<Tuple<List<FileEntry>, List<FileEntry>>>.Create((a, b) => a.Item1[0].Size.CompareTo(b.Item1[0].Size)));
        internal static void SortBySize(string baseDirectory = "") {
            bool sorted = false;
            while (!sorted) {
                try {
                    duplicatedFiles.Sort(Comparer<Tuple<FileEntryCollection, FileEntryCollection>>.Create((a, b) => {
                        long aLength = 0, bLength = 0;
                        if (a.Item1.Count > 0) {
                            aLength = GetFileLength(baseDirectory + a.Item1[0].Path);
                            if (aLength < 0) {
                                a.Item1.RemoveAt(0);
                                throw new SortException();
                            }
                        }
                        if (b.Item1.Count > 0) {
                            bLength = GetFileLength(baseDirectory + b.Item1[0].Path);
                            if (bLength < 0) {
                                b.Item1.RemoveAt(0);
                                throw new SortException();
                            }
                        }
                        return aLength.CompareTo(bLength);
                    }));
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
            duplicatedFilesPrimaryOnly.Sort(Comparer<FileEntryCollection>.Create((a, b) => a[0].Path.CompareTo(b[0].Path)));
        }

        internal static void SortBySizePrimaryOnly(string baseDirectory = "") {
            bool sorted = false;
            while (!sorted) {
                try {
                    duplicatedFilesPrimaryOnly.Sort(Comparer<FileEntryCollection>.Create((a, b) => {
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
                    }));
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
        #endregion


        internal static void RemoveAllEmptyDirectoriesPrimary(string baseDirectory = "") {
            SetProgressStatus(0, emptyDirectoriesPrimary.Count);
            for (int i = 0; i < emptyDirectoriesPrimary.Count; i++) {
                if (stopTask) {
                    AbortRemoveAllEmptyDirectoriesPrimary(i);
                    stopTask = false;
                    return;
                }
                RemoveFile(baseDirectory + emptyDirectoriesPrimary[i].Path);
                SetProgressStatus(i);
            }
            emptyDirectoriesPrimary.Clear();
        }

        private static void AbortRemoveAllEmptyDirectoriesPrimary(int i) {
            emptyDirectoriesPrimary.RemoveRange(0, i);
        }

        internal static void RemoveAllEmptyDirectoriesSecondary(string baseDirectory = "") {
            SetProgressStatus(0, emptyDirectoriesSecondary.Count);
            for (int i = 0; i < emptyDirectoriesSecondary.Count; i++) {
                if (stopTask) {
                    AbortRemoveAllEmptyDirectoriesSecondary(i);
                    stopTask = false;
                    return;
                }
                RemoveFile(baseDirectory + emptyDirectoriesSecondary[i].Path);
                SetProgressStatus(i);
            }
            emptyDirectoriesSecondary.Clear();
        }

        private static void AbortRemoveAllEmptyDirectoriesSecondary(int i) {
            emptyDirectoriesSecondary.RemoveRange(0, i);
        }

        internal static void RemoveAllEmptyFilesPrimary(string baseDirectory = "") {
            SetProgressStatus(0, emptyFilesPrimary.Count);
            for (int i = 0; i < emptyFilesPrimary.Count; i++) {
                if (stopTask) {
                    AbortRemoveAllEmptyFilesPrimary(i);
                    stopTask = false;
                    return;
                }
                RemoveFile(baseDirectory + emptyFilesPrimary[i].Path);
                SetProgressStatus(i);
            }
            emptyFilesPrimary.Clear();
        }

        private static void AbortRemoveAllEmptyFilesPrimary(int i) {
            emptyFilesPrimary.RemoveRange(0, i);
        }

        internal static void RemoveAllEmptyFilesSecondary(string baseDirectory = "") {
            SetProgressStatus(0, emptyFilesSecondary.Count);
            for (int i = 0; i < emptyFilesSecondary.Count; i++) {
                if (stopTask) {
                    AbortRemoveAllEmptyFilesSecondary(i);
                    stopTask = false;
                    return;
                }
                RemoveFile(baseDirectory + emptyFilesSecondary[i].Path);
                SetProgressStatus(i);
            }
            emptyFilesSecondary.Clear();
        }

        private static void AbortRemoveAllEmptyFilesSecondary(int i) {
            emptyFilesSecondary.RemoveRange(0, i);
        }

        internal static void RemoveAllPrimary(string baseDirectory = "") {
            SetProgressStatus(0, duplicatedFiles.Count);
            for (int i = 0, progress = 0; i < duplicatedFiles.Count; progress++) {
                for (int j = 0; j < duplicatedFiles[i].Item1.Count; j++) {
                    if (stopTask) {
                        AbortRemoveAllPrimary(i);
                        stopTask = false;
                        return;
                    }
                    RemoveFile(baseDirectory + duplicatedFiles[i].Item1[j].Path);
                }
                if (duplicatedFiles[i].Item2.Count <= 1) {/// <= 0 ? then no need for count variable probably
                    duplicatedFiles.RemoveAt(i);    ///this is slow when right side is removed and on the left every single needs to be erased exclusively instead of bulk clear
                }
                else {
                    duplicatedFiles[i].Item1.Clear();
                    i++;
                }
                SetProgressStatus(progress);
            }
        }

        private static void AbortRemoveAllPrimary(int i) {
            if (duplicatedFiles[i].Item2.Count <= 1)
                duplicatedFiles.RemoveAt(i);
            else
                duplicatedFiles[i].Item1.Clear();
        }

        internal static void RemoveAllSecondary(string baseDirectory = "") {
            SetProgressStatus(0, duplicatedFiles.Count);
            for (int i = 0, progress = 0; i < duplicatedFiles.Count; progress++) {
                for (int j = 0; j < duplicatedFiles[i].Item2.Count; j++) {
                    if (stopTask) {
                        AbortRemoveAllSecondary(i);
                        stopTask = false;
                        return;
                    }
                    RemoveFile(baseDirectory + duplicatedFiles[i].Item2[j].Path);
                }
                if (duplicatedFiles[i].Item1.Count <= 1) {///
                    duplicatedFiles.RemoveAt(i);
                }
                else {
                    duplicatedFiles[i].Item2.Clear();
                    i++;
                }
                SetProgressStatus(progress);
            }
        }

        /// put abort functions as extension to observablecollection
        private static void AbortRemoveAllSecondary(int i) {
            if (duplicatedFiles[i].Item1.Count <= 1)
                duplicatedFiles.RemoveAt(i);
            else
                duplicatedFiles[i].Item2.Clear();
        }

        ///
        private static void SetProgressStatus(int progress) {
            string stateInfo = progress + " / " + statusBarViewModel.MaxProgress;
            dispatcher.BeginInvoke((Action)(() => {
                statusBarViewModel.StateInfo = stateInfo;
                statusBarViewModel.Progress = progress;
            }));
        }

        private static void SetProgressStatus(int progress, int maxProgress) {
            string stateInfo = progress + " / " + maxProgress;
            dispatcher.BeginInvoke((Action)(() => {
                statusBarViewModel.StateInfo = stateInfo;
                statusBarViewModel.MaxProgress = maxProgress;
                statusBarViewModel.Progress = progress;
            }));
        }
        
        private static void SetProgressStatus(string state) {
            dispatcher.BeginInvoke((Action)(() => {
                statusBarViewModel.State = state;
            }));
        }

        private static void SetProgressStatus(string state, bool isIndeterminate) {
            dispatcher.BeginInvoke((Action)(() => {
                statusBarViewModel.State = state;
                statusBarViewModel.IsIndeterminate = isIndeterminate;
            }));
        }

        private static void SetProgressStatus(string state, int progress, int maxProgress) {
            string stateInfo = progress + " / " + maxProgress;
            dispatcher.BeginInvoke((Action)(() => {
                statusBarViewModel.State = state;
                statusBarViewModel.StateInfo = stateInfo;
                statusBarViewModel.IsIndeterminate = false;
                statusBarViewModel.MaxProgress = maxProgress;
                statusBarViewModel.Progress = progress;
            }));
        }

        private static void AddRangeToCollection<T>(ObservableRangeCollection<T> collection, List<T> list) {
            dispatcher.BeginInvoke((Action)(() => {
                collection.AddRange(list);
            }));
        }

        public static ImageSource GetFileIcon(string filePath) {
            //TODO Hashtable store extenion
            ImageSource icon = IconReader.GetFileIcon(filePath, IconReader.IconSize.Small, false);
            icon?.Freeze();
            return icon;
        }
        public static ImageSource GetFolderIcon(string filePath) {
            //TODO Hashtable store
            ImageSource icon = IconReader.GetFolderIcon(filePath, IconReader.IconSize.Small, IconReader.FolderType.Closed);
            icon?.Freeze();
            return icon;
        }
    }
}
