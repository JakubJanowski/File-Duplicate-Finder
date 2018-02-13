using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;

namespace File_Duplicate_Finder {
    static class Utility {

        public static ListView listView;
        public static void CheckDirectories(string primaryDirectory, string secondaryDirectory, ref bool error, Dispatcher dispatcher) {
            try {
                Directory.GetAccessControl(primaryDirectory);
            }
            catch (UnauthorizedAccessException) {
                error = true;
                dispatcher.Invoke(() => listView.Items.Add("Primary directory is not accessible."));
            }
            catch {
                error = true;
                if (!Directory.Exists(primaryDirectory))
                    dispatcher.Invoke(() => listView.Items.Add("Primary directory does not exist."));
                else
                    dispatcher.Invoke(() => listView.Items.Add("Unknown error in primary directory."));
            }

            try {
                Directory.GetAccessControl(secondaryDirectory);
            }
            catch (UnauthorizedAccessException) {
                error = true;
                dispatcher.Invoke(() => listView.Items.Add("Secondary directory is not accessible."));
            }
            catch {
                error = true;
                if (!Directory.Exists(secondaryDirectory))
                    dispatcher.Invoke(() => listView.Items.Add("Secondary directory does not exist."));
                else
                    dispatcher.Invoke(() => listView.Items.Add("Unknown error in secondary directory."));
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
                return Path.GetFullPath(new Uri(path).LocalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch {
                return path;
            }
        }
    }
}
