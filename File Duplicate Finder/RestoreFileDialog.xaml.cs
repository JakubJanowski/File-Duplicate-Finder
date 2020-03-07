using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace FileDuplicateFinder {
    public partial class RestoreFileDialog: Window {
        ObservableRangeCollection<Tuple<string, string>> storedFiles;
        ///todo could pass just a list? what if a new file will get removed while the window is open?
        public RestoreFileDialog(ObservableRangeCollection<Tuple<string, string>> storedFiles) {
            InitializeComponent();
            this.storedFiles = storedFiles;
            restoreFileListView.ItemsSource = storedFiles;
        }

        private void RestoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            for (int i = 0; i < storedFiles.Count; i++) {
                if (storedFiles[i].Item1.Equals(path)) {
                    if (string.IsNullOrEmpty(storedFiles[i].Item2)) {
                        Directory.CreateDirectory(path);
                    } else {
                        FileInfo fileInfo = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "tmp/" + storedFiles[i].Item2);
                        fileInfo.MoveTo(path);
                    }
                    storedFiles.RemoveAt(i);
                    break;
                }
            }
        }

        private void Close(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
