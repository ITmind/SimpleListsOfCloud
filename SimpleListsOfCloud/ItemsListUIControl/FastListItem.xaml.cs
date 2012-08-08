using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using SimpleListsOfCloud.Utils;

namespace SimpleListsOfCloud.ItemsListUIControl
{
    public partial class FastListItem : UserControl
    {
        public event EventHandler<MessageEventArgs> ListRefill;
        public ListItem CurrItem;
        private double _lastDraggedYDist = -1.0;
        private ListBoxItem firstItem = null;
        public bool IsDragItem { get; set; }
        public bool IsDragList { get; set; }

        public void OnListRefill(string folderName)
        {
            EventHandler<MessageEventArgs> handler = ListRefill;
            if (handler != null) handler(this, new MessageEventArgs(folderName));
        }

        public FastListItem()
        {

            InitializeComponent();
        }

        public void FillList(ListItem items)
        {
            CurrItem = items;
            list.ItemsSource = items.Items;
        }

        public void RefillList()
        {
            //list.UpdateLayout();
        }

        private void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //если убирается выделение, то ничего не делаем
            if (e.AddedItems.Count > 0)
            {
                CurrItem = (ListItem) ((LongListSelectorItem) e.AddedItems[0]).Item;
                list.ItemsSource = CurrItem.Items;
            }
        }

        private void list_Loaded(object sender, RoutedEventArgs e)
        {
            //get TemplatedListBox inside LongListSelector
            FrameworkElement tlb = VisualTreeHelper.GetChild(list, 0) as FrameworkElement;
            //get ScrollViewer inside TemplatedListBox
            FrameworkElement sv = VisualTreeHelper.GetChild(tlb, 0) as FrameworkElement;
            //MS says VisualGroups are inside first Child of ScrollViewer 
            FrameworkElement here = VisualTreeHelper.GetChild(sv, 0) as FrameworkElement;
            var groups = VisualStateManager.GetVisualStateGroups(here);
            VisualStateGroup vc = null;
            foreach (VisualStateGroup g in groups)
            {
                if (g.Name == "VerticalCompression")
                {
                    vc = g;
                    break;
                }
            }
            vc.CurrentStateChanged += vc_CurrentStateChanged;
        }

        void vc_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            if (IsDragItem) return;
            if (e.NewState.Name == "CompressionTop" && firstItem == null)
            {
                //put your code for loading new items here 
                CurrItem.Add();
                list.UpdateLayout();
                firstItem = FindVisualChild<ListBoxItem>(list);
                TransformUtil.setScaleX(firstItem, 0.9);
                firstItem.Height = 10;
                TransformUtil.setTranslateX(firstItem, 20.0);
            }
        }

        private void GestureListener_DragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            if (IsDragItem || !IsDragList) return;
            if(firstItem == null) return;
            //Debug.WriteLine("GestureListener_DragDelta {0}", e.VerticalChange);
            double num1 = Math.Min((_lastDraggedYDist + e.VerticalChange) / 100, 1.0);

            _lastDraggedYDist += e.VerticalChange;
            if (_lastDraggedYDist >0 && _lastDraggedYDist < 100)
            {
                firstItem.Height = _lastDraggedYDist;
                TransformUtil.setScaleX(firstItem, 0.9 + num1/10);
                TransformUtil.setTranslateX(firstItem, 20.0 - (num1*20));                
            }
        }

        private void GestureListener_DragCompleted(object sender, DragCompletedGestureEventArgs e)
        {
            if (IsDragItem || !IsDragList) return;

            _lastDraggedYDist = -1.0;
            if (firstItem != null)
            {
                if (firstItem.Height > 70)
                {
                    firstItem.Height = 100;
                    TransformUtil.setScaleX(firstItem, 1);
                    TransformUtil.setTranslateX(firstItem, 0);
                }
                else
                {
                    //Debug.WriteLine("Delete");
                    CurrItem.Delete(0);
                }
            }
            firstItem = null;
            IsDragList = false;
        }

        public static TChildItem FindVisualChild<TChildItem>(DependencyObject obj) where TChildItem : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is TChildItem)
                {
                    return (TChildItem)child;
                }
                else
                {
                    var childOfChild = FindVisualChild<TChildItem>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }

        private void GestureListener_DragStarted(object sender, DragStartedGestureEventArgs e)
        {
            if (!IsDragItem)
            {
                IsDragList = true;
            }
        }

        
    }
}
