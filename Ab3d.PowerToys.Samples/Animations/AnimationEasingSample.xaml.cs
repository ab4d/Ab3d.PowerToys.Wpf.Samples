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
    /// Interaction logic for AnimationEasingSample.xaml
    /// </summary>
    public partial class AnimationEasingSample : Page
    {
        private AnimationController _animationController;

        private Position3DKeyFrame[] _positionKeyFrames;

        public AnimationEasingSample()
        {
            InitializeComponent();


            _positionKeyFrames = new Position3DKeyFrame[]
            {
                new Position3DKeyFrame(0, new Point3D(0, 0, 0)),
                new Position3DKeyFrame(100, new Point3D(100, 0, 0)),
                new Position3DKeyFrame(200, new Point3D(200, 0, 0)),
                new Position3DKeyFrame(300, new Point3D(300, 0, 0)),
            };


            _animationController = new AnimationController();
            _animationController.AutoRepeat = true;


            var sphere1AnimationNode = CreateAnimationNode(Sphere1, _positionKeyFrames);
            //sphere1AnimationNode.PositionTrack.SetInterpolationToAllKeys(BSplineInterpolation.LinearInterpolation); // when no interpolation is used, then a linear interpolation is used
            _animationController.AnimationNodes.Add(sphere1AnimationNode);

            var sphere2AnimationNode = CreateAnimationNode(Sphere2, _positionKeyFrames);
            sphere2AnimationNode.PositionTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);
            _animationController.AnimationNodes.Add(sphere2AnimationNode);

            var sphere3AnimationNode = CreateAnimationNode(Sphere3, _positionKeyFrames);
            sphere3AnimationNode.PositionTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseOutFunction);
            _animationController.AnimationNodes.Add(sphere3AnimationNode);

            var sphere4AnimationNode = CreateAnimationNode(Sphere4, _positionKeyFrames);
            sphere4AnimationNode.PositionTrack.EasingFunction = Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction;
            _animationController.AnimationNodes.Add(sphere4AnimationNode);


            AddPositionKeyLines(sphere1AnimationNode.PositionTrack, majorLineColor: Colors.Black, minorLineColor: Colors.Red, startPosition: Sphere1.CenterPosition, segmentsCount: 30);
            AddPositionKeyLines(sphere2AnimationNode.PositionTrack, majorLineColor: Colors.Black, minorLineColor: Colors.Red, startPosition: Sphere2.CenterPosition, segmentsCount: 30);
            AddPositionKeyLines(sphere3AnimationNode.PositionTrack, majorLineColor: Colors.Black, minorLineColor: Colors.Red, startPosition: Sphere3.CenterPosition, segmentsCount: 30);
            AddPositionKeyLines(sphere4AnimationNode.PositionTrack, majorLineColor: Colors.Black, minorLineColor: Colors.Red, startPosition: Sphere4.CenterPosition, segmentsCount: 30);


            _animationController.StartAnimation(subscribeToRenderingEvent: true);
        }

        private Visual3DAnimationNode CreateAnimationNode(Visual3D visual, IList<Position3DKeyFrame> positionKeyFrames)
        {
            var animationNode = new Visual3DAnimationNode(visual);

            foreach (var positionKeyFrame in positionKeyFrames)
                animationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(positionKeyFrame.FrameNumber, positionKeyFrame.Position));

            return animationNode;
        }

        private void AddPositionKeyLines(Position3DTrack positionTrack, Color majorLineColor, Color minorLineColor, Point3D startPosition, int segmentsCount)
        {
            // First create WireCrossVisual3D and PolyLineVisual3D at the key frame positions
            var point3DCollection = new Point3DCollection(positionTrack.KeysCount);

            foreach (var positionKeyFrame in positionTrack.Keys)
            {
                point3DCollection.Add(positionKeyFrame.Position);

                var wireCrossVisual3D = new Ab3d.Visuals.WireCrossVisual3D()
                {
                    Position = new Point3D(positionKeyFrame.Position.X + startPosition.X, positionKeyFrame.Position.Y + 1, positionKeyFrame.Position.Z + startPosition.Z),
                    LinesLength = 8,
                    LineThickness = 2,
                    LineColor = majorLineColor
                };

                MainViewport.Children.Add(wireCrossVisual3D);
            }

            var polyLineVisual3D = new Ab3d.Visuals.PolyLineVisual3D()
            {
                LineColor = majorLineColor,
                LineThickness = 2,
                Transform = new TranslateTransform3D(startPosition.X, 1, startPosition.Z),
                Positions = point3DCollection
            };

            MainViewport.Children.Add(polyLineVisual3D);


            // Now add minor WireCrossVisual3D that will show how animation progresses through time
            double framesPerSegment = (double)positionTrack.LastFrame / (double)segmentsCount;

            Vector3D startPositionOffset = new Vector3D(startPosition.X, 0.8, startPosition.Z);
            for (int i = 0; i <= segmentsCount; i++)
            {
                double frameNumber = i * framesPerSegment;

                Point3D positionForFrame = positionTrack.GetPositionForFrame(frameNumber) + startPositionOffset;

                var wireCrossVisual3D = new Ab3d.Visuals.WireCrossVisual3D()
                {
                    Position = positionForFrame,
                    LineColor = minorLineColor,
                    LineThickness = 1,
                    LinesLength = 6
                };

                MainViewport.Children.Add(wireCrossVisual3D);
            }
        }
    }
}
