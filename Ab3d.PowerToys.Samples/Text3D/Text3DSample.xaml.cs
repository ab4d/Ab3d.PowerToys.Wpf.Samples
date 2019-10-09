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
using Ab3d.Common.Cameras;

namespace Ab3d.PowerToys.Samples.Text3D
{
    /// <summary>
    /// Interaction logic for Text3DSample.xaml
    /// </summary>
    public partial class Text3DSample : Page
    {
        public Text3DSample()
        {
            InitializeComponent();

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                RefreshText();

                TextBox1.Focus();
                TextBox1.CaretIndex = TextBox1.Text.Length;
            };
        }


        private void OnTextSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            RefreshText();
        }

        private void TextBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            TextVisual1.Text = TextBox1.Text;
            CenteredTextVisual1.Text = TextBox1.Text;

            RefreshRectangle();
        }

        private void OnTextVisualTypeChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (NormalRadioButton.IsChecked ?? false)
                TextBox1.Text = TextBox1.Text.Replace("CenteredTextVisual3D", "TextVisual3D");
            else
                TextBox1.Text = TextBox1.Text.Replace("TextVisual3D", "CenteredTextVisual3D");


            // We need to refresh the rectangle as it is not binded
            RefreshRectangle();
        }

        private void RefreshRectangle()
        {
            Size textSize = Ab3d.Models.Text3DFactory.MeasureText(TextVisual1.Text, TextVisual1.FontSize);

            if (textSize.IsEmpty)
            {
                RectangleVisual1.IsVisible = false;

                TextSizeTextBlock.Text = "Text size: Empty";
            }
            else
            {
                if (NormalRadioButton.IsChecked ?? false)
                {
                    RectangleVisual1.Position = new Point3D(TextVisual1.Position.X - TextVisual1.LineThickness,
                                                            TextVisual1.Position.Y + TextVisual1.LineThickness,
                                                            TextVisual1.Position.Z);
                }
                else
                {
                    // centered text
                    RectangleVisual1.Position = new Point3D(CenteredTextVisual1.CenterPosition.X - CenteredTextVisual1.LineThickness - textSize.Width / 2,
                                                            CenteredTextVisual1.CenterPosition.Y + CenteredTextVisual1.LineThickness + textSize.Height / 2,
                                                            CenteredTextVisual1.CenterPosition.Z);
                }

                RectangleVisual1.Size = new Size(textSize.Width + TextVisual1.LineThickness * 2,
                                                 textSize.Height + TextVisual1.LineThickness * 2);

                if (!RectangleVisual1.IsVisible)
                    RectangleVisual1.IsVisible = true;


                TextSizeTextBlock.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                       "Text size: {0:0.#} x {1:0.#}", 
                                                       textSize.Width, textSize.Height);
            }
        }

        private void RefreshText()
        {
            // We can change only CenteredTextVisual1 properties 
            // The TextVisual1 properties will be changed also because they are binded

            // First we start the initialization with BeginInit to prevent mesh recreation after each change
            CenteredTextVisual1.BeginInit();

            CenteredTextVisual1.Text = TextBox1.Text;
            CenteredTextVisual1.LineThickness = double.Parse((string)((ComboBoxItem)LineThicknessComboBox.SelectedItem).Content);
            CenteredTextVisual1.FontSize = double.Parse((string)((ComboBoxItem)FontSizeComboBox.SelectedItem).Content);

            switch ((string)((ComboBoxItem)TextColorComboBox.SelectedItem).Content)
            {
                case "White":
                    CenteredTextVisual1.TextColor = Colors.White;
                    break;

                case "Yellow":
                    CenteredTextVisual1.TextColor = Colors.Yellow;
                    break;

                case "LightBlue":
                    CenteredTextVisual1.TextColor = Colors.LightBlue;
                    break;
            }

            CenteredTextVisual1.EndInit();

            RefreshRectangle();
        }
    }
}
