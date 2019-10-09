using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for MeshInspectorOverlaySample.xaml
    /// </summary>
    public partial class MeshInspectorOverlaySample : Page
    {
        private MeshGeometry3D _rootMesh;
        private TypeConverter _colorTypeConverter;

        public MeshInspectorOverlaySample()
        {
            InitializeComponent();

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                CreateRootModel();
            };
        }

        private void ObjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            CreateRootModel();
        }
        
        private void CreateRootModel()
        {
            switch (ObjectComboBox.SelectedIndex)
            {
                case 0:
                    Ab3d.Meshes.BoxMesh3D box = new Ab3d.Meshes.BoxMesh3D(new Point3D(0, 0, 0), new Size3D(130, 60, 100), 1, 1, 1);
                    _rootMesh = box.Geometry;
                    break;

                case 1:
                    Ab3d.Meshes.BoxMesh3D box2 = new Ab3d.Meshes.BoxMesh3D(new Point3D(0, 0, 0), new Size3D(130, 60, 100), 4, 4, 4);
                    _rootMesh = box2.Geometry;
                    break;

                case 2:
                    Ab3d.Meshes.SphereMesh3D sphere = new Ab3d.Meshes.SphereMesh3D(new Point3D(0, 0, 0), 80, 10);
                    _rootMesh = sphere.Geometry;
                    break;

                case 3:
                    Ab3d.Meshes.SphereMesh3D sphere2 = new Ab3d.Meshes.SphereMesh3D(new Point3D(0, 0, 0), 80, 5);
                    _rootMesh = sphere2.Geometry;
                    break;

                case 4:
                    Ab3d.Meshes.CylinderMesh3D cylinder = new Ab3d.Meshes.CylinderMesh3D(new Point3D(0, -50, 0), 60, 100, 12, true);
                    _rootMesh = cylinder.Geometry;
                    break;

                case 5:
                    Ab3d.Meshes.ConeMesh3D cone = new Ab3d.Meshes.ConeMesh3D(new Point3D(0, -50, 0), 30, 60, 100, 12, true);
                    _rootMesh = cone.Geometry;
                    break;

                case 6:
                    Ab3d.Meshes.ConeMesh3D cone2 = new Ab3d.Meshes.ConeMesh3D(new Point3D(0, -50, 0), 30, 60, 100, 6, false);
                    _rootMesh = cone2.Geometry;
                    break;

                case 7:
                    var readerObj = new Ab3d.ReaderObj();
                    var teapotModel = readerObj.ReadModel3D(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\ObjFiles\Teapot.obj")) as GeometryModel3D;

                    if (teapotModel == null)
                        return;

                    // Get the teapot MeshGeometry3D
                    _rootMesh = (MeshGeometry3D)teapotModel.Geometry;

                    break;

                default:
                    _rootMesh = null;
                    break;
            }


            var geometryModel3D = new GeometryModel3D(_rootMesh, new DiffuseMaterial(Brushes.Silver));

            MainViewport.Children.Clear();
            MainViewport.Children.Add(geometryModel3D.CreateModelVisual3D());

            MeshInspector.MeshGeometry3D = _rootMesh;

            Camera1.Refresh(); // Recreate the camera's light
        }

        private void PositionsTextColorComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            MeshInspector.PositionsTextColor = GetColorFromComboBox(PositionsTextColorComboBox);
        }

        private void TriangleIndexesTextColorComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            MeshInspector.TriangleIndexesTextColor = GetColorFromComboBox(TriangleIndexesTextColorComboBox);
        }


        private void OnPositionsTextFontWeightCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            MeshInspector.PositionsTextFontWeight = (PositionsTextFontWeightCheckBox.IsChecked ?? false) ? FontWeights.Bold : FontWeights.Normal;
        }

        private void OnTriangleIndexesTextFontWeightCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            MeshInspector.TriangleIndexesTextFontWeight = (TriangleIndexesTextFontWeightCheckBox.IsChecked ?? false) ? FontWeights.Bold : FontWeights.Normal;
        }


        private Color GetColorFromComboBox(ComboBox comboBox)
        {
            if (_colorTypeConverter == null)
                _colorTypeConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Color));

            var comboBoxItem = (ComboBoxItem)comboBox.SelectedItem;

            var selectedColorText = (string)comboBoxItem.Content;
            if (string.IsNullOrEmpty(selectedColorText))
                return Colors.Black;

            return (Color)_colorTypeConverter.ConvertFromString(selectedColorText);
        }
    }
}
