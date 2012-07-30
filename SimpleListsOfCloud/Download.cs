using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Live;
using SimpleListsOfCloud.Utils;

namespace SimpleListsOfCloud
{
    public class Download : IDisposable
    {
        public event EventHandler DownloadComplited;
        public event EventHandler<MessageEventArgs> Error;

        private readonly LiveConnectClient _client;
        private readonly ListItem _cache;
        private int _downloadCounter = 0;
        private bool _disposed = false;

        public Download(ListItem cache)
        {
            //_client = client;
            _client = new LiveConnectClient(App.Current.LiveSession);
            _cache = cache;
            _client.DownloadCompleted += ClientDownloadCompleted;
        }

        ~Download()
        {
            Dispose(false);
        }

        public void Start(IEnumerable<object> files)
        {
            foreach (IDictionary<string, object> content in files)
            {
                var filename = (string)content["name"];
                if (!filename.EndsWith(".txt"))
                {
                    continue;
                }
                string name = filename.Substring(0, filename.Length - 4);
                ListItem newGroup = _cache.FindItem(name);
                DateTime updateTime = Convert.ToDateTime(content["updated_time"]);
                if (newGroup != null)
                {
                    //проверим время модификации

                    if (newGroup.ModifyTime < updateTime)
                    {
                        _downloadCounter++;
                        _client.DownloadAsync(String.Format("{0}/content", (string)content["id"]), newGroup);
                    }                
                }
                else
                {
                    newGroup = new ListItem { Name = name, ModifyTime = updateTime };
                    _cache.Items.Add(newGroup);
                    _downloadCounter++;
                    _client.DownloadAsync(String.Format("{0}/content", (string)content["id"]), newGroup);
                }


            }

            _downloadCounter++;
            ClientDownloadCompleted(this,new LiveDownloadCompletedEventArgs(null,null));
        }

        void ClientDownloadCompleted(object sender, LiveDownloadCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                var parrentItem = (ListItem)e.UserState;
                //parrentItem.Items.Clear();
                var sr = new StreamReader(e.Result);
                string text = sr.ReadToEnd();
                e.Result.Close();
                //parrentItem.Items = new ObservableCollection<ListItem>();

                string[] itemsArray = text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                bool isMark = true;                

                var numItem = itemsArray.Length > 3 && CommonUtil.IsTrial() ? 3 : itemsArray.Length;

                for (int index = 0; index < numItem; index++)
                {
                    var task = itemsArray[index];
                    bool childMark = false;
                    if (task[0] == '-')
                    {
                        childMark = true;
                        task = task.Substring(1);
                    }
                    else
                    {
                        isMark = false;
                    }

                    var item = parrentItem.FindItem(task, false) ?? parrentItem.Add(task, true);
                    item.Mark = childMark;

                }
                parrentItem.Mark = parrentItem.Items.Count > 0 && isMark;
            }
            else if(e.Error!=null)
            {
                OnError(e.Error.Message);
            }

            _downloadCounter--;
            if (_downloadCounter <= 0)
            {
                //Save();
                OnDownloadComplited(EventArgs.Empty);
            }
        }

        public void OnDownloadComplited(EventArgs e)
        {
            EventHandler handler = DownloadComplited;
            if (handler != null) handler(this, e);
        }

        public void OnError(string message)
        {
            EventHandler<MessageEventArgs> handler = Error;
            if (handler != null) handler(this, new MessageEventArgs(message));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                if (disposing)
                {
                    _client.DownloadCompleted -= ClientDownloadCompleted;
                    DownloadComplited = null;
                }

                _disposed = true;

            }
        }
    }
}
