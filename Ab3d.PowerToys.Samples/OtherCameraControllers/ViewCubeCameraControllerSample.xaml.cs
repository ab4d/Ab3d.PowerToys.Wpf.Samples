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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab3d.Controls;

namespace Ab3d.PowerToys.Samples.OtherCameraControllers
{
    /// <summary>
    /// Interaction logic for ViewCubeCameraControllerSample.xaml
    /// </summary>
    public partial class ViewCubeCameraControllerSample : Page
    {
        public ViewCubeCameraControllerSample()
        {
            InitializeComponent();


            var textColor = Colors.Black; // default color is Color.FromRgb(40, 40, 40);
            var borderBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40)); // Same as default brush
            var backgroundBrush = Brushes.LightGreen; // Default Brush is the same as ViewCubeCameraController.Foreground
            int fontSize = 27; // Same as default size

            ViewCubeCameraController3.SetViewCubeBitmaps(/* right  */ ViewCubeCameraController.RenderViewCubeBitmap("EAST",  fontSize, textColor, borderBrush, backgroundBrush),
                                                         /* left   */ ViewCubeCameraController.RenderViewCubeBitmap("WEST",  fontSize, textColor, borderBrush, backgroundBrush),
                                                         /* top    */ ViewCubeCameraController.RenderViewCubeBitmap("UP",    fontSize, textColor, borderBrush, backgroundBrush),
                                                         /* bottom */ ViewCubeCameraController.RenderViewCubeBitmap("DOWN",  fontSize, textColor, borderBrush, backgroundBrush),
                                                         /* front  */ ViewCubeCameraController.RenderViewCubeBitmap("SOUTH", fontSize, textColor, borderBrush, backgroundBrush),
                                                         /* bach   */ ViewCubeCameraController.RenderViewCubeBitmap("NORTH", fontSize, textColor, borderBrush, backgroundBrush));

            ViewCubeCameraController3.Foreground = backgroundBrush; // This will make the rotation circle in the same color as the background of the face's bitmaps


            // If you would like to customize how the RenderViewCubeBitmap renders the bitmap, see the method source code below.
        }

        // Source code that creates the default bitmaps:

        //private void SetDefaultViewCubeBitmaps()
        //{
        //    var textColor = Color.FromRgb(40, 40, 40);
        //    var borderBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40));
        //    var backgroundBrush = this.Foreground;
        //    int fontSize = 27;

        //    SetViewCubeBitmaps(RenderViewCubeBitmap("RIGHT",  fontSize, textColor, borderBrush, backgroundBrush),
        //                       RenderViewCubeBitmap("LEFT",   fontSize, textColor, borderBrush, backgroundBrush),
        //                       RenderViewCubeBitmap("TOP",    fontSize, textColor, borderBrush, backgroundBrush),
        //                       RenderViewCubeBitmap("BOTTOM", fontSize, textColor, borderBrush, backgroundBrush),
        //                       RenderViewCubeBitmap("FRONT",  fontSize, textColor, borderBrush, backgroundBrush),
        //                       RenderViewCubeBitmap("BACK",   fontSize, textColor, borderBrush, backgroundBrush));
        //}

        ///// <summary>
        ///// Renders the specified text to a 128 x 128 texture that can be used for a face on the ViewCube.
        ///// </summary>
        ///// <param name="text">text</param>
        ///// <param name="fontSize">size of the text</param>
        ///// <param name="textColor">color of the text</param>
        ///// <param name="borderBrush">brush of the border</param>
        ///// <param name="backgroundBrush">background brush</param>
        ///// <returns>BitmapSource that can be used for a face on the ViewCube</returns>
        //public static BitmapSource RenderViewCubeBitmap(string text, int fontSize, Color textColor, Brush borderBrush, Brush backgroundBrush)
        //{
        //    var border = new Border()
        //    {
        //        BorderBrush = borderBrush,
        //        BorderThickness = new Thickness(1, 1, 1, 1),
        //        Background = backgroundBrush,
        //        Width = 128,
        //        Height = 128
        //    };

        //    var textBlock = new TextBlock()
        //    {
        //        Text = text,
        //        FontSize = fontSize,
        //        FontWeight = FontWeights.Bold,
        //        Foreground = new SolidColorBrush(textColor),
        //        VerticalAlignment = VerticalAlignment.Center,
        //        HorizontalAlignment = HorizontalAlignment.Center
        //    };

        //    border.Child = textBlock;


        //    border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        //    border.Arrange(new Rect(0, 0, border.Width, border.Height));

        //    var renderedBitmap = new RenderTargetBitmap((int)border.Width, (int)border.Height, 96, 96, PixelFormats.Pbgra32);
        //    renderedBitmap.Render(border);

        //    return renderedBitmap;
        //}
    }
}
