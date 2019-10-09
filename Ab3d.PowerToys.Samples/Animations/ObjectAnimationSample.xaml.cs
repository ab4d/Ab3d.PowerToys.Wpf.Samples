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
    /// Interaction logic for ObjectAnimationSample.xaml
    /// </summary>
    public partial class ObjectAnimationSample : Page
    {
        private readonly AnimationController _animationController;
        private Point3DCollection _usedAnimationPositions;
        private Point3D[] _originalAnimationPositions;

        public ObjectAnimationSample()
        {
            InitializeComponent();

            // Original positions that are used to animate the object.
            // Those positions can be smoothed with creating a curve through the positions.
            _originalAnimationPositions = new Point3D[]
            {
                new Point3D(0, 0, 0),
                new Point3D(15, 0, 0),
                new Point3D(40, 0, 40),
                new Point3D(80, 0, -30),
                new Point3D(110, 0, 20),
                new Point3D(150, 0, 0),
                new Point3D(180, 0, 0)
            };


            _animationController = new AnimationController();

            // Set animation speed with setting FramesPerSecond.
            // Though this value does not have any effect on rendering, its default value is set to 60.
            // But with setting the FramesPerSecond to 100, it is easier to define the FrameNumber values for KeyFrames,
            // for example FrameNumber = 300 is at 3rd second; FrameNumber = 500 is at 5th second, etc.
            _animationController.FramesPerSecond = 100;


            _animationController.AnimationCompleted += delegate (object sender, EventArgs args)
            {
                System.Diagnostics.Debug.WriteLine("AnimationCompleted");
            };

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                UpdateUsedPositions();
                StartAnimation1();
            };

            this.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                _animationController.StopAnimation();
            };
        }

        private void StartAnimationButton_OnClick(object sender, RoutedEventArgs e)
        {
            StartAnimation1();
        }

        private void OnSmoothAnimationCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateUsedPositions();
        }

        
        private void StartAnimation1()
        {
            // Create a new CameraAnimationNode that will animate the Camera1
            var objectAnimationNode = new Visual3DAnimationNode(AnimatedObjectVisual3D);

            // NOTE:
            // To animate Model3DGroup or GeometryModel3D create Model3DAnimationNode instead of Visual3DAnimationNode

            // When Visual3DAnimationNode (or Model3DAnimationNode) is created, the RotationCenterPosition is set to the center of the specified 3D object.
            // This makes the rotation and scale animation rotate and scale from the center of the object.
            // In our case we want to only scale upwards (not in all directions).
            // To do that we just adjust the RotationCenterPosition so that its Y position is at the bottom of the 3D object (y = 0)
            objectAnimationNode.RotationCenterPosition = new Point3D(objectAnimationNode.RotationCenterPosition.X, 0, objectAnimationNode.RotationCenterPosition.Z);

            
            // adjust number of frames between each position key frame so that the whole position animation takes 500 frames (5 seconds)
            int framesPerPosition = 500 / _usedAnimationPositions.Count;


            // Create key frames for position animation
            for (var i = 0; i < _usedAnimationPositions.Count; i++)
                objectAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(i * framesPerPosition, _usedAnimationPositions[i]));
            
            objectAnimationNode.PositionTrack.EasingFunction = Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction;

            

            // After position animation is completed, we start simple rotation and scale animation
            double rotationScaleStartFrame = objectAnimationNode.PositionTrack.LastFrame;

            objectAnimationNode.RotationTrack.Keys.Add(new AnglesRotationKeyFrame(0, 0, 0));
            objectAnimationNode.RotationTrack.Keys.Add(new AnglesRotationKeyFrame(rotationScaleStartFrame, 0, 0));
            objectAnimationNode.RotationTrack.Keys.Add(new AnglesRotationKeyFrame(rotationScaleStartFrame + 200, 0, 180));
            objectAnimationNode.RotationTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);

            objectAnimationNode.ScaleTrack.Keys.Add(new Vector3DKeyFrame(0,   new Vector3D(1, 1, 1))); // No scale until frame 600
            objectAnimationNode.ScaleTrack.Keys.Add(new Vector3DKeyFrame(rotationScaleStartFrame, new Vector3D(1, 1, 1))); // At frame 600 start scaling ...
            objectAnimationNode.ScaleTrack.Keys.Add(new Vector3DKeyFrame(rotationScaleStartFrame + 100, new Vector3D(3, 3, 3))); // ... to 3x the size ...
            objectAnimationNode.ScaleTrack.Keys.Add(new Vector3DKeyFrame(rotationScaleStartFrame + 200, new Vector3D(1, 1, 1))); // ... and then back to original size
            objectAnimationNode.ScaleTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            // Set this animation to revert but not to repeat
            _animationController.AutoReverse = false;
            _animationController.AutoRepeat = false;

            // Use StartAnimation helper method to stop current animation and start the animation defined in cameraAnimationNode
            StartAnimation(objectAnimationNode);
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



            _animationController.AnimationCompleted += delegate (object sender, EventArgs args)
            {
                StartAnimationButton.IsEnabled = true;
                SmoothAnimationCheckBox.IsEnabled = true;
            };

            // And start the animation
            _animationController.StartAnimation();

            StartAnimationButton.IsEnabled = false; // Disable button until end of animation
            SmoothAnimationCheckBox.IsEnabled = false; 


            // We can get details about the animation with calling GetDumpString method on AnimationController or any other animation related class.
            // It is also possible to call Dump method in the Visual Studio immediate method. This will dump the details to the same window.
            DumpTextBox.Text = _animationController.GetDumpString();
        }

        private void UpdateUsedPositions()
        {
            if (SmoothAnimationCheckBox.IsChecked ?? false)
            {
                // Use BezierCurve to create curve through our positions
                _usedAnimationPositions = Ab3d.Utilities.BezierCurve.CreateBezierCurveThroughPoints(_originalAnimationPositions, positionsPerSegment: 10);
            }
            else
            {
                _usedAnimationPositions = new Point3DCollection(_originalAnimationPositions);
            }

            AnimationPathVisual3D.Positions = _usedAnimationPositions;
            AnimationPathVisual3D.Transform = new TranslateTransform3D(-90, 1, 0);
        }
    }
}
