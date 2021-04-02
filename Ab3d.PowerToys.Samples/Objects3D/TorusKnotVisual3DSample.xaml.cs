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
    /// Interaction logic for TorusKnotVisual3DSample.xaml
    /// </summary>
    public partial class TorusKnotVisual3DSample : Page
    {
        private bool _isUserAskedToReduceSegments;

        public TorusKnotVisual3DSample()
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
            UpdateTrianglesAndNormals();

            Camera1.Refresh(); // This will measure the models on the scene and reposition the scene camera
        }

        private void UpdateTrianglesAndNormals()
        {
            TrianglesGroup.Children.Clear();
            
            // When SphereVisual3D.UseCachedMeshGeometry3D is set to true (by default),
            // then a shared MeshGeometry3D with radius 1 and at center position (0,0,0) is used.
            // This MeshGeometry3D is then transformed by a MatrixTransform3D to produce the desired plane.
            // To get the transformed MeshGeometry3D we need to manually transform the Positions and Normals (or set UseCachedMeshGeometry3D to false).
            var sphereMeshGeometry3D = TorusKnotVisual3D1.Geometry;
            sphereMeshGeometry3D = Ab3d.Utilities.MeshUtils.TransformMeshGeometry3D(sphereMeshGeometry3D, TorusKnotVisual3D1.Content.Transform, transformNormals: true);

            if (ShowTrianglesCheckBox.IsChecked ?? false)
            {
                var wireframeModel = Ab3d.Models.WireframeFactory.CreateWireframe(sphereMeshGeometry3D, 2, Color.FromRgb(47, 72, 57), MainViewport) as GeometryModel3D;
                TrianglesGroup.Children.Add(wireframeModel);
            }


            NormalsGroup.Children.Clear();

            if (ShowNormalsCheckBox.IsChecked ?? false)
            {
                var normalsModel = Ab3d.Models.WireframeFactory.CreateNormals(sphereMeshGeometry3D, 10, 2, Color.FromRgb(179, 140, 57), true, MainViewport);
                NormalsGroup.Children.Add(normalsModel);
            }


            MeshInspector.MeshGeometry3D = TorusKnotVisual3D1.Geometry;

            UpdateInfoTextBlock();
        }
        
        private void UpdateInfoTextBlock()
        {
            if (TorusKnotVisual3D1.Geometry == null)
            {
                InfoTextBlock.Text = "";
                return;
            }

            InfoTextBlock.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Positions: {0:#,##0}\r\nTriangles: {1:#,##0}", 
                TorusKnotVisual3D1.Geometry.Positions.Count, TorusKnotVisual3D1.Geometry.TriangleIndices.Count / 3);
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
        
        private void ShowMeshInspectorCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            if (TorusKnotVisual3D1.Geometry != null && TorusKnotVisual3D1.Geometry.Positions.Count > 1000 && !_isUserAskedToReduceSegments)
            {
                var result = MessageBox.Show(
                    "MeshInspectorOverlay will be very slow because number of positions is very high. It is recommended to lower the number of segments before showing MeshInspectorOverlay.\r\n\r\nDo you want to reduce the number of segments before showing MeshInspectorOverlay?",
                    "MeshInspectorOverlay", MessageBoxButton.YesNo, MessageBoxImage.Question);

                _isUserAskedToReduceSegments = true;

                if (result != MessageBoxResult.No)
                {
                    ShowMeshInspectorCheckBox.IsChecked = false;
                    return;
                }
            }

            MeshInspector.Visibility = Visibility.Visible;
        }

        private void ShowMeshInspectorCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            MeshInspector.Visibility = Visibility.Collapsed;
        }
    }
}

