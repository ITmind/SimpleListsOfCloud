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

namespace SimpleListsOfCloud
{
    public partial class ItemUI : UserControl
    {
        public bool disableGesture;
        private bool reorderStarted;
        private double dragStartX;
        private bool draggingToDelete;
        private ItemListUI listbox;

        public ItemUI()
        {
            InitializeComponent();
        }

        public ItemUI(ItemListUI listbox)
            : this()
        {
            this.listbox = listbox;
        }

        public void setText(string str)
        {
            this.text.Text = str;
            this.Dispatcher.BeginInvoke((Action)(() => this.onTextChanged((object)null, (TextChangedEventArgs)null)));
        }

        private void onTextChanged(object sender, TextChangedEventArgs e)
        {
            text.UpdateLayout();
        }

        public void enableTextEventTrapper(bool enable)
        {
            if (enable)
            {
                text.IsHitTestVisible = false;
            }
            else
            {
                text.IsHitTestVisible = true;
                text.Select(this.text.Text.Length, this.text.Text.Length);
                text.Focus();
            }
        }

        private void onTapTextEventTrapper(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (this.disableGesture)
                return;
            e.Handled = true;
            this.enableTextEventTrapper(false);
        }

        private void onGotFocusText(object sender, RoutedEventArgs e)
        {
            this.listbox.onEditText();
        }

        private void onLostFocusText(object sender, RoutedEventArgs e)
        {
            this.setText(this.text.Text.Trim());
            this.enableTextEventTrapper(true);
            if (this.text.Text.Length == 0)
                this.listbox.onDeleteItem(this);
            this.listbox.onCompletedEditText(this);
        }

        private void onTapTextBox(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (this.disableGesture)
                return;
            e.Handled = true;
        }

        private void onTapCounterGrid(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (this.disableGesture)
                return;
            e.Handled = true;
            this.listbox.maximizeTaskList(this);
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
                if (this.draggingToDelete)
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
                this.listbox.onDraggingToDeleteEnded();
                double translateX = TransformUtil.getTranslateX((FrameworkElement)LayoutRoot);
                if (translateX > -150.0 || MessageBox.Show("Delete list", "Are you sure?", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    AnimationUtil.translateX((FrameworkElement)LayoutRoot, translateX, 0.0, 100, (Action<object, EventArgs>)null);
                }
                else
                {
                    AnimationUtil.translateX((FrameworkElement)LayoutRoot, translateX, -this.ActualWidth, 100, (Action<object, EventArgs>)((s, ev) => this.listbox.onDeleteItem(this)));
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
            else if (!this.draggingToDelete && flag)
            {
                e.Handled = true;
                this.draggingToDelete = true;
                this.listbox.onDraggingToDelete();
                TransformUtil.addTranslateDelta((FrameworkElement)LayoutRoot, e.HorizontalChange, 0.0);
            }
            else
            {
                if (!this.draggingToDelete)
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
    }
}
