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

namespace Ab3d.PowerToys.Samples.MouseCameraControllerSamples
{
    /// <summary>
    /// Interaction logic for RotateAroundCustomPoint.xaml
    /// </summary>
    public partial class RotateAroundCustomPoint : Page
    {
        private bool _isButtonDownSubscribed;

        public RotateAroundCustomPoint()
        {
            InitializeComponent();
        }

        private void RotationCenterPositionRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            Point3D? rotationCenterPosition;
            bool rotateAroundMousePosition = false;

            UnsubscribeButtonDown();


            if (NoCenterRadioButton.IsChecked ?? false)
            {
                rotationCenterPosition = null; // RotationCenterPosition is nullable Point3D type
            }
            else if (RedBoxRadioButton.IsChecked ?? false)
            {
                rotationCenterPosition = RedBox.CenterPosition;
            }
            else if (YellowBoxRadioButton.IsChecked ?? false)
            {
                rotationCenterPosition = YellowBox.CenterPosition;
            }
            else if (OrangeBoxRadioButton.IsChecked ?? false)
            {
                rotationCenterPosition = OrangeBox.CenterPosition;
            }
            else if (MousePositionAutoBoxRadioButton.IsChecked ?? false)
            {
                rotationCenterPosition = null;
                rotateAroundMousePosition = true;
            }
            else if (MousePositionManualBoxRadioButton.IsChecked ?? false)
            {
                rotationCenterPosition = null;
                SubscribeButtonDown();
            }
            else
            {
                rotationCenterPosition = null;
            }

            Camera1.RotationCenterPosition = rotationCenterPosition;
            MouseCameraController1.RotateAroundMousePosition = rotateAroundMousePosition;
        }

        private void SubscribeButtonDown()
        {
            if (_isButtonDownSubscribed)
                return;

            ViewportBorder.PreviewMouseDown += ViewportBorderOnMouseUp;
            _isButtonDownSubscribed = true;
        }

        private void UnsubscribeButtonDown()
        {
            if (!_isButtonDownSubscribed)
                return;

            ViewportBorder.PreviewMouseDown -= ViewportBorderOnMouseUp;
            _isButtonDownSubscribed = false;
        }

        private void ViewportBorderOnMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var mousePosition = mouseButtonEventArgs.GetPosition(ViewportBorder);

            var hitTestResult = VisualTreeHelper.HitTest(MainViewport, mousePosition) as RayMeshGeometry3DHitTestResult;

            if (hitTestResult != null)
                Camera1.RotationCenterPosition = hitTestResult.PointHit;
            else
                Camera1.RotationCenterPosition = null; // Rotate around the center
        }
    }
}
