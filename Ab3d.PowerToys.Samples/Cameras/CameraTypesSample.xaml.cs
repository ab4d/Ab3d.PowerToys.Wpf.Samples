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

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for CameraTypesSample.xaml
    /// </summary>
    public partial class CameraTypesSample : Page
    {
        public CameraTypesSample()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Create wrireframe models from the scene and the person
            WireframeModelGroup1.Children.Add(CreateWireframe(TargetPositionCameraViewport));
            WireframeModelGroup2.Children.Add(CreateWireframe(FreeCameraViewport));
            WireframeModelGroup3.Children.Add(CreateWireframe(SceneCameraViewport));
            WireframeModelGroup4.Children.Add(CreateWireframe(ThirdPersonCameraViewport));
            WireframeModelGroup5.Children.Add(CreateWireframe(FirstPersonCameraViewport));

            // Add wirefrome 3D boxes to show the area the camera is pointing at
            WireframeModelGroup3.Children.Add(Ab3d.Models.Line3DFactory.CreateWireBox3D(SceneModel1.Content.Bounds, 2, Colors.Red, SceneCameraViewport));
            WireframeModelGroup4.Children.Add(Ab3d.Models.Line3DFactory.CreateWireBox3D(PersonModel1.Content.Bounds, 2, Colors.Red, ThirdPersonCameraViewport));

            TargetPositionCamera1.Refresh();
        }

        private Model3DGroup CreateWireframe(Viewport3D viewport3D)
        {
            var wireframeGroup = new Model3DGroup();

            var wireframe = Ab3d.Models.WireframeFactory.CreateWireframe(SceneModel1.Content, 1, true, Colors.Black, viewport3D);
            wireframeGroup.Children.Add(wireframe);

            wireframe = Ab3d.Models.WireframeFactory.CreateWireframe(PersonModel1.Content, 1, true, Colors.Black, viewport3D);
            wireframeGroup.Children.Add(wireframe);

            return wireframeGroup;
        }




        private void InvokePrint(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new System.Windows.Controls.PrintDialog();

            // If debugger stops on exception in the next line, just click continue
            if (printDialog.ShowDialog() == true)
            {
                // Adjust settings before printing
                PrintButton.Visibility = Visibility.Collapsed;

                CameraControlPanel1.Visibility = System.Windows.Visibility.Collapsed;
                CameraControlPanel2.Visibility = System.Windows.Visibility.Collapsed;
                CameraControlPanel3.Visibility = System.Windows.Visibility.Collapsed;
                CameraControlPanel4.Visibility = System.Windows.Visibility.Collapsed;
                CameraControlPanel5.Visibility = System.Windows.Visibility.Collapsed;
                
                MainGrid.Margin = new Thickness(40, 20, 40, 20);

                //MainGrid.RenderTransform = new ScaleTransform(0.66, 0.66);
                
                
                
                printDialog.PrintVisual(MainGrid, "Ab3d.PowerToys camera types");


                // Reset settings back
                MainGrid.Margin = new Thickness(0, 0, 10, 0);
                MainGrid.RenderTransform = null;

                CameraControlPanel1.Visibility = System.Windows.Visibility.Visible;
                CameraControlPanel2.Visibility = System.Windows.Visibility.Visible;
                CameraControlPanel3.Visibility = System.Windows.Visibility.Visible;
                CameraControlPanel4.Visibility = System.Windows.Visibility.Visible;
                CameraControlPanel5.Visibility = System.Windows.Visibility.Visible;

                PrintButton.Visibility = Visibility.Visible;
            }
        }
    }
}
