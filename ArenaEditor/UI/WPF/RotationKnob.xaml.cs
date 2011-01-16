using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfMouse = System.Windows.Input.Mouse;
using Microsoft.Xna.Framework;

namespace AW2.UI.WPF
{
    public partial class RotationKnob : UserControl
    {
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("WpfAngle", typeof(double), typeof(RotationKnob), new UIPropertyMetadata(0.0));

        public double WpfAngle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        public float AWAngle
        {
            get { return MathHelper.ToRadians(-(float)WpfAngle); }
        }

        public RotationKnob()
        {
            InitializeComponent();
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
            if (WpfMouse.Captured == this)
            {
                var mousePosPoint = WpfMouse.GetPosition(this);
                var mousePos = new Vector2((float)mousePosPoint.X, (float)mousePosPoint.Y);
                var knobCenter = new Vector2((float)ActualWidth, (float)ActualHeight) / 2;
                var relativeMousePos = mousePos - knobCenter;
                var dot = Vector2.Dot(relativeMousePos, Vector2.UnitX);
                var acos = MathHelper.ToDegrees((float)Math.Acos(dot / relativeMousePos.Length()));
                WpfAngle = relativeMousePos.Y >= 0 ? acos : -acos;
            }
        }
    }
}
