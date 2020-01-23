using FileDuplicateFinder.ViewModel;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;

namespace FileDuplicateFinder.View {
    public partial class MainWindowView: Window {
        internal MainWindowViewModel ViewModel { get; }
        /// add to settings only and make modifiable
        private string tmpDirectory = AppDomain.CurrentDomain.BaseDirectory + "tmp/";
        
        public MainWindowView() {
            SetCulture();
            InitializeComponent();

            ViewModel = DataContext as MainWindowViewModel;

            ViewModel.StatusBarViewModel = statusBarView.ViewModel;
            ViewModel.tmpDirectory = tmpDirectory;

            ViewModel.DirectoryPickerViewModel.StatusBarViewModel = statusBarView.ViewModel;
            ViewModel.MainTabControlViewModel.StatusBarViewModel = statusBarView.ViewModel;

            Utility.statusBarViewModel = statusBarView.ViewModel;
            Utility.dispatcher = Dispatcher;
            
            FileManager.statusBarViewModel = statusBarView.ViewModel;
            FileManager.tmpDirectory = tmpDirectory;
            FileManager.dispatcher = Dispatcher;

            Directory.CreateDirectory(tmpDirectory);
        }

        private void SetCulture() {
            System.Diagnostics.Debug.WriteLine("The current culture is {0}", Thread.CurrentThread.CurrentCulture.Name);
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-UK");
            System.Diagnostics.Debug.WriteLine("The new culture is {0}", Thread.CurrentThread.CurrentCulture.Name);
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            ViewModel.WindowClosing();
        }

        private void basePathsCheckBox_Checked(object sender, RoutedEventArgs e) {
            ViewModel.ShowBasePaths = true;
        }

        private void basePathsCheckBox_Unchecked(object sender, RoutedEventArgs e) {
            ViewModel.ShowBasePaths = false;

        }
    }
}
