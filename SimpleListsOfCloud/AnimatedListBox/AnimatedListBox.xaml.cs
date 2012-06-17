using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using SimpleListsOfCloud.Utils;
using Microsoft.Phone.Controls;
using System.Diagnostics;

namespace SimpleListsOfCloud
{
    public partial class AnimatedListBox : UserControl
    {
        public double opacityValue = 1.0;
        private double itemHeight = -1.0;
        //private double autoScrollHitAreaHeight = 100.0;
        //private double autoScrollDelta = 10.0;
        private double lastDraggedYDist = -1.0;
        private double dragStartY = -1.0;

        //private bool isManualScrollingEnabled;
        private bool isReordering;
        //private bool isAddingItemViaZoom;
        private bool isAddingItemViaDragTop;
        private bool isDraggingListBox;
        //private bool isPinching;
        private bool disableTopLevelGesture;

        VirtualizingStackPanel itemsPanel;


        public AnimatedListBox()
        {
            InitializeComponent();
            listbox.ItemsSource = App.Current.ListItems.StartNode.ViewItems;
        }

        #region Gestures

        private void onDragStarted(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {

            if (disableTopLevelGesture || isReordering || isDraggingListBox)
                return;
            e.Handled = true;
            isDraggingListBox = true;
            lastDraggedYDist = 0.0;
            dragStartY = e.GetPosition((UIElement)listbox).Y;
        }

        private void onDragCompleted(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {

            if (disableTopLevelGesture || !isDraggingListBox)
                return;
            isDraggingListBox = false;
            if (!isAddingItemViaDragTop)
                return;

            //Debug.WriteLine("add items");
            //App.Current.ListItems.StartNode.Add();

            //if (TransformUtil.getScaleY((FrameworkElement)newItem) < 0.7)
            //{
            //    itemList.Remove(this.newItem);
            //    itemsPanel.Children.Remove((UIElement)newItem);
            //}
            //else
            //{
            //    TransformUtil.setScaleY((FrameworkElement)newItem, 1.0);
            //    TransformUtil.setRotateX((FrameworkElement)newItem, 0.0);
            //    TransformUtil.setScaleX((FrameworkElement)newItem, 1.0);
            //    TransformUtil.setTranslateX((FrameworkElement)newItem, 0.0);
            //    //sortZIndex();
            //    newItem.setText("");
            //    newItem.text.Focus();
            //}
            //updateItemGridHeight();
            //for (int idx = indexOfItem(newItem) + 1; idx < itemList.Count; ++idx)
            //    TransformUtil.setTranslateY((FrameworkElement)itemList[idx], idxToPosition(idx));
            //newItem = (ItemUI)null;
            //isAddingItemViaDragTop = false;
            //enableNativeScrolling(true);
            //enableAllChildrenGesture(true);
        }

        private void onDragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            if (disableTopLevelGesture || !isDraggingListBox)
                return;
            bool flag1 = e.GetPosition((UIElement)this.listbox).Y > dragStartY;
            bool flag2 = itemsPanel.ScrollOwner.VerticalOffset < 10.0 / 100;
            double verticalOffset = itemsPanel.ScrollOwner.VerticalOffset;
            double scrollableHeight = itemsPanel.ScrollOwner.ScrollableHeight;

            if (!this.isAddingItemViaDragTop && flag2 && flag1)
            {
                Debug.WriteLine("drag");
                e.Handled = true;
                isAddingItemViaDragTop = true;
                //Debug.WriteLine("before: " + itemsPanel.Children.Count());
                App.Current.ListItems.StartNode.Add();
                //Debug.WriteLine("after: "+itemsPanel.Children.Count());
                //    enableAllChildrenGesture(false);
                //    newItem = this.addItem("new item");
                //    //newItem.setText("pull to add list");

                //    itemList.Remove(newItem);
                //    itemList.Insert(0, newItem);

                //    moveItemToIdxPosition(newItem, 0);
                //    TransformUtil.setScaleY((FrameworkElement)newItem, lastDraggedYDist);
                //    TransformUtil.setScaleX((FrameworkElement)newItem, 0.9);
                //    TransformUtil.setTranslateX((FrameworkElement)newItem, 20.0);
                //    //TransformUtil.setRotateX((FrameworkElement)this.newItem, -90.0);
                //    itemGrid.Height = (double)(itemGrid.Children.Count - 1) * itemHeight;
            }
            else
            {
                if (!isAddingItemViaDragTop || itemsPanel.Children.Count == 0)
                    return;
                e.Handled = true;
                //Debug.WriteLine("another: " + itemsPanel.Children.Count());
                //Debug.WriteLine("lastDraggedYDist:{0} VerticalChange:{1}", lastDraggedYDist, e.VerticalChange);
                double num1 = Math.Min((lastDraggedYDist + e.VerticalChange) / itemHeight, 1.0);
                //    //Debug.WriteLine(num1);
                lastDraggedYDist += e.VerticalChange;
                TransformUtil.setScaleY((FrameworkElement)itemsPanel.Children[0], num1);
                TransformUtil.setScaleX((FrameworkElement)itemsPanel.Children[0], 0.9 + num1 / 10);
                TransformUtil.setTranslateX((FrameworkElement)itemsPanel.Children[0], 20.0 - (num1 * 20));
                //TransformUtil.setRotateX((FrameworkElement)this.newItem, num1 * 90.0 - 90.0);

                //    double num2 = TransformUtil.getScaleY((FrameworkElement)newItem) * itemHeight;
                //    itemGrid.Height = Math.Max((double)(itemGrid.Children.Count - 1) * itemHeight + num2, 0.0);

                //    for (int index = indexOfItem(newItem) + 1; index < itemList.Count; ++index)
                //        TransformUtil.setTranslateY((FrameworkElement)itemList[index], idxToPosition(index - 1) + num2);
            }
        }

        #endregion

        private void listbox_Loaded(object sender, RoutedEventArgs e)
        {
            itemsPanel = UiUtil.FindChildOfType<VirtualizingStackPanel>((DependencyObject)listbox);
        }
    }
}
