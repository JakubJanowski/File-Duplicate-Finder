using System;
using System.Runtime.Serialization;

namespace FileDuplicateFinder {
    [Serializable]
    internal class SortException: Exception {
        public SortException() {
        }

        public SortException(string message) : base(message) {
        }

        public SortException(string message, Exception innerException) : base(message, innerException) {
        }

        protected SortException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}