using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for InteractiveUserControl.xaml
    /// </summary>
    public partial class InteractiveUserControl : UserControl
    {
        private TypeConverter _colorTypeConverter;

        public event EventHandler ObjectSettingsChanged;

        public event EventHandler AddNewButtonClicked;

        public Color SelectedColor
        {
            get { return GetSelectedColor(); }
        }

        public bool IsBox
        {
            get { return BoxRadioButton.IsChecked ?? false; }
        }

        public InteractiveUserControl()
        {
            InitializeComponent();

            _colorTypeConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Color));
        }

        private Color GetSelectedColor()
        {
            Color selectedColor = Colors.Black;

            var listBoxItem = (ListBoxItem)ColorListBox.SelectedItem;

            if (listBoxItem != null)
            {
                string colorText = listBoxItem.Content as string;

                try
                {
                    selectedColor = (Color) _colorTypeConverter.ConvertFromString(colorText);
                }
                catch
                {
                }
            }

            return selectedColor;
        }

        protected void OnObjectSettingsChanged()
        {
            if (ObjectSettingsChanged != null)
                ObjectSettingsChanged(this, null);
        }

        private void AddNewButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (AddNewButtonClicked != null)
                AddNewButtonClicked(this, null);
        }

        private void ColorListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            OnObjectSettingsChanged();
        }

        private void OnShapeTypeCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            OnObjectSettingsChanged();
        }
    }
}
