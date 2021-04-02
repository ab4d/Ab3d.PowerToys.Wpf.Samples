using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Ab3d.PowerToys.Samples.Objects3D
{
    // Ab3d.PowerToys provide a few ways in which it is possible to create meshes by using boolean operations.
    //
    // This sample shows how to use MeshBooleanOperations class. 
    // This is the simplest way to do boolean operations.
    //
    // The methods in MeshBooleanOperations class also provide the processOnlyIntersectingTriangles parameter.
    // When set to true (by default), then only the triangles that intersect with the second mesh are processed.
    // This preserve other triangles and can lead to faster processing and simpler final mesh.
    // See the next (AdvancedBooleanOperations) for more info about that.
    //
    // When you need to do multiple boolean operations on one object,
    // then it is recommended to combine all other meshes into one mesh
    // by using the Ab3d.Utilities.MeshUtils.CombineMeshes method (this is very fast).
    // This way you can do the boolean operation only once and this is much
    // faster then doing multiple boolean operations.
    // See the UseCases/ModelingWithBooleanOperations sample.
    //
    // If you do not have all the meshes available or you want to do one boolean operation,
    // then get the result and then do other boolean operation and then another, 
    // then it is recommended to use BooleanMesh3D because it preserves the intermediate objects,
    // is faster and can be also easy used on background thread.
    // See the UseCases/ModelingWithBooleanOperations sample for its usage in the background thread.
    //
    // It is also possible to call Subtract method on any Visual3D object
    // from Ab3d.PowerToys library that is derived from BaseModelVisual3D (BoxVisual3D, SphereVisual3D).
    // This method internally uses MeshBooleanOperations class.
    // This means that when using multiple boolean operations it is recommended to use BooleanMesh3D.
    //
    //
    // Note:
    // Boolean operations do not preserve or create TextureCoordinates. 
    // So if you want to show textures on those objects, you need to generate TextureCoordinates after the boolean mesh is created.
    // See Utilities/TextureCoordinatesGeneratorSample and UseCases/ModelingWithBooleanOperations for samples on how to do that.

    /// <summary>
    /// Interaction logic for BooleanOperationsSample.xaml
    /// </summary>
    public partial class BooleanOperationsSample : Page
    {
        public BooleanOperationsSample()
        {
            InitializeComponent();

            var boxMesh = new Ab3d.Meshes.BoxMesh3D(new Point3D(0, 0, 0), new Size3D(100, 100, 100), 1, 1, 1).Geometry;
            var sphereMesh = new Ab3d.Meshes.SphereMesh3D(new Point3D(0, 0, 0), 65, 16).Geometry;


            // Note that we set processOnlyIntersectingTriangles to false because boxMesh and sphereMesh are almost the same size and
            // therefore all triangles need to be processes. With false value we skip checking which triangles are intersecting.
            // If the sphereMesh would be much smaller than it is recommended to set that to true (this is also the default value so we can skip that).
            // See the next (AdvancedBooleanOperations) for more info about that.
            
            // Subtract
            var subtractedMesh = Ab3d.Utilities.MeshBooleanOperations.Subtract(boxMesh, sphereMesh, processOnlyIntersectingTriangles: false);
            ShowMesh(subtractedMesh, -150);

            // Intersect
            var intersectedMesh = Ab3d.Utilities.MeshBooleanOperations.Intersect(boxMesh, sphereMesh, processOnlyIntersectingTriangles: false);
            ShowMesh(intersectedMesh, 0);

            //Union
            var unionMesh = Ab3d.Utilities.MeshBooleanOperations.Union(boxMesh, sphereMesh, processOnlyIntersectingTriangles: false);
            ShowMesh(unionMesh, 150);


            // SEE ALSO:
            // AdvancedBooleanOperations.xaml.cs
            // UseCases/ModelingWithBooleanOperations.xaml.cs
            // Utilities/TextureCoordinatesGeneratorSample (to generate texture coordinates that are lost with boolean operations)
        }

        private void ShowMesh(MeshGeometry3D meshGeometry3D, double xOffset)
        {
            var wireframeVisual3D = new Ab3d.Visuals.WireframeVisual3D()
            {
                WireframeType = Ab3d.Visuals.WireframeVisual3D.WireframeTypes.WireframeWithOriginalSolidModel,
                UseModelColor = false,
                LineColor = Colors.Black,
                LineThickness = 1,
                Transform = new TranslateTransform3D(xOffset, 0, 0)
            };

            var geometryModel = new GeometryModel3D(meshGeometry3D, new DiffuseMaterial(Brushes.Gold));
            geometryModel.BackMaterial = new DiffuseMaterial(Brushes.Red);

            wireframeVisual3D.OriginalModel = geometryModel;

            RootModelVisual.Children.Add(wireframeVisual3D);
        }

        private void SetWireframeType(Ab3d.Visuals.WireframeVisual3D.WireframeTypes wireframeType)
        {
            foreach (var wireframeVisual3D in RootModelVisual.Children.OfType<WireframeVisual3D>())
                wireframeVisual3D.WireframeType = wireframeType;
        }

        private void CreateEdgeLines(Color lineColor)
        {
            foreach (var wireframeVisual3D in RootModelVisual.Children.OfType<WireframeVisual3D>())
            {
                var edgeLinePositions = new Point3DCollection();
                EdgeLinesFactory.AddEdgeLinePositions(wireframeVisual3D.OriginalModel, edgeStartAngleInDegrees: 30, linePositions: edgeLinePositions);

                var multiLineVisual3D = new MultiLineVisual3D()
                {
                    Positions = edgeLinePositions,
                    LineColor = lineColor,
                    LineThickness = 2,
                    Transform = wireframeVisual3D.Transform
                };

                EdgeLinesModelVisual.Children.Add(multiLineVisual3D);
            }
        }

        private void SolidModelRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            SetWireframeType(WireframeVisual3D.WireframeTypes.OriginalSolidModel);
            EdgeLinesModelVisual.Children.Clear();
        }

        private void SolidWithWireframeRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            SetWireframeType(WireframeVisual3D.WireframeTypes.WireframeWithOriginalSolidModel);
            EdgeLinesModelVisual.Children.Clear();
        }

        private void SolidWithEdgeLinesRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            SetWireframeType(WireframeVisual3D.WireframeTypes.OriginalSolidModel);

            EdgeLinesModelVisual.Children.Clear();
            CreateEdgeLines(Colors.Black);
        }

        private void EdgeLinesRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            SetWireframeType(WireframeVisual3D.WireframeTypes.None);

            EdgeLinesModelVisual.Children.Clear();
            CreateEdgeLines(Colors.Yellow);
        }
    }
}
