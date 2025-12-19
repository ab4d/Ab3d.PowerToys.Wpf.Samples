using Ab3d.Common.Cameras;
using Ab3d.Controls;
using Ab3d.Utilities;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Ab3d.Cameras;

namespace Ab3d.PowerToys.Samples.MouseCameraControllerSamples
{
    /// <summary>
    /// Interaction logic for CameraSmoothingSample.xaml
    /// </summary>
    public partial class CameraSmoothingSample : Page, ICompositionRenderingSubscriber
    {
        private double _previewPositionsPerSecond = 20;
        private double _previewPositionsXScale = 5;

        private double _targetHeading;
        private double _targetAttitude;
        private double _targetDistance;
        private double _maxDistance;
        
        private DateTime _lastPreviewUpdateTime;
        private PointCollection _headingPositions1 = new PointCollection();
        private PointCollection _headingPositions2 = new PointCollection();
        private PointCollection _attitudePositions1 = new PointCollection();
        private PointCollection _attitudePositions2 = new PointCollection();
        private PointCollection _distancePositions1 = new PointCollection();
        private PointCollection _distancePositions2 = new PointCollection();
        private Polyline _heading1Polyline;
        private Polyline _heading2Polyline;
        private Polyline _attitude1Polyline;
        private Polyline _attitude2Polyline;
        private Polyline _distance1Polyline;
        private Polyline _distance2Polyline;

        public CameraSmoothingSample()
        {
            InitializeComponent();

            // This samples initially sets CameraSmoothing to Slow preset to show the smoothing effect more clearly.
            // This enables rotation, movement and zoom smoothing.
            // To enable just some of those, use the SetAdvancedSmoothSettings method.
            MouseCameraController1.CameraSmoothing = MouseCameraController.CameraSmoothingPresets.Slow;

            // Subscribe to custom rotation and zoom smoothing only to get the target
            // heading, attitude and distance values that are shown in the graph.
            MouseCameraController1.SetAdvancedSmoothSettings(customRotationSmoothingFunction: CustomRotationSmoothingFunction,
                                                             customZoomSmoothingFunction: CustomZoomSmoothingFunction);
            
            MouseCameraControllerInfo1.AddCustomInfoLine(MouseCameraController.MouseAndKeyboardConditions.MiddleMouseButtonPressed, "Zoom");
            
            // Store initial values
            _targetHeading = Camera1.Heading;
            _targetAttitude = Camera1.Attitude;
            
            SetupPreviewCanvas();


            _targetDistance = Camera1.Distance;
            _maxDistance = _targetDistance * 2;

            // Instead of directly subscribing to CompositionTarget.Rendering event,
            // we use a safer CompositionRenderingHelper and ICompositionRenderingSubscriber
            // (this does not pin this class to static CompositionTarget).
            CompositionRenderingHelper.Instance.Subscribe(this);
            
            //CompositionTarget.Rendering += OnCompositionTargetOnRendering;
            this.Unloaded += (s, e) =>
            {
                CompositionRenderingHelper.Instance.Unsubscribe(this);
            };
        }

        private void CustomRotationSmoothingFunction(double targetHeading, double targetAttitude, double dt, ref double newHeadingChange, ref double newAttitudeChange)
        {
            // We subscribed to this function only to get the target and actual heading and attitude values for the preview graph.
            _targetHeading  = targetHeading;
            _targetAttitude = targetAttitude;
        }
        
        private void CustomZoomSmoothingFunction(double targetDistance, double targetCameraWidth, double elapsedTime, ref double newDistance, ref double newWidth)
        {
            // We subscribed to this function only to get the targetDistance for the preview graph.
            _targetDistance = targetDistance;
        }
        
        public void OnRendering(EventArgs e)
        {
            UpdatePreviewCanvas();
        }

        private void PresetComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var newSmoothingPreset = (MouseCameraController.CameraSmoothingPresets)PresetComboBox.SelectedIndex;
            
            // The easiest way to change the camera smoothing is to set the CameraSmoothing property.
            MouseCameraController1.CameraSmoothing = newSmoothingPreset;
            
            // You can also enable individual camera smoothing modes by calling the SetCameraSmoothingMode method:
            // MouseCameraController1.SetAdvancedSmoothSettings(isSmoothRotationEnabled: true,
            //                                                  isSmoothMovementEnabled: true,
            //                                                  isSmoothZoomEnabled: true,
            //                                                  customSmoothFactor: 16, // optional nullable parameter that sets the custom smooth factor (bigger values mean faster smoothing; Slow: 9; Normal: 16; Fast: 22)
            //                                                  customRotationSmoothingFunction: null, // optional
            //                                                  customMovementSmoothingFunction: null, // optional
            //                                                  customZoomSmoothingFunction: null); // optional
            //
            // // We can also call the following overload to set only one or all custom smoothing functions (see constructor of this sample):
            // MouseCameraController1.SetAdvancedSmoothSettings(customRotationSmoothingFunction: null, // optional
            //                                                  customMovementSmoothingFunction: null, // optional
            //                                                  customZoomSmoothingFunction: null); // optional
 
