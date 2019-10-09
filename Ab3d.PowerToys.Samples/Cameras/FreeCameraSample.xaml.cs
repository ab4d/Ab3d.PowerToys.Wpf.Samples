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

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for FreeCameraSample.xaml
    /// </summary>
    public partial class FreeCameraSample : Page
    {
        public FreeCameraSample()
        {
            InitializeComponent();

            //Camera1.StartRotation(45, 0);
        }

        private void ResetCameraButton_OnClick(object sender, RoutedEventArgs e)
        {
            ResetCamera();
        }

        private void ResetCamera()
        {
            Camera1.TargetPosition = new Point3D(0, 10, 0);
            Camera1.CameraPosition = new Point3D(0, 10, -100);
            Camera1.UpDirection = new Vector3D(0, 1, 0);
        }

        private void OnRotationUpAxisCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;


            // Start using new RotationUpAxis from the initial camera position
            ResetCamera();


            Vector3D? newRotationUpAxis;

            if (YAxisRadioButton.IsChecked ?? false)
            {
                newRotationUpAxis = new Vector3D(0, 1, 0);
            }
            else if (ZAxisRadioButton.IsChecked ?? false)
            {
                newRotationUpAxis = new Vector3D(0, 0, 1);

                // If we would like to use tha camera so that Z axis is up axis in the scene,
                // then we also need to update the CameraPosition and UpDirection
                Camera1.TargetPosition = new Point3D(0, 0, 10);
                Camera1.CameraPosition = new Point3D(0, -100, 10);
                Camera1.UpDirection    = new Vector3D(0, 0, 1); // Set Z as up direction
            }
            else
            {
                newRotationUpAxis = null;
            }

            Camera1.RotationUpAxis = newRotationUpAxis;
        }
    }
}
