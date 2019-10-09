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
using Ab3d.Common.Cameras;

namespace Ab3d.PowerToys.Samples.Text3D
{
    /// <summary>
    /// Interaction logic for AdjustingTextDirectionSample.xaml
    /// </summary>
    public partial class AdjustingTextDirectionSample : Page
    {
        public AdjustingTextDirectionSample()
        {
            InitializeComponent();

            Camera1.StartRotation(45, 0);

            Camera1.CameraChanged += Camera1OnCameraChanged;
        }


        private void Camera1OnCameraChanged(object o, CameraChangedRoutedEventArgs cameraChangedRoutedEventArgs)
        {
            // To check if we need to flip TextDirection,
            // we first calculate the 3D vector that points into the direction from which the text is correctly seen.
            // This can be get with calculating cross product from TextDirection and UpDirection - getting perpendicular vector to those two vectors.
            Vector3D textFaceVector = Vector3D.CrossProduct(CenteredTextVisual2.TextDirection, CenteredTextVisual2.UpDirection);

            // Now calculate dot product from textFaceVector and camera's LookDirection
            // If the result is negative, then those two vectors are facing in opposite direction.
            // This is a desired result - text is facing towards camera and camera towards text.
            // But if the result is positive, then text is seen correctly from the other side.
            // In this case we flip the TextDirection by multiplying it with -1.
            // In our case this flips between initial (1, 0, 0) and (-1, 0, 0) vectors.
            if (Vector3D.DotProduct(textFaceVector, Camera1.LookDirection) > 0)
                CenteredTextVisual2.TextDirection = CenteredTextVisual2.TextDirection * (-1);
        }
    }
}
