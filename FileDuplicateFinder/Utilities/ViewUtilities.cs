using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FileDuplicateFinder.Utilities {
    public static class ViewUtilities {
        public static void ShowFileListItemButtons(Border sender) {
            Border border = sender as Border;
            Grid grid = border.Child as Grid;
            border.Background = Brushes.AliceBlue;
            foreach (var child in grid.Children)
                if (child is Button)
                    ((Button)child).Visibility = Visibility.Visible;
        }

        public static void HideFileListItemButtons(Border sender) {
            Border border = sender as Border;
            Grid grid = border.Child as Grid;
            border.Background = Brushes.Transparent;
            foreach (var child in grid.Children)
                if (child is Button)
                    ((Button)child).Visibility = Visibility.Collapsed;
        }
    }
}
