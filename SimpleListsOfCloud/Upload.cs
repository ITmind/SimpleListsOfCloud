using System;
using System.Collections.Generic;
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
    public class Upload
    {
        public event EventHandler UploadComplited;
        //public event EventHandler DeleteComplited;

        private readonly LiveConnectClient _client;
        private readonly ListItem _cache;
        private int _uploadCounter = 0;
        private int _deleteCounter = 0;
        readonly List<string> _filesForDelete = new List<string>();

        public Upload(LiveConnectClient client, ListItem cache)
        {
            _client = client;
            _cache = cache;
            client.UploadCompleted += ClientUploadCompleted;
            client.DeleteCompleted += ClientDeleteCompleted;
        }

        void ClientDeleteCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            _deleteCounter--;
            if (_deleteCounter <= 0)
            {
                OnUploadComplited(EventArgs.Empty);
            }
        }

        void ClientUploadCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            _uploadCounter--;
            if (_uploadCounter <= 0)
            {
                DeleteSync();
                //OnUploadComplited(EventArgs.Empty);
            }  
        }

        private void UploadItems(ListItem item)
        {
            _uploadCounter++;
            var newFile = new MemoryStream();
            var writer = new StreamWriter(newFile);
            foreach (var listItem in item.Items)
            {
                if (listItem.Deleted) continue;
                writer.WriteLine("{0}{1}", listItem.Mark ? "-" : "", listItem.Name);
            }
            writer.Flush();
            newFile.Flush();
            newFile.Seek(0, SeekOrigin.Begin);
            //writer.Close();
            _client.UploadAsync(App.Current.SkyDriveFolders.FolderID, item.Name + ".txt", true, newFile, item);
        }

        public void UploadSync(IEnumerable<object> files)
        {
            
            foreach (IDictionary<string, object> content in files)
            {
                var filename = (string)content["name"];
                if (!filename.EndsWith(".txt"))
                {
                    continue;
                }
                string name = filename.Substring(0, filename.Length - 4);
                var findedGroup = _cache.FindItem(name);
                var updateTime = Convert.ToDateTime(content["updated_time"]);
                if (findedGroup != null && !findedGroup.Deleted)
                {
                    //проверим время модификации
                    if (findedGroup.ModifyTime > updateTime)
                    {
                        //делаем загрузку в облако
                        _uploadCounter++;
                        UploadItems(findedGroup);
                    }
                }
                else if (findedGroup != null && findedGroup.Deleted)
                {
                    _filesForDelete.Add((string)content["id"]);  
                }
            }

            foreach (var item in _cache.Items)
            {
                if (!item.Deleted)
                {
                    UploadItems(item);
                }
            }
  
        }

        private void DeleteSync()
        {
            foreach (var file in _filesForDelete)
            {
                _deleteCounter++;
                _client.DeleteAsync(file);
            }
        }

        #region EventHandler        
        public void OnUploadComplited(EventArgs e)
        {
            EventHandler handler = UploadComplited;
            if (handler != null) handler(this, e);
        }

        //public void OnDeleteComplited(EventArgs e)
        //{
        //    EventHandler handler = DeleteComplited;
        //    if (handler != null) handler(this, e);
        //}
        #endregion
    }
}
