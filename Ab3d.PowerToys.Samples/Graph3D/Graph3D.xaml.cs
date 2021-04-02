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

namespace Ab3d.PowerToys.Samples.Graph3D
{
    /// <summary>
    /// Interaction logic for Graph3D.xaml
    /// </summary>
    public partial class Graph3D : Page
    {
        private List<SphereDataView> _allSpheresData;

        private bool _isSelecting;
        private bool _isSelectStarting;
        private Point _startSelectionPosition;

        private List<SphereData> _sampleData;
        private Rect _xyDataRange;

        private Color[] _gradientColorsArray;

        public Graph3D()
        {
            InitializeComponent();

            CameraControllerInfo.AddCustomInfoLine(0, Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed, "Click to select sphere\r\nDrag for rectangular section");


            var linearGradientBrush = CreateGradientBrush();
            _gradientColorsArray = HeightMapMesh3D.GetGradientColorsArray(linearGradientBrush, 30);

            LegendRectangle.Fill = linearGradientBrush;


            _xyDataRange = new Rect(-10, -10, 20, 20);
            _sampleData = GenerateRandomData(_xyDataRange, relativeMargin: 0.2, dataCount: 20);


            // Setup axis limits and shown values
            AxesBox.SetAxisDataRange(AxesBoxVisual3D.AxisTypes.XAxis, minimumValue: 0, maximumValue: _sampleData.Count, majorTicksStep: 2, minorTicksStep: 0, snapMaximumValueToMajorTicks: true);
            AxesBox.SetAxisDataRange(AxesBoxVisual3D.AxisTypes.YAxis, minimumValue: _xyDataRange.Y, maximumValue: _xyDataRange.Y + _xyDataRange.Height, majorTicksStep: 5, minorTicksStep: 2.5, snapMaximumValueToMajorTicks: true);
            AxesBox.SetAxisDataRange(AxesBoxVisual3D.AxisTypes.ZAxis, minimumValue: _xyDataRange.X, maximumValue: _xyDataRange.X + _xyDataRange.Width,  majorTicksStep: 5, minorTicksStep: 2.5, snapMaximumValueToMajorTicks: true);


            // All data will be displayed in a 3D box defined below:
            var displayedDataBounds = new Rect3D(AxesBox.CenterPosition.X - AxesBox.Size.X * 0.5,
                                                 AxesBox.CenterPosition.Y - AxesBox.Size.Y * 0.5,
                                                 AxesBox.CenterPosition.Z - AxesBox.Size.Z * 0.5,
                                                 AxesBox.Size.X,
                                                 AxesBox.Size.Y,
                                                 AxesBox.Size.Z);

            ShowData(_sampleData, displayedDataBounds, _xyDataRange);

            UpdateSelectedSpheresData();

            MinValueTextBlock.Text = string.Format("{0:0}", _xyDataRange.Y);
            MaxValueTextBlock.Text = string.Format("{0:0}", _xyDataRange.Y + _xyDataRange.Height);

            // Subscribe mouse events that will be used to create selection rectangle
            ViewportBorder.MouseLeftButtonDown += SelectionOverlayCanvasOnMouseLeftButtonDown;
            ViewportBorder.MouseMove           += SelectionOverlayCanvasOnMouseMove;
            ViewportBorder.MouseLeftButtonUp   += SelectionOverlayCanvasOnMouseLeftButtonUp;
        }

