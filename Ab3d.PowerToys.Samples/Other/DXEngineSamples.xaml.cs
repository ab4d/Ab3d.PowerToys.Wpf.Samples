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

namespace Ab3d.PowerToys.Samples.Other
{
    /// <summary>
    /// Interaction logic for DXEngineSamples.xaml
    /// </summary>
    public partial class DXEngineSamples : Page
    {
        private string _sampleSolutionPath;

        public DXEngineSamples()
        {
            InitializeComponent();

            _sampleSolutionPath = System.IO.Path.Combine(
                                        AppDomain.CurrentDomain.BaseDirectory,
                                        @"..\..\..\..\Ab3d.DXEngine\Ab3d.DXEngine MAIN SAMPLES.sln");

            if (System.IO.File.Exists(_sampleSolutionPath))
            {
                OpenSolutionTextBlock.Visibility = Visibility.Visible;

                SampleImage.Cursor = Cursors.Hand;
                SampleImage.MouseLeftButtonUp += SampleImage_OnMouseLeftButtonUp;
            }
        }

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            OpenSolutionHyperlink.IsEnabled = false;
            OpenSamplesSolution();
        }

        private void SampleImage_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenSamplesSolution();
        }

        private void OpenSamplesSolution()
        {
            try
            {
                System.Diagnostics.Process.Start(_sampleSolutionPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error opening solution", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
