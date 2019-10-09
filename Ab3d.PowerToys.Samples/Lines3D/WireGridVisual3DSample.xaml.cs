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
using Ab3d.PowerToys.Samples.UseCases;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Ab3d.Common.EventManager3D;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    /// <summary>
    /// Interaction logic for WireGridVisual3DSample.xaml
    /// </summary>
    public partial class WireGridVisual3DSample : Page
    {
        private TypeConverter _sizeTypeConverter;
        private TypeConverter _colorTypeConverter;

        public WireGridVisual3DSample()
        {
            InitializeComponent();

            _sizeTypeConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Size));
            _colorTypeConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Color));

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                UpdateSize();
                UpdateLineColors();
            };
        }

        private void SizeComboBox_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateSize();
        }

        private void OnLineColorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateLineColors();
        }


        private void UpdateSize()
        {
            var comboBoxItem = SizeComboBox.SelectedItem as ComboBoxItem;

            if (comboBoxItem != null && comboBoxItem.Content != null)
            {
                var selectedSizeText = comboBoxItem.Content.ToString();
                WireGridVisual3D1.Size = (Size) _sizeTypeConverter.ConvertFromString(selectedSizeText);
            }
        }

        private void UpdateLineColors()
        {
            WireGridVisual3D1.LineColor = GetColorFromComboBox(LineColorComboBox);
            WireGridVisual3D1.MajorLineColor = GetColorFromComboBox(MajorLineColorComboBox);
        }

        private Color GetColorFromComboBox(ComboBox comboBox)
        {
            var comboBoxItem = comboBox.SelectedItem as ComboBoxItem;

            if (comboBoxItem != null && comboBoxItem.Content != null)
            {
                var selectedSizeText   = comboBoxItem.Content.ToString();
                return (Color)_colorTypeConverter.ConvertFromString(selectedSizeText);
            }

            return Colors.Red; // Invalid
        }

        private void OnRenderingTechniqueChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            WireGridVisual3D1.RenderingTechnique = (ScreenSpace3DLinesTechnique.IsChecked ?? false) ? 
                                                            WireGridVisual3D.WireGridRenderingTechniques.ScreenSpace3DLines : 
                                                            WireGridVisual3D.WireGridRenderingTechniques.FixedMesh3DLines;

            IsEmissiveMaterialCheckBox.IsEnabled = FixedMesh3DLinesTechnique.IsChecked ?? false;
        }
    }
}
