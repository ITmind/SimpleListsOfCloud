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
    public class TransformUtil
    {
        public static void setRotateX(FrameworkElement item, double value)
        {
            ((PlaneProjection)item.Projection).RotationX = value;
        }

        public static double getRotateX(FrameworkElement item)
        {
            TransformUtil.addPlaneProjection(item);
            return ((PlaneProjection)item.Projection).RotationX;
        }

        public static double getRotateY(FrameworkElement item)
        {
            TransformUtil.addPlaneProjection(item);
            return ((PlaneProjection)item.Projection).RotationY;
        }

        public static void setRotateY(FrameworkElement item, double value)
        {
            TransformUtil.addPlaneProjection(item);
            ((PlaneProjection)item.Projection).RotationY = value;
        }

        public static void setRotateZ(FrameworkElement item, double value)
        {
            ((PlaneProjection)item.Projection).RotationZ = value;
        }

        public static double getRotateZ(FrameworkElement item)
        {
            return ((PlaneProjection)item.Projection).RotationZ;
        }

        public static void addTranslateDelta(FrameworkElement item, double xDelta, double yDelta)
        {
            TransformUtil.addCompositeTransform(item);
            CompositeTransform compositeTransform = (CompositeTransform)item.RenderTransform;
            compositeTransform.TranslateX += xDelta;
            compositeTransform.TranslateY += yDelta;
        }

        public static void setTranslateX(FrameworkElement item, double value)
        {
            if (Double.IsInfinity(value) ) return;
            TransformUtil.addCompositeTransform(item);
            ((CompositeTransform)item.RenderTransform).TranslateX = value;
        }

        public static double getTranslateX(FrameworkElement item)
        {
            
            TransformUtil.addCompositeTransform(item);
            return ((CompositeTransform)item.RenderTransform).TranslateX;
        }

        public static void setTranslateY(FrameworkElement item, double value)
        {
            if (Double.IsInfinity(value)) return;
            TransformUtil.addCompositeTransform(item);
            ((CompositeTransform)item.RenderTransform).TranslateY = value;
        }

        public static double getTranslateY(FrameworkElement item)
        {
            TransformUtil.addCompositeTransform(item);
            return ((CompositeTransform)item.RenderTransform).TranslateY;
        }

        public static void setGlobalOffsetZ(FrameworkElement item, double value)
        {
            ((PlaneProjection)item.Projection).GlobalOffsetZ = value;
        }

        public static double getGlobalOffsetZ(FrameworkElement item)
        {
            return ((PlaneProjection)item.Projection).GlobalOffsetZ;
        }

        public static double getScaleY(FrameworkElement item)
        {
            TransformUtil.addCompositeTransform(item);
            return ((CompositeTransform)item.RenderTransform).ScaleY;
        }

        public static void setScaleY(FrameworkElement item, double value)
        {
            TransformUtil.addCompositeTransform(item);
            ((CompositeTransform)item.RenderTransform).ScaleY = value;
        }

        public static double getScaleX(FrameworkElement item)
        {
            TransformUtil.addCompositeTransform(item);
            return ((CompositeTransform)item.RenderTransform).ScaleY;
        }

        public static void setScaleX(FrameworkElement item, double value)
        {
            TransformUtil.addCompositeTransform(item);
            ((CompositeTransform)item.RenderTransform).ScaleX = value;
        }

        public static void addCompositeTransform(FrameworkElement item)
        {
            if (item.RenderTransform != null && item.RenderTransform is CompositeTransform)
                return;
            item.RenderTransform = (Transform)new CompositeTransform();
        }

        public static void addPlaneProjection(FrameworkElement item)
        {
            if (item.Projection != null)
                return;
            item.Projection = (Projection)new PlaneProjection();
        }
    }
}
