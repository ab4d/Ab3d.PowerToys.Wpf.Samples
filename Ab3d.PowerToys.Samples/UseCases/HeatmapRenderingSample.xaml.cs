using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Ab3d.Common.Cameras;
using Ab3d.Controls;
using Ab3d.Visuals;
using Point = System.Windows.Point;

namespace Ab3d.PowerToys.Samples.UseCases
{
    // This sample is showing how to render a heatmap of a 3D object.
    //
    // This is done with first creating a 128 x 1 texture from the selected gradient.
    // This texture is then set as a DiffuseMaterial to the 3D object.
    // To specify which color to show for each position, we set the TextureCoordinates of the MeshGeometry3D.
    // The X value in the TextureCoordinate defines the color from the gradient - 0 is the top / left color; 1 is the bottom / right color.
    //
    // Note that because color is set for each position, it is possible to see the mesh of the 3D object.
    // This can be prevented with defining more detailed 3D model or with dividing each triangle into smaller triangles.
    // Also using more color transitions makes this effect more noticeable (see the difference between first and last gradient).
    //
    // It would be also possible to show heatmap with using VertexColorMaterial in Ab3d.DXEngine that can define color for each position.
    // But this would not give so accurate results in this case the colors are always interpolated from one color to another and 
    // the user does not have so good control over the transition as when using color gradient. 
    // For example when using color gradient the transition from blue to red can go through yellow,
    // but when using VertexColorMaterial, this transition is always direct as in the RBG circle - showing magenta as the intermediate color.

    /// <summary>
    /// Interaction logic for HeatmapRenderingSample.xaml
    /// </summary>
    public partial class HeatmapRenderingSample : Page
    {
        private GeometryModel3D _teapotGeometryModel3D;
        private MeshGeometry3D _teapotMeshGeometry3D;

        private bool _isUserBeamControl;
        private DateTime _animationStartTime;
        private ImageBrush _gradientImageBrush;

        public HeatmapRenderingSample()
        {
            InitializeComponent();

            // Use CameraControllerInfo to show that we can use left mouse button to set custom beam destination on the 3D model
            CameraControllerInfo.AddCustomInfoLine(0, MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed, "SET BEAM DESTINATION");

            // When the ViewportBorder size is change the size of the overlay Canvas (drawn over the 3D scene)
            ViewportBorder.SizeChanged += delegate(object sender, SizeChangedEventArgs args)
            {
                UpdateOverlayCanvasSize();
            };


            // Process mouse events
            ViewportBorder.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                // Start user beam control
                _isUserBeamControl = true;

                ViewportBorder.CaptureMouse();

                var position = e.GetPosition(ViewportBorder);
                ProcessMouseHit(position);
            };

            ViewportBorder.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
            {
                // Stop user beam control
                _isUserBeamControl = false;
                ViewportBorder.ReleaseMouseCapture();

                ProcessMouseOutOfModel();
            };

            // Subscribe to MouseMove to allow user to specify the beam target
            ViewportBorder.MouseMove += delegate (object sender, MouseEventArgs e)
            {
                if (_isUserBeamControl)
                    ProcessMouseHit(e.GetPosition(ViewportBorder));
                else
                    ProcessMouseOutOfModel();
            };


            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                LoadTestModel();

                // Start animating the beam position
                CompositionTarget.Rendering += CompositionTargetOnRendering;
            };

