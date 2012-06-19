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
using System.Collections.ObjectModel;

namespace SimpleListsOfCloud
{
    public partial class ItemListUI : UserControl
    {
        public Color colorPivot = Color.FromArgb(byte.MaxValue, (byte)50, (byte)50, (byte)50);
        public double opacityValue = 1.0;
        private double itemHeight = -1.0;
        private double autoScrollHitAreaHeight = 100.0;
        private double autoScrollDelta = 10.0;
        private double lastDraggedYDist = -1.0;
        private double dragStartY = -1.0;
        public ItemListUI task_listbox;
        public ItemUI maximizedTaskList;
        //public List<ItemUI> itemList;
        private ScrollViewer scrollViewer;
        private bool isManualScrollingEnabled;
        private bool isReordering;
        private bool isAddingItemViaDragTop;
        private bool isDraggingListBox;
        private bool disableTopLevelGesture;
        private ItemUI newItem;
        public ListItem currItem;
        private int minimizedCount = 0;



        public ItemListUI()
        {
            InitializeComponent();
            this.Loaded += (RoutedEventHandler)((s, e) => scrollViewer = UiUtil.FindChildOfType<ScrollViewer>((DependencyObject)listbox));
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
            if (TransformUtil.getScaleY((FrameworkElement)newItem) < 0.7)
            {
                itemGrid.Children.Remove((UIElement)newItem);
            }
            else
            {
                TransformUtil.setScaleY((FrameworkElement)newItem, 1.0);
                TransformUtil.setRotateX((FrameworkElement)newItem, 0.0);
                TransformUtil.setScaleX((FrameworkElement)newItem, 1.0);
                TransformUtil.setTranslateX((FrameworkElement)newItem, 0.0);
                newItem.Tag = currItem.Add();
                newItem.Update();
                //sortZIndex();
                newItem.setText("");
                newItem.text.Focus();
            }
            updateItemGridHeight();
            for (int idx = indexOfItem(newItem) + 1; idx < itemGrid.Children.Count; ++idx)
                TransformUtil.setTranslateY((FrameworkElement)itemGrid.Children[idx], idxToPosition(idx));
            newItem = (ItemUI)null;
            isAddingItemViaDragTop = false;
            enableNativeScrolling(true);
            enableAllChildrenGesture(true);
        }

        private void onDragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            if (disableTopLevelGesture || !isDraggingListBox)
                return;
            bool flag1 = e.GetPosition((UIElement)this.listbox).Y > dragStartY;
            bool flag2 = scrollViewer.VerticalOffset < 10.0 / itemGrid.Height;
            double verticalOffset = scrollViewer.VerticalOffset;
            double scrollableHeight = scrollViewer.ScrollableHeight;

            if (!this.isAddingItemViaDragTop && flag2 && flag1)
            {
                e.Handled = true;
                isAddingItemViaDragTop = true;
                enableAllChildrenGesture(false);
                newItem = addItem("new item");
                //newItem.setText("pull to add list");

                //itemGrid.Children.Remove(newItem);
                //itemGrid.Children.Insert(0, newItem);
                
                moveItemToIdxPosition(newItem, 0);
                TransformUtil.setScaleY((FrameworkElement)newItem, lastDraggedYDist);
                TransformUtil.setScaleX((FrameworkElement)newItem, 0.9);
                TransformUtil.setTranslateX((FrameworkElement)newItem, 20.0);
                //TransformUtil.setRotateX((FrameworkElement)this.newItem, -90.0);
                itemGrid.Height = (double)(itemGrid.Children.Count - 1) * itemHeight;
            }
            else
            {
                if (!isAddingItemViaDragTop)
                    return;
                e.Handled = true;
                //Debug.WriteLine("lastDraggedYDist:{0} VerticalChange:{1}", lastDraggedYDist, e.VerticalChange);
                double num1 = Math.Min((lastDraggedYDist + e.VerticalChange) / itemHeight, 1.0);
                //Debug.WriteLine(num1);
                lastDraggedYDist += e.VerticalChange;
                TransformUtil.setScaleY((FrameworkElement)newItem, num1);
                TransformUtil.setScaleX((FrameworkElement)newItem, 0.9+num1/10);
                TransformUtil.setTranslateX((FrameworkElement)newItem, 20.0-(num1*20));
                //TransformUtil.setRotateX((FrameworkElement)this.newItem, num1 * 90.0 - 90.0);

                double num2 = TransformUtil.getScaleY((FrameworkElement)newItem) * itemHeight;
                itemGrid.Height = Math.Max((double)(itemGrid.Children.Count - 1) * itemHeight + num2, 0.0);

                for (int index = indexOfItem(newItem) + 1; index < itemGrid.Children.Count; ++index)
                    TransformUtil.setTranslateY((FrameworkElement)itemGrid.Children[index], idxToPosition(index - 1) + num2);
            }
        }

        #endregion

        public void RefillList()
        {
            FillList(currItem);
        }

