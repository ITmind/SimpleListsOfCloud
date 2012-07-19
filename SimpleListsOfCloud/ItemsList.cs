using System;
using System.Net;
using System.Runtime.Serialization;
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
using System.Xml.Serialization;
using WP7Mail.Helpers;
using WP7Mail.Net;

namespace SimpleListsOfCloud
{
    public class ItemsList
    {
        private ListItem _startNode;
        public event EventHandler GetDataComplited;      

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

        #region EventHandler
        
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

        #endregion

        //protected void On
        public ItemsList()
        {
            StartNode = new ListItem();
            UploadComplite = false;
            DownloadComplite = false;
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
                    UpdateModifyTime();

                    //опреации производим с кешем
                    UploadSync(files, _startNode.Copy());

                    if (_uploadCounter <= 0)
                    {
                        OnUploadComplited(EventArgs.Empty);
                    }
                }
                else if (!DownloadComplite)
                {
                    DownloadSync(files);
                    if (_downloadCounter <= 0)
                    {
                        //Save();
                        OnGetDataComplited(EventArgs.Empty);
                    }
                }

                //удалим остатки
                //DeleteNotSyncItems(StartNode.Items);
            }

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


        public void DeleteSyncItems()
        {
            StartNode.DeleteSyncItems();
        }

        void UpdateModifyTime()
        {
            foreach (var item in StartNode.Items)
            {
                foreach (var listItem in item.Items)
                {
                    if(item.ModifyTime<listItem.ModifyTime)
                    {
                        item.ModifyTime = listItem.ModifyTime;
                    }
                    else if (item.ModifyTime > listItem.ModifyTime)
                    {
                        listItem.ModifyTime = item.ModifyTime;
                    }
                }
            }
        }
    
        void MailSync()
        {
            GMailOAuthHelper v= new GMailOAuthHelper("anonymous","anonymous");
            PopClient pop = new PopClient("pop.gmail.com",110);
        }
    }

    
}
