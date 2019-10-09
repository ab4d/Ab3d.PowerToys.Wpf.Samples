using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for AnimationSettings.xaml
    /// </summary>
    public partial class AnimationSettings : Page
    {
        private Func<double, double> _selectedEasingFunction;
        private AnimationController _animationController;
        private Visual3DAnimationNode _visual3DAnimationNode;
        private Line _currentTimeLine;
        
        public AnimationSettings()
        {
            InitializeComponent();

            _selectedEasingFunction = GetSelectedEasingFunction();

            if (!DesignerProperties.GetIsInDesignMode(this))
                UpdateEasingGraph();

            _animationController = new AnimationController();

            // After each animation frame we will update the position of the red line that shows where the animation progress is
            _animationController.AfterFrameUpdated += delegate (object sender, EventArgs args) { UpdateCurrentTimeLine(); };


            _visual3DAnimationNode = new Visual3DAnimationNode(Sphere1);

            _visual3DAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(0, new Point3D(0, 0, 0)));
            _visual3DAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(120, new Point3D(0, 100, 0)));

            _animationController.AnimationNodes.Add(_visual3DAnimationNode);
            _animationController.FramesPerSecond = 60;
            _animationController.AutoRepeat = true;
            _animationController.AutoReverse = true;

            _visual3DAnimationNode.PositionTrack.EasingFunction = _selectedEasingFunction;


            _animationController.StartAnimation(subscribeToRenderingEvent: true);

            _animationController.AnimationStopped += delegate(object sender, EventArgs args)
            {
                UpdateStartStopAnimationButton();
            };

            this.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                // AnimationController will stop animating when it is collected by GC, but anyway it is better to stop the animation when we leave this sample
                _animationController.StopAnimation();
            };
        }

        private Func<double, double> GetSelectedEasingFunction()
        {
            if (QuadraticEaseInRadioButton.IsChecked ?? false)
                return Ab3d.Animation.EasingFunctions.QuadraticEaseInFunction;

            if (QuadraticEaseInOutRadioButton.IsChecked ?? false)
                return Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction;

            if (QuadraticEaseOutRadioButton.IsChecked ?? false)
                return Ab3d.Animation.EasingFunctions.QuadraticEaseOutFunction;

            if (CubicEaseInOutRadioButton.IsChecked ?? false)
                return Ab3d.Animation.EasingFunctions.CubicEaseInOutFunction;

            if (ExponentEaseInOutRadioButton.IsChecked ?? false)
                return Ab3d.Animation.EasingFunctions.ExponentialEaseInOutFunction;

            if (CustomEasingtRadioButton.IsChecked ?? false)
                return CustomEasingFunction;

            return null; // No easing / interpolation
        }

        // This function is the same as Ab3d.Animation.EasingFunctions.SinusoidalEaseInOutFunction
        // It is written here to show how to defined a custom easing function
        private static double CustomEasingFunction(double t)
        {
            return -0.5 * (Math.Cos(t * Math.PI) - 1);
        }

        private void UpdateCurrentTimeLine()
        {
            double frameNumber = _animationController.GetFrameNumber();

            double position = frameNumber / _animationController.LastFrameNumber;
            double xPos = position * AnimationGraphCanvas.Width;

            _currentTimeLine.X1 = xPos;
            _currentTimeLine.X2 = xPos;
        }

        private void UpdateEasingGraph()
        {
            AnimationGraphCanvas.Children.Clear();

            Polyline graph = new Polyline();
            graph.Stroke = Brushes.Red;
            graph.StrokeThickness = 1;

            var width = AnimationGraphCanvas.Width;
            var height = AnimationGraphCanvas.Height;

            var linearLine = new Line()
            {
                X1 = 0,
                Y1 = height,
                X2 = width,
                Y2 = 0,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };

            AnimationGraphCanvas.Children.Add(linearLine);


            for (double t = 0; t < 1.01; t += 0.025)
            {
                double x, y;

                x = t * width;

                if (_selectedEasingFunction != null)
                    y = 1 - _selectedEasingFunction(t);
                else
                    y = 1 - t;

                y *= height;

                graph.Points.Add(new Point(x, y));
            }

            AnimationGraphCanvas.Children.Add(graph);



            _currentTimeLine = new Line()
            {
                X1 = 0,
                Y1 = 0,
                X2 = 0,
                Y2 = height,
                Stroke = Brushes.Blue,
                StrokeThickness = 1
            };

            AnimationGraphCanvas.Children.Add(_currentTimeLine);

        }

        private void SetNewEasingFunction(Func<double, double> easingFunction)
        {
            _selectedEasingFunction = easingFunction;
            UpdateEasingGraph();

            _visual3DAnimationNode.PositionTrack.EasingFunction = easingFunction;
        }

        private void OnEasingTypeCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var selectedEasingFunction = GetSelectedEasingFunction();
            SetNewEasingFunction(selectedEasingFunction);
        }

        private void OnAnimationControlChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            _animationController.AutoRepeat  = AutoRepeatCheckBox.IsChecked ?? false;
            _animationController.AutoReverse = AutoReverseCheckBox.IsChecked ?? false;
        }

        private void UpdateStartStopAnimationButton()
        {
            if (_animationController.IsAnimationStarted)
                StartStopAnimationButton.Content = "Stop animation";
            else
                StartStopAnimationButton.Content = "Start animation";
        }

        private void FpsSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            _animationController.FramesPerSecond = FpsSlider.Value;
        }

        private void StartStopAnimationButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_animationController.IsAnimationStarted)
                _animationController.StopAnimation();
            else
                _animationController.StartAnimation();

            UpdateStartStopAnimationButton();
        }

        private void PauseAnimationButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_animationController.IsAnimationPaused)
            {
                _animationController.ResumeAnimation();
                PauseAnimationButton.Content = "Pause animation";
            }
            else
            {
                _animationController.PauseAnimation();
                PauseAnimationButton.Content = "Resume animation";
            }
        }
    }
}
