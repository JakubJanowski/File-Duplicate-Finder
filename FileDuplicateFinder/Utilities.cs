using FileDuplicateFinder.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.AccessControl;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FileDuplicateFinder {
    public static class Utilities {

        public static ListView logListView;
        public static TabItem logTabItem;
        public static StatusBarViewModel statusBarViewModel;

        public static void CheckDirectory(string directory, SearchDirectoryType searchDirectoryType, ref bool error) {
            try {
                ///Directory.GetAccessControl(directory);
                if (Directory.Exists(directory)) {
                    new DirectorySecurity(directory, AccessControlSections.Owner | AccessControlSections.Group | AccessControlSections.Access);
                } else {
                    error = true;
                    LogFromNonGUIThread($"{searchDirectoryType.ToString()} directory does not exist.");
                }
            } catch (UnauthorizedAccessException) {
                error = true;
                LogFromNonGUIThread($"{searchDirectoryType.ToString()} directory is not accessible.");
            } catch (SystemException e) when (e is ArgumentNullException
                                           || e is IOException
                                           || e is PlatformNotSupportedException
                                           || e is SystemException) {
                error = true;
                LogFromNonGUIThread($"Unknown error in {searchDirectoryType.ToString().ToLower(CultureInfo.CurrentCulture)} directory.");
            }
        }

        public static void CheckDirectories(string primaryDirectory, string secondaryDirectory, ref bool error) {
            CheckDirectory(primaryDirectory, SearchDirectoryType.Primary, ref error);
            CheckDirectory(secondaryDirectory, SearchDirectoryType.Secondary, ref error);
        }

        /// <summary>
        /// Checks whether a path represented by this string is a subdirectory (direct or nested) of specified parent direcory.
        /// </summary>
        /// <param name="candidate">path to directory that will be checked</param>
        /// <param name="parent">parent directory</param>
        /// <returns><see langword="true"/> if path is a subdirectory of parent, <see langword="false"/> otherwise</returns>
        /// <exception cref="ArgumentNullException">candidate or parent is null</exception>
        public static bool IsSubdirectoryOf(this string candidate, string parent) {
            if (candidate is null)
                throw new ArgumentNullException(nameof(candidate));
            if (parent is null)
                throw new ArgumentNullException(nameof(parent));

            bool isChild = false;
            try {
                DirectoryInfo candidateInfo = new DirectoryInfo(candidate);
                DirectoryInfo parentInfo = new DirectoryInfo(parent);

                while (candidateInfo.Parent != null) {
                    if (candidateInfo.Parent.FullName == parentInfo.FullName) {
                        isChild = true;
                        break;
                    } else
                        candidateInfo = candidateInfo.Parent;
                }
            } catch (Exception error) when (error is ArgumentNullException
                                         || error is SecurityException
                                         || error is ArgumentException
                                         || error is PathTooLongException) {
                Trace.WriteLine($"Unable to check directories {candidate} and {parent}: {error}");
            }

            return isChild;
        }

        /// <summary>
        /// Normalizes a path, so that all paths that point to the same directory will be equal after normalization.
        /// </summary>
        /// <param name="path">Path to a directory</param>
        /// <returns>Normalized path</returns>
        public static string NormalizeDirectoryPath(string path) {
            try {
                path = Path.GetFullPath(new Uri(path).LocalPath);
            } catch (Exception error) when (error is ArgumentException
                                         || error is ArgumentNullException
                                         || error is InvalidOperationException
                                         || error is NotSupportedException
                                         || error is PathTooLongException
                                         || error is SecurityException
                                         || error is UriFormatException) {
            }

            if (path?.Length > 0 && path.Last() != Path.DirectorySeparatorChar)
                path += Path.DirectorySeparatorChar;
            return path;
        }

        public static void Log(string message) {
            int i = logListView.Items.Add(message);
            logListView.ScrollIntoView(logListView.Items[i]);
            logTabItem.IsSelected = true;
        }

        public static void LogFromNonGUIThread(string message) {
            BeginInvoke(() => Log(message));
        }

        public static DispatcherOperation BeginInvoke(Action callback) {
            return Application.Current.Dispatcher.BeginInvoke(callback);
        }

        public static string PrettyPrintSize(long bytes) {
            if (bytes < 0)
                throw new ArgumentException();

            string[] sizes = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };
            int order = 0;
            int remainder = 0;

            while (bytes >= 1024 * 1024 && order < sizes.Length - 1) {
                order++;
                bytes /= 1024;
            }
            if (bytes >= 1024 && order < sizes.Length - 1) {
                order++;
                remainder = (int)bytes % 1024;
                bytes /= 1024;
            }

            double size = bytes + ((double)remainder / 1024);
            string digits;

            if (order == 0)
                digits = string.Format("{0:0}", size);
            else
                digits = string.Format("{0:0.00}", size);

            if (digits.Length >= 5) {
                if (new string(new char[] { digits[3] }).Equals(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator))
                    digits = digits.Substring(0, 3);
                else
                    digits = digits.Substring(0, 4);
            }
            return digits + " " + sizes[order];
        }

        public static string PrettyPrintSizeOptimised(long bytes) {
            throw new NotImplementedException();

            //const long B = 1, KiB = 1024, MiB = KiB * 1024, GiB = MiB * 1024, TiB = GiB * 1024, PiB = TiB * 1024, EiB = PiB * 1024;
            //double size;
            //string suffix;

            //if (bytes < KiB) {
            //    size = bytes;
            //    remainder = 0;
            //    suffix = nameof(B);
            //}
            //else if (bytes < MiB) {
            //    remainder = (ushort)(bytes % 1024);   //x % 1024 === x & 0x3ff
            //    size = bytes / 1024;
            //    suffix = nameof(KiB);
            //}
            //else if (bytes < GiB) {
            //    bytes /= KiB;
            //    remainder = (ushort)(bytes % 1024);
            //    size = bytes / 1024;
            //    suffix = nameof(MiB);
            //}
            //else if (bytes < TiB) {
            //    bytes /= MiB;
            //    remainder = (ushort)(bytes % 1024);
            //    size = bytes / 1024;
            //    suffix = nameof(GiB);
            //}

            //else if (bytes < PiB) {
            //    size = bytes / TiB + (bytes % TiB) / 1024;
            //    suffix = nameof(TiB);
            //}
            //else if (bytes < EiB) {
            //    size = bytes / PiB + (bytes % PiB) / 1024;
            //    suffix = nameof(PiB);
            //}
            //else {
            //    size = bytes / EiB + (bytes % EiB) / 1024;
            //    suffix = nameof(EiB);
            //}
        }

        /// IEnumerable?
        /// 
        ///Returns true if an item was removed
        public static bool Remove(this ObservableRangeCollection<FileEntry> collection, string path) {
            for (int i = 0; i < collection.Count; i++) {
                if (collection[i].Path.Equals(path, StringComparison.Ordinal)) {
                    collection.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}
