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
using Ab3d.Common;
using Ab3d.Controls;

namespace Ab3d.PowerToys.Samples.OtherCameraControllers
{
    /// <summary>
    /// Interaction logic for ViewCubeCameraControllerSample.xaml
    /// </summary>
    public partial class ViewCubeCameraControllerSample : Page
    {
        private BitmapSource[] _normalViewCubeBitmaps;
        private BitmapSource[] _selectedViewCubeBitmaps;

        public ViewCubeCameraControllerSample()
        {
            InitializeComponent();


            int fontSize = 27; // Same as default size
            var textColor = Colors.DarkBlue; // default color is Color.FromRgb(40, 40, 40);
            var borderBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40)); // Same as default brush
            var backgroundBrush = Brushes.LightGreen; // Default Brush is the same as ViewCubeCameraController.Foreground

            var selectedBackgroundBrush = new RadialGradientBrush(Colors.White, Colors.LightGreen);

            var viewCubePlaneTexts = new string[] {"EAST", "WEST", "UP", "DOWN", "SOUTH", "NORTH"};

            _normalViewCubeBitmaps = new BitmapSource[6];
            _selectedViewCubeBitmaps = new BitmapSource[6];

            for (var i = 0; i < 6; i++)
            {
                string planeText = viewCubePlaneTexts[i];

                // To get default text use:
                //string planeText = ViewCubeCameraController3.DefaultCubePlaneTexts[i];

                _normalViewCubeBitmaps[i]   = ViewCubeCameraController.RenderViewCubeBitmap(planeText, fontSize - 2, textColor, borderBrush, backgroundBrush);
                _selectedViewCubeBitmaps[i] = ViewCubeCameraController.RenderViewCubeBitmap(planeText, fontSize + 2, textColor, borderBrush, selectedBackgroundBrush);

                // To render standard selection bitmap with thick border use the following:
                //_selectedViewCubeBitmaps[i] = ViewCubeCameraController.RenderViewCubeBitmap(planeText, fontSize + 2, textColor, borderBrush, Brushes.White, backgroundBrush, 2, 16, 128);
            }

            ViewCubeCameraController3.SetViewCubeBitmaps(_normalViewCubeBitmaps);
            ViewCubeCameraController3.SetSelectedViewCubeBitmaps(_selectedViewCubeBitmaps);

            // Set also Foreground and SelectionBrush - this is used to color the rotation circle
            ViewCubeCameraController3.Foreground = backgroundBrush; 
            ViewCubeCameraController3.SelectionBrush = new SolidColorBrush(Color.FromRgb(200, 255, 200)); // very light green


            // We could also set bitmaps with the SetViewCubeBitmaps method that does not take array of BitmapSources:
            //ViewCubeCameraController3.SetViewCubeBitmaps(/* right  */ ViewCubeCameraController.RenderViewCubeBitmap("EAST", fontSize, textColor, borderBrush, backgroundBrush),
            //                                             /* left   */ ViewCubeCameraController.RenderViewCubeBitmap("WEST", fontSize, textColor, borderBrush, backgroundBrush),
            //                                             /* top    */ ViewCubeCameraController.RenderViewCubeBitmap("UP", fontSize, textColor, borderBrush, backgroundBrush),
            //                                             /* bottom */ ViewCubeCameraController.RenderViewCubeBitmap("DOWN", fontSize, textColor, borderBrush, backgroundBrush),
            //                                             /* front  */ ViewCubeCameraController.RenderViewCubeBitmap("SOUTH", fontSize, textColor, borderBrush, backgroundBrush),
            //                                             /* back   */ ViewCubeCameraController.RenderViewCubeBitmap("NORTH", fontSize, textColor, borderBrush, backgroundBrush));



            // If we would like to further customize the appearance of the ViewCubeCameraController
            // when mouse enters and leaves a plane, we can use the following methods
            // (the code below just sets the bitmaps that are also set by SetViewCubeBitmaps and SetSelectedViewCubeBitmaps methods):

            //ViewCubeCameraController3.ViewCubePlaneEnter += delegate(object sender, ViewCubePlaneEventArgs e)
            //{
            //    int index = (int) e.Plane;
            //    ViewCubeCameraController3.SetViewCubeBitmap(e.Plane, _selectedViewCubeBitmaps[index]);

            //    e.CancelEventHandling = true; // We have manually updated the plane's bitmap so prevent ViewCubeCameraController doing that also
            //};

            //ViewCubeCameraController3.ViewCubePlaneLeave += delegate(object sender, ViewCubePlaneEventArgs e)
            //{
            //    int index = (int) e.Plane;
            //    ViewCubeCameraController3.SetViewCubeBitmap(e.Plane, _normalViewCubeBitmaps[index]);

            //    e.CancelEventHandling = true; // We have manually updated the plane's bitmap so prevent ViewCubeCameraController doing that also
            //};

            //ViewCubeCameraController3.RotationCircleEnter += delegate (object sender, EventArgs args)
            //{
            //    ViewCubeCameraController3.SetRotationCircleBrush(Brushes.Red);
            //};

            //ViewCubeCameraController3.RotationCircleLeave += delegate (object sender, EventArgs args)
            //{
            //    ViewCubeCameraController3.SetRotationCircleBrush(Brushes.Yellow);
            //};


