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

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for OffCenterCameraSample.xaml
    /// </summary>
    public partial class OffCenterCameraSample : Page
    {
        public OffCenterCameraSample()
        {
            InitializeComponent();

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                UpdateCurrentSettings();
            };
        }

        private void OnRenderingCenterPositionChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (ReferenceEquals(sender, LeftToggleButton))
            {
                Camera1.TargetPosition = new Point3D(30, 0, 0);

                // We also need to set the RotationCenterPosition.
                // By default RotationCenterPosition is set to null and this means that 
                // camera is rotating around the TargetPosition.
                Camera1.RotationCenterPosition = new Point3D(0, 0, 0);

                CenterToggleButton.IsChecked = false;
                RightToggleButton.IsChecked = false;
            }
            else if (ReferenceEquals(sender, CenterToggleButton))
            {
                Camera1.TargetPosition = new Point3D(0, 0, 0);

                // We also need to set the RotationCenterPosition.
                // By default RotationCenterPosition is set to null and this means that 
                // camera is rotating around the TargetPosition.
                Camera1.RotationCenterPosition = null;

                LeftToggleButton.IsChecked = false;
                RightToggleButton.IsChecked  = false;
            }
            else if (ReferenceEquals(sender, RightToggleButton))
            {
                Camera1.TargetPosition = new Point3D(-30, 0, 0);

                // We also need to set the RotationCenterPosition.
                // By default RotationCenterPosition is set to null and this means that 
                // camera is rotating around the TargetPosition.
                Camera1.RotationCenterPosition = new Point3D(0, 0, 0);

                CenterToggleButton.IsChecked = false;
                LeftToggleButton.IsChecked  = false;
            }

            // NOTES:
            // - MouseCameraController.RotateAroundMousePosition must be false, otherwise RotationCenterPosition will be overwritten by the mouse position.
            // - RotationCenterPosition property is supported only by TargetPositionCamera and FreeCamera.

            UpdateCurrentSettings();
        }

        private void UpdateCurrentSettings()
        {
            TargetPositionCross.Position = Camera1.TargetPosition;
            TargetPositionValueTextBlock.Text = string.Format("{0:0} {1:0} {2:0}", Camera1.TargetPosition.X, Camera1.TargetPosition.Y, Camera1.TargetPosition.Z);

            if (Camera1.RotationCenterPosition.HasValue)
            {
                RotationCenterPositionCross.Position = Camera1.RotationCenterPosition.Value;
                RotationCenterPositionCross.IsVisible = true;

                RotationCenterPositionValueTextBlock.Text = string.Format("{0:0} {1:0} {2:0}", Camera1.RotationCenterPosition.Value.X, Camera1.RotationCenterPosition.Value.Y, Camera1.RotationCenterPosition.Value.Z);
            }
            else
            {
                RotationCenterPositionCross.IsVisible = false;
                RotationCenterPositionValueTextBlock.Text = "null";
            }
        }
    }
}
