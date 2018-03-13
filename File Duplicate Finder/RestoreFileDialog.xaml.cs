using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace FileDuplicateFinder {
    /// <summary>
    /// Interaction logic for RestoreFileDialog.xaml
    /// </summary>
    public partial class RestoreFileDialog: Window {
        List<Tuple<string, string>> storedFiles;
        public RestoreFileDialog(List<Tuple<string, string>> storedFiles) {
            InitializeComponent();
            this.storedFiles = storedFiles;
            restoreFileListView.ItemsSource = storedFiles;
        }

        private void RestoreFile(object sender, RoutedEventArgs e) {
            string path = ((TextBlock)((Grid)((Button)sender).Parent).Children[0]).Text;
            for (int i = 0; i < storedFiles.Count; i++) {
                if (storedFiles[i].Item1.Equals(path)) {
                    if (storedFiles[i].Item2.Equals("")) {
                        Directory.CreateDirectory(path);
                    } else {
                        FileInfo fileInfo = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "tmp/" +storedFiles[i].Item2);
                        fileInfo.MoveTo(path);
                    }
                    storedFiles.RemoveAt(i);
                    restoreFileListView.Items.Refresh();
                    break;
                }
            }
        }

        private void Close(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
