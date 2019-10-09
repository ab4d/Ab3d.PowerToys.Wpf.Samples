using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using Ab3d.Cameras;
using Ab3d.Common.Cameras;
using Ab3d.Controls;
using Ab3d.Meshes;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for LatheCreator.xaml
    /// </summary>
    public partial class LatheCreator : Page
    {
        private List<LatheSection> _sections;

        private GeometryModel3D _latheModel3D;

        private Line _firstLine;
        private Line _lastLine;
        private Line _lastAddedLine;

        private DiffuseMaterial _standardMaterial;
        private DiffuseMaterial _backMaterial;
        private DiffuseMaterial _textureMaterial;

        public LatheCreator()
        {
            _sections = new List<LatheSection>();

            InitializeComponent();

            _standardMaterial = new DiffuseMaterial(Brushes.Green);
            _backMaterial = new DiffuseMaterial(Brushes.Red);

            Reset();
        }

        private void ShapeCanvas_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            UserInfoTextBlock.Visibility = Visibility.Collapsed;

            Point relativeMousePosition = GetRelativeMousePosition(e);
            AddMousePosition(relativeMousePosition);
        }

        public void AddMousePosition(Point relativeMousePosition)
        { 
            var newLatheSection = new LatheSection(relativeMousePosition.Y, relativeMousePosition.X, true);
            _sections.Add(newLatheSection);

            var checkBox = new CheckBox();
            UpdateSectionCheckBox(checkBox, newLatheSection);

            checkBox.IsChecked = true;
            checkBox.Tag = _sections.Count - 1;
            checkBox.Unchecked += SharpEdgeCheckBoxChanged;
            checkBox.Checked += SharpEdgeCheckBoxChanged;

            SectionsStackPanel.Children.Add(checkBox);

            AddNewSections();
        }

        private void UpdateSectionCheckBox(CheckBox checkBox, LatheSection section)
        {
            checkBox.Content = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                             "Offset = {0:0.00}; Radius = {1:0.00}; IsSharpEdge = {2}",
                                             section.Offset, section.Radius, section.IsSharpEdge);
        }

        private void SharpEdgeCheckBoxChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            if (!this.IsLoaded)
                return;


            var checkBox = (CheckBox)sender;
            int index = (int) checkBox.Tag;

            _sections[index] = new LatheSection(_sections[index].Offset,
                                                _sections[index].Radius,
                                                checkBox.IsChecked ?? false);

            UpdateSectionCheckBox(checkBox, _sections[index]);

            UpdateLatheMesh();
        }

        private void ShapesBorder_OnMouseEnter(object sender, MouseEventArgs e)
        {
            MousePositionEllipse.Visibility = Visibility.Visible;
            
            if (_lastAddedLine != null)
                _lastAddedLine.Visibility = Visibility.Visible;

            UpdateMousePosition(e);
        }

        private void ShapesBorder_OnMouseLeave(object sender, MouseEventArgs e)
        {
            MousePositionEllipse.Visibility = Visibility.Collapsed;

            if (_lastAddedLine != null)
                _lastAddedLine.Visibility = Visibility.Collapsed;

            if (_lastLine.Visibility == Visibility.Visible)
            {
                Point lastPoint;

                if (_sections.Count > 0)
                {
                    var lastSection = _sections[_sections.Count - 1];
                    lastPoint = new Point(lastSection.Radius * ShapeCanvas.ActualWidth, lastSection.Offset * ShapeCanvas.ActualHeight);
                }
                else
                {
                    lastPoint = new Point(0, ShapeCanvas.Height);
                }

                _lastLine.X2 = lastPoint.X;
                _lastLine.Y2 = lastPoint.Y;
            }
        }

        private void ShapesBorder_OnMouseMove(object sender, MouseEventArgs e)
        {
            Point relativeMousePosition = GetRelativeMousePosition(e);

            UpdateMousePosition(relativeMousePosition);

            if (_lastAddedLine != null)
            {
                var p1 = new Point(relativeMousePosition.X * ShapeCanvas.ActualWidth, relativeMousePosition.Y * ShapeCanvas.ActualHeight);
                _lastAddedLine.X2 = p1.X;
                _lastAddedLine.Y2 = p1.Y;

                if (_lastLine.Visibility == Visibility.Visible)
                {
                    _lastLine.X2 = p1.X;
                    _lastLine.Y2 = p1.Y;
                }
            }
        }

        private void OnCheckboxChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            _firstLine.Visibility = (StartCheckBox.IsChecked ?? false) ? Visibility.Visible : Visibility.Collapsed;
            _lastLine.Visibility = (EndCheckBox.IsChecked ?? false) ? Visibility.Visible : Visibility.Collapsed;

            UpdateLatheMesh();
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLatheMesh();
        }


        private void Reset()
        {
            _sections.Clear();
            ShapeCanvas.Children.Clear();
            SectionsStackPanel.Children.Clear();

            if (_latheModel3D != null)
                _latheModel3D.Geometry = null;

            _firstLine = new Line()
            {
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                X1 = 0,
                Y1 = 0,
                X2 = 0,
                Y2 = 0
            };

            _lastLine = new Line()
            {
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                X1 = 0,
                Y1 = ShapeCanvas.Height,
                X2 = 0,
                Y2 = ShapeCanvas.Height
            };

            _lastAddedLine = _firstLine;

            ShapeCanvas.Children.Add(_firstLine);
            ShapeCanvas.Children.Add(_lastLine);

            UpdateCodePreview();
        }

        private void UpdateMousePosition(MouseEventArgs e)
        {
            Point relativeMousePosition = GetRelativeMousePosition(e);
            UpdateMousePosition(relativeMousePosition);
        }

        private void UpdateMousePosition(Point relativeMousePosition)
        {
            Canvas.SetLeft(MousePositionEllipse, relativeMousePosition.X * ShapesOverlayCanvas.ActualWidth - MousePositionEllipse.Width / 2);
            Canvas.SetTop(MousePositionEllipse, relativeMousePosition.Y * ShapesOverlayCanvas.ActualHeight - MousePositionEllipse.Height / 2);
        }

        private Point GetRelativeMousePosition(MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(ShapeCanvas);

            double x = mousePosition.X / ShapeCanvas.ActualWidth;
            double y = mousePosition.Y / ShapeCanvas.ActualHeight;

            if (x < 0) x = 0;
            else if (x > 1) x = 1;
            
            if (y < 0) y = 0;
            else if (y > 1) y = 1;

            return new Point(x, y);
        }

        private void AddNewSections()
        {
            var lastSection = _sections[_sections.Count - 1];
            var lastPoint = new Point(lastSection.Radius * ShapeCanvas.ActualWidth, lastSection.Offset * ShapeCanvas.ActualHeight);

            var ellipse = new Ellipse()
            {
                Width = 4,
                Height = 4,
                Stroke = Brushes.Red,
                StrokeThickness = 1
            };
            Canvas.SetLeft(ellipse, lastPoint.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, lastPoint.Y - ellipse.Height / 2);
            ShapeCanvas.Children.Add(ellipse);

            if (_sections.Count < 1)
                return;

            if (_lastAddedLine != null)
            {
                _lastAddedLine.X2 = lastPoint.X;
                _lastAddedLine.Y2 = lastPoint.Y;
            }


            _lastAddedLine = new Line()
            {
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                X1 = lastPoint.X,
                Y1 = lastPoint.Y,
                X2 = lastPoint.X,
                Y2 = lastPoint.Y
            };

            _firstLine.X2 = _sections[0].Radius * ShapeCanvas.ActualWidth;
            _firstLine.Y2 = _sections[0].Offset * ShapeCanvas.ActualHeight;

            _lastLine.X2 = lastPoint.X;
            _lastLine.Y2 = lastPoint.Y;


            ShapeCanvas.Children.Add(_lastAddedLine);

            UpdateLatheMesh();
        }

        private bool TryParseTextEntries(out Point3D startPosition, out Point3D endPosition, out int segments, out double radius, out Vector3D? startAngleVector3D)
        {
            bool success = true;
            double x, y, z;


            string[] parts = StartPositionTextBox.Text.Split(' ');

            if (parts.Length >= 3 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            {
                startPosition = new Point3D(x, y, z);
            }
            else
            {
                startPosition = new Point3D();
                success = false;
            }


            parts = EndPositionTextBox.Text.Split(' ');

            if (parts.Length >= 3 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            {
                endPosition = new Point3D(x, y, z);
            }
            else
            {
                endPosition = new Point3D();
                success = false;
            }



            parts = StartAngleVector3DTextBox.Text.Split(' ');

            if (parts.Length >= 3 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            {
                startAngleVector3D = new Vector3D(x, y, z);
            }
            else
            {
                startAngleVector3D = null;
            }



            if (!Int32.TryParse(SegmentsTextBox.Text, out segments))
                segments = 0;

            if (segments <= 3)
                success = false;


            if (!double.TryParse(MaxRadiusTextBox.Text, out radius))
                radius = 0;

            if (radius <= 0)
                success = false;

            return success;
        }

        private void UpdateLatheMesh()
        {
            bool isStartPositionClosed = StartCheckBox.IsChecked ?? false;
            bool isEndPositionClosed = EndCheckBox.IsChecked ?? false;

            // When start and end positions are closed we are allowed to create lathe with just one segment
            // When start or end position are opened than we need at least 2 segments
            if (_sections.Count < 1 || ((!isStartPositionClosed || !isEndPositionClosed) && _sections.Count < 2))
                return;

            Point3D startPosition, endPosition;
            int segments;
            double radius;
            Vector3D? startAngleVector3D;

            bool success = TryParseTextEntries(out startPosition, out endPosition, out segments, out radius, out startAngleVector3D);

            if (!success)
                return;

            bool generateTextureCoordinates = TextureCheckBox.IsChecked ?? false;

            
            // We need to multiply the Radius by radius
            LatheSection[] latheSections = _sections.ToArray();
            for (int i = 0; i < latheSections.Length; i++)
            {
                latheSections[i].Radius = latheSections[i].Radius * radius;
                latheSections[i].Offset = latheSections[i].Offset;
                latheSections[i].IsSharpEdge = latheSections[i].IsSharpEdge;
            }


            LatheMesh3D latheMesh3D;


            double startAngle, endAngle;
            GetStartAndEndAngle(out startAngle, out endAngle);

            if (startAngle != 0 || endAngle != 360)
            {
                latheMesh3D = new Ab3d.Meshes.LatheMesh3D(startPosition,
                                                         endPosition, 
                                                         latheSections, 
                                                         segments,
                                                         isStartPositionClosed, 
                                                         isEndPositionClosed, 
                                                         generateTextureCoordinates,
                                                         startAngle, 
                                                         endAngle, 
                                                         isMeshClosed: CloseMeshCheckBox.IsChecked ?? false);
            }
            else
            {
                latheMesh3D = new Ab3d.Meshes.LatheMesh3D(startPosition, 
                                                          endPosition,
                                                          latheSections, 
                                                          segments,
                                                          isStartPositionClosed,
                                                          isEndPositionClosed,
                                                          generateTextureCoordinates);
            }


            // StartAngleVector3D is a Vector3D that is used to define the direction in which the segment with StartAngle is facing.
            // This is used as the X axis for the base lathe geometry; the Y axis is defined by calculating a perpendicular vector to
            // StartAngleVector3D and the lathe direction (= endPosition - startPosition).
            //
            // When StartAngleVector3D is null (by default), then MathUtils.GetPerpendicularVectors method is used to calculate the X and Y axes.
            //
            // StartAngleVector3D is used when custom start and end angles are used or when texture coordinates are generated.
            //
            // For example:
            // - StartPosition is above the EndPosition (lathe direction is 0,-1,0),
            // - StartAngle = 0; EndAngle = 90,
            // - StartAngleVector3D is set to (1,0,0),
            //
            // In this case the lathe starts in the X axis direction (1,0,0) and then the angle increases until it reaches 90 degrees in the (0,0,-1) direction.
            latheMesh3D.StartAngleVector3D = startAngleVector3D;


            if (_latheModel3D == null)
            {
                _latheModel3D = new GeometryModel3D();
                _latheModel3D.BackMaterial = _backMaterial;

                var modelVisual3D = new ModelVisual3D();
                modelVisual3D.Content = _latheModel3D;

                MainViewport.Children.Add(modelVisual3D);
            }

            if (generateTextureCoordinates)
            {
                if (_textureMaterial == null)
                {
                    var imageBrush = new ImageBrush();
                    imageBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/10x10-texture.png"));

                    // We need to set Tile mode for images used on spheres
                    // Otherwise the image does not continue nicely (there is a linear edge) when the sphere comes around itself
                    imageBrush.TileMode = TileMode.Tile;

                    _textureMaterial = new DiffuseMaterial(imageBrush);
                }

                _latheModel3D.Material = _textureMaterial;
            }
            else
            {
                _latheModel3D.Material = _standardMaterial;
            }

            _latheModel3D.Geometry = latheMesh3D.Geometry;

            UpdateCodePreview();


            MeshInspector.MeshGeometry3D = latheMesh3D.Geometry;
        }


        private void UpdateCodePreview()
        {
            if (_sections.Count < 1)
            {
                CodePreviewTextBox.Text = "// Define at least one section";
                return;
            }


            Point3D startPosition, endPosition;
            int segments;
            double radius;
            Vector3D? startAngleVector3D;

            bool success = TryParseTextEntries(out startPosition, out endPosition, out segments, out radius, out startAngleVector3D);

            if (!success)
                return;


            double startAngle, endAngle;
            GetStartAndEndAngle(out startAngle, out endAngle);

            bool isStartPositionClosed = StartCheckBox.IsChecked ?? false;
            bool isEndPositionClosed = EndCheckBox.IsChecked ?? false;
            bool isMeshClosed = CloseMeshCheckBox.IsChecked ?? false;
            bool generateTextureCoordinates = TextureCheckBox.IsChecked ?? false;


            var sb = new StringBuilder();

            sb.AppendLine("var sections = new Ab3d.Meshes.LatheSection[] {");
            for (int i = 0; i < _sections.Count; i++)
            {
                sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, 
                                "    new Ab3d.Meshes.LatheSection({0:0.00}, {1:0.00}, {2})",
                                _sections[i].Offset, _sections[i].Radius * radius, _sections[i].IsSharpEdge ? "true" : "false");

                if (i < _sections.Count - 1)
                    sb.Append(",\r\n");
                else
                    sb.AppendLine();
            }

            sb.AppendLine("};\r\n");
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
@"var latheMesh3D = new Ab3d.Meshes.LatheMesh3D(new Point3D({0}), 
                                              new Point3D({1}), 
                                              sections, 
                                              segments: {2},
                                              isStartPositionClosed: {3},
                                              isEndPositionClosed: {4},
                                              generateTextureCoordinates: {5},
                                              startAngle: {6},
                                              endAngle: {7},
                                              isMeshClosed: {8});",
                startPosition, endPosition, segments, isStartPositionClosed.ToString().ToLower(), isEndPositionClosed.ToString().ToLower(), generateTextureCoordinates.ToString().ToLower(), startAngle, endAngle, isMeshClosed.ToString().ToLower());

            if (startAngleVector3D.HasValue)
            {
                sb.AppendFormat(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "\r\n\r\nlatheMesh3D.StartAngleVector3D = new Vector3D({0}, {1}, {2});",
                    startAngleVector3D.Value.X, startAngleVector3D.Value.Y, startAngleVector3D.Value.Z);
            }

            CodePreviewTextBox.Text = sb.ToString();
        }

        private void OnAngleTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            double startAngle, endAngle;
            bool success = GetStartAndEndAngle(out startAngle, out endAngle);

            if (success)
            {
                CloseMeshCheckBox.IsEnabled = (startAngle != 0 || endAngle != 360);
                UpdateLatheMesh();
            }
        }

        private bool GetStartAndEndAngle(out double startAngle, out double endAngle)
        {
            bool success = true;

            if (!double.TryParse(StartAngleTextBox.Text, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out startAngle))
            {
                startAngle = 0;
                success = false;
            }

            if (!double.TryParse(EndAngleTextBox.Text, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out endAngle))
            {
                endAngle = 360;
                success = false;
            }

            return success;
        }

        private void OnCloseMeshCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateLatheMesh();
        }
    }
}
