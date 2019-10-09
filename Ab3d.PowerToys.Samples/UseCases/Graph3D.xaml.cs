using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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
using Ab3d.Common.EventManager3D;
using Ab3d.Common.Models;
using Ab3d.Controls;
using Ab3d.Meshes;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for Graph3D.xaml
    /// </summary>
    public partial class Graph3D : Page
    {
        private List<SphereDataView> _allPositionsData;

        private static Random _rnd = new Random();

        private bool _isSelecting;
        private bool _isSelectStarting;
        private Point _startSelectionPosition;

        private List<SphereData> _sampleData;

        private Size _locationSize = new Size(10, 10);
        private double _maxSize = 10;

        public Graph3D()
        {
            InitializeComponent();

            CameraControllerInfo.AddCustomInfoLine(0, Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed, "Click to select sphere\r\nDrag for rectangular section");

            _sampleData = InitializeData();
            ShowData(_sampleData);

            // Subscribe mouse events that will be used to create selection rectangle
            ViewportBorder.MouseLeftButtonDown += SelectionOverlayCanvasOnMouseLeftButtonDown;
            ViewportBorder.MouseMove += SelectionOverlayCanvasOnMouseMove;
            ViewportBorder.MouseLeftButtonUp += SelectionOverlayCanvasOnMouseLeftButtonUp;
        }

        public List<SphereData> InitializeData()
        {
            // originalData will hold our original random data
            var originalData = new List<SphereData>();

            // Start in the middle
            double x = _locationSize.Width / 2;
            double y = _locationSize.Height / 2;

            double randomizationFactor = 0.5; // How much can the data go up and down

            // Add some random data
            for (int i = 0; i < 20; i++)
            {
                // Get random offset
                double dx = _locationSize.Width * randomizationFactor * (_rnd.NextDouble() - 0.5);  //_rnd.NextDouble() * locationSize.Width * randomizationFactor - locationSize.Width * randomizationFactor * 0.5;
                double dy = _locationSize.Height * randomizationFactor * (_rnd.NextDouble() - 0.5); // _rnd.NextDouble() * locationSize.Height * randomizationFactor - locationSize.Height * randomizationFactor * 0.5;

                x += dx;
                y += dy;

                // Prevent going out of limits (0,0 ... locationSize)
                if (x < 0)
                    x = -x;
                else if (x > _locationSize.Width)
                    x = _locationSize.Width - 1 * (x - _locationSize.Width);

                if (y < 0)
                    y = -y;
                else if (y > _locationSize.Height)
                    y = _locationSize.Height - 1 * (y - _locationSize.Height);

                double power = _rnd.NextDouble() * _maxSize;

                var sphereData = new SphereData(i, new Point(x, y), power);
                originalData.Add(sphereData);

                //System.Diagnostics.Debug.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "originalData.Add(new SphereData({0}, new Point({1:0}, {2:0}), {3:0.00}));", i, x, y, power));
            }

            return originalData;
        }

        public void ShowData(List<SphereData> originalData)
        { 
            // Now use original data to create our data view objects

            // All data will be displayed in a 3D box defined below:
            var displayedDataBounds = new Rect3D(-40, -20, -40, 80, 40, 80);

            _allPositionsData = new List<SphereDataView>(originalData.Count);

            foreach (var originalSphereData in originalData)
            {
                var sphereDataView = SphereDataView.Create(originalSphereData, displayedDataBounds, originalData.Count, _locationSize, _maxSize);

                sphereDataView.IsSelectedChanged += delegate (object sender, EventArgs args)
                {
                    var changedPositionDataView = (SphereDataView)sender;
                    if (changedPositionDataView.IsSelected)
                        DataListBox.SelectedItems.Add(changedPositionDataView);
                    else
                        DataListBox.SelectedItems.Remove(changedPositionDataView);

                    UpdateSelectedSpheresData();
                };

                _allPositionsData.Add(sphereDataView);
            }


            // Setup axis limits and shown values
            AxisWireBox.MinYValue = 0;
            AxisWireBox.MaxYValue = 10;

            AxisWireBox.YAxisValues = new double[] { 0, 2, 4, 6, 8, 10 };


            // Bind positions data to ListBox
            DataListBox.ItemsSource = _allPositionsData;

            
            // Create curve through all positions
            // This is done by first creating all points that define the curve (10 points between each position that define the curve)
            List<Point3D> allPositions = new List<Point3D>();

            foreach (var positionData in _allPositionsData)
                allPositions.Add(positionData.Position);

            BezierCurve bezierCurve = Ab3d.Utilities.BezierCurve.CreateFromCurvePositions(allPositions);
            Point3DCollection curvePoints = bezierCurve.CreateBezierCurve(positionsPerSegment: 10); // How many points between each defined position in the curve

            // Create 3D Polyline from curvePoints
            Model3D curveModel = Ab3d.Models.Line3DFactory.CreatePolyLine3D(curvePoints, thickness: 2, color: Colors.Blue, isClosed: false, startLineCap: LineCap.Flat, endLineCap: LineCap.Flat, parentViewport3D: MainViewport);
            CurveModelVisual.Content = curveModel;


            // Now create 3D sphere objects for each position
            SpheresModelVisual.Children.Clear();

            // Each sphere will also need MouseEnter, MouseLeave and MouseClick event handlers
            // We use EventManager3D for that
            var eventManager3D = new Ab3d.Utilities.EventManager3D(MainViewport);


            // Add 3D sphere for each position
            foreach (var positionData in _allPositionsData)
            {
                SpheresModelVisual.Children.Add(positionData.ModelVisual3D); // Sphere Visual3D is created in SphereDataView object

                // Add event handlers (sphere Visual3D will be the source of the events)
                var visualEventSource3D = new VisualEventSource3D(positionData.ModelVisual3D);

                visualEventSource3D.MouseEnter += delegate (object sender, Mouse3DEventArgs e)
                {
                    if (_isSelecting) return;

                    // Use hand cursor
                    Mouse.OverrideCursor = Cursors.Hand;

                    // Find selected position data
                    var selectedPositionData = _allPositionsData.FirstOrDefault(p => p.ModelVisual3D == e.HitObject);

                    SelectData(selectedPositionData, e.CurrentMousePosition);
                };

                visualEventSource3D.MouseLeave += delegate (object sender, Mouse3DEventArgs e)
                {
                    if (_isSelecting) return;

                    Mouse.OverrideCursor = null;

                    DataToolTipBorder.Visibility = Visibility.Collapsed;
                    DataToolTipBorder.DataContext = null;

                    SelectedSphereLinesVisual.Children.Clear();
                };

                visualEventSource3D.MouseClick += delegate (object sender, MouseButton3DEventArgs e)
                {
                    // Select / deselect on mouse click
                    var clickedPositionData = _allPositionsData.FirstOrDefault(p => p.ModelVisual3D == e.HitObject);

                    if (clickedPositionData != null)
                        positionData.IsSelected = !clickedPositionData.IsSelected;
                };

                // Register the event source
                eventManager3D.RegisterEventSource3D(visualEventSource3D);
            }
        }

        public void SelectData(SphereDataView selectedPositionData, Point mousePosition)
        {
            // Show tooltip with details
            DataToolTipBorder.DataContext = selectedPositionData;

            Canvas.SetLeft(DataToolTipBorder, mousePosition.X + 10);
            Canvas.SetTop(DataToolTipBorder,  mousePosition.Y + 10);

            DataToolTipBorder.Visibility = Visibility.Visible;


            // Add additional 3D lines that will show where the sphere is in the 3D space
            double wireBoxBottom = AxisWireBox.CenterPosition.Y - AxisWireBox.Size.Y * 0.5;

            var verticalLineVisual3D = new Ab3d.Visuals.LineVisual3D()
            {
                StartPosition = selectedPositionData.Position,
                EndPosition = new Point3D(selectedPositionData.Position.X, wireBoxBottom, selectedPositionData.Position.Z),
                LineThickness = 2,
                LineColor = Colors.Gray
            };

            SelectedSphereLinesVisual.Children.Add(verticalLineVisual3D);

            double x1 = AxisWireBox.CenterPosition.X - AxisWireBox.Size.X * 0.5;
            double x2 = AxisWireBox.CenterPosition.X + AxisWireBox.Size.X * 0.5;

            var xLineVisual3D = new Ab3d.Visuals.LineVisual3D()
            {
                StartPosition = new Point3D(x1, wireBoxBottom, selectedPositionData.Position.Z),
                EndPosition = new Point3D(x2, wireBoxBottom, selectedPositionData.Position.Z),
                LineThickness = 2,
                LineColor = Colors.Gray
            };

            SelectedSphereLinesVisual.Children.Add(xLineVisual3D);

            double z1 = AxisWireBox.CenterPosition.Z - AxisWireBox.Size.Z * 0.5;
            double z2 = AxisWireBox.CenterPosition.Z + AxisWireBox.Size.Z * 0.5;

            var zLineVisual3D = new Ab3d.Visuals.LineVisual3D()
            {
                StartPosition = new Point3D(selectedPositionData.Position.X, wireBoxBottom, z1),
                EndPosition = new Point3D(selectedPositionData.Position.X, wireBoxBottom, z2),
                LineThickness = 2,
                LineColor = Colors.Gray
            };

            SelectedSphereLinesVisual.Children.Add(zLineVisual3D);
        }

        private void DataListBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // User clicked on item in ListBox
            foreach (var addedItem in e.AddedItems)
                ((SphereDataView) addedItem).IsSelected = true;

            foreach (var addedItem in e.RemovedItems)
                ((SphereDataView) addedItem).IsSelected = false;
        }

        private void RecreateDataButtonOnClick(object sender, RoutedEventArgs e)
        {
            _sampleData = InitializeData();
            ShowData(_sampleData);
        }

        private void UpdateSelectedSpheresData()
        {
            if (DataListBox.SelectedItems == null || DataListBox.SelectedItems.Count == 0)
            {
                SelectionDataTextBlock.Text = "no spheres selected";
                return;
            }

            double totalSize = DataListBox.SelectedItems.OfType<SphereDataView>().Sum(d => d.OriginalSphereData.Size);
            SelectionDataTextBlock.Text = string.Format("Count: {0}\r\nTotal size: {1:0.0}", DataListBox.SelectedItems.Count, totalSize);
        }

        #region Rectangular selection
        private void SelectionOverlayCanvasOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // We do not start selecting immediately when mouse is down 
            // but wait until mouse is moved for a few pixels.
            // This prevents interfering with handling mouse click events
            _isSelectStarting = true; 

            Point position = e.GetPosition(SelectionOverlayCanvas);
            _startSelectionPosition = position;

            // Calculate screen positions for all spheres so that we can do a quick check if a sphere is inside selection rectangle
            foreach (var positionData in _allPositionsData)
                positionData.ScreenPosition = Camera1.Point3DTo2D(positionData.Position);

            e.Handled = true;
        }
        
        private void SelectionOverlayCanvasOnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting && !_isSelectStarting)
            {
                e.Handled = false;
                return;
            }

            Point position = e.GetPosition(SelectionOverlayCanvas);

            double dx = position.X - _startSelectionPosition.X;
            double dy = position.Y - _startSelectionPosition.Y;

            double width = Math.Abs(dx);
            double height = Math.Abs(dy);

            if (_isSelectStarting)
            {
                if (width > 2 || height > 2)
                {
                    // User moved the mouse enought to start rectangular selection
                    _isSelectStarting = false;
                    _isSelecting = true;

                    SelectionRectangle.Visibility = Visibility.Visible;
                }
                else
                {
                    return;
                }
            }

            double x = _startSelectionPosition.X;
            double y = _startSelectionPosition.Y;

            if (dx < 0)
                x += dx;

            if (dy < 0)
                y += dy;

            // Update selection recrange
            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);

            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;


            Rect selectionRect = new Rect(x, y, width, height);

            // Select all spheres that are inside selection rectangle
            foreach (var positionData in _allPositionsData)
                positionData.IsSelected = positionData.IsInScreenRectangle(selectionRect);

            e.Handled = true;
        }

        private void SelectionOverlayCanvasOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isSelecting = false;
            _isSelectStarting = false;

            SelectionRectangle.Visibility = Visibility.Collapsed;

            e.Handled = true;
        }

        private void ClearSelectionButtonOnClick(object sender, RoutedEventArgs e)
        {
            foreach (var sphereDataView in _allPositionsData)
                sphereDataView.IsSelected = false;
        }
        #endregion
    }


    // SphereData contains raw data about each sampled data
    public class SphereData
    {
        public double Time { get; set; }    // Time will determine the x position in 3D space
        public Point Location { get; set; } // Location will determine the y and z position in 3D space

        public double Size { get; set; }    // Size will determine the radius of the sphere

        public SphereData(double time, Point location, double size)
        {
            Time = time;
            Location = location;
            Size = size;
        }
    }

    // SphereDataView is created from raw SphereData and contains additional properties and methods to show the data
    public class SphereDataView
    {
        private bool _isSelected;

        private SphereVisual3D _sphere;

        private DiffuseMaterial _selectedMaterial = new DiffuseMaterial(Brushes.Red);
        private static Color[] _gradientColorsArray;


        // Initial data:
        public SphereData OriginalSphereData { get; private set; }

        public Point3D Position { get; private set; }
        public Color Color { get; private set; }
        public double Radius { get; private set; }

        // View data:
        public Point ScreenPosition { get; set; }

        public ModelVisual3D ModelVisual3D
        {
            get
            {
                if (_sphere == null)
                    _sphere = CreateSphereVisual3D();

                return _sphere;
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;

                if (_sphere != null)
                    _sphere.Material = GetCurrentSphereMaterial();

                OnIsSelectedChanged();
            }
        }

        public event EventHandler IsSelectedChanged;


        public SphereDataView(SphereData originalSphereData, Point3D position, Color color, double radius)
        {
            OriginalSphereData = originalSphereData;

            Position = position;
            Color = color;
            Radius = radius;

            ScreenPosition = new Point(double.NaN, double.NaN);
        }

        public static SphereDataView Create(SphereData originalSphereData, Rect3D displayedDataBounds, double maxTime, Size locationBounds, double maxSize)
        {
            // First calculate normalized values (from 0 to 1)
            double x = originalSphereData.Time / maxTime;
            double y = originalSphereData.Location.Y / locationBounds.Height;
            double z = originalSphereData.Location.X / locationBounds.Width;


            // Set color of the sphere based on its y position
            // We choose the color from the gradient defined in EnsureGradientColorsArray
            EnsureGradientColorsArray();

            int colorArrayIndex = (int)(y * _gradientColorsArray.Length);
            Color color = _gradientColorsArray[colorArrayIndex];


            // Now put normalized values into a 3D space that is defined by displayedDataBounds
            x = (x * displayedDataBounds.SizeX) + displayedDataBounds.X;
            y = (y * displayedDataBounds.SizeY) + displayedDataBounds.Y;
            z = (z * displayedDataBounds.SizeZ) + displayedDataBounds.Z;

            Point3D position3D = new Point3D(x, y, z);


            // Use Size value to define the sphere radius in a range from 1 to 3
            double sphereRadius = (originalSphereData.Size / maxSize) * 2 + 1;


            // Now we have all the data to create the SphereDataView
            var sphereDataView = new SphereDataView(originalSphereData, position3D, color, sphereRadius);

            return sphereDataView;
        }


        public bool IsInScreenRectangle(Rect selectionRectangle)
        {
            if (double.IsNaN(ScreenPosition.X))
                return false;

            return selectionRectangle.X < ScreenPosition.X && ScreenPosition.X < (selectionRectangle.X + selectionRectangle.Width) &&
                   selectionRectangle.Y < ScreenPosition.Y && ScreenPosition.Y < (selectionRectangle.Y + selectionRectangle.Height);
        }


        private static void EnsureGradientColorsArray()
        {
            // We use HeightMapMesh3D.GetGradientColorsArray to create an array with color values created from the gradient. The array size is 30.
            var gradientStopCollection = new GradientStopCollection();
            gradientStopCollection.Add(new GradientStop(Colors.Yellow, 1));
            gradientStopCollection.Add(new GradientStop(Colors.DodgerBlue, 0));
            var linearGradientBrush = new LinearGradientBrush(gradientStopCollection, new Point(0, 0), new Point(0, 1));

            _gradientColorsArray = HeightMapMesh3D.GetGradientColorsArray(linearGradientBrush, 30);
        }

        private SphereVisual3D CreateSphereVisual3D()
        {
            var sphereVisual3D = new SphereVisual3D()
            {
                CenterPosition = Position,
                Material = GetCurrentSphereMaterial(),
                Radius = Radius
            };

            return sphereVisual3D;
        }

        private Material GetCurrentSphereMaterial()
        {
            return IsSelected ? _selectedMaterial : new DiffuseMaterial(new SolidColorBrush(Color));
        }

        protected void OnIsSelectedChanged()
        {
            if (IsSelectedChanged != null)
                IsSelectedChanged(this, null);
        }
    }
}
