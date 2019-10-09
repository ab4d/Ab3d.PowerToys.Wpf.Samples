﻿using System;
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

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for CameraOrientation.xaml
    /// </summary>
    public partial class CameraOrientation : Page
    {
        public CameraOrientation()
        {
            InitializeComponent();
        }

        private void BankSlider_OnValueChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            // Because we need to negate the angle we cannot directly bind EndAngle to slider's value
            BankLineArc.EndAngle = -BankSlider.Value;
        }
    }
}
