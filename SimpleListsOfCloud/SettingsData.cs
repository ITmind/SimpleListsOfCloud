using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SimpleListsOfCloud
{
    public enum ShowNumTaskEnum
    {
        None,
        NotMark,
        All
    }

    public class SettingsData: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public ShowNumTaskEnum ShowNumTask { get; set; }
       
        public SettingsData()
        {
            ShowNumTask = ShowNumTaskEnum.NotMark;
        }

        public void Load()
        {
        }

        public void Save()
        {

        }
    }
}
