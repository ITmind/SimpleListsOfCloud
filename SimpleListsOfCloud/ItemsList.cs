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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Microsoft.Live;
using System.IO;
using System.Linq;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;

namespace SimpleListsOfCloud
{
    public class ItemsList
    {
        private int downloadCounter = 0;
        private int uploadCounter = 0;
        
        private ListItem startNode;
        public event EventHandler GetDataComplited;
        public ListItem StartNode
        {
            get
            {
                return startNode;
            }
            set
            {
                startNode = value;
            }
        }

        public bool UploadComplite { get; set; }
        public bool DownloadComplite { get; set; }
        //TODO: for fast sync chage to ListItem
        List<string> syncItems = new List<string>();

        protected void OnGetDataComplited(EventArgs e)
        {
            DownloadComplite = true;
            foreach (var item in StartNode.Items)
            {
                if (!syncItems.Contains(item.Name))
                {
                    item.Deleted = true;
                }
            }

            DeleteSyncItems();
            Save();

            EventHandler handler = GetDataComplited;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnUploadComplited(EventArgs e)
        {
            UploadComplite = true;
            App.Current.SkyDriveFolders.GetFiles();
           //DownloadSync(App.Current.SkyDriveFolders.files);
        }

        public ItemsList()
        {
            StartNode = new ListItem();
            UploadComplite = false;
            DownloadComplite = false;
        }

        private void UploadItems(ListItem item)
        {
            LiveConnectClient client = new LiveConnectClient(App.Current.LiveSession);            
            client.UploadCompleted += new EventHandler<LiveOperationCompletedEventArgs>(client_UploadCompleted);

            MemoryStream newFile = new MemoryStream();
            StreamWriter writer = new StreamWriter(newFile);
            foreach (var listItem in item.Items)
            {
                if (listItem.Deleted) continue;
                writer.WriteLine(listItem.Name);
            }
            writer.Flush();
            //writer.Close();

            client.UploadAsync(App.Current.SkyDriveFolders.FolderID, item.Name + ".txt", newFile);
        }

        private void UploadSync(IList<object> files)
        {
            List<string> filesForDelete = new List<string>();
            syncItems.Clear();

            LiveConnectClient client = new LiveConnectClient(App.Current.LiveSession);
            client.UploadCompleted += new EventHandler<LiveOperationCompletedEventArgs>(client_UploadCompleted);
            foreach (IDictionary<string, object> content in files)
            {
                string filename = (string)content["name"];
                if (!filename.EndsWith(".txt"))
                {
                    continue;
                }
                string name = filename.Substring(0, filename.Length - 4);
                ListItem newGroup = FindGroup(name);
                DateTime update_time = Convert.ToDateTime(content["updated_time"]);
                if (newGroup != null && !newGroup.Deleted)
                {
                    syncItems.Add(name);
                    //проверим время модификации
                    if (newGroup.ModifyTime >= update_time)
                    {
                        //делаем загрузку в облако
                        uploadCounter++;
                        UploadItems(newGroup);
                        newGroup.Sync = true;
                        //continue;
                    }
                    else
                    {
                        newGroup.Sync = false;
                    }
                }
                else if (newGroup != null && newGroup.Deleted)
                {
                    filesForDelete.Add((string)content["id"]);
                    //client.UploadAsync(String.Format("{0}/content", (string)content["id"]), newGroup);    
                }               
            }

            foreach (var item in StartNode.Items)
            {
                if (!item.Sync && !item.Deleted && !syncItems.Contains(item.Name))
                {
                    uploadCounter++;
                    UploadItems(item);
                }
            }

            foreach (var file in filesForDelete)
            {
                client.DeleteAsync(file);                
            }
        }

        void client_UploadCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            uploadCounter--;
            if (uploadCounter <= 0)
            {
                OnUploadComplited(EventArgs.Empty);
            }           
        }

