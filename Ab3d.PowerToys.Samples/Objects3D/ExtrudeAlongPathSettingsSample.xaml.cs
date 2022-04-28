using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
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
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for ExtrudeAlongPathSettingsSample.xaml
    /// </summary>
    public partial class ExtrudeAlongPathSettingsSample : Page
    {
        private Point[] _currentShapePath;
        private Point3D[] _currentExtrudePath;

        private int _currentExtrudePathIndex;

        private ModelVisual3D _extrudedMeshRootVisual3D;
        private GeometryModel3D _extrudedModel3D;


        public ExtrudeAlongPathSettingsSample()
        {
            InitializeComponent();

            CreateScene();
        }

        private void CreateScene()
        {
            MainViewport.Children.Clear();

            _extrudedMeshRootVisual3D = new ContentVisual3D();
            MainViewport.Children.Add(_extrudedMeshRootVisual3D);


            _currentShapePath = new Point[]
            {
                new Point(-10, 0),
                new Point(10, 0),
                new Point(10, 10),
                new Point(6, 14),
                new Point(-6, 14),
                new Point(-10, 10),
            };

            // Now define a simple 3D path:
            _currentExtrudePathIndex = 0;
            SetNextExtrudedPath();
            
            // Create extruded models:
            CreateExtrudedMesh();
        }

        private void CreateExtrudedMesh()
        {
            // Until now we only provided the shape positions to the CreateExtrudedMeshGeometry.
            // This method then triangulated the shape in case it was closed.
            // Here we manually triangulate the shape and provide the shapeTriangleIndices to CreateExtrudedMeshGeometry:
            var triangulator = new Ab3d.Utilities.Triangulator(_currentShapePath);

            // NOTE: CreateTriangleIndices can throw FormatException when the positions are not correctly defined (for example if the lines intersect each other).
            List<int> triangleIndices = triangulator.CreateTriangleIndices();


            bool flipNormals;
            if (FlipNormalsComboBox.SelectedIndex == 0)
                flipNormals = false;
            else if (FlipNormalsComboBox.SelectedIndex == 1)
                flipNormals = true;
            else
                flipNormals = triangulator.IsClockwise;  // If true than normals are flipped - used when positions are defined in a counter clockwise order
            

            MeshGeometry3D extrudedMesh = Mesh3DFactory.CreateExtrudedMeshGeometry(
                shapePositions: _currentShapePath,
                shapeTriangleIndices: triangleIndices,
                extrudePathPositions: _currentExtrudePath,
                shapeYVector3D: new Vector3D(0, 1, 0),
                isClosed: IsClosedCheckBox.IsChecked ?? false,
                isSmooth: IsSmoothCheckBox.IsChecked ?? false,
                flipNormals: flipNormals,
                preserveShapeSizeAtJunctions: PreserveShapeSizeAtJunctionsCheckBox.IsChecked ?? false,
                preserveShapeYVector: PreserveShapeYVectorCheckBox.IsChecked ?? false);


            bool setBackMaterial = SetBackMaterialCheckBox.IsChecked ?? false;
            bool isTransparent = IsTransparentCheckBox.IsChecked ?? false;

            CreateGeometryModel(extrudedMesh, setBackMaterial, isTransparent);
        }

        private void CreateGeometryModel(MeshGeometry3D meshGeometry3D, bool setBackMaterial, bool isTransparent)
        {
            _extrudedModel3D = new GeometryModel3D();

            SetMaterial(setBackMaterial, isTransparent);

            _extrudedModel3D.Geometry = meshGeometry3D;


            _extrudedMeshRootVisual3D.Children.Clear();
            _extrudedMeshRootVisual3D.Children.Add(_extrudedModel3D.CreateModelVisual3D());

            Camera1.TargetPosition = _extrudedModel3D.Bounds.GetCenterPosition();


            MeshInspector.MeshGeometry3D = meshGeometry3D;
        }

        private void SetMaterial(bool setBackMaterial, bool isTransparent)
        {
            if (_extrudedModel3D == null)
                return;

            var brush = new SolidColorBrush(Colors.Green);
            var backBrush = new SolidColorBrush(Colors.Gray);

            if (isTransparent)
            {
                brush.Opacity     = 0.5;
                backBrush.Opacity = 0.5;
            }


            _extrudedModel3D.Material = new DiffuseMaterial(brush);

            if (setBackMaterial)
                _extrudedModel3D.BackMaterial = new DiffuseMaterial(backBrush);
            else
                _extrudedModel3D.BackMaterial = null;
        }
        
        private void OnPathSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            CreateExtrudedMesh(); 
        }

        private void OnViewSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            bool setBackMaterial = SetBackMaterialCheckBox.IsChecked ?? false;
            bool isTransparent = IsTransparentCheckBox.IsChecked ?? false;

            SetMaterial(setBackMaterial, isTransparent);
        }

        private void NextPathButton_OnClick(object sender, RoutedEventArgs e)
        {
            SetNextExtrudedPath();
            CreateExtrudedMesh();
        }

        private void SetNextExtrudedPath()
        {
            switch (_currentExtrudePathIndex)
            {
                case 0:
                    _currentExtrudePath = new Point3D[]
                    {
                        new Point3D(0, 0, 0),
                        new Point3D(0, 0, -50),
                        new Point3D(50, 0, -100),
                        new Point3D(50, 0, -150),
                        new Point3D(100, 0, -150),
                        new Point3D(50, 0, -200),
                        new Point3D(100, 0, -200),
                        new Point3D(150, 50, -200),
                        new Point3D(200, -50, -200),
                        new Point3D(250, -50, -200),
                        new Point3D(250, 0, -150),
                        new Point3D(250, 0, -100),
                        new Point3D(250, 50, -100),
                        new Point3D(250, 50, -50),
                        new Point3D(250, 50, 0),
                    };
                    break;

                default:
                    _currentExtrudePath = new Point3D[10];

                    // Create random path
                    var onePosition = new Point3D(0, 0, 0);
                    var rnd = new Random();

                    for (int i = 0; i < 10; i++)
                    {
                        _currentExtrudePath[i] = onePosition;

                        var randomVector = new Vector3D(40, rnd.NextDouble() * 60 - 30, rnd.NextDouble() * 60 - 30);
                        onePosition += randomVector;
                    }

                    break;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < _currentExtrudePath.Length; i++)
            {
                var pathPosition = _currentExtrudePath[i];
                sb.AppendFormat("{0:0} {1:0} {2:0}", pathPosition.X, pathPosition.Y, pathPosition.Z);

                if (i < _currentExtrudePath.Length - 1)
                    sb.AppendLine();
            }

            PositionsTextBox.Text = sb.ToString();

            _currentExtrudePathIndex ++;
        }
    }
}
