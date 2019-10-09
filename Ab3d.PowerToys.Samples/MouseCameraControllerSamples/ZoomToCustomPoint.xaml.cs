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
using Ab3d.Cameras;
using Ab3d.Controls;

namespace Ab3d.PowerToys.Samples.MouseCameraControllerSamples
{
    /// <summary>
    /// Interaction logic for ZoomToCustomPoint.xaml
    /// </summary>
    public partial class ZoomToCustomPoint : Page
    {
        public ZoomToCustomPoint()
        {
            InitializeComponent();

            MouseCameraControllerInfo1.AddCustomInfoLine(0, MouseCameraController.MouseAndKeyboardConditions.MiddleMouseButtonPressed, 
                                                         "Use mouse wheel to zoom or use pinch zoom on touch display");

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                UpdateCurrentZoomMode();
            };
        }

        private void RotationCenterPositionRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateCurrentZoomMode();
        }

        private void UpdateCurrentZoomMode()
        {
            var zoomMode = MouseCameraController.CameraZoomMode.Viewport3DCenter; // Viewport3DCenter is default ZoomMode

            Point3D? rotationCenterPosition = null; // RotationCenterPosition is nullable Point3D type
            bool rotateAroundMousePosition = false;


            if (MousePositionRadioButton.IsChecked ?? false)
            {
                // MousePosition zoom mode zooms into the 3D position that is "behind" current mouse position
                zoomMode = MouseCameraController.CameraZoomMode.MousePosition;
                rotateAroundMousePosition = true;
            }
            else if (CenterOfViewport3DRadioButton.IsChecked ?? false)
            {
                // Default zoom mode is 
                zoomMode = MouseCameraController.CameraZoomMode.Viewport3DCenter; // Viewport3DCenter is default ZoomMode
            }
            else if (RedBoxRadioButton.IsChecked ?? false)
            {
                zoomMode = MouseCameraController.CameraZoomMode.CameraRotationCenterPosition;
                rotationCenterPosition = RedBox.CenterPosition;
            }
            else if (YellowBoxRadioButton.IsChecked ?? false)
            {
                zoomMode = MouseCameraController.CameraZoomMode.CameraRotationCenterPosition;
                rotationCenterPosition = YellowBox.CenterPosition;
            }
            else if (OrangeBoxRadioButton.IsChecked ?? false)
            {
                zoomMode = MouseCameraController.CameraZoomMode.CameraRotationCenterPosition;
                rotationCenterPosition = OrangeBox.CenterPosition;
            }

            MouseCameraController1.ZoomMode = zoomMode;

            MouseCameraController1.RotateAroundMousePosition = rotateAroundMousePosition;
            Camera1.RotationCenterPosition = rotationCenterPosition;
        }

        private void ResetCameraButton_OnClick(object sender, RoutedEventArgs e)
        {
            Camera1.BeginInit();

            Camera1.Heading = 30;
            Camera1.Attitude = -20;
            Camera1.Bank = 0;
            Camera1.Distance = 200;
            Camera1.TargetPosition = new Point3D(0, 0, 0);
            Camera1.Offset = new Vector3D(0, 0, 0);

            Camera1.EndInit();
        }
    }
}
