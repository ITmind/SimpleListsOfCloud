using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Live.Controls;
using Microsoft.Live;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Phone.Shell;

namespace SimpleListsOfCloud
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region "Members"

        private LiveConnectClient client;
        private bool loaded = false;
        
        BackgroundWorker backgroundLogin = new BackgroundWorker();

        //anim
        DispatcherTimer syncAnim = new DispatcherTimer();
        int numFrames = 6;
        int curFrame = 0;

        
        #endregion

        // Конструктор
        public MainPage()
        {

            DataContext = this;
            InitializeComponent();
            backgroundLogin.DoWork += new DoWorkEventHandler(backgroundLogin_DoWork);
            App.Current.SkyDriveFolders.FolderIDSearchComplited += new EventHandler(skyDriveFolders_FolderIDSearchComplited);
            App.Current.SkyDriveFolders.GetFilesComplited += new EventHandler(skyDriveFolders_GetFilesComplited);
            App.Current.ListItems.GetDataComplited += new EventHandler(itemsList_GetDataComplited);
            //lbItems.ItemsSource = App.Current.ListItems.StartNode.ViewItems;
            //App.Current.ListItems.StartNode.Sort();
            tasklist_listbox.FillList(App.Current.ListItems.StartNode);

            backgroundLogin.RunWorkerAsync();
            syncAnim.Interval = new TimeSpan(0, 0, 0, 0, 100);
            syncAnim.Tick += new EventHandler(syncAnim_Tick);

            //listBox.ItemsSource = new List<string>() { "1", "2" };
            //
        }

        void syncAnim_Tick(object sender, EventArgs e)
        {  
            curFrame++;
            if (curFrame == numFrames) curFrame = 0;

            ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
            btn.IconUri = new Uri(String.Format("/Images/appbar.refresh.{0}.png", curFrame), UriKind.Relative);
        }

        void backgroundLogin_DoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("backgroundLogin_DoWork");
            this.Dispatcher.BeginInvoke(() =>
                {                    
                    LoginToSkyDrive();
                });
        }

        void itemsList_GetDataComplited(object sender, EventArgs e)
        {
            Debug.WriteLine("itemsList_GetDataComplited");

            //tasklist_listbox.FillList(App.Current.ListItems.StartNode);
            tasklist_listbox.RefillList();

            syncAnim.Stop();
            if (ApplicationBar != null && ApplicationBar.Buttons.Count > 0)
            {
                ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
                btn.IconUri = new Uri("/Images/appbar.refresh.0.png", UriKind.Relative);
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (tasklist_listbox.currItem != App.Current.ListItems.StartNode)
            {
                e.Cancel = true;
                tasklist_listbox.FillList(App.Current.ListItems.StartNode);
            }
            else
            {
                base.OnBackKeyPress(e);
            }
            
        }

        #region "Event Handlers"        

        void OnGetCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            Debug.WriteLine("OnGetCompleted");
            if (e.Error == null)
            {
                if (e.Result.ContainsKey("first_name") &&
                    e.Result.ContainsKey("last_name"))
                {
                    if (e.Result["first_name"] != null &&
                        e.Result["last_name"] != null)
                    {
                        this.txtWelcome.Text =
                            "Hello, " +
                            e.Result["first_name"].ToString() + " " +
                            e.Result["last_name"].ToString() + "!";
                    }
                }
                else
                {
                    txtWelcome.Text = "Hello, signed-in user!";
                }

                //IsAppFolderPresent();

                App.Current.SkyDriveFolders.GetFolderID();
            }
            else
            {
                txtWelcome.Text = "Error calling API: " +
                    e.Error.ToString();
            }
        }

        void skyDriveFolders_FolderIDSearchComplited(object sender, EventArgs e)
        {
            Debug.WriteLine("skyDriveFolders_FolderIDSearchComplited");
            string temp = App.Current.SkyDriveFolders.FolderID;
            App.Current.SkyDriveFolders.GetFiles();
            //throw new NotImplementedException();
        }

        void skyDriveFolders_GetFilesComplited(object sender, EventArgs e)
        {
            Debug.WriteLine("skyDriveFolders_GetFilesComplited");
            //start sync anim       
            App.Current.ListItems.Sync(App.Current.SkyDriveFolders.files);
        }

        void auth_LoginCompleted(object sender, LoginCompletedEventArgs e)
        {

            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                client = new LiveConnectClient(e.Session);
                App.Current.LiveSession = e.Session;
                this.txtLoginResult.Text = "Signed in.";
                this.txtWelcome.Visibility = System.Windows.Visibility.Visible;

                client.GetCompleted += new EventHandler<LiveOperationCompletedEventArgs>(OnGetCompleted);
                client.GetAsync("me", null);
            }
            else if (e.Status == LiveConnectSessionStatus.Unknown && loaded)
            {
                syncAnim.Stop();
                LoginToSkyDrive(true);
            }
            else
            {
                syncAnim.Stop();
                this.txtLoginResult.Text = "Not signed in.";
                this.txtWelcome.Visibility = System.Windows.Visibility.Collapsed;
                client = null;
            }

            //App.Current.LiveSession = e.Session;   

        }
        #endregion

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            syncAnim.Start();
            App.Current.ListItems.DownloadComplite = false;
            App.Current.ListItems.UploadComplite = false;
            //если еще идет процесс инициализации то отмена
            if (backgroundLogin.IsBusy) return;

            if (App.Current.LiveSession == null)
            {
                LoginToSkyDrive();
            }
            else
            {
                //App.Current.ListItems.Sync(skyDriveFolders.files);
                App.Current.SkyDriveFolders.GetFiles();
            }
            
        }

        void LoginToSkyDrive(bool login = false)
        {
            App.Current.ListItems.DownloadComplite = false;
            App.Current.ListItems.UploadComplite = false;

            LiveAuthClient auth = new LiveAuthClient("00000000480C5667");

            if (login)
            {
                syncAnim.Stop();
                auth.LoginCompleted += new EventHandler<LoginCompletedEventArgs>(auth_LoginCompleted);
                auth.LoginAsync(new string[] { "wl.signin", "wl.basic", "wl.skydrive", "wl.skydrive_update", "wl.offline_access" });
            }
            else
            {
                syncAnim.Start();
                auth.InitializeAsync(new string[] { "wl.signin", "wl.basic", "wl.skydrive", "wl.skydrive_update", "wl.offline_access" });
                auth.InitializeCompleted += new EventHandler<LoginCompletedEventArgs>(auth_LoginCompleted);
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            loaded = true;
        }

        /// <summary>
        /// button back
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplicationBarIconButton_Click_1(object sender, EventArgs e)
        {
            tasklist_listbox.FillList(App.Current.ListItems.StartNode);
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            App.Current.ListItems.StartNode.Sort();
            tasklist_listbox.RefillList();
        }
        
    }
}