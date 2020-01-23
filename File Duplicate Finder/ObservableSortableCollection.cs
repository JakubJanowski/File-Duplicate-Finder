namespace System.Collections.ObjectModel {
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    public enum SortDirection { Ascending, Descending }

    public class ObservableSortableCollection<T>: ObservableRangeCollection<T> {
        public ObservableSortableCollection() : base() { }

        public ObservableSortableCollection(IEnumerable<T> collection) : base(collection) { }

        private bool reordering;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            base.OnCollectionChanged(e);
            if (reordering)
                return;
            switch (e.Action) {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Reset:
                    return;
            }
        }

        public void Sort(Comparer<T> comparer, SortDirection sortDirection = SortDirection.Ascending) {
            List<T> sorted = sortDirection == SortDirection.Ascending ? this.OrderBy(x => x, comparer).ToList() : this.OrderByDescending(x => x, comparer).ToList();
            reordering = true;
            for (int i = 0; i < sorted.Count(); i++)
                Move(IndexOf(sorted[i]), i);
            reordering = false;
        }

        public void Sort(SortDirection sortDirection = SortDirection.Ascending) {
            List<T> sorted = sortDirection == SortDirection.Ascending ? this.OrderBy(x => x).ToList() : this.OrderByDescending(x => x).ToList();
            reordering = true;
            for (int i = 0; i < sorted.Count(); i++)
                Move(IndexOf(sorted[i]), i);
            reordering = false;
        }
    }
}