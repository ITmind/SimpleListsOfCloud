using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SimpleListsOfCloud.Utils
{
    public class AnimationUtil
    {
        public static void colour(Brush item, Color from, Color to, int msec, Action<object, EventArgs> callback)
        {
            Storyboard storyboard = new Storyboard();
            ColorAnimation colorAnimation1 = new ColorAnimation();
            colorAnimation1.From = new Color?(from);
            colorAnimation1.To = new Color?(to);
            colorAnimation1.Duration = new Duration(new TimeSpan(0, 0, 0, 0, msec));
            ColorAnimation colorAnimation2 = colorAnimation1;
            Storyboard.SetTarget((Timeline)colorAnimation2, (DependencyObject)item);
            Storyboard.SetTargetProperty((Timeline)colorAnimation2, new PropertyPath("Color", new object[0]));
            storyboard.Children.Add((Timeline)colorAnimation2);
            if (callback != null)
                storyboard.Completed += new EventHandler(callback.Invoke);
            storyboard.Begin();
        }

        public static void opacity(FrameworkElement item, double to, int msec, Action<object, EventArgs> callback)
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation doubleAnimation1 = new DoubleAnimation();
            doubleAnimation1.From = new double?(item.Opacity);
            doubleAnimation1.To = new double?(to);
            doubleAnimation1.Duration = new Duration(new TimeSpan(0, 0, 0, 0, msec));
            DoubleAnimation doubleAnimation2 = doubleAnimation1;
            Storyboard.SetTarget((Timeline)doubleAnimation2, (DependencyObject)item);
            Storyboard.SetTargetProperty((Timeline)doubleAnimation2, new PropertyPath("(UIElement.Opacity)", new object[0]));
            storyboard.Children.Add((Timeline)doubleAnimation2);
            if (callback != null)
                storyboard.Completed += new EventHandler(callback.Invoke);
            storyboard.Begin();
        }

        public static void zoom(FrameworkElement item, double to, int msec, Action<object, EventArgs> callback)
        {
            double globalOffsetZ = TransformUtil.GetGlobalOffsetZ(item);
            Storyboard storyboard = new Storyboard();
            DoubleAnimation doubleAnimation1 = new DoubleAnimation();
            doubleAnimation1.From = new double?(globalOffsetZ);
            doubleAnimation1.To = new double?(to);
            doubleAnimation1.Duration = new Duration(new TimeSpan(0, 0, 0, 0, msec));
            DoubleAnimation doubleAnimation2 = doubleAnimation1;
            Storyboard.SetTarget((Timeline)doubleAnimation2, (DependencyObject)item);
            Storyboard.SetTargetProperty((Timeline)doubleAnimation2, new PropertyPath("(UIElement.Projection).(PlaneProjection.GlobalOffsetZ)", new object[0]));
            storyboard.Children.Add((Timeline)doubleAnimation2);
            if (callback != null)
                storyboard.Completed += new EventHandler(callback.Invoke);
            storyboard.Begin();
        }

        public static void bounce(FrameworkElement item, double height, int msec, Action<object, EventArgs> callback)
        {
            double translateY = TransformUtil.getTranslateY(item);
            Storyboard storyboard = new Storyboard();
            DoubleAnimationUsingKeyFrames animationUsingKeyFrames = new DoubleAnimationUsingKeyFrames();
            EasingDoubleKeyFrame easingDoubleKeyFrame1 = new EasingDoubleKeyFrame();
            easingDoubleKeyFrame1.KeyTime = (KeyTime)new TimeSpan(0, 0, 0, 0, (int)((double)msec * 0.5));
            easingDoubleKeyFrame1.Value = translateY - height;
            animationUsingKeyFrames.KeyFrames.Add((DoubleKeyFrame)easingDoubleKeyFrame1);
            EasingDoubleKeyFrame easingDoubleKeyFrame2 = new EasingDoubleKeyFrame();
            easingDoubleKeyFrame2.KeyTime = (KeyTime)new TimeSpan(0, 0, 0, 0, msec);
            easingDoubleKeyFrame2.Value = translateY;
            animationUsingKeyFrames.KeyFrames.Add((DoubleKeyFrame)easingDoubleKeyFrame2);
            Storyboard.SetTarget((Timeline)animationUsingKeyFrames, (DependencyObject)item);
            Storyboard.SetTargetProperty((Timeline)animationUsingKeyFrames, new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.TranslateY)", new object[0]));
            storyboard.Children.Add((Timeline)animationUsingKeyFrames);
            if (callback != null)
                storyboard.Completed += new EventHandler(callback.Invoke);
            storyboard.Begin();
        }

        public static void translateY(FrameworkElement item, double toY, int msec, Action<object, EventArgs> callback)
        {
            double translateY = TransformUtil.getTranslateY(item);
            Storyboard storyboard = new Storyboard();
            DoubleAnimation doubleAnimation1 = new DoubleAnimation();
            doubleAnimation1.From = new double?(translateY);
            doubleAnimation1.To = new double?(toY);
            doubleAnimation1.Duration = new Duration(new TimeSpan(0, 0, 0, 0, msec));
            DoubleAnimation doubleAnimation2 = doubleAnimation1;
            Storyboard.SetTarget((Timeline)doubleAnimation2, (DependencyObject)item);
            Storyboard.SetTargetProperty((Timeline)doubleAnimation2, new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.TranslateY)", new object[0]));
            storyboard.Children.Add((Timeline)doubleAnimation2);
            if (callback != null)
                storyboard.Completed += new EventHandler(callback.Invoke);
            storyboard.Begin();
        }

        public static void translateX(FrameworkElement item, double fromX, double toX, int msec, Action<object, EventArgs> callback)
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation doubleAnimation1 = new DoubleAnimation();
            doubleAnimation1.From = new double?(fromX);
            doubleAnimation1.To = new double?(toX);
            doubleAnimation1.Duration = new Duration(new TimeSpan(0, 0, 0, 0, msec));
            DoubleAnimation doubleAnimation2 = doubleAnimation1;
            Storyboard.SetTarget((Timeline)doubleAnimation2, (DependencyObject)item);
            Storyboard.SetTargetProperty((Timeline)doubleAnimation2, new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.TranslateX)", new object[0]));
            storyboard.Children.Add((Timeline)doubleAnimation2);
            if (callback != null)
                storyboard.Completed += new EventHandler(callback.Invoke);
            storyboard.Begin();
        }

        public static void rotateY(FrameworkElement item, double toY, int msec, Action<object, EventArgs> callback)
        {
            double rotateY = TransformUtil.getRotateY(item);
            Storyboard storyboard = new Storyboard();
            DoubleAnimation doubleAnimation1 = new DoubleAnimation();
            doubleAnimation1.From = new double?(rotateY);
            doubleAnimation1.To = new double?(toY);
            doubleAnimation1.Duration = new Duration(new TimeSpan(0, 0, 0, 0, msec));
            DoubleAnimation doubleAnimation2 = doubleAnimation1;
            Storyboard.SetTarget((Timeline)doubleAnimation2, (DependencyObject)item);
            Storyboard.SetTargetProperty((Timeline)doubleAnimation2, new PropertyPath("(UIElement.Projection).(PlaneProjection.RotationY)", new object[0]));
            storyboard.Children.Add((Timeline)doubleAnimation2);
            if (callback != null)
                storyboard.Completed += new EventHandler(callback.Invoke);
            storyboard.Begin();
        }
    }
}
