using Xunit;
using System.IO;
using System.Reflection;
using System;
using System.Windows;
using System.Linq;

namespace FileDuplicateFinder.Tests {
    public class TestsFixture: IDisposable {
        public string TestDirectory { get; } = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))))), @"test\TEST");

        public TestsFixture() {
            if (Application.Current is null)
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }

        public void Dispose() {
            Application.Current.Shutdown();
        }
    }

    public class UtilitiesTests: IClassFixture<TestsFixture> {
        private readonly string testDirectory;

        public UtilitiesTests(TestsFixture data) {
            testDirectory = data.TestDirectory;
        }

        #region IsSubDirectoryOf tests
        [Fact]
        public void IsSubDirectoryOf_ShouldReturnTrue_ForDirectSubdirectory() {
            const string parentDirectory = @"C:\test\parent";
            const string directory = @"C:\test\parent\directory";

            bool result = directory.IsSubdirectoryOf(parentDirectory);

            Assert.True(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnTrue_ForIndirectSubdirectory() {
            const string parentDirectory = @"C:\test\parent";
            const string directory = @"C:\test\parent\directory\subdirectory\subsubdirectory";

            bool result = directory.IsSubdirectoryOf(parentDirectory);

            Assert.True(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnFalse_ForSiblingDirectory() {
            const string directory1 = @"C:\test\parent\directory";
            const string directory2 = @"C:\test\parent\sibling";

            bool result = directory1.IsSubdirectoryOf(directory2);

            Assert.False(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnFalse_ForNonSubdirectory() {
            const string directory1 = @"C:\test\parent\directory";
            const string directory2 = @"C:\test\parent\sibling\subdirectory";

            bool result = directory2.IsSubdirectoryOf(directory1);

            Assert.False(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnFalse_ForParentDirectory() {
            const string parentDirectory = @"C:\test\parent";
            const string directory = @"C:\test\parent\directory";

            bool result = parentDirectory.IsSubdirectoryOf(directory);

            Assert.False(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnFalse_ForSameDirectory() {
            const string directory = @"C:\test\parent\directory";

            bool result = directory.IsSubdirectoryOf(directory);

            Assert.False(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnFalse_ForEmptyParentDirectory() {
            const string directory = @"C:\test\parent\directory";

            bool result = directory.IsSubdirectoryOf("");

            Assert.False(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldThrowArgumentNullException_ForNullArgument() {
            const string directory = @"C:\test\parent\directory";

            Assert.Throws<ArgumentNullException>(() => directory.IsSubdirectoryOf(null));
            Assert.Throws<ArgumentNullException>(() => Utilities.IsSubdirectoryOf(null, ""));
        }
        #endregion

        #region CheckDirectory tests

        [Fact]
        public void CheckDirectory_ShouldSetErrorToTrue_ForNonexistentDirectory() {
            const string directory = "C:\\test\\non_existent_path\\directory";
            bool result = false;

            Utilities.CheckDirectory(directory, SearchDirectoryType.Primary, ref result);

            Assert.True(result);
        }

        [Fact]
        public void CheckDirectory_ShouldNotSetError_ForExistingDirectory() {
            bool result1 = false;
            bool result2 = true;

            Utilities.CheckDirectory(testDirectory, SearchDirectoryType.Primary, ref result1);
            Utilities.CheckDirectory(testDirectory, SearchDirectoryType.Primary, ref result2);

            Assert.False(result1);
            Assert.True(result2);
        }

        #endregion

        #region CheckDirectories tests
        [Fact]
        public void CheckDirectories_ShouldSetErrorToTrue_ForNonexistentPrimaryDirectory() {
            string directory1 = "C:\\test\\non_existent_path\\directory";
            string directory2 = Path.Combine(testDirectory, "2");
            bool result = false;

            Utilities.CheckDirectories(directory1, directory2, ref result);

            Assert.True(result);
        }

        [Fact]
        public void CheckDirectories_ShouldSetErrorToTrue_ForNonexistentSecondaryDirectory() {
            string directory1 = Path.Combine(testDirectory, "1");
            string directory2 = "C:\\test\\non_existent_path\\directory";
            bool result = false;

            Utilities.CheckDirectories(directory1, directory2, ref result);

            Assert.True(result);
        }

        [Fact]
        public void CheckDirectories_ShouldNotSetError_ForExistingDirectories() {
            string directory1 = Path.Combine(testDirectory, "1");
            string directory2 = Path.Combine(testDirectory, "2");
            bool result1 = false;
            bool result2 = true;

            Utilities.CheckDirectories(directory1, directory2, ref result1);
            Utilities.CheckDirectories(directory1, directory2, ref result2);

            Assert.False(result1);
            Assert.True(result2);
        }
        #endregion

        #region NormalizePath tests
        [Fact]
        public void NormalizePath_ShouldReturnIdenticalStrings_ForDifferentlyFormatedPathsToTheSameFile() {
            string[] paths = new string[] {
                @"C:\test\parent\directory\file.txt",
                @"C:/test/parent/directory/file.txt",
                @"C:\\\test/parent\directory////file.txt",
                @"   C:/test/parent/directory/file.txt   ",
                "\t \tC:/test/parent/directory/file.txt  \t",
                @"C:/test/parent/../parent/./directory/file.txt"
            };

            string[] results = paths.Select(p => Utilities.NormalizePath(p)).ToArray();

            Assert.Single(results.Distinct());
        }

        [Fact]
        public void NormalizePath_ShouldReturnIdenticalStrings_ForDifferentlyFormatedPathsToTheSameDirectory() {
            string[] paths = new string[] {
                @"C:\test\parent\directory",
                @"C:/test/parent/directory",
                @"C:\test\parent\directory\",
                @"C:/test/parent/directory/",
                @"C:\test\parent\directory\.",
                @"C:/test/parent/directory/.",
                @"C:\test\parent\directory\\\",
                @"C:/test/parent/directory///",
                @"C:\test\parent\directory\subdirectory\..",
                @"C:\test\parent\directory\subdirectory\..\",
            };

            string[] results = paths.Select(p => Utilities.NormalizePath(p)).ToArray();

            Assert.Single(results.Distinct());
        }

        [Fact]
        public void NormalizePath_ShouldReturnDifferentStrings_ForDifferentPaths() {
            string[] paths = new string[] {
                @"C:\test\parent\directory",
                @"C:\test\parent\directory\file.txt",
                @"C:\test\parent\directory\otherfile.txt",
                @"C:\test\parent\directory\subdirectory",
                @"C:\test\parent\otherdirectory",
                @"C:\test\parent\otherdirectory\file.txt",
            };

            string[] results = paths.Select(p => Utilities.NormalizePath(p)).ToArray();

            Assert.Equal(results.Length, results.Distinct().Count());
        }

        [Fact]
        public void NormalizePath_ShouldReturnTheSameString_ForIncorrectlyFormattedPaths() {
            string[] paths = new string[] {
                null,
                @"",
                @"\\",
                @"\\.",
                @"\\..",
                @"\\file.txt",
                @"directory\\file.txt"
            };

            string[] results = paths.Select(p => Utilities.NormalizePath(p)).ToArray();

            for (int i = 0; i < results.Length; i++)
                Assert.Equal(paths[i], results[i]);
        }

        [Fact]
        public void NormalizePath_ShouldEnsureTrailingSlash_ForDrivePath() {
            string[] pathsWithoutSlash = new string[] {
                @"A:",
                @"C:",
                @"D:",
                @"V:"
            };

            string[] pathsWithSlash = new string[] {
                @"B:\",
                @"E:\",
                @"S:\",
                @"Z:\"
            };

            string[] results1 = pathsWithoutSlash.Select(p => Utilities.NormalizePath(p)).ToArray();
            string[] results2 = pathsWithSlash.Select(p => Utilities.NormalizePath(p)).ToArray();

            for (int i = 0; i < results1.Length; i++)
                Assert.Equal(pathsWithoutSlash[i] + @"\", results1[i]);
            for (int i = 0; i < results2.Length; i++)
                Assert.Equal(pathsWithSlash[i], results2[i]);
        }
        #endregion

        #region PrettyPrintSize tests
        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(1, "1 B")]
        [InlineData(25, "25 B")]
        [InlineData(512, "512 B")]
        [InlineData(1023, "1023 B")]
        [InlineData(1024, "1,00 KiB")]
        [InlineData(1029, "1,00 KiB")]
        [InlineData(1030, "1,01 KiB")]
        [InlineData(1300, "1,27 KiB")]
        [InlineData(1000000, "976 KiB")]
        [InlineData(1048576, "1,00 MiB")]
        [InlineData(129452999, "123 MiB")]
        [InlineData(1325607178, "1,23 GiB")]
        [InlineData(21474836480, "20,0 GiB")]
        [InlineData(22441204122, "20,9 GiB")]
        [InlineData(549755813888000, "500 TiB")]
        [InlineData(1125899906842624000, "1000 PiB")]
        [InlineData(long.MaxValue, "8,00 EiB")]
        public void PrettyPrintSize_ShouldReturnCOrrectStrings(long bytes, string expectedResult) {
            string result = Utilities.PrettyPrintSize(bytes);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void PrettyPrintSize_ShouldThrowArgumentException_ForNegativeBytes() {
            const long bytes = -123;

            Assert.Throws<ArgumentException>(() =>  Utilities.PrettyPrintSize(bytes));
        }
        #endregion
    }
}
