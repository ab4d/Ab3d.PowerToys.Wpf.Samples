using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using Ab3d.Common;
using Ab3d.Meshes;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for TriangulatorWithHolesSample.xaml
    /// </summary>
    public partial class TriangulatorWithHolesSample : Page
    {
        private TypeConverter _geometryConverter;

        private PointCollection _triangulatedPositions;
        private List<int> _triangulatedIndices;

        private Color[] _randomColors;

        public TriangulatorWithHolesSample()
        {
            InitializeComponent();

            CreateTextScene(preserveCamera: false);
        }

        private void CreateSimplePolygonsScene(bool preserveCamera)
        {
            // Define an outer polygon and two holes
            // We use Y-down 2D coordinate system.
            // Outer polygons needs to be defined in clockwise direction, holes in anti-clockwise direction
            var outerPositions = new PointCollection()
            {
                new Point(0,0),
                new Point(100,0),
                new Point(100,100),
                new Point(0,100),
            };

            // 2 holes (anti-clockwise direction):
            var holePositions1 = new PointCollection()
            {
                new Point(30,40),
                new Point(30,50),
                new Point(50,50),
                new Point(50,40),
            };

            // NOTE: This hole is organized in clockwise direction - the AddHole method will return false and we will need to reverse the direction
            var holePositions2 = new PointCollection()
            {
                new Point(80,20),
                new Point(80,60),
                new Point(60,60),
                new Point(60,20),
            };

            // We could create an array of PointCollection and set that in a Triangulator constructor.
            // Triangulator would automatically determine which polygons defined outer shapes and which define holes.
            //var allPolygons = new PointCollection[] { outerPositions, holePositions1, holePositions2 };
            //var triangulator = new Triangulator(allPolygons, isYAxisUp: false);

            // But instead here we demonstrate the usage of AddHole method.
            // In this case we first create Triangulator by providing an outer polygon and then call AddHole methods to add holes.
            // In this case Triangulator reverses the order outer polygon and holes if they are not correctly oriented (outer polygon should be clockwise and holes should be anti-clockwise).
            var triangulator = new Triangulator(outerPositions, isYAxisUp: false);

            // AddHole method returns a Boolean that specifies if the order of positions was correct (order need to be counter-clockwise).
            // In this case we need to reverse the orientation of the positions in the hole
            bool isCorrectOrientation = triangulator.AddHole(holePositions1);
            if (!isCorrectOrientation) ReversePointCollection(holePositions1);

            isCorrectOrientation = triangulator.AddHole(holePositions2);
            if (!isCorrectOrientation) ReversePointCollection(holePositions2);

            // Triangulate by creating triangleIndices
            List<int> triangleIndices;
            PointCollection triangulatedPositions;
            triangulator.Triangulate(out triangulatedPositions, out triangleIndices);

            var allPolygons = new PointCollection[] { outerPositions, holePositions1, holePositions2 };

            // If points in polygon are counter-clockwise oriented we need to flip normals (change order of triangle indices)
            // to make the triangles correctly oriented - so the normals are pointing out of the object
            var flipNormals = !triangulator.IsClockwise;

            ShowTriangulated3DMesh(triangulatedPositions, triangleIndices, allPolygons, triangulator.IsYAxisUp, flipNormals, preserveCamera);

            InitTriangulatedGeometryShape(triangulatedPositions, triangleIndices);
        }

        private void CreateTextScene(bool preserveCamera)
        {
            PointCollection[] allPolygons;

            switch (TextComboBox.SelectedIndex)
            {
                case 0:  // ASCII chars from 32 - 128
                    var allChars1Text = CreateAllCharsText(from: 32, to: 128, lineLength: 16);
                    allPolygons = CreateText(allChars1Text);
                    break;
                
                case 1:  // ASCII chars from 128 - 255
                    var allChars2Text = CreateAllCharsText(from: 128, to: 255, lineLength: 16);
                    allPolygons = CreateText(allChars2Text);
                    break;
                
                case 2:  // AB4D
                    allPolygons = CreateText("AB4D");
                    break;
                
                case 3:  // Symbol chars
                    allPolygons = CreateText("⇦⇧⇨⇩↯");
                    break;
                
                case 4:  // Chinese chars
                    allPolygons = CreateText("太極拳");
                    break;

                default:
                    allPolygons = null;
                    break;
            }

            if (allPolygons == null)
                return;

            var triangulator = new Triangulator(allPolygons, isYAxisUp: false);

            List<int> triangleIndices;
            PointCollection triangulatedPositions;
            triangulator.Triangulate(out triangulatedPositions, out triangleIndices);

            ShowTriangulated3DMesh(triangulatedPositions, triangleIndices, allPolygons, triangulator.IsYAxisUp, triangulator.IsPolygonPositionsOrderReversed, preserveCamera);

            InitTriangulatedGeometryShape(triangulatedPositions, triangleIndices);
        }

        private void CreateShapesScene(bool preserveCamera)
        {
            var ellipseGeometry = new EllipseGeometry(new Point(100, 100), 100, 80);

            var outerPolygon = GetPolygonFromGeometry(ellipseGeometry);


            // See: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/path-markup-syntax?view=netframeworkdesktop-4.8
            
            // We can define path geometry from text
            if (_geometryConverter == null)
                _geometryConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Geometry));
            
            var pathText = "M40,120 L160,120 C160,150 120,150 100,150 C40,150 40,150 40,120z";
            var pathGeometry1 = _geometryConverter.ConvertFromInvariantString(pathText) as StreamGeometry;

            var hole1Polygon = GetPolygonFromGeometry(pathGeometry1);


            // or from explicitly adding path segments:
            var pathSegments = new List<PathSegment>();
            pathSegments.Add(new LineSegment(point: new Point(150, 40), isStroked: false));
            pathSegments.Add(new LineSegment(point: new Point(150, 100), isStroked: false));
            pathSegments.Add(new BezierSegment(point1: new Point(120, 100), point2: new Point(120, 100), isStroked: false, point3: new Point(120, 60)));

            var pathFigure = new PathFigure(start: new Point(120, 60), segments: pathSegments, closed: true);

            var pathGeometry2 = new PathGeometry();
            pathGeometry2.Figures.Add(pathFigure);

            var hole2Polygon = GetPolygonFromGeometry(pathGeometry2);


            var rectangleGeometry = new RectangleGeometry(new Rect(new Point(50, 50), new Point(80, 100)), radiusX: 5, radiusY: 5);
            var hole3Polygon = GetPolygonFromGeometry(rectangleGeometry);


            // NOTE:
            // You can combine multiple geometries by using CombinedGeometry. This is not demonstrated here.


            var triangulator = new Triangulator(outerPolygon, isYAxisUp: false);

            bool isCorrectOrientation = triangulator.AddHole(hole1Polygon);
            if (!isCorrectOrientation) ReversePointCollection(hole1Polygon);

            isCorrectOrientation = triangulator.AddHole(hole2Polygon);
            if (!isCorrectOrientation) ReversePointCollection(hole2Polygon);

            isCorrectOrientation = triangulator.AddHole(hole3Polygon);
            if (!isCorrectOrientation) ReversePointCollection(hole3Polygon);

            List<int> triangleIndices;
            PointCollection triangulatedPositions;
            triangulator.Triangulate(out triangulatedPositions, out triangleIndices);

            var allPolygons = new PointCollection[] { outerPolygon, hole1Polygon, hole2Polygon, hole3Polygon };


            // If points in polygon are counter-clockwise oriented we need to flip normals (change order of triangle indices)
            // to make the triangles correctly oriented - so the normals are pointing out of the object
            var flipNormals = !triangulator.IsClockwise;

            ShowTriangulated3DMesh(triangulatedPositions, triangleIndices, allPolygons, triangulator.IsYAxisUp, flipNormals, preserveCamera);

            InitTriangulatedGeometryShape(triangulatedPositions, triangleIndices);
        }

        private PointCollection[] CreateText(string text)
        {
            var formattedText = new FormattedText(text,
                                                  CultureInfo.CurrentCulture,
                                                  FlowDirection.LeftToRight,
                                                  new Typeface("Arial Black"),
                                                  pixelsPerDip: 1,
                                                  foreground: Brushes.Black,
                                                  emSize: 100);

            var wpfGeometry = formattedText.BuildGeometry(new System.Windows.Point(0, 0));

            var allPolygons = GetAllPolygonsFromGeometry(wpfGeometry);

            return allPolygons;
        }

        private PointCollection GetPolygonFromGeometry(Geometry wpfGeometry)
        {
            var comboBoxItem = (ComboBoxItem)FlatteningToleranceComboBox.SelectedItem;
            double flatteningTolerance = double.Parse((string)comboBoxItem.Content, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

            var flattenedPathGeometry = wpfGeometry.GetFlattenedPathGeometry(flatteningTolerance, ToleranceType.Relative);

            if (flattenedPathGeometry.Figures.Count == 0)
                return new PointCollection();

            // get only the first Figure
            var oneFigurePolygon = GetPolylinePoints(flattenedPathGeometry.Figures[0]);

            return oneFigurePolygon;
        }
        
        private PointCollection[] GetAllPolygonsFromGeometry(Geometry wpfGeometry)
        {
            var comboBoxItem = (ComboBoxItem)FlatteningToleranceComboBox.SelectedItem;
            double flatteningTolerance = double.Parse((string)comboBoxItem.Content, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

            var flattenedPathGeometry = wpfGeometry.GetFlattenedPathGeometry(flatteningTolerance, ToleranceType.Relative);

            var allPolygons = new PointCollection[flattenedPathGeometry.Figures.Count];

            for (var i = 0; i < flattenedPathGeometry.Figures.Count; i++)
            {
                var figure = flattenedPathGeometry.Figures[i];
                var oneFigurePolygon = GetPolylinePoints(figure);

                if (oneFigurePolygon.Count >= 3) // each polygon should have at least 3 positions
                    allPolygons[i] = oneFigurePolygon;
            }

            return allPolygons;
        }

        private static PointCollection GetPolylinePoints(PathFigure figure)
        {
            // First count all positions so we can correctly pre-alloacte the PointCollection
            int pointsCount = 1; // Start with StartPoint

            for (var i = 0; i < figure.Segments.Count; i++)
            {
                var polyLineSegment = figure.Segments[i] as PolyLineSegment;
                if (polyLineSegment != null)
                    pointsCount += polyLineSegment.Points.Count; 
            }


            var points = new PointCollection(pointsCount);

            points.Add(figure.StartPoint);

            for (var i = 0; i < figure.Segments.Count; i++)
            {
                var polyLineSegment = figure.Segments[i] as PolyLineSegment;
                if (polyLineSegment != null)
                {
                    var polylinePoints = polyLineSegment.Points;
                    var polylinePointCount = polylinePoints.Count;
                    for (int j = 0; j < polylinePointCount; j++)
                        points.Add(polylinePoints[j]);
                }
            }

            return points;
        }

        private void ShowTriangulated3DMesh(PointCollection triangulatedPositions, List<int> triangulatedIndices, PointCollection[] allPolygons, bool isYAxisUp, bool flipNormals, bool preserveCamera)
        {
            MainViewport.Children.Clear();

            var comboBoxItem = (ComboBoxItem)ExtrudeDistanceComboBox.SelectedItem;
            double extrudeDistance = double.Parse((string)comboBoxItem.Content, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

            if (extrudeDistance == 0)
            {
                // Mesh3DFactory.CreateExtrudedMeshGeometry cannot create a mesh with zero extrude distance.
                // In this case we create a simple mesh with using the triangulatedPositions and triangulatedIndices
                ShowTriangulated3DPolygon(triangulatedPositions, triangulatedIndices, preserveCamera);
                return;
            }

            var meshGeometry3D = Ab3d.Meshes.Mesh3DFactory.CreateExtrudedMeshGeometry(triangulatedPositions,
                                                                                      triangulatedIndices,
                                                                                      allPolygons,
                                                                                      isSmooth: false,
                                                                                      isYAxisUp: isYAxisUp,
                                                                                      flipNormals: flipNormals,
                                                                                      modelOffset: new Vector3D(0, 0, 0),
                                                                                      extrudeVector: new Vector3D(0, 0, extrudeDistance),
                                                                                      meshXVector: new Vector3D(1, 0, 0), 
                                                                                      meshYVector: new Vector3D(0, 1, 0), 
                                                                                      textureCoordinatesGenerationType: ExtrudeTextureCoordinatesGenerationType.None);


            GeometryModel3D model = new GeometryModel3D();
            model.Material = new DiffuseMaterial(Brushes.Orange);
            model.BackMaterial = new DiffuseMaterial(Brushes.Red); // We set BackMaterial so we see if the normals or model is wrong
            model.Geometry = meshGeometry3D;

            MainViewport.Children.Add(model.CreateModelVisual3D());

            if (!preserveCamera)
            {
                Camera1.TargetPosition = model.Bounds.GetCenterPosition();
                Camera1.Distance = model.Bounds.GetDiagonalLength() * 2;
            }
        }

        private void ShowTriangulated3DPolygon(IList<Point> trianglePositions, List<int> triangleIndices, bool preserveCamera)
        {
            // First convert 2D positions into 3D positions:
            Point3DCollection positions = Convert2DTo3DPositions(trianglePositions,
                                                                 shapeXVector3D: new Vector3D(1, 0, 0),
                                                                 shapeYVector3D: new Vector3D(0, 1, 0)); // In 2D the y axis is down, but we used FlipYAxis in Triangulator

            var meshGeometry3D = new MeshGeometry3D();
            meshGeometry3D.Positions = positions;
            meshGeometry3D.TriangleIndices = new Int32Collection(triangleIndices);

            GeometryModel3D model = new GeometryModel3D();
            model.Material = new DiffuseMaterial(Brushes.Orange);
            model.BackMaterial = new DiffuseMaterial(Brushes.Red);
            model.Geometry = meshGeometry3D;

            MainViewport.Children.Add(model.CreateModelVisual3D());

            if (!preserveCamera)
            {
                Camera1.TargetPosition = model.Bounds.GetCenterPosition();
                Camera1.Distance = model.Bounds.GetDiagonalLength() * 2;
            }
        }


        private void InitTriangulatedGeometryShape(PointCollection triangulatedPositions, List<int> triangulatedIndices)
        {
            int trianglesCount = triangulatedIndices.Count / 3;
            EndTriangleIndexSlider.Maximum = trianglesCount;
            EndTriangleIndexSlider.Value = trianglesCount;

            _triangulatedPositions = triangulatedPositions;
            _triangulatedIndices = triangulatedIndices;

            UpdateTriangulatedGeometryShape();
        }

        private void UpdateTriangulatedGeometryShape()
        {
            if (_triangulatedPositions == null)
            {
                TrianglesCanvas.Children.Clear();
                return;
            }

            ShowTriangulatedGeometryShape(_triangulatedPositions, _triangulatedIndices, 1, new Point(0, 0), flipYAxis: true);
        }

        private void ShowTriangulatedGeometryShape(PointCollection trianglePositions, List<int> triangleIndices, double scale, Point offset, bool flipYAxis)
        {
            int trianglesCount = triangleIndices.Count / 3;

            InitRandomColors(trianglesCount);

            int lastShownTriangle = (int)EndTriangleIndexSlider.Value;
            lastShownTriangle = Math.Min(lastShownTriangle, trianglesCount); // just in case clamp to trianglesCount

            TrianglesRangeTextBlock.Text = string.Format("Show triangles from 1 to {0}; all triangles count: {1}", lastShownTriangle, trianglesCount);

            TrianglesCanvas.Children.Clear();

            if (lastShownTriangle == 0)
                return;


            double maxY;
            if (flipYAxis)
                maxY = trianglePositions.Max(p => p.Y); // uh, using linq is not the very fast but will do for this sample 
            else
                maxY = 0; // This will not be needed

            var bounds = Rect.Empty;

            for (int triangleIndex = 0; triangleIndex < lastShownTriangle; triangleIndex ++)
            {
                int index = triangleIndex * 3;

                int i1 = triangleIndices[index];
                int i2 = triangleIndices[index + 1];
                int i3 = triangleIndices[index + 2];

                var p1 = trianglePositions[i1];
                var p2 = trianglePositions[i2];
                var p3 = trianglePositions[i3];

                if (flipYAxis)
                {
                    p1 = new Point(p1.X, maxY - p1.Y);
                    p2 = new Point(p2.X, maxY - p2.Y);
                    p3 = new Point(p3.X, maxY - p3.Y);
                }

                var pointCollection = new PointCollection(3);
                pointCollection.Add(new System.Windows.Point((double)p1.X * scale + offset.X, (double)p1.Y * scale + offset.Y));
                pointCollection.Add(new System.Windows.Point((double)p2.X * scale + offset.X, (double)p2.Y * scale + offset.Y));
                pointCollection.Add(new System.Windows.Point((double)p3.X * scale + offset.X, (double)p3.Y * scale + offset.Y));

                bounds.Union(p1);
                bounds.Union(p2);
                bounds.Union(p3);

                var polygon = new Polygon()
                {
                    Points = pointCollection,
                    Fill = GetRandomBrush(triangleIndex),
                    //StrokeThickness = 1,
                    //Stroke = Brushes.Black
                };

                TrianglesCanvas.Children.Add(polygon);
            }

            TrianglesCanvas.Width = bounds.Right;
            TrianglesCanvas.Height = bounds.Bottom;
        }
        
        private void InitRandomColors(int colorsCount)
        {
            if (_randomColors != null && _randomColors.Length >= colorsCount) // reuse the existing random colors
                return;

            var rnd = new Random();

            _randomColors = new Color[colorsCount];
            for (int i = 0; i < colorsCount; i++)
                _randomColors[i] = System.Windows.Media.Color.FromRgb((byte)rnd.Next(255), (byte)rnd.Next(255), (byte)rnd.Next(255));
        }

        private System.Windows.Media.Brush GetRandomBrush(int index)
        {
            if (_randomColors == null || _randomColors.Length <= index)
                InitRandomColors(index);

            var color = _randomColors[index];
            return new System.Windows.Media.SolidColorBrush(color);
        }

        public static void ReversePointCollection(PointCollection pointCollection)
        {
            int index1 = 0;
            int index2 = pointCollection.Count - 1;

            while (index1 < index2)
            {
                Point temp = pointCollection[index1];
                pointCollection[index1] = pointCollection[index2];
                pointCollection[index2] = temp;

                index1++;
                index2--;
            }
        }

        // Convert list of 2D positions into a list of 3D positions with specifying custom X axis and Y axis
        private Point3DCollection Convert2DTo3DPositions(IList<Point> trianglePositions, Vector3D shapeXVector3D, Vector3D shapeYVector3D)
        {
            shapeXVector3D.Normalize();
            shapeYVector3D.Normalize();

            var positions3D = new Point3DCollection(trianglePositions.Count);

            for (int i = 0; i < trianglePositions.Count; i++)
            {
                double xShape = trianglePositions[i].X;
                double yShape = trianglePositions[i].Y;

                positions3D.Add(new Point3D(xShape * shapeXVector3D.X + yShape * shapeYVector3D.X,
                                            xShape * shapeXVector3D.Y + yShape * shapeYVector3D.Y,
                                            xShape * shapeXVector3D.Z + yShape * shapeYVector3D.Z));
            }

            return positions3D;
        }

        private string CreateAllCharsText(int from = 32, int to = 128, int lineLength = 16)
        {
            string text = "";
            for (int i = from; i <= to; i++)
            {
                text += (char)i;
                if (i > from && (i % lineLength) == 0)
                    text += "\r\n";
            }

            return text;
        }
        
        private void OnPolygonTypesChanged(object sender, RoutedEventArgs e)
        {
            RecreateScene(preserveCamera: false);
        }

        private void TextComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecreateScene(preserveCamera: false);
        }

        private void FlatteningToleranceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecreateScene(preserveCamera: true);
        }

        private void ExtrudeDistanceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecreateScene(preserveCamera: true);
        }

        private void EndTriangleIndexSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            UpdateTriangulatedGeometryShape();
        }

        private void RecreateScene(bool preserveCamera)
        {
            if (!this.IsLoaded)
                return;

            if (SimplePolygonsRadioButton.IsChecked ?? false)
                CreateSimplePolygonsScene(preserveCamera);
            else if (TextRadioButton.IsChecked ?? false)
                CreateTextScene(preserveCamera);
            else if (ShapesRadioButton.IsChecked ?? false)
                CreateShapesScene(preserveCamera);
        }
    }
}
