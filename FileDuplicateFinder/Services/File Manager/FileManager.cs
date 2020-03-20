using FileDuplicateFinder.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FileDuplicateFinder.Services {
    using FileEntryCollection = ObservableRangeCollection<FileEntry>;
    using TupleOfFileEntryCollections = Tuple<ObservableRangeCollection<FileEntry>, ObservableRangeCollection<FileEntry>>;

    public static class FileManager {
        public static string tmpDirectory;
        private const int mebiByte = 1048576;
        private volatile static bool stopTask = false;
        private static readonly byte[] bufferPrimary = new byte[mebiByte];
        private static readonly byte[] bufferSecondary = new byte[mebiByte];
        private static Dictionary<int, int> duplicateIndexingPrimary = new Dictionary<int, int>();
        private static Dictionary<int, int> duplicateIndexingSecondary = new Dictionary<int, int>();

        // holds relative path to all files in primary directory
        public static List<string> primaryFiles = new List<string>();
        // holds relative path to all files in secondary directory
        public static List<string> secondaryFiles = new List<string>();
        public static ObservableSortableCollection<TupleOfFileEntryCollections> duplicatedFiles = new ObservableSortableCollection<TupleOfFileEntryCollections>();
        public static ObservableSortableCollection<FileEntryCollection> duplicatedFilesPrimaryOnly = new ObservableSortableCollection<FileEntryCollection>();
        public static FileEntryCollection emptyDirectoriesPrimary = new FileEntryCollection();
        public static FileEntryCollection emptyDirectoriesSecondary = new FileEntryCollection();
        public static FileEntryCollection emptyFilesPrimary = new FileEntryCollection();
        public static FileEntryCollection emptyFilesSecondary = new FileEntryCollection();
        /// should storedFiles list be made as ObservableRangeCollection in RestoreFileDialog class too?
        public static ObservableRangeCollection<Tuple<string, string>> storedFiles = new ObservableRangeCollection<Tuple<string, string>>();

        public delegate void SearchProgressUpdatedEventHandler(DuplicateSearchProgress progress);
        public static event SearchProgressUpdatedEventHandler SearchProgressUpdated;
        public delegate void RemoveProgressUpdatedEventHandler(RemoveProgress progress);
        public static event RemoveProgressUpdatedEventHandler RemoveProgressUpdated;

        static FileManager() {
            ///this or stick to refresh and no use for observable
            BindingOperations.EnableCollectionSynchronization(emptyDirectoriesPrimary, new object()); /// store objects
            BindingOperations.EnableCollectionSynchronization(emptyFilesPrimary, new object());
            BindingOperations.EnableCollectionSynchronization(emptyDirectoriesSecondary, new object());
            BindingOperations.EnableCollectionSynchronization(emptyFilesSecondary, new object());
            BindingOperations.EnableCollectionSynchronization(duplicatedFiles, new object());
            BindingOperations.EnableCollectionSynchronization(emptyDirectoriesPrimary, new object());
            BindingOperations.EnableCollectionSynchronization(emptyFilesPrimary, new object());
            BindingOperations.EnableCollectionSynchronization(duplicatedFilesPrimaryOnly, new object());
        }

        public static void StopTask() {
            stopTask = true;
        }

        /// <summary>
        /// Search for duplicated files in primary directory only
        /// </summary>
        /// <param name="directory">The base directory to start recursive search in</param>
        /// <param name="showBasePaths">A flag specifying whether an absolute or relative path should be displayed</param>
        public static void FindDuplicatedFiles(string directory, bool showBasePaths) {
            SearchProgressUpdated?.Invoke(new DuplicateSearchProgress() {
                Description = "Processing directory",
                State = DuplicateSearchProgressState.Processing
            });

            ProcessDirectory(directory, primaryFiles, emptyDirectoriesPrimary, emptyFilesPrimary, directory, showBasePaths);

            if (stopTask) {
                stopTask = false;
                return;
            }

            SearchProgressUpdated?.Invoke(new DuplicateSearchProgress() {
                Description = "Sorting files by size",
                State = DuplicateSearchProgressState.Sorting
            });

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
                } catch (InvalidOperationException ex) {
                    if (ex.InnerException is SortException) {
                        for (int i = 0; i < primaryFiles.Count; i++) {
                            if (GetFileLength(directory + primaryFiles[i]) < 0) {
                                primaryFiles.RemoveAt(i);
                                i--;
                            }
                        }
                        continue;
                    } else if (ex.InnerException is TaskAbortedException) {
                        stopTask = false;
                        return;
                    }
                }
                sorted = true;
            }

            SearchProgressUpdated?.Invoke(new DuplicateSearchProgress() {
                Description = "Searching for duplicates...",
                MaxProgress = primaryFiles.Count,
                State = DuplicateSearchProgressState.StartingSearch
            });

            DuplicateSearchProgress progress = new DuplicateSearchProgress() {
                MaxProgress = primaryFiles.Count,
                State = DuplicateSearchProgressState.Searching
            };

            int index = 0;
            int otherIndex;
            long length;
            long otherLength;
            int groupIndex;

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
                        string absolutePath = directory + primaryFiles[index];
                        string displayPath, displayPathOther;
                        if (showBasePaths)
                            displayPath = absolutePath;
                        else
                            displayPath = primaryFiles[index];

                        for (otherIndex = index + 1, groupIndex = -1; otherIndex < primaryFiles.Count; otherIndex++) {
                            if (stopTask) {
                                duplicateIndexingPrimary.Clear();
                                stopTask = false;
                                return;
                            }

                            string absolutePathOther = directory + primaryFiles[otherIndex];
                            long tmpLength = GetFileLength(absolutePathOther);

                            if (tmpLength < 0)
                                continue;
                            else if (tmpLength != length)
                                break;

                            if (groupIndex != -1) {
                                if (CompareFileContent(absolutePath, absolutePathOther, length)) {
                                    if (showBasePaths)
                                        displayPathOther = absolutePathOther;
                                    else
                                        displayPathOther = primaryFiles[otherIndex];

                                    duplicatedFilesPrimaryOnly[groupIndex].Add(new FileEntry(displayPathOther, CommonUtilities.PrettyPrintSize(length), GetFileIcon(absolutePathOther)));
                                    duplicateIndexingPrimary[otherIndex] = groupIndex;
                                }
                            } else if (!duplicateIndexingPrimary.ContainsKey(index)) {
                                if (CompareFileContent(absolutePath, absolutePathOther, length)) {
                                    groupIndex = duplicatedFilesPrimaryOnly.Count;
                                    duplicateIndexingPrimary[otherIndex] = duplicatedFilesPrimaryOnly.Count;

                                    if (showBasePaths)
                                        displayPathOther = absolutePathOther;
                                    else
                                        displayPathOther = primaryFiles[otherIndex];

                                    FileEntryCollection list = new FileEntryCollection {
                                        new FileEntry(displayPath, CommonUtilities.PrettyPrintSize(length), GetFileIcon(absolutePath)),
                                        new FileEntry(displayPathOther, CommonUtilities.PrettyPrintSize(length), GetFileIcon(absolutePathOther))
                                    };

                                    ///
                                    CommonUtilities.BeginInvoke(() => BindingOperations.EnableCollectionSynchronization(list, new object()));

                                    duplicatedFilesPrimaryOnly.Add(list);
                                }
                            }
                        }
                        index++;
                    } while (index < otherIndex - 1);
                    duplicateIndexingPrimary.Clear();
                }

                progress.Progress = otherIndex;
                SearchProgressUpdated?.Invoke(progress);
            }
            stopTask = false;
        }

        public static void RemoveSecondaryFileWithPath(string path) {
            if (!emptyFilesSecondary.Remove(path)) {
                for (int i = 0; i < duplicatedFiles.Count; i++) {
                    int index = duplicatedFiles[i].Item2.FindIndex(f => f.Path == path);
                    if (index >= 0) {
                        duplicatedFiles[i].Item2.RemoveAt(index);
                        if (duplicatedFiles[i].Item2.Count == 0 && duplicatedFiles[i].Item1.Count <= 1)
                            duplicatedFiles.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public static void RemovePrimaryFileWithPath(string path) {
            if (!emptyFilesPrimary.Remove(path)) {
                for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++) {
                    int index = duplicatedFilesPrimaryOnly[i].FindIndex(f => f.Path == path);
                    if (index >= 0) {
                        duplicatedFilesPrimaryOnly[i].RemoveAt(index);
                        if (duplicatedFilesPrimaryOnly[i].Count <= 1)
                            duplicatedFilesPrimaryOnly.RemoveAt(i);
                        return;
                    }
                }
                for (int i = 0; i < duplicatedFiles.Count; i++) {
                    int index = duplicatedFiles[i].Item1.FindIndex(f => f.Path == path);
                    if (index >= 0) {
                        duplicatedFiles[i].Item1.RemoveAt(index);
                        if (duplicatedFiles[i].Item2.Count == 0 && duplicatedFiles[i].Item1.Count <= 1)
                            duplicatedFiles.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public static void Initialize() {
            primaryFiles.Clear();
            secondaryFiles.Clear();
            emptyDirectoriesPrimary.Clear();
            emptyFilesPrimary.Clear();
            emptyDirectoriesSecondary.Clear();
            emptyFilesSecondary.Clear();
            duplicatedFiles.Clear();
            duplicatedFilesPrimaryOnly.Clear();
        }

        public static void HidePrimaryBasePaths(string primaryDirectory) {
            for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++)
                for (int p = 0; p < duplicatedFilesPrimaryOnly[i].Count; p++)
                    duplicatedFilesPrimaryOnly[i][p].Path = new string(duplicatedFilesPrimaryOnly[i][p].Path.Skip(primaryDirectory.Length).ToArray());
            for (int i = 0; i < emptyDirectoriesPrimary.Count; i++)
                emptyDirectoriesPrimary[i].Path = new string(emptyDirectoriesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());
            for (int i = 0; i < emptyFilesPrimary.Count; i++)
                emptyFilesPrimary[i].Path = new string(emptyFilesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());

            duplicatedFilesPrimaryOnly.Refresh();
            emptyDirectoriesPrimary.Refresh();
            emptyFilesPrimary.Refresh();
        }

        public static void HideBasePaths(string primaryDirectory, string secondaryDirectory) {
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int p = 0; p < duplicatedFiles[i].Item1.Count; p++)
                    duplicatedFiles[i].Item1[p].Path = new string(duplicatedFiles[i].Item1[p].Path.Skip(primaryDirectory.Length).ToArray());
                for (int s = 0; s < duplicatedFiles[i].Item2.Count; s++)
                    duplicatedFiles[i].Item2[s].Path = new string(duplicatedFiles[i].Item2[s].Path.Skip(secondaryDirectory.Length).ToArray());
            }
            for (int i = 0; i < emptyDirectoriesPrimary.Count; i++)
                emptyDirectoriesPrimary[i].Path = new string(emptyDirectoriesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());
            for (int i = 0; i < emptyFilesPrimary.Count; i++)
                emptyFilesPrimary[i].Path = new string(emptyFilesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());
            for (int i = 0; i < emptyDirectoriesSecondary.Count; i++)
                emptyDirectoriesSecondary[i].Path = new string(emptyDirectoriesSecondary[i].Path.Skip(secondaryDirectory.Length).ToArray());
            for (int i = 0; i < emptyFilesSecondary.Count; i++)
                emptyFilesSecondary[i].Path = new string(emptyFilesSecondary[i].Path.Skip(secondaryDirectory.Length).ToArray());

            duplicatedFiles.Refresh();
            emptyDirectoriesPrimary.Refresh();
            emptyFilesPrimary.Refresh();
            emptyDirectoriesSecondary.Refresh();
            emptyFilesSecondary.Refresh();
        }

        public static void ShowPrimaryBasePaths(string primaryDirectory) {
            for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++)
                for (int p = 0; p < duplicatedFilesPrimaryOnly[i].Count; p++)
                    duplicatedFilesPrimaryOnly[i][p].Path = primaryDirectory + duplicatedFilesPrimaryOnly[i][p].Path;
            for (int i = 0; i < emptyDirectoriesPrimary.Count; i++)
                emptyDirectoriesPrimary[i].Path = primaryDirectory + emptyDirectoriesPrimary[i].Path;
            for (int i = 0; i < emptyFilesPrimary.Count; i++)
                emptyFilesPrimary[i].Path = primaryDirectory + emptyFilesPrimary[i].Path;

            duplicatedFilesPrimaryOnly.Refresh();
            emptyDirectoriesPrimary.Refresh();
            emptyFilesPrimary.Refresh();
        }

        public static void ShowBasePaths(string primaryDirectory, string secondaryDirectory) {
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int p = 0; p < duplicatedFiles[i].Item1.Count; p++)
                    duplicatedFiles[i].Item1[p].Path = primaryDirectory + duplicatedFiles[i].Item1[p].Path;
                for (int s = 0; s < duplicatedFiles[i].Item2.Count; s++)
                    duplicatedFiles[i].Item2[s].Path = secondaryDirectory + duplicatedFiles[i].Item2[s].Path;
            }
            for (int i = 0; i < emptyDirectoriesPrimary.Count; i++)
                emptyDirectoriesPrimary[i].Path = primaryDirectory + emptyDirectoriesPrimary[i].Path;
            for (int i = 0; i < emptyFilesPrimary.Count; i++)
                emptyFilesPrimary[i].Path = primaryDirectory + emptyFilesPrimary[i].Path;
            for (int i = 0; i < emptyDirectoriesSecondary.Count; i++)
                emptyDirectoriesSecondary[i].Path = secondaryDirectory + emptyDirectoriesSecondary[i].Path;
            for (int i = 0; i < emptyFilesSecondary.Count; i++)
                emptyFilesSecondary[i].Path = secondaryDirectory + emptyFilesSecondary[i].Path;

            duplicatedFiles.Refresh();
            emptyDirectoriesPrimary.Refresh();
            emptyFilesPrimary.Refresh();
            emptyDirectoriesSecondary.Refresh();
            emptyFilesSecondary.Refresh();
        }

        public static void FindDuplicatedFiles(string primaryDirectory, string secondaryDirectory, bool showBasePaths) {
            SearchProgressUpdated?.Invoke(new DuplicateSearchProgress() {
                Description = "Processing primary directory",
                State = DuplicateSearchProgressState.Processing
            });

            ProcessDirectory(primaryDirectory, primaryFiles, emptyDirectoriesPrimary, emptyFilesPrimary, primaryDirectory, showBasePaths);

            if (stopTask) {
                stopTask = false;
                return;
            }

            SearchProgressUpdated?.Invoke(new DuplicateSearchProgress() {
                Description = "Processing secondary directory",
                State = DuplicateSearchProgressState.Processing
            });

            ProcessDirectory(secondaryDirectory, secondaryFiles, emptyDirectoriesSecondary, emptyFilesSecondary, secondaryDirectory, showBasePaths);

            if (stopTask) {
                stopTask = false;
                return;
            }

            SearchProgressUpdated?.Invoke(new DuplicateSearchProgress() {
                Description = "Sorting files by size in primary directory",
                State = DuplicateSearchProgressState.Sorting
            });

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
                } catch (InvalidOperationException ex) {
                    if (ex.InnerException is SortException) {
                        for (int i = 0; i < primaryFiles.Count; i++) {
                            if (GetFileLength(primaryDirectory + primaryFiles[i]) < 0) {
                                primaryFiles.RemoveAt(i);
                                i--;
                            }
                        }
                        continue;
                    } else if (ex.InnerException is TaskAbortedException) {
                        stopTask = false;
                        return;
                    }
                }
                sorted = true;
            }

            SearchProgressUpdated?.Invoke(new DuplicateSearchProgress() {
                Description = "Sorting files by size in secondary directory",
                State = DuplicateSearchProgressState.Sorting
            });

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
                } catch (InvalidOperationException ex) {
                    if (ex.InnerException is SortException) {
                        for (int i = 0; i < secondaryFiles.Count; i++) {
                            if (GetFileLength(secondaryDirectory + secondaryFiles[i]) < 0) {
                                secondaryFiles.RemoveAt(i);
                                i--;
                            }
                        }
                        continue;
                    } else if (ex.InnerException is TaskAbortedException) {
                        stopTask = false;
                        return;
                    }
                }
                sorted = true;
            }

            SearchProgressUpdated?.Invoke(new DuplicateSearchProgress() {
                Description = "Searching for duplicates...",
                MaxProgress = primaryFiles.Count + secondaryFiles.Count,
                Progress = 0,
                State = DuplicateSearchProgressState.StartingSearch
            });

            DuplicateSearchProgress progress = new DuplicateSearchProgress() {
                MaxProgress = primaryFiles.Count + secondaryFiles.Count,
                State = DuplicateSearchProgressState.Searching
            };

            int indexPrimary = 0;
            int indexSecondary = 0;
            long lengthPrimary, lengthSecondary;
            while (indexPrimary < primaryFiles.Count && indexSecondary < secondaryFiles.Count) {
                if (stopTask) {
                    stopTask = false;
                    return;
                }

                progress.Progress = indexPrimary + indexSecondary;
                SearchProgressUpdated?.Invoke(progress);

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
                            duplicateIndexingPrimary.Clear();
                            duplicateIndexingSecondary.Clear();
                            stopTask = false;
                            return;
                        }

                        string absolutePathPrimary = primaryDirectory + primaryFiles[indexPrimary];
                        string displayPathPrimary;
                        if (showBasePaths)
                            displayPathPrimary = absolutePathPrimary;
                        else
                            displayPathPrimary = primaryFiles[indexPrimary];

                        tmpLength = GetFileLength(absolutePathPrimary);
                        if (tmpLength < 0)
                            continue;
                        else if (tmpLength != commonLength)
                            break;

                        for (indexSecondary = indexSecondaryStart; indexSecondary < secondaryFiles.Count; indexSecondary++) {
                            if (stopTask) {
                                duplicateIndexingPrimary.Clear();
                                duplicateIndexingSecondary.Clear();
                                stopTask = false;
                                return;
                            }

                            string absolutePathSecondary = secondaryDirectory + secondaryFiles[indexSecondary];
                            string displayPathSecondary;
                            if (showBasePaths)
                                displayPathSecondary = absolutePathSecondary;
                            else
                                displayPathSecondary = secondaryFiles[indexSecondary];

                            tmpLength = GetFileLength(absolutePathSecondary);
                            if (tmpLength < 0)
                                continue;
                            else if (tmpLength != commonLength)
                                break;

                            if (duplicateIndexingPrimary.ContainsKey(indexPrimary) && duplicateIndexingSecondary.ContainsKey(indexSecondary))
                                continue; // if it was included in the indexing before
                            if (CompareFileContent(primaryDirectory + primaryFiles[indexPrimary], secondaryDirectory + secondaryFiles[indexSecondary], commonLength)) {
                                if (duplicateIndexingPrimary.ContainsKey(indexPrimary)) {
                                    duplicatedFiles[duplicateIndexingPrimary[indexPrimary]].Item2.Add(new FileEntry(displayPathSecondary, CommonUtilities.PrettyPrintSize(commonLength), GetFileIcon(absolutePathSecondary)));
                                    duplicateIndexingSecondary[indexSecondary] = duplicateIndexingPrimary[indexPrimary];
                                } else if (duplicateIndexingSecondary.ContainsKey(indexSecondary)) {
                                    duplicatedFiles[duplicateIndexingSecondary[indexSecondary]].Item1.Add(new FileEntry(displayPathPrimary, CommonUtilities.PrettyPrintSize(commonLength), GetFileIcon(absolutePathPrimary)));
                                    duplicateIndexingPrimary[indexPrimary] = duplicateIndexingSecondary[indexSecondary];
                                } else {
                                    duplicateIndexingPrimary[indexPrimary] = duplicatedFiles.Count;
                                    duplicateIndexingSecondary[indexSecondary] = duplicatedFiles.Count;
                                    TupleOfFileEntryCollections tuple = Tuple.Create(new FileEntryCollection(), new FileEntryCollection());

                                    ///
                                    CommonUtilities.BeginInvoke(() => {
                                        BindingOperations.EnableCollectionSynchronization(tuple.Item1, new object());
                                        BindingOperations.EnableCollectionSynchronization(tuple.Item2, new object());
                                    });

                                    tuple.Item1.Add(new FileEntry(displayPathPrimary, CommonUtilities.PrettyPrintSize(commonLength), GetFileIcon(absolutePathPrimary)));
                                    tuple.Item2.Add(new FileEntry(displayPathSecondary, CommonUtilities.PrettyPrintSize(commonLength), GetFileIcon(absolutePathSecondary)));
                                    duplicatedFiles.Add(tuple);
                                }
                            }
                        }
                    }
                    duplicateIndexingPrimary.Clear();
                    duplicateIndexingSecondary.Clear();
                } else if (lengthPrimary < lengthSecondary)
                    indexPrimary++;
                else
                    indexSecondary++;
            }
            stopTask = false;
        }

        private static void ProcessDirectory(string targetDirectory, List<string> fileList, ICollection<FileEntry> emptyDirectories, ICollection<FileEntry> emptyFiles, string originalDirectory, bool showBasePaths) {
            if (stopTask)
                return;
            string[] files;
            try {
                files = Directory.GetFiles(targetDirectory);
            } catch (UnauthorizedAccessException) {
                CommonUtilities.LogFromNonGUIThread("Access to path \"" + targetDirectory + "\" was denied.");
                return;
            }
            if (files.Length == 0) {
                if (Directory.GetDirectories(targetDirectory).Length == 0) {
                    if (showBasePaths)
                        emptyDirectories.Add(new FileEntry(targetDirectory, GetFolderIcon(targetDirectory)));
                    else
                        emptyDirectories.Add(new FileEntry(new string(targetDirectory.Skip(originalDirectory.Length).ToArray()), GetFolderIcon(targetDirectory)));
                }
            } else {
                foreach (var file in files) {
                    long fileLength = GetFileLength(file);
                    string relativePath = new string(file.Skip(originalDirectory.Length).ToArray());
                    if (fileLength == 0) {
                        if (showBasePaths)
                            emptyFiles.Add(new FileEntry(file, GetFileIcon(file)));
                        else
                            emptyFiles.Add(new FileEntry(relativePath, GetFileIcon(file)));
                    } else if (fileLength > 0) {
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
            } catch (FileNotFoundException) {
                CommonUtilities.LogFromNonGUIThread("Could not find file \"" + path + "\". It has been probably deleted just now.");
                return -1;
            }
        }

        private static bool CompareFileContent(string filePrimary, string fileSecondary, long fileLength) {
            FileStream fileStreamPrimary = null;
            FileStream fileStreamSecondary = null;
            try {
                fileStreamPrimary = File.OpenRead(filePrimary);
                try {
                    fileStreamSecondary = File.OpenRead(fileSecondary);

                    while (fileLength > 0) {
                        if (stopTask)
                            return false;

                        if (fileLength >= mebiByte) {
                            fileStreamPrimary.Read(bufferPrimary, 0, mebiByte);
                            fileStreamSecondary.Read(bufferSecondary, 0, mebiByte);
                            if (!Enumerable.SequenceEqual(bufferPrimary, bufferSecondary))
                                return false;
                        } else {
                            fileStreamPrimary.Read(bufferPrimary, 0, (int)fileLength);
                            fileStreamSecondary.Read(bufferSecondary, 0, (int)fileLength);
                            if (!Enumerable.SequenceEqual(Enumerable.Take(bufferPrimary, (int)fileLength), Enumerable.Take(bufferSecondary, (int)fileLength)))
                                return false;
                        }
                        fileLength -= mebiByte;
                    }
                } catch (IOException) {
                    CommonUtilities.LogFromNonGUIThread("Could not access file " + fileSecondary + " because it is being used by another process.");
                    return false;
                } finally {
                    fileStreamSecondary?.Close();
                }
            } catch (IOException) {
                CommonUtilities.LogFromNonGUIThread("Could not access file " + filePrimary + " because it is being used by another process.");
                return false;
            } finally {
                fileStreamPrimary?.Close();
            }

            return true;
        }

        public static void EmptyDirectoriesPrimaryIgnoreFile(string path) {
            emptyDirectoriesPrimary.Remove(path);
        }

        public static void EmptyFilesPrimaryIgnoreFile(string path) {
            emptyFilesPrimary.Remove(path);
        }

        public static void EmptyDirectoriesSecondaryIgnoreFile(string path) {
            emptyDirectoriesSecondary.Remove(path);
        }

        public static void EmptyFilesSecondaryIgnoreFile(string path) {
            emptyFilesSecondary.Remove(path);
        }

        public static void DuplicatedFilesPrimaryIgnoreFile(string path) {
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int j = 0; j < duplicatedFiles[i].Item1.Count; j++) {
                    if (stopTask) {
                        stopTask = false;
                        return;
                    }
                    if (string.Equals(duplicatedFiles[i].Item1[j].Path, path, StringComparison.Ordinal)) {
                        duplicatedFiles[i].Item1.RemoveAt(j);
                        if (duplicatedFiles[i].Item1.Count + duplicatedFiles[i].Item2.Count <= 1)
                            duplicatedFiles.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public static void DuplicatedFilesSecondaryIgnoreFile(string path) {
            for (int i = 0; i < duplicatedFiles.Count; i++) {
                for (int j = 0; j < duplicatedFiles[i].Item2.Count; j++) {
                    if (stopTask) {
                        stopTask = false;
                        return;
                    }
                    if (string.Equals(duplicatedFiles[i].Item2[j].Path, path, StringComparison.Ordinal)) {
                        duplicatedFiles[i].Item2.RemoveAt(j);
                        if (duplicatedFiles[i].Item1.Count + duplicatedFiles[i].Item2.Count <= 1)
                            duplicatedFiles.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public static void DuplicatedFilesPrimaryOnlyIgnoreFile(string path) {
            for (int i = 0; i < duplicatedFilesPrimaryOnly.Count; i++) {
                for (int j = 0; j < duplicatedFilesPrimaryOnly[i].Count; j++) {
                    if (stopTask) {
                        stopTask = false;
                        return;
                    }
                    if (string.Equals(duplicatedFilesPrimaryOnly[i][j].Path, path, StringComparison.Ordinal)) {
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
        public static bool RemoveFile(string path, bool backupFiles = false, bool askLarge = false) {
            bool canRestoreFiles = false;
            try {
                if (File.GetAttributes(path).HasFlag(FileAttributes.Directory)) {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    if (backupFiles) {
                        storedFiles.Add(Tuple.Create(path, ""));
                        canRestoreFiles = true;
                    }
                    directoryInfo.Delete();
                } else {  // file
                    FileInfo fileInfo = new FileInfo(path);
                    if (fileInfo.Length < 2 * mebiByte) {   // don't move large files
                        if (backupFiles) {
                            Guid guid = Guid.NewGuid();
                            fileInfo.MoveTo(tmpDirectory + guid.ToString());
                            storedFiles.Add(Tuple.Create(path, guid.ToString()));
                            canRestoreFiles = true;
                        } else {
                            fileInfo.Delete();
                        }
                    } else {
                        if (!askLarge || MessageBox.Show("You will permanently delete file " + path) == MessageBoxResult.OK)
                            fileInfo.Delete();
                    }
                }
            } catch (IOException) {
                CommonUtilities.LogFromNonGUIThread("File " + path + " no longer exists.");
            } catch (UnauthorizedAccessException) {
                CommonUtilities.LogFromNonGUIThread("Access denied for " + path);
            }

            return canRestoreFiles;
        }

        public static void ClearTmpDirectory() {
            ClearDirectory(tmpDirectory);
            storedFiles.Clear();
        }

        public static void ClearDirectory(string path) {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            foreach (FileInfo file in directoryInfo.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                dir.Delete(true);
        }


        #region Sorting functions
        public static void SortAlphabetically() {
            duplicatedFiles.Sort(Comparer<TupleOfFileEntryCollections>.Create((a, b) => string.Compare(a.Item1[0].Path, b.Item1[0].Path, StringComparison.InvariantCultureIgnoreCase)));
        }

        /// could do simpler sort as follows:
        /// duplicatedFiles.Sort(Comparer<Tuple<List<FileEntry>, List<FileEntry>>>.Create((a, b) => a.Item1[0].Size.CompareTo(b.Item1[0].Size)));
        public static void SortBySize(string baseDirectory = "") {
            bool sorted = false;
            while (!sorted) {
                try {
                    duplicatedFiles.Sort(Comparer<TupleOfFileEntryCollections>.Create((a, b) => {
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
                } catch (InvalidOperationException ex) {
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

        public static void SortAlphabeticallyPrimaryOnly() {
            duplicatedFilesPrimaryOnly.Sort(Comparer<FileEntryCollection>.Create((a, b) => string.Compare(a[0].Path, b[0].Path, StringComparison.InvariantCultureIgnoreCase)));
        }

        public static void SortBySizePrimaryOnly(string baseDirectory = "") {
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
                } catch (InvalidOperationException ex) {
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


        public static void RemoveAllEmptyDirectoriesPrimary(string baseDirectory = "") {
            RemoveProgressUpdated?.Invoke(new RemoveProgress() {
                MaxProgress = emptyDirectoriesPrimary.Count,
                State = RemoveProgressState.StartingRemoval
            });

            RemoveProgress progress = new RemoveProgress() {
                MaxProgress = emptyDirectoriesPrimary.Count,
                State = RemoveProgressState.Removing
            };

            for (int i = 0; i < emptyDirectoriesPrimary.Count; i++) {
                if (stopTask) {
                    AbortRemoveAllEmptyDirectoriesPrimary(i);
                    stopTask = false;
                    return;
                }
                RemoveFile(baseDirectory + emptyDirectoriesPrimary[i].Path);
                progress.Progress = i;
                RemoveProgressUpdated?.Invoke(progress);
            }
            emptyDirectoriesPrimary.Clear();
        }

        private static void AbortRemoveAllEmptyDirectoriesPrimary(int i) {
            emptyDirectoriesPrimary.RemoveRange(0, i);
        }

        public static void RemoveAllEmptyDirectoriesSecondary(string baseDirectory = "") {
            RemoveProgressUpdated?.Invoke(new RemoveProgress() {
                MaxProgress = emptyDirectoriesSecondary.Count,
                State = RemoveProgressState.StartingRemoval
            });

            RemoveProgress progress = new RemoveProgress() {
                MaxProgress = emptyDirectoriesSecondary.Count,
                State = RemoveProgressState.Removing
            };

            for (int i = 0; i < emptyDirectoriesSecondary.Count; i++) {
                if (stopTask) {
                    AbortRemoveAllEmptyDirectoriesSecondary(i);
                    stopTask = false;
                    return;
                }
                RemoveFile(baseDirectory + emptyDirectoriesSecondary[i].Path);
                progress.Progress = i;
                RemoveProgressUpdated?.Invoke(progress);
            }
            emptyDirectoriesSecondary.Clear();
        }

        private static void AbortRemoveAllEmptyDirectoriesSecondary(int i) {
            emptyDirectoriesSecondary.RemoveRange(0, i);
        }

        public static void RemoveAllEmptyFilesPrimary(string baseDirectory = "") {
            RemoveProgressUpdated?.Invoke(new RemoveProgress() {
                MaxProgress = emptyFilesPrimary.Count,
                State = RemoveProgressState.StartingRemoval
            });

            RemoveProgress progress = new RemoveProgress() {
                MaxProgress = emptyFilesPrimary.Count,
                State = RemoveProgressState.Removing
            };

            for (int i = 0; i < emptyFilesPrimary.Count; i++) {
                if (stopTask) {
                    AbortRemoveAllEmptyFilesPrimary(i);
                    stopTask = false;
                    return;
                }
                RemoveFile(baseDirectory + emptyFilesPrimary[i].Path);
                progress.Progress = i;
                RemoveProgressUpdated?.Invoke(progress);
            }
            emptyFilesPrimary.Clear();
        }

        private static void AbortRemoveAllEmptyFilesPrimary(int i) {
            emptyFilesPrimary.RemoveRange(0, i);
        }

        public static void RemoveAllEmptyFilesSecondary(string baseDirectory = "") {
            RemoveProgressUpdated?.Invoke(new RemoveProgress() {
                MaxProgress = emptyFilesSecondary.Count,
                State = RemoveProgressState.StartingRemoval
            });

            RemoveProgress progress = new RemoveProgress() {
                MaxProgress = emptyFilesSecondary.Count,
                State = RemoveProgressState.Removing
            };

            for (int i = 0; i < emptyFilesSecondary.Count; i++) {
                if (stopTask) {
                    AbortRemoveAllEmptyFilesSecondary(i);
                    stopTask = false;
                    return;
                }
                RemoveFile(baseDirectory + emptyFilesSecondary[i].Path);
                progress.Progress = i;
                RemoveProgressUpdated?.Invoke(progress);
            }
            emptyFilesSecondary.Clear();
        }

        private static void AbortRemoveAllEmptyFilesSecondary(int i) {
            emptyFilesSecondary.RemoveRange(0, i);
        }

        public static void RemoveAllPrimary(string baseDirectory = "") {
            RemoveProgressUpdated?.Invoke(new RemoveProgress() {
                MaxProgress = duplicatedFiles.Count,
                State = RemoveProgressState.StartingRemoval
            });

            RemoveProgress progress = new RemoveProgress() {
                MaxProgress = duplicatedFiles.Count,
                State = RemoveProgressState.Removing
            };

            for (int i = 0, progressIndex = 0; i < duplicatedFiles.Count; progressIndex++) {
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
                } else {
                    duplicatedFiles[i].Item1.Clear();
                    i++;
                }
                progress.Progress = progressIndex;
                RemoveProgressUpdated?.Invoke(progress);
            }
        }

        private static void AbortRemoveAllPrimary(int i) {
            if (duplicatedFiles[i].Item2.Count <= 1)
                duplicatedFiles.RemoveAt(i);
            else
                duplicatedFiles[i].Item1.Clear();
        }

        public static void RemoveAllSecondary(string baseDirectory = "") {
            RemoveProgressUpdated?.Invoke(new RemoveProgress() {
                MaxProgress = duplicatedFiles.Count,
                State = RemoveProgressState.StartingRemoval
            });

            RemoveProgress progress = new RemoveProgress() {
                MaxProgress = duplicatedFiles.Count,
                State = RemoveProgressState.Removing
            };

            for (int i = 0, progressIndex = 0; i < duplicatedFiles.Count; progressIndex++) {
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
                } else {
                    duplicatedFiles[i].Item2.Clear();
                    i++;
                }
                progress.Progress = progressIndex;
                RemoveProgressUpdated?.Invoke(progress);
            }
        }

        /// put abort functions as extension to observablecollection
        private static void AbortRemoveAllSecondary(int i) {
            if (duplicatedFiles[i].Item1.Count <= 1)
                duplicatedFiles.RemoveAt(i);
            else
                duplicatedFiles[i].Item2.Clear();
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
