namespace FileDuplicateFinder.Models {
    class ApplicationState {
        public bool AskLarge { get; set; } = true;
        public bool BackupFiles { get; set; } = true;
        public bool IsGUIEnabled { get; set; } = true;
        public bool PrimaryOnly { get; set; } = false;
        public DuplicateSearchProgressState Progress { get; set; }
        public bool ShowBasePaths { get; set; } = false;

        public ApplicationState() {
            /// todo load data
        }
    }
}
