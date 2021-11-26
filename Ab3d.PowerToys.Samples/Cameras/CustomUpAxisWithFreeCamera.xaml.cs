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
using Ab3d.Controls;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for CustomUpAxisWithFreeCamera.xaml
    /// </summary>
    public partial class CustomUpAxisWithFreeCamera : Page
    {
        public CustomUpAxisWithFreeCamera()
        {
            InitializeComponent();

            // Because WPF uses y as up axis, but this sample uses Z as up axis,
            // we need to transform the axis that is used to generate the ViewCubeCameraController.
            // 
            // This is not needed when using a global transform as seen in the "CustomUpAxisWithGlobalTransform.xaml" sample.
            // 
            // The transformation defines the new axis - defined in matrix columns in upper left 3x3 part of the matrix:
            // x axis -1st column: 1  0  0(in the positive x direction - same as WPF 3D)
            // y axis -2nd column: 0  0 - 1(in the negative z direction - into the screen)
            // z axis -3rd column: 0  1  0(in the positive y direction - up)

            ViewCube1.AxisTransform = new MatrixTransform3D(new Matrix3D(1, 0, 0, 0,
                                                                         0, 0, 1, 0,
                                                                         0, -1, 0, 0,
                                                                         0, 0, 0, 1));
        }
    }
}
