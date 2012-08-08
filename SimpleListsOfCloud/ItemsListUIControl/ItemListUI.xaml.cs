using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public event EventHandler<MessageEventArgs> ListRefill;

        public void OnListRefill(string folderName)
        {
            EventHandler<MessageEventArgs> handler = ListRefill;
            if (handler != null) handler(this, new MessageEventArgs(folderName));
        }

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
        readonly BackgroundWorker _backgroundAdd = new BackgroundWorker();



        public ItemListUI()
        {
            InitializeComponent();
            this.Loaded += (RoutedEventHandler)((s, e) => _scrollViewer = UiUtil.FindChildOfType<ScrollViewer>((DependencyObject)listbox));
            _backgroundAdd.DoWork += BackgroundAddDoWork;
        }

        void BackgroundAddDoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 11; i < CurrItem.Items.Count; i++)
            {
                int i1 = i;
                Dispatcher.BeginInvoke(() =>
                                           {
                                               if (!CurrItem.Items[i1].Deleted)
                                               {
                                                   AddItem(CurrItem.Items[i1]);
                                               }
                                               itemGrid.UpdateLayout();
                                           });

            }
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
            if (TransformUtil.GetScaleY((FrameworkElement)_newItem) < 0.7)
            {
                itemGrid.Children.Remove((UIElement)_newItem);
            }
            else
            {
                TransformUtil.SetScaleY((FrameworkElement)_newItem, 1.0);
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
            UpdateItemGridHeight();
            for (int idx = IndexOfItem(_newItem) + 1; idx < itemGrid.Children.Count; ++idx)
                TransformUtil.setTranslateY((FrameworkElement)itemGrid.Children[idx], IdxToPosition(idx));
            _newItem = (ItemUI)null;
            _isAddingItemViaDragTop = false;
            EnableNativeScrolling(true);
            EnableAllChildrenGesture(true);
        }

        private void onDragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            if (_disableTopLevelGesture || !_isDraggingListBox)
                return;
            bool flag1 = e.GetPosition(listbox).Y > _dragStartY;
            bool flag2 = _scrollViewer.VerticalOffset < 10.0 / itemGrid.Height;
            double verticalOffset = _scrollViewer.VerticalOffset;
            double scrollableHeight = _scrollViewer.ScrollableHeight;

            if (!_isAddingItemViaDragTop && flag2 && flag1)
            {                
                e.Handled = true;
                _isAddingItemViaDragTop = true;
                EnableAllChildrenGesture(false);
                _newItem = AddItem("new item");
                
                TransformUtil.setScaleX(_newItem, 0.9);
                TransformUtil.setTranslateX(_newItem, 20.0);

            }
            else
            {
                
                if (!_isAddingItemViaDragTop)
                    return;                
                e.Handled = true;

                double num1 = Math.Min((_lastDraggedYDist + e.VerticalChange) / _itemHeight, 1.0);

                _lastDraggedYDist += e.VerticalChange;
                TransformUtil.SetScaleY(_newItem, num1);
                TransformUtil.setScaleX(_newItem, 0.9+num1/10);
                TransformUtil.setTranslateX(_newItem, 20.0-(num1*20));

                UpdateItemGrid(0);

            }
        }

        #endregion

        public void RefillList()
        {
            //FillList(currItem);
            var forDelete = new List<ItemUI>(10);
            var presentInGrid = new List<ListItem>(10);

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
                AddItem(item);
            }

            UpdateItemGridHeight();
            for (int idx = 0; idx < itemGrid.Children.Count; ++idx)
                AnimationUtil.translateY((FrameworkElement)itemGrid.Children[idx], IdxToPosition(idx), 50, null);
            UpdateColor();
        }

        public void FillList(ListItem items)
        {
            OnListRefill(items.Name);
            items.UpdateViews();
            CurrItem = items;
            MinimizedTaskList(); 
        }

        void Fill()
        {
            //TODO: загружаем сразу 10 элементов, остальноые в фоне
            Stopwatch stopwatch = Stopwatch.StartNew();
            itemGrid.Children.Clear();
            int numloaded = 0;
            foreach (var item in CurrItem.Items)
            {
                if(item.Deleted) continue;                
                AddItem(item);
                numloaded++;
                if(numloaded>=10) break;
            }

            if (CurrItem.Items.Count > numloaded)
            {
                //start background fill
                _backgroundAdd.RunWorkerAsync();
            }

            UpdateColor();
            stopwatch.Stop();
            Debug.WriteLine("fill time: {0}",stopwatch.ElapsedMilliseconds);
            MaximizeTaskList();
        }

        public void UpdateColor()
        {
            foreach (ItemUI item in itemGrid.Children)
            {
                item.Update();
            }
        }

        public ItemUI AddItem(ListItem data)
        {
            var listBoxItem = new ItemUI(this) {Tag = data};
            listBoxItem.SetText(data.Name);
            itemGrid.Children.Add(listBoxItem);
            TransformUtil.setTranslateY(listBoxItem, 0);

            listBoxItem.markComplite.Visibility = data.Mark ? Visibility.Visible : Visibility.Collapsed;
            itemGrid.UpdateLayout();
            _itemHeight = listBoxItem.ActualHeight;
            UpdateItemGridHeight();
            MoveItemToIdxPosition(listBoxItem, IndexOfItem(listBoxItem));
            SortZIndex();
            return listBoxItem;
        }

        public ItemUI AddItem(string text)
        {

            var listBoxItem = new ItemUI(this);
            listBoxItem.SetText("new item");
            TransformUtil.SetScaleY(listBoxItem, 0);
            itemGrid.Children.Insert(0,listBoxItem);
            //обновим иначе не получим высоту
            itemGrid.UpdateLayout();
            _itemHeight = listBoxItem.ActualHeight;
            //Debug.WriteLine("add new item with height:{0}",_itemHeight);
            MoveItemToIdxPosition(listBoxItem, IndexOfItem(listBoxItem));
            SortZIndex();
            return listBoxItem;
        }
            

        public void MinimizedTaskList()
        {
            _minimizedCount = itemGrid.Children.Count;
            if (_minimizedCount == 0)
            {
                AnimationUtil.opacity(itemGrid, 0.0, 200, CallbackMinimized);
                //Fill();                
            }
            else
            {
                AnimationUtil.opacity(itemGrid, 0.0, 200, null);

                foreach (UIElement t in itemGrid.Children)
                    AnimationUtil.translateY((FrameworkElement) t, 0, 100, CallbackMinimized);
            }
        }

        private void CallbackMinimized(object o, EventArgs eventArgs)
        {
            _minimizedCount--;
            if (_minimizedCount <= 0)
            {
                Fill();
            }
        }

        public void MaximizeTaskList()
        {
            //TODO: im many items not work
            AnimationUtil.opacity(itemGrid, 1.0, 200, null);
            //this.updateList(taskList);
            for (int idx = 0; idx < itemGrid.Children.Count; ++idx)
                AnimationUtil.translateY((FrameworkElement)itemGrid.Children[idx], IdxToPosition(idx), 100, null);            
        }

        public void onReorderStarted(ItemUI child)
        {
            if (itemGrid.Children.Count <= 1)
                return;
            _isReordering = true;
            EnableNativeScrolling(false);
            _isManualScrollingEnabled = false;
            child.OnReorderStarted();
        }

        public void onReorderCompleted(ItemUI child)
        {
            _isReordering = false;
            EnableNativeScrolling(true);
            _isManualScrollingEnabled = false;
            SortZIndex();
            AnimationUtil.translateY(child, IdxToPosition(IndexOfItem(child)), 200, null);
            //поменяем позицию в объекте
            CurrItem.Items.Remove((ListItem) child.Tag);
            CurrItem.Items.Insert(IndexOfItem(child), (ListItem)child.Tag);
            child.OnReorderCompleted();
        }

        public void onReorderDelta(ItemUI child, double delta)
        {
            UpdateDeltaItemPosition(child, delta);
            CheckIfScrollingIsNeeded(child);
        }

        private void UpdateDeltaItemPosition(ItemUI item, double delta)
        {
            int idx = IndexOfItem(item);
            MoveDeltaItemWithinBounds(item, delta);
            int num = PositionToIdx(item);
            if (idx == num)
                return;
            int index = idx - num < 0 ? idx + 1 : idx - 1;
            SwapItemsInList(item, (ItemUI)itemGrid.Children[index]);
            //Debug.WriteLine("index:{0} idx:{1}",index,idx);
            itemGrid.UpdateLayout();
            AnimationUtil.translateY((FrameworkElement)itemGrid.Children[idx], IdxToPosition(idx), 200, null);            
            
        }

        private void CheckIfScrollingIsNeeded(ItemUI item)
        {
            double num1 = _scrollViewer.RenderSize.Height + _scrollViewer.VerticalOffset * itemGrid.ActualHeight - AutoScrollHitAreaHeight;
            double num2 = AutoScrollHitAreaHeight + _scrollViewer.VerticalOffset * itemGrid.ActualHeight;
            double translateY = TransformUtil.getTranslateY((FrameworkElement)item);
            if (translateY > num1)
            {
                if (_isManualScrollingEnabled)
                    return;
                _isManualScrollingEnabled = true;
                BeginScrollDown(item);
            }
            else if (translateY + this._itemHeight < num2)
            {
                if (this._isManualScrollingEnabled)
                    return;
                this._isManualScrollingEnabled = true;
                this.BeginScrollUp(item);
            }
            else
                this._isManualScrollingEnabled = false;
        }

        
        public void onDraggingToDelete()
        {
            _disableTopLevelGesture = true;
            EnableNativeScrolling(false);
        }

        public void onDraggingToDeleteEnded()
        {
            _disableTopLevelGesture = false;
            EnableNativeScrolling(true);
        }

        public void onDeleteItem(ItemUI item)
        {
            ((ListItem) item.Tag).SetDeleted(true);
            int num = IndexOfItem(item);
            itemGrid.Children.Remove(item);            
            UpdateItemGridHeight();
            for (int idx = num; idx < itemGrid.Children.Count; ++idx)
                AnimationUtil.translateY((FrameworkElement)itemGrid.Children[idx], IdxToPosition(idx), 100, null);
            SortZIndex();
        }

        public void UpdateItemGrid(int animSpeed = 100)
        {
            //при сортировке не перемещаем
            if (_isReordering) return;

            //Debug.WriteLine("Update itemGrid");
            
            UpdateItemGridHeight();
            //return;
            for (int idx = 0; idx < itemGrid.Children.Count; ++idx)
            {
                if(animSpeed == 0)
                {
                    TransformUtil.setTranslateY((FrameworkElement)itemGrid.Children[idx], IdxToPosition(idx));
                }
                else
                {
                    AnimationUtil.translateY((FrameworkElement)itemGrid.Children[idx], IdxToPosition(idx), 1, null);    
                }
                
            }
        }

        public void onEditText()
        {
            _disableTopLevelGesture = true;
            EnableNativeScrolling(false);
            EnableAllChildrenGesture(false);
        }

        public void onCompletedEditText(ItemUI item)
        {
            _disableTopLevelGesture = false;
            EnableNativeScrolling(true);
            EnableAllChildrenGesture(true);
        }

       
        private void SortZIndex()
        {
            for (int index = 0; index < itemGrid.Children.Count; ++index)
                Canvas.SetZIndex((UIElement)itemGrid.Children[index], itemGrid.Children.Count - index);
        }

        private void EnableAllChildrenGesture(bool enable)
        {
            foreach (ItemUI taskListListBoxItem in itemGrid.Children)
            {
                if (taskListListBoxItem.DisableGesture == enable)
                    taskListListBoxItem.DisableGesture = !enable;
            }
        }

        private void UpdateItemGridHeight()
        {            
            itemGrid.Height = itemGrid.Children.Sum(x => ((ItemUI) x).ActualHeight);
            //Debug.WriteLine("Update itemGrid height: {0}", itemGrid.Height);
            //itemGrid.Height = (double)itemGrid.Children.Count * this._itemHeight;
        }

        private void BeginScrollDown(ItemUI item)
        {
            if (!_isManualScrollingEnabled)
                return;
            _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + AutoScrollDelta * (_scrollViewer.ViewportHeight / _scrollViewer.RenderSize.Height));
            UpdateDeltaItemPosition(item, AutoScrollDelta);
            Dispatcher.BeginInvoke(() => BeginScrollDown(item));
        }

        private void BeginScrollUp(ItemUI item)
        {
            if (!_isManualScrollingEnabled)
                return;
            _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - AutoScrollDelta * (_scrollViewer.ViewportHeight / _scrollViewer.RenderSize.Height));
            UpdateDeltaItemPosition(item, -AutoScrollDelta);
            Dispatcher.BeginInvoke(() => BeginScrollUp(item));
        }

        private void EnableNativeScrolling(bool enable)
        {
            if (enable)
                _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            else
                _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        private int PositionToIdx(ItemUI item)
        {
            //var transform = item.TransformToVisual(itemGrid);
            //var absolutePosition = transform.TransformBounds(new Rect(0,0,0,0));
            double y = TransformUtil.getTranslateY(item);
            var currItemIndex = itemGrid.Children.IndexOf(item);

            double curr = 0.0;
            for (int index = 0; index < itemGrid.Children.Count; index++)
            {
                var child = itemGrid.Children[index] as ItemUI;
                if (child == null) return currItemIndex;

                if ((index < currItemIndex && (y <= curr + child.ActualHeight / 2)) ||
                    (index > currItemIndex && (y + item.ActualHeight >= curr + child.ActualHeight / 2)))
                {
                    //Debug.WriteLine("index: {0}",index);
                    return index;
                }
                

                curr += child.ActualHeight;
            }

            //var temp = (int)Math.Floor((y + item.ActualHeight / 2.0) / item.ActualHeight);
            //Debug.WriteLine("Position {0} TO Idx: {1}",y,temp);
            return currItemIndex;
        }

        private double IdxToPosition(int idx)
        {
            //Debug.WriteLine("idx {0}",idx);
            double result = 0.0;
            if (itemGrid.Children != null)
            {
                for (int i = 0; i < idx; i++)
                {
                    var item = itemGrid.Children[i] as ItemUI;
                    if (item != null)
                    {
                        //Debug.WriteLine("{0}: ActualHeight {1} ScaleY {2}", i, item.ActualHeight, TransformUtil.GetScaleY(item));
                        result += item.ActualHeight * TransformUtil.GetScaleY(item);
                    }

                }
            }
            //var result = itemGrid.Children.Sum(x => ((ItemUI) x).ActualHeight);
            //Debug.WriteLine("result {0}",result);
            return result;
            //return (double)idx * _itemHeight;
        }

        private void MoveItemToIdxPosition(ItemUI item, int idx)
        {
            TransformUtil.setTranslateY(item, IdxToPosition(idx));
        }

        private void MoveDeltaItemWithinBounds(ItemUI item, double delta)
        {
            int count = itemGrid.Children.Count;
            double num = TransformUtil.getTranslateY(item) + delta;
            if (num < 0.0)
                num = 0.0;
            else if (num > IdxToPosition(count - 1))
                num = IdxToPosition(count - 1);
            TransformUtil.setTranslateY(item, num);
        }

        private int IndexOfItem(ItemUI item)
        {
            return itemGrid.Children.IndexOf(item);
        }

        private void SwapItemsInList(ItemUI a, ItemUI b)
        {
            int index1 = IndexOfItem(a);
            int index2 = IndexOfItem(b);
            itemGrid.Children[index1] = new ItemUI();
            itemGrid.Children[index2] = a;
            itemGrid.Children[index1] = b;
            
        }

        public void OnUncompleteItem(ItemUI item)
        {
            ((ListItem) item.Tag).SetMark(false);
            item.Update();
        }

        public void OnCompleteItem(ItemUI item)
        {
            if (item.Tag != null)
            {
                ((ListItem) item.Tag).SetMark(true);
                item.Update();
            }
        }
    }
}
