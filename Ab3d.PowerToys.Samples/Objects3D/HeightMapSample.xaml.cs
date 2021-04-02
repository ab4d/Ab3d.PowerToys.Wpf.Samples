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

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for HeightMapSample.xaml
    /// </summary>
    public partial class HeightMapSample : Page
    {
        private string _heightMapFileName;

        public HeightMapSample()
        {
            InitializeComponent();

            _heightMapFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\HeightMaps\\simpleHeightMap.png");
            OpenHeightMapDataFile(_heightMapFileName);

            UpdateHeightMapMaterial();
        }

        private void MaterialRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateHeightMapMaterial();
        }

        private void UpdateHeightMapMaterial()
        { 
            if (SmoothGradientMaterialRadioButton.IsChecked ?? false)
            {
                // When using gradient texture then it is recommended to set UseHeightValuesAsTextureCoordinates to true.
                // In this case height values are used for texture coordinates - texture coordinate (0, 0.5) is set the minimum height value and texture coordinate (1, 0.5) is set to the maximum height value.
                // This requires a one dimensional gradient texture and usually produces more accurate results than when UseHeightValuesAsTextureCoordinates is false.
                // This should not be used for cases when a bitmap is shown on the height map
                //
                // // To demonstrate the quality different between different values of UseHeightValuesAsTextureCoordinates, set its value to false in the next if (for HardGradientMaterialRadioButton):
                HeigthMap1.UseHeightValuesAsTextureCoordinates = true;

                // Define the gradient
                GradientStopCollection stops = new GradientStopCollection();
                stops.Add(new GradientStop(Colors.Red, 1));
                stops.Add(new GradientStop(Colors.Yellow, 0.75));
                stops.Add(new GradientStop(Colors.LightGreen, 0.5));
                stops.Add(new GradientStop(Colors.Aqua, 0.25));
                stops.Add(new GradientStop(Colors.Blue, 0));


                LinearGradientBrush gradient = new LinearGradientBrush(stops);
                // NOTE: We do not have to specify the StartPoint and EndPoint

                // It will be used in the CreateHeightTextureFromGradient method 
                // to create the texture from the actual height map data and with the specified LinearGradientBrush

                HeigthMap1.CreateHeightTextureFromGradient(gradient);


                // This method is internally calling static CreateHeightTexture and GetGradientColorsArray methods on Ab3d.Meshes.HeightMapMesh3D.
                // It is also possible to create the textures manually (uncomment the following code):

                // The simple way
                ////WriteableBitmap texture = Ab3d.Meshes.HeightMapMesh3D.CreateHeightTexture(_data, _minYValue, _maxYValue, (LinearGradientBrush)selectedRectangle.Fill);

                // An advanced way with first creating an array of colors (with specifying the size of array - by default also 100):
                ////uint[] gradientColorsArray = Ab3d.Meshes.HeightMapMesh3D.GetGradientColorsArray(selectedRectangle.Fill as LinearGradientBrush, 100);
                ////WriteableBitmap texture = Ab3d.Meshes.HeightMapMesh3D.CreateHeightTexture(_data, _minYValue, _maxYValue, gradientColorsArray);

                // After that we set the Material of the HeightMap1
                ////HeightMap1.Material = new DiffuseMaterial(new ImageBrush(texture));
            }
            else if (HardGradientMaterialRadioButton.IsChecked ?? false)
            {
                // NOTE:
                // To demonstrate the quality different between different values of UseHeightValuesAsTextureCoordinates, set the following to false:
                HeigthMap1.UseHeightValuesAsTextureCoordinates = true;

                // The gradient with hard transition is defined by making the transition from one color to another very small (for example from 0.89 to 0.90)
                GradientStopCollection stops = new GradientStopCollection();
                stops.Add(new GradientStop(Colors.Red, 1));
                stops.Add(new GradientStop(Colors.Red, 0.9));
                stops.Add(new GradientStop(Colors.Yellow, 0.89));
                stops.Add(new GradientStop(Colors.Yellow, 0.75));
                stops.Add(new GradientStop(Colors.LightGreen, 0.749));
                stops.Add(new GradientStop(Colors.LightGreen, 0.5));
                stops.Add(new GradientStop(Colors.Aqua, 0.499));
                stops.Add(new GradientStop(Colors.Aqua, 0.25));
                stops.Add(new GradientStop(Colors.Blue, 0.249));
                stops.Add(new GradientStop(Colors.Blue, 0));


                LinearGradientBrush gradient = new LinearGradientBrush(stops);
                // NOTE: We do not have to specify the StartPoint and EndPoint

                // It will be used in the CreateHeightTextureFromGradient method 
                // to create the texture from the actual height map data and with the specified LinearGradientBrush

                HeigthMap1.CreateHeightTextureFromGradient(gradient);
            }
            else if (BitmapMaterialRadioButton.IsChecked ?? false)
            {
                // When showing texture on the height map, the UseHeightValuesAsTextureCoordinates must be set to false (this is its default value).
                // This way the texture coordinates will be set based on their position on the height map (texture coordinate (0, 0) is used for the first position and (1, 1) for the last position).
                HeigthMap1.UseHeightValuesAsTextureCoordinates = false;

                HeigthMap1.Material = new DiffuseMaterial(new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/PowerToysTexture.png", UriKind.RelativeOrAbsolute))));
            }
            else
            {
                // When using solid color material, then the value of UseHeightValuesAsTextureCoordinates is irrelevant.
                HeigthMap1.Material = new DiffuseMaterial(Brushes.Silver);
            }
        }

        private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            HeigthMap1.Size = new System.Windows.Media.Media3D.Size3D(HeigthMap1.Size.X, HeightSlider.Value, HeigthMap1.Size.Z);

            WireBox1.CenterPosition = new System.Windows.Media.Media3D.Point3D(WireBox1.CenterPosition.X, HeightSlider.Value / 2, WireBox1.CenterPosition.Z);
            WireBox1.Size = new System.Windows.Media.Media3D.Size3D(WireBox1.Size.X, HeightSlider.Value, WireBox1.Size.Z);
        }

        private void OpenHeightMapDataFile(string fileName)
        {
            BitmapImage heightImage = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute));

            double[,] heightData = OpenHeightMapDataFile(heightImage, InvertCheckBox.IsChecked ?? false);

            if (heightData != null)
                HeigthMap1.HeightData = heightData;
        }


        // Returns value in range from 0 to 1
        public static double[,] OpenHeightMapDataFile(BitmapSource heightImage, bool invertData)
        {
            var width         = heightImage.PixelWidth;
            var height        = heightImage.PixelHeight;
            var bytesPerPixel = heightImage.Format.BitsPerPixel / 8;

            byte[] heightImageArray = new byte[width * height * bytesPerPixel];
            heightImage.CopyPixels(heightImageArray, width * bytesPerPixel, 0);

            double[,] heightData = new double[width, height];

            double factor = 1.0 / (255.0 * bytesPerPixel); // this will be used to multiply the bytes (multiplying is faster than dividing)
            double offset = 0;

            if (invertData)
            {
                factor = -factor;
                offset = 1;
            }


            int index = 0;

            if (bytesPerPixel == 1) // optimize for 8-bit (one byte) per pixel (remove inner for)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        heightData[x, y] = heightImageArray[index] * factor + offset;
                        index++;
                    }
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int colorsSum = 0;
                        for (int i = 0; i < bytesPerPixel; i++)
                            colorsSum += heightImageArray[index + i];

                        heightData[x, y] = colorsSum * factor + offset;

                        index += bytesPerPixel;
                    }
                }
            }

            return heightData;
        }

        private void InvertCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            OpenHeightMapDataFile(_heightMapFileName);
        }
    }
}
