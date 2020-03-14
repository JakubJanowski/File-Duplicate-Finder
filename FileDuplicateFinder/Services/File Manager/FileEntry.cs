using System.Windows.Media;

namespace FileDuplicateFinder {
    public class FileEntry {
        public string Path { get; set; }
        public string Size { get; set; }
        public ImageSource Icon { get; set; }

        public FileEntry(string path, string size, ImageSource icon) {
            Path = path;
            Size = size;
            Icon = icon;
        }

        public FileEntry(string path, ImageSource icon) : this(path, null, icon) { }
    }
}
