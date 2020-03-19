namespace FileDuplicateFinder {
    public class DuplicateSearchProgress {
        public string Description { get; set; }
        public int MaxProgress { get; set; }
        public int Progress { get; set; }
        public DuplicateSearchProgressState State { get; set; }
    }
}
