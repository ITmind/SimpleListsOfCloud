using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
        public bool Sync { get; set; }
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
            Sync = false;
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
                                      Sync = this.Sync,
                                      Mark = this.Mark,
                                      Items = new ObservableCollection<ListItem>(this.Items)
                                  };
            return newItemList;
        }

        public void UpdateViews()
        {
            if (Items.Count == 0) return;

            foreach (var item in Items)
            {
                if (item.Items.Count == 0)
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
            if (FindItem(name) == null)
            {
                var newItem = new ListItem {Name = name, Sync = isSync, Mark = false};
                //newItem.Parent = this;
                Items.Insert(0, newItem);
                OnAddItem(new ListItemEventArgs(newItem));
                return newItem;
            }
            else
            {
                return null;
            }
        }

        public ListItem FindItem(string name)
        {
            var findedGroup = from item in Items
                              where item.Name.Equals(name)
                              select item;
            return findedGroup.FirstOrDefault();
        }

        /// <summary>
        /// сначала элементы-группы, затем обычные.
        /// Сортировка по алфавиту
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
                    result = ListItem.FindParrent(item, findNode); 
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

        public void DeleteSyncItems()
        {
            var itemsForDel = new List<ListItem>();
            foreach (var item in Items)
            {
                if (item.Deleted && item.Sync)
                {
                    itemsForDel.Add(item);
                }
            }

            foreach (var item in itemsForDel)
            {
                Delete(item);
            }

            //в оставщихся удаляем подчиненные
            foreach (var item in Items)
            {
                item.DeleteSyncItems();
            }
        }
    }
}