using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.AccessControl;

namespace FileDuplicateFinder.Utilities {
    public static class FileUtilities {
        public static void CheckDirectory(string directory, SearchDirectoryType searchDirectoryType, ref bool error) {
            try {
                ///Directory.GetAccessControl(directory);
                if (Directory.Exists(directory)) {
                    new DirectorySecurity(directory, AccessControlSections.Owner | AccessControlSections.Group | AccessControlSections.Access);
                } else {
                    error = true;
                    CommonUtilities.LogFromNonGUIThread($"{searchDirectoryType.ToString()} directory does not exist.");
                }
            } catch (UnauthorizedAccessException) {
                error = true;
                CommonUtilities.LogFromNonGUIThread($"{searchDirectoryType.ToString()} directory is not accessible.");
            } catch (SystemException e) when (e is ArgumentNullException
                                           || e is IOException
                                           || e is PlatformNotSupportedException
                                           || e is SystemException) {
                error = true;
                CommonUtilities.LogFromNonGUIThread($"Unknown error in {searchDirectoryType.ToString().ToLower(CultureInfo.CurrentCulture)} directory.");
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
    }
}
