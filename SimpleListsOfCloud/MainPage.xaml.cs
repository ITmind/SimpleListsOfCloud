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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Live.Controls;
using Microsoft.Live;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Phone.Net.NetworkInformation;
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
        private bool clickSyncBtn = false;

        
        #endregion

        // Конструктор
        public MainPage()
        {

            DataContext = this;
            InitializeComponent();
            backgroundLogin.DoWork += BackgroundLoginDoWork;
            App.Current.SkyDriveFolders.FolderIDSearchComplited += skyDriveFolders_FolderIDSearchComplited;
            App.Current.SkyDriveFolders.GetFilesComplited += skyDriveFolders_GetFilesComplited;
            App.Current.SkyDriveFolders.Error += ListItemsError;
            App.Current.ListItems.GetDataComplited += ItemsListGetDataComplited;
            App.Current.ListItems.Error += ListItemsError;
            
            //tasklist_listbox.FillList(App.Current.ListItems.StartNode);
            //clickSyncBtn = false;

            //backgroundLogin.RunWorkerAsync();
            //syncAnim.Interval = new TimeSpan(0, 0, 0, 0, 100);
            //syncAnim.Tick += SyncAnimTick;

            //listBox.ItemsSource = new List<string>() { "1", "2" };
            //
        }

        void ListItemsError(object sender, MessageEventArgs e)
        {
            syncAnim.Stop();
            TitlePanel.Background = new SolidColorBrush(Colors.Red);
            txtCurFolder.Text = e.Message;
            txtLoginResult.Text = "Not signed in.";
            App.Current.LiveSession = null;
        }

        void SyncAnimTick(object sender, EventArgs e)
        {  
            curFrame++;
            if (curFrame == numFrames) curFrame = 0;

            var btn = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
            btn.IconUri = new Uri(String.Format("/Images/appbar.refresh.{0}.png", curFrame), UriKind.Relative);
        }

        void BackgroundLoginDoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("backgroundLogin_DoWork");
            this.Dispatcher.BeginInvoke(() => LoginToSkyDrive());
        }

        void ItemsListGetDataComplited(object sender, EventArgs e)
        {
            Debug.WriteLine("itemsList_GetDataComplited");

            //tasklist_listbox.FillList(App.Current.ListItems.StartNode);
            tasklist_listbox.RefillList();

            syncAnim.Stop();
            if (ApplicationBar != null && ApplicationBar.Buttons.Count > 0)
            {
                var btn = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
                btn.IconUri = new Uri("/Images/appbar.refresh.0.png", UriKind.Relative);
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            //TODO: добавить перехват кнопки при активной записи так, что бы она просто ставла не активной
            if (tasklist_listbox.CurrItem != App.Current.ListItems.StartNode)
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
                //if (e.Result.ContainsKey("first_name") &&
                //    e.Result.ContainsKey("last_name"))
                //{
                //    if (e.Result["first_name"] != null &&
                //        e.Result["last_name"] != null)
                //    {
                //        this.txtWelcome.Text =
                //            "Hello, " +
                //            e.Result["first_name"].ToString() + " " +
                //            e.Result["last_name"].ToString() + "!";
                //    }
                //}
                //else
                //{
                //    txtWelcome.Text = "Hello, signed-in user!";
                //}

                //IsAppFolderPresent();

                App.Current.SkyDriveFolders.GetFolderID();
            }
            else
            {
                txtCurFolder.Text = "Error calling API: " +
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
                syncAnim.Start();
                client = new LiveConnectClient(e.Session);
                App.Current.LiveSession = e.Session;
                TitlePanel.Background = new SolidColorBrush(Colors.Green);
                this.txtLoginResult.Text = "Signed in.";
                this.txtCurFolder.Visibility = Visibility.Visible;

                client.GetCompleted += new EventHandler<LiveOperationCompletedEventArgs>(OnGetCompleted);
                client.GetAsync("me", null);
            }
            else if (e.Status == LiveConnectSessionStatus.Unknown && loaded)
            {
                syncAnim.Stop();
                if (this.clickSyncBtn)
                {
                    clickSyncBtn = false;
                    LoginToSkyDrive(true);
                }                
            }
            else
            {
                syncAnim.Stop();
                this.txtLoginResult.Text = "Not signed in.";
                this.txtCurFolder.Visibility = Visibility.Collapsed;
                client = null;
            }

            //App.Current.LiveSession = e.Session;   

        }
        #endregion

        private void ApplicationBarIconButtonClick(object sender, EventArgs e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable() || NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.None)
            {
                //MessageBox.Show("No internet connection is available.  Try again later.");
                txtLoginResult.Text = "No network";
                TitlePanel.Background = new SolidColorBrush(Colors.Red);
                syncAnim.Stop();
                App.Current.LiveSession = null;
                return;
            }

            clickSyncBtn = true;
            syncAnim.Start();

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
            if (!NetworkInterface.GetIsNetworkAvailable() || NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.None)
            {
                //MessageBox.Show("No internet connection is available.  Try again later.");
                txtLoginResult.Text = "No network";
                TitlePanel.Background = new SolidColorBrush(Colors.Red);
                syncAnim.Stop();
                clickSyncBtn = false;
                return;
            }
        

            var auth = new LiveAuthClient("00000000480C5667");

            if (login)
            {
                syncAnim.Stop();
                auth.LoginCompleted += auth_LoginCompleted;
                auth.LoginAsync(new[] { "wl.offline_access","wl.signin","wl.skydrive_update" });//,
            }
            else
            {
                syncAnim.Start();
                auth.InitializeAsync(new[] { "wl.offline_access", "wl.signin", "wl.skydrive_update" });
                auth.InitializeCompleted += auth_LoginCompleted;
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.NavigationMode != NavigationMode.Back)
            {                
                backgroundLogin.RunWorkerAsync();
                syncAnim.Interval = new TimeSpan(0, 0, 0, 0, 100);
                syncAnim.Tick += SyncAnimTick;
            }

            base.OnNavigatedTo(e);
        }
        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            loaded = true;
            tasklist_listbox.ListRefill += TasklistListboxListRefill;
            tasklist_listbox.FillList(App.Current.ListItems.StartNode);
            clickSyncBtn = false;

            //backgroundLogin.RunWorkerAsync();
            //syncAnim.Interval = new TimeSpan(0, 0, 0, 0, 100);
            //syncAnim.Tick += SyncAnimTick;
        }

        void TasklistListboxListRefill(object sender, MessageEventArgs e)
        {
            txtCurFolder.Text = e.Message=="_StartNode"?"top":e.Message;
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
            //tasklist_listbox.RefillList();
            tasklist_listbox.FillList(tasklist_listbox.CurrItem);
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/HelpPage.xaml",UriKind.Relative));
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
        }
        
    }
}