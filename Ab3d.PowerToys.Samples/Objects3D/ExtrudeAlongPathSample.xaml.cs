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
using Ab3d.Common;
using Ab3d.Meshes;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for ExtrudeAlongPathSample.xaml
    /// </summary>
    public partial class ExtrudeAlongPathSample : Page
    {
        public ExtrudeAlongPathSample()
        {
            InitializeComponent();


            // First prepare two 2D shapes:
            var letterTShapePositions = new Point[]
            {
                new Point(5, 0),
                new Point(-5, 0),
                new Point(-5, 40),
                new Point(-20, 40),
                new Point(-20, 50),
                new Point(20, 50),
                new Point(20, 40),
                new Point(5, 40),
            };

            var ellipsePositionList = new List<Point>();
            for (int i = 0; i < 360; i += 20)
            {
                ellipsePositionList.Add(new Point(Math.Sin(i / 180.0 * Math.PI) * 20, Math.Cos(i / 180.0 * Math.PI) * 10));
            }


            // Now define a simple 3D path:
            var extrudePath = new Point3D[]
            {
                new Point3D(0,    0,   0),
                new Point3D(0,    90,  0),
                new Point3D(-20,  110,  0),
                new Point3D(-50,  130, 20),
                new Point3D(-50,  130, 100),
            };


            // Create extruded models:

            MeshGeometry3D extrudedMesh1 = Mesh3DFactory.CreateExtrudedMeshGeometry(
                shapePositions: ellipsePositionList,
                extrudePathPositions: extrudePath,
                shapeYVector3D: new Vector3D(0, 0, -1),
                isClosed: true,
                isSmooth: true);

            CreateGeometryModel(extrudedMesh1, offset: new Vector3D(-150, 0, -50), setBackMaterial: false);



            var extrudedMesh2 = Mesh3DFactory.CreateExtrudedMeshGeometry(
                shapePositions: ellipsePositionList,
                extrudePathPositions: extrudePath,
                shapeYVector3D: new Vector3D(0, 0, -1),
                isClosed: false,
                isSmooth: false);

            // Because this mesh will not be closed, we will be able to see inside - so set the back material to dim gray.
            CreateGeometryModel(extrudedMesh2, offset: new Vector3D(0, 0, -50), setBackMaterial: true);



            // Until now we only provided the shape positions to the CreateExtrudedMeshGeometry.
            // This method then triangulated the shape in case it was closed.
            // Here we manually triangulate the shape and provide the shapeTriangleIndices to CreateExtrudedMeshGeometry:
            var triangulator = new Ab3d.Utilities.Triangulator(letterTShapePositions);

            // NOTE: CreateTriangleIndices can throw FormatException when the positions are not correctly defined (for example if the lines intersect each other).
            List<int> triangleIndices = triangulator.CreateTriangleIndices();

            MeshGeometry3D extrudedMesh = Mesh3DFactory.CreateExtrudedMeshGeometry(
                shapePositions: letterTShapePositions,
                shapeTriangleIndices: triangleIndices,
                extrudePathPositions: extrudePath,
                shapeYVector3D: new Vector3D(0, 0, -1),
                isClosed: true,
                isSmooth: false,
                flipNormals: triangulator.IsClockwise); // If true than normals are flipped - used when positions are defined in a counter clockwise order

            CreateGeometryModel(extrudedMesh, offset: new Vector3D(150, 0, -50), setBackMaterial: false);
        }


        private void CreateGeometryModel(MeshGeometry3D meshGeometry3D, Vector3D offset, bool setBackMaterial)
        {
            GeometryModel3D model = new GeometryModel3D();

            model.Material = new DiffuseMaterial(Brushes.Green);

            if (setBackMaterial)
                model.BackMaterial = new DiffuseMaterial(Brushes.DimGray);

            model.Geometry = meshGeometry3D;
            model.Transform = new TranslateTransform3D(offset);

            MainViewport.Children.Add(model.CreateModelVisual3D());

            // Uncomment to show triangles and normals:
            //var modelDecoratorVisual3D = new Ab3d.Visuals.ModelDecoratorVisual3D()
            //{
            //    TargetModel3D = model,
            //    ShowNormals = true,
            //    ShowTriangles = true,
            //    NormalsLineLength = 10
            //};

            //MainViewport.Children.Add(modelDecoratorVisual3D);
        }
    }
}
