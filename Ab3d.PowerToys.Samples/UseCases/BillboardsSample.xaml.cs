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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab3d.Common.Cameras;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for BillboardsSample.xaml
    /// </summary>
    public partial class BillboardsSample : Page
    {
        private TransparencySorter _transparencySorter;

        private double _overlayBrushHeight;

        public BillboardsSample()
        {
            InitializeComponent();
            
            _transparencySorter = new Ab3d.Utilities.TransparencySorter(TreesPlaceholerVisual3D)
            {
                UsedCamera = Camera1
            };

            // Explicitly set which Visual3D objects should be considered transparent.
            // This is needed because TransparencySorter does not know if texture image have transparency or not and therefore does not consider them transparent.
            // With calling AddTransparentModels we define that.
            // Another option to "persuade" TransparencySorter to consider objects are transparent is to set very small opacity to the ImageBrush - for example set Opacity to 0.99
            _transparencySorter.AddTransparentModels(TreePlaneVisual1, TreePlaneVisual2, TreePlaneVisual3, TreePlaneVisual4);

            _transparencySorter.Sort(TransparencySorter.SortingModeTypes.ByCameraDistance);


            Camera1.CameraChanged += Camera1OnCameraChanged;

            Camera1.StartRotation(30, 0);

            // PlaneVisual3D uses MatrixTransform3D to position, scale and orient the plane. This greatly improves the performance.
            // To disable that and change MeshGeometry3D instead of MatrixTransform3D, set UseMatrixTransform3D to false:
            //PlaneVisual1.UseMatrixTransform3D = false;

            MainViewport.SizeChanged += (sender, args) => UpdateOverlayCanvasElements();
        }

        private void Camera1OnCameraChanged(object sender, CameraChangedRoutedEventArgs cameraChangedRoutedEventArgs)
        {
            if (!(AlignWithCameraCheckBox.IsChecked ?? false))
                return;


            // On each change of camera we will update the orientation of billboard objects.
            // When using PlaneVisual3D, TextBlockVisual3D or TextVisual3D we can simply call the AlignWithCamera method.

            // If we did some changes to the Camera1 in the code,
            // it is recommended to call Refresh before calling AlignWithCamera method:
            // Camera1.Refresh();

            bool fixYAxis = FixYAxisCheckBox.IsChecked ?? false;

            PlaneVisual1.AlignWithCamera(Camera1);
            TextBlockVisual3D1.AlignWithCamera(Camera1);


            if (fixYAxis)
            {
                // To show a billboard with fixed Y axis, we first call AlignWithCamera and then set the UpDirection back to Y axis.
                PlaneVisual1.HeightDirection = Ab3d.Common.Constants.UpVector; // = new Vector3D(0, 1, 0);
                TextBlockVisual3D1.UpDirection = Ab3d.Common.Constants.UpVector;
            }


            if (FixScreenSizeCheckBox.IsChecked ?? false)
            {
                // When FixScreenSizeCheckBox is checked, we update the size of the billboards in 3D works
                // so that they are rendered to the same size on the screen.
                //
                // This is done with the GetWorldSize method that calculates a size in 3D world coordinates from a size provided in 2D screen coordinates.
                // You can also use CameraUtils.GetOrthographicWorldSize and CameraUtils.GetPerspectiveWorldSize methods to get the same results but with more low level parameters.
                //
                // It is also possible to get the opposite value - size on screen - with the following methods:
                // Camera1.GetScreenSize(Size worldSize, Point3D targetPosition3D)
                // CameraUtils.GetPerspectiveScreenSize(Size worldSize, double lookDirectionDistance, double fieldOfView, Size viewport3DSize)
                // CameraUtils.GetOrthographicScreenSize(Size worldSize, double cameraWidth, Size viewport3DSize)

                // Set the size so that it will be shown in 200 x 40 box on the screen.
                Size worldSize = Camera1.GetWorldSize(new Size(200, 40), TextBlockVisual3D1.Position);


                // With TextBlockVisual3D we can use multiple methods to adjust the size of the text:
                // 1) Change FontSize property - this is the most performance expensive method because this will require to update the text (and render the bitmap if it is used to show the content of TextBlockVisual3D - for example when in DXEngine).
                // 2) Change Size property - this is better but would require to update the plane's MeshGeometry3D. This creates a lot of objects on each camera change and require frequent garbage collections.
                //    TextBlockVisual3D1.Size = worldSize;
                // 3) Scale the TextBlockVisual3D1 to the required size. This is by far the fastest and most efficient method because only the transformation is changed:

                var scaleTransform3D = TextBlockVisual3D1.Transform as ScaleTransform3D;
                if (scaleTransform3D == null)
                {
                    scaleTransform3D = new ScaleTransform3D();

                    // To prevent scaling the position (multiplying it with the scale factor), we need to set the center of scale
                    scaleTransform3D.CenterX = TextBlockVisual3D1.Position.X;
                    scaleTransform3D.CenterY = TextBlockVisual3D1.Position.Y;
                    scaleTransform3D.CenterZ = TextBlockVisual3D1.Position.Z;

                    TextBlockVisual3D1.Transform = scaleTransform3D;
                }

                // Calculate required scale to get the size of the TextBlockVisual3D to the required worldSize
                double scaleFactor = worldSize.Width / TextBlockVisual3D1.Size.Width;
                scaleTransform3D.ScaleX = scaleFactor;
                scaleTransform3D.ScaleY = scaleFactor;
                scaleTransform3D.ScaleZ = scaleFactor;


                // Do the same for PlaneVisual3D
                worldSize = Camera1.GetWorldSize(new Size(200, 40), PlaneVisual1.CenterPosition);

                // The easiest way to update the size of a PlaneVisual3D is to change its Size.
                // But because this creates a new MeshGeometry3D, this can produce a lot of garbage because this is called on each frame (on each camera change).
                // Therefore it is better to use ScaleTransform3D to update the size.
                //PlaneVisual1.Size = worldSize;

                scaleTransform3D = PlaneVisual1.Transform as ScaleTransform3D;
                if (scaleTransform3D == null)
                {
                    scaleTransform3D = new ScaleTransform3D();

                    // To prevent scaling the position (multiplying it with the scale factor), we need to set the center of scale
                    scaleTransform3D.CenterX = PlaneVisual1.CenterPosition.X;
                    scaleTransform3D.CenterY = PlaneVisual1.CenterPosition.Y;
                    scaleTransform3D.CenterZ = PlaneVisual1.CenterPosition.Z;

                    PlaneVisual1.Transform = scaleTransform3D;
                }

                scaleFactor = worldSize.Width / PlaneVisual1.Size.Width;
                scaleTransform3D.ScaleX = scaleFactor;
                scaleTransform3D.ScaleY = scaleFactor;
                scaleTransform3D.ScaleZ = scaleFactor;
            }
            else
            {
                // When FixScreenSizeCheckBox is unchecked, reset the size back to initial size

                TextBlockVisual3D1.Transform = null;
                PlaneVisual1.Transform = null;

                // The following code is used if we do not use ScaleTransform3D but instead change the Size

                //if (TextBlockVisual3D1.Size.Width != 80)
                //    TextBlockVisual3D1.Size = new Size(80, 20);

                //if (PlaneVisual1.Size.Width != 80)
                //    PlaneVisual1.Size = new Size(80, 20); 
            }


            // Update all tree objects
            foreach (var treePlaneVisual in TreesPlaceholerVisual3D.Children.OfType<PlaneVisual3D>())
            {
                treePlaneVisual.AlignWithCamera(Camera1);

                if (fixYAxis)
                    treePlaneVisual.HeightDirection = Ab3d.Common.Constants.UpVector;
            }


            // After objects were rotated, we can reorder them so that those farther away from the camera are rendered first.
            // This way the objects will be correctly visible through transparent objects.
            _transparencySorter.Sort(TransparencySorter.SortingModeTypes.ByCameraDistance);


            UpdateOverlayCanvasElements();
        }

        private void UpdateOverlayCanvasElements()
        {
            if (!this.IsLoaded)
                return;

            // Start with right top back edge of the Box
            //var position3D = GoldBoxVisual3D.CenterPosition + new Vector3D(-GoldBoxVisual3D.Size.X / 2, GoldBoxVisual3D.Size.Y / 2, GoldBoxVisual3D.Size.Z / 2);
            var position3D = GoldBoxVisual3D.CenterPosition + new Vector3D(0, GoldBoxVisual3D.Size.Y / 2, 0);

            if (_overlayBrushHeight < 0.001)
            {
                OverlayInfoTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                _overlayBrushHeight = OverlayInfoTextBlock.DesiredSize.Height + OverlayInfoBorder.BorderThickness.Bottom + OverlayInfoBorder.BorderThickness.Top;
            }

            var pos1 = Camera1.Point3DTo2D(position3D);
            var pos2 = pos1 + new Vector(30, -20);

            OverlayLine.X1 = pos1.X;
            OverlayLine.Y1 = pos1.Y;

            OverlayLine.X2 = pos2.X;
            OverlayLine.Y2 = pos2.Y;

            Canvas.SetLeft(OverlayInfoBorder, pos2.X);
            Canvas.SetTop(OverlayInfoBorder, pos2.Y - _overlayBrushHeight / 2);
        }
    }
}
