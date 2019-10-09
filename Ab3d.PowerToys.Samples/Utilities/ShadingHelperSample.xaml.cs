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

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for ShadingHelperSample.xaml
    /// </summary>
    public partial class ShadingHelperSample : Page
    {
        private GeometryModel3D _originalModel;

        public ShadingHelperSample()
        {
            InitializeComponent();

            _originalModel = Ab3d.Models.Model3DFactory.CreateCylinder(new Point3D(0, 0, 0), 80, 80, 15, true, null);

            this.Loaded += new RoutedEventHandler(ShadingHelperSample_Loaded);
        }

        void ShadingHelperSample_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateModel();

            Camera1.Refresh(); // This will measure the models on the scene and reposition the scene camera
        }

        private GeometryModel3D ApplyShading()
        {
            GeometryModel3D model;
            MeshGeometry3D geometry;

            if (SmoothRadioButton.IsChecked ?? false)
            {
                model = _originalModel.Clone();
                Ab3d.Utilities.ShadingHelper.SmoothModel3D(model);
            }
            else if (FlatRadioButton.IsChecked ?? false)
            {
                model = _originalModel.Clone();
                Ab3d.Utilities.ShadingHelper.FlattenModel3D(model);
            }
            else
            {
                model = _originalModel;
            }

            geometry = model.Geometry as MeshGeometry3D;

            PositionsTextBlock.Text = string.Format("Positions count: {0}", geometry.Positions.Count);
            IndicesTextBlock.Text = string.Format("TriangleIndices count: {0}", geometry.TriangleIndices.Count);

            return model;
        }

        private void UpdateModel()
        {
            DiffuseMaterial material;
            ImageBrush imageBrush;
            GeometryModel3D wireframeModel;
            GeometryModel3D normalsModel;
            GeometryModel3D model;


            material = new DiffuseMaterial();

            if (TextureMaterialCheckBox.IsChecked ?? false)
            {
                imageBrush = new ImageBrush();
                imageBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/PowerToysTexture.png"));

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


            model = ApplyShading();

            model.Material = material;

            if ((SemiTransparentMaterialCheckBox.IsChecked ?? false) || (TextureMaterialCheckBox.IsChecked ?? false))
                model.BackMaterial = material;


            MainModel3DGroup.Children.Clear();

            if (ShowTrianglesCheckBox.IsChecked ?? false)
            {
                wireframeModel = Ab3d.Models.WireframeFactory.CreateWireframe(model.Geometry as MeshGeometry3D, 2, Color.FromRgb(47, 72, 57), MainViewport);

                if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                    wireframeModel.BackMaterial = wireframeModel.Material;

                MainModel3DGroup.Children.Add(wireframeModel);
            }

            MainModel3DGroup.Children.Add(model);

            if (ShowNormalsCheckBox.IsChecked ?? false)
            {
                normalsModel = Ab3d.Models.WireframeFactory.CreateNormals(model.Geometry as MeshGeometry3D, 10, 2, Color.FromRgb(179, 140, 57), true, MainViewport);
                MainModel3DGroup.Children.Add(normalsModel);
            }

            if ((SmoothRadioButton.IsChecked ?? false) && (TextureMaterialCheckBox.IsChecked ?? false))
                MessageBox.Show("When smoothing mesh geometry the Positions Point3DCollection is optimized - all the same Positions are removed from the collection. That means that the size of TextureCoordinates collection is also reduced.\r\n\nThis can lead to wrongly mapped textures!", "Smoothing warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OnViewSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateModel();
        }
    }
}

