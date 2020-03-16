using FileDuplicateFinder.Services;
using FileDuplicateFinder.ViewModel;
using Moq;
using System;
using System.IO;
using System.Linq;
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

            FileManager.Initialize();
            FileManager.FindDuplicatedFiles(testDirectory, false);

            Assert.Equal(paths.Length, FileManager.emptyFilesPrimary.Count);
            for (int i = 0; i < paths.Length; i++)
                Assert.Equal(paths[i], FileManager.emptyFilesPrimary[i].Path);
        }

        [Fact]
        public void FindDuplicatedFiles_ShouldFindEmptyDirectories() {
            string[] paths = {
                @"\1\dir\empty dir",
                @"\2\empty dir"
            };
            FileManager.statusBarViewModel = new Mock<StatusBarViewModel>().Object;

            FileManager.Initialize();
            FileManager.FindDuplicatedFiles(testDirectory, false);

            Assert.Equal(paths.Length, FileManager.emptyDirectoriesPrimary.Count);
            for (int i = 0; i < paths.Length; i++)
                Assert.Equal(paths[i], FileManager.emptyDirectoriesPrimary[i].Path);
        }

        [Fact]
        public void FindDuplicatedFiles_ShouldFindDuplicatedFiles() {
            string[][] paths = {
                new string[] {
                    @"\1\abc - Copy.txt",
                    @"\1\abc.txt",
                    @"\2\text.doc"
                },
                new string[] {
                    @"\1\dir\2MiB1.bin",
                    @"\2\2MiB1.bin"
                }
            };
            FileManager.statusBarViewModel = new Mock<StatusBarViewModel>().Object;

            FileManager.Initialize();
            FileManager.FindDuplicatedFiles(testDirectory, false);

            Assert.Equal(paths.Length, FileManager.duplicatedFilesPrimaryOnly.Count);
            for (int group = 0; group < paths.Length; group++) {
                Assert.Equal(paths[group].Length, FileManager.duplicatedFilesPrimaryOnly[group].Count);
                Assert.Single(FileManager.duplicatedFilesPrimaryOnly[group].Select(f => f.Size).Distinct());

                for (int i = 0; i < paths[group].Length; i++)
                    Assert.Equal(paths[group][i], FileManager.duplicatedFilesPrimaryOnly[group][i].Path);
            }
        }
        #endregion
    }
}
