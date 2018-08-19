using FileDuplicateFinder.ViewModel;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;

namespace FileDuplicateFinder.View {
    public partial class MainWindowView: Window {
        internal MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();
        /// add to settings only
        private string tmpDirectory = AppDomain.CurrentDomain.BaseDirectory + "tmp/";
        
        public MainWindowView() {
            SetCulture();
            InitializeComponent();
            DataContext = ViewModel;

            ViewModel.DirectoryPickerViewModel = directoryPickerView.ViewModel;
            ViewModel.MainTabControlViewModel = mainTabControlView.ViewModel;
            ViewModel.StatusBarViewModel = statusBarView.ViewModel;
            ViewModel.tmpDirectory = tmpDirectory;
            
            directoryPickerView.StatusBarView = statusBarView;
            directoryPickerView.MainTabControlView = mainTabControlView;
            mainTabControlView.DirectoryPickerView = directoryPickerView;
            mainTabControlView.StatusBarView = statusBarView;
            mainTabControlView.MainWindowView = this;

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
            mainTabControlView.RefreshListViews();
        }

        private void basePathsCheckBox_Unchecked(object sender, RoutedEventArgs e) {
            ViewModel.ShowBasePaths = false;
            mainTabControlView.RefreshListViews();

        }
    }
}
