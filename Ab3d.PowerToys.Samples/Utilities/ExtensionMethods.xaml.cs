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
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for ExtensionMethods.xaml
    /// </summary>
    public partial class ExtensionMethods : Page
    {
        public ExtensionMethods()
        {
            InitializeComponent();


            var point1 = new Point3D(10, 10, 10);
            var vector1 = point1.ToVector3D();
            
            var vector2 = new Vector3D(10, 10, 10);
            var point2 = vector2.ToPoint3D();


            var sphereMesh = new Ab3d.Meshes.SphereMesh3D(new Point3D(0, 0, 0), 10, 30).Geometry;
            var sphereModel3D = new GeometryModel3D(sphereMesh, new DiffuseMaterial(Brushes.Gold));

            var sphereModelVisual = sphereModel3D.CreateModelVisual3D();


            Point3D centerPosition = sphereModel3D.Bounds.GetCenterPosition();



            // Load sample model
            var readerObj = new Ab3d.ReaderObj();
            var rootModel3DGroup = readerObj.ReadModel3D("pack://application:,,,/Ab3d.PowerToys.Samples;component/Resources/ObjFiles/robotarm.obj") as Model3DGroup;



            Viewport3D MainViewport3D = new Viewport3D();
            MainViewport3D.Name = "MainViewport3D";
            MainViewport3D.Children.Add(rootModel3DGroup.CreateModelVisual3D());


            var redDiffuseMaterial = new DiffuseMaterial(Brushes.Red);

            MainViewport3D.Children.ForEachGeometryModel3D((geometryModel3D) =>
            {
                // This code is called for every GeometryModel3D inside rootModel3DGroup
                geometryModel3D.Material = redDiffuseMaterial;
            });



            int totalPositions = 0;

            rootModel3DGroup.ForEachGeometryModel3D((geometryModel3D) =>
            {
                // This code is called for every GeometryModel3D inside rootModel3DGroup
                var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;
                if (meshGeometry3D != null && meshGeometry3D.Positions != null)
                    totalPositions += meshGeometry3D.Positions.Count;
            });


            MainViewport3D.Children.ForEachVisual3D((modelVisual3D) =>
            {
                // This code is called for every ModelVisual3D in MainViewport3D
                var sphereVisual3D = modelVisual3D as Ab3d.Visuals.SphereVisual3D;
                if (sphereVisual3D != null)
                    sphereVisual3D.Radius *= 1.2;
            });



            var allPositions = new List<Point3D>();

            Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(model3D: rootModel3DGroup,
                parentTransform3D: null,
                callback: delegate (GeometryModel3D geometryModel3D, Transform3D transform3D)
                {
                    // This code is called for every GeometryModel3D inside rootModel3DGroup
                    // transform3D is set to the Transform3D with all parent transformations or to null if there is no parent transformation
                    var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;

                    if (meshGeometry3D != null)
                    {
                        var positions = meshGeometry3D.Positions;
                        if (positions != null)
                        {
                            int positionsCount = positions.Count;
                            for (var i = 0; i < positionsCount; i++)
                            {
                                Point3D onePosition = positions[i];

                                if (transform3D != null)
                                    onePosition = transform3D.Transform(positions[i]);

                                allPositions.Add(onePosition);
                            }
                        }
                    }
                });


            // MainViewport3D.DumpHierarchy()
            DumpHierarchyTextBlock.Text = Ab3d.Utilities.Dumper.GetObjectHierarchyString(MainViewport3D);



            // Most extension methods are meant to be used in Visual Studio Immediate Window
            // For example: geometryModel3D.Dump();
            // This writes detailed information about geometryModel3D into the Immediate Window (using Console.Write)
            // For this sample, we do not want to display info text into Colose.Write, but instead show the text in the UI
            // To do this we use GetDumpString and other methods that are also used by the Dump extension.

            // Same as: rootModel3DGroup.Dump()
            string model3DGroupDumpString = Ab3d.Utilities.Dumper.GetDumpString(rootModel3DGroup);

            var baseMotorGeometryModel3D = readerObj.NamedObjects["BaseMotor"] as GeometryModel3D;
            // Same as: geometryModel3D.Dump();
            string geometryModel3DDumpString = Ab3d.Utilities.Dumper.GetDumpString(baseMotorGeometryModel3D);

            // Same as geometryModel3D.Geometry.Dump(5, "0.0")
            // Max 6 lines of data
            // "0.0" is format string
            string geometryDumpString = Ab3d.Utilities.Dumper.GetDumpString(baseMotorGeometryModel3D.Geometry, 6, "0.0");


            // Create a custom specular material
            var materialGroup = new MaterialGroup();
            materialGroup.Children.Add(new DiffuseMaterial(Brushes.Gold));
            materialGroup.Children.Add(new SpecularMaterial(Brushes.White, 16));

            // Same as: materialGroup.Dump();
            string materialDump = Ab3d.Utilities.Dumper.GetDumpString(materialGroup);


            var axisAngleRotation3D = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 30);
            var rotateTransform3D = new RotateTransform3D(axisAngleRotation3D, 100, 200, 300);

            var transform3DGroup = new Transform3DGroup();
            transform3DGroup.Children.Add(new ScaleTransform3D(1.0, 2.0, 0.5));
            transform3DGroup.Children.Add(rotateTransform3D);

            // Same as: transform3DGroup.Value.Dump();
            string matrixDump = Ab3d.Utilities.Dumper.GetMatrix3DText(transform3DGroup.Value);

            // Same as: transform3DGroup.Value.Dump(5);
            string matrix5Dump = Ab3d.Utilities.Dumper.GetMatrix3DText(transform3DGroup.Value, numberOfDecimals: 5);

            // Same as: transform3DGroup.Dump();
            string transformDump = Ab3d.Utilities.Dumper.GetTransformText(transform3DGroup);

            // Same as: rootModel3DGroup.Bounds.Dump();
            string boundDump = Ab3d.Utilities.Dumper.GetBoundsText(rootModel3DGroup.Bounds);

            string meshInitializationText = Ab3d.Utilities.Dumper.GetMeshInitializationCode(baseMotorGeometryModel3D.Geometry);


            ModelGroupDumpTextBlock.Text = model3DGroupDumpString;
            GeometryModelDumpTextBlock.Text = geometryModel3DDumpString;
            MeshGeometryDumpTextBlock.Text = geometryDumpString;
            MaterialDumpTextBlock.Text = materialDump;
            MatrixDumpTextBlock.Text = matrixDump;
            MatrixDump5TextBlock.Text = matrix5Dump;
            TransformDumpTextBlock.Text = transformDump;
            BoundsTextBlock.Text = boundDump;
            MeshInitializationTextBlock.Text = meshInitializationText;
        }
    }
}
