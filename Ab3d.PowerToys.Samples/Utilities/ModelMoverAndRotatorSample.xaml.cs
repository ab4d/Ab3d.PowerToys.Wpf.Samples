//#define USE_GENERIC_MODEL3D // Uncomment define to use the more generic 3D models instead of BoxUIElement3D

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
using Ab3d.Common;
using Ab3d.Common.Cameras;
using Ab3d.Common.EventManager3D;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for ModelMoverAndRotatorSample.xaml
    /// </summary>
    public partial class ModelMoverAndRotatorSample : Page
    {

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // !!!  IMPORTANT for using this sample inside Ab3d.DXEngine  !!!
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //
        // When MouseMoverVisual3D is used inside DXEngine, the mouse events on UIElement3D objects that are used inside MouseMoverVisual3D will not work.
        // See the ModelMoverInsideObjectSample sample that comes with Ab3d.DXEngine samples project on how to use MouseMoverVisual3D inside DXEngine.

        private ModelMoverVisual3D _modelMover;

        private Point3D _startMovePosition;
        private double _startRotationAngle;

        private AxisAngleRotation3D _xAxisAngleRotation3D;
        private AxisAngleRotation3D _yAxisAngleRotation3D;
        private AxisAngleRotation3D _zAxisAngleRotation3D;

        private AxisAngleRotation3D _selectedAxisAngleRotation3D;

        private TranslateTransform3D _translateTransform3D;
        private ModelRotatorVisual3D _modelRotator;

        private Point3D _initialPosition;
        private Transform3DGroup _transform3DGroup;


        public ModelMoverAndRotatorSample()
        {
            InitializeComponent();

            CreateScene();
            SetupModelMover();
            SetupModelRotator();


            // We need to synchronize the Camera and Lights in OverlayViewport with the camera in the MainViewport
            Camera1.CameraChanged += delegate (object s, CameraChangedRoutedEventArgs args)
            {
                OverlayViewport.Camera = MainViewport.Camera;
                OverlayViewportLight.Direction = ((DirectionalLight)Camera1.CameraLight).Direction;
            };
        }

        private void CreateScene()
        {
            // Load sample model
            var readerObj = new Ab3d.ReaderObj();
            var rootModel3DGroup = readerObj.ReadModel3D("pack://application:,,,/Ab3d.PowerToys.Samples;component/Resources/ObjFiles/robotarm.obj") as Model3DGroup;

            _initialPosition = rootModel3DGroup.Bounds.GetCenterPosition();

            RootModelVisual3D.Content = rootModel3DGroup;

            _transform3DGroup = new Transform3DGroup();

            _xAxisAngleRotation3D = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0);
            _yAxisAngleRotation3D = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
            _zAxisAngleRotation3D = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
            _translateTransform3D = new TranslateTransform3D();

            _transform3DGroup.Children.Add(new RotateTransform3D(_xAxisAngleRotation3D));
            _transform3DGroup.Children.Add(new RotateTransform3D(_yAxisAngleRotation3D));
            _transform3DGroup.Children.Add(new RotateTransform3D(_zAxisAngleRotation3D));
            _transform3DGroup.Children.Add(_translateTransform3D);

            RootModelVisual3D.Transform = _transform3DGroup;
        }

        private void SetupModelMover()
        {
            // Create a new ModelMover
            _modelMover = new ModelMoverVisual3D();

            var selectedModelBounds = RootModelVisual3D.Content.Bounds;
            _modelMover.Position = _initialPosition;

            // Calculate axis length from model size
            double axisLength = Math.Max(selectedModelBounds.Size.X, Math.Max(selectedModelBounds.Size.Y, selectedModelBounds.Size.Z));

            _modelMover.AxisLength = axisLength;

            // Set AxisRadius and AxisArrowRadius based on axis length
            _modelMover.AxisRadius = axisLength / 100;
            _modelMover.AxisArrowRadius = _modelMover.AxisRadius * 3;


            // Setup event handlers
            _modelMover.ModelMoveStarted += delegate (object o, EventArgs eventArgs)
            {
                _startMovePosition = new Point3D(_translateTransform3D.OffsetX, _translateTransform3D.OffsetY, _translateTransform3D.OffsetZ);
                _modelMover.Position = _initialPosition.ToVector3D() + _startMovePosition;
            };

            _modelMover.ModelMoved += delegate(object o, Ab3d.Common.ModelMovedEventArgs e)
            {
                var newCenterPosition = _startMovePosition + e.MoveVector3D;

                //if (Math.Abs(newCenterPosition.X) > 2000 ||
                //    Math.Abs(newCenterPosition.Y) > 2000 ||
                //    Math.Abs(newCenterPosition.Z) > 2000)
                //{
                //    InfoTextBlock.Text = "Move out of range";
                //    return;
                //}

                // When model is moved we get the updated MoveVector3D
                // We use MoveVector3D to change the _currentTranslateTransform3D that is used on the currently selected model and on the ModelMover object
                _translateTransform3D.OffsetX = newCenterPosition.X;
                _translateTransform3D.OffsetY = newCenterPosition.Y;
                _translateTransform3D.OffsetZ = newCenterPosition.Z;

                _modelMover.Position = _initialPosition.ToVector3D() + newCenterPosition;

                if (_modelRotator != null)
                    _modelRotator.Position = newCenterPosition;
            };

            _modelMover.ModelMoveEnded += delegate(object sender, EventArgs args)
            {
            };


            OverlayViewport.Children.Add(_modelMover);
        }
        
        private void SetupModelRotator()
        {
            _modelRotator = new ModelRotatorVisual3D();

            var selectedModelBounds = RootModelVisual3D.Content.Bounds;

            // Calculate axis length from model size
            double circleWidth = Math.Max(selectedModelBounds.Size.X, Math.Max(selectedModelBounds.Size.Y, selectedModelBounds.Size.Z));

            _modelRotator.InnerRadius = circleWidth * 0.6;
            _modelRotator.OuterRadius = _modelRotator.InnerRadius + 10;


            // Setup events on ModelRotatorVisual3D
            _modelRotator.ModelRotateStarted += delegate (object sender, ModelRotatedEventArgs args)
            {
                _selectedAxisAngleRotation3D = null;

                if (args.RotationAxis == Ab3d.Common.Constants.XAxis)
                    _selectedAxisAngleRotation3D = _xAxisAngleRotation3D;

                else if (args.RotationAxis == Ab3d.Common.Constants.YAxis)
                    _selectedAxisAngleRotation3D = _yAxisAngleRotation3D;

                else if (args.RotationAxis == Ab3d.Common.Constants.ZAxis)
                    _selectedAxisAngleRotation3D = _zAxisAngleRotation3D;


                if ((IsCumulativeRotationCheckBox.IsChecked ?? false) && _selectedAxisAngleRotation3D != null)
                {
                    _selectedAxisAngleRotation3D = new AxisAngleRotation3D(_selectedAxisAngleRotation3D.Axis, 0);
                    var rotateTransform3D = new RotateTransform3D(_selectedAxisAngleRotation3D);

                    // Insert before TranslateTransform3D
                    _transform3DGroup.Children.Insert(_transform3DGroup.Children.Count - 2, rotateTransform3D);

                    _startRotationAngle = 0;
                }
                else
                {
                    if (_selectedAxisAngleRotation3D != null)
                        _startRotationAngle = _selectedAxisAngleRotation3D.Angle;
                }
            };

            _modelRotator.ModelRotated += delegate (object sender, ModelRotatedEventArgs args)
            {
                if (_selectedAxisAngleRotation3D == null)
                    return;

                _selectedAxisAngleRotation3D.Angle = _startRotationAngle + args.RotationAngle;
            };

            _modelRotator.ModelRotateEnded += delegate(object sender, ModelRotatedEventArgs args)
            {
                _selectedAxisAngleRotation3D = null;
            };


            OverlayViewport.Children.Add(_modelRotator);
        }

        private void OnShownCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;


            if (ShowModelMoverCheckBox.IsChecked ?? false)
            {
                if (!OverlayViewport.Children.Contains(_modelMover))
                    OverlayViewport.Children.Add(_modelMover);
            }
            else
            {
                if (OverlayViewport.Children.Contains(_modelMover))
                    OverlayViewport.Children.Remove(_modelMover);
            }

            if (ShowModelRotatorCheckBox.IsChecked ?? false)
            {
                if (!OverlayViewport.Children.Contains(_modelRotator))
                    OverlayViewport.Children.Add(_modelRotator);
            }
            else
            {
                if (OverlayViewport.Children.Contains(_modelRotator))
                    OverlayViewport.Children.Remove(_modelRotator);
            }
        }
    }
}