        private void DownloadSync(IList<object> files)
        {
            syncItems.Clear();
            LiveConnectClient client = new LiveConnectClient(App.Current.LiveSession);
            client.DownloadCompleted += new EventHandler<LiveDownloadCompletedEventArgs>(OnDownloadCompleted);
            foreach (IDictionary<string, object> content in files)
            {
                string filename = (string)content["name"];
                if (!filename.EndsWith(".txt"))
                {
                    continue;
                }
                string name = filename.Substring(0, filename.Length - 4);
                ListItem newGroup = FindGroup(name);
                DateTime update_time = Convert.ToDateTime(content["updated_time"]);
                if (newGroup != null && !newGroup.Sync)
                {
                    //проверим время модификации

                    if (newGroup.ModifyTime >= update_time)
                    {
                        newGroup.Sync = false;                        
                    }
                    else
                    {
                        downloadCounter++;
                        client.DownloadAsync(String.Format("{0}/content", (string)content["id"]), newGroup);
                    }
                }
                else if (newGroup != null && newGroup.Sync)
                {
                    syncItems.Add(name);
                }
                else if (newGroup == null)
                {
                    newGroup = new ListItem();
                    newGroup.Name = name;
                    newGroup.ModifyTime = update_time;
                    StartNode.Items.Add(newGroup);
                    downloadCounter++;
                    client.DownloadAsync(String.Format("{0}/content", (string)content["id"]), newGroup);
                }

                
            }
        }

        /// <summary>
        /// load from skydrive
        /// </summary>
        /// <param name="files"></param>
        public void Sync(IList<object> files)
        {
            //downloadCounter = files.Count;
            if (App.Current.LiveSession == null)
            {
                //infoTextBlock.Text = "You must sign in first.";
            }
            else
            {                   
                //сначала грузим все в облако
                if (!UploadComplite)
                {
                    UploadSync(files);
                    if (uploadCounter <= 0)
                    {
                        OnUploadComplited(EventArgs.Empty);
                    }
                }
                else if (!DownloadComplite)
                {
                    DownloadSync(files);
                    if (downloadCounter <= 0)
                    {
                        //Save();
                        OnGetDataComplited(EventArgs.Empty);
                    }
                }

                //удалим остатки
                //DeleteNotSyncItems(StartNode.Items);
            }

        }

        void OnDownloadCompleted(object sender, LiveDownloadCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                ListItem newGroup = (ListItem)e.UserState;
                newGroup.Items.Clear();
                StreamReader sr = new StreamReader(e.Result);
                string Text = sr.ReadToEnd();                
                e.Result.Close();
                newGroup.Items = new ObservableCollection<ListItem>();             

                string[] itemsArray = Text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (var task in itemsArray)
                {
                    newGroup.Items.Add(new ListItem() { Name = task, Sync = true });
                }
                newGroup.Sync = true;
                syncItems.Add(newGroup.Name);                
            }
            else
            {
                //infoTextBlock.Text = "Error downloading image: " + e.Error.ToString();
            }

            downloadCounter--;
            if (downloadCounter <= 0)
            {
                //Save();
                OnGetDataComplited(EventArgs.Empty);
            }
        }

        ListItem FindGroup(string groupName)
        {
            var findedGroup = from item in StartNode.Items
                           where item.Name.Equals(groupName)
                           select item;
            if (findedGroup.Count() > 0)
            {
                return findedGroup.First();
            }

            return null;

        }
        /// <summary>
        /// load from is
        /// </summary>
        public void Load()
        {
            IsolatedStorageSettings iss = IsolatedStorageSettings.ApplicationSettings;
            if (PhoneApplicationService.Current.State.ContainsKey("ItemsList"))
            {
                StartNode = PhoneApplicationService.Current.State["ItemsList"] as ListItem;
            }
            else
            {
                if (!iss.TryGetValue("ItemsList", out startNode))
                {
                    StartNode = new ListItem();
                }
            }
            //OnGetDataComplited(EventArgs.Empty);
            EventHandler handler = GetDataComplited;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void Save()
        {
            PhoneApplicationService.Current.State["ItemsList"] = StartNode;
            IsolatedStorageSettings iss = IsolatedStorageSettings.ApplicationSettings;
            if (!iss.Contains("ItemsList"))
            {
                iss.Add("ItemsList", StartNode);
            }
            else
            {
                iss["ItemsList"] = StartNode;
            }
            iss.Save();
        }


        public void DeleteSyncItems()
        {
            DeleteSyncItems(StartNode.Items);
        }

        void DeleteSyncItems(ObservableCollection<ListItem> list)
        {
            if (list == null || list.Count == 0) return;
            List<ListItem> itemsForDel = new List<ListItem>();

            foreach (var item in list)
            {
                var findedItems = from itemForDelete in StartNode.Items
                                  where itemForDelete.Sync && itemForDelete.Deleted
                                  select itemForDelete;

                foreach (var itemForDelete in findedItems.ToList())
                {
                    //StartNode.Delete(itemForDelete);
                    itemsForDel.Add((ListItem)itemForDelete);
                }

                DeleteSyncItems(item.Items);
            }

            foreach (var item in itemsForDel)
            {
                StartNode.Delete(item);
            }
        }

        void DeleteNotSyncItems(ObservableCollection<ListItem> list)
        {
            if (list == null || list.Count == 0) return;

            foreach (var item in list)
            {
                var findedItems = from itemForDelete in StartNode.Items
                                  where !itemForDelete.Sync
                                  select itemForDelete;

                foreach (var itemForDelete in findedItems)
                {
                    StartNode.Delete(itemForDelete);
                }

                DeleteNotSyncItems(item.Items);
            }
        }

    }

