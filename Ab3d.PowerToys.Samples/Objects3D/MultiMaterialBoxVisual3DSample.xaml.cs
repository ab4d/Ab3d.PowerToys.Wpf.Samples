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

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for MultiMaterialBoxVisual3DSample.xaml
    /// </summary>
    public partial class MultiMaterialBoxVisual3DSample : Page
    {
        public MultiMaterialBoxVisual3DSample()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(MultiMaterialBoxVisual3DSample_Loaded);
        }

        void MultiMaterialBoxVisual3DSample_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            MultiMaterialBoxVisual3D1.TopMaterial    = GetMaterial(TopMaterialComboBox);
            MultiMaterialBoxVisual3D1.BottomMaterial = GetMaterial(BottomMaterialComboBox);
            MultiMaterialBoxVisual3D1.LeftMaterial   = GetMaterial(LeftMaterialComboBox);
            MultiMaterialBoxVisual3D1.RightMaterial  = GetMaterial(RightMaterialComboBox);
            MultiMaterialBoxVisual3D1.FrontMaterial  = GetMaterial(FrontMaterialComboBox);
            MultiMaterialBoxVisual3D1.BackMaterial   = GetMaterial(BackMaterialComboBox);

            MultiMaterialBoxVisual3D1.FallbackMaterial = GetMaterial(FallbackMaterialComboBox);
        }

        private Material GetMaterial(ComboBox comboBox)
        {
            Material material;

            switch (comboBox.SelectedIndex)
            {
                case 0:
                    material = null;
                    break;

                case 1:
                    material = new DiffuseMaterial(Brushes.LightBlue);
                    break;

                case 2:
                    material = new DiffuseMaterial(Brushes.Yellow);
                    break;

                default:
                case 3:
                    material = new DiffuseMaterial(Brushes.Red);
                    break;
            }

            return material;
        }

        private void TopMaterialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
                UpdateMaterial();
        }
    }
}
