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

namespace SimpleListsOfCloud
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        private bool PageLoaded;
        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            switch (App.Current.Settings.ShowNumTask)
            {
                case ShowNumTaskEnum.None:
                    lpShowNumTask.SelectedIndex = 0;
                    break;
                case ShowNumTaskEnum.NotMark:
                    lpShowNumTask.SelectedIndex = 1;
                    break;
                case ShowNumTaskEnum.All:
                    lpShowNumTask.SelectedIndex = 2;
                    break;
                default:
                    lpShowNumTask.SelectedIndex = 0;
                    break;
            }

            
            PageLoaded = true;

        }

        private void ShowNumTask_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!PageLoaded) return;
            foreach (var item in e.AddedItems)
            {
                if ((string) item == "None")
                {
                    App.Current.Settings.ShowNumTask = ShowNumTaskEnum.None;
                }
                else if ((string) item == "All")
                {
                    App.Current.Settings.ShowNumTask = ShowNumTaskEnum.All;
                }
                else
                {
                    App.Current.Settings.ShowNumTask = ShowNumTaskEnum.NotMark;
                }
                break;
            }
        }
    }
}