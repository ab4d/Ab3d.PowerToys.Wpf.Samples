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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ab3d.PowerToys.Samples.MouseCameraControllerSamples
{
    /// <summary>
    /// Interaction logic for HorizontalCameraMovement.xaml
    /// </summary>
    public partial class HorizontalCameraMovement : Page
    {
        public HorizontalCameraMovement()
        {
            InitializeComponent();

            UpdateCamera1RotationCenter();
            UpdateCamera2RotationCenter();
        }

        private void OnCamera1MoveEnded(object sender, EventArgs e)
        {
            UpdateCamera1RotationCenter();
        }

        private void OnCamera2MoveEnded(object sender, EventArgs e)
        {
            UpdateCamera2RotationCenter();
        }

        private void UpdateCamera1RotationCenter()
        {
            // Move the ColoredAxisVisual3D so that it will show the current center of camera rotation
            AxisTranslation1.OffsetX = Camera1.TargetPosition.X + Camera1.Offset.X;
            AxisTranslation1.OffsetY = Camera1.TargetPosition.Y + Camera1.Offset.Y;
            AxisTranslation1.OffsetZ = Camera1.TargetPosition.Z + Camera1.Offset.Z;
        }

        private void UpdateCamera2RotationCenter()
        {
            // Move the ColoredAxisVisual3D so that it will show the current center of camera rotation
            AxisTranslation2.OffsetX = Camera2.TargetPosition.X + Camera2.Offset.X;
            AxisTranslation2.OffsetY = Camera2.TargetPosition.Y + Camera2.Offset.Y;
            AxisTranslation2.OffsetZ = Camera2.TargetPosition.Z + Camera2.Offset.Z;
        }
    }
}
