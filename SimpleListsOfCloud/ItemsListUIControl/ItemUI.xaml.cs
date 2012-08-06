using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SimpleListsOfCloud.Utils;
using Microsoft.Phone.Controls;

namespace SimpleListsOfCloud
{
    public partial class ItemUI : INotifyPropertyChanged
    {
        public bool DisableGesture;
        private bool _reorderStarted;
        private double _dragStartX;
        private bool _draggingToDelete;
        private bool _draggingToComplete;
        private ItemListUI listbox;
        bool _textFocused = false;

        private Color _markColor = Colors.Gray;//new HexColor("#E1E1E1");
        private Color _empetyColor = (Color)Application.Current.Resources["PhoneAccentColor"];//new HexColor("#00C1FF");//new HexColor("#F0FFF0");
        //private Color fillColor = new HexColor("#DCFFDC");

        public ItemUI()
        {
            DataContext = this;
            InitializeComponent();
            //_empetyColor = (Color) Application.Current.Resources["PhoneForegroundColor"];
        }

        public ItemUI(ItemListUI listbox)
            : this()
        {
            this.listbox = listbox;
            Update();
        }

        public string NumItems
        {
            get
            {
                string result = "0";
                if (Tag != null)
                {
                    if (App.Current.Settings.ShowNumTask == ShowNumTaskEnum.All)
                    {
                        result = ((ListItem) Tag).Items.Count(x => !x.Deleted).ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        result = ((ListItem)Tag).Items.Count(x => !x.Deleted && !x.Mark).ToString(CultureInfo.InvariantCulture);
                    }
                }                
                return result;
            }
        }

        public Visibility VisiblityNumTask
        {
            get
            {
                var result = Visibility.Collapsed;
                if (Tag != null && ((ListItem)Tag).Items.Count>0)
                {
                    if (App.Current.Settings.ShowNumTask == ShowNumTaskEnum.None) { 
                        result = Visibility.Collapsed;
                    }
                    else
                    {
                        result = Visibility.Visible;
                    }
                }
                return result;
            }
        }

        public Visibility VisiblityAlarm
        {
            get
            {
                var result = Visibility.Collapsed;
                if (Tag != null)
                {
                    var reminder = ((ListItem) Tag).Reminder;
                    if (Tag != null && reminder != null)
                    {
                        if (reminder.IsScheduled)
                        {
                            result = Visibility.Visible;
                        }
                    }
                }
                return result;
            }
        }

        public void Update()
        {
            if (Tag != null)
            {
                var count = ((ListItem)Tag).Items.Count(x => !x.Deleted);
                Color curColor;
                if (((ListItem)Tag).Mark)
                {
                    count = count < 10 ? count : 10;

                    curColor = new Color
                                   {
                                       B = (byte) (_markColor.B - count*10),
                                       R = (byte) (_markColor.R - count*10),
                                       G = (byte) (_markColor.G - count*10),
                                       A = _markColor.A
                                   };

                    markComplite.Visibility = Visibility.Visible;

                }
                else
                {

                    count = count < 8 ? count : 8;

                    curColor = new Color();
                    if (_empetyColor.B - count * 10 >= 0)
                    {
                        curColor.B = (byte)(_empetyColor.B - count * 10);
                    }
                    if (_empetyColor.R - count * 10 >= 0)
                    {
                        curColor.R = (byte)(_empetyColor.R - count * 10);
                    }
                    if (_empetyColor.G - count * 10 >= 0)
                    {
                        curColor.G = (byte)(_empetyColor.G - count * 10);
                    }
                    curColor.A = _empetyColor.A;


                    markComplite.Visibility = Visibility.Collapsed;
                }
                itemBorder.Background = new SolidColorBrush(curColor);
                
            }
        }

        public void SetText(string str)
        {
            text.Text = str;
            Dispatcher.BeginInvoke(() => OnTextChanged(null, null));
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            text.UpdateLayout();
        }

        private void onHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (DisableGesture || _reorderStarted)
                return;
            e.Handled = true;
            listbox.onReorderStarted(this);
        }

        private void onDragStarted(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            if (DisableGesture)
                return;
            if (_reorderStarted)
            {
                e.Handled = true;
            }
            else
            {
                if (_draggingToDelete)
                    return;
                _dragStartX = e.GetPosition(this).X;
            }
        }

