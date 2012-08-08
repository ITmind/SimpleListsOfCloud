using System;
using System.ComponentModel;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Live;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using WP7Mail.Helpers;
using WP7Mail.Net;

namespace SimpleListsOfCloud
{
    public class ItemsList
    {
        private ListItem _startNode;
        public event EventHandler GetDataComplited;
        public event EventHandler<MessageEventArgs> Error;

        public ListItem StartNode
        {
            get
            {
                return _startNode;
            }
            set
            {
                _startNode = value;
            }
        }

        private DateTime _lastSyncTime;
        private ListItem _cache;
        private IList<object> _files;

        #region EventHandler
        
        protected void OnGetDataComplited(EventArgs e)
        {
            //foreach (var item in StartNode.Items)
            //{
            //    if (!syncItems.Contains(item.Name))
            //    {
            //        item.Deleted = true;
            //    }
            //}

            //DeleteSyncItems();
            Save();

            EventHandler handler = GetDataComplited;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void OnError(string message)
        {
            EventHandler<MessageEventArgs> handler = Error;
            if (handler != null) handler(this, new MessageEventArgs(message));
        }

        #endregion

        //protected void On
        public ItemsList()
        {
            StartNode = new ListItem();
        }

        /// <summary>
        /// load from skydrive
        /// </summary>
        /// <param name="files"></param>
        public void Sync(IList<object> files)
        {
            _files = files;
            //downloadCounter = files.Count;
            if (App.Current.LiveSession == null)
            {
                //infoTextBlock.Text = "You must sign in first.";
            }
            else
            {
                _cache = StartNode.Copy();
                _cache.UpdateModifyTime();
                _lastSyncTime = DateTime.Now;

                var upload = new Upload(_cache);
                upload.UploadComplited += UploadUploadComplited;
                upload.Error += SyncError;
                upload.Start(_files);
                
            }

        }

        void SyncError(object sender, MessageEventArgs e)
        {
            OnError(e.Message);
        }

        void SyncWithCache(ListItem cache)
        {
            var syncItem = new List<ListItem>(10);
            var forDelete = new List<ListItem>(10);

            foreach (var item in StartNode.Items)
            {
                ListItem cacheItem = cache.FindItem(item.Name);
                if (cacheItem == null)
                {
                    if (item.ModifyTime < item.LastSyncTime)
                    {
                        forDelete.Add(item);
                    }
                }
                else
                {
                    syncItem.Add(cacheItem);

                    if (item.ModifyTime < item.LastSyncTime)
                    {
                        item.FillFrom(cacheItem);
                    }
                    else
                    {
                        //проверим все подчиненные
                        foreach (var listItem in item.Items)
                        {
                            if (listItem.ModifyTime < listItem.LastSyncTime)
                            {
                                var cacheItem2 = cacheItem.FindItem(listItem.Name);
                                if (cacheItem2 == null) continue;

                                item.FillFrom(cacheItem2);
                            }
                        }
                    }
                }
            }

            foreach (var item in cache.Items)
            {
                if (!syncItem.Contains(item))
                {
                    StartNode.Items.Add(item.Copy());
                }
            }

            foreach (var item in forDelete)
            {
                StartNode.Delete(item);
            }
        }

        void DownloadDownloadComplited(object sender, EventArgs e)
        {
            _cache.DeleteDeletedItems();
            _cache.SetModifyTime(_lastSyncTime);
            //зафиксируем время последней успешной синхронизации
            StartNode.SetLastSyncTime(_lastSyncTime);
            //удалим все помеченные на удаление и у которых время модификации меньше времени синхронизации
            StartNode.DeleteDeletedItems();
            //синхронизируем кеш с рабочими данными
            SyncWithCache(_cache);

            OnGetDataComplited(EventArgs.Empty);
            
        }

        void UploadUploadComplited(object sender, EventArgs e)
        {
            var download = new Download(_cache);
            download.DownloadComplited += DownloadDownloadComplited;
            download.Error += SyncError;
            download.Start(_files);
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
                if (!iss.TryGetValue("ItemsList", out _startNode))
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
 
        void MailSync()
        {
            GMailOAuthHelper v= new GMailOAuthHelper("anonymous","anonymous");
            PopClient pop = new PopClient("pop.gmail.com",110);
        }
    }

    
}
