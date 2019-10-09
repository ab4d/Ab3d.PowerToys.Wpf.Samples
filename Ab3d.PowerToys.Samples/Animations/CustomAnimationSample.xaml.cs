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
using Ab3d.Cameras;

namespace Ab3d.PowerToys.Samples.Animations
{
    /// <summary>
    /// Interaction logic for CustomAnimationSample.xaml
    /// </summary>
    public partial class CustomAnimationSample : Page
    {
        private SolidColorBrushAnimationNode _solidColorBrushAnimationNode;
        private AnimationController _animationController;
        private SolidColorBrush _solidColorBrush;
        private Color _initialColor;

        public CustomAnimationSample()
        {
            InitializeComponent();

            // First load a teapot model
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/ObjFiles/Teapot.obj");

            var readerObj = new Ab3d.ReaderObj();
            var readModel3D = readerObj.ReadModel3D(fileName) as GeometryModel3D; // We assume that a single GeometryModel3D is returned

            if (readModel3D == null)
                return;


            // Set initial material - white
            double initialColorAmount = 1.0;
            byte initialColorByte = (byte) (initialColorAmount * 255.0);

            _initialColor = Color.FromRgb(initialColorByte, initialColorByte, initialColorByte);

            _solidColorBrush = new SolidColorBrush(_initialColor);
            var diffuseMaterial = new DiffuseMaterial(_solidColorBrush);

            readModel3D.Material = diffuseMaterial;


            // Show teapot model
            var modelVisual3D = readModel3D.CreateModelVisual3D();
            MainViewport.Children.Add(modelVisual3D);


            // Create a custom SolidColorBrushAnimationNode (defined below)
            _solidColorBrushAnimationNode = new SolidColorBrushAnimationNode(_solidColorBrush);
            _solidColorBrushAnimationNode.ColorAmountTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 0, doubleValue: initialColorAmount)); 
            _solidColorBrushAnimationNode.ColorAmountTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 100, doubleValue: 0.0));                // quickly (1s) animate to 0 (black)
            _solidColorBrushAnimationNode.ColorAmountTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 400, doubleValue: initialColorAmount)); // slowly (3s) animate back to initial value (white)

            _solidColorBrushAnimationNode.ColorAmountTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);


            _animationController = new AnimationController();
            _animationController.FramesPerSecond = 100;
            _animationController.AutoRepeat = true;
            _animationController.AutoReverse = false;

            _animationController.AnimationNodes.Add(_solidColorBrushAnimationNode);
        }


        private void StartAnimationButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_animationController.IsAnimating)
            {
                StartAnimationButton.Content = "Start animation";
                ResetButton.IsEnabled = true;

                _animationController.StopAnimation();
            }
            else
            {
                StartAnimationButton.Content = "Stop animation";
                ResetButton.IsEnabled = false;

                _animationController.StartAnimation(subscribeToRenderingEvent: true);
            }
        }

        private void ResetButton_OnClick(object sender, RoutedEventArgs e)
        {
            _solidColorBrush.Color = _initialColor;
        }

        private void OnColorCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            _solidColorBrushAnimationNode.AnimateAlpha = AlphaCheckBox.IsChecked ?? false;
            _solidColorBrushAnimationNode.AnimateRed = RedCheckBox.IsChecked ?? false;
            _solidColorBrushAnimationNode.AnimateGreen = GreenCheckBox.IsChecked ?? false;
            _solidColorBrushAnimationNode.AnimateBlue = BlueCheckBox.IsChecked ?? false;
        }
    }

    /// <summary>
    /// SolidColorBrushAnimationNode provides logic to animate color of the SolidColorBrush.
    /// </summary>
    public class SolidColorBrushAnimationNode : AnimationNodeBase
    {
        /// <summary>
        /// Gets the first defined frame number for this AnimationNode.
        /// </summary>
        public override double FirstFrameNumber
        {
            // Return smallest frame number. If there are multiple Tracks defined then use code similar to: Math.Min(Math.Min(RotationTrack.FirstFrame, PositionTrack.FirstFrame), DistanceTrack.FirstFrame);
            get { return ColorAmountTrack.FirstFrame; }
        }

        /// <summary>
        /// Gets the last defined frame number for this AnimationNode.
        /// </summary>
        public override double LastFrameNumber
        {
            get { return ColorAmountTrack.LastFrame; }
        }

        /// <summary>
        /// SolidColorBrush that is animated. This value is set in the GeometryModel3DColorAnimationNode's constructor.
        /// </summary>
        public SolidColorBrush SolidColorBrush { get; private set; }

        /// <summary>
        /// Gets a DoubleTrack that defines double value key frames for color amount.
        /// </summary>
        public DoubleTrack ColorAmountTrack { get; private set; }

        // It is also possible to use:
        // - Position3DTrack
        // - Vector3DTrack
        // - RotationTrack
        // - CameraRotationTrack

        public bool AnimateAlpha { get; set; }
        public bool AnimateRed { get; set; }
        public bool AnimateGreen { get; set; }
        public bool AnimateBlue { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="solidColorBrush">SolidColorBrush that is animated</param>
        public SolidColorBrushAnimationNode(SolidColorBrush solidColorBrush)
        {
            if (solidColorBrush == null) throw new ArgumentNullException(nameof(solidColorBrush));

            AnimateAlpha = false;
            AnimateRed = true;
            AnimateGreen = true;
            AnimateBlue = true;

            SolidColorBrush = solidColorBrush;
            ColorAmountTrack = new DoubleTrack();
        }

        /// <inheritdoc />
        public override void GoToFrame(double frameNumber)
        {
            if (ColorAmountTrack.KeysCount == 0)
                return;

            var newDoubleValue = ColorAmountTrack.GetDoubleValueForFrame(frameNumber);

            newDoubleValue = Math.Min(1.0, Math.Max(0.0, newDoubleValue)); // Clip to range from 0 to 1

            byte newColorValue = (byte) (newDoubleValue * 255.0);

            var existingColor = SolidColorBrush.Color;
            SolidColorBrush.Color = Color.FromArgb(AnimateAlpha ? newColorValue : existingColor.A,
                                                   AnimateRed   ? newColorValue : existingColor.R,
                                                   AnimateGreen ? newColorValue : existingColor.G,
                                                   AnimateBlue  ? newColorValue : existingColor.B);
        }


        /// <summary>
        /// GetDumpString virtual method can be overridden to provide detailed description of this object.
        /// </summary>
        /// <returns>details about this object</returns>
        public override string GetDumpString()
        {
            var sb = new StringBuilder();

            var thisType = this.GetType();

            sb.Append(thisType.Name).AppendLine(":\r\n");
            sb.Append("DoubleTrack as ").AppendLine(this.ColorAmountTrack.GetDumpString());

            return sb.ToString();
        }
    }
}