        public void FillList(ListItem items)
        {
            //items.Sort();
            items.UpdateViews();
            currItem = items;
            minimizedTaskList(); 
        }

        void fill()
        {
            itemGrid.Children.Clear();
            foreach (var item in currItem.Items)
            {
                if(item.Deleted) continue;
                addItem(item);
            }
            UpdateColor();
            maximizeTaskList();
        }

        public void UpdateColor()
        {
            foreach (ItemUI item in itemGrid.Children)
            {
                item.Update();
            }
        }

        public ItemUI addItem(ListItem data)
        {
            var listBoxItem = new ItemUI(this);
            listBoxItem.Tag = (object)data;
            listBoxItem.setText(data.Name);
            itemGrid.Children.Add((UIElement)listBoxItem);
            TransformUtil.setTranslateY((FrameworkElement)listBoxItem, 0);

            if (data.Mark)
            {
                listBoxItem.markComplite.Visibility = Visibility.Visible;
            }
            else
            {
                listBoxItem.markComplite.Visibility = Visibility.Collapsed;
            }
            itemHeight = listBoxItem.Height;
            updateItemGridHeight();
            //moveItemToIdxPosition(listBoxItem, indexOfItem(listBoxItem));
            sortZIndex();
            return listBoxItem;
        }

        public ItemUI addItem(string text)
        {
            //var newListItem = currItem.Add();

            ItemUI listBoxItem = new ItemUI(this);
            //listBoxItem.Tag = (object)newListItem;
            listBoxItem.setText("new item");
            itemGrid.Children.Insert(0,(UIElement)listBoxItem);
            itemHeight = listBoxItem.Height;
            updateItemGridHeight();
            moveItemToIdxPosition(listBoxItem, indexOfItem(listBoxItem));
            sortZIndex();
            return listBoxItem;
        }
            

        public void minimizedTaskList()
        {
            minimizedCount = itemGrid.Children.Count;
            if (minimizedCount == 0)
            {
                fill();
            }
            else
            {

                AnimationUtil.opacity((FrameworkElement) itemGrid, 0.0, 300, (Action<object, EventArgs>) null);

                for (int idx = 0; idx < itemGrid.Children.Count; ++idx)
                    AnimationUtil.translateY((FrameworkElement) itemGrid.Children[idx], 0, 200, CallbackMinimized);
            }
        }

        private void CallbackMinimized(object o, EventArgs eventArgs)
        {
            minimizedCount--;
            if (minimizedCount <= 0)
            {
                fill();
            }
        }

        public void maximizeTaskList()
        {
            AnimationUtil.opacity((FrameworkElement)itemGrid, 1.0, 300, (Action<object, EventArgs>)null);
            //this.updateList(taskList);
            for (int idx = 0; idx < itemGrid.Children.Count; ++idx)
                AnimationUtil.translateY((FrameworkElement)itemGrid.Children[idx], idxToPosition(idx), 200, (Action<object, EventArgs>)null);
        }

        public void onReorderStarted(ItemUI child)
        {
            if (this.itemGrid.Children.Count <= 1)
                return;
            this.isReordering = true;
            this.enableNativeScrolling(false);
            this.isManualScrollingEnabled = false;
            child.onReorderStarted();
        }

        public void onReorderCompleted(ItemUI child)
        {
            this.isReordering = false;
            this.enableNativeScrolling(true);
            this.isManualScrollingEnabled = false;
            this.sortZIndex();
            AnimationUtil.translateY((FrameworkElement)child, idxToPosition(indexOfItem(child)), 200, (Action<object, EventArgs>)null);
            child.onReorderCompleted();
        }

        public void onReorderDelta(ItemUI child, double delta)
        {
            updateDeltaItemPosition(child, delta);
            checkIfScrollingIsNeeded(child);
        }

        private void updateDeltaItemPosition(ItemUI item, double delta)
        {
            int idx = indexOfItem(item);
            moveDeltaItemWithinBounds(item, delta);
            int num = positionToIdx(TransformUtil.getTranslateY((FrameworkElement)item));
            if (idx == num)
                return;
            int index = idx - num < 0 ? idx + 1 : idx - 1;
            AnimationUtil.translateY((FrameworkElement)itemGrid.Children[index], idxToPosition(idx), 200, (Action<object, EventArgs>)null);
            swapItemsInList(item, (ItemUI)itemGrid.Children[index]);
        }

        private void checkIfScrollingIsNeeded(ItemUI item)
        {
            double num1 = scrollViewer.RenderSize.Height + scrollViewer.VerticalOffset * itemGrid.ActualHeight - autoScrollHitAreaHeight;
            double num2 = autoScrollHitAreaHeight + scrollViewer.VerticalOffset * itemGrid.ActualHeight;
            double translateY = TransformUtil.getTranslateY((FrameworkElement)item);
            if (translateY > num1)
            {
                if (this.isManualScrollingEnabled)
                    return;
                this.isManualScrollingEnabled = true;
                this.beginScrollDown(item);
            }
            else if (translateY + this.itemHeight < num2)
            {
                if (this.isManualScrollingEnabled)
                    return;
                this.isManualScrollingEnabled = true;
                this.beginScrollUp(item);
            }
            else
                this.isManualScrollingEnabled = false;
        }

        
        public void onDraggingToDelete()
        {
            disableTopLevelGesture = true;
            enableNativeScrolling(false);
        }

