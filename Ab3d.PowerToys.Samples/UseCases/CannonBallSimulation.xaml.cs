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
using Ab3d.Animation;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for CannonBallSimulation.xaml
    /// </summary>
    public partial class CannonBallSimulation : Page, ICompositionRenderingSubscriber
    {
        private Vector3D _velocity;

        private DateTime _startTime;
        private DateTime _lastTime;

        private bool _isAnimating;


        public CannonBallSimulation()
        {
            InitializeComponent();

            HideLines();
        }
        
        private void FireButton_Click(object sender, RoutedEventArgs e)
        {
            // Initial velocity
            _velocity = new Vector3D(VelocitySlider.Value * Math.Cos(AngleSlider.Value * Math.PI / 180),
                                     VelocitySlider.Value * Math.Sin(AngleSlider.Value * Math.PI / 180),
                                     0);

            _lastTime = DateTime.MinValue;
            CannonBallTranslate.OffsetX = 0;
            CannonBallTranslate.OffsetY = 0;

            // If animation was not started yet, than start it now (else we just reset the animation data to fire new ball again)
            if (!_isAnimating)
            {
                _isAnimating = true;

                // Use CompositionRenderingHelper to subscribe to CompositionTarget.Rendering event
                // This is much safer because in case we forget to unsubscribe from Rendering, the CompositionRenderingHelper will unsubscribe us automatically
                // This allows to collect this class will Grabage collector and prevents infinite calling of Rendering handler.
                // After subscribing the ICompositionRenderingSubscriber.OnRendering method will be called on each CompositionTarget.Rendering event
                CompositionRenderingHelper.Instance.Subscribe(this);
            }
        }

        void ICompositionRenderingSubscriber.OnRendering(EventArgs e)
        {
            // Used physics from: http://galileo.phys.virginia.edu/classes/581/FallingInExcel.html

            double elapsedSeconds;
            DateTime now;
            Vector3D partialVelocity;

            double speedFactor;
            double dragCoefficient;
            double mass;
            Vector3D gravityVector;
            Vector3D dragVector;


            now = DateTime.Now;

            if (_lastTime == DateTime.MinValue)
            {
                // First time just mark the current time
                _lastTime = now;
                _startTime = now;
                return;
            }

            // Initialize parameters from Sliders
            gravityVector = new Vector3D(0, -GravitySlider.Value, 0);
            dragCoefficient = DragSlider.Value;
            speedFactor = SpeedSlider.Value;
            mass = BallMassSlider.Value;


            elapsedSeconds = (now - _lastTime).TotalSeconds;
            _lastTime = now;

            // Calculate drag vector
            dragVector = new Vector3D(-Math.Sign(_velocity.X) * (dragCoefficient / mass) * _velocity.X * _velocity.X,
                                      -Math.Sign(_velocity.Y) * (dragCoefficient / mass) * _velocity.Y * _velocity.Y,
                                      0);


            // Update velocity - multiply by elapsedSeconds * speedFactor to make sub-second changes
            _velocity += (gravityVector + dragVector) * elapsedSeconds * speedFactor;

            // partialVelocity will be used to update position - reduce the change by multiply by elapsedSeconds * speedFactor to make sub-second changes
            partialVelocity = _velocity * elapsedSeconds * speedFactor;

            CannonBallTranslate.OffsetX += partialVelocity.X;
            CannonBallTranslate.OffsetY += partialVelocity.Y;
            CannonBallTranslate.OffsetZ += partialVelocity.Z;

            
            UpdateVectorLines(gravityVector, dragVector, _velocity);


            // Update the cameras if needed
            if (FirstPersonCamera1.IsEnabled)
            {
                // Update the position of FirstPersonCamera 
                // and adjust the Attitude so the camera is looking in the direction of the Velocity
                FirstPersonCamera1.Position = GetCannonBallCenter();
                FirstPersonCamera1.Attitude = Math.Atan2(_velocity.Y, _velocity.X) * 180 / Math.PI;
            }
            else if (ThirdPersonCameraCamera1.IsEnabled)
            {
                // Call Refresh becasue the position of CenterObject is changed
                // NOTE:
                // It would be also possible to set IsDynamic property on ThirdPersonCamera to true
                // This would automatically update the camera when the CenterObject is changed
                // But this brings slight performance overhead because camera is subscribed to CompositionTarget.Rendering event
                // and checks the CenterObject on each rendering pass.
                // Because we know when the CenterObject is changed, it is better to call Refresh method manually
                ThirdPersonCameraCamera1.Refresh();
            }


            // Write info
            InfoTextBox.Text = string.Format("Elapsed time: {0:0.000}s\nDistance: {1:0.0}m\nHeight: {2:0.0}m\nVelocity: {3:0.00}\nDrag: {4:0.00}", (now - _startTime).TotalSeconds, CannonBallTranslate.OffsetX, CannonBallTranslate.OffsetY, _velocity.Length, dragVector.Length);

            // Check if the ball has hit the ground
            if (CannonBall.CenterPosition.Y + CannonBallTranslate.OffsetY < 0)
            {
                // STOP animation
                CompositionRenderingHelper.Instance.Unsubscribe(this);

                _isAnimating = false;
                HideLines();
            }
        }

        private void HideLines()
        {
            GravityVectorLine.IsVisible = false;
            DragVectorLine.IsVisible = false;
            VelocityVectorLine.IsVisible = false;

            // Just set EndPoint to the same point as StartPoint

            //GravityVectorLine.EndPosition = GravityVectorLine.StartPosition;
            //DragVectorLine.EndPosition = DragVectorLine.StartPosition;
            //VelocityVectorLine.EndPosition = VelocityVectorLine.StartPosition;
        }

        private Point3D GetCannonBallCenter()
        {
            Point3D sphereCenter;

            sphereCenter = CannonBallTranslate.Transform(CannonBall.CenterPosition);

            return sphereCenter;
        }

        private void UpdateVectorLines(Vector3D gravityVector, Vector3D dragVector, Vector3D velocityVector)
        {
            Vector3D normalizedVector;
            Point3D sphereCenter;

            // Do not show lines when using first person camera
            if (FirstPersonCamera1.IsEnabled)
                return;

            sphereCenter = GetCannonBallCenter();

            // Gravity
            if (ShowGravityCheckBox.IsChecked ?? false)
            {
                normalizedVector = gravityVector;
                normalizedVector.Normalize();

                GravityVectorLine.BeginInit();
                GravityVectorLine.StartPosition = sphereCenter + normalizedVector * CannonBall.Radius; // Move the start position for the radius so it starts at the edge of the cannon ball
                GravityVectorLine.EndPosition = GravityVectorLine.StartPosition + gravityVector;
                GravityVectorLine.EndInit();
            }


            // Drag
            if (ShowDragCheckBox.IsChecked ?? false)
            {
                normalizedVector = dragVector;
                normalizedVector.Normalize();

                DragVectorLine.BeginInit();
                DragVectorLine.StartPosition = sphereCenter + normalizedVector * CannonBall.Radius; // Move the start position for the radius so it starts at the edge of the cannon ball
                DragVectorLine.EndPosition = DragVectorLine.StartPosition + dragVector;
                DragVectorLine.EndInit();
            }


            // Velocity
            if (ShowVelocityCheckBox.IsChecked ?? false)
            {
                normalizedVector = velocityVector;
                normalizedVector.Normalize();

                VelocityVectorLine.BeginInit();
                VelocityVectorLine.StartPosition = sphereCenter + normalizedVector * CannonBall.Radius; // Move the start position for the radius so it starts at the edge of the cannon ball
                VelocityVectorLine.EndPosition = VelocityVectorLine.StartPosition + velocityVector;
                VelocityVectorLine.EndInit();
            }

            if (!GravityVectorLine.IsVisible)
            {
                GravityVectorLine.IsVisible = true;
                DragVectorLine.IsVisible = true;
                VelocityVectorLine.IsVisible = true;
            }
        }

        private void CameraRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            bool isCamera1SelectedBefore = Camera1.IsEnabled;

            var selectedCamera = GetSelectedCamera();

            // Enable only selected camera
            Camera1.IsEnabled = false;
            FirstPersonCamera1.IsEnabled = false;
            ThirdPersonCameraCamera1.IsEnabled = false;

            selectedCamera.IsEnabled = true;


            // TargetCamera should be manually changed on camera controllers
            MouseCameraController1.TargetCamera = selectedCamera;
            CameraControlPanel1.TargetCamera = selectedCamera;

            if (ReferenceEquals(selectedCamera, Camera1))
            {
                // If Camera1 was already enabled before, then we animate the camera change.
                // If some other camera was enabled before, we immediately set the camera properties
                AnimateCamera1(animateChange: isCamera1SelectedBefore);
            }
            else if (ReferenceEquals(selectedCamera, FirstPersonCamera1))
            {
                // If FirstPersonCamera1 was selected, adjust the Attitude to the same value as Cannon's Angle so the camera is looking from the cannon out
                FirstPersonCamera1.Attitude = AngleSlider.Value;
            }
        }

        private void VectorsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized || !_isAnimating)
                return;

            // Hide all lines
            // The lines that should appear will be updated on next rending event in UpdateVectorLines method
            HideLines();
        }

        private void LightRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            // Set the light based on the selected radio button
            if (EveningRadioButton.IsChecked ?? false)
            {
                MainSceneLight.Position = new Point3D(0, 200, 500); // from the side
                MainSceneLight.Color = Color.FromRgb(247, 244, 154);

            }
            else if (MoonlightRadioButton.IsChecked ?? false)
            {
                MainSceneLight.Position = new Point3D(0, 500, 500); // from top and side
                MainSceneLight.Color = Color.FromRgb(106, 221, 225);
            }
            else // DayLightRadioButton
            {
                MainSceneLight.Position = new Point3D(0, 500, 0); // from top
                MainSceneLight.Color = Colors.White;
            }
        }

        private Ab3d.Cameras.BaseCamera GetSelectedCamera()
        {
            Ab3d.Cameras.BaseCamera selectedCamera = null;

            if ((SceneCameraRadioButton.IsChecked ?? false) ||
                (SideSceneCameraRadioButton.IsChecked ?? false) ||
                (CannonCameraRadioButton.IsChecked ?? false))
            {
                selectedCamera = Camera1;
            }
            else if (FirstPersonCameraRadioButton.IsChecked ?? false)
            {
                selectedCamera = FirstPersonCamera1;
            }
            else if (ThirdPersonCameraRadioButton.IsChecked ?? false)
            {
                selectedCamera = ThirdPersonCameraCamera1;
            }

            return selectedCamera;
        }

        private void AnimateCamera1(bool animateChange)
        {
            double newHeading, newAttitude, newDistance;
            Point3D newTargetPosition;

            if (CannonCameraRadioButton.IsChecked ?? false)
            {
                newHeading = 80;
                newAttitude = -15;
                newDistance = 200;
                //newTargetPosition = new Point3D(-200, 50, 30);
                newTargetPosition = new Point3D(-90, 10, 0);
            }
            else if (SideSceneCameraRadioButton.IsChecked ?? false)
            {
                newHeading = 0;
                newAttitude = -10;
                newDistance = 400;
                newTargetPosition = new Point3D(0, 0, 0);
            }
            else // if (SceneCameraRadioButton.IsChecked ?? false)
            {
                newHeading = 60;
                newAttitude = -30;
                newDistance = 400;
                newTargetPosition = new Point3D(0, 0, 0);
            }

            if (animateChange)
            {
                AnimateCamera1(newHeading, newAttitude, newTargetPosition, newDistance);
            }
            else
            {
                Camera1.Heading  = newHeading;
                Camera1.Attitude = newAttitude;
                Camera1.Distance = newDistance;
                Camera1.TargetPosition = newTargetPosition;
            }
        }

        private void AnimateCamera1(double newHeading, double newAttitude, Point3D newTargetPosition, double newDistance)
        {
            // See samples in Animations folder for more info about animations

            // Create a new CameraAnimationNode that will animate the Camera1
            var cameraAnimationNode = new CameraAnimationNode(Camera1);

            if (Math.Abs(Camera1.Heading - newHeading) > 2 || Math.Abs(Camera1.Attitude - newAttitude) > 2) // We animate the Heading and Attitude if the difference is bigger the 2 degrees
            {
                cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(frameNumber: 0, heading: Camera1.Heading, attitude: Camera1.Attitude)); // Start with current rotation
                cameraAnimationNode.RotationTrack.Keys.Add(new CameraRotationKeyFrame(frameNumber: 100, heading: newHeading, attitude: newAttitude));

                cameraAnimationNode.RotationTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);
            }

            if ((Camera1.TargetPosition - newTargetPosition).Length > 5) // We animate the TargetPosition if the difference is bigger the 5
            {
                cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(0, Camera1.TargetPosition)); // Start with current TargetPosition
                cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(100, newTargetPosition));    // End at newTargetPosition 

                // Set easing function to smooth the transition
                cameraAnimationNode.PositionTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);
            }

            if (Math.Abs(Camera1.Distance - newDistance) > 5) // We animate the Distance if the difference is bigger the 5
            {
                cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 0, doubleValue: Camera1.Distance)); // Start with current Distance
                cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 100, doubleValue: newDistance));    // End with newDistance 

                cameraAnimationNode.DistanceTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);
            }

            Camera1.AnimationController.AnimationNodes.Clear();
            Camera1.AnimationController.AnimationNodes.Add(cameraAnimationNode);

            Camera1.AnimationController.AutoRepeat = false;
            Camera1.AnimationController.AutoReverse = false;
            Camera1.AnimationController.FramesPerSecond = 100; // Take 1 second for the whole animation (100 frames)

            Camera1.AnimationController.StartAnimation();
        }
    }
}
