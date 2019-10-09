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

namespace Ab3d.PowerToys.Samples.MouseCameraControllerSamples
{
    /// <summary>
    /// Interaction logic for MouseCameraControllerOptions.xaml
    /// </summary>
    public partial class MouseCameraControllerOptions : Page
    {
        public MouseCameraControllerOptions()
        {
            InitializeComponent();
        }
        
        private void CustomCursorRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            MouseCameraController1.RotationCursor = Cursors.Hand;
        }

        private void RotateCursorRightRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            MouseCameraController1.RotationCursor = MouseCameraController1.RotateCursorRight;
        }

        private void RotateCursorLeftRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            MouseCameraController1.RotationCursor = MouseCameraController1.RotateCursorLeft;
        }

        private void RotationInertiaRatioSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            MouseCameraController1.RotationInertiaRatio = RotationInertiaRatioSlider.Value;

            // It is also possible to change the RotationEasingFunction
            // The CubicEaseOut method (defined in CameraAnimationSample) is the function that is used by default 
            //MouseCameraController1.RotationEasingFunction = Ab3d.PowerToys.Samples.Cameras.CameraAnimationSample.CubicEaseOut;
        }
    }
}