        public static List<SphereData> GenerateRandomData(Rect xyDataRange, double relativeMargin, int dataCount)
        {
            // originalData will hold our original random data
            var originalData = new List<SphereData>();

            // Start in the middle
            var minX = xyDataRange.X;
            var minY = xyDataRange.Y;
            var maxX = xyDataRange.X + xyDataRange.Width;
            var maxY = xyDataRange.Y + xyDataRange.Height;

            minX += relativeMargin * xyDataRange.Width * 0.5;
            minY += relativeMargin * xyDataRange.Height * 0.5;
            maxX -= relativeMargin * xyDataRange.Width * 0.5;
            maxY -= relativeMargin * xyDataRange.Height * 0.5;
            

            // Set the initial position at the center of the xyDataRange
            double x = xyDataRange.X + xyDataRange.Width * 0.5;
            double y = xyDataRange.Y + xyDataRange.Height * 0.5;

            double vx = 0; // velocity
            double vy = 0;

            var rnd = new Random();
            double randomizationFactor = 0.1; // How much data can adjust its position in one step (30%)
            
            // Add some random data - start at (x,y) and then move in random mostly positive direction
            for (int i = 0; i < dataCount; i++)
            {
                // Get random positive offset
                double dx = xyDataRange.Width  * randomizationFactor * (rnd.NextDouble() * 2 - 1); // random from -1 to +1
                double dy = xyDataRange.Height * randomizationFactor * (rnd.NextDouble() * 2 - 1);

                // Change velocity
                vx += dx;
                vy += dy;
                
                // change position based on current velocity
                x += vx;
                y += vy;


                // Prevent going out of limits
                if (x < minX)
                {
                    x = minX + (minX - x);
                    vx = -vx;
                }
                else if (x > maxX)
                {
                    x = maxX - (x - maxX);
                    vx = -vx;
                }

                if (y < minY)
                {
                    y = minY + (minY - y);
                    vy = -vy;
                }
                else if (y > maxY)
                {
                    y = maxY - (y - maxY);
                    vy = -vy;
                }


                // Just in case velocity is very high, we ensure that the values does not go out of bounds
                x = Math.Max(minX, Math.Min(maxX, x));
                y = Math.Max(minY, Math.Min(maxY, y));


                double sphereSize = rnd.NextDouble() * 3 + 1;

                var sphereData = new SphereData(time: i + 1, location: new Point(x, y), size: sphereSize);
                originalData.Add(sphereData);
            }

            return originalData;
        }

        private LinearGradientBrush CreateGradientBrush()
        {
            // We use HeightMapMesh3D.GetGradientColorsArray to create an array with color values created from the gradient. The array size is 30.
            var gradientStopCollection = new GradientStopCollection();
            gradientStopCollection.Add(new GradientStop(Colors.Red, 1));
            gradientStopCollection.Add(new GradientStop(Colors.Yellow, 0.5));
            gradientStopCollection.Add(new GradientStop(Colors.DodgerBlue, 0));
            var linearGradientBrush = new LinearGradientBrush(gradientStopCollection, new Point(0, 1), new Point(0, 0));

            return linearGradientBrush;
        }


        public void ShowData(List<SphereData> originalData, Rect3D displayedDataBounds, Rect xyDataRange)
        { 
            // Now use original data to create our data view objects

            // All data will be displayed in a 3D box defined below:
            //var displayedDataBounds = new Rect3D(AxesBox.CenterPosition.X - AxesBox.Size.X * 0.5,
            //                                     AxesBox.CenterPosition.Y - AxesBox.Size.Y * 0.5,
            //                                     AxesBox.CenterPosition.Z - AxesBox.Size.Z * 0.5,
            //                                     AxesBox.Size.X,
            //                                     AxesBox.Size.Y,
            //                                     AxesBox.Size.Z);

            _allSpheresData = new List<SphereDataView>(originalData.Count);

            foreach (var originalSphereData in originalData)
            {
                // Set color of the sphere based on its y position
                // We choose the color from the gradient defined in EnsureGradientColorsArray
                double relativeY = (originalSphereData.Location.Y - xyDataRange.Y) / xyDataRange.Height;

                int colorArrayIndex = (int)(relativeY * (_gradientColorsArray.Length - 1));
                Color color = _gradientColorsArray[colorArrayIndex];


                var sphereDataView = SphereDataView.Create(originalSphereData, displayedDataBounds, originalData.Count, xyDataRange, color);

                sphereDataView.IsSelectedChanged += delegate (object sender, EventArgs args)
                {
                    var changedPositionDataView = (SphereDataView)sender;
                    if (changedPositionDataView.IsSelected)
                        DataListBox.SelectedItems.Add(changedPositionDataView);
                    else
                        DataListBox.SelectedItems.Remove(changedPositionDataView);

                    UpdateSelectedSpheresData();
                };

                _allSpheresData.Add(sphereDataView);
            }

            // Bind positions data to ListBox
            DataListBox.ItemsSource = _allSpheresData;

            
            // Create curve through all positions
            // This is done by first creating all points that define the curve (10 points between each position that define the curve)
            List<Point3D> allPositions = new List<Point3D>();

            foreach (var positionData in _allSpheresData)
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
            foreach (var positionData in _allSpheresData)
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
                    var selectedPositionData = _allSpheresData.FirstOrDefault(p => p.ModelVisual3D == e.HitObject);

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
                    var clickedPositionData = _allSpheresData.FirstOrDefault(p => p.ModelVisual3D == e.HitObject);

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
            var centerPosition = AxesBox.CenterPosition;
            var size = AxesBox.Size;

            double wireBoxBottom = centerPosition.Y - size.Y * 0.5;

            var verticalLineVisual3D = new Ab3d.Visuals.LineVisual3D()
            {
                StartPosition = selectedPositionData.Position,
                EndPosition = new Point3D(selectedPositionData.Position.X, wireBoxBottom, selectedPositionData.Position.Z),
                LineThickness = 2,
                LineColor = Colors.Gray
            };

            SelectedSphereLinesVisual.Children.Add(verticalLineVisual3D);

            double x1 = centerPosition.X - size.X * 0.5;
            double x2 = centerPosition.X + size.X * 0.5;

            var xLineVisual3D = new Ab3d.Visuals.LineVisual3D()
            {
                StartPosition = new Point3D(x1, wireBoxBottom, selectedPositionData.Position.Z),
                EndPosition = new Point3D(x2, wireBoxBottom, selectedPositionData.Position.Z),
                LineThickness = 2,
                LineColor = Colors.Gray
            };

            SelectedSphereLinesVisual.Children.Add(xLineVisual3D);

            double z1 = centerPosition.Z - size.Z * 0.5;
            double z2 = centerPosition.Z + size.Z * 0.5;

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
            _sampleData = GenerateRandomData(_xyDataRange, relativeMargin: 0.2, dataCount: 20);

            // All data will be displayed in a 3D box defined below:
            var displayedDataBounds = new Rect3D(AxesBox.CenterPosition.X - AxesBox.Size.X * 0.5,
                                                 AxesBox.CenterPosition.Y - AxesBox.Size.Y * 0.5,
                                                 AxesBox.CenterPosition.Z - AxesBox.Size.Z * 0.5,
                                                 AxesBox.Size.X,
                                                 AxesBox.Size.Y,
                                                 AxesBox.Size.Z);

            ShowData(_sampleData, displayedDataBounds, _xyDataRange);
        }

