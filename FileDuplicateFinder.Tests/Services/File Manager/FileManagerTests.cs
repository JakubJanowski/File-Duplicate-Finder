using FileDuplicateFinder.Tests;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FileDuplicateFinder.Services.Tests {
    public class FileManagerTests: IClassFixture<TestsFixture> {
        private readonly string testDirectory;

        public FileManagerTests(TestsFixture data) {
            testDirectory = data.TestDirectory;
        }

        #region FindDuplicatedFiles tests
        [Fact]
        public void FindDuplicatedFiles_ShouldFindEmptyFiles() {
            string[] paths = {
                @"\1\dir\empty file.txt",
                @"\2\empty file.bmp"
            };

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
                    @"\1\1MiB.dat",
                    @"\2\1MiB.dat"
                },
                new string[] {
                    @"\1\dir\2MiB1.bin",
                    @"\2\2MiB1.bin"
                }
            };

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

        [Fact]
        public void FindDuplicatedFiles_ShouldReportProgressStates() {
            Mock<FileManager.SearchProgressUpdatedEventHandler> handler = new Mock<FileManager.SearchProgressUpdatedEventHandler>();
            FileManager.SearchProgressUpdated += handler.Object;

            FileManager.Initialize();
            FileManager.FindDuplicatedFiles(testDirectory, false);

            handler.Verify(h => h(It.Is<DuplicateSearchProgress>(p => p.State == DuplicateSearchProgressState.Processing)));
            handler.Verify(h => h(It.Is<DuplicateSearchProgress>(p => p.State == DuplicateSearchProgressState.Sorting)));
            handler.Verify(h => h(It.Is<DuplicateSearchProgress>(p => p.State == DuplicateSearchProgressState.StartingSearch)), Times.Once);
            handler.Verify(h => h(It.Is<DuplicateSearchProgress>(p => p.State == DuplicateSearchProgressState.Searching)));
        }

        [Fact]
        public void FindDuplicatedFiles_ShouldReportNumberOfFiles_ForStartingSearchState() {
            Mock<FileManager.SearchProgressUpdatedEventHandler> handler = new Mock<FileManager.SearchProgressUpdatedEventHandler>();
            FileManager.SearchProgressUpdated += handler.Object;

            FileManager.Initialize();
            FileManager.FindDuplicatedFiles(testDirectory, false);

            handler.Verify(h => h(It.Is<DuplicateSearchProgress>(p => p.State == DuplicateSearchProgressState.StartingSearch && p.MaxProgress > 0)), Times.Once);
        }

        [Fact]
        public void FindDuplicatedFiles_ShouldReportGrowingProgress_ForSearchingState() {
            Mock<FileManager.SearchProgressUpdatedEventHandler> handler = new Mock<FileManager.SearchProgressUpdatedEventHandler>();
            List<int> progressList = new List<int>();
            int maxProgress = 0;
            handler.Setup(h => h(It.IsAny<DuplicateSearchProgress>())).Callback<DuplicateSearchProgress>(p => {
                if (p.State == DuplicateSearchProgressState.StartingSearch)
                    maxProgress = p.MaxProgress;
                else if (p.State == DuplicateSearchProgressState.Searching)
                    progressList.Add(p.Progress);
            });
            FileManager.SearchProgressUpdated += handler.Object;

            FileManager.Initialize();
            FileManager.FindDuplicatedFiles(testDirectory, false);

            Assert.True(progressList.Zip(progressList.Skip(1), (a, b) => a < b).All(a => a));
            Assert.True(progressList.First() > 0);
            Assert.True(progressList.Last() <= maxProgress);
        }
        #endregion
    }
}
