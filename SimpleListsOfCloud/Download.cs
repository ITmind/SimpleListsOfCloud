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

namespace SimpleListsOfCloud
{
    public class Download
    {
        public event EventHandler DownloadComplited;
        private readonly LiveConnectClient _client;
        private readonly ListItem _cache;
        private int _downloadCounter = 0;

        public Download(LiveConnectClient client, ListItem cache)
        {
            _client = client;
            _cache = cache;
            client.DownloadCompleted += ClientDownloadCompleted;
        }

        public void DownloadSync(IEnumerable<object> files)
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
        }

        void ClientDownloadCompleted(object sender, LiveDownloadCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                var parrentItem = (ListItem)e.UserState;
                parrentItem.Items.Clear();
                var sr = new StreamReader(e.Result);
                string text = sr.ReadToEnd();
                e.Result.Close();
                parrentItem.Items = new ObservableCollection<ListItem>();

                string[] itemsArray = text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                bool isMark = true;
                foreach (var task in itemsArray)
                {
                    //if (task[0] == '-')
                    var item = parrentItem.Add(task, true);
                    if (item.Name[0] == '-')
                    {
                        item.Mark = true;
                        item.Name = task.Substring(1);
                    }
                    else
                    {
                        isMark = false;
                    }

                }
                parrentItem.Mark = parrentItem.Items.Count > 0 && isMark;
            }
            else
            {
                //infoTextBlock.Text = "Error downloading image: " + e.Error.ToString();
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
    }
}
