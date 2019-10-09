using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab3d.Cameras;
using Ab3d.Common;
using Ab3d.Utilities;
using _3Dconnexion;
using Ab3d.PowerToys.Samples.Cameras;

namespace Ab3d.PowerToys.Samples.Input
{
    // NOTE: 
    // To use 3D mouse support, you need to add reference to Ab3d.PowerToys.Input assembly.
    //
    // You also need to accept 3Dconnexion SDK license (available here: http://www.3dconnexion.eu/service/software-developer/licence-agreement.html)

    /// <summary>
    /// Interaction logic for Mouse3DSample.xaml
    /// </summary>
    public partial class Mouse3DSample : Page
    {
        private Mouse3DConnector _mouse3DConnector;
        private BaseCamera _selectedCamera;

        public Mouse3DSample()
        {
            InitializeComponent();

            this.Loaded += delegate (object sender, RoutedEventArgs args)
            {
                // First check if 3D mouse is installed and available on this operating system.
                if (Mouse3DConnector.Is3DMouseInstalled())
                {
                    // If available, then initialize 3D mouse
                    Window parentWindow = Window.GetWindow(this);

                    bool isMouse3DConnected = InitializeMouse3D(parentWindow);

                    if (isMouse3DConnected) // is already connected?
                        UpdateIsMouseConnectedText();
                }
                else
                {
                    ConnectedTextBlock.Visibility = Visibility.Collapsed;

                    NotConnectedTextBlock.Text = "3D mouse not available!"; // not available on the OS
                    NotConnectedTextBlock.Visibility = Visibility.Visible;
                }


                // Update used camera based on selected RadioButton
                UpdateCurrentCamera();
            };

            this.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                if (_mouse3DConnector != null)
                {
                    _mouse3DConnector.Disconnect(); // Clean up the native resources and unhook from window message pump
                    _mouse3DConnector = null;
                }
            };
        }

        private bool InitializeMouse3D(Window parentWindow)
        {
            if (!Mouse3DConnector.Is3DMouseInstalled())
                return false;


            _mouse3DConnector = new Mouse3DConnector();

            _mouse3DConnector.IsXMovementEnabled = IsXMovementEnabledCheckBox.IsChecked ?? false;
            _mouse3DConnector.IsYMovementEnabled = IsYMovementEnabledCheckBox.IsChecked ?? false;
            _mouse3DConnector.IsZMovementEnabled = IsZMovementEnabledCheckBox.IsChecked ?? false;

            _mouse3DConnector.IsAttitudeRotationEnabled = IsXRotationEnabledCheckBox.IsChecked ?? false;
            _mouse3DConnector.IsHeadingRotationEnabled  = IsYRotationEnabledCheckBox.IsChecked ?? false;
            _mouse3DConnector.IsBankRotationEnabled     = IsZRotationEnabledCheckBox.IsChecked ?? false;


            _mouse3DConnector.IsConnectedChanged += delegate(object sender, EventArgs args)
            {
                UpdateIsMouseConnectedText();
            };

            _mouse3DConnector.LogAction = delegate(string message)
            {
                if (LogCheckBox.IsChecked ?? false)
                    System.Diagnostics.Debug.WriteLine("Mouse3DConnector log: " + message);
            };

            // We will also subscribe to events in case user presses a button on the 3D mouse
            _mouse3DConnector.ButtonPressed += Mouse3DConnectorOnButtonPressed;

            // Initialize the connection. We need Handle to main window for this
            IntPtr mainWindowPtr = new WindowInteropHelper(parentWindow).Handle;
            HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);

            var isConnected = _mouse3DConnector.Connect(applicationName: "Ab3d.PowerToys.Samples", hWndSource: mainWindowSrc);

