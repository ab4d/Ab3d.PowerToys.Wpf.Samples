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
using Ab3d.Cameras;
using Ab3d.Common;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for CameraMethods.xaml
    /// </summary>
    public partial class CameraMethods : Page
    {
        private BaseCamera _selectedCamera;

        private const double _cameraMoveAmount = 10;

        public CameraMethods()
        {
            InitializeComponent();

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                UpdateSelectedCamera();
            };
        }

        private void OnCameraTypeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateSelectedCamera();
        }

        private void UpdateSelectedCamera()
        {
            if (FirstPersonCameraRadioButton.IsChecked ?? false)
                _selectedCamera = FirstPersonCamera1;
            else if (FreeCameraRadioButton.IsChecked ?? false)
                _selectedCamera = FreeCamera1;
            else // if (TargetPositionCameraRadioButton.IsChecked ?? false)
                _selectedCamera = TargetPositionCamera1;



            CameraSpecificMethodsTitleTextBlock.Text = string.Format("{0} methods:", _selectedCamera.GetType().Name);

            TargetPositionCameraMethodsPanel.Visibility = (_selectedCamera is TargetPositionCamera) ? Visibility.Visible : Visibility.Collapsed;
            FirstPersonCameraMethodsPanel.Visibility    = (_selectedCamera is FirstPersonCamera) ? Visibility.Visible : Visibility.Collapsed;
            FreeCameraMethodsPanel.Visibility           = (_selectedCamera is FreeCamera) ? Visibility.Visible : Visibility.Collapsed;

            // TargetPositionCamera and FirstPersonCamera are both derived from SphericalCamera
            SphericalCameraMethodsPanel.Visibility = (_selectedCamera is SphericalCamera) ? Visibility.Visible : Visibility.Collapsed;



            FirstPersonCamera1.TargetViewport3D = null;
            FreeCamera1.TargetViewport3D = null;
            TargetPositionCamera1.TargetViewport3D = null;

            _selectedCamera.TargetViewport3D = MainViewport;

            MouseCameraController1.TargetCamera = _selectedCamera;
            CameraAxisPanel1.TargetCamera = _selectedCamera;
        }

        private void MoveUpButton_OnClick(object sender, RoutedEventArgs e)
        {
            var movableCamera = _selectedCamera as IMovableCamera;
            if (movableCamera != null)
                movableCamera.MoveUp(_cameraMoveAmount);
        }

        private void MoveDownButton_OnClick(object sender, RoutedEventArgs e)
        {
            var movableCamera = _selectedCamera as IMovableCamera;
            if (movableCamera != null)
                movableCamera.MoveDown(_cameraMoveAmount);
        }

        private void MoveLeftButton_OnClick(object sender, RoutedEventArgs e)
        {
            var movableCamera = _selectedCamera as IMovableCamera;
            if (movableCamera != null)
                movableCamera.MoveLeft(_cameraMoveAmount);
        }

        private void MoveRightButton_OnClick(object sender, RoutedEventArgs e)
        {
            var movableCamera = _selectedCamera as IMovableCamera;
            if (movableCamera != null)
                movableCamera.MoveRight(_cameraMoveAmount);
        }

        private void MoveForwardButton_OnClick(object sender, RoutedEventArgs e)
        {
            var movableCamera = _selectedCamera as IMovableCamera;
            if (movableCamera != null)
                movableCamera.MoveForward(_cameraMoveAmount);
        }

        private void MoveBackwardButton_OnClick(object sender, RoutedEventArgs e)
        {
            var movableCamera = _selectedCamera as IMovableCamera;
            if (movableCamera != null)
                movableCamera.MoveBackward(_cameraMoveAmount);
        }


        private void RenderToBitmapButton_OnClick(object sender, RoutedEventArgs e)
        {
            var renderedBitmap = _selectedCamera.RenderToBitmap(customWidth: 400, customHeight: 300, antialiasingLevel: 4, backgroundBrush: Brushes.White, dpi: 96);

            var window = new Window()
            {
                Title = "Bitmap created with RenderToBitmap method",
                SizeToContent = SizeToContent.WidthAndHeight
            };

            var image = new Image()
            {
                Source = renderedBitmap,
                Width = renderedBitmap.Width,
                Height = renderedBitmap.Height
            };

            window.Content = image;

            window.ShowDialog();
        }

        private void CreateFromButton_OnClick(object sender, RoutedEventArgs e)
        {
            var perspectiveCamera = new PerspectiveCamera()
            {
                Position = new Point3D(50, 50, 200),
                LookDirection = new Vector3D(-50, -50, -200),
                UpDirection = new Vector3D(0, 1, 0)
            };

            _selectedCamera.CreateFrom(perspectiveCamera);
        }

        private void CreateMouseRay3DButton_OnClick(object sender, RoutedEventArgs e)
        {
            Point3D rayOrigin;
            Vector3D rayDirection;

            bool isRayPossible = _selectedCamera.CreateMouseRay3D(new Point(100, 100), out rayOrigin, out rayDirection);

            MessageBox.Show(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Calling:\r\nCreateMouseRay3D(new Point(100, 100), out rayOrigin, out rayDirection):\r\n\r\nResults:\r\nrayOrigin: {0:0.0}\r\nrayDirection: {1:0.0}",
                rayOrigin, rayDirection));

            // You can also use static CreateMouseRay3D that can calculate the ray with camera's viewportSize, viewMatrix and projectionMatrix:
            // BaseCamera.CreateMouseRay3D(Point mousePosition, Size viewportSize, ref Matrix3D viewMatrix, ref Matrix3D projectionMatrix, out Point3D rayOrigin, out Vector3D rayDirection)
        }

        private void StartRotationButton_OnClick(object sender, RoutedEventArgs e)
        {
            // To start animation with some easing use the following method:
            _selectedCamera.StartRotation(headingChangeInSecond: 45, attitudeChangeInSecond: 0, accelerationSpeed: 50, easingFunction: Ab3d.Animation.EasingFunctions.QuadraticEaseInFunction);

            // Start animation immediately without any easing:
            //_selectedCamera.StartRotation(headingChangeInSecond: 45, attitudeChangeInSecond: 0);
        }

        private void StopRotationButton_OnClick(object sender, RoutedEventArgs e)
        {
            _selectedCamera.StopRotation();

            // Stop with easing:
            //_selectedCamera.StopRotation(decelerationSpeed: 10, easingFunction: Ab3d.Animation.EasingFunctions.CubicEaseOutFunction);
        }

        private void RotateCameraButton_OnClick(object sender, RoutedEventArgs e)
        {
            _selectedCamera.RotateCamera(headingChange: 10, attitudeChange: 5);
        }

        private void Point3DTo2DButton_OnClick(object sender, RoutedEventArgs e)
        {
            Point screenPosition = _selectedCamera.Point3DTo2D(new Point3D(0, 0, 0));

            MessageBox.Show(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Calling:\r\nPoint3DTo2D(new Point3D(0, 0, 0)):\r\n\r\nResult:\r\nscreenPosition: {0:0.0}",
                screenPosition));
        }

        private void Rect3DTo2DButton_OnClick(object sender, RoutedEventArgs e)
        {
            Rect screenRect = _selectedCamera.Rect3DTo2D(new Rect3D(new Point3D(-20, -20, -20), new Size3D(40, 40, 40)));

            MessageBox.Show(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Calling:\r\nRect3DTo2D(new Rect3D(new Point3D(-20, -20, -20), new Size3D(40, 40, 40)):\r\n\r\nResult:\r\nscreenRect: {0:0.0}",
                screenRect));
        }

        private void GetCameraMatricesButton_OnClick(object sender, RoutedEventArgs e)
        {
            Matrix3D viewMatrix, projectionMatrix;
            _selectedCamera.GetCameraMatrices(out viewMatrix, out projectionMatrix);

            MessageBox.Show(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Calling:\r\nGetCameraMatrices:\r\n\r\nResult:\r\nviewMatrix:\r\n{0}\r\nprojectionMatrix:\r\n{1}",
                Ab3d.Utilities.Dumper.GetMatrix3DText(viewMatrix),
                Ab3d.Utilities.Dumper.GetMatrix3DText(projectionMatrix)));

            // There are also the following 3 static GetCameraMatrices methods available:
            //public static bool GetCameraMatrices(Viewport3D viewport3D, out Matrix3D viewMatrix, out Matrix3D projectionMatrix)
            //public static bool GetCameraMatrices(Camera camera, double viewportAspectRatio, out Matrix3D viewMatrix, out Matrix3D projectionMatrix)
            //public static bool GetCameraMatrices(BaseCamera camera, double viewportAspectRatio, out Matrix3D viewMatrix, out Matrix3D projectionMatrix)
        }

        private void GetCameraPlaneOrientationButton_OnClick(object sender, RoutedEventArgs e)
        {
            Vector3D planeNormalVector3D, widthVector3D, heightVector3D;
            _selectedCamera.GetCameraPlaneOrientation(out planeNormalVector3D, out widthVector3D, out heightVector3D);

            MessageBox.Show(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Calling:\r\nGetCameraPlaneOrientation:\r\n\r\nResult:\r\nplaneNormalVector3D: {0:0.0}\r\nwidthVector3D: {1:0.0}\r\nheightVector3D: {2:0.0}",
                planeNormalVector3D,
                widthVector3D,
                heightVector3D));
        }

        private void GetMousePositionOnPlaneButton_OnClick(object sender, RoutedEventArgs e)
        {
            Point3D intersectionPoint;
            bool hasIntersection = _selectedCamera.GetMousePositionOnPlane(new Point(100, 100), new Point3D(0, 0, 0), new Vector3D(0, 1, 0), out intersectionPoint);

            MessageBox.Show(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Calling:\r\nGetMousePositionOnPlane(new Point(100, 100), new Point3D(0, 0, 0), new Vector3D(0, 1, 0)):\r\n\r\nResult:\r\nintersectionPoint: {0:0.0}",
                intersectionPoint));
        }

        private void GetWorldToViewportMatrixButton_OnClick(object sender, RoutedEventArgs e)
        {
            Matrix3D worldToViewportMatrix = Matrix3D.Identity;
            bool success = _selectedCamera.GetWorldToViewportMatrix(ref worldToViewportMatrix, forceMatrixRefresh: true);

            MessageBox.Show(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Calling:\r\nGetWorldToViewportMatrix():\r\n\r\nResult:\r\nworldToViewportMatrix:\r\n{0}",
                Ab3d.Utilities.Dumper.GetMatrix3DText(worldToViewportMatrix)));
        }

        private void GetCameraPositionButton_OnClick(object sender, RoutedEventArgs e)
        {
            var cameraPosition = _selectedCamera.GetCameraPosition();

            MessageBox.Show(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Calling:\r\nGetCameraPosition():\r\n\r\nResult:\r\ncameraPosition: {0:0.0}",
                cameraPosition));
        }


        private void GetNormalizedAnglesButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Note that TargetPositionCamera and FirstPersonCamera are both derived from SphericalCamera

            var sphericalCamera = _selectedCamera as SphericalCamera;
            if (sphericalCamera == null)
                return;

            var infoText = string.Format(
@"GetNormalized... methods return camera angles that are normalized to values between 0 and 360.
(or -180 to 180 when called with normalizeTo180Degrees parameter set to true).

For example:

Camera.Heading: {0:0}
GetNormalizedHeading(): {1:0}
GetNormalizedHeading(normalizeTo180Degrees: true): {2:0}

Camera.Attitude: {3:0}
GetNormalizedAttitude(): {4:0}
GetNormalizedAttitude(normalizeTo180Degrees: true): {5:0}

Camera.Bank: {6:0}
GetNormalizedBank(): {7:0}
GetNormalizedBank(normalizeTo180Degrees: true): {8:0}

If the normalized values are the same, then rotate the camera a few times to increase the values and then try again.",
                sphericalCamera.Heading, sphericalCamera.GetNormalizedHeading(), sphericalCamera.GetNormalizedHeading(normalizeTo180Degrees: true),
                sphericalCamera.Attitude, sphericalCamera.GetNormalizedAttitude(), sphericalCamera.GetNormalizedAttitude(normalizeTo180Degrees: true),
                sphericalCamera.Bank, sphericalCamera.GetNormalizedBank(), sphericalCamera.GetNormalizedBank(normalizeTo180Degrees: true));

            MessageBox.Show(infoText);
        }

        private void NormalizeAnglesButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Note that TargetPositionCamera and FirstPersonCamera are both derived from SphericalCamera

            var sphericalCamera = _selectedCamera as SphericalCamera;
            if (sphericalCamera == null)
                return;

            double oldHeading = sphericalCamera.Heading;
            double oldAttitude = sphericalCamera.Attitude;
            double oldBank = sphericalCamera.Bank;

            sphericalCamera.NormalizeAngles(normalizeTo180Degrees: false); // normalize between 0 and 360

            var infoText = string.Format(
                @"NormalizeAngles normalizes camera angles to values between 0 and 360 (or -180 to 180)

Angles before calling NormalizeAngles:
Camera.Heading: {0:0}
Camera.Attitude: {1:0}
Camera.Bank: {2:0};

Angles after calling NormalizeAngles:
Camera.Heading: {3:0}
Camera.Attitude: {4:0}
Camera.Bank: {5:0}",
                oldHeading, oldAttitude, oldBank,
                sphericalCamera.Heading, sphericalCamera.Attitude, sphericalCamera.Bank);

            MessageBox.Show(infoText);
        }

        private void RotateForButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Note that TargetPositionCamera and FirstPersonCamera are both derived from SphericalCamera

            var sphericalCamera = _selectedCamera as SphericalCamera;
            if (sphericalCamera == null)
                return;

            double changedHeading, changedAttitude;

            if (sphericalCamera is FirstPersonCamera)
            {
                // Do only a small change for FirstPersonCamera (to prevent loosing the teapot object)
                changedHeading = 10;
                changedAttitude = -5;
            }
            else
            {
                // For TargetPositionCamera we can make bigger change
                changedHeading = -90;
                changedAttitude = -20;
            }

            // RotateFor method rotates the camera for the specified amount.
            // The method also support animating the rotation.
            sphericalCamera.RotateFor(changedHeading, 
                                      changedAttitude, 
                                      animationDurationInMilliseconds: 1500, 
                                      easingFunction: Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);

            // To make the rotation immediate, we can skip the animation parameters:
            //sphericalCamera.RotateFor(30, 0);
        }

        private void RotateToButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Note that TargetPositionCamera and FirstPersonCamera are both derived from SphericalCamera

            var sphericalCamera = _selectedCamera as SphericalCamera;
            if (sphericalCamera == null)
                return;


            double targetHeading, targetAttitude;

            if (sphericalCamera is FirstPersonCamera)
            {
                // Do only a small change for FirstPersonCamera (to prevent loosing the teapot object)
                targetHeading = 25;
                targetAttitude = -15;
            }
            else
            {
                // For TargetPositionCamera we can make bigger change
                targetHeading = -30;
                targetAttitude = 20;
            }

            // RotateTo method rotates the camera to the targetHeading and targetAttitude.
            // The method also support animating the rotation.
            sphericalCamera.RotateTo(targetHeading,
                                     targetAttitude,
                                     animationDurationInMilliseconds: 1500,
                                     easingFunction: Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);

            // To make the rotation immediate, we can skip the animation parameters:
            //sphericalCamera.RotateTo(-30, 20);
        }


        private void TurnToButton_OnClick(object sender, RoutedEventArgs e)
        {
            var firstPersonCamera = _selectedCamera as FirstPersonCamera;
            if (firstPersonCamera == null)
                return;

            // TurnTo method turns the FirstPersonCamera so that it looks at the specified position.
            // It is also possible to animate the turning.
            firstPersonCamera.TurnTo(position: new Point3D(30, 30, 0), 
                                     animationDurationInMilliseconds: 1500, 
                                     easingFunction: Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);

            // To make the turn immediately, we can skip the animation parameters:
            //firstPersonCamera.TurnTo(new Point3D(30, 30, 0));

            // We can also call TurnTo with specifying direction instead of position:
            //firstPersonCamera.TurnTo(new Vector3D(-0.2, -0.2, -0.6));
        }


        private void FitIntoView_OnClick(object sender, RoutedEventArgs e)
        {
            var targetPositionCamera = _selectedCamera as TargetPositionCamera;
            if (targetPositionCamera == null)
                return;

            // First set TargetPosition
            targetPositionCamera.FitIntoView(FitIntoViewType.CheckAllPositions, // use more precise calculation with using all model positions instead of using only object bounds
                                             adjustTargetPosition: true,        // the method should change the TargetPosition to get better fit into view
                                             adjustmentFactor: 1.1);            // 10% margin around the object to the border

            // When you want to check only some 3D models and fit them into view, 
            // you can call FitIntoView with an IList of Visual3D objects as the first parameter.

            // You can also call GetFitIntoViewDistanceOrCameraWidth method to get the required distance or CameraWidth
            // that would fit the scene into view. This method does not change the camera.
            //targetPositionCamera.GetFitIntoViewDistanceOrCameraWidth(...)
        }


        private void SetCameraPositionButton_OnClick(object sender, RoutedEventArgs e)
        {
            var freeCamera = _selectedCamera as FreeCamera;
            if (freeCamera == null)
                return;

            // First set TargetPosition
            freeCamera.TargetPosition = new Point3D(0, 0, 0);

            // SetCameraPosition can be used to preserves the current TargetPosition and set the CameraPosition and UpDirection
            // based on the specified heading, attitude, bank and distance values.
            freeCamera.SetCameraPosition(heading: 30, attitude: -20, bank: 0, distance: 150);
        }

        private void SetTargetPositionButton_OnClick(object sender, RoutedEventArgs e)
        {
            var freeCamera = _selectedCamera as FreeCamera;
            if (freeCamera == null)
                return;

            freeCamera.CameraPosition = new Point3D(-50, 50, 120);

            // SetTargetPosition can be used to preserves the current CameraPosition and set the TargetPosition and UpDirection
            // based on the specified heading, attitude, bank and distance values.
            freeCamera.SetTargetPosition(heading: 30, attitude: -20, bank: 0, distance: 150);
        }
    }
}
