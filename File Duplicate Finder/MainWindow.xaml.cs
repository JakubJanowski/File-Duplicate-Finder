using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace FileDuplicateFinder {
    public partial class MainWindow: Window {
        private volatile bool stopTask = false;
        //get this from settings 
        private bool showBasePaths = false;
        private bool backupFiles = true;
        private bool askLarge = true;
        private string primaryDirectory;
        private string secondaryDirectory;
        private string tmpDirectory = AppDomain.CurrentDomain.BaseDirectory + "tmp/";

        public bool SortBySize { get; set; } = true;
        public bool SortBySizePrimaryOnly { get; set; } = true;

        public bool ShowBasePaths {
            get => showBasePaths;
            set {
                if (showBasePaths != value) {
                    showBasePaths = value;
                    mainTabControlView.ViewModel.ShowBasePaths = value;
                    if (value)
                        ShowBasePathsChecked();
                    else
                        ShowBasePathsUnchecked();
                    ///OnPropertyChanged("ShowBasePaths");
                }
            }
        }

        public MainWindow() {
            SetCulture();
            InitializeComponent();

            DataContext = this;
            //StatusBarView statusBarView = new StatusBarView();
            //DirectoryPickerView directoryPickerView = new DirectoryPickerView(statusBarView);
            directoryPickerView.StatusBarView = statusBarView;
            directoryPickerView.MainTabControlView = mainTabControlView;
            mainTabControlView.DirectoryPickerView = directoryPickerView;
            mainTabControlView.StatusBarView = statusBarView;

            Utility.statusBarView = statusBarView;
            Utility.dispatcher = Dispatcher;
            
            FileManager.statusBarView = statusBarView;
            FileManager.tmpDirectory = tmpDirectory;
            FileManager.dispatcher = Dispatcher;

            Directory.CreateDirectory(tmpDirectory);
        }

        private void SetCulture() {
            System.Diagnostics.Debug.WriteLine("The current culture is {0}", Thread.CurrentThread.CurrentCulture.Name);
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-UK");
            System.Diagnostics.Debug.WriteLine("The new culture is {0}", Thread.CurrentThread.CurrentCulture.Name);
            //var culture = new CultureInfo("en-US");
            //CultureInfo.DefaultThreadCurrentCulture = culture;
            //CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        private void FindDuplicatedFiles(object sender, RoutedEventArgs e) {
            InitializeDuplicateFinding();

            if (directoryPickerView.PrimaryOnly)
                new Thread(FindDuplicatedFilesInPrimaryOnly).Start();
            else
                new Thread(FindDuplicatedFilesInBoth).Start();
        }

        private void FindDuplicatedFilesInPrimaryOnly() {
            bool error = false;
            // chk empty or null string please specify directory to search in
            primaryDirectory = Utility.NormalizePath(primaryDirectory);

            try {
                Directory.GetAccessControl(primaryDirectory);
            }
            catch (UnauthorizedAccessException) {
                error = true;
                Utility.LogFromNonGUIThread("Primary directory is not accessible.");
            }
            catch {
                error = true;
                if (!Directory.Exists(primaryDirectory))
                    Utility.LogFromNonGUIThread("Primary directory does not exist.");
                else
                    Utility.LogFromNonGUIThread("Unknown error in primary directory.");
            }

            if (error) {
                Dispatcher.BeginInvoke((Action)(() => {///
                    statusBarView.ViewModel.State = "Failed";
                    statusBarView.ViewModel.ShowProgress = false;
                    UnlockGUI();
                }));
                return;
            }

            FileManager.FindDuplicatedFiles(primaryDirectory, ShowBasePaths);

            Dispatcher.BeginInvoke((Action)(() => {
                if (!SortBySizePrimaryOnly)
                    FileManager.duplicatedFilesPrimaryOnly.Sort(Comparer<ObservableRangeCollection<FileEntry>>.Create((a, b) => a[0].Path.CompareTo(b[0].Path)));
                FinalizeDuplicateFinding();
            }));
        }

        private void FindDuplicatedFilesInBoth() {
            bool error = false;
            primaryDirectory = Utility.NormalizePath(primaryDirectory);
            secondaryDirectory = Utility.NormalizePath(secondaryDirectory);
            if (primaryDirectory.ToUpperInvariant() == secondaryDirectory.ToUpperInvariant()) {
                error = true;
                Utility.LogFromNonGUIThread("Primary and secondary directories must be different.");
            }

            Utility.CheckDirectories(primaryDirectory, secondaryDirectory, ref error);
            if (primaryDirectory.IsSubDirectoryOf(secondaryDirectory)) {
                error = true;
                Utility.LogFromNonGUIThread("Primary directory cannot be a subdirectory of secondary directory.");
            }
            if (secondaryDirectory.IsSubDirectoryOf(primaryDirectory)) {
                error = true;
                Utility.LogFromNonGUIThread("Secondary directory cannot be a subdirectory of primary directory.");
            }

            if (error) {
                Dispatcher.BeginInvoke((Action)(() => {
                    statusBarView.ViewModel.State = "Failed";
                    statusBarView.ViewModel.ShowProgress = false;
                    UnlockGUI();
                }));
                return;
            }

            FileManager.FindDuplicatedFiles(primaryDirectory, secondaryDirectory, ShowBasePaths);


            Dispatcher.BeginInvoke((Action)(() => {
                if (!SortBySize)
                    FileManager.duplicatedFiles.Sort(Comparer<Tuple<ObservableRangeCollection<FileEntry>, ObservableRangeCollection<FileEntry>>>.Create((a, b) => a.Item1[0].Path.CompareTo(b.Item1[0].Path)));
                FinalizeDuplicateFinding();
            }));
        }

        private void InitializeDuplicateFinding() {
            LockGUI();
            statusBarView.ViewModel.State = "Initializing";
            statusBarView.ViewModel.Progress = 0;
            statusBarView.ViewModel.ShowProgress = true;
            FileManager.primaryFiles.Clear();
            FileManager.secondaryFiles.Clear();

            //logListView.Items.Clear();
            /// should be through MainTabControl
            Utility.logListView.Items.Clear();

            FileManager.emptyDirectoriesPrimary.Clear();
            FileManager.emptyFilesPrimary.Clear();
            FileManager.emptyDirectoriesSecondary.Clear();
            FileManager.emptyFilesSecondary.Clear();
            FileManager.duplicatedFiles.Clear();
            FileManager.duplicatedFilesPrimaryOnly.Clear();

            primaryDirectory = directoryPickerView.PrimaryDirectory;
            secondaryDirectory = directoryPickerView.SecondaryDirectory;
            ClearTmpDirectory();
            FileManager.storedFiles.Clear();
            restoreButton.IsEnabled = false;
        }

        private void FinalizeDuplicateFinding() {
            if (stopTask) {
                stopTask = false;
                statusBarView.ViewModel.State = "Stopped";
            }
            else
                statusBarView.ViewModel.State = "Done";
            statusBarView.ViewModel.ShowProgress = false;
            statusBarView.ViewModel.StateInfo = "";
            UnlockGUI();
        }

        private void ClearTmpDirectory() {
            DirectoryInfo directoryInfo = new DirectoryInfo(tmpDirectory);
            foreach (FileInfo file in directoryInfo.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                dir.Delete(true);
        }

        ///make private
        public void LockGUI() {
            directoryPickerView.LockGUI();
            basePathsCheckBox.IsEnabled = false;
            findButton.IsEnabled = false;

            mainTabControlView.LockGUI();

            stopButton.IsEnabled = true;
        }

        ///make private
        public void UnlockGUI() {
            directoryPickerView.UnlockGUI();
            findButton.IsEnabled = true;
            basePathsCheckBox.IsEnabled = true;

            mainTabControlView.UnlockGUI();

            stopButton.IsEnabled = false;
        }

        public void RemoveFile(string path, string baseDirectory) {
            if (!ShowBasePaths)
                path = baseDirectory + path;
            /// try catch log here
            if (FileManager.RemoveFile(path, backupFiles, askLarge))
                restoreButton.IsEnabled = true;
        }
        
        /// try to put this as binding property (if it won't slow down the app)
        private void ShowBasePathsChecked() {
            /// in different thread maybe?
            ShowBasePaths = true;
            ///if(primaryOnly){
            for (int i = 0; i < FileManager.duplicatedFiles.Count; i++) {
                for (int p = 0; p < FileManager.duplicatedFiles[i].Item1.Count; p++)
                    FileManager.duplicatedFiles[i].Item1[p].Path = primaryDirectory + FileManager.duplicatedFiles[i].Item1[p].Path;
                for (int s = 0; s < FileManager.duplicatedFiles[i].Item2.Count; s++)
                    FileManager.duplicatedFiles[i].Item2[s].Path = secondaryDirectory + FileManager.duplicatedFiles[i].Item2[s].Path;
            }
            for (int i = 0; i < FileManager.emptyDirectoriesPrimary.Count; i++)
                FileManager.emptyDirectoriesPrimary[i].Path = primaryDirectory + FileManager.emptyDirectoriesPrimary[i].Path;
            for (int i = 0; i < FileManager.emptyFilesPrimary.Count; i++)
                FileManager.emptyFilesPrimary[i].Path = primaryDirectory + FileManager.emptyFilesPrimary[i].Path;
            for (int i = 0; i < FileManager.emptyDirectoriesSecondary.Count; i++)
                FileManager.emptyDirectoriesSecondary[i].Path = secondaryDirectory + FileManager.emptyDirectoriesSecondary[i].Path;
            for (int i = 0; i < FileManager.emptyFilesSecondary.Count; i++)
                FileManager.emptyFilesSecondary[i].Path = secondaryDirectory + FileManager.emptyFilesSecondary[i].Path;

            mainTabControlView.duplicatedFilesListView.Items.Refresh();
            mainTabControlView.emptyDirectoriesPrimaryListView.Items.Refresh();
            mainTabControlView.emptyFilesPrimaryListView.Items.Refresh();
            mainTabControlView.emptyDirectoriesSecondaryListView.Items.Refresh();
            mainTabControlView.emptyFilesSecondaryListView.Items.Refresh();
            ///}else{
            for (int i = 0; i < FileManager.duplicatedFilesPrimaryOnly.Count; i++)
                for (int p = 0; p < FileManager.duplicatedFilesPrimaryOnly[i].Count; p++)
                    FileManager.duplicatedFilesPrimaryOnly[i][p].Path = primaryDirectory + FileManager.duplicatedFilesPrimaryOnly[i][p].Path;
            mainTabControlView.emptyDirectoriesPrimaryOnlyListView.Items.Refresh();
            mainTabControlView.emptyFilesPrimaryOnlyListView.Items.Refresh();
            mainTabControlView.duplicatedFilesPrimaryOnlyListView.Items.Refresh();
            ///}
        }

        private void ShowBasePathsUnchecked() {
            ShowBasePaths = false;
            for (int i = 0; i < FileManager.duplicatedFiles.Count; i++) {
                for (int p = 0; p < FileManager.duplicatedFiles[i].Item1.Count; p++)
                    FileManager.duplicatedFiles[i].Item1[p].Path = new string(FileManager.duplicatedFiles[i].Item1[p].Path.Skip(primaryDirectory.Length).ToArray());
                for (int s = 0; s < FileManager.duplicatedFiles[i].Item2.Count; s++)
                    FileManager.duplicatedFiles[i].Item2[s].Path = new string(FileManager.duplicatedFiles[i].Item2[s].Path.Skip(secondaryDirectory.Length).ToArray());
            }
            for (int i = 0; i < FileManager.emptyDirectoriesPrimary.Count; i++)
                FileManager.emptyDirectoriesPrimary[i].Path = new string(FileManager.emptyDirectoriesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());
            for (int i = 0; i < FileManager.emptyFilesPrimary.Count; i++)
                FileManager.emptyFilesPrimary[i].Path = new string(FileManager.emptyFilesPrimary[i].Path.Skip(primaryDirectory.Length).ToArray());
            for (int i = 0; i < FileManager.emptyDirectoriesSecondary.Count; i++)
                FileManager.emptyDirectoriesSecondary[i].Path = new string(FileManager.emptyDirectoriesSecondary[i].Path.Skip(secondaryDirectory.Length).ToArray());
            for (int i = 0; i < FileManager.emptyFilesSecondary.Count; i++)
                FileManager.emptyFilesSecondary[i].Path = new string(FileManager.emptyFilesSecondary[i].Path.Skip(secondaryDirectory.Length).ToArray());
            mainTabControlView.duplicatedFilesListView.Items.Refresh();
            mainTabControlView.emptyDirectoriesPrimaryListView.Items.Refresh();
            mainTabControlView.emptyFilesPrimaryListView.Items.Refresh();
            mainTabControlView.emptyDirectoriesSecondaryListView.Items.Refresh();
            mainTabControlView.emptyFilesSecondaryListView.Items.Refresh();

            for (int i = 0; i < FileManager.duplicatedFilesPrimaryOnly.Count; i++)
                for (int p = 0; p < FileManager.duplicatedFilesPrimaryOnly[i].Count; p++)
                    FileManager.duplicatedFilesPrimaryOnly[i][p].Path = new string(FileManager.duplicatedFilesPrimaryOnly[i][p].Path.Skip(primaryDirectory.Length).ToArray());
            mainTabControlView.emptyDirectoriesPrimaryOnlyListView.Items.Refresh();
            mainTabControlView.emptyFilesPrimaryOnlyListView.Items.Refresh();
            mainTabControlView.duplicatedFilesPrimaryOnlyListView.Items.Refresh();
        }

        private void BackupFilesChecked(object sender, RoutedEventArgs e) {
            backupFiles = true;
        }

        private void BackupFilesUnchecked(object sender, RoutedEventArgs e) {
            backupFiles = false;
            askLargeCheckBox.IsChecked = false;
        }

        private void AskLargeChecked(object sender, RoutedEventArgs e) {
            askLarge = true;
            backupFilesCheckBox.IsChecked = true;
        }

        private void AskLargeUnchecked(object sender, RoutedEventArgs e) {
            askLarge = false;
        }

        private void RestoreFiles(object sender, RoutedEventArgs e) {
            RestoreFileDialog popup = new RestoreFileDialog(FileManager.storedFiles);
            popup.ShowDialog();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            StopTask(null, null);
            FileManager.ClearDirectory(tmpDirectory);
        }

        private void StopTask(object sender, RoutedEventArgs e) {
            stopTask = true;
            mainTabControlView.ViewModel.StopTask();
            FileManager.StopTask();
        }
    }
}
