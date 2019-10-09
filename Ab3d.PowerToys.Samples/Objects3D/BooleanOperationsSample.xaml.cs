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
    // Ab3d.PowerToys provide a few ways in which it is possible to create meshes from boolean operations.
    //
    // This sample shows how to use MeshBooleanOperations class. 
    // This is the simplest way to create boolean operations.
    //
    // But when you need to do multiple boolean operations on one object,
    // is recommended to use BooleanMesh3D because it preserves the intermediate objects and
    // is much faster and can be also easy used on background thread.
    // See the UseCases/ModelingWithBooleanOperations sample for its usage.
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


            // Subtract
            var subtractedMesh = Ab3d.Utilities.MeshBooleanOperations.Subtract(boxMesh, sphereMesh);
            ShowMesh(subtractedMesh, -150);

            // Intersect
            var intersectedMesh = Ab3d.Utilities.MeshBooleanOperations.Intersect(boxMesh, sphereMesh);
            ShowMesh(intersectedMesh, 0);

            //Union
            var unionMesh = Ab3d.Utilities.MeshBooleanOperations.Union(boxMesh, sphereMesh);
            ShowMesh(unionMesh, 150);
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
    }
}
