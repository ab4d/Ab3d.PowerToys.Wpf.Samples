using System;
using System.Collections.Generic;
using System.Linq;
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
using Ab3d.Assimp;

namespace Ab3d.PowerToys.Samples.Other
{
    /// <summary>
    /// Interaction logic for BasicWpf3dObjectsTutorial.xaml
    /// </summary>
    public partial class BasicWpf3dObjectsTutorial : Page
    {
        private GeometryModel3D _geometryModel3;

        public BasicWpf3dObjectsTutorial()
        {
            InitializeComponent();

            var mesh2 = new MeshGeometry3D()
            {
                Positions = new Point3DCollection(new[]
                {
                    new Point3D(5,   0, 5),
                    new Point3D(100, 0, 5),
                    new Point3D(100, 0, 50),
                    new Point3D(5,   0, 50)
                }),

                TriangleIndices = new Int32Collection(new[]
                {
                    0, 2, 1,
                    3, 2, 0
                })
            };

            var geometryModel2 = new GeometryModel3D()
            {
                Geometry = mesh2,
                Material = new DiffuseMaterial(Brushes.LightGreen),
                BackMaterial = new DiffuseMaterial(Brushes.Red),
            };

            var modelVisual2 = new ModelVisual3D()
            {
                Content = geometryModel2
            };

            //// Using CreateModelVisual3D extensiton method from Ab3d.PowerToys
            //var modelVisual2 = geometryModel2.CreateModelVisual3D();
            //Viewport2.Children.Add(modelVisual2);

            //// in one line:
            //Viewport2.Children.Add(geometryModel2.CreateModelVisual3D());

            Viewport2.Children.Add(modelVisual2);

            MeshInspector2.MeshGeometry3D = mesh2;

            // #############

            var mesh3 = new MeshGeometry3D()
            {
                Positions = mesh2.Positions,
                TriangleIndices = new Int32Collection(new[]
                {
                    2, 0, 1, // changed from 0, 2, 1
                    3, 2, 0
                }),
                Normals = new Vector3DCollection(new [] { new Vector3D(0, 1, 0), new Vector3D(0, 1, 0), new Vector3D(0, 1, 0), new Vector3D(0, 1, 0)}) // We define Normals because the automatically generated normals are not correct because the two triangles are oriented differently and this produces zero length normals for the shared positions; note that for mesh2 this was not needed because triangles are correctly oriented and WPF was able to correctly calculate normals.
            };

            _geometryModel3 = new GeometryModel3D()
            {
                Geometry     = mesh3,
                Material     = new DiffuseMaterial(Brushes.LightGreen),
                BackMaterial = new DiffuseMaterial(Brushes.Red),
            };

            var modelVisual3 = new ModelVisual3D()
            {
                Content = _geometryModel3
            };

            Viewport3.Children.Add(modelVisual3);

            MeshInspector3.MeshGeometry3D = mesh3;

            // #############

            var modelVisual4 = new ModelVisual3D()
            {
                Content = _geometryModel3
            };

            Viewport4.Children.Add(modelVisual4);

            MeshInspector4.MeshGeometry3D = mesh3;

            // #############

            var mesh5 = new MeshGeometry3D()
            {
                Positions       = mesh2.Positions,
                TriangleIndices = mesh2.TriangleIndices,
                TextureCoordinates = new PointCollection(new[]
                {
                    new Point(0, 0),
                    new Point(1, 0),
                    new Point(1, 1),
                    new Point(0, 1),
                })
            };

            var textureImage = new BitmapImage(new Uri("pack://application:,,,/Resources/10x10-texture.png")); // Read image from resources
            var imageBrush = new ImageBrush(textureImage);

            var geometryModel5 = new GeometryModel3D()
            {
                Geometry     = mesh5,
                Material     = new DiffuseMaterial(imageBrush),
                BackMaterial = new DiffuseMaterial(Brushes.Red),
            };

            var modelVisual5 = new ModelVisual3D()
            {
                Content = geometryModel5
            };

            Viewport5.Children.Add(modelVisual5);

            MeshInspector4.MeshGeometry3D = mesh5;


            // #############

            //var geometryModel6 = new GeometryModel3D()
            //{
            //    Geometry = mesh2,
            //    Material = new DiffuseMaterial(Brushes.Blue)
            //};

            //// The same with passing parameters to constructor
            //var geometryModel7 = new GeometryModel3D(mesh2, new DiffuseMaterial(Brushes.Blue));

            //// Using custom color
            //var geometryModel8 = new GeometryModel3D(mesh2, new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 123, 0))));

            //// Using texture image
            //var textureImage1 = new BitmapImage(new Uri(@"c:\images\texture.png")); // Read image from file
            //var textureImage2 = new BitmapImage(new Uri("pack://application:,,,/Resources/10x10-texture.png")); // Read image from resources
            //var geometryModel9 = new GeometryModel3D(mesh2, new DiffuseMaterial(new ImageBrush(textureImage1)));

            //var geometryModel10 = new GeometryModel3D()
            //{
            //    Geometry = mesh2,
            //    Material = new DiffuseMaterial(Brushes.Green),
            //    BackMaterial = new DiffuseMaterial(Brushes.Red)
            //};


            var sphereMesh1 = new Ab3d.Meshes.SphereMesh3D(centerPosition: new Point3D(-30, 0, 0), radius: 10, segments: 30).Geometry;
            var sphereMesh2 = new Ab3d.Meshes.SphereMesh3D(centerPosition: new Point3D(0, 0, 0), radius: 10, segments: 30).Geometry;
            var sphereMesh3 = new Ab3d.Meshes.SphereMesh3D(centerPosition: new Point3D(30, 0, 0), radius: 10, segments: 30).Geometry;

            //var planeMesh1 = new Ab3d.Meshes.PlaneMesh3D(centerPosition: new Point3D(30, 0, 0), planeNormal: new Vector3D(0, 1, 0), planeHeightDirection: new Vector3D(0, 0, -1), size: new Size(200, 100), widthSegments: 1, heightSegments: 1).Geometry;
            //var boxMesh1 = new Ab3d.Meshes.BoxMesh3D(centerPosition: new Point3D(0, 0, 0), size: new Size3D(80, 40, 60), xSegments: 1, ySegments: 1, zSegments: 1).Geometry;


            var geometryModel11 = new GeometryModel3D(sphereMesh1, new DiffuseMaterial(Brushes.Green));

            var materialGroup12 = new MaterialGroup();
            materialGroup12.Children.Add(new DiffuseMaterial(Brushes.Green));
            materialGroup12.Children.Add(new SpecularMaterial(Brushes.White, 16));

            var geometryModel12 = new GeometryModel3D(sphereMesh2, materialGroup12);

            var materialGroup13 = new MaterialGroup();
            materialGroup13.Children.Add(new DiffuseMaterial(Brushes.Black)); // Add black DiffuseMaterial to EmissiveMaterial to make it opaque
            materialGroup13.Children.Add(new EmissiveMaterial(Brushes.Green));

            var geometryModel13 = new GeometryModel3D(sphereMesh3, materialGroup13);


            var modelVisual11 = new ModelVisual3D() { Content = geometryModel11 };
            var modelVisual12 = new ModelVisual3D() { Content = geometryModel12 };
            var modelVisual13 = new ModelVisual3D() { Content = geometryModel13 };

            Viewport6.Children.Add(modelVisual11);
            Viewport6.Children.Add(modelVisual12);
            Viewport6.Children.Add(modelVisual13);


            //var childModel3DGroup = new Model3DGroup();
            //childModel3DGroup.Children.Add(geometryModel11);
            //childModel3DGroup.Children.Add(geometryModel12);

            //var rootModel3DGroup = new Model3DGroup();
            //rootModel3DGroup.Children.Add(geometryModel2);
            //rootModel3DGroup.Children.Add(geometryModel5);
            //rootModel3DGroup.Children.Add(childModel3DGroup);


            AssimpLoader.LoadAssimpNativeLibrary();

            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\robotarm-upper-part.3ds");

            var assimpWpfImporter = new AssimpWpfImporter();
            var robotModel3D = assimpWpfImporter.ReadModel3D(fileName);

            string dumpString = Ab3d.Utilities.Dumper.GetObjectHierarchyString(robotModel3D);

            RobotArmSampleTextBox.Text = dumpString;


            //var transform3DGroup = new Transform3DGroup();
            //transform3DGroup.Children.Add(new ScaleTransform3D(2, 3, 2));
            //transform3DGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 45)));
            //transform3DGroup.Children.Add(new TranslateTransform3D(100, 0, 0));

            //geometryModel2.Transform = transform3DGroup;
        }

        private void OnOnlyFrontFacingTrianglesCheckBox1CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (OnlyFrontFacingTrianglesCheckBox1.IsChecked ?? false)
                _geometryModel3.BackMaterial = null;
            else
                _geometryModel3.BackMaterial = new DiffuseMaterial(Brushes.Red);
        }
    }
}
