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
using Ab3d.Cameras;

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for CameraAnimationSample.xaml
    /// </summary>
    public partial class CameraAnimationSample : Page
    {
        private bool _isRotationStarted;

        public CameraAnimationSample()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(CameraAnimationSample_Loaded);
        }

        void CameraAnimationSample_Loaded(object sender, RoutedEventArgs e)
        {
            StartAnimation(false);  // false: isActionImmediate
        }

        private void RotateToTopButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Animate camera to -90 attitude; current camera's heading is preserved
            Camera1.RotateTo(targetHeading: double.NaN, 
                             targetAttitude: -90, 
                             animationDurationInMilliseconds: 800, 
                             easingFunction: Ab3d.Animation.EasingFunctions.CubicEaseInOutFunction);

            // See Animations/CameraAnimation for advanced camera animation samples.
        }

        private void RotateToFrontButton_OnClick(object sender, RoutedEventArgs e)
        {
            Camera1.RotateTo(targetHeading: 0, 
                             targetAttitude: 0, 
                             animationDurationInMilliseconds: 800, 
                             easingFunction: Ab3d.Animation.EasingFunctions.CubicEaseInOutFunction);

            // See Animations/CameraAnimation for advanced camera animation samples.
        }

        private void RotateToLeftButton_OnClick(object sender, RoutedEventArgs e)
        {
            Camera1.RotateTo(targetHeading: 90, 
                             targetAttitude: 0, 
                             animationDurationInMilliseconds: 800, 
                             easingFunction: Ab3d.Animation.EasingFunctions.CubicEaseInOutFunction);

            // See Animations/CameraAnimation for advanced camera animation samples.
        }

        private void RotateToSideButton_OnClick(object sender, RoutedEventArgs e)
        {
            Camera1.RotateTo(targetHeading: 30, 
                             targetAttitude: -20, 
                             animationDurationInMilliseconds: 800, 
                             easingFunction: Ab3d.Animation.EasingFunctions.CubicEaseInOutFunction);

            // See Animations/CameraAnimation for advanced camera animation samples.
        }

        private void IncreaseHeadingButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Instead of RotateTo was can also use RotateFor method:
            Camera1.RotateFor(changedHeading: 90, 
                              changedAttitude: 0, 
                              animationDurationInMilliseconds: 800, 
                              easingFunction: Ab3d.Animation.EasingFunctions.CubicEaseInOutFunction);

            // See Animations/CameraAnimation for advanced camera animation samples.
        }



        private void StartStopSlowlyButton_Click(object sender, RoutedEventArgs e)
        {
            // NOTE: Before adding easing functions to mouse camera controller,
            // it was possible to ask if we have turned the rotation on with simply asking the SceneCamera1.IsRotating.
            // But now this property can be true when the camera's easing function is stopping the camera's rotation - so even without manually calling StartAnimation
            // Therefore we need our own private field _isRotationStarted
            // if (SceneCamera1.IsRotating) 
            if (_isRotationStarted)
                StopAnimation(false);  // false: isActionImmediate
            else
                StartAnimation(false); // false: isActionImmediate
        }

        private void StartStopNowButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRotationStarted)
                StopAnimation(true);  // true: isActionImmediate
            else
                StartAnimation(true); // true: isActionImmediate
        }

        private void StartAnimation(bool isActionImmediate)
        {
            // Camera can be animated with using StartRotation method.
            // StartRotation take two parameters - headingChangeInSecond and attitudeChangeInSecond
            // Advantage of using StartRotation method over using Storyboard animation of Heading or Attitude properties is that
            // StartRotation does not lock the Heading and Attitude properties.
            // The StartRotation also works very good with CameraControlPanel and MouseCameraController - while the camera is rotating it is possible 
            // to manually change camera with the mouse or with the buttons on CameraControlPanel

            double headingChangeInSecond = HeadingChangeInSecondSlider.Value;
            double attitudeChangeInSecond = AttitudeChangeInSecondSlider.Value;

            if (isActionImmediate)
            {
                Camera1.StartRotation(headingChangeInSecond, attitudeChangeInSecond); // No easing
            }
            else
            {
                double accelerationSpeed = AccelerationSpeedSlider.Value;
                SphericalCamera.EasingFunctionDelegate easingFunction = GetEaseInFunction(); // NOTE that ease in and ease out are different functions

                Camera1.StartRotation(headingChangeInSecond, attitudeChangeInSecond, accelerationSpeed, easingFunction);
            }

            StartStopSlowlyButton.Content = "STOP rotation slowly";
            StartStopNowButton.Content = "STOP rotation now";
            AccelerationSpeedTextBlock.Text = "decelerationSpeed (0 = disabled):";

            _isRotationStarted = true;
        }

        private void StopAnimation(bool isActionImmediate)
        {

            if (isActionImmediate)
            {
                Camera1.StopRotation(); // No easing
            }
            else
            {
                double accelerationSpeed = AccelerationSpeedSlider.Value;
                SphericalCamera.EasingFunctionDelegate easingFunction = GetEaseOutFunction(); // NOTE that ease in and ease out are different functions

                Camera1.StopRotation(accelerationSpeed, easingFunction);
            }

            StartStopSlowlyButton.Content = "START rotation slowly";
            StartStopNowButton.Content = "START rotation now";
            AccelerationSpeedTextBlock.Text = "accelerationSpeed (0 = disabled):";

            _isRotationStarted = false;
        }

        private SphericalCamera.EasingFunctionDelegate GetEaseInFunction()
        {
            if (!this.IsLoaded)
                return null;

            if (EasingComboBox.SelectedIndex == 1)
                return Ab3d.Animation.EasingFunctions.QuadraticEaseInFunction;

            if (EasingComboBox.SelectedIndex == 2)
                return Ab3d.Animation.EasingFunctions.CubicEaseInFunction;

            return null;
        }
        
        private SphericalCamera.EasingFunctionDelegate GetEaseOutFunction()
        {
            if (!this.IsLoaded)
                return null;

            if (EasingComboBox.SelectedIndex == 1)
                return Ab3d.Animation.EasingFunctions.QuadraticEaseOutFunction;
            
            if (EasingComboBox.SelectedIndex == 2)
                return Ab3d.Animation.EasingFunctions.CubicEaseOutFunction;
            
            return null;
        }

        // See http://gizma.com/easing/#cub2 for more easing algorithms
        //public static double QuadraticEaseIn(double x)
        //{
        //    return x * x;
        //}

        //public static double QuadraticEaseOut(double x)
        //{
        //    return -1 * x * x + 2 * x;
        //}

        //public static double QuadraticEaseInOutFunction(double t)
        //{
        //    t *= 2;

        //    if (t < 1)
        //        return t * t * 0.5;

        //    return -0.5 * ((t - 1) * (t - 3) - 1);
        //}

        //public static double CubicEaseIn(double x)
        //{
        //    return x * x * x;
        //}

        //public static double CubicEaseOut(double x)
        //{
        //    x -= 1;

        //    return x * x * x + 1;
        //}

        //public static double CubicEaseInOutFunction(double t)
        //{
        //    t *= 2;

        //    if (t < 1)
        //        return t * t * t * 0.5;

        //    t -= 2;
        //    return 0.5 * (t * t * t + 2);
        //}
    }
}
