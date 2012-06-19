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
using SimpleListsOfCloud.Utils;
using Microsoft.Phone.Controls;
using System.Diagnostics;

namespace SimpleListsOfCloud
{
    public partial class ItemUI : UserControl
    {
        public bool disableGesture;
        private bool reorderStarted;
        private double dragStartX;
        private bool draggingToDelete;
        private bool draggingToComplete;
        private ItemListUI listbox;
        bool textFocused = false;

        private Color _markColor = new HexColor("#E1E1E1");
        private Color _empetyColor = new HexColor("#F0FFF0");
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
            if (this.Tag != null)
            {
                if (((ListItem) Tag).Mark)
                {
                    var count = ((ListItem)Tag).Items.Count;
                    var curColor = new Color
                    {
                        B = (byte)(_markColor.B - count * 10),
                        R = (byte)(_markColor.R - count * 10),
                        G = (byte)(_markColor.G - count * 10),
                        A = _markColor.A
                    };

                    itemBorder.Background = new SolidColorBrush(curColor);
                    markComplite.Visibility = Visibility.Visible;  
                }
                else
                {
                    var count = ((ListItem)Tag).Items.Count;
                    var curColor = new Color
                                       {
                                           B = (byte) (_empetyColor.B - count*30),
                                           R = (byte) (_empetyColor.R - count*30),
                                           G = _empetyColor.G,
                                           A = _empetyColor.A
                                       };
                    itemBorder.Background = new SolidColorBrush(curColor);
                    markComplite.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void setText(string str)
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
            this.Dispatcher.BeginInvoke((Action)(() => onTextChanged((object)null, (TextChangedEventArgs)null)));
        }

        private void onTextChanged(object sender, TextChangedEventArgs e)
        {
            //if (this.Tag != null)
            //{
            //    ((ListItem)this.Tag).Name = text.Text;
            //}
            text.UpdateLayout();
        }

        private void onHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (this.disableGesture || this.reorderStarted)
                return;
            e.Handled = true;
            this.listbox.onReorderStarted(this);
        }

        private void onDragStarted(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            if (this.disableGesture)
                return;
            if (this.reorderStarted)
            {
                e.Handled = true;
            }
            else
            {
                if (draggingToDelete)
                    return;
                this.dragStartX = e.GetPosition((UIElement)this).X;
            }
        }

        private void onManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            this.onDragCompleted((object)this.LayoutRoot, (Microsoft.Phone.Controls.GestureEventArgs)null);
        }

        private void onDragCompleted(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            if (this.disableGesture)
                return;
            if (this.reorderStarted)
            {
                if (e != null)
                    e.Handled = true;
                this.listbox.onReorderCompleted(this);
            }
            else if (draggingToComplete)
            {
                if (e != null)
                    e.Handled = true;
                draggingToComplete = false;
                //listbox.onDraggingToCompleteEnded();
                listbox.onDraggingToDeleteEnded();
                double translateX = TransformUtil.getTranslateX((FrameworkElement)LayoutRoot);
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
                if (!this.draggingToDelete)
                    return;
                if (e != null)
                    e.Handled = true;
                this.draggingToDelete = false;
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
            if (this.disableGesture)
                return;
            bool flag = e.HorizontalChange < 0.0 && e.Direction == Orientation.Horizontal;
            bool flag2 = e.HorizontalChange > 0.0 && e.Direction == Orientation.Horizontal;

            if (this.reorderStarted)
            {
                e.Handled = true;
                listbox.onReorderDelta(this, e.VerticalChange);
            }
            else if (!draggingToDelete && flag && !draggingToComplete)
            {
                e.Handled = true;
                draggingToDelete = true;
                listbox.onDraggingToDelete();
                TransformUtil.addTranslateDelta((FrameworkElement)LayoutRoot, e.HorizontalChange, 0.0);
                DeleteImage.Visibility = System.Windows.Visibility.Visible;
                itemBorder.Width = 440 - DeleteImage.ActualWidth;
            }
            else if (!draggingToDelete && flag2 && !draggingToComplete)
            {
                e.Handled = true;
                draggingToComplete = true;
                //listbox.onDraggingToComplete(this);
                listbox.onDraggingToDelete();
                TransformUtil.addTranslateDelta((FrameworkElement)LayoutRoot, e.HorizontalChange, 0.0);
                //this.completeTxtPanel.Visibility = Visibility.Visible;
                //this.completeTxt.Text = this.isCompleted ? "Uncomplete" : "Complete";
            }
            else
            {
                if (!draggingToDelete && !draggingToComplete)
                    return;
                e.Handled = true;
                TransformUtil.addTranslateDelta((FrameworkElement)LayoutRoot, e.HorizontalChange, 0.0);
            }
        }

        public void onReorderStarted()
        {
            this.reorderStarted = true;
            AnimationUtil.zoom((FrameworkElement)this, 70.0, 100, (Action<object, EventArgs>)null);
            Canvas.SetZIndex((UIElement)this, 32766);
        }

        public void onReorderCompleted()
        {
            this.reorderStarted = false;
            AnimationUtil.zoom((FrameworkElement)this, 0.0, 100, (Action<object, EventArgs>)null);
        }

        private void onKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            listbox.Focus();
        }

        private void Border_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!textFocused)
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
            textFocused = true;
        }

        private void text_LostFocus(object sender, RoutedEventArgs e)
        {
            textFocused = false;
            if (this.Tag != null)
            {
                if (listbox.currItem.FindItem(text.Text) != null)
                {
                    MessageBox.Show(String.Format("Item {0} is alredy present in list. Select another name.", text.Text));
                    text.Focus();
                }
                else
                {
                    ((ListItem)this.Tag).Name = text.Text;
                }
            }
        }

        private void text_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            textFocused = true;
        }
    }
}
