namespace FileDuplicateFinder {
    public class RemoveProgress {
        public string Description { get; set; }
        public int MaxProgress { get; set; }
        public int Progress { get; set; }
        public RemoveProgressState State { get; set; }
    }
}
