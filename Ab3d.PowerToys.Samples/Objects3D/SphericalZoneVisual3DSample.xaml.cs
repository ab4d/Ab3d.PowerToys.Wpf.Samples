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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using Ab3d.PowerToys.Samples.UseCases;
using System.Collections.ObjectModel;
using Ab3d.Common.EventManager3D;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for SphericalZoneVisual3DSample.xaml
    /// </summary>
    public partial class SphericalZoneVisual3DSample : Page
    {
        public SphericalZoneVisual3DSample()
        {
            InitializeComponent();

            DirectionComboBox.ItemsSource = new Vector3D[] { new Vector3D(0, 1, 0), new Vector3D(1, 0, 0), new Vector3D(0, 0, -1), new Vector3D(0.7, 0.7, 0) };
            DirectionComboBox.SelectedIndex = 0;

            CircularRadiusComboBox.ItemsSource = new double[] { 40, 60, 80 };
            CircularRadiusComboBox.SelectedIndex = 2;

            DirectionalRadiusComboBox.ItemsSource = new double[] { 40, 60, 80 };
            DirectionalRadiusComboBox.SelectedIndex = 2;

            this.Loaded += new RoutedEventHandler(SphericalZoneVisual3DSample_Loaded);
        }

        void SphericalZoneVisual3DSample_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMaterial();
            UpdateTrianglesAndNormals();

            Camera1.Refresh(); // This will measure the models on the scene and reposition the scene camera
        }

        private void UpdateMaterial()
        {
            var material = new DiffuseMaterial();

            if (TextureMaterialCheckBox.IsChecked ?? false)
            {
                var imageBrush = new ImageBrush();
                imageBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/10x10-texture.png"));

                // We need to set Tile mode for images used on spheres
                // Otherwise the image does not continue nicely (there is a linear edge) when the sphere comes around itself
                imageBrush.TileMode = TileMode.Tile;

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

            SphericalZoneVisual3D1.Material = material;

            if ((SemiTransparentMaterialCheckBox.IsChecked ?? false) || (TextureMaterialCheckBox.IsChecked ?? false))
                SphericalZoneVisual3D1.BackMaterial = material;
            else
                SphericalZoneVisual3D1.BackMaterial = null;
        }

        private void UpdateTrianglesAndNormals()
        {
            TrianglesGroup.Children.Clear();

            if (ShowTrianglesCheckBox.IsChecked ?? false)
            {
                var wireframeModel = Ab3d.Models.WireframeFactory.CreateWireframe(SphericalZoneVisual3D1.Geometry, 2, Color.FromRgb(47, 72, 57), MainViewport);

                if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                    wireframeModel.BackMaterial = wireframeModel.Material;

                TrianglesGroup.Children.Add(wireframeModel);
            }


            NormalsGroup.Children.Clear();

            if (ShowNormalsCheckBox.IsChecked ?? false)
            {
                var normalsModel = Ab3d.Models.WireframeFactory.CreateNormals(SphericalZoneVisual3D1.Geometry, 10, 2, Color.FromRgb(179, 140, 57), true, MainViewport);
                NormalsGroup.Children.Add(normalsModel);
            }

            MeshInspector.MeshGeometry3D = SphericalZoneVisual3D1.Geometry;
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

        private void SphericalZoneVisual3D1_GeometryChanged(object sender, EventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateTrianglesAndNormals();
        }

        private void DirectionComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            SphericalZoneVisual3D1.Direction = (Vector3D)DirectionComboBox.SelectedItem;
        }
        
        private void CircularRadiusComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            SphericalZoneVisual3D1.CircularRadius = (double)CircularRadiusComboBox.SelectedItem;
        }
        
        private void DirectionalRadiusComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            SphericalZoneVisual3D1.DirectionalRadius = (double)DirectionalRadiusComboBox.SelectedItem;
        }
    }
}

