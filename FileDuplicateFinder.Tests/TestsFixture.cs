using System.IO;
using System.Reflection;
using System;
using System.Windows;

namespace FileDuplicateFinder.Tests {
    public class TestsFixture: IDisposable {
        public string TestDirectory { get; } = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))))), @"test\TEST");

        public TestsFixture() {
            if (Application.Current is null) {
                try {
                    new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                } catch (InvalidOperationException) { }
            }
        }

        public void Dispose() {
            try {
                Application.Current?.Shutdown();
            } catch(InvalidOperationException) { }
        }
    }
}
