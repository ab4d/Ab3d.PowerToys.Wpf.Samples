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
using Ab3d.Animation;

namespace Ab3d.PowerToys.Samples.Animations
{
    /// <summary>
    /// Interaction logic for CameraAnimation.xaml
    /// </summary>
    public partial class CameraAnimation : Page
    {
        private readonly AnimationController _animationController;

        public CameraAnimation()
        {
            InitializeComponent();

            // To animate the camera we can create a new AnimationController.
            // But it is better to use the AnimationController that can be get from camera.
            // This way it is possible to control if MouseCameraController can stop the animation when user starts camera rotation or movement by mouse or touch.
            // This is controller with the MouseCameraController.IsCameraAnimationStoppedOnUserAction property (by default set to true to stop the animation).
            //_animationController = new AnimationController();
            _animationController = Camera1.AnimationController;

            // Set animation speed with setting FramesPerSecond.
            // Though this value does not have any effect on rendering, its default value is set to 60.
            // But with setting the FramesPerSecond to 100, it is easier to define the FrameNumber values for KeyFrames,
            // for example FrameNumber = 300 is at 3rd second; FrameNumber = 500 is at 5th second, etc.
            _animationController.FramesPerSecond = 100;

            // Stop animation on user camera rotation or movement. See description above for more info.
            MouseCameraController1.IsCameraAnimationStoppedOnUserAction = true;


            _animationController.AnimationCompleted += delegate(object sender, EventArgs args)
            {
                System.Diagnostics.Debug.WriteLine("AnimationCompleted");
            };

            this.Unloaded += delegate(object sender, RoutedEventArgs args)
            {
                _animationController.StopAnimation();
            };
        }

        private void RotateToTopButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationTo(targetHeading: double.NaN, targetAttitude: -90);
        }