        private void onManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            OnDragCompleted((object)this.LayoutRoot, (Microsoft.Phone.Controls.GestureEventArgs)null);
        }

        private void OnDragCompleted(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            if (DisableGesture)
                return;
            if (_reorderStarted)
            {
                if (e != null)
                    e.Handled = true;
                listbox.onReorderCompleted(this);
            }
            else if (_draggingToComplete)
            {
                if (e != null)
                    e.Handled = true;
                _draggingToComplete = false;
                //listbox.onDraggingToCompleteEnded();
                listbox.onDraggingToDeleteEnded();
                double translateX = TransformUtil.getTranslateX(LayoutRoot);
                if (translateX < 150.0)
                {
                    AnimationUtil.translateX((FrameworkElement)LayoutRoot, translateX, 0.0, 100, (Action<object, EventArgs>)null);
                }
                else
                {
                    //setCompletingLook();
                    AnimationUtil.translateX((FrameworkElement)LayoutRoot, translateX, 0.0, 100, (Action<object, EventArgs>)((s, ev) =>
                    {
                        if ((Tag as ListItem).Mark)
                            listbox.OnUncompleteItem(this);
                        else
                            listbox.OnCompleteItem(this);
                    }));
                }
            }
            else
            {
                if (!this._draggingToDelete)
                    return;
                if (e != null)
                    e.Handled = true;
                this._draggingToDelete = false;
                DeleteImage.Visibility = System.Windows.Visibility.Collapsed;
                itemBorder.Width = 450;

                listbox.onDraggingToDeleteEnded();
                double translateX = TransformUtil.getTranslateX((FrameworkElement)LayoutRoot);
                if (translateX > -150.0 || MessageBox.Show("Delete list", "Are you sure?", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    AnimationUtil.translateX((FrameworkElement)LayoutRoot, translateX, 0.0, 100, (Action<object, EventArgs>)null);
                }
                else
                {
                    AnimationUtil.translateX((FrameworkElement)LayoutRoot, translateX, -this.ActualWidth, 100, (Action<object, EventArgs>)((s, ev) => listbox.onDeleteItem(this)));
                }
            }
        }

        private void onDragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            if (DisableGesture)
                return;
            bool flag = e.HorizontalChange < 0.0 && e.Direction == Orientation.Horizontal;
            bool flag2 = e.HorizontalChange > 0.0 && e.Direction == Orientation.Horizontal;

            if (_reorderStarted)
            {
                e.Handled = true;
                listbox.onReorderDelta(this, e.VerticalChange);
            }
            else if (!_draggingToDelete && flag && !_draggingToComplete)
            {
                e.Handled = true;
                _draggingToDelete = true;
                listbox.onDraggingToDelete();
                TransformUtil.addTranslateDelta(LayoutRoot, e.HorizontalChange, 0.0);
                DeleteImage.Visibility = System.Windows.Visibility.Visible;
                itemBorder.Width = 440 - DeleteImage.ActualWidth;
            }
            else if (!_draggingToDelete && flag2 && !_draggingToComplete)
            {
                e.Handled = true;
                _draggingToComplete = true;
                //listbox.onDraggingToComplete(this);
                listbox.onDraggingToDelete();
                TransformUtil.addTranslateDelta((FrameworkElement)LayoutRoot, e.HorizontalChange, 0.0);
                //this.completeTxtPanel.Visibility = Visibility.Visible;
                //this.completeTxt.Text = this.isCompleted ? "Uncomplete" : "Complete";
            }
            else
            {
                if (!_draggingToDelete && !_draggingToComplete)
                    return;
                e.Handled = true;
                TransformUtil.addTranslateDelta((FrameworkElement)LayoutRoot, e.HorizontalChange, 0.0);
            }
        }

        public void OnReorderStarted()
        {
            _reorderStarted = true;
            AnimationUtil.zoom(this, 70.0, 100, null);
            Canvas.SetZIndex(this, 32766);
        }

        public void OnReorderCompleted()
        {
            _reorderStarted = false;
            AnimationUtil.zoom(this, 0.0, 100, null);
        }

        private void onKeyUp(object sender, KeyEventArgs e)
        {
            //Debug.WriteLine(e.Key);
            if (e.Key != Key.Enter)
                return;

            var findedItem = listbox.CurrItem.FindItem(text.Text);
            if (findedItem != null && !findedItem.Deleted)
            {
                MessageBox.Show(String.Format("Item {0} is alredy present in list. Select another name.", text.Text));
            }
            else
            {
                listbox.Focus();
            }

        }

        private void BorderTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!_textFocused)
            {
                //Debug.WriteLine("go");
                //if (Tag != null && (Tag as ListItem).Items.Count > 0)
                var parrent = ListItem.FindParrent(App.Current.ListItems.StartNode, Tag as ListItem);
                if (Tag != null && parrent == App.Current.ListItems.StartNode)
                {
                    listbox.FillList((Tag as ListItem));
                }
            }
        }


        private void text_GotFocus(object sender, RoutedEventArgs e)
        {
            _textFocused = true;
            text.SelectionStart = text.Text.Length;
        }

        private void text_LostFocus(object sender, RoutedEventArgs e)
        {
            _textFocused = false;
            if (text.Text == "")
            {
                listbox.onDeleteItem(this);
            }
            else
            {
                if (Tag != null && ((ListItem)Tag).Name != text.Text)
                {
                    var findedItem = listbox.CurrItem.FindItem(text.Text);
                    if (findedItem != null && !findedItem.Deleted)
                    {
                        MessageBox.Show(String.Format("Item {0} is alredy present in list. Select another name.", text.Text));
                        text.Focus();
                    }
                    else if (findedItem != null && findedItem.Deleted)
                    {
                        listbox.CurrItem.Delete((ListItem) Tag);
                        findedItem.Deleted = false;
                        findedItem.Items.Clear();
                        findedItem.ModifyTime = DateTime.Now;
                        Tag = findedItem;
                    }
                    else
                    {
                        ((ListItem)Tag).Name = text.Text;
                    }
                }
            }
        }

        private void TextTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            _textFocused = true;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            listbox.UpdateItemGrid();
        }

        private void tapZoneForEditText_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            _textFocused = text.Focus();
            
        }

        private void tapZoneForEditText_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {

            listbox.Focus();
            ((PhoneApplicationFrame)Application.Current.RootVisual).Navigate(new Uri(String.Format("/AddEvent.xaml?name={0}&parent={1}", text.Text, listbox.CurrItem.Name), UriKind.Relative));
            //MessageBox.Show("Double tap");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AlarmButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ((PhoneApplicationFrame)Application.Current.RootVisual).Navigate(new Uri(String.Format("/AddEvent.xaml?name={0}&parent={1}", text.Text, listbox.CurrItem.Name), UriKind.Relative));
        }
    }
}
