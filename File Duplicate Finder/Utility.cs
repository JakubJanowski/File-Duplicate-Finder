using FileDuplicateFinder.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FileDuplicateFinder {
    static class Utility {

        internal static ListView logListView;
        internal static TabItem logTabItem;
        internal static Dispatcher dispatcher;
        internal static StatusBarViewModel statusBarViewModel;

        public static void CheckDirectories(string primaryDirectory, string secondaryDirectory, ref bool error) {
            try {
                Directory.GetAccessControl(primaryDirectory);
            }
            catch (UnauthorizedAccessException) {
                error = true;
                LogFromNonGUIThread("Primary directory is not accessible.");
            }
            catch {
                error = true;
                if (!Directory.Exists(primaryDirectory))
                    LogFromNonGUIThread("Primary directory does not exist.");
                else
                    LogFromNonGUIThread("Unknown error in primary directory.");
            }

            try {
                Directory.GetAccessControl(secondaryDirectory);
            }
            catch (UnauthorizedAccessException) {
                error = true;
                LogFromNonGUIThread("Secondary directory is not accessible.");
            }
            catch {
                error = true;
                if (!Directory.Exists(secondaryDirectory))
                    LogFromNonGUIThread("Secondary directory does not exist.");
                else
                    LogFromNonGUIThread("Unknown error in secondary directory.");
            }
        }
        public static bool IsSubDirectoryOf(this string candidate, string other) {
            var isChild = false;
            try {
                var candidateInfo = new DirectoryInfo(candidate);
                var otherInfo = new DirectoryInfo(other);

                while (candidateInfo.Parent != null) {
                    if (candidateInfo.Parent.FullName == otherInfo.FullName) {
                        isChild = true;
                        break;
                    }
                    else candidateInfo = candidateInfo.Parent;
                }
            }
            catch (Exception error) {
                var message = String.Format("Unable to check directories {0} and {1}: {2}", candidate, other, error);
                Trace.WriteLine(message);
            }

            return isChild;
        }

        public static string NormalizePath(string path) {
            try {
                path = Path.GetFullPath(new Uri(path).LocalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch { }

            if (path.Length > 0 && path.Last() == ':')
                path += '\\';
            return path;
        }

        public static void Log(string message) {
            int i = logListView.Items.Add(message);
            logListView.ScrollIntoView(logListView.Items[i]);
            logTabItem.IsSelected = true;
        }

        public static void LogFromNonGUIThread(string message) {
            dispatcher.BeginInvoke((Action)(() => Log(message)));
        }
        
        public static void BeginInvoke(Action callback) {
            dispatcher.BeginInvoke(callback);
        }

        public static string PrettyPrintSize(long bytes) {
            string[] sizes = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };
            int order = 0;
            int remainder = 0;

            while (bytes >= 1024 * 1024 && order < sizes.Length - 1) {
                order++;
                bytes = bytes / 1024;
            }
            if (bytes >= 1024 && order < sizes.Length - 1) {
                order++;
                remainder = (int)bytes % 1024;
                bytes = bytes / 1024;
            }

            double size = bytes + ((double)remainder / 1024);
            string digits;

            if (order == 0)
                digits = String.Format("{0:0}", size);
            else
                digits = String.Format("{0:0.00}", size);

            if(digits.Length >= 5) {
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
        public static bool Remove(this ObservableRangeCollection<FileEntry> collection, string path) {
            for (int i = 0; i < collection.Count; i++) {
                if (collection[i].Path.Equals(path)) {
                    collection.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}