        private void UpdateSelectedSpheresData()
        {
            if (DataListBox.SelectedItems == null || DataListBox.SelectedItems.Count == 0)
            {
                SelectionDataTextBlock.Text = "No spheres selected\r\n";
                ClearSelectionButton.IsEnabled = false;
                return;
            }

            double totalSize = DataListBox.SelectedItems.OfType<SphereDataView>().Sum(d => d.OriginalSphereData.Size);
            SelectionDataTextBlock.Text = string.Format("Count: {0}\r\nTotal size: {1:0.0}", DataListBox.SelectedItems.Count, totalSize);

            ClearSelectionButton.IsEnabled = true;
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
            foreach (var positionData in _allSpheresData)
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
            foreach (var positionData in _allSpheresData)
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
            foreach (var sphereDataView in _allSpheresData)
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

        public static SphereDataView Create(SphereData originalSphereData, Rect3D displayedDataBounds, double maxTime, Rect xyDataRange, Color color)
        {
            // First calculate normalized values (from 0 to 1)
            double x = originalSphereData.Time / maxTime;
            double y = (originalSphereData.Location.Y - xyDataRange.Y) / xyDataRange.Height;
            double z = (originalSphereData.Location.X - xyDataRange.X) / xyDataRange.Width;


            // Now put normalized values into a 3D space that is defined by displayedDataBounds
            x = (x * displayedDataBounds.SizeX) + displayedDataBounds.X;
            y = (y * displayedDataBounds.SizeY) + displayedDataBounds.Y;
            z = (z * displayedDataBounds.SizeZ) + displayedDataBounds.Z;

            Point3D position3D = new Point3D(x, y, z);

            // Now we have all the data to create the SphereDataView
            var sphereDataView = new SphereDataView(originalSphereData, position3D, color, originalSphereData.Size);

            return sphereDataView;
        }


        public bool IsInScreenRectangle(Rect selectionRectangle)
        {
            if (double.IsNaN(ScreenPosition.X))
                return false;

            return selectionRectangle.X < ScreenPosition.X && ScreenPosition.X < (selectionRectangle.X + selectionRectangle.Width) &&
                   selectionRectangle.Y < ScreenPosition.Y && ScreenPosition.Y < (selectionRectangle.Y + selectionRectangle.Height);
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
