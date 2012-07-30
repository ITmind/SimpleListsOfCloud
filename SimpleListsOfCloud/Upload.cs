using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string messageData)
        {
            Message = messageData;
        }

        public string Message { get; set; }
    }

    public class Upload: IDisposable
    {
        public event EventHandler UploadComplited;
        public event EventHandler<MessageEventArgs> Error;

        private readonly LiveConnectClient _client;
        private readonly ListItem _cache;
        private int _uploadCounter = 0;
        private int _deleteCounter = 0;
        readonly List<string> _filesForDelete = new List<string>();
        private bool _disposed = false;

        public Upload(ListItem cache)
        {
            //_client = client;
            _client = new LiveConnectClient(App.Current.LiveSession);
            _cache = cache;
            _client.UploadCompleted += ClientUploadCompleted;
            _client.DeleteCompleted += ClientDeleteCompleted;
        }

        ~Upload()
        {
            Dispose(false);
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
            if (e !=null && e.Error != null)
            {
                Debug.WriteLine("Error");
                OnError(e.Error.Message);
                return;
            }

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

        public void Start(IEnumerable<object> files)
        {
            List<object> deletingItems = new List<object>(5);

            foreach (IDictionary<string, object> content in files)
            {
                var filename = (string)content["name"];
                if (!filename.EndsWith(".txt"))
                {
                    continue;
                }
                string name = filename.Substring(0, filename.Length - 4);
                var findedGroup = _cache.FindItem(name);
                //var updateTime = Convert.ToDateTime(content["updated_time"]);
                if (findedGroup != null && !findedGroup.Deleted)
                {
                    //проверим время модификации
                    //if (findedGroup.ModifyTime > updateTime)
                    //{
                        //делаем загрузку в облако                        
                    //    UploadItems(findedGroup);
                    //}
                }
                else if (findedGroup != null && findedGroup.Deleted)
                {
                    _filesForDelete.Add((string)content["id"]);  
                    deletingItems.Add(content);
                }
            }

            foreach (var deletingItem in deletingItems)
            {
                ((List<object>) files).Remove(deletingItem);
            }

            foreach (var item in _cache.Items)
            {
                if (!item.Deleted && item.ModifyTime > item.LastSyncTime)
                {
                    UploadItems(item);
                }
            }

            _uploadCounter++;
            ClientUploadCompleted(null, null);

        }

        private void DeleteSync()
        {
            foreach (var file in _filesForDelete)
            {
                _deleteCounter++;
                _client.DeleteAsync(file);
            }

            _deleteCounter++;
            ClientDeleteCompleted(null, null);
        }

        #region EventHandler        
        public void OnUploadComplited(EventArgs e)
        {
            EventHandler handler = UploadComplited;
            if (handler != null) handler(this, e);
        }

        public void OnError(string message)
        {
            EventHandler<MessageEventArgs> handler = Error;
            if (handler != null) handler(this, new MessageEventArgs(message));
        }

        #endregion

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
                    _client.UploadCompleted -= ClientUploadCompleted;
                    _client.DeleteCompleted -= ClientDeleteCompleted;
                    UploadComplited = null;
                    _filesForDelete.Clear();
                }

                _disposed = true;

            }
        }
    }
}
