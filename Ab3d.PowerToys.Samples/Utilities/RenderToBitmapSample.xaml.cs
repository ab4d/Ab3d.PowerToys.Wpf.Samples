using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Ab3d.Cameras;
using Ab3d.Common.Cameras;
using Microsoft.Win32;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for RenderToBitmapSample.xaml
    /// </summary>
    public partial class RenderToBitmapSample : Page
    {
        public RenderToBitmapSample()
        {
            InitializeComponent();
        }

        private void RenderImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            var renderedBitmap = RenderToBitmap(Camera1);
            SaveBitmap(renderedBitmap);
        }

        private void RenderOfflineViewportButton_OnClick(object sender, RoutedEventArgs e)
        {
            // This method shows how to render a bitmap from a Viewport3D that is not shown in WPF visual tree (offline Viewport3D)

            // First create a new instance of Viewport3D
            var viewport3D = new Viewport3D();

            // IMPORTANT !!!
            // You need to specify the size of the Viewport3D (this is needed because projection matrix requires an aspect ratio value).
            viewport3D.Width = 600;
            viewport3D.Height = 400;


            // It is recommended to create the camera before the objects are added to the scene.
            // This is especially true for 3D lines because this will generate the initial line geometry with the correct camera setting.
            // If the camera is added later or changed after the 3D lines are added to the scene, you need to manually call Refresh on LinesUpdater to regenerate 3D lines:
            // Ab3d.Utilities.LinesUpdater.Instance.Refresh();
            //
            // This is not needed when the Viewport3D is shown in WPF visual tree.

            var targetPositionCamera = new TargetPositionCamera()
            {
                Heading = 30,
                Attitude = -20,
                Distance = 50,
                ShowCameraLight = ShowCameraLightType.Always,
                TargetViewport3D = viewport3D
            };

            // When Viewport3D is not shown, we need to manually refresh the camera to initialize property and add the light to the scene.
            targetPositionCamera.Refresh();


            // Now add 3D objects
            var boxVisual3D = new Ab3d.Visuals.BoxVisual3D()
            {
                CenterPosition = new Point3D(0, 0, 0),
                Size = new Size3D(8, 8, 8),
                Material = new DiffuseMaterial(Brushes.Orange),
            };

            viewport3D.Children.Add(boxVisual3D);


            var wireBoxVisual3D = new Ab3d.Visuals.WireBoxVisual3D()
            {
                CenterPosition = new Point3D(0, 0, 0),
                Size = new Size3D(10, 10, 10),
                LineColor = Colors.Red,
                LineThickness = 1
            };

            viewport3D.Children.Add(wireBoxVisual3D);


            // If the camera was changed after the 3D lines were added, we need to manually regenerate 3D line geometry by calling Refresh on LinesUpdater:
            //Ab3d.Utilities.LinesUpdater.Instance.Refresh();
            //
            // This is not needed when the Viewport3D is shown in WPF visual tree.

            // Now we can render Viewport3D to bitmap
            var renderedBitmap = RenderToBitmap(targetPositionCamera);
            SaveBitmap(renderedBitmap);


            // To make a simple demonstration on how to create a sequence of bitmaps
            // with changes the camera and object uncomment the following coed (you can also comment calling the SaveBitmap method in the RenderToBitmap)

            //for (int i = 1; i < 5; i++)
            //{
            //    targetPositionCamera.Heading = i * 20;
            //    boxVisual3D.Transform = new ScaleTransform3D(1, (double)i / 4, 1);

            //    // Now we can render Viewport3D to bitmap
            //    var bitmap = RenderToBitmap(targetPositionCamera);

            //    SaveBitmap(bitmap, $"c:\\temp\\bitmap_{i}.png"); // Change the path to a path that exist on your computer
            //}
        }


        private BitmapSource RenderToBitmap(BaseCamera cameraToRender)
        {
            var selectedAntialiasingLevel = GetSelectedAntialiasingLevel();
            var selectedDpi = GetSelectedDpi();

            int customWidth, customHeight;
            GetCustomBitmapSize(out customWidth, out customHeight);

            Brush backgroundBrush;
            if (BackgroundBrushCheckBox.IsChecked ?? false)
                backgroundBrush = ViewportBorder.Background; // Use current background gradient, but we could also specify any other brush that will be used as background on the bitmap
            else
                backgroundBrush = null; // Transparent background

            BitmapSource bitmap;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait; // This can take some time

                // For demonstration purposes we use call two different overloads of RenderToBitmap method
                // The the simple RenderToBitmap method is called when no custom size or antialiasing is required (this method uses a simpler and faster rendering methods)
                // We could call only the second method (internally that method checks for custom size or antialiasing and if they are not required calls the first method).
                // But for this demonstration I wanted to show you that you have two options

                if (customWidth == 0 && customHeight == 0 && selectedAntialiasingLevel < 1)
                {
                    // Note that backgroundBrush and dpi parameters are optional (backgroundBrush = null, dpi = 96)
                    // so if you want to get a bitmap of the current Viewport3D, you can simply call RenderToBitmap without any parameters
                    bitmap = cameraToRender.RenderToBitmap(backgroundBrush, selectedDpi);
                }
                else
                {
                    // When using customWidth and customHeight and the aspect ratio (= width / height) of the Viewport3D is not the same as
                    // the aspect ratio of the target bitmap, the Viewport3D is uniformly scaled to fill the target bitmap.
                    bitmap = cameraToRender.RenderToBitmap(customWidth, customHeight, selectedAntialiasingLevel, backgroundBrush, selectedDpi);
                }

                // RenderToBitmap is internally using Ab3d.Utilities.BitmapRendering.RenderToBitmap method.
                // You can use it to render any other FrameworkElement (instead of Viewport3D) to bitmap.
                // You can also use it to set the scaleToFill to false.


                // IMPORTANT:
                // If you will be calling RenderToBitmap ofter, then please note that there can be some problems when RenderTargetBitmap is used very ofter in WPF.
                // You can get "Out of memory" exceptions because garbage collector is not aware of the native bitmaps behind the RenderTargetBitmap.
                // A common workaround for that is to manually trigger garbage collection after some created RenderTargetBitmap objects - this can be done with the following 3 lines:
                // GC.Collect();
                // GC.WaitForPendingFinalizers();
                // GC.Collect();
                //
                // This is also used after saving the bitmap a few lines below.
                //
                // Another workaround is to reuse the RenderTargetBitmap instances. 
                // To do this you will need to change the source code for the RenderToBitmap method.
                // In this case please buy a Ab3d.PowerToys source code. You can also send me an email and I will provide you the original source code for the method.
                // 
                // If RenderToBitmap is used very ofter, then please check the internet for more information about that.
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;

                // This can happen when to big image is rendered (with combination of big antialiasing and DPI)
                MessageBox.Show("Error rendering bitmap:\r\n" + ex.Message);

                bitmap = null;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            return bitmap;
        }

        private void SaveBitmap(BitmapSource bitmap)
        {
            if (bitmap == null)
                return;


            var fileDialog = new Microsoft.Win32.SaveFileDialog();
            fileDialog.DefaultExt = "png";
            fileDialog.Filter = "Png files(*.png)|*.png";
            fileDialog.Title = "Select output png file";
            fileDialog.FileName = "Viewport3D.png";

            if (fileDialog.ShowDialog() ?? false)
            {
                try
                {
                    SaveBitmap(bitmap, fileDialog.FileName);

                    // Open the image in image viewer
                    Process.Start(fileDialog.FileName);


                    // To prevent memory problems when using RenderTargetBitmap (created by RenderToBitmap)
                    // we can do a manual garbage collect (see comments a few lines before)
                    // Here the user will not notice the wait for the garbage collection.

                    bitmap = null; // first release the reference

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving image:\r\n" + ex.Message);
                }
            }
        }

        private void SaveBitmap(BitmapSource bitmap, string fileName)
        {
            if (bitmap == null)
                return;

            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }
        }

        private void GetCustomBitmapSize(out int customWidth, out int customHeight)
        {
            // It is not very generic to use switch for ComboBox but for this simple demo it should be ok to use it.
            switch (BitmapSizeComboBox.SelectedIndex)
            {
                case 0:
                default:
                    // Use same size as current Viewport3D size
                    customWidth = 0;
                    customHeight = 0;
                    break;

                case 1:
                    customWidth = 600;
                    customHeight = 400;
                    break;

                case 2:
                    customWidth = 1280;
                    customHeight = 720;
                    break;

                case 3:
                    customWidth = 1920;
                    customHeight = 1080;
                    break;

                case 4:
                    // We set only width, height is set automatically based on the current Viewport3D size
                    customWidth = 1000;
                    customHeight = 0;
                    break;

                case 5:
                    // We set only height, width is set automatically based on the current Viewport3D size
                    customWidth = 0;
                    customHeight = 400;
                    break;
            }
        }

        private int GetSelectedAntialiasingLevel()
        {
            // we could also use Math.Power(2, AntialiasingComboBox.SelectedIndex), but the following is more simple:
            switch (AntialiasingComboBox.SelectedIndex)
            {
                case 0:
                default:
                    return 0; // or 1 - no antialiasing

                case 1:
                    return 2;

                case 2:
                    return 4;

                case 3:
                    return 8;
            }
        }

        private int GetSelectedDpi()
        {
            // we could also use Math.Power(2, AntialiasingComboBox.SelectedIndex), but the following is more simple:
            switch (DpiComboBox.SelectedIndex)
            {
                case 0:
                default:
                    return 72;

                case 1:
                    return 96; // This is standard WPF DPI setting

                case 2:
                    return 150;

                case 3:
                    return 300;
            }
        }
    }
}
