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
    /// Interaction logic for TubeVisual3DSample.xaml
    /// </summary>
    public partial class TubeVisual3DSample : Page
    {
        public TubeVisual3DSample()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(TubeVisual3D_Loaded);
        }

        void TubeVisual3D_Loaded(object sender, RoutedEventArgs e)
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

            TubeVisual3D1.Material = material;

            if ((SemiTransparentMaterialCheckBox.IsChecked ?? false) || (TextureMaterialCheckBox.IsChecked ?? false))
                TubeVisual3D1.BackMaterial = material;
            else
                TubeVisual3D1.BackMaterial = null;
        }

        private void UpdateTrianglesAndNormals()
        {
            GeometryModel3D wireframeModel;
            GeometryModel3D normalsModel;

            TrianglesGroup.Children.Clear();

            if (ShowTrianglesCheckBox.IsChecked ?? false)
            {
                wireframeModel = Ab3d.Models.WireframeFactory.CreateWireframe(TubeVisual3D1.Geometry, 2, Color.FromRgb(47, 72, 57), MainViewport);

                if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                    wireframeModel.BackMaterial = wireframeModel.Material;

                TrianglesGroup.Children.Add(wireframeModel);
            }


            NormalsGroup.Children.Clear();

            if (ShowNormalsCheckBox.IsChecked ?? false)
            {
                normalsModel = Ab3d.Models.WireframeFactory.CreateNormals(TubeVisual3D1.Geometry, 10, 2, Color.FromRgb(179, 140, 57), true, MainViewport);
                NormalsGroup.Children.Add(normalsModel);
            }

            MeshInspector.MeshGeometry3D = TubeVisual3D1.Geometry;
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

        private void HeightDirectionCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            Vector3D heightDirection;

            if (HeightDirectionRightRadioButton.IsChecked ?? false)
                heightDirection = new Vector3D(1, 0, 0);
            else if (HeightDirectionForwardRadioButton.IsChecked ?? false)
                heightDirection = new Vector3D(0, 0, -1);
            else // if (HeightDirectionUpRadioButton.IsChecked ?? false)
                heightDirection = new Vector3D(0, 1, 0);

            TubeVisual3D1.HeightDirection = heightDirection;
        }
    }
}

