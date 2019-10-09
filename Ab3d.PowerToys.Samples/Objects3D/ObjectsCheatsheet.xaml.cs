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

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for ObjectsCheatsheet.xaml
    /// </summary>
    public partial class ObjectsCheatsheet : Page
    {
        public ObjectsCheatsheet()
        {
            InitializeComponent();
        }

        private void InvokePrint(object sender, RoutedEventArgs e)
        {
            TitleGrid.Margin = new Thickness(60, 40, 60, 0);
            FirstPage.Margin = new Thickness(60, 0, 60, 0);
            SecondPage.Margin = new Thickness(60, 0, 60, 0);

            PrintDialog printDialog = new System.Windows.Controls.PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                PrintButton.Visibility = Visibility.Collapsed;
                IntroPanel.Visibility = Visibility.Collapsed;
                CopyrightPanel.Visibility = Visibility.Visible;

                PrintPanel(MainStackPanel, "3D Objects cheat sheet", printDialog, 0.75); // customScale = 0.75 - this should fit the document to 2 pages

                CopyrightPanel.Visibility = Visibility.Collapsed;
                IntroPanel.Visibility = Visibility.Visible;
                PrintButton.Visibility = Visibility.Visible;
            }

            TitleGrid.Margin = new Thickness(20, 0, 20, 0);
            FirstPage.Margin = new Thickness(20, 0, 20, 0);
            SecondPage.Margin = new Thickness(20, 0, 20, 0);
        }

        private void PrintPanel(Panel panelToPrint, string description, PrintDialog printDialog, double customScale)
        {
            double scale;
            Rect printableArea;
            System.Printing.PrintCapabilities capabilities;
            List<UIElement> uiElementsToPrint;

            FixedDocument myDocument;
            PageContent pageContent;
            StackPanel pageStackPanel;

            UIElement oneChild = null;
            double childHeight;

            double currentPageContentHeight;
            int pageNumber;



            capabilities = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);

            printableArea = new Rect(capabilities.PageImageableArea.OriginWidth, capabilities.PageImageableArea.OriginHeight,
                                     capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);


            if (panelToPrint.DesiredSize.Width > printableArea.Width)
                scale = printableArea.Width / panelToPrint.DesiredSize.Width;
            else
                scale = 1.0;

            if (customScale > 0 && customScale < scale)
                scale = customScale;

            // This does not work, because child elements that do not fit on the page are moved to the next page - this make the total height bigger than panelToPrint.DesiredSize.Height
            //if (fitToPagesCount > 0)
            //{
            //    double scale2;

            //    if (panelToPrint.DesiredSize.Height > printableArea.Height * fitToPagesCount)
            //    {
            //        scale2 = (printableArea.Height * fitToPagesCount) / panelToPrint.DesiredSize.Height;

            //        if (scale2 < scale)
            //            scale = scale2;
            //    }
            //}



            if (panelToPrint.DesiredSize.IsEmpty)
            {
                // If not measured and arranged yet, do it now
                panelToPrint.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                panelToPrint.Arrange(new Rect(new Point(0, 0), panelToPrint.DesiredSize));
            }




            // First disconnect the elements to print and collect them in uiElementsToPrint (in reverse order)
            uiElementsToPrint = new List<UIElement>();

            for (int i = panelToPrint.Children.Count - 1; i >= 0; i--)
            {
                oneChild = panelToPrint.Children[i];

                panelToPrint.Children.Remove(oneChild);
                uiElementsToPrint.Add(oneChild);
            }


            // Initialize
            currentPageContentHeight = 0;
            pageNumber = 0;

            myDocument = new FixedDocument();
            pageContent = null;
            pageStackPanel = null;

            childHeight = 0;

            FixedPage page = null;

            oneChild = null; // So it will not be added

            for (int i = uiElementsToPrint.Count - 1; i >= 0 || (pageContent == null && oneChild != null); i--) // !!! if (pageContent == null && oneChild != null) than we need to add new page and oneChild to it
            {
                if (pageContent == null)
                {
                    // Create new page
                    pageStackPanel = new StackPanel();
                    pageStackPanel.Orientation = Orientation.Vertical;
                    pageStackPanel.Margin = new Thickness(printableArea.X + 5, printableArea.Y + 5, printableArea.X + 5, printableArea.Y + 5);

                    page = new FixedPage();
                    page.Width = printDialog.PrintableAreaWidth;
                    page.Height = printDialog.PrintableAreaHeight;

                    page.Children.Add(pageStackPanel); // pageStackPanel will hold the controls to print 

                    pageContent = new PageContent();
                    ((System.Windows.Markup.IAddChild)pageContent).AddChild(page); // Child must be FixedPage

                    // Check if we need to scale to show the whole width
                    if (scale != 1.0)
                        pageStackPanel.LayoutTransform = new ScaleTransform(scale, scale);

                    currentPageContentHeight = 0;
                    pageNumber++;


                    if (oneChild != null)
                    {
                        // Add the child from previous loop pass

                        // Check if the child is too bit to go to one page - in this case scale the child to fit onto the page
                        if (childHeight > printableArea.Height)
                        {
                            double childScale;

                            childScale = printableArea.Height / childHeight;
                            oneChild.RenderTransform = new ScaleTransform(childScale, childScale);

                            childHeight *= childScale;
                        }

                        pageStackPanel.Children.Add(oneChild);
                        currentPageContentHeight += childHeight;
                    }
                }

                if (i < 0) // handle the case where we continued the loop to add the last element to the new page -> the new page with the last element is now created so we can break the loop
                    break;

                // Get the element
                oneChild = uiElementsToPrint[i];

                childHeight = oneChild.DesiredSize.Height * scale;


                if (currentPageContentHeight + childHeight > printableArea.Height)
                {
                    // The height with the oneChild would exceed the printable height
                    // So add the current page to the document, create a new page and add oneChild to the new page
                    myDocument.Pages.Add(pageContent);

                    pageContent = null; // this will create a new page
                }
                else
                {
                    // There is still room for oneChild

                    // And add it to new parent
                    pageStackPanel.Children.Add(oneChild);

                    currentPageContentHeight += childHeight;
                }
            }


            // Add last page (if it was not already added)
            if (pageContent != null)
                myDocument.Pages.Add(pageContent);

            printDialog.PrintDocument(myDocument.DocumentPaginator, description);



            for (int i = uiElementsToPrint.Count - 1; i >= 0; i--)
            {
                oneChild = uiElementsToPrint[i];

                StackPanel parent = VisualTreeHelper.GetParent(oneChild) as StackPanel;

                if (parent != null)
                    parent.Children.Remove(oneChild);

                panelToPrint.Children.Add(oneChild);
            }
        }
    }
}
