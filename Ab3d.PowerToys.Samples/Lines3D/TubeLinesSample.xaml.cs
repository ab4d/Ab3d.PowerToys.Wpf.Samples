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

            EmissiveMaterialInfoImage.ToolTip =
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
                    GenerateTextureCoordinates = false // No need to generate texture coordinates because we do not use texture meterial
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
    }
}
