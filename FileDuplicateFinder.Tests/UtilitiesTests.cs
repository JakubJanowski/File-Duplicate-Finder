using Xunit;
using Moq;
using System.IO;
using System.Reflection;
using System;
using System.Windows;

namespace FileDuplicateFinder.Tests {
    public class TestsFixture: IDisposable {
        public string TestDirectory { get; } = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))))), "test\\TEST");

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
            const string parentDirectory = "C:\\test\\parent";
            const string directory = "C:\\test\\parent\\directory";

            bool result = directory.IsSubdirectoryOf(parentDirectory);

            Assert.True(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnTrue_ForIndirectSubdirectory() {
            const string parentDirectory = "C:\\test\\parent";
            const string directory = "C:\\test\\parent\\directory\\subdirectory\\subsubdirectory";

            bool result = directory.IsSubdirectoryOf(parentDirectory);

            Assert.True(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnFalse_ForSiblingDirectory() {
            const string directory1 = "C:\\test\\parent\\directory";
            const string directory2 = "C:\\test\\parent\\sibling";

            bool result = directory1.IsSubdirectoryOf(directory2);

            Assert.False(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnFalse_ForNonSubdirectory() {
            const string directory1 = "C:\\test\\parent\\directory";
            const string directory2 = "C:\\test\\parent\\sibling\\subdirectory";

            bool result = directory2.IsSubdirectoryOf(directory1);

            Assert.False(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnFalse_ForParentDirectory() {
            const string parentDirectory = "C:\\test\\parent";
            const string directory = "C:\\test\\parent\\directory";

            bool result = parentDirectory.IsSubdirectoryOf(directory);

            Assert.False(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnFalse_ForSameDirectory() {
            const string directory = "C:\\test\\parent\\directory";

            bool result = directory.IsSubdirectoryOf(directory);

            Assert.False(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldReturnFalse_ForEmptyParentDirectory() {
            const string directory = "C:\\test\\parent\\directory";

            bool result = directory.IsSubdirectoryOf("");

            Assert.False(result);
        }

        [Fact]
        public void IsSubDirectoryOf_ShouldThrowArgumentNullException_ForNullArgument() {
            const string directory = "C:\\test\\parent\\directory";

            Assert.Throws<ArgumentNullException>(() => directory.IsSubdirectoryOf(null));
            Assert.Throws<ArgumentNullException>(() => Utilities.IsSubdirectoryOf(null, ""));
        }
        #endregion

        #region CheckDirectory tests

        [Fact]
        public void CheckDirectory_ShouldSetErrorToTrue_ForNonexistentDirectory() {
            const string directory = "C:\\test\\parent\\directory";
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
    }
}
