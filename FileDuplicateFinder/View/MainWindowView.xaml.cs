using FileDuplicateFinder.Services;
using FileDuplicateFinder.ViewModel;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace FileDuplicateFinder.View {
    public partial class MainWindowView: Window {
        private MainWindowViewModel ViewModel { get => DataContext as MainWindowViewModel; }

        public MainWindowView() {
            SetCulture();
            InitializeComponent();

            Utilities.dispatcher = Dispatcher;
            FileManager.dispatcher = Dispatcher;
        }

        private void SetCulture() {
            System.Diagnostics.Debug.WriteLine("The current culture is {0}", Thread.CurrentThread.CurrentCulture.Name);
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-UK");
            System.Diagnostics.Debug.WriteLine("The new culture is {0}", Thread.CurrentThread.CurrentCulture.Name);
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            ViewModel.WindowClosing();
        }
    }
}
