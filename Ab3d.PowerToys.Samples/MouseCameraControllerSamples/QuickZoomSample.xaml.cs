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
    /// Interaction logic for QuickZoomSample.xaml
    /// </summary>
    public partial class QuickZoomSample : Page
    {
        public QuickZoomSample()
        {
            InitializeComponent();

            QuickZoomMaxZoomInFactorInfoControl.InfoText =
@"The maximum zoom in factor is reached when the user moves the mouse for the QuickZoomMaxFactorScreenDistance distance in the forward direction.

If user moves the mouse farther away the zooming is not performed any more. 

Default value is 20 and means that the max zoom in factor is 20x - the Camera.Distance will be reduced to its 0.05 (1/20) initial value.";

            QuickZoomZoomOutFactorInfoControl.InfoText =
@"The property defines the zoom out factor that is used when the mouse travels for the QuickZoomMaxFactorScreenDistance amount in the backwards mouse direction.

Zoom out does not stop at this zoom factor as with zooming in (see QuickZoomMaxZoomInFactor).

Default value is 10 and means that the zoom out factor is 10x - the Camera.Distance will be increased to 10 times its initial value after the mouse moves for the QuickZoomMaxFactorScreenDistance amount in the backwards mouse direction.";

            QuickZoomMaxFactorScreenDistanceInfoControl.InfoText =
@"The QuickZoomMaxFactorScreenDistance defines how much the mouse needs to travel in the forward or backward direction to reach the QuickZoomMaxZoomInFactor or QuickZoomZoomOutFactor.

Default value is 200.";


            QuickZoomMaxZoomInFactorComboBox.ItemsSource         = new double[] {2, 5, 10, 20, 100};
            QuickZoomZoomOutFactorComboBox.ItemsSource           = new double[] {2, 5, 10, 20, 100};
            QuickZoomMaxFactorScreenDistanceComboBox.ItemsSource = new double[] {20, 50, 100, 200, 300};

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                QuickZoomMaxZoomInFactorComboBox.SelectedIndex =         3;
                QuickZoomZoomOutFactorComboBox.SelectedIndex           = 2;
                QuickZoomMaxFactorScreenDistanceComboBox.SelectedIndex = 2;
            };
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

        private void OnQuickZoomCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            UpdateQuickZoomConditions();
        }

        private void UpdateQuickZoomConditions()
        {
            var rotateConditions = MouseCameraController.MouseAndKeyboardConditions.Disabled;

            if (LeftButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed;

            if (MiddleButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.MiddleMouseButtonPressed;

            if (RightButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.RightMouseButtonPressed;


            if (ShiftKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.ShiftKey;

            if (ControlKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.ControlKey;

            if (AltKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.AltKey;

            MouseCameraController1.QuickZoomConditions = rotateConditions;
        }

        private void QuickZoomMaxZoomInFactorComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (!this.IsLoaded)
            //    return;

            MouseCameraController1.QuickZoomMaxZoomInFactor = (double)QuickZoomMaxZoomInFactorComboBox.SelectedItem;
        }

        private void QuickZoomZoomOutFactorComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (!this.IsLoaded)
            //    return;

            MouseCameraController1.QuickZoomZoomOutFactor = (double)QuickZoomZoomOutFactorComboBox.SelectedItem;
        }

        private void QuickZoomMaxFactorScreenDistanceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (!this.IsLoaded)
            //    return;

            MouseCameraController1.QuickZoomMaxFactorScreenDistance = (double) QuickZoomMaxFactorScreenDistanceComboBox.SelectedItem;
        }

        private void ZoomModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var zoomMode = MouseCameraController.CameraZoomMode.Viewport3DCenter; // Viewport3DCenter is default ZoomMode

            Point3D? rotationCenterPosition    = null; // RotationCenterPosition is nullable Point3D type
            bool     rotateAroundMousePosition = false;

            switch (ZoomModeComboBox.SelectedIndex)
            {
                case 0:
                    // Default zoom mode is 
                    zoomMode = MouseCameraController.CameraZoomMode.Viewport3DCenter; // Viewport3DCenter is default ZoomMode
                    break;

                case 1:
                    zoomMode = MouseCameraController.CameraZoomMode.CameraRotationCenterPosition;
                    rotationCenterPosition = RedBox.CenterPosition;
                    break;

                case 2:
                    zoomMode = MouseCameraController.CameraZoomMode.CameraRotationCenterPosition;
                    rotationCenterPosition = YellowBox.CenterPosition;
                    break;

                case 3:
                    zoomMode = MouseCameraController.CameraZoomMode.CameraRotationCenterPosition;
                    rotationCenterPosition = OrangeBox.CenterPosition;
                    break;

                case 4:
                    // MousePosition zoom mode zooms into the 3D position that is "behind" current mouse position
                    zoomMode = MouseCameraController.CameraZoomMode.MousePosition;
                    rotateAroundMousePosition = true;
                    break;

                default:
                    break;
            }

            MouseCameraController1.ZoomMode = zoomMode;

            MouseCameraController1.RotateAroundMousePosition = rotateAroundMousePosition;
            Camera1.RotationCenterPosition = rotationCenterPosition;
        }

        private void OnShowQuickZoomMarkerCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            MouseCameraController1.ShowQuickZoomMarker = ShowQuickZoomMarkerCheckBox.IsChecked ?? false;
        }
    }
}
