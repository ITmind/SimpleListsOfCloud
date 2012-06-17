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

        public void FillList(ListItem items)
        {
            //items.Sort();
            currItem = items;
            itemGrid.Children.Clear();
            foreach (var item in items.Items)
            {
                addItem(item);
            }
            UpdateColor();
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
            ItemUI listBoxItem = new ItemUI(this);
            listBoxItem.Tag = (object)data;
            listBoxItem.setText(data.Name);
            itemGrid.Children.Add((UIElement)listBoxItem);
            itemHeight = listBoxItem.Height;
            updateItemGridHeight();
            moveItemToIdxPosition(listBoxItem, indexOfItem(listBoxItem));
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

        public void cancelMinimizingTaskList()
        {
            AnimationUtil.translateY((FrameworkElement)maximizedTaskList, -itemHeight, CommonUtil.calcAnimateYTime((FrameworkElement)maximizedTaskList, -itemHeight, 0.7), (Action<object, EventArgs>)null);
        }

        public void minimizingTaskList(double topPosition, double pivotPosition, double ratio)
        {
            topPosition -= this.itemHeight;
            double num = pivotPosition - topPosition;
            TransformUtil.setTranslateY((FrameworkElement)this.maximizedTaskList, topPosition + ratio * num);
        }

        public void minimizedTaskList(List<ItemUI> taskList)
        {
            //this.task_listbox.IsHitTestVisible = false;
            //AnimationUtil.opacity((FrameworkElement)MainPage.self.bkgrdImg, 0.0, 300, (Action<object, EventArgs>)((s, e) => MainPage.self.bkgrdImg.Source = (ImageSource)null));
            //this.updateList(taskList);
            //for (int idx = 0; idx < this.itemList.Count; ++idx)
            //    AnimationUtil.translateY((FrameworkElement)this.itemList[idx], this.idxToPosition(idx), 200, (Action<object, EventArgs>)null);
            //this.IsHitTestVisible = true;
            //this.maximizedTaskList = (ItemUI)null;
        }

        public void maximizeTaskList(ItemUI item)
        {
            //this.IsHitTestVisible = false;
            //this.maximizedTaskList = item;
            //Point offset = item.TransformToVisual((UIElement)this).Transform(new Point(0.0, 0.0));
            //TaskListData listData = item.Tag as TaskListData;
            //this.task_listbox.colorPivot = ColourUtil.hexStringToColor(listData.colour);
            //this.task_listbox.opacityValue = listData.opacity;
            //this.task_listbox.imgFilename = listData.imgFilename;
            //this.task_listbox.isColourBkgrd = listData.isColourBkgrd;
            //this.task_listbox.isUsingGradient = listData.isUsingGradient;
            //this.task_listbox.lastModified = listData.lastModified;
            //AnimationUtil.zoom((FrameworkElement)item, -600.0, 100, (Action<object, EventArgs>)((s, ev) =>
            //{
            //    foreach (TaskData item_0 in listData.taskDataList)
            //        this.task_listbox.addItem(item_0);
            //    this.task_listbox.zoomBack(offset.Y);
            //    TimerUtil.Perform((Action)(() =>
            //    {
            //        AnimationUtil.translateY((FrameworkElement)item, -this.itemHeight, 200, (Action<object, EventArgs>)null);
            //        AnimationUtil.zoom((FrameworkElement)item, 0.0, 100, (Action<object, EventArgs>)null);
            //        this.task_listbox.updateInfoTxt();
            //        this.task_listbox.zoomFront();
            //        this.task_listbox.IsHitTestVisible = true;
            //    }), 0);
            //}));
            //int num = this.indexOfItem(item);
            //for (int index = 0; index < this.itemList.Count; ++index)
            //{
            //    if (index < num)
            //        AnimationUtil.translateY((FrameworkElement)this.itemList[index], -this.itemHeight, CommonUtil.calcAnimateYTime((FrameworkElement)this.itemList[index], -this.itemHeight), (Action<object, EventArgs>)null);
            //    else if (index > num)
            //        AnimationUtil.translateY((FrameworkElement)this.itemList[index], Math.Max(this.itemGrid.ActualHeight, 800.0), CommonUtil.calcAnimateYTime((FrameworkElement)this.itemList[index], Math.Max(this.itemGrid.ActualHeight, 800.0)), (Action<object, EventArgs>)null);
            //}
            //double v = this.scrollViewer.VerticalOffset;
            //this.Dispatcher.BeginInvoke((Action)(() => this.scrollViewer.ScrollToVerticalOffset(v)));
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
            (item.Tag as ListItem).Deleted = true;
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
            ListItem taskListData = item.Tag as ListItem;
            //if (taskListData.Name != item.text.Text)
            //{
            //    taskListData.title = item.text.Text;
            //    taskListData.lastModified = DateTimeUtil.getCurrUtcDateTime();

            //}
            disableTopLevelGesture = false;
            enableNativeScrolling(true);
            enableAllChildrenGesture(true);
        }

        public void updateList(List<ItemUI> itemList)
        {
        //    TaskListData taskListData = this.maximizedTaskList.Tag as TaskListData;
        //    taskListData.taskDataList.Clear();
        //    int num = 0;
        //    foreach (Task_ListBoxItem taskListBoxItem in itemList)
        //    {
        //        DateTime dateTime1 = taskListBoxItem.datePicker.Value.Value;
        //        DateTime dateTime2 = taskListBoxItem.timePicker.Value.Value;
        //        DateTime beginTime = new DateTime(dateTime1.Year, dateTime1.Month, dateTime1.Day, dateTime2.Hour, dateTime2.Minute, 0);
        //        if (taskListBoxItem.reminderChkbox.IsChecked.Value)
        //        {
        //            AlarmReminderUtil.deleteReminder(taskListBoxItem.reminderGuid);
        //            taskListBoxItem.reminderGuid = Guid.NewGuid().ToString();
        //            AlarmReminderUtil.createReminder(taskListBoxItem.reminderGuid, "Reminder", taskListBoxItem.text.Text, beginTime, beginTime.AddMinutes(1.0), RecurrenceInterval.None, (Uri)null);
        //        }
        //        beginTime = beginTime.ToUniversalTime();
        //        taskListData.taskDataList.Add(new TaskData()
        //        {
        //            content = taskListBoxItem.text.Text,
        //            isCompleted = taskListBoxItem.isCompleted,
        //            lastModified = taskListBoxItem.lastModified,
        //            gTaskID = taskListBoxItem.gTaskID,
        //            reminder = beginTime,
        //            reminderEnabled = taskListBoxItem.reminderChkbox.IsChecked.Value,
        //            reminderGuid = taskListBoxItem.reminderGuid
        //        });
        //        if (!taskListBoxItem.isCompleted)
        //            ++num;
        //        taskListBoxItem.lastModifiedDisplay.Text = taskListBoxItem.lastModified.ToString("M/d/yyyy hh:mm:ss tt");
        //    }
        //    this.maximizedTaskList.counter.Text = string.Concat((object)num);
        //    taskListData.colour = this.task_listbox.colorPivot.ToString();
        //    taskListData.opacity = this.task_listbox.opacityValue;
        //    taskListData.imgFilename = this.task_listbox.imgFilename;
        //    taskListData.isColourBkgrd = this.task_listbox.isColourBkgrd;
        //    taskListData.isUsingGradient = this.task_listbox.isUsingGradient;
        //    taskListData.lastModified = this.task_listbox.lastModified;
        //    this.maximizedTaskList.lastModifiedDisplay.Text = this.task_listbox.lastModified.ToString("M/d/yyyy hh:mm:ss tt");
        //    MainPage.self.updateLocalLastModified(this.task_listbox.lastModified);
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
            TransformUtil.setTranslateY((FrameworkElement)item, this.idxToPosition(idx));
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

    }
}
