using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using SimpleListsOfCloud.Utils;

namespace SimpleListsOfCloud
{
    public class ListItemEventArgs
    {
        public ListItemEventArgs(ListItem item) { Item = item; }
        public ListItem Item { get; private set; } // readonly
    }

    public class ListItem
    {
        public string Name { get; set; }
        public DateTime ModifyTime { get; set; }
        public DateTime LastSyncTime { get; set; }
        public bool Deleted { get; set; }
        public bool Mark { get; set; }
        public ObservableCollection<ListItem> Items { get; set; }

        public delegate void ListItemEventHandler(object sender, ListItemEventArgs e);

        public event EventHandler UpdateViewComplited;
        public event ListItemEventHandler AddItem;
        public event ListItemEventHandler DeleteItem;

        public ListItem()
        {
            Items = new ObservableCollection<ListItem>();       
            Name = "_StartNode";
            ModifyTime = DateTime.Now;
            LastSyncTime = ModifyTime.Subtract(new TimeSpan(0, 0, 5));
            Deleted = false;
            Mark = false;
        }

        protected void OnUpdateViewComplite(EventArgs e)
        {
            //Sort();
            EventHandler handler = UpdateViewComplited;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnAddItem(ListItemEventArgs e)
        {
            ModifyTime = DateTime.Now;
            //UpdateViews();

            ListItemEventHandler handler = AddItem;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnDeleteItem(ListItemEventArgs e)
        {
            ListItemEventHandler handler = DeleteItem;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public ListItem Copy()
        {
            var newItemList = new ListItem
                                  {
                                      Deleted = this.Deleted,
                                      Name = this.Name,
                                      ModifyTime = this.ModifyTime,
                                      LastSyncTime = this.LastSyncTime,
                                      Mark = this.Mark,
                                      Items = new ObservableCollection<ListItem>(this.Items)
                                  };
            return newItemList;
        }

        public void FillFrom(ListItem item)
        {            
            Mark = item.Mark;
            ModifyTime = LastSyncTime;
            Items = new ObservableCollection<ListItem>(item.Items);
        }

        public void UpdateViews()
        {
            if (Items.Count == 0) return;

            foreach (var item in Items)
            {
                if (item.Items.Count != 0)
                {
                    item.Mark = item.Items.All(childItem => childItem.Mark || childItem.Deleted);
                }
            }
            

        }

        public void Delete(ListItem itemForDelete)
        {
            if (Items == null || Items.Count == 0) return;

            Items.Remove(itemForDelete);
            OnDeleteItem(new ListItemEventArgs(itemForDelete));

            //foreach (var item in Items)
            //{
            //    item.Delete(itemForDelete);
            //}
        }

        /// <summary>
        /// add "new"
        /// </summary>
        public ListItem Add()
        {
            int count = 0;
            string name = "new";
            while (FindItem(name)!=null)
            {
                count++;
                name = "new"+count;
            }

            return Add(name);
        }

        public ListItem Add(string name, bool isSync = false)
        {
            if(CommonUtil.IsTrial() && Items.Count>=3)
            {
                MessageBox.Show("In trial version may be only 3 task in one list");
                return null;
            }

            if (FindItem(name) == null)
            {
                var newItem = new ListItem {Name = name, Mark = false};
                //newItem.Parent = this;
                Items.Insert(0, newItem);
                OnAddItem(new ListItemEventArgs(newItem));
                return newItem;
            }

            return null;
        }

        public ListItem FindItem(string name, bool includeDeleted = true)
        {
            //var findedGroup = from item in Items
            //                  where item.Name.Equals(name)
            //                  select item;
            var findedGroup = Items.FirstOrDefault(x => x.Name.Equals(name));

            if (findedGroup != null && (!includeDeleted && findedGroup.Deleted))
            {
                findedGroup = null;
            }
            return findedGroup;
        }

        /// <summary>
        /// ������� ��������-������, ����� �������.
        /// ���������� �� ��������
        /// </summary>
        public void Sort()
        {
            Items = new ObservableCollection<ListItem>(Items.OrderByDescending(x => x.Items.Count).ThenBy(x => x.Name).ToList());
            foreach (var item in Items)
            {
                item.Sort();
            }
            //Items = new ObservableCollection<ListItem>(Items.OrderBy(x => x.Name).ToList());
        }

        public static ListItem FindParrent(ListItem startNode, ListItem findNode)
        {
            var result = startNode;
            if(!startNode.Items.Contains(findNode))
            {                
                foreach (var item in startNode.Items)
                {
                    result = FindParrent(item, findNode); 
                    if(result!=null) break;
                }                
            }

            return result;
        }

        public void SetDeleted(bool deleted)
        {
            Deleted = deleted;
            ModifyTime = DateTime.Now;
            //UpdateViews();
        }

        public void SetMark(bool mark)
        {
            ModifyTime = DateTime.Now;
            Mark = mark;
        }

        public void SetLastSyncTime(DateTime time)
        {
            LastSyncTime = time;
            foreach (var item in Items)
            {
                item.LastSyncTime = time;
                foreach (var listItem in item.Items)
                {
                    listItem.LastSyncTime = time;
                }
            }
        }

        public void SetModifyTime(DateTime time)
        {
            ModifyTime = time;
            foreach (var item in Items)
            {
                item.ModifyTime = time;
                foreach (var listItem in item.Items)
                {
                    listItem.ModifyTime = time;
                }
            }
        }

        public void UpdateModifyTime()
        {            
            foreach (var item in Items)
            {
                //item.ModifyTime = time;
                foreach (var listItem in item.Items.Where(listItem => item.ModifyTime < listItem.ModifyTime))
                {
                    item.ModifyTime = listItem.ModifyTime;
                }
            }
        }

        public void DeleteDeletedItems()
        {
            var itemsForDel = Items.Where(item => item.Deleted && item.ModifyTime < item.LastSyncTime).ToList();

            foreach (var item in itemsForDel)
            {
                Delete(item);
            }

            //� ���������� ������� �����������
            foreach (var item in Items)
            {
                item.DeleteDeletedItems();
            }
        }
    }
}