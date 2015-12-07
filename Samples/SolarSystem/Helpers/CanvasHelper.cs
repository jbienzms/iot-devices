using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace SolarSystem
{
    public class CanvasHelper
    {
        public static readonly DependencyProperty CanvasLeftProperty =
            DependencyProperty.RegisterAttached(
                "CanvasLeft", typeof(double), typeof(CanvasHelper),
                new PropertyMetadata(null, PropertyChanged));

        public static readonly DependencyProperty CanvasTopProperty =
            DependencyProperty.RegisterAttached(
                "CanvasTop", typeof(double), typeof(CanvasHelper),
                new PropertyMetadata(null, PropertyChanged));

        public static double GetCanvasLeft(DependencyObject obj)
        {
            return (double)obj.GetValue(CanvasLeftProperty);
        }

        public static void SetCanvasLeft(DependencyObject obj, double value)
        {
            obj.SetValue(CanvasLeftProperty, value);
        }

        public static double GetCanvasTop(DependencyObject obj)
        {
            return (double)obj.GetValue(CanvasTopProperty);
        }

        public static void SetCanvasTop(DependencyObject obj, double value)
        {
            obj.SetValue(CanvasTopProperty, value);
        }

        private static void PropertyChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {

            if (e.Property == CanvasLeftProperty)
            {
                Canvas.SetLeft((UIElement)obj, (double)e.NewValue);
            }
            else
            {
                Canvas.SetTop((UIElement)obj, (double)e.NewValue);
            }
        }
    }
}