        private void RotateToFrontButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationTo(targetHeading: 0, targetAttitude: 0);
        }

        private void RotateToLeftButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationTo(targetHeading: 90, targetAttitude: 0);
        }

        private void RotateToSideButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationTo(targetHeading: 30, targetAttitude: -20);
        }

        private void ZoomOutButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraDistanceTo(Camera1.Distance * 1.5);
        }

        private void ToStandardDistance_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraDistanceTo(newDistance: 300);
        }

        private void Animation1Button_OnClick(object sender, RoutedEventArgs e)
        {
            // Create a new CameraAnimationNode that will animate the Camera1
            var cameraAnimationNode = new CameraAnimationNode(Camera1);

            // Start with current camera Heading and Attitude

            int startFrameNumber;

            // If animation camera's Heading is not 30 and Attitude is not -20 (with 0.5 degree of tolerance),
            // Than start with CameraRotationKeyFrame that will rotate camera from the current position to the 
            // animation start position: Heading = 30; Attitude = -20
            if (Math.Abs(Camera1.Heading - 30) > 0.5 ||
                Math.Abs(Camera1.Attitude - -20) > 0.5)
            {
                cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(frameNumber: 0, heading: Camera1.Heading, attitude: Camera1.Attitude));
                startFrameNumber = 100;
            }
            else
            {
                // Camera is already at desired start orientation
                startFrameNumber = 0;
            }

            // Move the predefined camera rotation in 200 frames (2 seconds in when using 100 animation frames per second setting - see constructor of this sample)
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(frameNumber: startFrameNumber, heading: 30, attitude: -20));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(100 + startFrameNumber, 90, -20));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(300 + startFrameNumber, 90, 90));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(400 + startFrameNumber, 180, -20));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(600 + startFrameNumber, 30, -20));

            // It is possible to set different interpolation mode to each KeyFrame, but it is easier
            // to set the same interpolation mode to all key frames with SetInterpolationToAllKeys method.
            cameraAnimationNode.RotationTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            // Set this animation to revert but not to repeat
            _animationController.AutoReverse = true;
            _animationController.AutoRepeat = false;

            // Use StartAnimation helper method to stop current animation and start the animation defined in cameraAnimationNode
            StartAnimation(cameraAnimationNode);
        }

        private void Animation2Button_OnClick(object sender, RoutedEventArgs e)
        {
            // Create a new CameraAnimationNode that will animate the Camera1
            var cameraAnimationNode = new CameraAnimationNode(Camera1);

            // Start with current camera Heading and Attitude

            int startFrameNumber;

            // If animation camera's Heading is not 30 and Attitude is not -20 (with 0.5 degree of tolerance),
            // Than start with CameraRotationKeyFrame that will rotate camera from the current position to the 
            // animation start position: Heading = 30; Attitude = -20
            //
            // We also check if the distance is 300
            if (Math.Abs(Camera1.Heading - 30) > 0.5 ||
                Math.Abs(Camera1.Attitude - -20) > 0.5 ||
                Math.Abs(Camera1.Distance - 300) > 1.0)
            {
                cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(frameNumber: 0, heading: Camera1.Heading, attitude: Camera1.Attitude));
                cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 0, doubleValue: Camera1.Distance));
                startFrameNumber = 100;
            }
            else
            {
                // Camera is already at desired start orientation
                startFrameNumber = 0;
            }

            // Move the predefined camera rotation in 200 frames (2 seconds in default 100 animation frames per second setting - see constructor of this sample)
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(frameNumber: startFrameNumber, heading: 30, attitude: -20));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(startFrameNumber + 300, -90, -30));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(startFrameNumber + 400, -90, -30));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(startFrameNumber + 500, -90, -30, bank: 360));

            // It is possible to set different interpolation mode to each KeyFrame, but it is easier
            // to set the same interpolation mode to all key frames with SetInterpolationToAllKeys method.
            cameraAnimationNode.RotationTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: startFrameNumber,       doubleValue: 300));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: startFrameNumber + 150, doubleValue: 500));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: startFrameNumber + 300, doubleValue: 200));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: startFrameNumber + 400, doubleValue: 120));
            
            // Note that rotation animation is longer then distance animation (last frame number is 500 vs 400).
            // This is not a problem when reversing the animation because the distance value will stay the same on all frame numbers bigger then 400.

            cameraAnimationNode.DistanceTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            // Set this animation to revert but not to repeat
            _animationController.AutoReverse = true;
            _animationController.AutoRepeat = false;

            // Use StartAnimation helper method to stop current animation and start the animation defined in cameraAnimationNode
            StartAnimation(cameraAnimationNode);
        }

        private void Animation3Button_OnClick(object sender, RoutedEventArgs e)
        {
            // Create a new CameraAnimationNode that will animate the Camera1
            var cameraAnimationNode = new CameraAnimationNode(Camera1);

            // Start with current camera Heading and Attitude

            // Move the predefined camera rotation in 200 frames (2 seconds in default 100 animation frames per second setting - see constructor of this sample)
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(frameNumber: 0, heading: Camera1.Heading, attitude: Camera1.Attitude));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(100, 30, -30));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(200, 120, -30));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(300, 210, -30));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(400, 300, -30));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(500, 300, -20));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(600, 30, 0));
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(700, 30, -20));

            // It is possible to set different interpolation mode to each KeyFrame, but it is easier
            // to set the same interpolation mode to all key frames with SetInterpolationToAllKeys method.
            cameraAnimationNode.RotationTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(0, Camera1.Distance));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(100, 150));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(200, 150));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(250, 400));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(300, 150));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(400, 150));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(500, 150));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(600, 40));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(700, 300));

            cameraAnimationNode.DistanceTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(0, Camera1.TargetPosition));
            cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(100, new Point3D(-50, 50, 0)));
            cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(200, new Point3D(-50, 50, 0)));
            cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(300, new Point3D(50, 50, 0)));
            cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(400, new Point3D(50, 50, 0)));
            cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(500, new Point3D(0, 50, 0)));
            cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(600, new Point3D(0, 50, 0)));
            cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(700, new Point3D(0, 0, 0)));

            cameraAnimationNode.PositionTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            // Set this animation to revert but not to repeat
            _animationController.AutoReverse = false;
            _animationController.AutoRepeat = false;

            _animationController.FramesPerSecond = 100;

            // Use StartAnimation helper method to stop current animation and start the animation defined in cameraAnimationNode
            StartAnimation(cameraAnimationNode);
        }       

        private void AnimateCameraRotationTo(double targetHeading, double targetAttitude, bool useShortestPath = true)
        {
            Camera1.RotateTo(targetHeading,
                             targetAttitude, 
                             animationDurationInMilliseconds: 1000, 
                             easingFunction: Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction, 
                             useShortestPath: true);

            DumpTextBox.Text = string.Format("Camera1.RotateTo({0}, {1}, 1000, Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction, useShortestPath: true);\r\n\r\n", targetHeading, targetAttitude);

            // We can get details about the animation with calling GetDumpString method on AnimationController or any other animation related class.
            // It is also possible to call Dump method in the Visual Studio immediate method. This will dump the details to the same window.
            DumpTextBox.Text += Camera1.AnimationController.GetDumpString();


            // The following commented code shows how to start animation manually without RotateTo method:
            /*
            double startHeading  = Camera1.Heading;
            double startAttitude = Camera1.Attitude;

            if (double.IsNaN(targetHeading))
                targetHeading = startHeading;

            if (double.IsNaN(targetAttitude))
                targetAttitude = startAttitude;


            // Adjust start heading and attitude so that we will come with the shortest path to the target heading and attitude
            if (useShortestPath)
            {
                startHeading  = Ab3d.Utilities.CameraUtils.GetClosestPathStartAngle(startHeading,  targetHeading);
                startAttitude = Ab3d.Utilities.CameraUtils.GetClosestPathStartAngle(startAttitude, targetAttitude);
            }


            // Create a new CameraAnimationNode that will animate the Camera1
            var cameraAnimationNode = new CameraAnimationNode(Camera1);

            // Start with current camera Heading and Attitude
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(0, startHeading, startAttitude));

            // Animate to the specified heading and attitude in 100 frames
            cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(100, targetHeading, targetAttitude));


            // It is possible to set different interpolation mode to each KeyFrame, but it is easier
            // to set the same interpolation mode to all key frames with SetInterpolationToAllKeys method.
            cameraAnimationNode.RotationTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            // Set this animation as one-time only
            _animationController.AutoRepeat = false;
            _animationController.AutoReverse = false;

            // Use StartAnimation helper method to stop current animation and start the animation defined in cameraAnimationNode
            StartAnimation(cameraAnimationNode);
            */
        }

        private void AnimateCameraDistanceTo(double newDistance)
        {
            // Create a new CameraAnimationNode that will animate the Camera1
            var cameraAnimationNode = new CameraAnimationNode(Camera1);

            // Start with current camera Heading and Attitude
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 0,   doubleValue: Camera1.Distance));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 100, doubleValue: newDistance));

            // It is possible to set different interpolation mode to each KeyFrame, but it is easier
            // to set the same interpolation mode to all key frames with SetInterpolationToAllKeys method.
            cameraAnimationNode.DistanceTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            // Set this animation as one-time only
            _animationController.AutoRepeat = false;
            _animationController.AutoReverse = false;

            // Use StartAnimation helper method to stop current animation and start the animation defined in cameraAnimationNode
            StartAnimation(cameraAnimationNode);
        }

        private void StartAnimation(AnimationNodeBase animationNode)
        {
            // First stop current animation and ...
            if (_animationController.IsAnimating)
                _animationController.StopAnimation();


            // NormalizeAngles normalizes the Heading, Attitude and Bank angles so that their values are between -180 and 180 (because normalizeTo180Degrees is true).
            // For example, this methods converts 390 value into 30. This prevent animating 390 to 40 instead of 30 to 40.
            Camera1.NormalizeAngles(normalizeTo180Degrees: true);


            // ... clear any existing AnimationNodes
            _animationController.AnimationNodes.Clear();


            // Add the CameraAnimationNode to animation controller
            _animationController.AnimationNodes.Add(animationNode);

            // And start the animation
            _animationController.StartAnimation();


            // We can get details about the animation with calling GetDumpString method on AnimationController or any other animation related class.
            // It is also possible to call Dump method in the Visual Studio immediate method. This will dump the details to the same window.
            DumpTextBox.Text = _animationController.GetDumpString();
        }
    }
}
