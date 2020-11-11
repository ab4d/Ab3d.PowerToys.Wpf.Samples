using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

namespace Ab3d.PowerToys.Samples
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Uncomment the _startupPage declaration to always start the samples with the specified page
        //private string _startupPage = "Lines3D/DynamicEdgeLinesSample.xaml";
        private string _startupPage = null;

        public MainWindow()
        {
            InitializeComponent();

            // SelectionChanged event handler is used to start the samples with the page set with _startupPage field.
            // SelectionChanged is used because SelectItem cannot be set from this.Loaded event.
            SampleList.SelectionChanged += delegate(object sender, SelectionChangedEventArgs args)
            {
                if (_startupPage != null)
                {
                    string savedStartupPage = _startupPage;
                    _startupPage = null;

                    SelectItem(savedStartupPage);
                }
            };
        }

        public void ShowSupportPage()
        {
            SelectItem("Other/SupportPage.xaml");
        }

        private void SelectItem(string pageName)
        {
            var supportPageElement = SampleList.Items.OfType<System.Xml.XmlElement>()
                                                     .First(x => x.Attributes["Page"] != null && x.Attributes["Page"].Value == pageName);

            SampleList.SelectedItem = supportPageElement;

            SampleList.ScrollIntoView(supportPageElement);
        }

        private void TextBlock_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            string newDescriptionText = "";

            var node = e.NewValue as System.Xml.XmlNode;

            if (node != null && node.Attributes != null)
            {
                System.Xml.XmlAttribute attribute = node.Attributes["Description"];

                if (attribute != null)
                    newDescriptionText = attribute.Value;
            }

            DescriptionTextBlock.ContentText = newDescriptionText;
        }

        private void LogoImage_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://www.ab4d.com");
        }

        private void ContentFrame_OnNavigated(object sender, NavigationEventArgs e)
        {
            // Prevent navigation (for example clicking back button) because our ListBox is not updated when this navigation occurs
            // We prevent navigation with clearing the navigation history each time navigation item changes
            ContentFrame.NavigationService.RemoveBackEntry(); 
        }
    }
}