            // If you would like to customize how the RenderViewCubeBitmap renders the bitmap, see the method source code below.
        }


        // Source code from ViewCubeCameraController that creates the default bitmaps:

        //private void SetDefaultViewCubeBitmaps()
        //{
        //    var textColor = Color.FromRgb(40, 40, 40);
        //    var borderBrush = new SolidColorBrush(textColor);
        //    var backgroundBrush = this.Foreground;
        //    int fontSize = (int)(PlaneBitmapSize * 0.21); // 27 for default plane size 128
        //    double borderThickness = PlaneBitmapSize / 64.0; // 2 for default plane size 128

        //    if (DefaultCubePlaneTexts == null || DefaultCubePlaneTexts.Length < 6)
        //        throw new Exception("DefaultCubePlaneTexts array is not defined or does not have at least 6 elements");

        //    var bitmaps = new BitmapSource[6];
        //    for (int i = 0; i < 6; i++)
        //    {
        //        bitmaps[i] = RenderViewCubeBitmap(text: DefaultCubePlaneTexts[i],
        //                                          fontSize: fontSize,
        //                                          textColor: textColor,
        //                                          borderBrush: borderBrush,
        //                                          backgroundBrush: backgroundBrush,
        //                                          innerBorderBrush: null,
        //                                          borderThickness: borderThickness,
        //                                          innerBorderThickness: 0,
        //                                          bitmapSize: PlaneBitmapSize);
        //    }

        //    SetViewCubeBitmaps(bitmaps);


        //    var selectionBrush = this.SelectionBrush;
        //    if (selectionBrush == null)
        //    {
        //        _selectedPlaneBitmaps = null;
        //    }
        //    else
        //    {
        //        double selectionBorderThickness = PlaneBitmapSize / 8.0; // 16 for default plane size 128

        //        bitmaps = new BitmapSource[6];
        //        for (int i = 0; i < 6; i++)
        //        {
        //            // The same as standard bitmap but has set selectionBrush as backgroundBrush and backgroundBrush as border brush with increased border thickness
        //            bitmaps[i] = RenderViewCubeBitmap(text: DefaultCubePlaneTexts[i],
        //                                              fontSize: fontSize,
        //                                              textColor: textColor,
        //                                              borderBrush: borderBrush,
        //                                              backgroundBrush: selectionBrush,
        //                                              innerBorderBrush: backgroundBrush,
        //                                              borderThickness: borderThickness,
        //                                              innerBorderThickness: selectionBorderThickness,
        //                                              bitmapSize: PlaneBitmapSize);
        //        }

        //        SetSelectedViewCubeBitmaps(bitmaps);
        //    }

        //    _isUsingCustomPlaneBitmaps = false;
        //}

        ///// <summary>
        ///// Renders the specified text to a texture that can be used for a face on the ViewCube.
        ///// </summary>
        ///// <param name="text">text</param>
        ///// <param name="fontSize">size of the text</param>
        ///// <param name="textColor">color of the text</param>
        ///// <param name="borderBrush">brush of the border</param>
        ///// <param name="backgroundBrush">background brush</param>
        ///// <param name="innerBorderBrush">(optional) when specified an inner border is rendered (used to show selected planes)</param>
        ///// <param name="borderThickness">(optional) thickness of the border; when not specified then 1 is used</param>
        ///// <param name="innerBorderThickness">(optional) thickness of the inner border; when not specified then 16 is used</param>
        ///// <param name="bitmapSize">(optional) size of the bitmap; when not specified then 128 is used</param>
        ///// <returns>BitmapSource that can be used for a face on the ViewCube</returns>
        //public static BitmapSource RenderViewCubeBitmap(string text,
        //                                                int fontSize,
        //                                                Color textColor,
        //                                                Brush borderBrush,
        //                                                Brush backgroundBrush,
        //                                                Brush innerBorderBrush = null,
        //                                                double borderThickness = 1,
        //                                                double innerBorderThickness = 16,
        //                                                int bitmapSize = 128)
        //{
        //    if (bitmapSize <= 0)
        //        throw new ArgumentOutOfRangeException(nameof(bitmapSize));

        //    // Use root grid so that text is rendered on top of any borders
        //    var rootGrid = new Grid();

        //    var border = new Border()
        //    {
        //        BorderBrush = borderBrush,
        //        BorderThickness = new Thickness(borderThickness),
        //        Width = bitmapSize,
        //        Height = bitmapSize,
        //        // border.Background is set to backgroundBrush only when innerBorder is not rendered (in the else below)
        //    };

        //    if (innerBorderBrush != null && innerBorderThickness > 0)
        //    {
        //        var innerBorder = new Border()
        //        {
        //            BorderBrush = innerBorderBrush,
        //            BorderThickness = new Thickness(innerBorderThickness),
        //            Background = backgroundBrush
        //        };

        //        border.Background = backgroundBrush;
        //        border.Child = innerBorder;
        //    }
        //    else
        //    {
        //        border.Background = backgroundBrush;
        //    }

        //    var textBlock = new TextBlock()
        //    {
        //        Text = text,
        //        FontSize = fontSize,
        //        FontWeight = FontWeights.Bold,
        //        Foreground = new SolidColorBrush(textColor),
        //        VerticalAlignment = VerticalAlignment.Center,
        //        HorizontalAlignment = HorizontalAlignment.Center
        //    };

        //    rootGrid.Children.Add(border);
        //    rootGrid.Children.Add(textBlock);


        //    rootGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        //    rootGrid.Arrange(new Rect(0, 0, bitmapSize, bitmapSize));

        //    var renderedBitmap = new RenderTargetBitmap(bitmapSize, bitmapSize, 96, 96, PixelFormats.Pbgra32);
        //    renderedBitmap.Render(rootGrid);

        //    return renderedBitmap;
        //}
    }
}
