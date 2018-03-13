namespace File_Duplicate_Finder {
    public class FileEntry {
        public string Path { get; set; }
        public string Size { get; set; }

        public FileEntry(string path, string size) {
            Path = path;
            Size = size;
        }
    }
}
