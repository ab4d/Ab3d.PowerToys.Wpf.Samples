using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ab3d.Controls;

namespace Ab3d.PowerToys.Samples.MouseCameraControllerSamples
{
    /// <summary>
    /// Interaction logic for MainMouseCameraControllerProperties.xaml
    /// </summary>
    public partial class MainMouseCameraControllerProperties : Page
    {
        public MainMouseCameraControllerProperties()
        {
            InitializeComponent();

            EventsSourceElementInfoControl.InfoText =
@"EventsSourceElement defines an element that is used for mouse and touch events. 

Usually EventsSourceElement is set to a Border element that is parent of Viewport3D. The Border needs to have its Background property set. This allows rotating and moving the camera even when mouse is not over 3D objects (but is over specified Border element).

If EventsSourceElement is not set or is set to Viewport3D, then rotation and movement is possible only when mouse is over 3D objects.

This can be tried with selecting Viewport3D RadioBox below (instead of Border).";

            UseMousePositionForMovementSpeedInfoControl.InfoText =
@"When UseMousePositionForMovementSpeed is true (CheckBox is checked) then the camera movement speed is determined by the distance to the 3D object behind the mouse. When no 3D object is behind the mouse or when UseMousePositionForMovementSpeed is set to false, then movement speed is determined by the distance from the camera to the TargetPosition is used. Default value is true.";

            MaxCameraDistanceInfoControl.InfoText =
@"When MaxCameraDistance is set to a value that is not double.NaN, than it specifies the maximum Distance of the camera or the maximum CameraWidth when OrthographicCamera is used.
This property can be set to a reasonable number to prevent float imprecision when the camera distance is very big. Default value is double.NaN.";


            MouseCameraController1.Loaded += MouseCameraController1_Loaded;
        }

        void MouseCameraController1_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateRotateCameraConditions();
            UpdateMoveCameraConditions();
        }

        private void OnRotateCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            UpdateRotateCameraConditions();
        }

        private void OnMoveCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            UpdateMoveCameraConditions();
        }

        private void OnUseMousePositionForMovementSpeedCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            MouseCameraController1.UseMousePositionForMovementSpeed = UseMousePositionForMovementSpeedCheckBox.IsChecked ?? false;
        }
        


        private void ParentElementRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Set ViewportBorder element that is Viewport3D parent as source of mouse events.
            // When the parent element have Background set (can be also Transparent), 
            // then user can rotate and move the camera also when mouse is not over 3D objects.
            MouseCameraController1.EventsSourceElement = ViewportBorder;
        }

        private void Viewport3DRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Set Viewport3D as source for mouse events.
            // This will rotate and move the camera only when mouse is over 3D objects.
            MouseCameraController1.EventsSourceElement = MainViewport; // This is the same as setting EventsSourceElement to null
        }

        private void UpdateRotateCameraConditions()
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

            MouseCameraController1.RotateCameraConditions = rotateConditions;
        }

        private void UpdateMoveCameraConditions()
        {
            var rotateConditions = MouseCameraController.MouseAndKeyboardConditions.Disabled;

            if (LeftButtonCheckBox2.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed;

            if (MiddleButtonCheckBox2.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.MiddleMouseButtonPressed;

            if (RightButtonCheckBox2.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.RightMouseButtonPressed;


            if (ShiftKeyCheckBox2.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.ShiftKey;

            if (ControlKeyCheckBox2.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.ControlKey;

            if (AltKeyCheckBox2.IsChecked ?? false)
                rotateConditions |= MouseCameraController.MouseAndKeyboardConditions.AltKey;

            MouseCameraController1.MoveCameraConditions = rotateConditions;
        }

        private void MaxCameraDistanceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            double newMaxCameraDistance;
            switch (MaxCameraDistanceComboBox.SelectedIndex)
            {
                case 1:
                    newMaxCameraDistance = 500;
                    break;

                case 2:
                    newMaxCameraDistance = 1000;
                    break;

                case 3:
                    newMaxCameraDistance = 5000;
                    break;

                case 0:
                default:
                    newMaxCameraDistance = double.NaN;
                    break;
            }

            MouseCameraController1.MaxCameraDistance = newMaxCameraDistance;
        }
    }
}
