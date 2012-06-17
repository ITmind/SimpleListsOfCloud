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
    public class CommonUtil
    {
        public static int calcAnimateYTime(FrameworkElement item, double destPosition)
        {
            return CommonUtil.calcAnimateYTime(item, destPosition, 1.0);
        }

        public static int calcAnimateYTime(FrameworkElement item, double destPosition, double ratio)
        {
            double translateY = TransformUtil.getTranslateY(item);
            int num = 100;
            return (int)(50.0 * Math.Abs(destPosition - translateY) / (double)num * ratio);
        }
    }
}
