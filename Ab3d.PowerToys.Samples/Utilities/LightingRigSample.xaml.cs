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
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for LightingRigSample.xaml
    /// </summary>
    public partial class LightingRigSample : Page
    {
        public LightingRigSample()
        {
            InitializeComponent();
        }

        private void KeyLightDirectionSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            LigthingRig.SetKeyLightDirection(heading: KeyLightHeadingSlider.Value, attitude: KeyLightAttitudeSlider.Value);
        }

        private void LightBrightnessSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            LigthingRig.KeyLightColor = LightingRigVisual3D.GetColorFromBrightness(KeyLightBrightnessSlider.Value);
            LigthingRig.FillLightColor = LightingRigVisual3D.GetColorFromBrightness(FillLightBrightnessSlider.Value);
            LigthingRig.BackLightColor = LightingRigVisual3D.GetColorFromBrightness(BackLightBrightnessSlider.Value);
            LigthingRig.AmbientLightColor = LightingRigVisual3D.GetColorFromBrightness(AmbientLightBrightnessSlider.Value);
        }
    }
}
