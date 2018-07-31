using System;
using System.Windows;
using System.Windows.Data;

namespace FileDuplicateFinder.Converter {
    class NotBooleanToVisibilityHiddenConverter: IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value is Boolean && (bool)value)
                return Visibility.Hidden;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value is Visibility && (Visibility)value == Visibility.Visible)
                return false;
            return true;
        }
    }
}
