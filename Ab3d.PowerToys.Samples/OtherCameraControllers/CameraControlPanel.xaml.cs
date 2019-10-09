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
using System.Windows.Media.Media3D;
using Ab3d.Controls;

namespace Ab3d.PowerToys.Samples.OtherCameraControllers
{
    /// <summary>
    /// Interaction logic for CameraControlPanel.xaml
    /// </summary>
    public partial class CameraControlPanel : Page
    {
        public CameraControlPanel()
        {
            InitializeComponent();
        }

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            // make all the changes at once
            SceneCamera1.BeginInit();

            SceneCamera1.Heading = -30;
            SceneCamera1.Attitude = -15;
            SceneCamera1.Distance = 2;
            SceneCamera1.Offset = new Vector3D(0, 0, 0);

            SceneCamera1.EndInit();
        }

        private void MoveAmountSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MoveAmountSlider.Value <= 0.1)
                MoveAmountValueTextBlock.Text = "(auto)";
            else
                MoveAmountValueTextBlock.Text = string.Format("{0:0}", MoveAmountSlider.Value);
        }
    }
}
