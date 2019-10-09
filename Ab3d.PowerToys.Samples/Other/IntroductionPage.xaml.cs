using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using Ab3d.PowerToys.Samples.Common;

namespace Ab3d.PowerToys.Samples.Other
{
    public partial class IntroductionPage : Page
    {
        public IntroductionPage()
        {
            // NOTE:
            // If you get:
            // "A first chance exception of type 'System.IO.FileNotFoundException' occurred in mscorlib.dll"
            // FileNotFoundException here, please continue execution or disable stopping on exceptions.
            // The exception can be thrown by DocumentViewer in .Net 4 but is handled inside .Net (there seems to be no way to prevent that; in .Net 3.5 this worked without any exception)

            InitializeComponent();

            this.Loaded += (sender, args) => UpdateInfoTextBlockWidths();
            this.SizeChanged += (sender, args) => UpdateInfoTextBlockWidths();
        }

        private void UpdateInfoTextBlockWidths()
        {
            if (ActualWidth < 750)
            {
                // Hide the image
                BannerImage.Visibility = Visibility.Collapsed;

                InfoTextBlock.ClearValue(TextBlock.MaxWidthProperty);
            }
            else
            {
                // Adjust the TextBlockEx controls width so the controls do not overlap with with BannerImage
                double infoTextWidth = ActualWidth - BannerImage.ActualWidth - 60;
                InfoTextBlock.MaxWidth = infoTextWidth;

                if (BannerImage.Visibility == Visibility.Collapsed)
                    BannerImage.Visibility = Visibility.Visible;
            }
        }

        private void InfoTextBlock_OnLinkClicked(TextBlockEx sender, TextBlockEx.LinkClickedEventArgs e)
        {
            if (e.Url.Contains("/support"))
            {
                var mainWindow = (MainWindow) Application.Current.MainWindow; // This is not nice code, but it works for our case
                mainWindow.ShowSupportPage();
            }
            else
            {
                Process.Start(e.Url);
            }
        }
    }
}