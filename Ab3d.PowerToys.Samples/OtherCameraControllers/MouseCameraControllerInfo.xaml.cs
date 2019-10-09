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
using Ab3d.Controls;

namespace Ab3d.PowerToys.Samples.OtherCameraControllers
{
    /// <summary>
    /// Interaction logic for MouseCameraControllerInfo.xaml
    /// </summary>
    public partial class MouseCameraControllerInfo : Page
    {
        private bool _isCustomInfoShown;

        public MouseCameraControllerInfo()
        {
            InitializeComponent();

            MouseCameraController1.Loaded += new RoutedEventHandler(MouseCameraController1_Loaded);
        }

        void MouseCameraController1_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateRotateCameraConditions();
            UpdateMoveCameraConditions();
        }

        private void OnRotateCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateRotateCameraConditions();
        }

        private void OnMoveCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateMoveCameraConditions();
        }

        private void UpdateRotateCameraConditions()
        {
            var rotateConditions = Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.Disabled;

            if (LeftButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed;

            if (MiddleButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.MiddleMouseButtonPressed;

            if (RightButtonCheckBox1.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.RightMouseButtonPressed;


            if (ShiftKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.ShiftKey;

            if (ControlKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.ControlKey;

            if (AltKeyCheckBox1.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.AltKey;

            MouseCameraController1.RotateCameraConditions = rotateConditions;
        }

        private void UpdateMoveCameraConditions()
        {
            var rotateConditions = Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.Disabled;

            if (LeftButtonCheckBox2.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed;

            if (MiddleButtonCheckBox2.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.MiddleMouseButtonPressed;

            if (RightButtonCheckBox2.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.RightMouseButtonPressed;


            if (ShiftKeyCheckBox2.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.ShiftKey;

            if (ControlKeyCheckBox2.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.ControlKey;

            if (AltKeyCheckBox2.IsChecked ?? false)
                rotateConditions |= Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.AltKey;

            MouseCameraController1.MoveCameraConditions = rotateConditions;
        }

        private void ShowCustomInfoButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (_isCustomInfoShown)
            {
                // ClearCustomInfoLines method removes all custom info
                CameraControllerInfo.ClearCustomInfoLines();

                ShowCustomInfoButton.Content = "Show custom info";
                _isCustomInfoShown = false;
            }
            else
            {
                // AddCustomInfoLine method adds custom message with keyboard and mouse button icon to the existing mouse controller info.
                // The first parameter (insertRowIndex) specifies the index of the inserted row at the time of insertion
                // In our example we are inserting two texts both time to index = 0
                // This means that the first line will be inserted first and then the second line will be inserted at position 0 (so the first line will become second)
                CameraControllerInfo.AddCustomInfoLine(0, Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.MiddleMouseButtonPressed, "Custom info text 2");
                CameraControllerInfo.AddCustomInfoLine(0, Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.ShiftKey | Ab3d.Controls.MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed, "Custom info text 1");

                ShowCustomInfoButton.Content = "Hide custom info";
                _isCustomInfoShown = true;
            }
        }
    }
}
