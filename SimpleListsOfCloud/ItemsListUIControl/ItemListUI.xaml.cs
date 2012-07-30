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
        public Color ColorPivot = Color.FromArgb(byte.MaxValue, (byte)50, (byte)50, (byte)50);
        public double OpacityValue = 1.0;
        private double _itemHeight = -1.0;
        private const double AutoScrollHitAreaHeight = 100.0;
        private const double AutoScrollDelta = 10.0;
        private double _lastDraggedYDist = -1.0;
        private double _dragStartY = -1.0;
        public ItemListUI TaskListbox;
        public ItemUI MaximizedTaskList;
        //public List<ItemUI> itemList;
        private ScrollViewer _scrollViewer;
        private bool _isManualScrollingEnabled;
        private bool _isReordering;
        private bool _isAddingItemViaDragTop;
        private bool _isDraggingListBox;
        private bool _disableTopLevelGesture;
        private ItemUI _newItem;
        public ListItem CurrItem;
        private int _minimizedCount = 0;



        public ItemListUI()
        {
            InitializeComponent();
            this.Loaded += (RoutedEventHandler)((s, e) => _scrollViewer = UiUtil.FindChildOfType<ScrollViewer>((DependencyObject)listbox));
        }

        #region Gestures

        private void onDragStarted(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            if (_disableTopLevelGesture || _isReordering || _isDraggingListBox)
                return;
            e.Handled = true;
            _isDraggingListBox = true;
            _lastDraggedYDist = 0.0;
            _dragStartY = e.GetPosition((UIElement)listbox).Y;
        }

        private void onDragCompleted(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            if (_disableTopLevelGesture || !_isDraggingListBox)
                return;
            _isDraggingListBox = false;
            if (!_isAddingItemViaDragTop)
                return;
            if (TransformUtil.getScaleY((FrameworkElement)_newItem) < 0.7)
            {
                itemGrid.Children.Remove((UIElement)_newItem);
            }
            else
            {
                TransformUtil.setScaleY((FrameworkElement)_newItem, 1.0);
                TransformUtil.setRotateX((FrameworkElement)_newItem, 0.0);
                TransformUtil.setScaleX((FrameworkElement)_newItem, 1.0);
                TransformUtil.setTranslateX((FrameworkElement)_newItem, 0.0);
                _newItem.Tag = CurrItem.Add();
                if (_newItem.Tag == null)
                {
                    itemGrid.Children.Remove((UIElement)_newItem);
                }
                else
                {
                    _newItem.Update();
                    //sortZIndex();
                    _newItem.SetText("");
                    _newItem.text.Focus();
                }
            }
            updateItemGridHeight();
            for (int idx = indexOfItem(_newItem) + 1; idx < itemGrid.Children.Count; ++idx)
                TransformUtil.setTranslateY((FrameworkElement)itemGrid.Children[idx], idxToPosition(idx));
            _newItem = (ItemUI)null;
            _isAddingItemViaDragTop = false;
            enableNativeScrolling(true);
            enableAllChildrenGesture(true);
        }

        private void onDragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            if (_disableTopLevelGesture || !_isDraggingListBox)
                return;
            bool flag1 = e.GetPosition((UIElement)this.listbox).Y > _dragStartY;
            bool flag2 = _scrollViewer.VerticalOffset < 10.0 / itemGrid.Height;
            double verticalOffset = _scrollViewer.VerticalOffset;
            double scrollableHeight = _scrollViewer.ScrollableHeight;

            if (!this._isAddingItemViaDragTop && flag2 && flag1)
            {
                e.Handled = true;
                _isAddingItemViaDragTop = true;
                enableAllChildrenGesture(false);
                _newItem = addItem("new item");
                //newItem.setText("pull to add list");

                //itemGrid.Children.Remove(newItem);
                //itemGrid.Children.Insert(0, newItem);
                
                moveItemToIdxPosition(_newItem, 0);
                TransformUtil.setScaleY((FrameworkElement)_newItem, _lastDraggedYDist);
                TransformUtil.setScaleX((FrameworkElement)_newItem, 0.9);
                TransformUtil.setTranslateX((FrameworkElement)_newItem, 20.0);
                //TransformUtil.setRotateX((FrameworkElement)this.newItem, -90.0);
                itemGrid.Height = (double)(itemGrid.Children.Count - 1) * _itemHeight;
            }
            else
            {
                if (!_isAddingItemViaDragTop)
                    return;
                e.Handled = true;
                //Debug.WriteLine("lastDraggedYDist:{0} VerticalChange:{1}", lastDraggedYDist, e.VerticalChange);
                double num1 = Math.Min((_lastDraggedYDist + e.VerticalChange) / _itemHeight, 1.0);
                //Debug.WriteLine(num1);
                _lastDraggedYDist += e.VerticalChange;
                TransformUtil.setScaleY((FrameworkElement)_newItem, num1);
                TransformUtil.setScaleX((FrameworkElement)_newItem, 0.9+num1/10);
                TransformUtil.setTranslateX((FrameworkElement)_newItem, 20.0-(num1*20));
                //TransformUtil.setRotateX((FrameworkElement)this.newItem, num1 * 90.0 - 90.0);

                double num2 = TransformUtil.getScaleY((FrameworkElement)_newItem) * _itemHeight;
                itemGrid.Height = Math.Max((double)(itemGrid.Children.Count - 1) * _itemHeight + num2, 0.0);

                for (int index = indexOfItem(_newItem) + 1; index < itemGrid.Children.Count; ++index)
                    TransformUtil.setTranslateY((FrameworkElement)itemGrid.Children[index], idxToPosition(index - 1) + num2);
            }
        }

        #endregion

        public void RefillList()
        {
            //FillList(currItem);
            List<ItemUI> forDelete = new List<ItemUI>(10);
            List<ListItem> presentInGrid = new List<ListItem>(10);

            CurrItem.UpdateViews();
            foreach (ItemUI item in itemGrid.Children)
            {
                if (!CurrItem.Items.Contains((ListItem) item.Tag))
                {
                    forDelete.Add(item);
                }
                else
                {
                    presentInGrid.Add((ListItem) item.Tag);
                }
            }

            foreach (var itemUi in forDelete)
            {
                itemGrid.Children.Remove(itemUi);
            }

            foreach (var item in CurrItem.Items)
            {
                if (presentInGrid.Contains(item)) continue;
                if (item.Deleted) continue;
                addItem(item);
            }

            updateItemGridHeight();
            for (int idx = 0; idx < itemGrid.Children.Count; ++idx)
                AnimationUtil.translateY((FrameworkElement)itemGrid.Children[idx], this.idxToPosition(idx), 50, (Action<object, EventArgs>)null);
            UpdateColor();
        }

        public void FillList(ListItem items)
        {
            //items.Sort();
            items.UpdateViews();
            CurrItem = items;
            minimizedTaskList(); 
        }

        void fill()
        {
            itemGrid.Children.Clear();
            foreach (var item in CurrItem.Items)
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
            listBoxItem.SetText(data.Name);
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
            _itemHeight = listBoxItem.Height;
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
            listBoxItem.SetText("new item");
            itemGrid.Children.Insert(0,(UIElement)listBoxItem);
            _itemHeight = listBoxItem.Height;
            updateItemGridHeight();
            moveItemToIdxPosition(listBoxItem, indexOfItem(listBoxItem));
            sortZIndex();
            return listBoxItem;
        }
            

        public void minimizedTaskList()
        {
            _minimizedCount = itemGrid.Children.Count;
            if (_minimizedCount == 0)
            {
                fill();
            }
            else
            {

                AnimationUtil.opacity((FrameworkElement) itemGrid, 0.0, 200, (Action<object, EventArgs>) null);

                for (int idx = 0; idx < itemGrid.Children.Count; ++idx)
                    AnimationUtil.translateY((FrameworkElement) itemGrid.Children[idx], 0, 100, CallbackMinimized);
            }
        }

        private void CallbackMinimized(object o, EventArgs eventArgs)
        {
            _minimizedCount--;
            if (_minimizedCount <= 0)
            {
                fill();
            }
        }

        public void maximizeTaskList()
        {
            //TODO: im many items not work
            AnimationUtil.opacity((FrameworkElement)itemGrid, 1.0, 200, (Action<object, EventArgs>)null);
            //this.updateList(taskList);
            for (int idx = 0; idx < itemGrid.Children.Count; ++idx)
                AnimationUtil.translateY((FrameworkElement)itemGrid.Children[idx], idxToPosition(idx), 100, (Action<object, EventArgs>)null);
        }

        public void onReorderStarted(ItemUI child)
        {
            if (this.itemGrid.Children.Count <= 1)
                return;
            this._isReordering = true;
            this.enableNativeScrolling(false);
            this._isManualScrollingEnabled = false;
            child.OnReorderStarted();
        }

        public void onReorderCompleted(ItemUI child)
        {
            this._isReordering = false;
            this.enableNativeScrolling(true);
            this._isManualScrollingEnabled = false;
            this.sortZIndex();
            AnimationUtil.translateY((FrameworkElement)child, idxToPosition(indexOfItem(child)), 200, (Action<object, EventArgs>)null);
            //поменяем позицию в объекте
            CurrItem.Items.Remove((ListItem) child.Tag);
            CurrItem.Items.Insert(indexOfItem(child), (ListItem)child.Tag);
            child.OnReorderCompleted();
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
            double num1 = _scrollViewer.RenderSize.Height + _scrollViewer.VerticalOffset * itemGrid.ActualHeight - AutoScrollHitAreaHeight;
            double num2 = AutoScrollHitAreaHeight + _scrollViewer.VerticalOffset * itemGrid.ActualHeight;
            double translateY = TransformUtil.getTranslateY((FrameworkElement)item);
            if (translateY > num1)
            {
                if (this._isManualScrollingEnabled)
                    return;
                this._isManualScrollingEnabled = true;
                this.beginScrollDown(item);
            }
            else if (translateY + this._itemHeight < num2)
            {
                if (this._isManualScrollingEnabled)
                    return;
                this._isManualScrollingEnabled = true;
                this.beginScrollUp(item);
            }
            else
                this._isManualScrollingEnabled = false;
        }

        
        public void onDraggingToDelete()
        {
            _disableTopLevelGesture = true;
            enableNativeScrolling(false);
        }

        public void onDraggingToDeleteEnded()
        {
            _disableTopLevelGesture = false;
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
            _disableTopLevelGesture = true;
            enableNativeScrolling(false);
            enableAllChildrenGesture(false);
        }

        public void onCompletedEditText(ItemUI item)
        {
            _disableTopLevelGesture = false;
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
                if (taskListListBoxItem.DisableGesture == enable)
                    taskListListBoxItem.DisableGesture = !enable;
            }
        }

        private void updateItemGridHeight()
        {
            this.itemGrid.Height = (double)itemGrid.Children.Count * this._itemHeight;
        }

        private void beginScrollDown(ItemUI item)
        {
            if (!this._isManualScrollingEnabled)
                return;
            this._scrollViewer.ScrollToVerticalOffset(this._scrollViewer.VerticalOffset + AutoScrollDelta * (this._scrollViewer.ViewportHeight / this._scrollViewer.RenderSize.Height));
            this.updateDeltaItemPosition(item, AutoScrollDelta);
            this.Dispatcher.BeginInvoke((Action)(() => this.beginScrollDown(item)));
        }

        private void beginScrollUp(ItemUI item)
        {
            if (!this._isManualScrollingEnabled)
                return;
            this._scrollViewer.ScrollToVerticalOffset(this._scrollViewer.VerticalOffset - AutoScrollDelta * (this._scrollViewer.ViewportHeight / this._scrollViewer.RenderSize.Height));
            this.updateDeltaItemPosition(item, -AutoScrollDelta);
            this.Dispatcher.BeginInvoke((Action)(() => this.beginScrollUp(item)));
        }

        private void enableNativeScrolling(bool enable)
        {
            if (enable)
                this._scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            else
                this._scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        private int positionToIdx(double y)
        {
            return (int)Math.Floor((y + this._itemHeight / 2.0) / this._itemHeight);
        }

        private double idxToPosition(int idx)
        {
            return (double)idx * this._itemHeight;
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
