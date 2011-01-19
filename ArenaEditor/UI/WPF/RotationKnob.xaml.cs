using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using WpfMouse = System.Windows.Input.Mouse;

namespace AW2.UI.WPF
{
    public partial class RotationKnob : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty DegreesProperty =
            DependencyProperty.Register("Degrees", typeof(float), typeof(RotationKnob), new UIPropertyMetadata(0.0f));

        public event PropertyChangedEventHandler PropertyChanged;

        public float Degrees
        {
            get { return (float)GetValue(DegreesProperty); }
            set
            {
                if (value != Degrees) OnPropertyChanged("Degrees");
                SetValue(DegreesProperty, value);
            }
        }

        public RotationKnob()
        {
            InitializeComponent();
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            KnobEllipse.Stroke = Brushes.Red;
            WpfMouse.Capture(this);
        }

        private void MouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            WpfMouse.Capture(null);
            KnobEllipse.Stroke = Brushes.Black;
        }

        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (WpfMouse.Captured != this) return;
            var mousePosPoint = WpfMouse.GetPosition(this);
            var mousePos = new Vector2((float)mousePosPoint.X, (float)-mousePosPoint.Y);
            var knobCenter = new Vector2((float)ActualWidth, (float)-ActualHeight) / 2;
            var deltaPos = mousePos - knobCenter;
            var degrees = MathHelper.ToDegrees(deltaPos.Angle());
            Degrees = degrees.Modulo(360);
        }
    }
}
