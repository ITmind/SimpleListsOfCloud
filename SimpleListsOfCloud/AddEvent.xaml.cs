using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using Microsoft.Live;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;

namespace SimpleListsOfCloud
{
    public partial class AddEvent : INotifyPropertyChanged
    {
        public string NameEvent {get; set;}
        private ListItem _item;
        public DateTime StartDate { get; set; }
        public DateTime StartTime { get; set; }
        private ScheduledAction _reminder;

        private string _description;

        public AddEvent()
        {
            DataContext = this;
            StartDate = DateTime.Now;
            StartTime = DateTime.Now;
            _description = "";
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if(e.NavigationMode == NavigationMode.Back) return;

            if (NavigationContext.QueryString.ContainsKey("name"))
            {
                NameEvent = NavigationContext.QueryString["name"];
                OnPropertyChanged("NameEvent");
            }

            if (NavigationContext.QueryString.ContainsKey("parent"))
            {
                var parent = NavigationContext.QueryString["parent"];
                var parentItem = App.Current.ListItems.StartNode;
                if (!parent.Equals("_StartNode"))
                {
                    parentItem = parentItem.FindItem(parent);
                }

                if (parentItem != null)
                {
                    _item = parentItem.FindItem(NameEvent);
                }
            }

            if (_item == null)
            {
                NavigationService.GoBack();
            }
            else
            {
                FillEventByItem();
                base.OnNavigatedTo(e);    
            }
            
        }

        private void FillEventByItem()
        {
            _reminder = _item.Reminder;
            if(_reminder!=null)
            {
                StartDate = _reminder.BeginTime;
                StartTime = _reminder.BeginTime;
            }
        }

        private void CreateEvent()
        {
            var start = new DateTime(StartDate.Year, StartDate.Month, StartDate.Day, StartTime.Hour, StartTime.Minute, 0,DateTimeKind.Local);
            if (_reminder!=null)
            {
                if (_reminder.BeginTime != start)
                {
                    ScheduledActionService.Remove(_item.ReminderName);
                }
                else
                {
                    return;
                }
            }
            else
            {
                _item.ReminderName = FindFreeTask();    
            }
            
            if (string.IsNullOrEmpty(_item.ReminderName))
            {
                MessageBox.Show("More than 100 reminders established. Adding a new abolished.");
            }
            else if (start>DateTime.Now)
            {
                var r = new Reminder(_item.ReminderName)
                {
                    Content = _description,
                    BeginTime = start,
                    Title = NameEvent
                };
                ScheduledActionService.Add(r); 
            }
            else
            {
                MessageBox.Show("Wrong start time");
            }

            App.Current.ListItems.Save();
        }

        string FindFreeTask()
        {
            for (int i = 0; i < 100; i++)
            {
                var taskName = String.Format("Task{0}", i);
                if (ScheduledActionService.Find(taskName) == null) return taskName;                    
            }
            return String.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            //OK
            CreateEvent();
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void ApplicationBarIconButton_Click_1(object sender, EventArgs e)
        {
            //Cancel
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}