            if (newSmoothingPreset == MouseCameraController.CameraSmoothingPresets.Disabled)
            {
                _heading1Polyline.Visibility  = Visibility.Collapsed;
                _attitude1Polyline.Visibility = Visibility.Collapsed;
                _distance1Polyline.Visibility = Visibility.Collapsed;
            }
            else
            {
                _heading1Polyline.Visibility  = Visibility.Visible;
                _attitude1Polyline.Visibility = Visibility.Visible;
                _distance1Polyline.Visibility = Visibility.Visible;
            }
        }
        

        private void AddAngleValue(double angleValue, Polyline polyline)
        {
            var heading = CameraUtils.NormalizeAngleTo180(angleValue); // get angle in range from -180 to 180
            var positionValue = (heading / 360) + 0.5;                 // positionValue in range from 0 to 1
            
            var positions = polyline.Points;
            polyline.Points = null; // Disconnect the PointCollection so the change event is not fired for each point change

            AddPositionValue(positionValue, positions);

            polyline.Points = positions;
        }
        
        private void AddDistanceValue(double distanceValue, Polyline polyline)
        {
            var polylineValue = distanceValue / _maxDistance; // polylineValue in range from 0 to 1
            
            var positions = polyline.Points;
            polyline.Points = null; // Disconnect the PointCollection so the change event is not fired for each point change

            AddPositionValue(polylineValue, positions);

            polyline.Points = positions;
        }
        
        private void AddPositionValue(double positionValue, PointCollection positions)
        {
            double w = PreviewCanvas.Width;
            double h = PreviewCanvas.Height;

            int lastPosIndex = (int)(w / _previewPositionsXScale) - 1;
            double y = (1 - positionValue) * h; // invert y axis

            if (positions.Count <= lastPosIndex)
            {
                // Just add new position
                positions.Add(new Point(positions.Count * _previewPositionsXScale, y));
                return;
            }

            // remove first position and shift all other positions to left
            for (int i = 0; i < lastPosIndex; i++)
                positions[i] = new Point(i * _previewPositionsXScale, positions[i + 1].Y);

            // Set last position
            positions[lastPosIndex] = new Point(lastPosIndex * _previewPositionsXScale, y);
        }

        private void SetupPreviewCanvas()
        {
            _heading1Polyline = new Polyline()
            {
                Points = _headingPositions1,
                Stroke = Brushes.LightGreen,
                StrokeThickness = 2.2
            };

            PreviewCanvas.Children.Add(_heading1Polyline);
            
            
            _heading2Polyline = new Polyline()
            {
                Points = _headingPositions2,
                Stroke = Brushes.Green,
                StrokeThickness = 1
            };

            PreviewCanvas.Children.Add(_heading2Polyline);
            
            
            _attitude1Polyline = new Polyline()
            {
                Points = _attitudePositions1,
                Stroke = Brushes.LightBlue,
                StrokeThickness = 2.2
            };

            PreviewCanvas.Children.Add(_attitude1Polyline);
            
            
            _attitude2Polyline = new Polyline()
            {
                Points = _attitudePositions2,
                Stroke = Brushes.Blue,
                StrokeThickness = 1
            };

            PreviewCanvas.Children.Add(_attitude2Polyline); 
            
            
            _distance1Polyline = new Polyline()
            {
                Points = _distancePositions1,
                Stroke = Brushes.Gray,
                StrokeThickness = 2.2
            };

            PreviewCanvas.Children.Add(_distance1Polyline);
            
            
            _distance2Polyline = new Polyline()
            {
                Points = _distancePositions2,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            PreviewCanvas.Children.Add(_distance2Polyline);
        }
        
        private void UpdatePreviewCanvas()
        {
            var now = DateTime.Now;
            var diff = (now - _lastPreviewUpdateTime).TotalSeconds;
            var minTime = 1.0 / _previewPositionsPerSecond;
            
            if (diff < minTime) // update 30 times per second (by default)
                return;

            _lastPreviewUpdateTime = now;
            
            AddAngleValue(_targetHeading, _heading1Polyline);
            AddAngleValue(Camera1.Heading, _heading2Polyline);
            
            AddAngleValue(_targetAttitude, _attitude2Polyline);
            AddAngleValue(Camera1.Attitude, _attitude1Polyline);


            var cameraDistance = Camera1.Distance;

            // Update _maxDistance
            // Note that when _maxDistance increases, the old values in the graph will not be adjusted because we do not save the old distances but only positions in the polyline. It is not worth complicating this sample to make this 100% correct.
            _maxDistance = Math.Max(_maxDistance, _targetDistance * 1.1);
            _maxDistance = Math.Max(_maxDistance, cameraDistance * 1.1);
            
            AddDistanceValue(_targetDistance, _distance2Polyline);
            AddDistanceValue(cameraDistance, _distance1Polyline);

            HeadingValueTextBlock.Text  = CameraUtils.NormalizeAngleTo180(Camera1.Heading).ToString("F0");
            AttitudeValueTextBlock.Text = CameraUtils.NormalizeAngleTo180(Camera1.Attitude).ToString("F0");
            DistanceValueTextBlock.Text = Camera1.Distance.ToString("F0");
            OffsetValueTextBlock.Text = string.Format("{0:0} {1:0} {2:0}", Camera1.Offset.X, Camera1.Offset.Y, Camera1.Offset.Z);
        }
    }
}
