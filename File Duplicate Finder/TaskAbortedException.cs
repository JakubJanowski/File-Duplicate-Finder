using System;
using System.Runtime.Serialization;

namespace FileDuplicateFinder {
    [Serializable]
    internal class TaskAbortedException: Exception {
        public TaskAbortedException() {
        }

        public TaskAbortedException(string message) : base(message) {
        }

        public TaskAbortedException(string message, Exception innerException) : base(message, innerException) {
        }

        protected TaskAbortedException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}