        public void onDraggingToDeleteEnded()
        {
            disableTopLevelGesture = false;
            enableNativeScrolling(true);
        }

        public void onDeleteItem(ItemUI item)
        {
            ((ListItem) item.Tag).SetDeleted(true);
            int num = indexOfItem(item);
            itemGrid.Children.Remove(item);            
            updateItemGridHeight();
            for (int idx = num; idx < itemGrid.Children.Count; ++idx)
                AnimationUtil.translateY((FrameworkElement)itemGrid.Children[idx], this.idxToPosition(idx), 100, (Action<object, EventArgs>)null);
            sortZIndex();
        }

        public void onEditText()
        {
            disableTopLevelGesture = true;
            enableNativeScrolling(false);
            enableAllChildrenGesture(false);
        }

        public void onCompletedEditText(ItemUI item)
        {
            disableTopLevelGesture = false;
            enableNativeScrolling(true);
            enableAllChildrenGesture(true);
        }

       
        private void sortZIndex()
        {
            for (int index = 0; index < itemGrid.Children.Count; ++index)
                Canvas.SetZIndex((UIElement)itemGrid.Children[index], itemGrid.Children.Count - index);
        }

        private void enableAllChildrenGesture(bool enable)
        {
            foreach (ItemUI taskListListBoxItem in itemGrid.Children)
            {
                if (taskListListBoxItem.disableGesture == enable)
                    taskListListBoxItem.disableGesture = !enable;
            }
        }

        private void updateItemGridHeight()
        {
            this.itemGrid.Height = (double)itemGrid.Children.Count * this.itemHeight;
        }

        private void beginScrollDown(ItemUI item)
        {
            if (!this.isManualScrollingEnabled)
                return;
            this.scrollViewer.ScrollToVerticalOffset(this.scrollViewer.VerticalOffset + this.autoScrollDelta * (this.scrollViewer.ViewportHeight / this.scrollViewer.RenderSize.Height));
            this.updateDeltaItemPosition(item, this.autoScrollDelta);
            this.Dispatcher.BeginInvoke((Action)(() => this.beginScrollDown(item)));
        }

        private void beginScrollUp(ItemUI item)
        {
            if (!this.isManualScrollingEnabled)
                return;
            this.scrollViewer.ScrollToVerticalOffset(this.scrollViewer.VerticalOffset - this.autoScrollDelta * (this.scrollViewer.ViewportHeight / this.scrollViewer.RenderSize.Height));
            this.updateDeltaItemPosition(item, -this.autoScrollDelta);
            this.Dispatcher.BeginInvoke((Action)(() => this.beginScrollUp(item)));
        }

        private void enableNativeScrolling(bool enable)
        {
            if (enable)
                this.scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            else
                this.scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        private int positionToIdx(double y)
        {
            return (int)Math.Floor((y + this.itemHeight / 2.0) / this.itemHeight);
        }

        private double idxToPosition(int idx)
        {
            return (double)idx * this.itemHeight;
        }

        private void moveItemToIdxPosition(ItemUI item, int idx)
        {
            TransformUtil.setTranslateY((FrameworkElement)item, idxToPosition(idx));
        }

        private void moveDeltaItemWithinBounds(ItemUI item, double delta)
        {
            int count = itemGrid.Children.Count;
            double num = TransformUtil.getTranslateY((FrameworkElement)item) + delta;
            if (num < 0.0)
                num = 0.0;
            else if (num > this.idxToPosition(count - 1))
                num = this.idxToPosition(count - 1);
            TransformUtil.setTranslateY((FrameworkElement)item, num);
        }

        private int indexOfItem(ItemUI item)
        {
            return itemGrid.Children.IndexOf(item);
        }

        private void swapItemsInList(ItemUI a, ItemUI b)
        {
            //return;
            int index1 = indexOfItem(a);
            int index2 = indexOfItem(b);
            //itemGrid.Children.Insert(index1, b);
            //itemGrid.Children.Remove(a);
            //itemGrid.Children.Remove(b);
            itemGrid.Children[index1] = new ItemUI();
            itemGrid.Children[index2] = new ItemUI();
            itemGrid.Children[index1] = b;
            itemGrid.Children[index2] = a;
        }

        public void onUncompleteItem(ItemUI item)
        {
            ((ListItem) item.Tag).SetMark(false);
            item.Update();
        }

        public void onCompleteItem(ItemUI item)
        {
            if (item.Tag != null)
            {
                ((ListItem) item.Tag).SetMark(true);
                item.Update();
            }
        }
    }
}