    public class ListItemEventArgs
    {
        public ListItemEventArgs(ListItem item) { Item = item; }
        public ListItem Item { get; private set; } // readonly
    }

    public class ListItem
    {
        private bool sync;
        private bool deleted;

        public string Name { get; set; }
        public DateTime ModifyTime { get; set; }
        public bool Sync
        {
            get
            {
                return sync;
            }
            set
            {
                sync = value;
                if (Items != null && Items.Count > 0)
                {
                    foreach (var item in Items)
                    {
                        item.Sync = value;
                    }
                }
            }
        }
        public bool Deleted
        {
            get
            {
                return deleted;
            }
            set
            {
                deleted = value;
                if (Items != null && Items.Count > 0)
                {
                    foreach (var item in Items)
                    {
                        item.Deleted = value;
                    }
                }
            }
        }

        ObservableCollection<ListItem> _items { get; set; }
        public ObservableCollection<ListItem> Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items = value;
                _items.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Items_CollectionChanged);
                UpdateViews();
            }
        }
        public ObservableCollection<ListItem> ViewItems { get; set; }

        public delegate void ListItemEventHandler(object sender, ListItemEventArgs e);

        public event EventHandler UpdateViewComplited;
        public event ListItemEventHandler AddItem;
        public event ListItemEventHandler DeleteItem;

        public ListItem()
        {
            ViewItems = new ObservableCollection<ListItem>();
            Items = new ObservableCollection<ListItem>();       
            Name = "new";
            ModifyTime = DateTime.Now;
            Sync = false;
            Deleted = false;
            
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

        void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ModifyTime = DateTime.Now;
            UpdateViews();
        }

        public void UpdateViews()
        {
            if (Items.Count == 0) return;
            ViewItems.Clear();
            foreach (var item in Items)
            {
                if (item.Deleted) continue;
                ViewItems.Add(item);
            }
            OnUpdateViewComplite(EventArgs.Empty);
        }

        public void Delete(ListItem itemForDelete)
        {
            if (Items == null || Items.Count == 0) return;

            Items.Remove(itemForDelete);
            OnDeleteItem(new ListItemEventArgs(itemForDelete));

            foreach (var item in Items)
            {
                item.Delete(itemForDelete);
            }
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

        public ListItem Add(string name)
        {
            ListItem newItem = new ListItem();
            newItem.Name = name;
            Items.Insert(0, newItem);
            OnAddItem(new ListItemEventArgs(newItem));
            return newItem;
        }

        ListItem FindItem(string name)
        {
            var findedGroup = from item in Items
                              where item.Name.Equals(name)
                              select item;
            if (findedGroup.Count() > 0)
            {
                return findedGroup.First();
            }

            return null;

        }

        /// <summary>
        /// сначала элементы-группы, затем обычные.
        /// Сортировка по алфавиту
        /// </summary>
        public void Sort()
        {
            Items = new ObservableCollection<ListItem>(Items.OrderByDescending(x => x.Items.Count).ThenBy(x => x.Name).ToList());
            //Items = new ObservableCollection<ListItem>(Items.OrderBy(x => x.Name).ToList());
        }

    }
}
