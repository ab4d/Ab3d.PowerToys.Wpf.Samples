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
        }

        private void MaterialRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (Material1RadioButton.IsChecked ?? false)
            {
                HeigthMap1.Material = new DiffuseMaterial(Brushes.Silver);
            }
            else if (Material2RadioButton.IsChecked ?? false)
            {
                HeigthMap1.Material = new DiffuseMaterial(new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/PowerToysTexture.png", UriKind.RelativeOrAbsolute))));
            }
            else
            {
                // Create the following LinearGradientBrush
                //<LinearGradientBrush StartPoint="0 1" EndPoint="0 0">
                //    <LinearGradientBrush.GradientStops>
                //        <GradientStop Color="Red" Offset="0"/>
                //        <GradientStop Color="Yellow" Offset="1"/>
                //    </LinearGradientBrush.GradientStops>
                //</LinearGradientBrush>

                GradientStopCollection stops = new GradientStopCollection();
                stops.Add(new GradientStop(Colors.Yellow, 1));
                stops.Add(new GradientStop(Colors.Red, 0));
                

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


        public static double[,] OpenHeightMapDataFile(BitmapSource heightImage, bool invertData)
        {
            if (heightImage.Format != PixelFormats.Gray8 && heightImage.Format != PixelFormats.Indexed8)
            {
                MessageBox.Show("This sample support only 8-bit grayscale images for height map data!");
                return null;
            }

            byte[] heightImageArray = new byte[heightImage.PixelWidth * heightImage.PixelHeight];
            heightImage.CopyPixels(heightImageArray, heightImage.PixelWidth, 0);

            double[,] heightData = new double[heightImage.PixelWidth, heightImage.PixelHeight];

            double factor = 1.0 / 255.0; // this will be used to multiply the bytes (multiplying is faster than dividing)

            int index = 0;

            if (invertData)
            {
                for (int y = 0; y < heightImage.PixelHeight; y++)
                {
                    for (int x = 0; x < heightImage.PixelWidth; x++)
                    {
                        // We invert the data
                        heightData[x, y] = 1.0 - (heightImageArray[index] * factor);
                        index++;
                    }
                }
            }
            else
            {
                for (int y = 0; y < heightImage.PixelHeight; y++)
                {
                    for (int x = 0; x < heightImage.PixelWidth; x++)
                    {
                        heightData[x, y] = heightImageArray[index] * factor;
                        index++;
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
