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
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for CustomUpAxisWithGlobalTransform.xaml
    /// </summary>
    public partial class CustomUpAxisWithGlobalTransform : Page
    {
        public CustomUpAxisWithGlobalTransform()
        {
            InitializeComponent();

            // See XAML for more comments

            // Set the axes so they show left handed coordinate system such Autocad (with z up)
            // Note: WPF uses right handed coordinate system (such as OpenGL)
            //       DirectX also uses left handed coordinate system with y up
            CustomCameraAxisPanel1.CustomizeAxes(new Vector3D(1, 0, 0), "X", Colors.Red,
                                                 new Vector3D(0, 1, 0), "Z", Colors.Blue,
                                                 new Vector3D(0, 0, -1), "Y", Colors.Green);
        }
    }
}
