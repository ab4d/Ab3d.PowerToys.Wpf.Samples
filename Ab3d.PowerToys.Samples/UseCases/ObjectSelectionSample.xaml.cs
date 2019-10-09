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
using Ab3d.Common.EventManager3D;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for ObjectSelectionSample.xaml
    /// </summary>
    public partial class ObjectSelectionSample : Page
    {
        private Material _standardMaterial;
        private Material _selectedMaterial;

        private Ab3d.Visuals.BoxVisual3D _selectedBoxVisual3D;

        private Ab3d.Visuals.WireBoxVisual3D _wireBoxVisual3D;

        public ObjectSelectionSample()
        {
            InitializeComponent();

            _standardMaterial = new DiffuseMaterial(Brushes.DeepSkyBlue);
            _standardMaterial.Freeze();

            _selectedMaterial = new DiffuseMaterial(Brushes.Yellow);
            _selectedMaterial.Freeze();

            // _wireBoxVisual3D will be used to show box with mouse over
            _wireBoxVisual3D = new Ab3d.Visuals.WireBoxVisual3D()
            {
                LineColor = Colors.Yellow,
                LineThickness = 2
            };

            CreateSceneObjects();


            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                // Start with selecting 8th box
                SelectBox(8);
            };
        }

        private void SelectBox(int boxIndex)
        {
            var allBoxes = SelectionRootModelVisual3D.Children.OfType<Ab3d.Visuals.BoxVisual3D>().ToList();

            boxIndex = boxIndex % allBoxes.Count; // put index in range of all boxes count

            SelectBox(allBoxes[boxIndex]);
        }

        private int GetCurrentlySelectedBoxIndex()
        {
            if (_selectedBoxVisual3D == null)
                return -1;

            var allBoxes = SelectionRootModelVisual3D.Children.OfType<Ab3d.Visuals.BoxVisual3D>().ToList();

            int index = allBoxes.IndexOf(_selectedBoxVisual3D);

            return index;
        }

        private void SelectBox(BoxVisual3D selectedBoxVisual3D)
        {
            if (_selectedBoxVisual3D != null)
                _selectedBoxVisual3D.Material = _standardMaterial;

            if (selectedBoxVisual3D != null)
            {
                selectedBoxVisual3D.Material = _selectedMaterial;
                _selectedBoxVisual3D = selectedBoxVisual3D;

                MoveCameraTo(selectedBoxVisual3D);
            }
        }

        private void MoveCameraTo(BoxVisual3D selectedBoxVisual3D)
        {
            // We will animate the camera's TargetPosition from the current position to the center of the selected Box
            // We will also animate camera distance (it will increase slightly and then decrease)
            var targetPosition = selectedBoxVisual3D.CenterPosition;

            if (MathUtils.IsSame(targetPosition, Camera1.TargetPosition))
                return; // If camera is already pointing to the desired location, we do not need to create an animation


            // Create a new CameraAnimationNode that will animate the Camera1
            var cameraAnimationNode = new CameraAnimationNode(Camera1);


            // Move the predefined camera rotation in 200 frames (2 seconds in when using 100 animation frames per second setting)
            cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(frameNumber: 0, position: Camera1.TargetPosition));
            cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(frameNumber: 200, position: targetPosition));

            // It is possible to set different interpolation mode to each KeyFrame, but it is easier
            // to set the same interpolation mode to all key frames with SetInterpolationToAllKeys method.
            cameraAnimationNode.PositionTrack.EasingFunction = Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction;


            // Animate camera distance from the current distance to 100
            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 0, doubleValue: Camera1.Distance));

            // If camera is close to the box, then we animate distance with increasing it at first and then going closer later.
            // If camera is farther away, then we go directly to the final distance.
            if (Camera1.Distance < 200)
                cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 100, doubleValue: 200));

            cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 200, doubleValue: 100));

            cameraAnimationNode.DistanceTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.SinusoidalEaseInOutFunction);


            // Use StartAnimation helper method to stop current animation and start the animation defined in cameraAnimationNode
            StartAnimation(cameraAnimationNode);
        }

        private void StartAnimation(AnimationNodeBase animationNode)
        {
            // First stop current animation and ...
            if (Camera1.AnimationController.IsAnimating)
                Camera1.AnimationController.StopAnimation();


            // NormalizeAngles normalizes the Heading, Attitude and Bank angles so that their values are between -180 and 180 (because normalizeTo180Degrees is true).
            // For example, this methods converts 390 value into 30. This prevent animating 390 to 40 instead of 30 to 40.
            Camera1.NormalizeAngles(normalizeTo180Degrees: true);


            // ... clear any existing AnimationNodes
            Camera1.AnimationController.AnimationNodes.Clear();

            // Set animation speed with setting FramesPerSecond.
            // Though this value does not have any effect on rendering, its default value is set to 60.
            // But with setting the FramesPerSecond to 100, it is easier to define the FrameNumber values for KeyFrames,
            // for example FrameNumber = 300 is at 3rd second; FrameNumber = 500 is at 5th second, etc.
            Camera1.AnimationController.FramesPerSecond = 100;

            Camera1.AnimationController.AutoRepeat = false;
            Camera1.AnimationController.AutoReverse = false;
            Camera1.AnimationController.AutoStopAnimation = true;


            // Add the CameraAnimationNode to animation controller
            Camera1.AnimationController.AnimationNodes.Add(animationNode);

            // And start the animation
            Camera1.AnimationController.StartAnimation();
        }

        private void CreateSceneObjects()
        {
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    var boxVisual3D = new Ab3d.Visuals.BoxVisual3D()
                    {
                        CenterPosition = new Point3D(40 * x - 100, 6, y * 40 - 80),
                        Size = new Size3D(10, 10, 10),
                        Material = _standardMaterial
                    };

                    SelectionRootModelVisual3D.Children.Add(boxVisual3D);
                }
            }

            // Use EventManager3D to handle clicking on selected boxes
            var eventManager3D = new Ab3d.Utilities.EventManager3D(MainViewport);

            var multiVisualEventSource3D = new MultiVisualEventSource3D(SelectionRootModelVisual3D.Children);

            multiVisualEventSource3D.MouseEnter += delegate (object sender, Mouse3DEventArgs e)
            {
                Mouse.OverrideCursor = Cursors.Hand;

                var hitBoxVisual3D = e.HitObject as Ab3d.Visuals.BoxVisual3D;
                if (hitBoxVisual3D != null)
                {
                    _wireBoxVisual3D.CenterPosition = hitBoxVisual3D.CenterPosition;
                    _wireBoxVisual3D.Size = hitBoxVisual3D.Size;

                    MainViewport.Children.Add(_wireBoxVisual3D);
                }
            };

            multiVisualEventSource3D.MouseLeave += delegate (object sender, Mouse3DEventArgs e)
            {
                Mouse.OverrideCursor = null;

                if (_wireBoxVisual3D != null)
                    MainViewport.Children.Remove(_wireBoxVisual3D);
            };

            multiVisualEventSource3D.MouseClick += delegate (object sender, MouseButton3DEventArgs e)
            {
                if (_selectedBoxVisual3D != null)
                    _selectedBoxVisual3D.Material = _standardMaterial;

                var hitBoxVisual3D = e.HitObject as Ab3d.Visuals.BoxVisual3D;
                if (hitBoxVisual3D != null)
                {
                    hitBoxVisual3D.Material = _selectedMaterial;
                    _selectedBoxVisual3D = hitBoxVisual3D;

                    if (_wireBoxVisual3D != null)
                        MainViewport.Children.Remove(_wireBoxVisual3D);

                    MoveCameraTo(hitBoxVisual3D);
                }
                else
                {
                    _selectedBoxVisual3D = null;
                }
            };

            eventManager3D.RegisterEventSource3D(multiVisualEventSource3D);

            // Exclude _wireBoxVisual3D from receiving mouse events
            eventManager3D.RegisterExcludedVisual3D(_wireBoxVisual3D);
        }

        private void NextBoxButton_OnClick(object sender, RoutedEventArgs e)
        {
            var currentIndex = GetCurrentlySelectedBoxIndex();
            SelectBox(currentIndex + 1);
        }

        private void PrevousBoxButton_OnClick(object sender, RoutedEventArgs e)
        {
            var currentIndex = GetCurrentlySelectedBoxIndex();
            SelectBox(currentIndex - 1);
        }
    }
}