            return isConnected;
        }

        // Usually ConnectMouse3D is called from code that is defined in main window's code (for example in MainWindow.xaml.cs).
        // There it is possible to override the OnSourceInitialized method (this method is not available in Page class).
        // The following commented methods shows a sample OnSourceInitialized.
        //protected override void OnSourceInitialized(EventArgs e)
        //{
        //    base.OnSourceInitialized(e);

        //    InitializeMouse3D(this);
        //}


        private void Mouse3DConnectorOnButtonPressed(Mouse3DConnector sender, Mouse3DButtonEventArgs e)
        {
            double newHeading = double.NaN;
            double newAttitude = double.NaN;
            Vector3D newLookDirection = new Vector3D();
            Vector3D newUpDirection = new Vector3D();


            // Button 
            switch (e.FunctionNumber)
            {
                case (int)SiApp.V3DCMD.V3DCMD_VIEW_TOP:
                    newHeading = 0;
                    newAttitude = -90;
                    newLookDirection = new Vector3D(0, -1, 0);
                    newUpDirection = new Vector3D(0, 0, -1);
                    break;

                case (int)SiApp.V3DCMD.V3DCMD_VIEW_LEFT:
                    newHeading = 90;
                    newAttitude = 0;
                    newLookDirection = new Vector3D(1, 0, 0);
                    newUpDirection = new Vector3D(0, 1, 0);
                    break;

                case (int)SiApp.V3DCMD.V3DCMD_VIEW_RIGHT:
                    newHeading = -90;
                    newAttitude = 0;
                    newLookDirection = new Vector3D(-1, 0, 0);
                    newUpDirection = new Vector3D(0, 1, 0);
                    break;

                case (int)SiApp.V3DCMD.V3DCMD_VIEW_FRONT:
                    newHeading = 0;
                    newAttitude = 0;
                    newLookDirection = new Vector3D(0, 0, -1);
                    newUpDirection = new Vector3D(0, 1, 0);
                    break;

                case (int)SiApp.V3DCMD.V3DCMD_VIEW_BOTTOM:
                    newHeading = 0;
                    newAttitude = 90;
                    newLookDirection = new Vector3D(0, 1, 0);
                    newUpDirection = new Vector3D(0, 0, 1);
                    break;

                case (int)SiApp.V3DCMD.V3DCMD_VIEW_BACK:
                    newHeading = 180;
                    newAttitude = 0;
                    newLookDirection = new Vector3D(0, 0, 1);
                    newUpDirection = new Vector3D(0, 1, 0);
                    break;

                case (int)SiApp.V3DCMD.V3DCMD_SAVE_VIEW_1:
                    newHeading = 30;
                    newAttitude = -15;
                    newLookDirection = new Vector3D(0.482962913144534, -0.258819045102521, -0.836516303737808);
                    newUpDirection = new Vector3D(0.12940952255126, 0.965925826289068, -0.224143868042013);
                    break;

                case (int)SiApp.V3DCMD.V3DCMD_SAVE_VIEW_2:
                    newHeading = -30;
                    newAttitude = -15;
                    newLookDirection = new Vector3D(-0.482962913144534, -0.258819045102521, -0.836516303737808);
                    newUpDirection = new Vector3D(-0.12940952255126, 0.965925826289068, -0.224143868042013);
                    break;

                case (int)SiApp.V3DCMD.V3DCMD_VIEW_FIT:
                case (int)SiApp.V3DCMD.V3DCMD_FILTER_DOMINANT: // Fix for wrongly send FIT command
                    var baseTargetPositionCamera = _selectedCamera as BaseTargetPositionCamera;

                    if (baseTargetPositionCamera != null)
                        baseTargetPositionCamera.FitIntoView(FitIntoViewType.CheckBounds);
                    break;
            }

            if (!double.IsNaN(newHeading) && !double.IsNaN(newAttitude))
            {
                var sphericalCamera = _selectedCamera as Ab3d.Cameras.SphericalCamera;
                if (sphericalCamera != null)
                {
                    sphericalCamera.BeginInit();
                    sphericalCamera.Heading = newHeading;
                    sphericalCamera.Attitude = newAttitude;
                    sphericalCamera.EndInit();
                }
                else
                {
                    var freeCamera = _selectedCamera as Ab3d.Cameras.FreeCamera;
                    if (freeCamera != null)
                    {
                        // Update CameraPosition based on newLookDirection; preserve the current distance between CameraPosition and TargetPosition
                        double distance = (freeCamera.TargetPosition - freeCamera.CameraPosition).Length;
                        freeCamera.CameraPosition = freeCamera.TargetPosition - newLookDirection * distance;
                        freeCamera.UpDirection = newUpDirection;
                    }
                }
            }
        }

        private void UpdateIsMouseConnectedText()
        {
            // Call GetDeviceInfo to manually update the IsConnected value
            _mouse3DConnector.GetDeviceInfo();

            if (_mouse3DConnector.IsConnected)
            {
                var deviceName = _mouse3DConnector.GetDeviceName();

                ConnectedTextBlock.Text = deviceName + " connected";
                ConnectedTextBlock.Visibility = Visibility.Visible;
                NotConnectedTextBlock.Visibility = Visibility.Collapsed;

                UpdateCurrentCamera();
            }
            else
            {
                ConnectedTextBlock.Visibility = Visibility.Collapsed;

                NotConnectedTextBlock.Text = "3D mouse not connected!";
                NotConnectedTextBlock.Visibility = Visibility.Visible;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // 3D Mouse can also trigger keyboard events.
            // You can get them in this method.

            base.OnKeyDown(e);
        }

        private void UpdateCurrentCamera()
        {
            TargetPositionCamera1.TargetViewport3D = null;
            FirstPersonCamera1.TargetViewport3D = null;
            FreeCamera1.TargetViewport3D = null;

            if (TargetPositionCameraRadioButton.IsChecked ?? false)
            {
                _selectedCamera = TargetPositionCamera1;
            }
            else if (FreeCameraRadioButton.IsChecked ?? false)
            {
                _selectedCamera = FreeCamera1;
            }
            else if (FirstPersonCameraRadioButton.IsChecked ?? false)
            {
                _selectedCamera = FirstPersonCamera1;
            }
            else
            {
                _selectedCamera = null;
            }

            if (_selectedCamera != null)
            {
                //_selectedCamera.FieldOfView = FieldOfViewSlider.Value;

                _selectedCamera.TargetViewport3D = MainViewport;
                MouseCameraController1.TargetCamera = _selectedCamera;
                CameraAxisPanel1.TargetCamera = _selectedCamera;

                if (_mouse3DConnector != null)
                    _mouse3DConnector.Camera = _selectedCamera;
            }
        }

        private void ResetCameraButton_OnClick(object sender, RoutedEventArgs e)
        {
            ResetCamera();
        }

        private void ResetCamera()
        {
            var baseTargetPositionCamera = _selectedCamera as BaseTargetPositionCamera;

            if (baseTargetPositionCamera != null)
            {
                baseTargetPositionCamera.FitIntoView(FitIntoViewType.CheckBounds);

                if (baseTargetPositionCamera.CameraType == BaseCamera.CameraTypes.PerspectiveCamera)
                    baseTargetPositionCamera.Distance *= 3;
                else
                    baseTargetPositionCamera.CameraWidth *= 3;
            }
            else
            {
                var firstPersonCamera = _selectedCamera as FirstPersonCamera;
                if (firstPersonCamera != null)
                {
                    firstPersonCamera.Heading = 0;
                    firstPersonCamera.Attitude = -15;
                    firstPersonCamera.Position = new Point3D(0, 250, 1000);
                    firstPersonCamera.Offset = new System.Windows.Media.Media3D.Vector3D(0, 0, 0);
                }
                else
                {
                    var freeCamera = _selectedCamera as FreeCamera;
                    if (freeCamera != null)
                    {
                        freeCamera.TargetPosition = new Point3D(0, 0, 0);
                        freeCamera.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
                        freeCamera.CameraPosition = new Point3D(0, 250, 1000);
                        freeCamera.Offset = new System.Windows.Media.Media3D.Vector3D(0, 0, 0);
                    }
                }
            }
        }

        private void OperatingModeRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_mouse3DConnector == null)
                return;

            _mouse3DConnector.OperatingMode = (ObjectModeRadioButton.IsChecked ?? false) ? Mouse3DConnector.Mouse3DOperatingMode.ObjectMode : Mouse3DConnector.Mouse3DOperatingMode.CameraMode;
        }

        private void CameraTypeRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_mouse3DConnector == null)
                return;

            UpdateCurrentCamera();
            ResetCamera();
        }

        private void RotationSensitivitySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mouse3DConnector == null)
                return;

            _mouse3DConnector.RotationSensitivity = RotationSensitivitySlider.Value;
        }

        private void MovementSensitivitySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mouse3DConnector == null)
                return;

            _mouse3DConnector.MovementSensitivity = MovementSensitivitySlider.Value;
        }

        private void ZoomingSensitivitySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mouse3DConnector == null)
                return;

            _mouse3DConnector.ZoomingSensitivity = ZoomingSensitivitySlider.Value;
        }

        private void RecheckIfConnectedButton_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateIsMouseConnectedText();
        }

        //private void FieldOfViewSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    if (_selectedCamera != null)
        //        _selectedCamera.FieldOfView = FieldOfViewSlider.Value;
        //}

        private void OnOrthographicCameraCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var newCameraType = (OrthographicCameraCheckBox.IsChecked ?? false) ? BaseCamera.CameraTypes.OrthographicCamera :
                                                                                  BaseCamera.CameraTypes.PerspectiveCamera;

            TargetPositionCamera1.CameraType = newCameraType;
            FirstPersonCamera1.CameraType = newCameraType;
            FreeCamera1.CameraType = newCameraType;
        }

        private void OnMovementEnabledCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_mouse3DConnector == null)
                return;

            _mouse3DConnector.IsXMovementEnabled = IsXMovementEnabledCheckBox.IsChecked ?? false;
            _mouse3DConnector.IsYMovementEnabled = IsYMovementEnabledCheckBox.IsChecked ?? false;
            _mouse3DConnector.IsZMovementEnabled = IsZMovementEnabledCheckBox.IsChecked ?? false;

            MouseCameraController1.IsXMovementEnabled = _mouse3DConnector.IsXMovementEnabled;
            MouseCameraController1.IsYMovementEnabled = _mouse3DConnector.IsYMovementEnabled;
        }

        private void OnRotationEnabledCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_mouse3DConnector == null)
                return;

            _mouse3DConnector.IsAttitudeRotationEnabled = IsXRotationEnabledCheckBox.IsChecked ?? false;
            _mouse3DConnector.IsHeadingRotationEnabled  = IsYRotationEnabledCheckBox.IsChecked ?? false;
            _mouse3DConnector.IsBankRotationEnabled     = IsZRotationEnabledCheckBox.IsChecked ?? false;

            MouseCameraController1.IsHeadingRotationEnabled  = _mouse3DConnector.IsHeadingRotationEnabled;
            MouseCameraController1.IsAttitudeRotationEnabled = _mouse3DConnector.IsAttitudeRotationEnabled;
        }
    }
}
