using FileDuplicateFinder.Services;
using FileDuplicateFinder.ViewModel;
using Moq;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Xunit;

namespace FileDuplicateFinder.Tests {
    public class FileManagerTestsFixture: IDisposable {
        public string TestDirectory { get; } = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))))), @"test\TEST");
        public FileManagerTestsFixture() {
            if (Application.Current is null) {
                try {
                    new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                } catch (InvalidOperationException) { }
            }
        }

        public void Dispose() {
            try {
                Application.Current?.Shutdown();
            } catch (InvalidOperationException) { }
        }
    }

    public class FileManagerTests: IClassFixture<FileManagerTestsFixture> {
        private readonly string testDirectory;

        public FileManagerTests(FileManagerTestsFixture data) {
            testDirectory = data.TestDirectory;
        }

        #region FindDuplicatedFiles tests
        [Fact]
        public void FindDuplicatedFiles_ShouldFindEmptyFiles() {
            string[] paths = {
                @"\1\dir\empty file.txt",
                @"\2\empty file.bmp"
            };
            FileManager.statusBarViewModel = new Mock<StatusBarViewModel>().Object;

            FileManager.FindDuplicatedFiles(testDirectory, false);

            Assert.Equal(2, FileManager.emptyFilesPrimary.Count);
            for (int i = 0; i < 2; i++)
                Assert.Equal(paths[i], FileManager.emptyFilesPrimary[i].Path);
        }
        #endregion
    }
}
