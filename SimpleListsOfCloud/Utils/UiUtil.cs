using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace SimpleListsOfCloud.Utils
{
    public class UiUtil
    {
        public static void setVisible(FrameworkElement ui, bool visible)
        {
            if (visible)
                ui.Visibility = Visibility.Visible;
            else
                ui.Visibility = Visibility.Collapsed;
        }

        public static bool isVisible(FrameworkElement ui)
        {
            return ui.Visibility == Visibility.Visible;
        }

        public static T FindChildOfType<T>(DependencyObject root) where T : class
        {
            Queue<DependencyObject> queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                DependencyObject reference = queue.Dequeue();
                for (int childIndex = VisualTreeHelper.GetChildrenCount(reference) - 1; 0 <= childIndex; --childIndex)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(reference, childIndex);
                    T obj = child as T;
                    if ((object)obj != null)
                        return obj;
                    queue.Enqueue(child);
                }
            }
            return default(T);
        }
    }
}
