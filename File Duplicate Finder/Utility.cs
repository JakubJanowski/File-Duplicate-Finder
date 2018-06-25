// log from thread
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FileDuplicateFinder {
    static class Utility {

        internal static ListView logListView;
        internal static TabItem logTabItem;
        internal static Dispatcher dispatcher;
        internal static TextBlock progressTextBlock;

        public static void CheckDirectories(string primaryDirectory, string secondaryDirectory, ref bool error) {
            try {
                Directory.GetAccessControl(primaryDirectory);
            }
            catch (UnauthorizedAccessException) {
                error = true;
                dispatcher.Invoke(() => Log("Primary directory is not accessible."));
            }
            catch {
                error = true;
                if (!Directory.Exists(primaryDirectory))
                    dispatcher.Invoke(() => Log("Primary directory does not exist."));
                else
                    dispatcher.Invoke(() => Log("Unknown error in primary directory."));
            }

            try {
                Directory.GetAccessControl(secondaryDirectory);
            }
            catch (UnauthorizedAccessException) {
                error = true;
                dispatcher.Invoke(() => Log("Secondary directory is not accessible."));
            }
            catch {
                error = true;
                if (!Directory.Exists(secondaryDirectory)) {
                    dispatcher.Invoke(() => Log("Secondary directory does not exist."));
                }
                else {
                    dispatcher.Invoke(() => Log("Unknown error in secondary directory."));
                }
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
            dispatcher.Invoke(() => {
                int i = logListView.Items.Add(message);
                logListView.ScrollIntoView(logListView.Items[i]);
                logTabItem.IsSelected = true;
            });
        }

        public static string PrettyPrintSize(long bytes) {
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB" };
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
            return String.Format("{0:0.##} {1}", size, sizes[order]);
        }
        
        public static void SetProgress(int done, int outOf) {
            progressTextBlock.Text = done + " / " + outOf;
        }

        public static bool Remove(this List<FileEntry> list, string path) {
            for (int i = 0; i < list.Count; i++) {
                if (list[i].Path.Equals(path)) {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}
