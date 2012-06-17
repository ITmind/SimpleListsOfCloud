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
using Microsoft.Live;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleListsOfCloud
{
    public class SkyDriveFolders
    {
        private string folderID = "";
        public event EventHandler FolderIDSearchComplited;
        public event EventHandler GetFilesComplited;

        public List<object> files = null;

        public string FolderID
        {
            get
            {
                return folderID;
            }
            set
            {
                folderID = value;
                OnFolderIDSearchComplited(EventArgs.Empty);
            }
        }

        protected void OnFolderIDSearchComplited(EventArgs e)
        {
            EventHandler handler = FolderIDSearchComplited;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnGetFilesComplited(EventArgs e)
        {
            EventHandler handler = GetFilesComplited;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #region FolderID
        public void GetFolderID()
        {
            Debug.WriteLine("GetFolderID");
            LiveConnectClient client = new LiveConnectClient(App.Current.LiveSession);
            client.GetCompleted += new EventHandler<LiveOperationCompletedEventArgs>(clientDataFetch_GetCompleted);
            client.GetAsync("/me/skydrive/files");

        }

        void clientDataFetch_GetCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            Debug.WriteLine("clientDataFetch_GetCompleted");
            if (e.Error == null)
            {
                List<object> data = (List<object>)e.Result["data"];
                foreach (IDictionary<string, object> content in data)
                {
                    if ((string)content["name"] == "SimpleListsOfCloudData")
                    {
                        FolderID = (string)content["id"];
                        return;
                    }
                    //skyContent.Name = (string)content["name"];
                }

                CreateFolder("SimpleListsOfCloudData");
            }
        }

        void CreateFolder(string folderName)
        {
            Debug.WriteLine("CreateFolder");
            if (App.Current.LiveSession == null)
            {
                //infoTextBlock.Text = "You must sign in first.";
            }
            else
            {
                Dictionary<string, object> folderData = new Dictionary<string, object>();
                folderData.Add("name", folderName);
                LiveConnectClient client = new LiveConnectClient(App.Current.LiveSession);
                client.PostCompleted +=
                    new EventHandler<LiveOperationCompletedEventArgs>(CreateFolder_Completed);
                client.PostAsync("me/skydrive", folderData);
            }
        }

        void CreateFolder_Completed(object sender, LiveOperationCompletedEventArgs e)
        {
            Debug.WriteLine("CreateFolder_Completed");
            if (e.Error == null)
            {
                FolderID = (string)e.Result["id"];
                //infoTextBlock.Text = "Folder created.";
            }
            else
            {
                //infoTextBlock.Text = "Error calling API: " + e.Error.ToString();
            }
        }
        #endregion

        #region GetFiles
        public void GetFiles()
        {
            Debug.WriteLine("GetFiles");
            LiveConnectClient client = new LiveConnectClient(App.Current.LiveSession);
            client.GetCompleted += new EventHandler<LiveOperationCompletedEventArgs>(client_GetFilesCompleted);
            client.GetAsync(String.Format("{0}/files", FolderID));
        }

        void client_GetFilesCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            Debug.WriteLine("client_GetFilesCompleted");
            if (e.Error == null)
            {
                files = (List<object>)e.Result["data"];
                OnGetFilesComplited(EventArgs.Empty);
            }
        }

        #endregion

    }
}
