using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SimpleListsOfCloud.Utils;
using Microsoft.Phone.Controls;

namespace SimpleListsOfCloud
{
    public partial class ItemUI : UserControl
    {
        public bool DisableGesture;
        private bool _reorderStarted;
        private double _dragStartX;
        private bool _draggingToDelete;
        private bool _draggingToComplete;
        private ItemListUI listbox;
        bool _textFocused = false;

        private Color _markColor = Colors.Gray;//new HexColor("#E1E1E1");
        private Color _empetyColor = new HexColor("#00C1FF");//new HexColor("#F0FFF0");
        //private Color fillColor = new HexColor("#DCFFDC");

        public ItemUI()
        {
            InitializeComponent();
        }

        public ItemUI(ItemListUI listbox)
            : this()
        {
            this.listbox = listbox;
            Update();
        }

        public void Update()
        {
            if (Tag != null)
            {
                var count = ((ListItem)Tag).Items.Count(x => !x.Deleted);
                Color curColor;
                if (((ListItem)Tag).Mark)
                {                    
                    if (count < 10)
                    {
                        curColor = new Color
                        {
                            B = (byte)(_markColor.B - count * 10),
                            R = (byte)(_markColor.R - count * 10),
                            G = (byte)(_markColor.G - count * 10),
                            A = _markColor.A
                        };
                    }
                    else
                    {
                        curColor = new Color
                        {
                            B = (byte)(_markColor.B - 100),
                            R = (byte)(_markColor.R - 100),
                            G = (byte)(_markColor.G - 100),
                            A = _markColor.A
                        };
                    }
                    markComplite.Visibility = Visibility.Visible;
                    
                }
                else
                {               
                    if (count < 15)
                    {
                        curColor = new Color
                                           {
                                               B = (byte)(_empetyColor.B - count * 0),
                                               R = (byte)(_empetyColor.R - count * 0),
                                               G = (byte)(_empetyColor.G - count * 10),
                                               A = _empetyColor.A
                                           };
                    }
                    else
                    {
                        curColor = new Color
                        {
                            B = (byte)(_empetyColor.B - count * 0),
                            R = (byte)(_empetyColor.R - count * 0),
                            G = (byte)(_empetyColor.G - 15 * 10),
                            A = _empetyColor.A
                        };
                    }
                    markComplite.Visibility = Visibility.Collapsed;
                }
                itemBorder.Background = new SolidColorBrush(curColor);
                
            }
        }

        public void SetText(string str)
        {
            text.Text = str;
            //if (this.Tag != null)
            //{
            //    if(listbox.currItem.FindItem(str)!=null)
            //    {
            //        //уже есть

            //    }
            //    //else
            //    //{
            //    //    ((ListItem)this.Tag).Name = str;    
            //    //}

            //}
            Dispatcher.BeginInvoke(() => OnTextChanged(null, null));
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            //if (this.Tag != null)
            //{
            //    ((ListItem)this.Tag).Name = text.Text;
            //}
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
                            listbox.onUncompleteItem(this);
                        else
                            listbox.onCompleteItem(this);
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
                itemBorder.Width = 440;

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
            if (this.DisableGesture)
                return;
            bool flag = e.HorizontalChange < 0.0 && e.Direction == Orientation.Horizontal;
            bool flag2 = e.HorizontalChange > 0.0 && e.Direction == Orientation.Horizontal;

            if (this._reorderStarted)
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
            AnimationUtil.zoom((FrameworkElement)this, 70.0, 100, (Action<object, EventArgs>)null);
            Canvas.SetZIndex((UIElement)this, 32766);
        }

        public void OnReorderCompleted()
        {
            this._reorderStarted = false;
            AnimationUtil.zoom((FrameworkElement)this, 0.0, 100, (Action<object, EventArgs>)null);
        }

        private void onKeyUp(object sender, KeyEventArgs e)
        {
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
    }
}