            // Cleanup
            this.Unloaded += delegate(object sender, RoutedEventArgs args)
            {
                CompositionTarget.Rendering -= CompositionTargetOnRendering;
            };
        }

        private void LoadTestModel()
        {
            // Load teapot model from obj file
            var readerObj = new Ab3d.ReaderObj();
            _teapotGeometryModel3D = readerObj.ReadModel3D(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\ObjFiles\teapot-hires.obj")) as GeometryModel3D;

            if (_teapotGeometryModel3D == null)
                return;

            _teapotMeshGeometry3D = (MeshGeometry3D)_teapotGeometryModel3D.Geometry;

            // Create gradient texture (128 x 1 texture with selected gradient)
            var gradientTexture = CreateGradientTexture();
            _gradientImageBrush               = new ImageBrush(gradientTexture);
            _gradientImageBrush.Viewport      = new Rect(0, 0, 1, 1);
            _gradientImageBrush.ViewportUnits = BrushMappingMode.Absolute; // Set ViewportUnits to absolute so the TextureCoordinates values are in range from 0 to 1 (defined in Viewport)

            // Set TextureCoordinates to the color for the lowest temperature - set target position far away
            UpdateTextureCoordinatesForDistance(_teapotMeshGeometry3D, targetPosition: new Point3D(10000, 0, 0), maxDistance: 50);

            _teapotGeometryModel3D.Material     = new DiffuseMaterial(_gradientImageBrush);
            _teapotGeometryModel3D.BackMaterial = new DiffuseMaterial(Brushes.Black); // Show inside of the Teapot as black

            MainViewport.Children.Add(_teapotGeometryModel3D.CreateModelVisual3D());
        }

        private WriteableBitmap CreateGradientTexture()
        {
            Rectangle selectedRectangle;

            if (Gradient1RadioButton.IsChecked ?? false)
                selectedRectangle = Rectangle1;
            else if (Gradient2RadioButton.IsChecked ?? false)
                selectedRectangle = Rectangle2;
            else // if (Gradient3RadioButton.IsChecked ?? false)
                selectedRectangle = Rectangle3;


            // We can use the GetGradientColorsArray from HeightMapMesh3D (Ab3d.PowerToys library) to convert the LinearGradientBrush to array of 128 colors
            var gradientColorsArray = Ab3d.Meshes.HeightMapMesh3D.GetGradientColorsUIntArray((LinearGradientBrush)selectedRectangle.Fill, 128);

            // Create new writable bitmap and write our gradient to
            WriteableBitmap texture = new WriteableBitmap(gradientColorsArray.Length, 1, 96, 96, PixelFormats.Pbgra32, null);
            texture.Lock();

            Int32Rect rect = new Int32Rect(0, 0, texture.PixelWidth, 1); // size: 128 x 1
            texture.WritePixels(rect, gradientColorsArray, stride: gradientColorsArray.Length * 4, offset: 0);

            texture.Unlock();

            return texture;
        }

        private void UpdateTextureCoordinatesForDistance(MeshGeometry3D mesh, Point3D targetPosition, double maxDistance)
        {
            Point3DCollection positions          = mesh.Positions;
            PointCollection   textureCoordinates = mesh.TextureCoordinates;

            mesh.TextureCoordinates = null; // Disconnect TextureCoordinates from MeshGeometry3D - this prevents events that are triggered on each TextureCoordinate change in the loop below

            var positionsCount = positions.Count;

            for (int i = 0; i < positionsCount; i++)
            {
                var onePosition = positions[i];

                // Get distance of this position from the targetPosition
                double length = (onePosition - targetPosition).Length;
                if (length > maxDistance)
                    length = maxDistance;

                // set the relative color index that is a number from 0 to 1 where 0 is the first color in the gradient and 1 is the last color in the gradient
                double relativeColorIndex = length / maxDistance;

                // Set X texture coordinate to the color in the _gradientColorsArray. 
                // We also invert the color so the colors starts from the bottom up.
                textureCoordinates[i] = new Point(1.0 - relativeColorIndex, 0);
            }

            mesh.TextureCoordinates = textureCoordinates; // Connect TextureCoordinates back to MeshGeometry3D
        }


        private void ProcessMouseHit(System.Windows.Point mousePosition)
        {
            RayMeshGeometry3DHitTestResult hitTestResult;

            if (double.IsNaN(mousePosition.X)) // if mousePosition.X is NaN, then we consider this as mouse did not hit the model
            {
                hitTestResult = null;
            }
            else
            {
                hitTestResult = VisualTreeHelper.HitTest(MainViewport, mousePosition) as RayMeshGeometry3DHitTestResult;

                BeamLine1.X2 = mousePosition.X;
                BeamLine1.Y2 = mousePosition.Y;
                BeamLine1.Visibility = Visibility.Visible;

                BeamLine2.X2 = mousePosition.X;
                BeamLine2.Y2 = mousePosition.Y;
                BeamLine2.Visibility = Visibility.Visible;
            }

            if (hitTestResult != null)
            {
                var hitPoint3D = hitTestResult.PointHit;

                // Update TextureCoordinates so that the correct colors from the gradient textures are shown
                UpdateTextureCoordinatesForDistance(_teapotMeshGeometry3D, hitPoint3D, maxDistance: 50);

                BeamLine1.Stroke = Brushes.Red;
                BeamLine2.Stroke = Brushes.Red;
            }
            else
            {
                // Show Gray line for missed position

                BeamLine1.Stroke = Brushes.Gray;
                BeamLine2.Stroke = Brushes.Gray;

                // use some distant position for hit point so that we get the whole model in the lowest color
                UpdateTextureCoordinatesForDistance(_teapotMeshGeometry3D, new Point3D(10000, 0, 0), 50);
            }
        }

        private void ProcessMouseOutOfModel()
        {
            ProcessMouseHit(new System.Windows.Point(double.NaN, double.NaN)); // This will act as we missed the 3D model
        }


        // Animate the beam position
        private void CompositionTargetOnRendering(object sender, EventArgs eventArgs)
        {
            if (_isUserBeamControl) // If user is controlling the beam position (when left mouse button is pressed), then we do not animate it
                return;

            if (_animationStartTime == DateTime.MinValue)
            {
                _animationStartTime = DateTime.Now;
                return;
            }

            double elapsedSeconds = (DateTime.Now - _animationStartTime).TotalSeconds;


            // Position around center of the OverlayCanvas and with radius = 100 and in clockwise direction (negate the elapsedSeconds)
            double xPos = Math.Sin(-elapsedSeconds) * 100 + OverlayCanvas.Width / 2;
            double yPos = Math.Cos(-elapsedSeconds) * 100 + OverlayCanvas.Height / 2;

            // Simulate mouse hit at the specified positions
            ProcessMouseHit(new System.Windows.Point(xPos, yPos));
        }

        private void UpdateOverlayCanvasSize()
        {
            OverlayCanvas.Width = ViewportBorder.ActualWidth;
            OverlayCanvas.Height = ViewportBorder.ActualHeight;

            BeamLine1.X1 = OverlayCanvas.Width * 2.0 / 5.0;
            BeamLine1.Y1 = OverlayCanvas.Height;

            BeamLine2.X1 = OverlayCanvas.Width * 3.0 / 5.0;
            BeamLine2.Y1 = OverlayCanvas.Height;

            BeamLine1.Visibility = Visibility.Collapsed;
            BeamLine2.Visibility = Visibility.Collapsed;
        }

        private void GradientRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateGradientTexture();
        }

        private void Rectangle1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Gradient1RadioButton.IsChecked = true;
        }

        private void Rectangle2_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Gradient2RadioButton.IsChecked = true;
        }

        private void Rectangle3_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Gradient3RadioButton.IsChecked = true;
        }

        private void UpdateGradientTexture()
        {
            _gradientImageBrush.ImageSource = CreateGradientTexture();
        }
    }
}
