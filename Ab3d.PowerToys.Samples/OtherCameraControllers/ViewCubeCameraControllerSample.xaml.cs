using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private TypeConverter _colorTypeConverter;

        private bool _isCustomViewCubeBitmaps;

        public ViewCubeCameraControllerSample()
        {
            InitializeComponent();

            // To preserve default plane textures but only use custom texts, you can change the DefaultCubePlaneTexts.
            // This needs to be done before the control is loaded. For example:
            //ViewCubeCameraController1.DefaultCubePlaneTexts = new string[] { "EAST", "WEST", "UP", "DOWN", "SOUTH", "NORTH" };
        }

        private void OnViewCubeSettingsChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            ResetViewCubeSettings();
        }

        private void OnIsEdgeSelectionEnabledCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            // Set IsEdgeSelectionEnabled here instead of using binding in XAML (when using biding the IsEdgeSelectionEnabled value is not yet set when calling SetDefaultViewCubeBitmaps in this method).
            ViewCubeCameraController1.IsEdgeSelectionEnabled = IsEdgeSelectionEnabledCheckBox.IsChecked ?? false;

            if (_isCustomViewCubeBitmaps)
                SetupCustomViewCubeBitmaps();
            else
                ViewCubeCameraController1.SetDefaultViewCubeBitmaps(); // Renders default ViewCube bitmaps based on the Foreground and SelectionBrush properties
        }


        private void OnIsCustomBitmapsShownCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (IsCustomBitmapsShownCheckBox.IsChecked ?? false)
            {
                SetupCustomViewCubeBitmaps();
            }
            else
            {
                RemoveCustomViewCubeBitmaps();
                ResetViewCubeSettings();
            }
        }

        private void ResetViewCubeSettings()
        {
            double size = 100 + SizeComboBox.SelectedIndex * 50; // 0: 100; 1: 150; 2: 200

            ViewCubeCameraController1.Width  = size;
            ViewCubeCameraController1.Height = size;


            ViewCubeCameraController1.Foreground        = GetBrushFromComboBox(ForegroundComboBox);
            ViewCubeCameraController1.SelectionBrush    = GetBrushFromComboBox(SelectionBrushComboBox);
            ViewCubeCameraController1.AmbientLightColor = GetColorFromComboBox(AmbientLightColorComboBox);
            ViewCubeCameraController1.Background        = GetBrushFromComboBox(BackgroundComboBox);

            if (_isCustomViewCubeBitmaps)
                SetupCustomViewCubeBitmaps();
        }

        private void RemoveCustomViewCubeBitmaps()
        {
            ViewCubeCameraController1.SetViewCubeBitmaps(null);
            ViewCubeCameraController1.SetSelectedViewCubeBitmaps(null);

            _isCustomViewCubeBitmaps = false;
        }

        private Brush GetBrushFromComboBox(ComboBox comboBox)
        {
            var color = GetColorFromComboBox(comboBox);
            return new SolidColorBrush(color);
        }

        private Color GetColorFromComboBox(ComboBox comboBox)
        {
            var comboBoxItem = comboBox.SelectedItem as ComboBoxItem;

            if (comboBoxItem != null && comboBoxItem.Content != null)
            {
                var selectedText = comboBoxItem.Content.ToString();

                if (_colorTypeConverter == null)
                    _colorTypeConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Color));

                // Remove text after space (for example "White (default)")
                int spacePos = selectedText.IndexOf(' ');
                if (spacePos != -1)
                    selectedText = selectedText.Substring(0, spacePos);

                return (Color)_colorTypeConverter.ConvertFromString(selectedText);
            }

            return Colors.Red; // Invalid
        }

        private void SetupCustomViewCubeBitmaps()
        {
            int fontSize    = 30;                                             // Same as default size
            var textColor   = Colors.DarkBlue;                                // default color is Color.FromRgb(40, 40, 40);
            var borderBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40)); // Same as default brush

            var foregroundColor = GetColorFromComboBox(ForegroundComboBox);
            var selectionColor  = GetColorFromComboBox(SelectionBrushComboBox);

            // IMPORTANT:
            // When IsEdgeSelectionEnabled is true, then the backgroundBrush must be transparent.
            // If not, then the edge and corner selection will not be visible through the background.
            Brush bitmapBackgroundBrush;
            if (ViewCubeCameraController1.IsEdgeSelectionEnabled)
                bitmapBackgroundBrush = new RadialGradientBrush(foregroundColor, Colors.Transparent);
            else
                bitmapBackgroundBrush = new RadialGradientBrush(selectionColor, foregroundColor);

            var selectedBackgroundBrush = new RadialGradientBrush(foregroundColor, selectionColor);

            var viewCubePlaneTexts = new string[] { "EAST", "WEST", "UP", "DOWN", "SOUTH", "NORTH" };


            _normalViewCubeBitmaps   = new BitmapSource[6];
            _selectedViewCubeBitmaps = new BitmapSource[6];

            for (var i = 0; i < 6; i++)
            {
                string planeText = viewCubePlaneTexts[i];

                // To get default text use:
                //string planeText = ViewCubeCameraController1.DefaultCubePlaneTexts[i];

                _normalViewCubeBitmaps[i]   = ViewCubeCameraController.RenderViewCubeBitmap(planeText, fontSize - 2, textColor, borderBrush, bitmapBackgroundBrush, innerBorderBrush: null, borderThickness: 1, innerBorderThickness: 16, bitmapSize: 128);
                _selectedViewCubeBitmaps[i] = ViewCubeCameraController.RenderViewCubeBitmap(planeText, fontSize + 2, textColor, borderBrush, selectedBackgroundBrush, innerBorderBrush: null, borderThickness: 1, innerBorderThickness: 16, bitmapSize: 128);
            }

            ViewCubeCameraController1.SetViewCubeBitmaps(_normalViewCubeBitmaps);

            // IMPORTANT:
            // Custom selection bitmaps can be only used when edge selection is disabled (otherwise an exception is thrown)
            if (!ViewCubeCameraController1.IsEdgeSelectionEnabled)
                ViewCubeCameraController1.SetSelectedViewCubeBitmaps(_selectedViewCubeBitmaps);


            // Set also Foreground and SelectionBrush - this is used to color the rotation circle and
            // when IsEdgeSelectionEnabled is true to color the cube, edges and corners.
            ViewCubeCameraController1.Foreground     = new SolidColorBrush(foregroundColor);
            ViewCubeCameraController1.SelectionBrush = new SolidColorBrush(selectionColor);

            _isCustomViewCubeBitmaps = true;


            //If we would like to further customize the appearance of the ViewCubeCameraController
            // when mouse enters and leaves a plane, we can use the following events
            // (the code below just sets the bitmaps that are also set by SetViewCubeBitmaps and SetSelectedViewCubeBitmaps methods):

            //ViewCubeCameraController1.ViewCubePlaneEnter += delegate (object sender, ViewCubePlaneEventArgs e)
            //{
            //    int index = (int)e.Plane;
            //    ViewCubeCameraController1.SetViewCubeBitmap(e.Plane, _selectedViewCubeBitmaps[index]);

            //    e.CancelEventHandling = true; // We have manually updated the plane's bitmap so prevent ViewCubeCameraController doing that also
            //};

            //ViewCubeCameraController1.ViewCubePlaneLeave += delegate (object sender, ViewCubePlaneEventArgs e)
            //{
            //    int index = (int)e.Plane;
            //    ViewCubeCameraController1.SetViewCubeBitmap(e.Plane, _normalViewCubeBitmaps[index]);

            //    e.CancelEventHandling = true; // We have manually updated the plane's bitmap so prevent ViewCubeCameraController doing that also
            //};

            //ViewCubeCameraController1.RotationCircleEnter += delegate (object sender, EventArgs args)
            //{
            //    ViewCubeCameraController1.SetRotationCircleBrush(Brushes.Red);
            //};

            //ViewCubeCameraController1.RotationCircleLeave += delegate (object sender, EventArgs args)
            //{
            //    ViewCubeCameraController1.SetRotationCircleBrush(Brushes.Yellow);
            //};


            // For further customization see below for the source code of the SetDefaultViewCubeBitmaps and RenderViewCubeBitmap methods.
        }



        // Source code from ViewCubeCameraController that creates the default bitmaps:

        //private void SetDefaultViewCubeBitmaps()
        //{
        //    var textColor = Color.FromRgb(40, 40, 40);
        //    var borderBrush = new SolidColorBrush(textColor);
        //    int fontSize = (int)(PlaneBitmapSize * 0.21); // 27 for default plane size 128
        //    double borderThickness = PlaneBitmapSize / 64.0; // 2 for default plane size 128

        //    if (DefaultCubePlaneTexts == null || DefaultCubePlaneTexts.Length < 6)
        //        throw new Exception("DefaultCubePlaneTexts array is not defined or does not have at least 6 elements");


        //    Brush backgroundBrush;

        //    if (this.IsEdgeSelectionEnabled)
        //        backgroundBrush = null;
        //    else
        //        backgroundBrush = this.Foreground;

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


        //    if (this.IsEdgeSelectionEnabled)
        //        return; // Do not create selection bitmaps


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
        ///// <param name="backgroundBrush">(optional) background brush</param>
        ///// <param name="innerBorderBrush">(optional) when specified an inner border is rendered (used to show selected planes)</param>
        ///// <param name="borderThickness">(optional) thickness of the border; when not specified then 1 is used</param>
        ///// <param name="innerBorderThickness">(optional) thickness of the inner border; when not specified then 16 is used</param>
        ///// <param name="bitmapSize">(optional) size of the bitmap; when not specified then 128 is used</param>
        ///// <returns>BitmapSource that can be used for a face on the ViewCube</returns>
        //public static BitmapSource RenderViewCubeBitmap(string text,
        //                                                int fontSize,
        //                                                Color textColor,
        //                                                Brush borderBrush,
        //                                                Brush backgroundBrush = null,
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
