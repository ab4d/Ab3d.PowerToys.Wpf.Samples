using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for TrapezoidVisual3DSample.xaml
    /// </summary>
    public partial class TrapezoidVisual3DSample : Page
    {
        private TypeConverter _sizeTypeConverter;

        public TrapezoidVisual3DSample()
        {
            InitializeComponent();

            _sizeTypeConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Size));

            this.Loaded += new RoutedEventHandler(TrapezoidVisual3DSample_Loaded);
        }

        void TrapezoidVisual3DSample_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMaterial();
            UpdateTrianglesAndNormals();

            Camera1.Refresh(); // This will measure the models on the scene and reposition the scene camera
        }

        private void OnMaterialSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateMaterial();
        }

        private void OnWireSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateTrianglesAndNormals();
        }

        private void TrapezoidVisual3D1_GeometryChanged(object sender, EventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateTrianglesAndNormals();
        }

        private void BottomSizeComboBox_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var comboBoxItem = BottomSizeComboBox.SelectedItem as ComboBoxItem;
            var selectedSizeText = (string)comboBoxItem.Content;

            TrapezoidVisual3D1.BottomSize = (Size) _sizeTypeConverter.ConvertFromString(selectedSizeText);
        }

        private void TopSizeComboBox_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var comboBoxItem = TopSizeComboBox.SelectedItem as ComboBoxItem;
            var selectedSizeText = (string)comboBoxItem.Content;

            TrapezoidVisual3D1.TopSize = (Size)_sizeTypeConverter.ConvertFromString(selectedSizeText);
        }

        private void TrapezoidTypeCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (StandardTrapezoidRadioButton.IsChecked ?? false)
            {
                TrapezoidVisual3D1.IsVisible = true;
                StandardTrapezoidPanel.Visibility = Visibility.Visible;

                CustomTrapezoidVisual3D.Content = null;
            }
            else
            {
                TrapezoidVisual3D1.IsVisible = false;
                StandardTrapezoidPanel.Visibility = Visibility.Collapsed;

                if (CustomTopCenterRadioButton.IsChecked ?? false)
                {
                    // Create Trapezoid with custom TopCenterPosition
                    var trapezoidModel = Ab3d.Models.Model3DFactory.CreateTrapezoid(bottomCenterPosition: new Point3D(0, 0, 0), 
                                                                                    bottomSize: new Size(160, 120), 
                                                                                    topCenterPosition: new Point3D(80, 70, 0),
                                                                                    topSize: new Size(80, 50), 
                                                                                    material: TrapezoidVisual3D1.Material);
                    CustomTrapezoidVisual3D.Content = trapezoidModel;
                }
                else if (CustomDirectionRadioButton.IsChecked ?? false)
                {
                    // Create Trapezoid with custom direction
                    // The direction is specifed with setting sizeWidthVector3D and sizeHeightVector3D
                    // Those two vectors define orientation of base and top rectangle with defining the directions in which and height of the rectangle points
                    var trapezoidModel = Ab3d.Models.Model3DFactory.CreateTrapezoid(bottomCenterPosition: new Point3D(0, 0, 0), 
                                                                                    bottomSize: new Size(150, 120), 
                                                                                    topCenterPosition: new Point3D(120, 20, 0), 
                                                                                    topSize: new Size(80, 50), 
                                                                                    sizeWidthVector3D: Constants.ZAxis, 
                                                                                    sizeHeightVector3D: Constants.YAxis,
                                                                                    material: TrapezoidVisual3D1.Material);
                    CustomTrapezoidVisual3D.Content = trapezoidModel;
                }
            }

            UpdateMaterial();
            UpdateTrianglesAndNormals();
        }


        private void UpdateMaterial()
        {
            DiffuseMaterial material;
            ImageBrush imageBrush;


            material = new DiffuseMaterial();

            if (TextureMaterialCheckBox.IsChecked ?? false)
            {
                imageBrush = new ImageBrush();
                imageBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/10x10-texture.png"));

                if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                    imageBrush.Opacity = 0.8;

                material.Brush = imageBrush;
            }
            else
            {
                material.Brush = new SolidColorBrush(Color.FromRgb(39, 126, 147));

                if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                    material.Brush.Opacity = 0.8;
            }

            GeometryModel3D geometryModel3D = CustomTrapezoidVisual3D.Content as GeometryModel3D;
            if (geometryModel3D != null)
            {
                geometryModel3D.Material = material;

                if ((SemiTransparentMaterialCheckBox.IsChecked ?? false) || (TextureMaterialCheckBox.IsChecked ?? false))
                    geometryModel3D.BackMaterial = material;
            }
            else
            {
                TrapezoidVisual3D1.Material = material;

                if ((SemiTransparentMaterialCheckBox.IsChecked ?? false) || (TextureMaterialCheckBox.IsChecked ?? false))
                    TrapezoidVisual3D1.BackMaterial = material;
            }
        }

        private void UpdateTrianglesAndNormals()
        {
            GeometryModel3D wireframeModel;
            GeometryModel3D normalsModel;

            TrianglesGroup.Children.Clear();

            MeshGeometry3D geometry;

            var geometryModel3D = CustomTrapezoidVisual3D.Content as GeometryModel3D;
            if (geometryModel3D != null)
                geometry = (MeshGeometry3D)geometryModel3D.Geometry;
            else
                geometry = TrapezoidVisual3D1.Geometry;

            if (ShowTrianglesCheckBox.IsChecked ?? false)
            {
                wireframeModel = Ab3d.Models.WireframeFactory.CreateWireframe(geometry, 2, Color.FromRgb(47, 72, 57), MainViewport) as GeometryModel3D;

                if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                    wireframeModel.BackMaterial = wireframeModel.Material;

                TrianglesGroup.Children.Add(wireframeModel);
            }


            NormalsGroup.Children.Clear();

            if (ShowNormalsCheckBox.IsChecked ?? false)
            {
                normalsModel = Ab3d.Models.WireframeFactory.CreateNormals(geometry, 10, 2, Color.FromRgb(179, 140, 57), true, MainViewport);
                NormalsGroup.Children.Add(normalsModel);
            }


            MeshInspector.MeshGeometry3D = geometry;
        }
    }
}
