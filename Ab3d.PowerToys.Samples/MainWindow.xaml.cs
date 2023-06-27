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
        //private string _startupPage = "OtherCameraControllers/CameraNavigationCirclesSample.xaml";
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

        private void RightSideBorder_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            string newDescriptionText = "";

            var node = e.NewValue as System.Xml.XmlNode;

            if (node != null && node.Attributes != null)
            {
                var descriptionAttribute = node.Attributes["Description"];

                if (descriptionAttribute != null)
                    newDescriptionText = descriptionAttribute.Value;


                var seeAlsoAttribute = node.Attributes["SeeAlso"];

                if (seeAlsoAttribute != null)
                {
                    var seeAlsoText = seeAlsoAttribute.Value;
                    var seeAlsoParts = seeAlsoText.Split(';');

                    var seeAlsoContent = new StringBuilder();
                    for (var i = 0; i < seeAlsoParts.Length; i++)
                    {
                        var seeAlsoPart = seeAlsoParts[i].Trim();
                        if (seeAlsoPart.Length == 0)
                            continue;

                        if (seeAlsoContent.Length > 0)
                            seeAlsoContent.Append(", ");

                        // TextBlockEx support links, for example: "click here \@Ab3d.PowerToys:https://www.ab4d.com/PowerToys.aspx| to learn more"
                        if (seeAlsoPart.StartsWith("\\@"))
                        {
                            seeAlsoContent.Append(seeAlsoPart);
                        }
                        else
                        {
                            string linkDescription;

                            // remove prefix that specifies the type ("T_"), property ("P_"), event ("E_"), ...
                            if (seeAlsoPart[1] == '_') // "T_Ab3d_Controls_MouseCameraController", "P_Ab3d_Controls_MouseCameraController_ClosedHandCursor", ...
                                linkDescription = seeAlsoPart.Substring(2);
                            else
                                linkDescription = seeAlsoPart;

                            linkDescription = linkDescription.Replace('_', '.')                // Convert '_' to '.'
                                                             .Replace("Ab3d.Controls.", "")    // Remove the most common namespaces (preserve less common, for example Ab3d.Utilities)
                                                             .Replace("Ab3d.Visuals.", "")
                                                             .Replace("Ab3d.Cameras.", "")
                                                             .Replace(".1", " (overload)")      // replace end _1: M_Ab3d_Cameras_BaseCamera_StartRotation_1.htm
                                                             .Replace(".2", " (overload)")
                                                             .Replace(".3", " (overload)");

                            if (seeAlsoPart.EndsWith(".html") || seeAlsoPart.EndsWith(".htm"))
                                linkDescription = linkDescription.Replace(".html", "").Replace(".htm", ""); // remove .html / .htm from linkDescription
                            else
                                seeAlsoPart += ".htm";                                                      // and make sure that the link will end with .htm

                            seeAlsoContent.AppendFormat("\\@{0}:https://www.ab4d.com/help/PowerToys/html/{1}|", linkDescription, seeAlsoPart);
                        }
                    }

                    if (seeAlsoContent.Length > 0)
                    {
                        if (newDescriptionText.Length > 0 && !newDescriptionText.EndsWith("\\n"))
                            newDescriptionText += "\\n"; // Add new line for TextBlockEx

                        newDescriptionText += "See also: " + seeAlsoContent.ToString();
                    }
                }
            }

            if (newDescriptionText.Length > 0)
            {
                DescriptionTextBlock.ContentText = newDescriptionText;
                DescriptionExpander.Visibility = Visibility.Visible;
            }
            else
            {
                DescriptionTextBlock.ContentText = null;
                DescriptionExpander.Visibility = Visibility.Collapsed;
            }
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
