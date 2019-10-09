using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using Ab3d.Meshes;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for ExtrudeCreator.xaml
    /// </summary>
    public partial class ExtrudeCreator : Page
    {
        private List<Point> _shapePositions;
        private bool _startNewPolygon;

        private bool _isExtrudedModelShown;
        private GeometryModel3D _shownModel;

        private DiffuseMaterial _transparentMaterial;
        private DiffuseMaterial _standardMaterial;
        

        public ExtrudeCreator()
        {
            InitializeComponent();

            _transparentMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(150, 255, 170, 0)));
            _standardMaterial = new DiffuseMaterial(Brushes.Orange);

            _shapePositions = new List<Point>();

            UpdateEnabledButtons();
        }

        private void ExtrudeButton_OnClick(object sender, RoutedEventArgs e)
        {
            CreateExtrudedModel();
        }

        public void CreateExtrudedModel()
        { 
            Vector3D extrudeVector, shapeYVector3D;

            bool isSuccess = GetExtrudeAndUpVectors(out extrudeVector, out shapeYVector3D);

            if (!isSuccess)
                return;

            bool isSmooth = IsSmoothCheckBox.IsChecked ?? false;

            Vector3D modelOffset = new Vector3D(0, 0, 0);

            // Adjust shape positions so that (0,0) is at the center of the area
            // We also invert y so that y increased upwards (as the blue arrows shows) and not downwards as in Canvas coordinate system
            var centeredShapePositions = InvertYAndCenterPoints(_shapePositions);

            try
            {
                MeshGeometry3D extrudedMesh = Mesh3DFactory.CreateExtrudedMeshGeometry(positions: centeredShapePositions, 
                                                                                       isSmooth: isSmooth,
                                                                                       modelOffset: modelOffset, 
                                                                                       extrudeVector: extrudeVector, 
                                                                                       shapeYVector: shapeYVector3D, 
                                                                                       textureCoordinatesGenerationType: ExtrudeTextureCoordinatesGenerationType.Cylindrical,
                                                                                       addBottomTriangles: AddBottomTrianglesCheckBox.IsChecked ?? false, 
                                                                                       addTopTriangles: AddTopTrianglesCheckBox.IsChecked ?? false);

                CreateGeometryModel(extrudedMesh);

                _isExtrudedModelShown = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error extruding shape:\r\n" + ex.Message);
            }
        }

        private void TriangulateButton_OnClick(object sender, RoutedEventArgs e)
        {
            List<int> triangleIndices;

            // We will manually create the triangle indices with Triangulator
            // This way we can catch the FormatException in case the _points are not correct (for example if lines intersect each other)
            // If we do not need to show the exception message we could simply call the CreateExtrudedMeshGeometry without triangleIndices as second parameter.
            // Than in case the _points were not correct we would simply get null back.


            // Triangulator takes a list of 2D points and connects them with creating triangles (it creates triangle indices that are declared as Int32Collection).

            // If we would only need triangle indices, we could also use static Triangulate method, but we need IsClockwise property later
            //triangleIndices = Ab3d.Utilities.Triangulator.Triangulate(_points);


            // Adjust shape positions so that (0,0) is at the center of the area
            // We also invert y so that y increased upwards (as the blue arrows shows) and not downwards as in Canvas coordinate system
            var centeredShapePositions = InvertYAndCenterPoints(_shapePositions);

            Ab3d.Utilities.Triangulator triangulator = new Ab3d.Utilities.Triangulator(centeredShapePositions);

            try
            {
                triangleIndices = triangulator.CreateTriangleIndices();
            }
            catch (FormatException ex)
            {
                // Usually thrown when the polygon lines intersect each other

                MessageBox.Show(ex.Message);
                return;
            }


            Vector3D extrudeVector, shapeYVector3D;

            bool isSuccess = GetExtrudeAndUpVectors(out extrudeVector, out shapeYVector3D);

            if (!isSuccess)
                return;

            // Get the 3D vector for the positions x axis direction (upVector3D is direction of y axis)
            Vector3D shapeXVector3D = Vector3D.CrossProduct(extrudeVector, shapeYVector3D);

            if (shapeXVector3D.LengthSquared < 0.001)
            {
                MessageBox.Show("Cannot extrude shape because extrudeVector and shapeYVector are parallel and not perpendicular.");
                return;
            }

            // Convert list of 2D positions into a list of 3D positions with specifying custom X axis and Y axis
            Point3DCollection positions = Convert2DTo3DPositions(centeredShapePositions, shapeXVector3D, shapeYVector3D);

            //for (int i = 0; i < _shapePositions.Count; i++)
            //    positions.Add(new Point3D(_shapePositions[i].X + xOffset, yOffset, _shapePositions[i].Y + zOffset));

            var mesh = new MeshGeometry3D();
            mesh.Positions = positions;
            mesh.TriangleIndices = new Int32Collection(triangleIndices);

            CreateGeometryModel(mesh);

            _isExtrudedModelShown = false;
        }

        // Convert list of 2D positions into a list of 3D positions with specifying custom X axis and Y axis
        private Point3DCollection Convert2DTo3DPositions(List<Point> shapePositions, Vector3D shapeXVector3D, Vector3D shapeYVector3D)
        {
            shapeXVector3D.Normalize();
            shapeYVector3D.Normalize();

            var positions3D = new Point3DCollection(shapePositions.Count);

            for (int i = 0; i < shapePositions.Count; i++)
            {
                double xShape = shapePositions[i].X;
                double yShape = shapePositions[i].Y;

                positions3D.Add(new Point3D(xShape * shapeXVector3D.X + yShape * shapeYVector3D.X,
                                            xShape * shapeXVector3D.Y + yShape * shapeYVector3D.Y,
                                            xShape * shapeXVector3D.Z + yShape * shapeYVector3D.Z));
            }

            return positions3D;
        }

        private void CreateGeometryModel(MeshGeometry3D meshGeometry3D)
        {
            _shownModel = new GeometryModel3D();
            //model.Material = new DiffuseMaterial(Brushes.Blue);
            //model.BackMaterial = new DiffuseMaterial(Brushes.Red); // We set BackMaterial so we see if the normals or model is wrong

            if (TransparentMaterialCheckBox.IsChecked ?? false)
                _shownModel.Material = _transparentMaterial;
            else
                _shownModel.Material = _standardMaterial;

            _shownModel.BackMaterial = _shownModel.Material;

            _shownModel.Geometry = meshGeometry3D;


            ObjectsGroup.Children.Clear();
            ObjectsGroup.Children.Add(_shownModel);

            if (AutoClearPathCheckBox.IsChecked ?? false)
                _startNewPolygon = true;
        }

        private void UserPathCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            UserInfoTextBlock.Visibility = Visibility.Collapsed;

            Point point = e.GetPosition(UserPathCanvas);

            if (_startNewPolygon)
            {
                _shapePositions.Clear();
                _startNewPolygon = false;
            }

            AddPosition(point);
        }

        public void AddPosition(Point point)
        {
            _shapePositions.Add(point);

            DrawOriginalPoints();

            UpdateEnabledButtons();
        }

        private void DrawOriginalPoints()
        {
            SourcePolyline.Points.Clear();

            AdditionalDetailsPanel.Children.Clear();

            for (int i = 0; i < _shapePositions.Count; i++)
            {
                SourcePolyline.Points.Add(_shapePositions[i]);

                TextBlock tb = new TextBlock();
                tb.FontSize = 9;
                tb.FontWeight = FontWeights.Bold;
                tb.Foreground = Brushes.Red;
                tb.Text = i.ToString();

                Canvas.SetLeft(tb, _shapePositions[i].X - 10);
                Canvas.SetTop(tb, _shapePositions[i].Y - 5);

                AdditionalDetailsPanel.Children.Add(tb);
            }

            if (_shapePositions.Count > 2)
                SourcePolyline.Points.Add(_shapePositions[0]);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _shapePositions.Clear();
            SourcePolyline.Points.Clear();
            AdditionalDetailsPanel.Children.Clear();

            UpdateEnabledButtons();

            _shownModel = null;
            _isExtrudedModelShown = false;
        }

        private void UpdateEnabledButtons()
        {
            var count = _shapePositions.Count;

            ClearButton.IsEnabled       = count > 0;
            TriangulateButton.IsEnabled = count >= 3;
            ExtrudeButton.IsEnabled     = count >=  3;
        }

        private List<Point> GetPointsFromPathGeometry(Geometry geometry, double tolerance, ToleranceType toleranceType)
        {
            List<Point> points;

            if (geometry == null)
                return null;

            PathGeometry flattenGeometry = geometry.GetFlattenedPathGeometry(tolerance, toleranceType);

            if (flattenGeometry == null || flattenGeometry.Figures == null || flattenGeometry.Figures.Count == 0)
                return null;

            points = new List<Point>();
            points.Add(flattenGeometry.Figures[0].StartPoint);

            // We need only the first Figure
            foreach (var oneSegment in flattenGeometry.Figures[0].Segments)
            {
                if (oneSegment is PolyLineSegment)
                    points.AddRange(((PolyLineSegment)oneSegment).Points);
                else if (oneSegment is LineSegment)
                    points.Add(((LineSegment)oneSegment).Point);
            }

            return points;
        }

        private List<Point> InvertYAndCenterPoints(List<Point> originalPoints)
        {
            int count = originalPoints.Count;
            var centeredPoints = new List<Point>(count);

            double actualWidth = UserPathCanvas.ActualWidth;
            double actualHeight = UserPathCanvas.ActualHeight;

            double dx = -actualWidth / 2;
            double dy = -actualHeight / 2;

            for (int i = 0; i < count; i++)
                centeredPoints.Add(new Point(originalPoints[i].X + dx, actualHeight - originalPoints[i].Y + dy));

            return centeredPoints;
        }


        private bool GetExtrudeAndUpVectors(out Vector3D extrudeVector3D, out Vector3D shapeYVector3D)
        {
            bool isSuccess = GetVector3D(ExtrudeVectorTextBox, out extrudeVector3D);
            isSuccess &= GetVector3D(ShapeYVectorTextBox, out shapeYVector3D);

            return isSuccess;
        }

        // Returns false if vector cannot be read
        private bool GetVector3D(TextBox textBox, out Vector3D vector)
        {
            bool isSuccess = false;

            if (textBox == null)
            {
                vector = new Vector3D();
                return false;
            }

            try
            {
                string[] vectorParts = textBox.Text.Split(' ');

                double x, y, z;

                if (vectorParts.Length == 3 &&
                    double.TryParse(vectorParts[0], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x) &&
                    double.TryParse(vectorParts[1], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y) &&
                    double.TryParse(vectorParts[2], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z))
                {
                    vector = new Vector3D(x, y, z);
                    isSuccess = true;
                }
                else
                {
                    vector = new Vector3D();
                }
            }
            catch
            {
                vector = new Vector3D();
            }

            if (isSuccess)
                textBox.Foreground = Brushes.Black;
            else
                textBox.Foreground = Brushes.Red;

            return isSuccess;
        }

        private void UserPathCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateShapeYVectorLine();
        }

        private void ExtrudeVectorTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExtrudeAndYVectorLines();
        }

        private void ShapeYVectorTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExtrudeAndYVectorLines();
        }

        private void UpdateShapeYVectorLine()
        {
            double actualWidth = UserPathCanvas.ActualWidth;
            double actualHeight = UserPathCanvas.ActualHeight;

            double centerX = actualWidth / 2;
            double centerY = actualHeight / 2;

            double topLineY = centerY / 2;

            ShapeYVectorLine.X1 = centerX; ShapeYVectorLine.Y1 = centerY;
            ShapeYVectorLine.X2 = centerX; ShapeYVectorLine.Y2 = topLineY;

            ShapeYVectorArrow1Line.X1 = centerX - 4; ShapeYVectorArrow1Line.Y1 = topLineY + 10;
            ShapeYVectorArrow1Line.X2 = centerX;     ShapeYVectorArrow1Line.Y2 = topLineY;

            ShapeYVectorArrow2Line.X1 = centerX + 4; ShapeYVectorArrow2Line.Y1 = topLineY + 10;
            ShapeYVectorArrow2Line.X2 = centerX;     ShapeYVectorArrow2Line.Y2 = topLineY;
        }

        private void UpdateExtrudeAndYVectorLines()
        {
            Vector3D extrudeVector, shapeYVector3D;

            bool isSuccess = GetExtrudeAndUpVectors(out extrudeVector, out shapeYVector3D);

            if (!isSuccess)
                return;

            ExtrudeVectorLine3D.EndPosition = ExtrudeVectorLine3D.StartPosition + extrudeVector;

            shapeYVector3D.Normalize();
            shapeYVector3D *= 100;

            ShapeYVectorLine3D.EndPosition = ShapeYVectorLine3D.StartPosition + shapeYVector3D;
        }

        private void OnShowWireframeCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            WireframeVisual3D.WireframeType = (ShowWireframeCheckBox.IsChecked ?? false) ? WireframeVisual3D.WireframeTypes.WireframeWithOriginalSolidModel : 
                                                                                           WireframeVisual3D.WireframeTypes.OriginalSolidModel;
        }

        private void OnTransparentMaterialCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_shownModel == null)
                return;

            if (TransparentMaterialCheckBox.IsChecked ?? false)
                _shownModel.Material = _transparentMaterial;
            else
                _shownModel.Material = _standardMaterial;
        }

        private void OnExtrudeSettingsCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_isExtrudedModelShown)
                CreateExtrudedModel();
        }
    }
}
