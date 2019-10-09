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
    /// Interaction logic for SphereVisual3DSample.xaml
    /// </summary>
    public partial class SphereVisual3DSample : Page
    {
        public SphereVisual3DSample()
        {
            InitializeComponent();

            // Note that we have set UseCachedMeshGeometry3DCheckBox on SphereVisual3D to false
            // This means that we will not use cached MeshGeometry3D and change its transformation to set the size of the sphere,
            // but will instead regenerate the MeshGeometry3D for each sphere.
            // This is needed because we are showing triangles and normals - we need MeshGeometry3D for that.

            this.Loaded += new RoutedEventHandler(SphereVisual3DSample_Loaded);
        }

        void SphereVisual3DSample_Loaded(object sender, RoutedEventArgs e)
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

            SphereVisual3D1.Material = material;

            if ((SemiTransparentMaterialCheckBox.IsChecked ?? false) || (TextureMaterialCheckBox.IsChecked ?? false))
                SphereVisual3D1.BackMaterial = material;
        }

        private void UpdateTrianglesAndNormals()
        {
            TrianglesGroup.Children.Clear();


            // When SphereVisual3D.UseCachedMeshGeometry3D is set to true (by default),
            // then a shared MeshGeometry3D with radius 1 and at center position (0,0,0) is used.
            // This MeshGeometry3D is then transformed by a MatrixTransform3D to produce the desired plane.
            // To get the transformed MeshGeometry3D we need to manually transform the Positions and Normals (or set UseCachedMeshGeometry3D to false).
            var sphereMeshGeometry3D = SphereVisual3D1.Geometry;
            sphereMeshGeometry3D = Ab3d.Utilities.MeshUtils.TransformMeshGeometry3D(sphereMeshGeometry3D, SphereVisual3D1.Content.Transform, transformNormals: true);

            if (ShowTrianglesCheckBox.IsChecked ?? false)
            {
                var wireframeModel = Ab3d.Models.WireframeFactory.CreateWireframe(sphereMeshGeometry3D, 2, Color.FromRgb(47, 72, 57), MainViewport) as GeometryModel3D;

                if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                    wireframeModel.BackMaterial = wireframeModel.Material;

                TrianglesGroup.Children.Add(wireframeModel);
            }


            NormalsGroup.Children.Clear();

            if (ShowNormalsCheckBox.IsChecked ?? false)
            {
                var normalsModel = Ab3d.Models.WireframeFactory.CreateNormals(sphereMeshGeometry3D, 10, 2, Color.FromRgb(179, 140, 57), true, MainViewport);
                NormalsGroup.Children.Add(normalsModel);
            }


            MeshInspector.MeshGeometry3D = SphereVisual3D1.Geometry;
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

        private void SphereVisual3D1_GeometryChanged(object sender, EventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateTrianglesAndNormals();
        }
    }
}

