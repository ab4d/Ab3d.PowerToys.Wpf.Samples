﻿using System;
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
    /// Interaction logic for IcosphereVisual3DSample.xaml
    /// </summary>
    public partial class IcosphereVisual3DSample : Page
    {
        public IcosphereVisual3DSample()
        {
            InitializeComponent();

            // Note that we have set UseCachedMeshGeometry3DCheckBox on SphereVisual3D to false
            // This means that we will not use cached MeshGeometry3D and change its transformation to set the size of the sphere,
            // but will instead regenerate the MeshGeometry3D for each sphere.
            // This is needed because we are showing triangles and normals - we need MeshGeometry3D for that.

            this.Loaded += new RoutedEventHandler(IcosphereVisual3DSample_Loaded);
        }

        void IcosphereVisual3DSample_Loaded(object sender, RoutedEventArgs e)
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
                imageBrush.ViewportUnits = BrushMappingMode.Absolute;

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

            IcosphereVisual3D1.Material = material;

            if ((SemiTransparentMaterialCheckBox.IsChecked ?? false) || (TextureMaterialCheckBox.IsChecked ?? false))
                IcosphereVisual3D1.BackMaterial = material;
        }

        private void UpdateTrianglesAndNormals()
        {
            TrianglesGroup.Children.Clear();
            NormalsGroup.Children.Clear();


            // When SphereVisual3D.UseCachedMeshGeometry3D is set to true (by default),
            // then a shared MeshGeometry3D with radius 1 and at center position (0,0,0) is used.
            // This MeshGeometry3D is then transformed by a MatrixTransform3D to produce the desired plane.
            // To get the transformed MeshGeometry3D we need to manually transform the Positions and Normals (or set UseCachedMeshGeometry3D to false).
            var icosphereMeshGeometry3D = IcosphereVisual3D1.Geometry;
            icosphereMeshGeometry3D = Ab3d.Utilities.MeshUtils.TransformMeshGeometry3D(icosphereMeshGeometry3D, IcosphereVisual3D1.Content.Transform, transformNormals: true);


            // When subdivisions is more than 5 (positions count > 20000), then prevent showing triangles, normals and MeshInspector
            if (icosphereMeshGeometry3D.Positions.Count > 20000)
            {
                ShowTrianglesCheckBox.IsEnabled = false;
                ShowNormalsCheckBox.IsEnabled = false;
                ShowMeshInspectorCheckBox.IsEnabled = false;

                MeshInspector.MeshGeometry3D = null;
            }
            else
            {
                ShowTrianglesCheckBox.IsEnabled = true;
                ShowNormalsCheckBox.IsEnabled = true;
                ShowMeshInspectorCheckBox.IsEnabled = true;

                if (ShowTrianglesCheckBox.IsChecked ?? false)
                {
                    var wireframeModel = Ab3d.Models.WireframeFactory.CreateWireframe(icosphereMeshGeometry3D, 2, Color.FromRgb(47, 72, 57), MainViewport) as GeometryModel3D;

                    if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                        wireframeModel.BackMaterial = wireframeModel.Material;

                    TrianglesGroup.Children.Add(wireframeModel);
                }


                if (ShowNormalsCheckBox.IsChecked ?? false)
                {
                    var normalsModel = Ab3d.Models.WireframeFactory.CreateNormals(icosphereMeshGeometry3D, 10, 2, Color.FromRgb(179, 140, 57), true, MainViewport);
                    NormalsGroup.Children.Add(normalsModel);
                }


                MeshInspector.MeshGeometry3D = icosphereMeshGeometry3D;
            }

            PositionsCountTextBlock.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Positions count: {0:#,##0}\r\nTriangles count: {1:#,##0}", icosphereMeshGeometry3D.Positions.Count, icosphereMeshGeometry3D.TriangleIndices.Count / 3);
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

        private void IcosphereVisual3D1_GeometryChanged(object sender, EventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateTrianglesAndNormals();
        }
    }
}

