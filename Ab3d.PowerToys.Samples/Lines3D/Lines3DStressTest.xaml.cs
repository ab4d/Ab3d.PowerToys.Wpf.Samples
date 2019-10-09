using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Ab3d.Meshes;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    /// <summary>
    /// Interaction logic for Lines3DStressTest.xaml
    /// </summary>
    public partial class Lines3DStressTest : Page
    {
        private bool _isAnimationStarted;
        private DispatcherTimer _sliderTimer;

        private double _lastRefreshedHeading;
        private int _manualUpdatesCount;

        public Lines3DStressTest()
        {
            InitializeComponent();

            Ab3d.Utilities.LinesUpdater.Instance.IsEmissiveMaterialUsed = true;

            this.Loaded += Lines3DStressTest_Loaded;
            this.Unloaded += Lines3DStressTest_Unloaded;
        }

        void Lines3DStressTest_Unloaded(object sender, RoutedEventArgs e)
        {
            StopAnimation();

            _sliderTimer.Stop();
            _sliderTimer.Tick -= _sliderTimer_Tick;
            _sliderTimer = null;

            // When leaving set UpdateMode to Always
            Ab3d.Utilities.LinesUpdater.Instance.UpdateMode = Ab3d.Common.Utilities.LinesUpdaterMode.Always;
        }

        void Lines3DStressTest_Loaded(object sender, RoutedEventArgs e)
        {
            _sliderTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _sliderTimer.Interval = TimeSpan.FromSeconds(0.5);
            _sliderTimer.Tick += _sliderTimer_Tick;

            // This will update the camera so that when lines will be created (with calling Ab3d.Utilities.LinesUpdater.Instance.Refresh())
            // the camera will be already set so the lines will be created based on the correct camera matrixes.
            // If we would not call Refresh now, then the lines would be created based on the not-up-to-date camera
            // so when the camera would be refreshed in the Camera's Loaded event (called after this method) the camera would be updated and lines would need to be updated again.
            // This would create the lines geometry twice and would decrease the startup performance.
            // So with calling Refresh now, we prevent one unneeded lines regeneration.
            Camera1.Refresh(); 


            UpdateLines();
            StartAnimation();

            Camera1.CameraChanged += Camera1_CameraChanged;

            _manualUpdatesCount = 0;
        }

        void _sliderTimer_Tick(object sender, EventArgs e)
        {
            _sliderTimer.Stop();
            UpdateLines();
        }

        private void SegmentsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!SegmentsSlider.IsInitialized)
                return;

            // timer is used to delay the creation of wireframe so if user moves the slider from 40 to 80, the wireframe is not created for 50, 60 and 70 segments.
            // Restart the timer
            _sliderTimer.Stop();
            _sliderTimer.Start();
        }

        private void AnimationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimationStarted)
                StopAnimation();
            else
                StartAnimation();
        }

        private void StopAnimation()
        {
            Camera1.StopRotation();
            AnimationButton.Content = "Start camera animation";
            
            _isAnimationStarted = false;
        }

        private void StartAnimation()
        {
            Camera1.StartRotation(20, 0); // animate 20 degrees in second
            AnimationButton.Content = "Stop camera animation";

            _isAnimationStarted = true;
        }

        private void UpdateLines()
        {
            var sphereMesh = new Ab3d.Meshes.SphereMesh3D(new Point3D(0, 0, 0), 100, Convert.ToInt32(SegmentsSlider.Value));

            GeometryModel3D wireframeModel = Ab3d.Models.WireframeFactory.CreateWireframe(sphereMesh.Geometry, 1, Colors.White, MainViewport);

            MainModel3DGroup.Children.Clear();
            MainModel3DGroup.Children.Add(wireframeModel);

            // After the line is connected to Viewport3D we can create its geometry.
            // This is automatically done with LinesUpdater in its Rendering event handler.
            // But to get the linesGeometry.Positions.Count we can call the Refresh method manually to recreate the Geometry.
            Ab3d.Utilities.LinesUpdater.Instance.Refresh();

            PositionsTextBlock.Text = string.Format("Sphere positions count: {0}", sphereMesh.Geometry.Positions.Count);

            var linesGeometry = wireframeModel.Geometry as MeshGeometry3D;
            if (linesGeometry == null)
                LinesTextBlock.Text = "";
            else
                LinesTextBlock.Text = string.Format("3D Line Positions count: {0:0}", linesGeometry.Positions.Count / 4); // 4 positions for one line

            FpsMeter1.Reset(); // Resets the average
        }

        private void IsManualRefreshCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            // Disable automatic updating of the lines
            Ab3d.Utilities.LinesUpdater.Instance.UpdateMode = Ab3d.Common.Utilities.LinesUpdaterMode.Never;

            ManualRefreshPanel.IsEnabled = true;
            _lastRefreshedHeading = Camera1.Heading;
        }

        private void IsManualRefreshCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            // Enable automatic updating of the lines
            Ab3d.Utilities.LinesUpdater.Instance.UpdateMode = Ab3d.Common.Utilities.LinesUpdaterMode.Always;

            ManualRefreshPanel.IsEnabled = false;
        }

        private void RefreshLines()
        {
            // Manually update the 3D lines - all the lines are recreated based on the current camera position
            Ab3d.Utilities.LinesUpdater.Instance.Refresh();

            _lastRefreshedHeading = Camera1.Heading;

            _manualUpdatesCount ++;
            ManualUpdatesCountTextBox.Text = _manualUpdatesCount.ToString();
        }

        void Camera1_CameraChanged(object sender, Ab3d.Common.Cameras.CameraChangedRoutedEventArgs e)
        {
            // If automatic line updating is disabled, we can manually refresh the lines
            // This can be done with checking if the Heading property of the camera has been changed enough
            // For example if TwentyDegreesRadioButton is checked, this means that we will not update the lines until the Heading is changed for 20 degrees
            // Because recreation of big number of 3d lines can take some time, manually updating the lines can improve the performance significantly
            // If the change angle is not too big and the lines are not wide (in our sample the LineThicness is 2) the user should not see any anomalies on the lines.
            if (ManualRefreshPanel.IsEnabled)
            {
                double headingChangeAngle = GetCurrentHeadingChangeAngle();

                if (headingChangeAngle == 0)
                {
                    RefreshLines();
                }
                else if (headingChangeAngle != double.MaxValue)
                {
                    double currentHeading = Camera1.Heading;

                    if (currentHeading < _lastRefreshedHeading)
                    {
                        // angle changed from 360 to 0
                        _lastRefreshedHeading -= 360;
                    }

                    if ((currentHeading - _lastRefreshedHeading) > headingChangeAngle)
                        RefreshLines();
                }
            }
        }

        private double GetCurrentHeadingChangeAngle()
        {
            double headingChangeAngle;

            if (AlwaysRadioButton.IsChecked ?? false)
                headingChangeAngle = 0.0;
            else if (TenDegreesRadioButton.IsChecked ?? false)
                headingChangeAngle = 10.0;
            else if (TwentyDegreesRadioButton.IsChecked ?? false)
                headingChangeAngle = 20.0;
            else if (ThirtyDegreesRadioButton.IsChecked ?? false)
                headingChangeAngle = 30.0;
            else // if (NeverRadioButton.IsChecked ?? false)
                headingChangeAngle = double.MaxValue;

            return headingChangeAngle;
        }

        private void RefreshNowButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshLines();
        }
    }
}

