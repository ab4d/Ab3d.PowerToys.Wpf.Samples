using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
using Ab3d.Cameras;
using Ab3d.Common.Cameras;
using Ab3d.Common.Models;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    // PERFORMANCE NOTE:
    // Rendering many 3D lines is very slow when using WPF 3D rendering engine.
    //
    // To render millions of 3D lines very efficiently, use Ab3d.DXEngine.
    // See the "Ab3d.DXEngine Hit Testing / 3D Lines selector" sample on how to use
    // DXLineSelectorData for Ab3d.DXEngine and how to efficiently create many 3D lines.

    /// <summary>
    /// Interaction logic for LinesSelector.xaml
    /// </summary>
    public partial class LinesSelector : Page
    {
        private static readonly bool AddTranslate = false;     // Set to true to test adding transformation to positions
        private static readonly float LinePositionsRange = 100; // defines the length of the generated lines


        private Random _rnd = new Random();

        private List<LineSelectorData> _allLineSelectorData;
        private List<LineSelectorData> _selectedLineSelectorData;

        private LineSelectorData _lastSelectedLineSelector;
        private Color _savedLineColor;

        private bool _isCameraChanged;

        private Ab3d.Visuals.SphereVisual3D _closestPositionSphereVisual3D;
        private Point _lastMousePosition;

        private double _maxSelectionDistance;

        private Stopwatch _stopwatch;


        public LinesSelector()
        {
            InitializeComponent();

            // LineSelectorData is a utility class that can be used to get the closest distance 
            // of a specified screen position to the 3D line.
            _allLineSelectorData = new List<LineSelectorData>();
            _selectedLineSelectorData = new List<LineSelectorData>();

            _stopwatch = new Stopwatch();

            CreateSampleLines();

            _isCameraChanged = true; // When true, the CalculateScreenSpacePositions method is called before calculating line distances

            Camera1.CameraChanged += delegate(object sender, CameraChangedRoutedEventArgs e)
            {
                _isCameraChanged = true; // This will call CalculateScreenSpacePositions
                UpdateClosestLine();
            };

            this.MouseMove += delegate(object sender, MouseEventArgs e)
            {
                _lastMousePosition = e.GetPosition(MainBorder);
                UpdateClosestLine();
            };

            Camera1.StartRotation(20, 0);
        }


        private void CreateSampleLines()
        {
            int simpleLinesCount;
            int polyLinesCount;
            int multiLinesCount;

            if (LinesCountComboBox.SelectedIndex == 0)
            {
                simpleLinesCount = 10;
                polyLinesCount   = 20;
                multiLinesCount  = 0;
            }
            else if (LinesCountComboBox.SelectedIndex == 1)
            {
                simpleLinesCount = 10;
                polyLinesCount   = 0;
                multiLinesCount  = 20;
            }
            else
            {
                simpleLinesCount = 0;
                multiLinesCount  = 0;

                var comboBoxItem1 = (ComboBoxItem)LinesCountComboBox.SelectedItem;
                var selectedText1 = (string)comboBoxItem1.Content;

                var selectedTextParts = selectedText1.Split(' ');

                polyLinesCount = Int32.Parse(selectedTextParts[0]);
            }


            var comboBoxItem2 = (ComboBoxItem)LinesSegmentsComboBox.SelectedItem;
            var selectedText2 = (string)comboBoxItem2.Content;

            int lineSegmentsCount = int.Parse(selectedText2);


            CreateSampleLines(simpleLinesCount, polyLinesCount, multiLinesCount, lineSegmentsCount);


            if (polyLinesCount >= 100 || lineSegmentsCount >= 100)
                DXEngineInfoTextBlock.Foreground = Brushes.Red;
            else
                DXEngineInfoTextBlock.ClearValue(ForegroundProperty);
        }

        private void CreateSampleLines(int simpleLinesCount, int polyLinesCount, int multiLinesCount, int lineSegmentsCount)
        {
            _allLineSelectorData.Clear();
            MainViewport.Children.Clear();
            _closestPositionSphereVisual3D = null;

            bool checkBoundingBox = CheckBoundingBoxCheckBox.IsChecked ?? false;


            for (int i = 0; i < simpleLinesCount; i++)
                AddRandomLineWithLineSelectorData(lineLength: 2, checkBoundingBox, isPolyLine: false);

            for (int i = 0; i < polyLinesCount; i++)
                AddRandomLineWithLineSelectorData(lineLength: lineSegmentsCount, checkBoundingBox, isPolyLine: true);

            for (int i = 0; i < multiLinesCount; i++)
                AddRandomLineWithLineSelectorData(lineLength: lineSegmentsCount, checkBoundingBox, isPolyLine: false);


            _isCameraChanged = true; // This will force calling CalculateScreenSpacePositions again
        }

        private void AddRandomLineWithLineSelectorData(int lineLength, bool checkBoundingBox, bool isPolyLine)
        {
            var lineColor = GetRandomColor();
            float lineThickness = (float)_rnd.NextDouble() * 5 + 1;

            var linePositions = CreateRandomPositions(lineLength);


            BaseLineVisual3D createdLineObject;

            if (lineLength == 2)
            {
                var lineVisual3D = new Ab3d.Visuals.LineVisual3D()
                {
                    StartPosition = linePositions[0],
                    EndPosition = linePositions[linePositions.Count - 1],
                    LineColor = lineColor,
                    LineThickness = lineThickness,
                };

                MainViewport.Children.Add(lineVisual3D);

                createdLineObject = lineVisual3D;
            }
            else
            {
                var positionsCollection = new Point3DCollection(linePositions.Count);
                for (int i = 0; i < linePositions.Count; i++)
                    positionsCollection.Add(linePositions[i]);

                BaseLineVisual3D lineVisual3D;
                if (isPolyLine)
                {
                    lineVisual3D = new Ab3d.Visuals.PolyLineVisual3D()
                    {
                        LineColor = lineColor,
                        LineThickness = lineThickness,
                        Positions = positionsCollection
                    };
                }
                else
                {
                    lineVisual3D = new Ab3d.Visuals.MultiLineVisual3D()
                    {
                        LineColor = lineColor,
                        LineThickness = lineThickness,
                        Positions = positionsCollection
                    };
                }

                MainViewport.Children.Add(lineVisual3D);

                createdLineObject = lineVisual3D;
            }


            // Create the LineSelectorData that will be used to get the distance of the mouse position from the lines.
            // There are multiple constructors available, the one with the best performance takes List<Point3D> with positions and Rect3D with bounding box.
            var lineSelectorData = new LineSelectorData(createdLineObject, Camera1, adjustLineDistanceWithLineThickness: true);

            lineSelectorData.CheckBoundingBox = checkBoundingBox;

            if (AddTranslate)
            {
                var translation = new TranslateTransform3D(0, 50, 0);
                createdLineObject.Transform = translation;
                lineSelectorData.PositionsTransform3D = translation;
            }

            _allLineSelectorData.Add(lineSelectorData);
        }
        

        private void UpdateClosestLine()
        {
            if (_allLineSelectorData == null || _allLineSelectorData.Count == 0)
                return;

            bool isMultiThreaded = MultiThreadedCheckBox.IsChecked ?? false;
            var orderByDistance = OrderByDistanceCheckBox.IsChecked ?? false;

            double calculateScreenSpacePositionsTime;
            double getClosestDistanceTime;

            _stopwatch.Restart();


            if (_isCameraChanged)
            {
                // Each time camera is changed, we need to call CalculateScreenSpacePositions method.
                // This will update the 2D screen positions of the 3D lines.

                // IMPORTANT:
                // Before calling CalculateScreenSpacePositions it is highly recommended to call Refresh method on the camera.
                Camera1.Refresh();

                if (isMultiThreaded)
                {
                    // This code demonstrates how to use call CalculateScreenSpacePositions from multiple threads.
                    // This significantly improves performance when many 3D lines are used (thousands).
                    //
                    // When calling CalculateScreenSpacePositions we need to prepare all the data
                    // from WPF properties before calling the method because those properties 
                    // are not accessible from the other thread.
                    // We need worldToViewportMatrix and
                    // the _lineSelectorData[i].Camera and _lineSelectorData[i].UsedLineThickness need to be set
                    // (in this sample they are set in the LineSelectorData constructor).

                    // You can calculate the worldToViewportMatrix manually by the following code:
                    //
                    // Matrix3D viewMatrix, projectionMatrix;
                    // Camera1.GetCameraMatrices(out viewMatrix, out projectionMatrix);
                    //
                    // var worldToViewportMatrix = viewMatrix * projectionMatrix * GetViewportMatrix(new Size(MainViewport.ActualWidth, MainViewport.ActualHeight));
                    //
                    // public static Matrix3D GetViewportMatrix(Size viewportSize)
                    // {
                    //     if (viewportSize.IsEmpty)
                    //         return Ab3d.Common.Constants.ZeroMatrix;

                    //     return new Matrix3D(viewportSize.Width / 2, 0, 0, 0,
                    //                         0, -viewportSize.Height / 2, 0, 0,
                    //                         0, 0, 1, 0,
                    //                         viewportSize.Width / 2,
                    //                         viewportSize.Height / 2, 0, 1);
                    // }

                    var worldToViewportMatrix = new Matrix3D();
                    bool isWorldToViewportMatrixValid = Camera1.GetWorldToViewportMatrix(ref worldToViewportMatrix, forceMatrixRefresh: false);

                    if (isWorldToViewportMatrixValid)
                    {
                        Parallel.For(0, _allLineSelectorData.Count, 
                                     i => _allLineSelectorData[i].CalculateScreenSpacePositions(ref worldToViewportMatrix, transform: null));
                    }
                }
                else
                {
                    for (var i = 0; i < _allLineSelectorData.Count; i++)
                        _allLineSelectorData[i].CalculateScreenSpacePositions(Camera1);
                }

                _isCameraChanged = false;

                calculateScreenSpacePositionsTime = _stopwatch.Elapsed.TotalMilliseconds;
                _stopwatch.Restart();
            }
            else
            {
                calculateScreenSpacePositionsTime = 0;
            }


            // Now we can call the GetClosestDistance method.
            // This method calculates the closest distance from the _lastMousePosition to the line that was used to create the LineSelectorData.
            // GetClosestDistance also sets the LastDistance, LastLinePositionIndex properties on the LineSelectorData.

            // IMPORTANT:
            // When CheckBoundingBox is true, then we must call GetClosestDistance that also gets the maxSelectionDistance as parameter.
            // In case CheckBoundingBox is true then GetClosestDistance the method returns float.MaxValue when the screenPosition is outside the bounding box.

            if (isMultiThreaded)
            {
                Parallel.For(0, _allLineSelectorData.Count, 
                             i => _allLineSelectorData[i].GetClosestDistance(_lastMousePosition, _maxSelectionDistance));
            }
            else
            {
                for (var i = 0; i < _allLineSelectorData.Count; i++)
                    _allLineSelectorData[i].GetClosestDistance(_lastMousePosition, _maxSelectionDistance);
            }


            // Get the lines that are within _maxSelectionDistance and add them to _selectedLineSelectorData
            // We are reusing _selectedLineSelectorData list to prevent new allocations on each call of UpdateClosestLine
            
            foreach (var lineSelectorData in _allLineSelectorData)
            {
                if (lineSelectorData.LastDistance <= _maxSelectionDistance)
                    _selectedLineSelectorData.Add(lineSelectorData);
            }


            LineSelectorData closestLineSelector = null;
            Point3D closestPositionOnLine = new Point3D();

            if (_selectedLineSelectorData.Count > 0)
            {
                double closestDistance = double.MaxValue;

                if (orderByDistance)
                {
                    // Order by camera distance (line that is closest to the camera is selected)
                    
                    foreach (var oneLineSelectorData in _selectedLineSelectorData)
                    {
                        var oneDistanceFromCamera = oneLineSelectorData.LastDistanceFromCamera;

                        if (oneDistanceFromCamera < closestDistance)
                        {
                            closestDistance = oneDistanceFromCamera;
                            closestLineSelector = oneLineSelectorData;
                            closestPositionOnLine = oneLineSelectorData.LastClosestPositionOnLine;
                        }
                    }
                }
                else
                {
                    // Order by distance to the specified position (line that is closes to the specified position is selected)

                    foreach (var lineSelectorData in _selectedLineSelectorData)
                    {
                        if (lineSelectorData.LastDistance < closestDistance)
                        {
                            closestDistance = lineSelectorData.LastDistance;
                            closestLineSelector = lineSelectorData;
                        }
                    }

                    if (closestLineSelector != null)
                        closestPositionOnLine = closestLineSelector.LastClosestPositionOnLine;
                }
            }

            getClosestDistanceTime = _stopwatch.Elapsed.TotalMilliseconds;


            // The closest position on the line is shown with a SphereVisual3D
            if (_closestPositionSphereVisual3D == null)
            {
                _closestPositionSphereVisual3D = new SphereVisual3D()
                {
                    Radius = 2,
                    Material = new DiffuseMaterial(Brushes.Red)
                };

                MainViewport.Children.Add(_closestPositionSphereVisual3D);
            }


            string newClosestDistanceText;
            string newLineSegmentIndexText;
            if (closestLineSelector == null)
            {
                newClosestDistanceText = "";
                newLineSegmentIndexText = "";
                _closestPositionSphereVisual3D.IsVisible = false;
            }
            else
            {
                newClosestDistanceText = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0}", closestLineSelector.LastDistance);
                newLineSegmentIndexText = closestLineSelector.LastLinePositionIndex.ToString();

                _closestPositionSphereVisual3D.CenterPosition = closestPositionOnLine;
                _closestPositionSphereVisual3D.IsVisible = true;
            }
            
            if (ClosestDistanceValue.Text != newClosestDistanceText)
                ClosestDistanceValue.Text = newClosestDistanceText;
            
            if (LineSegmentIndexValue.Text != newLineSegmentIndexText)
                LineSegmentIndexValue.Text = newLineSegmentIndexText;

            string newUpdateTimeText = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.##} + {1:0.##}", calculateScreenSpacePositionsTime, getClosestDistanceTime);

            if (UpdateTimeValue.Text != newUpdateTimeText)
                UpdateTimeValue.Text = newUpdateTimeText;


            // Show the closest line as red
            if (!ReferenceEquals(_lastSelectedLineSelector, closestLineSelector))
            {
                if (_lastSelectedLineSelector != null)
                    _lastSelectedLineSelector.LineVisual3D.LineColor = _savedLineColor;

                if (closestLineSelector != null)
                {
                    _savedLineColor = closestLineSelector.LineVisual3D.LineColor;
                    closestLineSelector.LineVisual3D.LineColor = Colors.Orange;
                }

                _lastSelectedLineSelector = closestLineSelector;
            }


            // Clear _selectedLineSelectorData so it can be used in next UpdateClosestLine
            _selectedLineSelectorData.Clear();
        }

        private Point3DCollection CreateRandomPositions(int pointsCount)
        {
            var positions = new Point3DCollection(pointsCount);

            var onePosition = new Point3D(_rnd.NextDouble() * LinePositionsRange - LinePositionsRange * 0.5f,
                                          _rnd.NextDouble() * LinePositionsRange - LinePositionsRange * 0.5f,
                                          _rnd.NextDouble() * LinePositionsRange - LinePositionsRange * 0.5f);


            // direction in range from -1 ... +1
            var lineDirection = new Vector3D(_rnd.NextDouble() * 2.0 - 1.0,
                                             _rnd.NextDouble() * 1.0 - 0.5,
                                             _rnd.NextDouble() * 2.0 - 1.0);

            var lineRightDirection = new Vector3D(lineDirection.Z, lineDirection.Y, lineDirection.X); // switch X and Z to get vector to the right of lineDirection
            var lineUpDirection = new Vector3D(0, 1, 0);

            var positionAdvancement = LinePositionsRange / pointsCount;
            var displacementRange = Math.Max(0.1, LinePositionsRange / pointsCount);

            for (int i = 0; i < pointsCount; i++)
            {
                var vector = lineDirection * positionAdvancement;
                vector += lineUpDirection * displacementRange * (_rnd.NextDouble() * 2.0 - 1.0);
                vector += lineRightDirection * displacementRange * (_rnd.NextDouble() * 2.0 - 1.0);

                onePosition += vector;

                positions.Add(onePosition);
            }

            return positions;
        }

        private Color GetRandomColor()
        {
            byte amount = (byte) (_rnd.Next(200));

            return Color.FromArgb(255, amount, amount, amount);
            //return Color.FromArgb(255, (byte)_rnd.Next(255), (byte)_rnd.Next(255), (byte)_rnd.Next(255));
        }

        private void UpdateMaxDistanceText()
        {
            if (_maxSelectionDistance < 0)
                MaxDistanceValue.Text = "unlimited";
            else
                MaxDistanceValue.Text = string.Format("{0:0}", _maxSelectionDistance);
        }

        private void MaxDistanceSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _maxSelectionDistance = MaxDistanceSlider.Value;

            if (_maxSelectionDistance > 20)
                _maxSelectionDistance = -1;

            UpdateMaxDistanceText();
        }

        private void OnCheckBoundingBoxCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded || _allLineSelectorData == null)
                return;

            var isChecked = CheckBoundingBoxCheckBox.IsChecked ?? false;

            foreach (var dxLineSelectorData in _allLineSelectorData)
                dxLineSelectorData.CheckBoundingBox = isChecked;

            _isCameraChanged = true; // this will force calling CalculateScreenSpacePositions again
        }

        private void LinesCountComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            CreateSampleLines();
        }

        private void LinesSegmentsComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            CreateSampleLines();
        }

        private void CameraRotationButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Camera1.IsRotating)
            {
                Camera1.StopRotation();
                CameraRotationButton.Content = "Start camera rotation";
            }
            else
            {
                Camera1.StartRotation(20, 0);
                CameraRotationButton.Content = "Stop camera rotation";
            }
        }
    }
}
