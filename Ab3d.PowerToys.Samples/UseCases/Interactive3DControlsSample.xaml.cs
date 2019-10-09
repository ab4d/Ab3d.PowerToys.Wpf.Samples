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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for Interactive3DControlsSample.xaml
    /// </summary>
    public partial class Interactive3DControlsSample : Page
    {
        private BaseModelVisual3D _currentModel3D;

        public Interactive3DControlsSample()
        {
            InitializeComponent();

            // Important:
            // Interactive controls do not work when Viewport2DVisual3D is rendered with Ab3d.DXEngine.
            // To use Viewport2DVisual3D in Ab3d.DXEngine create a new Viewport3D with only Viewport2DVisual3D and put it on top of DXViewportView control.


            // Set MeshGeometry3D that will show the interactive WPF controls
            InteractiveVisual3D.Geometry = new Ab3d.Meshes.PlaneMesh3D(centerPosition: new Point3D(50, 15, 20),
                                                                       planeNormal: new Vector3D(0, 0, 1),
                                                                       planeHeightDirection: new Vector3D(0, 1, 0),
                                                                       size: new Size(80, 110),
                                                                       widthSegments: 1, heightSegments: 1).Geometry;

            InteractiveVisual3D.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -20));


            // Subscribe to changes in InteractiveUserControl
            InteractiveUserControl1.ObjectSettingsChanged += delegate(object sender, EventArgs args)
            {
                UpdateCurrentModel3D(); 
            };

            InteractiveUserControl1.AddNewButtonClicked += delegate(object sender, EventArgs args)
            {
                CurrentModelPlaceholder.Children.Remove(_currentModel3D);
                OlderModelsPlaceholder.Children.Add(_currentModel3D);
                
                MoveAllOlderModels(80);

                _currentModel3D = null; // This will create a new model in UpdateCurrentModel3D
                UpdateCurrentModel3D();
            };

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                UpdateCurrentModel3D();
            };
        }

        private void UpdateCurrentModel3D()
        {
            if (InteractiveUserControl1.IsBox)
            {
                if (!(_currentModel3D is BoxVisual3D))
                {
                    CurrentModelPlaceholder.Children.Clear();

                    _currentModel3D = new BoxVisual3D()
                    {
                        Size = new Size3D(50, 50, 50),
                        CenterPosition = new Point3D(-50, 0, 0)
                    };

                    CurrentModelPlaceholder.Children.Add(_currentModel3D);
                }
            }
            else
            {
                if (!(_currentModel3D is SphereVisual3D))
                {
                    CurrentModelPlaceholder.Children.Clear();

                    _currentModel3D = new SphereVisual3D()
                    {
                        Radius = 25,
                        CenterPosition = new Point3D(-50, 0, 0)
                    };

                    CurrentModelPlaceholder.Children.Add(_currentModel3D);
                }
            }

            var selectedColor = InteractiveUserControl1.SelectedColor;
            var diffuseMaterial = new DiffuseMaterial(new SolidColorBrush(selectedColor));

            _currentModel3D.Material = diffuseMaterial;
        }

        private void MoveAllOlderModels(double offsetAmount)
        {
            foreach (var baseModelVisual3D in OlderModelsPlaceholder.Children)
            {
                var translateTransform3D = baseModelVisual3D.Transform as TranslateTransform3D;
                if (translateTransform3D == null)
                {
                    translateTransform3D = new TranslateTransform3D();
                    baseModelVisual3D.Transform = translateTransform3D;
                }

                translateTransform3D.OffsetZ -= offsetAmount;
            }
        }
    }
}
