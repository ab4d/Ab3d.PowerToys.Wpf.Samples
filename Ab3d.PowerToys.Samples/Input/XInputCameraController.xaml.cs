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

namespace Ab3d.PowerToys.Samples.Input
{
    // NOTE: 
    // To use game controller support, you need to add reference to Ab3d.PowerToys.Input assembly.

    /// <summary>
    /// Interaction logic for XInputCameraController.xaml
    /// </summary>
    public partial class XInputCameraController : Page
    {
        public XInputCameraController()
        {
            InitializeComponent();

            this.Loaded += (sender, args) => UpdateIsControllerConnected();
        }

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            // make all the changes at once
            SceneCamera1.BeginInit();

            SceneCamera1.Heading = -30;
            SceneCamera1.Attitude = -15;
            SceneCamera1.Distance = 2;
            SceneCamera1.Offset = new Vector3D(0, 0, 0);

            SceneCamera1.EndInit();
        }

        private void XInputCameraController1_OnIsConnectedChanged(object sender, EventArgs e)
        {
            UpdateIsControllerConnected();
        }

        private void UpdateIsControllerConnected()
        {
            if (XInputCameraController1.IsControllerConnected)
            {
                NoControllerTextBlock.Visibility = Visibility.Collapsed;
                ControllerConnectedTextBlock.Visibility = Visibility.Visible;

                SettingsPanel.IsEnabled = true;
            }
            else
            {
                ControllerConnectedTextBlock.Visibility = Visibility.Collapsed;
                NoControllerTextBlock.Visibility = Visibility.Visible;

                SettingsPanel.IsEnabled = false;
            }
        }

        private void RotationSpeedSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            // We cannot data-bind to RotationSpeed property because it is not a DependencyProperty but a simple get; set; property
            XInputCameraController1.RotationSpeed = RotationSpeedSlider.Value;
        }

        private void MovementSpeedSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            // We cannot data-bind to MovementSpeed property because it is not a DependencyProperty but a simple get; set; property
            XInputCameraController1.MovementSpeed = MovementSpeedSlider.Value;
        }

        private void OnInvertHeadingRotationDirectionCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            XInputCameraController1.InvertHeadingRotationDirection = InvertHeadingRotationDirectionCheckBox.IsChecked ?? false;
        }

        private void OnInvertAttitudeRotationDirectionCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            XInputCameraController1.InvertAttitudeRotationDirection = InvertAttitudeRotationDirectionCheckBox.IsChecked ?? false;
        }

        private void OnMoveOnlyHorizontallyCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            XInputCameraController1.MoveOnlyHorizontally = MoveOnlyHorizontallyCheckBox.IsChecked ?? false;
        }

        private void MoveVerticallyWithDPadButtonsCheckedChanged(object sender, RoutedEventArgs e)
        {
            XInputCameraController1.MoveVerticallyWithDPadButtons = MoveVerticallyWithDPadButtonsCheckBox.IsChecked ?? false;
        }
    }
}
