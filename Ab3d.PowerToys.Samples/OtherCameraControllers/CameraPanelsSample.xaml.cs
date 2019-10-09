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

namespace Ab3d.PowerToys.Samples.OtherCameraControllers
{
    /// <summary>
    /// Interaction logic for CameraPanelsSample.xaml
    /// </summary>
    public partial class CameraPanelsSample : Page
    {
        GeometryModel3D _defaultCenterObject;
        Material _defaultCenterObjectMateral;

        public CameraPanelsSample()
        {
            InitializeComponent();
             
            // Set the axes so they show left handed coordinate system such Autocad (with z up)
            // Note: WPF uses right handed coordinate system (such as OpenGL)
            //       DirectX also uses left handed coordinate system with y up
            CustomCameraAxisPanel1.CustomizeAxes(new Vector3D(1, 0, 0),  "X", Colors.Red,
                                                 new Vector3D(0, 1, 0),  "Z", Colors.Green,
                                                 new Vector3D(0, 0, -1), "Y", Colors.Blue);

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            // Save default center object so it can be later changed and set back
            _defaultCenterObject = CameraPreviewPanel1.CenterObjectModel3D as GeometryModel3D;

            if (_defaultCenterObject != null)
                _defaultCenterObjectMateral = _defaultCenterObject.Material;
        }

        private void ShowColoredAxisCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (ShowColoredAxisCheckBox.IsChecked ?? false)
            {
                // Set back the default colors
                // Note the similarity: RGB = XYZ (Red = X, Green = Y, Blue = Z)
                CameraAxisPanel1.XAxisColor = Colors.Red;
                CameraAxisPanel1.YAxisColor = Colors.Green;
                CameraAxisPanel1.ZAxisColor = Colors.Blue;

                CustomCameraAxisPanel1.XAxisColor = Colors.Red;
                CustomCameraAxisPanel1.YAxisColor = Colors.Green;
                CustomCameraAxisPanel1.ZAxisColor = Colors.Blue;
            }
            else
            {
                Color singleColor;

                if (CameraAxisPanel1.Is3DAxesShown)
                    singleColor = Colors.Silver;
                else
                    singleColor = Colors.Black;

                CameraAxisPanel1.XAxisColor = singleColor;
                CameraAxisPanel1.YAxisColor = singleColor;
                CameraAxisPanel1.ZAxisColor = singleColor;

                CustomCameraAxisPanel1.XAxisColor = singleColor;
                CustomCameraAxisPanel1.YAxisColor = singleColor;
                CustomCameraAxisPanel1.ZAxisColor = singleColor;
            }
        }

        private void CenterObject_Default_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!CenterObject_Default_RadioButton.IsInitialized)
                return;

            _defaultCenterObject.Material = _defaultCenterObjectMateral;
            CameraPreviewPanel1.CenterObjectModel3D = _defaultCenterObject;
        }

        private void CenterObject_Blue_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!CenterObject_Blue_RadioButton.IsInitialized)
                return;

            _defaultCenterObject.Material = new DiffuseMaterial(Brushes.Blue);
            CameraPreviewPanel1.CenterObjectModel3D = _defaultCenterObject;
        }

        private void CenterObject_Box_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!CenterObject_Box_RadioButton.IsInitialized)
                return;

            GeometryModel3D newCenterObject;

            // Copy a box form our shown object and use it as a new CameraPreviewPanel center object
            newCenterObject = ((System.Windows.Media.Media3D.Model3DGroup)(((System.Windows.Media.Media3D.ModelVisual3D)(SceneCameraViewport.Children[0])).Content)).Children[4] as GeometryModel3D;
            newCenterObject = newCenterObject.Clone(); // Clone it because CameraPreviewPanel can change the object and we do not want to change the original

            CameraPreviewPanel1.CenterObjectModel3D = newCenterObject;
        }
    }
}
