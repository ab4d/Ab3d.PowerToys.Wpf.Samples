using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Ab3d.Controls;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.MouseCameraControllerSamples
{
    /// <summary>
    /// Interaction logic for CustomMouseEventsSample.xaml
    /// </summary>
    public partial class CustomMouseEventsSample : Page
    {
        private bool _isProcessingMouse;
        
        private Point _lastMousePosition;
        private MouseCameraController _mouseCameraController;

        private double _mouseMoveThreshold = 3; // mouse move to start rotation / movement
        private double _movementSpeedFactor = 2;

        private bool _isMouseRotation;
        private bool _isMouseMovement;

        public CustomMouseEventsSample()
        {
            InitializeComponent();

            _mouseCameraController = new MouseCameraController()
            {
                ShowRotationCenterMarker = true,
                ZoomMode = MouseCameraController.CameraZoomMode.MousePosition,
                EventsSourceElement = ViewportBorder, // We need to set EventsSourceElement to show Rotation center marker
                TargetCamera = Camera1
            };

            ViewportBorder.MouseMove += OnMouseMove;

            CreateTestScene();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            // Only Left mouse button must be pressed
            var isCorrectMouseButtonPressed = e.LeftButton == MouseButtonState.Pressed &&
                                              e.MiddleButton == MouseButtonState.Released &&
                                              e.RightButton == MouseButtonState.Released &&
                                              Keyboard.Modifiers == ModifierKeys.None; // keyboard modifiers (shift, ctrl, alt)

            if (_isProcessingMouse)
            {
                if (!isCorrectMouseButtonPressed)
                {
                    _isProcessingMouse = false;
                    _isMouseRotation = false;
                    _isMouseMovement = false;

                    _mouseCameraController.StopCurrentMouseProcessing();
                }
                else
                {
                    var mousePosition = e.GetPosition(ViewportBorder);

                    double dx = mousePosition.X - _lastMousePosition.X;
                    double dy = mousePosition.Y - _lastMousePosition.Y;

                    if (InvertMovementCheckBox.IsChecked ?? false)
                        dy *= -1;

                    
                    if (SimultaneousRotateAndMoveCheckBox.IsChecked ?? false)
                    {
                        // Simple processing (allows simultaneous rotation and movement):

                        if (dx != 0)
                            _mouseCameraController.RotateCamera(-dx, 0);

                        if (dy != 0)
                        {
                            // If we would move the camera in x or y direction, then we could call the MoveCamera method.
                            //_mouseCameraController.MoveCamera(-dy * _movementSpeedFactor, 0);

                            // But here we need to move the camera in the z axis, so we do that manually:
                            Camera1.TargetPosition += new Vector3D(0, 0, -dy * _movementSpeedFactor);
                        }

                        _lastMousePosition = mousePosition;
                    }
                    else
                    {
                        // More complex processing (allows only rotation or only movement)

                        if (_isMouseRotation)
                        {
                            if (dx != 0)
                                _mouseCameraController.RotateCamera(-dx, 0);

                            _lastMousePosition = mousePosition;
                        }
                        else if (_isMouseMovement)
                        {
                            if (dy != 0)
                            {
                                // If we would move the camera in x or y direction, then we could call the MoveCamera method.
                                //_mouseCameraController.MoveCamera(-dy * _movementSpeedFactor, 0);

                                // But here we need to move the camera in the z axis, so we do that manually:
                                Camera1.TargetPosition += new Vector3D(0, 0, -dy * _movementSpeedFactor);
                            }

                            _lastMousePosition = mousePosition;
                        }
                        else
                        {
                            if (Math.Abs(dx) >= _mouseMoveThreshold)
                            {
                                // Show the rotation center marker and rotation cursor
                                _mouseCameraController.StartCameraRotation(mousePosition);

                                _isMouseRotation = true;
                                _lastMousePosition = mousePosition;
                            }
                            else if (Math.Abs(dy) >= _mouseMoveThreshold)
                            {
                                // Show move cursor
                                _mouseCameraController.StartCameraMovement();

                                _isMouseMovement = true;
                                _lastMousePosition = mousePosition;
                            }
                        }
                    }
                }
            }
            else
            {
                if (isCorrectMouseButtonPressed)
                {
                    _isProcessingMouse = true;
                    _lastMousePosition = e.GetPosition(ViewportBorder);
                }
            }
        }

        private void CreateTestScene()
        {
            var rnd = new Random();

            for (int i = 0; i < 19; i++)
            {
                var r = rnd.NextDouble() * 10 + 10;
                var color = Color.FromRgb((byte)rnd.Next(255), (byte)rnd.Next(255), (byte)rnd.Next(255));

                var sphereVisual3D = new SphereVisual3D()
                {
                    CenterPosition = new Point3D(0, r + 3, -i * 50 - 50),
                    Radius = r,
                    Material = new DiffuseMaterial(new SolidColorBrush(color))
                };

                MainViewport.Children.Add(sphereVisual3D);
            }
        }
    }
}
