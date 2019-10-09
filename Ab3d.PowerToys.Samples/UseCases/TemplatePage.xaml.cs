using System;
using System.Collections.Generic;
using System.IO;
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
using Path = System.IO.Path;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for TemplatePage.xaml
    /// </summary>
    public partial class TemplatePage : Page
    {
        public TemplatePage()
        {
            InitializeComponent();

            var stackPanel = new StackPanel()
            {
                Orientation = Orientation.Vertical
            };

            
            // titleTextBlock is defined here because it is not part of standard XAML
            var titleTextBlock = new TextBlock()
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = Brushes.DimGray,
                Margin = new Thickness(10, 10, 10, 10),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextWrapping = TextWrapping.Wrap
            };

            titleTextBlock.Text = 
@"This page represents an XAML template for the most commonly used Ab3d.PowerToys controls and classes.
You can simply grab the XAML and copy it to your UserControl, Page or Window to quickly add the standard boilerplate for any 3D content.";

            stackPanel.Children.Add(titleTextBlock);


            var button = new Button()
            {
                Content = "Copy XAML to clipboard",
                FontSize = 16,
                Margin = new Thickness(10, 0, 10, 10),
                Padding = new Thickness(10, 3, 10, 3),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            button.Click += ButtonOnClick;

            stackPanel.Children.Add(button);


            RootGrid.Children.Add(stackPanel);
        }

        private void ButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\UseCases\TemplatePage.xaml");

                string xaml = File.ReadAllText(fileName);

                Clipboard.SetText(xaml);

                MessageBox.Show("Successfully copied template XAML to clipboard");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error getting XAML to clipboard\r\n" + ex.Message);
            }
        }
    }
}
