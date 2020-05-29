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
    /// Interaction logic for FreeCameraAnimation.xaml
    /// </summary>
    public partial class FreeCameraAnimation : Page
    {
        private readonly AnimationController _animationController;

        public FreeCameraAnimation()
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
            AnimateCameraRotationToWithCameraAnimationNode(targetHeading: double.NaN, targetAttitude: -90);
        }

        private void RotateToFrontButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationToWithCameraAnimationNode(targetHeading: 0, targetAttitude: 0);
        }

        private void RotateToLeftButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationToWithCameraAnimationNode(targetHeading: 90, targetAttitude: 0);
        }

        private void RotateToSideButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationToWithCameraAnimationNode(targetHeading: 30, targetAttitude: -20);
        }

        
        private void FreeCameraRotateToTopButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationTo(targetCameraPosition: new Point3D(0, 400, 0), targetUpDirection: new Vector3D(0, 0, -1));
        }

        private void FreeCameraRotateToFrontButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationTo(targetCameraPosition: new Point3D(0, 0, 400), targetUpDirection: new Vector3D(0, 1, 0));
        }

        private void FreeCameraRotateToBackButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationTo(targetCameraPosition: new Point3D(0, 0, -400), targetUpDirection: new Vector3D(0, 1, 0));
        }

        private void FreeCameraRotateToLeftButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationTo(targetCameraPosition: new Point3D(-400, 0, 0), targetUpDirection: new Vector3D(0, 1, 0));
        }

        private void FreeCameraRotateToSideButton_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraRotationTo(targetCameraPosition: new Point3D(150, 150, 100), targetUpDirection: new Vector3D(0, 1, 0));
        }


        private void ZoomOutButton_OnClick(object sender, RoutedEventArgs e)
        {
            var currentDistance = (Camera1.TargetPosition - Camera1.CameraPosition).Length;

            AnimateCameraDistanceTo(currentDistance * 1.5);

            // When changing distance the animation is the same when using CameraAnimationNode or FreeCameraAnimationNode
            //AnimateCameraDistanceToWithCameraAnimationNode(currentDistance * 1.5);
        }

        private void ToStandardDistance_OnClick(object sender, RoutedEventArgs e)
        {
            AnimateCameraDistanceTo(newDistance: 300);

            // When changing distance the animation is the same when using CameraAnimationNode or FreeCameraAnimationNode
            //AnimateCameraDistanceToWithCameraAnimationNode(newDistance: 300);
        }


        private void AnimateCameraRotationTo(Point3D targetCameraPosition, Vector3D targetUpDirection)
        {
            var animationType = (SphericalInterpolationCheckBox.IsChecked ?? false)
                ? FreeCameraAnimationNode.FreeCameraAnimationTypes.SphericalInterpolation
                : FreeCameraAnimationNode.FreeCameraAnimationTypes.LinearInterpolation;

            // The easiest way to rotate the free camera it to use RotateTo method.
            Camera1.RotateTo(targetCameraPosition, targetUpDirection, 
                             animationDurationInMilliseconds: 1000, 
                             easingFunction: Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction, 
                             animationType: animationType);

            // But if you want more control, it is possible to manually define the rotation.
            // The following code does the same as Camera1.RotateTo (except that it uses our own _animationController instead of Camera1.AnimationController):

            //// Create a new CameraAnimationNode that will animate the Camera1
            //var freeCameraAnimationNode = new FreeCameraAnimationNode(Camera1);

            //freeCameraAnimationNode.AnimationType = animationType;

            //if (Camera1.CameraPosition != targetCameraPosition)
            //{
            //    freeCameraAnimationNode.CameraPositionTrack.Keys.Add(new Position3DKeyFrame(0,   Camera1.CameraPosition));
            //    freeCameraAnimationNode.CameraPositionTrack.Keys.Add(new Position3DKeyFrame(100, targetCameraPosition));

            //    // It is possible to set different interpolation mode to each KeyFrame, but it is easier
            //    // to set the same interpolation mode to all key frames with SetInterpolationToAllKeys method.
            //    freeCameraAnimationNode.CameraPositionTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);
            //}

            //if (Camera1.UpDirection != targetUpDirection)
            //{
            //    freeCameraAnimationNode.UpDirectionTrack.Keys.Add(new Vector3DKeyFrame(0,   Camera1.UpDirection));
            //    freeCameraAnimationNode.UpDirectionTrack.Keys.Add(new Vector3DKeyFrame(100, targetUpDirection));

            //    freeCameraAnimationNode.UpDirectionTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);
            //}
            
            //// It is possible to set different interpolation mode to each KeyFrame, but it is easier
            //// to set the same interpolation mode to all key frames with SetInterpolationToAllKeys method.
            //freeCameraAnimationNode.TargetPositionTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            //// Set this animation as one-time only
            //_animationController.AutoRepeat = false;
            //_animationController.AutoReverse = false;

            //// Use StartAnimation helper method to stop current animation and start the animation defined in cameraAnimationNode
            //StartAnimation(freeCameraAnimationNode);
        }

        private void AnimateCameraRotationToWithCameraAnimationNode(double targetHeading, double targetAttitude, bool useShortestPath = true)
        {
            var lookDirection = Camera1.TargetPosition - Camera1.CameraPosition;
            var distance = lookDirection.Length;

            lookDirection /= distance; // Normalize


            // IMPORTANT:
            // FreeCamera allows rotation around arbitrary axes and cannot be always converted reliably into heading, attitude and bank.
            // This means that angles (heading, attitude and bank) at the start of the animation may not be always correct.

            double startHeading, startAttitude, startBank;
            Ab3d.Utilities.CameraUtils.CalculateCameraAngles(lookDirection, Camera1.UpDirection, out startHeading, out startAttitude, out startBank);


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
        }

        private void AnimateCameraDistanceTo(double newDistance)
        {
            // Create a new CameraAnimationNode that will animate the Camera1
            var freeCameraAnimationNode = new FreeCameraAnimationNode(Camera1);

            var cameraDirection = Camera1.CameraPosition - Camera1.TargetPosition;
            cameraDirection.Normalize();

            var newCameraPosition = Camera1.TargetPosition + newDistance * cameraDirection;

            // Start with current camera Heading and Attitude
            freeCameraAnimationNode.CameraPositionTrack.Keys.Add(new Position3DKeyFrame(0, Camera1.CameraPosition));
            freeCameraAnimationNode.CameraPositionTrack.Keys.Add(new Position3DKeyFrame(100, newCameraPosition));

            // It is possible to set different interpolation mode to each KeyFrame, but it is easier
            // to set the same interpolation mode to all key frames with SetInterpolationToAllKeys method.
            freeCameraAnimationNode.CameraPositionTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            // Set this animation as one-time only
            _animationController.AutoRepeat = false;
            _animationController.AutoReverse = false;

            // Use StartAnimation helper method to stop current animation and start the animation defined in cameraAnimationNode
            StartAnimation(freeCameraAnimationNode);
        }

        private void AnimateCameraDistanceToWithCameraAnimationNode(double newDistance)
        {
            // Create a new CameraAnimationNode that will animate the Camera1
            var cameraAnimationNode = new CameraAnimationNode(Camera1);

            var currentDistance = (Camera1.CameraPosition - Camera1.TargetPosition).Length;

            // Start with current camera Heading and Attitude
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 0,   doubleValue: currentDistance));
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 100, doubleValue: newDistance));

            // It is possible to set different interpolation mode to each KeyFrame, but it is easier
            // to set the same interpolation mode to all key frames with SetInterpolationToAllKeys method.
            cameraAnimationNode.DistanceTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            // Set this animation as one-time only
            _animationController.AutoRepeat  = false;
            _animationController.AutoReverse = false;

            // Use StartAnimation helper method to stop current animation and start the animation defined in cameraAnimationNode
            StartAnimation(cameraAnimationNode);
        }

        private void StartAnimation(AnimationNodeBase animationNode)
        {
            // First stop current animation and ...
            if (_animationController.IsAnimating)
                _animationController.StopAnimation();


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
