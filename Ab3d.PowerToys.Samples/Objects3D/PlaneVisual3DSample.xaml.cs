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

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for PlaneVisual3DSample.xaml
    /// </summary>
    public partial class PlaneVisual3DSample : Page
    {
        public PlaneVisual3DSample()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(PlaneVisual3DSample_Loaded);
        }

        void PlaneVisual3DSample_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMaterial();
            UpdateTrianglesAndNormals();

            Camera1.Refresh(); // This will measure the models on the scene and reposition the scene camera
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

            PlaneVisual3D1.Material = material;

            if ((SemiTransparentMaterialCheckBox.IsChecked ?? false) || (TextureMaterialCheckBox.IsChecked ?? false))
                PlaneVisual3D1.BackMaterial = material;
        }

        private void UpdateTrianglesAndNormals()
        {
            TrianglesGroup.Children.Clear();


            // When PlaneVisual3D.UseMatrixTransform3D is set to true (by default),
            // then a shared MeshGeometry3D with size (1,1) and at position (0,0,0) is used.
            // This MeshGeometry3D is then transformed by a MatrixTransform3D to produce the desired plane.
            // To get the transformed MeshGeometry3D we need to manually transform the Positions and Normals (or set UseMatrixTransform3D to false).
            var planeMeshGeometry3D = PlaneVisual3D1.Geometry;
            planeMeshGeometry3D = Ab3d.Utilities.MeshUtils.TransformMeshGeometry3D(planeMeshGeometry3D, PlaneVisual3D1.Content.Transform, transformNormals: true);

            if (ShowTrianglesCheckBox.IsChecked ?? false)
            {
                var wireframeModel = Ab3d.Models.WireframeFactory.CreateWireframe(planeMeshGeometry3D, 2, Color.FromRgb(47, 72, 57), MainViewport);

                if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                    wireframeModel.BackMaterial = wireframeModel.Material;

                TrianglesGroup.Children.Add(wireframeModel);
            }


            NormalsGroup.Children.Clear();

            if (ShowNormalsCheckBox.IsChecked ?? false)
            {
                var normalsModel = Ab3d.Models.WireframeFactory.CreateNormals(planeMeshGeometry3D, 10, 2, Color.FromRgb(179, 140, 57), true, MainViewport);
                NormalsGroup.Children.Add(normalsModel);
            }

            MeshInspector.MeshGeometry3D = planeMeshGeometry3D;
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

        private void PlaneVisual3D1_GeometryChanged(object sender, EventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateTrianglesAndNormals();
        }

        private void AlignWithCameraButton_OnClick(object sender, RoutedEventArgs e)
        {
            PlaneVisual3D1.AlignWithCamera(Camera1);

            UpdateTrianglesAndNormals();

            NormalTextBox.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                               "{0:0.0} {1:0.0} {2:0.0}", PlaneVisual3D1.Normal.X, PlaneVisual3D1.Normal.Y, PlaneVisual3D1.Normal.Z);

            HeightDirectionTextBox.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                        "{0:0.0} {1:0.0} {2:0.0}", PlaneVisual3D1.HeightDirection.X, PlaneVisual3D1.HeightDirection.Y, PlaneVisual3D1.HeightDirection.Z);
        }
    }
}

