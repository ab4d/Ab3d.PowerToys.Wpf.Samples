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
using Ab3d.Meshes;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    /// <summary>
    /// Interaction logic for TubeLinesSample.xaml
    /// </summary>
    public partial class TubeLinesSample : Page
    {
        // to create a curve we create 10 segments between each control point
        // bigger values create more precise and more complex geometry
        // smaller values improve performance but can create curve with not so smooth lines
        private int _curveSegmentsBetweenControlPoints = 10; 

        public TubeLinesSample()
        {
            InitializeComponent();

            CreateCurveWithVisuals();
            CreateCurveWithMeshes();


            LineArrowAngle = 15;

            //var rnd = new Random();
            //for (int i = 0; i < 5; i++)
            //{
            //    var p1 = new Point3D(rnd.NextDouble() * 100 + 100, rnd.NextDouble() * 50 - 25, rnd.NextDouble() * 100 + 100);
            //    var p2 = new Point3D(rnd.NextDouble() * 100 + 100, rnd.NextDouble() * 50 - 25, rnd.NextDouble() * 100 + 100);

            //    var tubeRadius = rnd.NextDouble() * 3 + 1;
            //    AddTubeLineWithEndLineArrow(p1, p2, tubeRadius, arrowRadius: tubeRadius * 2);
            //}

            for (int i = 0; i < 5; i++)
            {
                var p1 = new Point3D(100, 0, 220 - i * 30);
                var p2 = new Point3D(200, 0, 220 - i * 30);

                var tubeRadius = 1 + i * 0.75;
                AddTubeLineWithEndLineArrow(p1, p2, tubeRadius, arrowRadius: tubeRadius * 2);
            }


            EmissiveMaterialInfoControl.InfoText =
@"If unchecked then the standard DiffuseMaterial is used to show the tube lines.
This means that the tube lines will be affected by rendered based on their and light positions and properties.

When this CheckBox is checked, the EmissiveMaterial will be used to render the lines.
This material will have the same color regardless of the lights (but can show some artifacts when two or more tube lines are drawing on top of each other).";
        }

        // Use TubeLineVisual3D to create curve
        // TubeLineVisual3D is more easy to use, but when there are a lot of TubeLineVisual3D instances it can decrease performance
        // In this case it is better to use the combined TubeLineMesh3D as seen in the next method (CreateCurveWithMeshes)
        private void CreateCurveWithVisuals()
        {
            // Create curve through those points
            var controlPoints = new Point3D[]
            {
                new Point3D(100, 0, 20),
                new Point3D(250, 0, 0),
                new Point3D(250, 0, 200),
                new Point3D(350, 0, 200)
            };

            var bezierCurve = Ab3d.Utilities.BezierCurve.CreateFromCurvePositions(controlPoints); // To create curve through specified points we must first convert the points into a bezierCurve (curve that has tangents defined for each point)
            var curvePositions = bezierCurve.CreateBezierCurve(_curveSegmentsBetweenControlPoints); 

            var curveTubeLineMaterial = CreateMaterial(Brushes.Orange);
            int selectedSegmentsCount = GetSelectedSegmentsCount();


            // Create TubeLineVisual3D for each curve segment
            Point3D previousCurvePosition = curvePositions[0];
            for (int i = 1; i < curvePositions.Count; i++)
            {
                Point3D curvePosition = curvePositions[i];

                var tubeLineVisual3D = new TubeLineVisual3D()
                {
                    StartPosition = previousCurvePosition,
                    EndPosition = curvePosition,
                    Material = curveTubeLineMaterial,
                    Segments = selectedSegmentsCount,  // Each tube has 8 segments or sides
                    Radius = 1,     // NOTE: Radius is not in screen space coordinates as this is for other 3D lines. Here radius is in 3D world coordinates.
                    GenerateTextureCoordinates = false // No need to generate texture coordinates because we do not use texture material
                };

                MainViewport.Children.Add(tubeLineVisual3D);

                previousCurvePosition = curvePosition;
            }
        }

        // This method created curve with tube lines but instead of using TubeLineVisual3D
        // here we create a series of MeshGeometry3D objects - one for each line segment.
        // Then we combine all those segments into one MeshGeometry3D and use it to create GeometryModel3D.
        // This can greatly improve performance because GeometryModel3D can be drawn with only one draw call on graphics card.
        // What is more is that the GeometryModel3D can be frozen. 
        // This means that this method can be executed on background thread and then the GeometryModel3D can be passed to UI thread.
        private void CreateCurveWithMeshes()
        {
            // Create curve through those points
            var controlPoints = new Point3D[]
            {
                new Point3D(100, 50, 20),
                new Point3D(250, 50, 0),
                new Point3D(250, 50, 200),
                new Point3D(350, 50, 200)
            };

            var bezierCurve = Ab3d.Utilities.BezierCurve.CreateFromCurvePositions(controlPoints); // To create curve through specified points we must first convert the points into a bezierCurve (curve that has tangents defined for each point)
            var curvePositions = bezierCurve.CreateBezierCurve(_curveSegmentsBetweenControlPoints);

            int selectedSegmentsCount = GetSelectedSegmentsCount();

            // meshes list will hold MeshGeometry3D for each curve segment
            var meshes = new List<MeshGeometry3D>(curvePositions.Count);

            Point3D previousCurvePosition = curvePositions[0];
            for (int i = 1; i < curvePositions.Count; i++)
            {
                Point3D curvePosition = curvePositions[i];

                var tubeLineMesh3D = new Ab3d.Meshes.TubeLineMesh3D(startPosition: previousCurvePosition, 
                                                                    endPosition: curvePosition, 
                                                                    radius: 1,
                                                                    segments: selectedSegmentsCount, 
                                                                    generateTextureCoordinates: false);


                var meshGeometry = tubeLineMesh3D.Geometry;
                meshes.Add(meshGeometry);

                previousCurvePosition = curvePosition;
            }

            // Combine all meshes into one MeshGeometry3D
            var combinedMeshGeometry = Ab3d.Utilities.MeshUtils.CombineMeshes(meshes);

            // Create one GeometryModel3D
            var geometryModel3D = new GeometryModel3D(combinedMeshGeometry, CreateMaterial(Brushes.GreenYellow));
            geometryModel3D.Freeze();

            MeshTubeLinesVisual.Content = geometryModel3D;
        }

        private void ChangeSegmentsCount(int segments)
        {
            foreach (var oneTubeLine in MainViewport.Children.OfType<TubeLineVisual3D>())
                oneTubeLine.Segments = segments;

            CreateCurveWithVisuals();
            CreateCurveWithMeshes();
        }

        private int GetSelectedSegmentsCount()
        {
            var comboBoxItem = SegmentsComboBox.SelectedItem as ComboBoxItem;

            if (comboBoxItem == null)
                return 8; // Default

            return Int32.Parse((string)comboBoxItem.Content);
        }

        private void SegmentsComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var selectedSegmentsCount = GetSelectedSegmentsCount();
            ChangeSegmentsCount(selectedSegmentsCount);
        }

        private void EmissiveMaterialCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var material = CreateMaterial(Brushes.Silver);

            foreach (var oneTubeLine in MainViewport.Children.OfType<TubeLineVisual3D>())
                oneTubeLine.Material = material;

            foreach (var oneArrowVisual in MainViewport.Children.OfType<ArrowVisual3D>())
                oneArrowVisual.Material = material;

            CreateCurveWithVisuals();
            CreateCurveWithMeshes();
        }

        private Material CreateMaterial(Brush brush)
        {
            Material material;
            if (EmissiveMaterialCheckBox.IsChecked ?? false)
            {
                var materialGroup = new MaterialGroup();
                materialGroup.Children.Add(new DiffuseMaterial(Brushes.Black));
                materialGroup.Children.Add(new EmissiveMaterial(brush));

                material = materialGroup;
            }
            else
            {
                material = new DiffuseMaterial(brush);
            }           
 
            return material;
        }



        private double _lineArrowLengthFactor = 3.73205081; // = 1 / Math.Tan(15 * Math.PI / 180);

        private double _lineArrowAngle = 15;

        /// <summary>
        /// Gets or sets the angle of the line arrows. Default value is 15 degrees.
        /// Note that if the line is short so that the arrow length exceeds the amount defined by MaxLineArrowLength, the arrow is shortened which increased the arrow angle.
        /// </summary>
        public double LineArrowAngle
        {
            get { return _lineArrowAngle; }
            set
            {
                _lineArrowAngle = value;
                _lineArrowLengthFactor = 1 / (float)Math.Tan(_lineArrowAngle * Math.PI / 180);
            }
        }


        private double _maxLineArrowLength = 0.333; // max arrow length is 1 / 3 of the line's length

        /// <summary>
        /// Gets or sets a float value that specifies the maximum arrow length set as fraction of the line length - e.g. 0.333 means that the maximum arrow length will be 1 / 3 (=0.333) of the line length.
        /// If the line is short so that the arrow length exceeds the amount defined by MaxLineArrowLength, the arrow is shortened (the arrow angle is increased).
        /// </summary>
        public double MaxLineArrowLength
        {
            get { return _maxLineArrowLength; }
            set { _maxLineArrowLength = value; }
        }

 

        private void AddTubeLineWithEndLineArrow(Point3D startPosition, Point3D endPosition, double tubeRadius, double arrowRadius)
        {
            var curveTubeLineMaterial = CreateMaterial(Brushes.Silver);
            int selectedSegmentsCount = GetSelectedSegmentsCount();

            var arrowLength = arrowRadius * _lineArrowLengthFactor;

            var lineVector = endPosition - startPosition;
            var lineLength = lineVector.Length;

            lineVector /= lineLength; // normalize


            var arrowLengthFraction = arrowLength / lineLength;

            // Usually the arrow angle that is passed to the ArrowVisual3D is twice the LineArrowAngle
            var fullArrowAngle = LineArrowAngle * 2;

            // We adjust the arrow when it occupies more than max fraction of the line (1/3 by default)
            if (arrowLengthFraction > _maxLineArrowLength)
            {
                arrowLength = lineLength * _maxLineArrowLength;

                // When the arrow is shortened, we also need to increase the arrow angle
                var updatedLineArrowLengthFactor = arrowLength / arrowRadius;
                fullArrowAngle = Math.Atan(1 / updatedLineArrowLengthFactor) * 360 / Math.PI; // This is inverted as when calculating _lineArrowLengthFactor and then multiplied by 2 (360 = 180 * 2)
            }

            // Because we are adding an end line arrow, we go back from the end position
            var startArrowPosition = endPosition - lineVector * arrowLength;


            var tubeLineVisual3D = new TubeLineVisual3D()
            {
                StartPosition = startPosition,
                EndPosition = startArrowPosition,
                Material = curveTubeLineMaterial,
                Segments = selectedSegmentsCount,  
                Radius = tubeRadius,               
                GenerateTextureCoordinates = false // No need to generate texture coordinates because we do not use texture material
            };

            MainViewport.Children.Add(tubeLineVisual3D);


            var arrowVisual3D = new ArrowVisual3D()
            {
                StartPosition = startArrowPosition,
                EndPosition = endPosition,
                Radius = arrowRadius,
                ArrowAngle = fullArrowAngle,
                Segments = selectedSegmentsCount,
                Material = curveTubeLineMaterial,
                GenerateTextureCoordinates = false
            };

            MainViewport.Children.Add(arrowVisual3D);
        }
    }
}
