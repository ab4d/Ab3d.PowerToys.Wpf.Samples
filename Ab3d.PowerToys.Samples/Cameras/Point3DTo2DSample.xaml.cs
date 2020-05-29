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
using System.Diagnostics;
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

        private Point _oldSphere1ScreenPosition;
        private Rect _oldBox1ScreenBounds;
        private Point[] _sphere2MeshScreenPositions;

        private Ellipse[] _sphere2Ellipses;

        private DateTime _lastTime;


        public Point3DTo2DSample()
        {
            InitializeComponent();


            Camera1.CameraChanged += delegate (object sender, CameraChangedRoutedEventArgs args)
            {
                // Update the positions of the 2D elements on the OverlayCanvas on every camera change
                UpdateOverlayData();
            };

            MainViewport.SizeChanged += delegate (object sender, SizeChangedEventArgs args)
            {
                // Update the positions of the 2D elements on the OverlayCanvas when the size of Viewport3D is changed
                UpdateOverlayData();
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

            _isAnimating = true;
        }

        private void StopAnimation()
        {
            if (!_isAnimating)
                return;

            CompositionRenderingHelper.Instance.Unsubscribe(this);

            _isAnimating = false;

            _lastTime = DateTime.MinValue;
        }

        void ICompositionRenderingSubscriber.OnRendering(EventArgs e)
        {
            AnimateObjects();
            UpdateOverlayData();
        }

        private void AnimateObjects()
        {
            DateTime now = DateTime.Now;

            if (_lastTime == DateTime.MinValue)
            {
                _lastTime = now;
                return;
            }

            double secondsDiff = (now - _lastTime).TotalSeconds;
            double speed = SpeedSlider.Value;

            AnimateObjects(secondsDiff, speed);

            _lastTime = now;
        }

        public void AnimateObjects(double time, double speed)
        {
            AnimateTranslateTransform3D(Box1TranslateTransform3D,    time, speed);
            AnimateTranslateTransform3D(Sphere1TranslateTransform3D, time, speed);
            AnimateTranslateTransform3D(Sphere2TranslateTransform3D, time, speed);
        }

        private void AnimateTranslateTransform3D(TranslateTransform3D translateTransform3D, double time, double speed)
        {
            Point3D boxPosition = new Point3D(translateTransform3D.OffsetX, translateTransform3D.OffsetY, translateTransform3D.OffsetZ);

            // Rotate the box position for the secondsDiff * SpeedSlider.Value degrees
            AxisAngleRotation3D axisAngleRotation3D = new AxisAngleRotation3D(new Vector3D(0, 1, 0), time * speed);
            RotateTransform3D   rotateTransform3D   = new RotateTransform3D(axisAngleRotation3D);

            Point3D rotatedPosition = rotateTransform3D.Transform(boxPosition);

            translateTransform3D.OffsetX = rotatedPosition.X;
            translateTransform3D.OffsetY = rotatedPosition.Y;
            translateTransform3D.OffsetZ = rotatedPosition.Z;
        }

        //public void SetBoxPosition(Vector3D offset)
        //{
        //    AnimatedBoxTranslate.OffsetX = offset.X;
        //    AnimatedBoxTranslate.OffsetY = offset.Y;
        //    AnimatedBoxTranslate.OffsetZ = offset.Z;

        //    UpdateOverlayData();
        //}

        private void UpdateOverlayData()
        {
            //
            // Convert Point3D (sphere's CenterPosition) to 2D position on the screen with using camera.Point3DTo2D method
            //
            var sphereCenterPosition = Sphere1Visual3D.CenterPosition;
            sphereCenterPosition = Sphere1Visual3D.Transform.Transform(sphereCenterPosition);

            var spheresCenterOnScreen = Camera1.Point3DTo2D(sphereCenterPosition);

            // NOTE:
            // The above method requires that the camera is attached to real a Viewport3D
            // If you need to convert 3D coordinates to 2D space without creating Viewport3D, 
            // you can use the overloaded method that takes viewportSize as parameter - for example:
            //var targetPositionCamera = new Ab3d.Cameras.TargetPositionCamera()
            //{
            //    Heading = 30,
            //    Attitude = -20,
            //    Distance = 200
            //};
            //var point2D = targetPositionCamera.Point3DTo2D(new Point3D(100, 100, 100), viewportSize: new Size(200, 100));


            // Update UI only when the difference is significant (more than 1 pixel)
            bool isSphere1ScreenPositionChangedSignificantly =
                (Math.Abs(spheresCenterOnScreen.X - _oldSphere1ScreenPosition.X) >= 1.0 ||
                 Math.Abs(spheresCenterOnScreen.Y - _oldSphere1ScreenPosition.Y) >= 1.0);

            if (isSphere1ScreenPositionChangedSignificantly)
            {
                Sphere1ConnectionLine.X1 = spheresCenterOnScreen.X;
                Sphere1ConnectionLine.Y1 = spheresCenterOnScreen.Y;                
                
                Sphere1ConnectionLine.X2 = Sphere1ConnectionLine.X1 + 30;
                Sphere1ConnectionLine.Y2 = Sphere1ConnectionLine.Y1 - 15;


                Sphere1InfoTextBlock.Text = string.Format("Screen position\r\nby Point3DTo2D:\r\nx:{0:0} y:{1:0}",
                    spheresCenterOnScreen.X, spheresCenterOnScreen.Y);

                Canvas.SetLeft(Sphere1InfoBorder, spheresCenterOnScreen.X + 29);
                Canvas.SetTop(Sphere1InfoBorder, spheresCenterOnScreen.Y - Sphere1InfoBorder.ActualHeight - 14);

                _oldSphere1ScreenPosition = spheresCenterOnScreen;
            }


            // 
            // Convert Rect3D (Box's Bounds) to 2D Rect on the screen with using camera.Rect3DTo2D method
            //
            var boxBounds3D = Box1Visual3D.Content.Bounds;
            boxBounds3D = Box1Visual3D.Transform.TransformBounds(boxBounds3D);

            Rect screenBoxBounds2D = Camera1.Rect3DTo2D(boxBounds3D);


            // Update UI only when the difference is significant (more than 1 pixel)
            if (Math.Abs(screenBoxBounds2D.X - _oldBox1ScreenBounds.X) >= 1.0 ||
                Math.Abs(screenBoxBounds2D.Y - _oldBox1ScreenBounds.Y) >= 1.0 ||
                Math.Abs(screenBoxBounds2D.Width  - _oldBox1ScreenBounds.Width) >= 1.0 ||
                Math.Abs(screenBoxBounds2D.Height - _oldBox1ScreenBounds.Height) >= 1.0)
            {
                Canvas.SetLeft(Box1OverlayRectangle, screenBoxBounds2D.X);
                Canvas.SetTop(Box1OverlayRectangle, screenBoxBounds2D.Y);
                Box1OverlayRectangle.Width = screenBoxBounds2D.Width;
                Box1OverlayRectangle.Height = screenBoxBounds2D.Height;

                Box1ConnectionLine.X1 = screenBoxBounds2D.X + screenBoxBounds2D.Width - 1;
                Box1ConnectionLine.Y1 = screenBoxBounds2D.Y + 1;
                
                Box1ConnectionLine.X2 = Box1ConnectionLine.X1 + 20;
                Box1ConnectionLine.Y2 = Box1ConnectionLine.Y1 - 10;


                Box1InfoTextBlock.Text = string.Format("Screen bounds by\r\nRect3DTo2D:\r\nx:{0:0} y:{1:0}\r\nw:{2:0} h:{3:0}", screenBoxBounds2D.X, screenBoxBounds2D.Y, screenBoxBounds2D.Width, screenBoxBounds2D.Height);

                Canvas.SetLeft(Box1InfoBorder, screenBoxBounds2D.X + screenBoxBounds2D.Width + 18);
                Canvas.SetTop(Box1InfoBorder, screenBoxBounds2D.Y - Box1InfoBorder.ActualHeight - 8);

                _oldBox1ScreenBounds = screenBoxBounds2D;
            }


            if (isSphere1ScreenPositionChangedSignificantly) // reuse the test done for Sphere1
            {
                var sphereGeometryModel3D = Sphere2Visual3D.Content as GeometryModel3D;

                if (sphereGeometryModel3D != null)
                {
                    var meshGeometry3D = (MeshGeometry3D) sphereGeometryModel3D.Geometry;

                    var positionsCount = meshGeometry3D.Positions.Count;
                    if (_sphere2MeshScreenPositions == null || _sphere2MeshScreenPositions.Length != positionsCount)
                    {
                        // Do not create new array on each UI update
                        _sphere2MeshScreenPositions = new Point[positionsCount];

                        _sphere2Ellipses = new Ellipse[positionsCount];
                        for (int i = 0; i < positionsCount; i++)
                        {
                            var ellipse = new Ellipse()
                            {
                                Fill = Brushes.Yellow,
                                Width = 4,
                                Height = 4
                            };

                            OverlayCanvas.Children.Add(ellipse);

                            _sphere2Ellipses[i] = ellipse;
                        }
                    }

                    // Points3DTo2D also support parallel calculations (for lots of positions the perf gains are significant).
                    // Tests show that it is worth using parallel algorithm when number of positions is more the 300 (but this may be highly CPU dependent)
                    bool useParallelFor = positionsCount > 300;

                    bool success = Camera1.Points3DTo2D(points3D: meshGeometry3D.Positions,
                                                        points2D: _sphere2MeshScreenPositions,
                                                        transform: Sphere2Visual3D.Transform,
                                                        useParallelFor: useParallelFor);

                    if (success) // success can be false when the Viewport3D is not initialized (does not have its size) or the camera is not yet initialized
                    {
                        double xSum = 0;
                        double ySum = 0;
                        int samplesCount = 0;

                        double halfEllipseWidth = _sphere2Ellipses[0].Width / 2;
                        double halfEllipseHeight = _sphere2Ellipses[0].Width / 2;

                        for (var i = 0; i < _sphere2Ellipses.Length; i++)
                        {
                            var ellipse = _sphere2Ellipses[i];

                            double x = _sphere2MeshScreenPositions[i].X;
                            double y = _sphere2MeshScreenPositions[i].Y;
                            
                            if (double.IsNaN(x) || double.IsNaN(y)) // This may happen when object is on the camera's near plane
                                continue;

                            Canvas.SetLeft(ellipse, x - halfEllipseWidth);
                            Canvas.SetTop(ellipse, y - halfEllipseHeight);

                            xSum += x;
                            ySum += y;
                            samplesCount++;
                        }


                        // Calculate the center by averaging the screen positions:
                        double xCenter = xSum / samplesCount;
                        double yCenter = ySum / samplesCount;


                        Sphere2ConnectionLine.X1 = xCenter;
                        Sphere2ConnectionLine.Y1 = yCenter;

                        Sphere2ConnectionLine.X2 = xCenter + 49;
                        Sphere2ConnectionLine.Y2 = yCenter - 25;


                        Canvas.SetLeft(Sphere2InfoBorder, xCenter + 49);
                        Canvas.SetTop(Sphere2InfoBorder, yCenter - Sphere2InfoBorder.ActualHeight - 24);
                    }
                }
            }


            //Point3D centerPoint3D;
            //Point centerPoint2D;

            //// In this case we could also calculate centerPoint2D from bounds2D (see commented line below).
            //// But to demonstrate the Point3DTo2D method we are not using bounds2D
            ////centerPoint2D = new Point(bounds2D.X + bounds2D.Width / 2, bounds2D.Y + bounds2D.Height / 2);

            //centerPoint3D = new Point3D(bounds3D.X + bounds3D.SizeX / 2,
            //                            bounds3D.Y + bounds3D.SizeY / 2,
            //                            bounds3D.Z + bounds3D.SizeZ / 2);




            //int segments = 200;
            //bool parallel = false;
            //var sphereMesh3D = new Ab3d.Meshes.SphereMesh3D(new Point3D(0,0,0), 20, segments).Geometry;

            //var points2D = new Point[sphereMesh3D.Positions.Count];
            //var point3Ds = sphereMesh3D.Positions.ToArray();
            ////var point3Ds = sphereMesh3D.Positions;
            ////point3Ds.Freeze();

            //var stopwatch = new Stopwatch();
            //stopwatch.Start();

            //var translateTransform3D = new TranslateTransform3D(100, 0, 0);

            //bool isCorrect = false;
            //for (int i = 0; i < 10; i++)
            //{
            //    isCorrect = Camera1.Points3DTo2D(point3Ds, points2D, translateTransform3D, useParallelFor: parallel);
            //}



            //stopwatch.Stop();

            //if (isCorrect)
            //    MessageBox.Show("Time: " + stopwatch.Elapsed.TotalMilliseconds);



            //for (var i = 0; i < sphereMesh3D.Positions.Count; i++)
            //{
            //    var point3D = point3Ds[i];

            //    if (translateTransform3D != null)
            //        point3D = translateTransform3D.Transform(point3D);

            //    var point2D = Camera1.Point3DTo2D(point3D);
            //    var distance = (point2D - points2D[i]).Length;
            //    if (distance > 0.0001)
            //    {

            //    }
            //}




            // ADDITIONAL NOTE:
            // To convert 3D positions of a 3D LINE to screen positions, use the Line3DTo2D method instead of two calls to Point3DTo2D. 
            // The Line3DTo2D method correctly handles the case when the 3D line crosses the camera near plane (goes behind the camera).
            // In this case the line needs to be cropped at the camera near plane, otherwise the results are incorrect.

        }

        private Rect3D GetTransformedSphereBounds()
        {
            Rect3D transformedBounds;
            Rect3D bounds3D;
            Point3D center;

            bounds3D = Box1Visual3D.Content.Bounds;

            center = Box1Visual3D.Transform.Transform(Box1Visual3D.CenterPosition);

            transformedBounds = new Rect3D(center.X - bounds3D.SizeX / 2,
                                           center.Y - bounds3D.SizeY / 2,
                                           center.Z - bounds3D.SizeZ / 2,
                                           bounds3D.SizeX,
                                           bounds3D.SizeY,
                                           bounds3D.SizeZ);

            return transformedBounds;
        }
    }
}
