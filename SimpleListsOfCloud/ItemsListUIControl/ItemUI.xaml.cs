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
        private ItemListUI listbox;
        bool textFocused = false;

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
                int count = (Tag as ListItem).Items.Count;
                Color curColor = (itemBorder.Background as SolidColorBrush).Color;
                curColor.B = (byte)(226 - count * 10);
                curColor.G = (byte)(226 - count * 10);
                curColor.R = (byte)(226 - count * 10);
                itemBorder.Background = new SolidColorBrush(curColor);
                //if (count > 0)
                //{
                //    .A = 50;
                //}
            }
        }

        public void setText(string str)
        {
            text.Text = str;
            if (this.Tag != null)
            {
                ((ListItem)this.Tag).Name = str;
            }
            this.Dispatcher.BeginInvoke((Action)(() => onTextChanged((object)null, (TextChangedEventArgs)null)));
        }

        private void onTextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.Tag != null)
            {
                ((ListItem)this.Tag).Name = text.Text;
            }
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
            else
            {
                if (!this.draggingToDelete)
                    return;
                if (e != null)
                    e.Handled = true;
                this.draggingToDelete = false;
                DeleteImage.Visibility = System.Windows.Visibility.Collapsed;
                itemBorder.Width = 440;

                this.listbox.onDraggingToDeleteEnded();
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
            if (this.reorderStarted)
            {
                e.Handled = true;
                this.listbox.onReorderDelta(this, e.VerticalChange);
            }
            else if (!draggingToDelete && flag)
            {
                e.Handled = true;
                this.draggingToDelete = true;
                this.listbox.onDraggingToDelete();
                TransformUtil.addTranslateDelta((FrameworkElement)LayoutRoot, e.HorizontalChange, 0.0);
                DeleteImage.Visibility = System.Windows.Visibility.Visible;
                itemBorder.Width = 440 - DeleteImage.ActualWidth;
            }
            else
            {
                if (!draggingToDelete)
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
            this.listbox.Focus();
        }

        private void Border_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!textFocused)
            {
                //Debug.WriteLine("go");
                if (Tag != null && (Tag as ListItem).Items.Count > 0)
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
        }

        private void text_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            textFocused = true;
        }
    }
}
