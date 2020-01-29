using System;
using System.ComponentModel;

namespace FileDuplicateFinder {
    public abstract class ObjectBase: INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        protected internal void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Bind(string propertyName, Action action) {
            PropertyChanged += (s, e) => {
                if (e.PropertyName.Equals(propertyName, StringComparison.Ordinal))
                    action();
            };
        }
    }
}