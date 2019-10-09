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
    /// Interaction logic for CameraTypeSample.xaml
    /// </summary>
    public partial class CameraTypeSample : Page
    {
        public CameraTypeSample()
        {
            InitializeComponent();
        }

        private void OnCameraTypeChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (PerspectiveCameraRadioButton.IsChecked ?? false)
            { 
                if (Camera1.CameraType != Ab3d.Cameras.BaseCamera.CameraTypes.PerspectiveCamera)
                    Camera1.CameraType = Ab3d.Cameras.BaseCamera.CameraTypes.PerspectiveCamera;
            }
            else if (OrthographicCameraRadioButton.IsChecked ?? false)
            {
                if (Camera1.CameraType != Ab3d.Cameras.BaseCamera.CameraTypes.OrthographicCamera)
                    Camera1.CameraType = Ab3d.Cameras.BaseCamera.CameraTypes.OrthographicCamera;
            }
        }
    }
}
