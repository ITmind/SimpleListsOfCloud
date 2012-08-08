using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

namespace SimpleListsOfCloud.ItemsListUIControl
{
    public partial class FastItemContol : UserControl
    {
        private FastListItem _list;
        private bool _draggingToDelete = false;
        private bool _draggingToComplete = false;
        private Color _markColor = Colors.Gray;
        private Color _empetyColor = (Color)Application.Current.Resources["PhoneAccentColor"];

        public FastItemContol()
        {
            //DataContext = this;
            InitializeComponent();
        }

        private void itemBorder_Tap(object sender, GestureEventArgs e)
        {
            
        }

        public void Update()
        {

            var count = ((ListItem) DataContext).Items.Count(x => !x.Deleted);
            Color curColor;
            if (((ListItem)DataContext).Mark)
            {
                count = count < 10 ? count : 10;

                curColor = new Color
                               {
                                   B = (byte) (_markColor.B - count*10),
                                   R = (byte) (_markColor.R - count*10),
                                   G = (byte) (_markColor.G - count*10),
                                   A = _markColor.A
                               };
            }
            else
            {

                count = count < 8 ? count : 8;

                curColor = new Color();
                if (_empetyColor.B - count*10 >= 0)
                {
                    curColor.B = (byte) (_empetyColor.B - count*10);
                }
                if (_empetyColor.R - count*10 >= 0)
                {
                    curColor.R = (byte) (_empetyColor.R - count*10);
                }
                if (_empetyColor.G - count*10 >= 0)
                {
                    curColor.G = (byte) (_empetyColor.G - count*10);
                }
                curColor.A = _empetyColor.A;
            }
            itemBorder.Background = new SolidColorBrush(curColor);

        }

        private FastListItem FindFastListItem(DependencyObject item)
        {
            DependencyObject par = VisualTreeHelper.GetParent(item);

            return (par.GetType() == typeof(FastListItem)) ? par as FastListItem : FindFastListItem(par);
        }

        private void GestureListener_DragStarted(object sender, Microsoft.Phone.Controls.DragStartedGestureEventArgs e)
        {
            //Debug.WriteLine("item_DragStarted");
            if (_list == null)
            {
                _list = FindFastListItem(this);
            }
            _list.IsDragItem = true;
        }

        private void GestureListener_DragDelta(object sender, Microsoft.Phone.Controls.DragDeltaGestureEventArgs e)
        {
            //Debug.WriteLine("item_DragDelta");
            bool flag = e.HorizontalChange < 0.0 && e.Direction == Orientation.Horizontal;
            bool flag2 = e.HorizontalChange > 0.0 && e.Direction == Orientation.Horizontal;

            if (!_draggingToDelete && flag && !_draggingToComplete)
            {
                _draggingToDelete = true;
                TransformUtil.addTranslateDelta(LayoutRoot, e.HorizontalChange, 0.0);
                DeleteImage.Visibility = Visibility.Visible;
                itemBorder.Width = 440 - DeleteImage.ActualWidth;
            }
            else if (!_draggingToDelete && flag2 && !_draggingToComplete)
            {
                _draggingToComplete = true;
                TransformUtil.addTranslateDelta(LayoutRoot, e.HorizontalChange, 0.0);
            }
            else
            {
                if (!_draggingToDelete && !_draggingToComplete)
                    return;
                TransformUtil.addTranslateDelta(LayoutRoot, e.HorizontalChange, 0.0);
            }
        }

        private void GestureListener_DragCompleted(object sender, Microsoft.Phone.Controls.DragCompletedGestureEventArgs e)
        {
            //Debug.WriteLine("item_DragCompleted");
            _list.IsDragItem = false;
           
            if (_draggingToComplete)
            {
                _draggingToComplete = false;
                double translateX = TransformUtil.getTranslateX(LayoutRoot);
                if (translateX < 150.0)
                {
                    AnimationUtil.translateX(LayoutRoot, translateX, 0.0, 100, null);
                }
                else
                {
                    //setCompletingLook();
                    AnimationUtil.translateX(LayoutRoot, translateX, 0.0, 100, (s, ev) =>
                                                                                   {
                                                                                       var item = (ListItem) DataContext;                                                                                  
                                                                                       item.SetMark(!item.Mark);
                                                                                       Update();
                                                                                   });
                    
                }
            }
            else if (_draggingToDelete)
            {
                _draggingToDelete = false;
                DeleteImage.Visibility = Visibility.Collapsed;
                itemBorder.Width = 450;

                //listbox.onDraggingToDeleteEnded();
                double translateX = TransformUtil.getTranslateX((FrameworkElement)LayoutRoot);
                if (translateX > -150.0 || MessageBox.Show("Delete list", "Are you sure?", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    AnimationUtil.translateX(LayoutRoot, translateX, 0.0, 100, null);
                }
                else
                {
                    //AnimationUtil.translateX(LayoutRoot, translateX, -this.ActualWidth, 100, (Action<object, EventArgs>)((s, ev) => listbox.onDeleteItem(this)));
                }
            }
        }

        
    }
}
