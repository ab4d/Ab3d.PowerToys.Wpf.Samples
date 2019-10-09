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

namespace Ab3d.PowerToys.Samples.MouseCameraControllerSamples
{
    /// <summary>
    /// Interaction logic for RotationDirectionSample.xaml
    /// </summary>
    public partial class RotationDirectionSample : Page
    {
        public RotationDirectionSample()
        {
            InitializeComponent();
        }

        private void OnCameraRotateStarted(object sender, EventArgs e)
        {
            var mousePosition = Mouse.GetPosition(ViewportBorder);

            Point3D rotatedPosition; // Hit position or position on horizontal plane

            var hitTestResult = VisualTreeHelper.HitTest(MainViewport, mousePosition) as RayMeshGeometry3DHitTestResult;

            if (hitTestResult != null)
            {
                rotatedPosition = hitTestResult.PointHit;
            }
            else
            {
                // Get intersection of ray created from mouse position and the horizontal plane (position: 0,0,0; normal: 0,1,0)
                bool hasIntersection = Camera1.GetMousePositionOnPlane(mousePosition, Constants.ZeroPoint3D, Constants.YAxis, out rotatedPosition);

                if (!hasIntersection)
                    return;
            }

            PositionCross.Position = rotatedPosition;
            PositionCross.IsVisible = true;

            ShowCameraRotationHelpers();
        }

        private void OnCameraRotateEnded(object sender, EventArgs e)
        {
            HideCameraRotationHelpers();
        }

        private void OnCameraMoveStarted(object sender, EventArgs e)
        {
            ShowCameraRotationHelpers();
        }

        private void OnCameraMoveEnded(object sender, EventArgs e)
        {
            // Move the ColoredAxisVisual3D so that it will show the current center of camera rotation
            AxisTranslation.OffsetX = Camera1.TargetPosition.X + Camera1.Offset.X;
            AxisTranslation.OffsetY = Camera1.TargetPosition.Y + Camera1.Offset.Y;
            AxisTranslation.OffsetZ = Camera1.TargetPosition.Z + Camera1.Offset.Z;

            HideCameraRotationHelpers();
        }


        private void ShowCameraRotationHelpers()
        {
            YAxisLine.IsVisible = true;
            CameraRotationCenterVisual.IsVisible = true;
        }

        private void HideCameraRotationHelpers()
        {
            PositionCross.IsVisible = false;

            if (!(ShowCenterOfRotationCheckBox.IsChecked ?? false))
            {
                YAxisLine.IsVisible = false;
                CameraRotationCenterVisual.IsVisible = false;
            }
        }

        private void ShowCenterOfRotationCheckBox_OnCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (ShowCenterOfRotationCheckBox.IsChecked ?? false)
                ShowCameraRotationHelpers();
            else
                HideCameraRotationHelpers();
        }
    }
}
