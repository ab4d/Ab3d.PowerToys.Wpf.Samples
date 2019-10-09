using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using Ab3d.PowerToys.Samples.UseCases;
using System.Collections.ObjectModel;
using Ab3d.Common.EventManager3D;
using System.Windows.Media.Animation;
using Ab3d.Common.Cameras;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for Point3DTo2DSample.xaml
    /// </summary>
    public partial class Point3DTo2DSample : Page, ICompositionRenderingSubscriber
    {
        private bool _isAnimating;

        private Rect _oldBounds2D;

        private DateTime _lastTime;


        public Point3DTo2DSample()
        {
            InitializeComponent();


            Camera1.CameraChanged += delegate (object sender, CameraChangedRoutedEventArgs args)
            {
                // Update the positions of the 2D elements on the OverlayCanvas on every camera change
                UpdateObjectPositions();
            };

            MainViewport.SizeChanged += delegate (object sender, SizeChangedEventArgs args)
            {
                // Update the positions of the 2D elements on the OverlayCanvas when the size of Viewport3D is changed
                UpdateObjectPositions();
            };


            this.Loaded += delegate(object o, RoutedEventArgs args)
            {
                StartAnimation();
            };

            this.Unloaded += delegate(object o, RoutedEventArgs args)
            {
                StopAnimation();
            };
        }

        private void StartAnimation()
        {
            if (_isAnimating)
                return;

            // Use CompositionRenderingHelper to subscribe to CompositionTarget.Rendering event
            // This is much safer because in case we forget to unsubscribe from Rendering, the CompositionRenderingHelper will unsubscribe us automatically
            // This allows to collect this class will Grabage collector and prevents infinite calling of Rendering handler.
            // After subscribing the ICompositionRenderingSubscriber.OnRendering method will be called on each CompositionTarget.Rendering event
            CompositionRenderingHelper.Instance.Subscribe(this);

            StartStopButton.Content = "Stop animation";
            _isAnimating = true;
        }

        private void StopAnimation()
        {
            if (!_isAnimating)
                return;

            CompositionRenderingHelper.Instance.Unsubscribe(this);

            StartStopButton.Content = "Start animation";
            _isAnimating = false;

            _lastTime = DateTime.MinValue;
        }

        void ICompositionRenderingSubscriber.OnRendering(EventArgs e)
        {
            AnimateBox();
            UpdateObjectPositions();
        }

        private void AnimateBox()
        {
            DateTime now = DateTime.Now;

            if (_lastTime == DateTime.MinValue)
            {
                _lastTime = now;
                return;
            }

            double secondsDiff = (now - _lastTime).TotalSeconds;

            Point3D boxPosition = new Point3D(AnimatedBoxTranslate.OffsetX, AnimatedBoxTranslate.OffsetY, AnimatedBoxTranslate.OffsetZ);

            // Rotate the box position for the secondsDiff * SpeedSlider.Value degrees
            AxisAngleRotation3D axisAngleRotation3D = new AxisAngleRotation3D(new Vector3D(0, 1, 0), secondsDiff * SpeedSlider.Value);
            RotateTransform3D rotateTransform3D = new RotateTransform3D(axisAngleRotation3D);

            Point3D rotatedPosition = rotateTransform3D.Transform(boxPosition);

            AnimatedBoxTranslate.OffsetX = rotatedPosition.X;
            AnimatedBoxTranslate.OffsetY = rotatedPosition.Y;
            AnimatedBoxTranslate.OffsetZ = rotatedPosition.Z;

            _lastTime = now;
        }

        public void SetBoxPosition(Vector3D offset)
        {
            AnimatedBoxTranslate.OffsetX = offset.X;
            AnimatedBoxTranslate.OffsetY = offset.Y;
            AnimatedBoxTranslate.OffsetZ = offset.Z;

            UpdateObjectPositions();
        }

        private void UpdateObjectPositions()
        {
            Rect3D bounds3D = GetTransformedSphereBounds();

            Rect bounds2D = Camera1.Rect3DTo2D(bounds3D);

            // Check if the difference is significant (more than 1 pixel)
            if (Math.Abs(bounds2D.X - _oldBounds2D.X) < 1.0 &&
                Math.Abs(bounds2D.Y - _oldBounds2D.Y) < 1.0 &&
                Math.Abs(bounds2D.Width - _oldBounds2D.Width) < 1.0 &&
                Math.Abs(bounds2D.Height - _oldBounds2D.Height) < 1.0)
            {
                // We do not update the positions if the change is very small
                return;
            }


            if (RectRadioButton.IsChecked ?? false)
            {
                Canvas.SetLeft(OverlayRectangle, bounds2D.X);
                Canvas.SetTop(OverlayRectangle, bounds2D.Y);
                OverlayRectangle.Width = bounds2D.Width;
                OverlayRectangle.Height = bounds2D.Height;

                if (OverlayRectangle.Visibility != Visibility.Visible)
                {
                    OverlayRectangle.Visibility = Visibility.Visible;
                    OverlayEllipse.Visibility = Visibility.Hidden;
                }

                InfoTextBlock.Text = string.Format("Screen bounds:\r\nx:{0:0} y:{1:0}\r\nw:{2:0} h:{3:0}", bounds2D.X, bounds2D.Y, bounds2D.Width, bounds2D.Height);
            }
            else
            {
                Point3D centerPoint3D;
                Point centerPoint2D;

                // In this case we could also calculate centerPoint2D from bounds2D (see commented line below).
                // But to demonstrate the Point3DTo2D method we are not using bounds2D
                //centerPoint2D = new Point(bounds2D.X + bounds2D.Width / 2, bounds2D.Y + bounds2D.Height / 2);
                
                centerPoint3D = new Point3D(bounds3D.X + bounds3D.SizeX / 2,
                                            bounds3D.Y + bounds3D.SizeY / 2,
                                            bounds3D.Z + bounds3D.SizeZ / 2);

                centerPoint2D = Camera1.Point3DTo2D(centerPoint3D);

                // NOTE:
                // The above method requires that the camera is attached to real Viewport3D
                // If you need to convert 3D coordinates to 2D space without creating Viewport3D, 
                // you can use the overloaded method that takes viewportSize as parameter - for example:
                //var targetPositionCamera = new Ab3d.Cameras.TargetPositionCamera()
                //{
                //    Heading = 30,
                //    Attitude = -20,
                //    Distance = 200
                //};
                //var point2D = targetPositionCamera.Point3DTo2D(new Point3D(100, 100, 100), new Size(200, 100));

                // ADDITIONAL NOTE:
                // To convert 3D positions of a 3D LINE to screen positions, use the Line3DTo2D method instead of two calls to Point3DTo2D. 
                // The Line3DTo2D method correctly handles the case when the 3D line crosses the camera near plane (goes behind the camera).
                // In this case the line needs to be cropped at the camera near plane, otherwise the results are incorrect.

                Canvas.SetLeft(OverlayEllipse, centerPoint2D.X);
                Canvas.SetTop(OverlayEllipse, centerPoint2D.Y);

                if (OverlayEllipse.Visibility != Visibility.Visible)
                {
                    OverlayEllipse.Visibility = Visibility.Visible;
                    OverlayRectangle.Visibility = Visibility.Hidden;
                }

                InfoTextBlock.Text = string.Format("Center position:\r\nx:{0:0} y:{1:0}", bounds2D.X + bounds2D.Width / 2, bounds2D.Y + bounds2D.Height / 2);
            }

            Canvas.SetLeft(InfoBorder, bounds2D.X + bounds2D.Width + 5);
            Canvas.SetTop(InfoBorder, bounds2D.Y);


            _oldBounds2D = bounds2D;
        }

        private Rect3D GetTransformedSphereBounds()
        {
            Rect3D transformedBounds;
            Rect3D bounds3D;
            Point3D center;

            bounds3D = AnimatedBox.Content.Bounds;

            center = AnimatedBox.Transform.Transform(AnimatedBox.CenterPosition);

            transformedBounds = new Rect3D(center.X - bounds3D.SizeX / 2,
                                           center.Y - bounds3D.SizeY / 2,
                                           center.Z - bounds3D.SizeZ / 2,
                                           bounds3D.SizeX,
                                           bounds3D.SizeY,
                                           bounds3D.SizeZ);

            return transformedBounds;
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating)
                StopAnimation();
            else
                StartAnimation();
        }
    }
}
