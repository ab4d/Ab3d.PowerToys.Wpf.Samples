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

namespace Ab3d.PowerToys.Samples.UseCases
{
    // Earth heigth map and image source: http://earthobservatory.nasa.gov/Features/BlueMarble/BlueMarble_monthlies.php

    /// <summary>
    /// Interaction logic for EarthView.xaml
    /// </summary>
    public partial class EarthView : Page
    {
        private string _openedFileName;

        private string _baseFolder;

        public EarthView()
        {
            InitializeComponent();

            _baseFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\HeightMaps\\");

            OpenHeightMapDataFile(_baseFolder + "europe_height.png");
            OpenHeightMapTextureFile(_baseFolder + "europe.png");

            this.Loaded += new RoutedEventHandler(HeightMapSample_Loaded);
        }

        void HeightMapSample_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLightDirection();
        }

        private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            HeightMap1.Size = new System.Windows.Media.Media3D.Size3D(HeightMap1.Size.X, HeightSlider.Value, HeightMap1.Size.Z);
        }

        private void DirectionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            UpdateLightDirection();
        }

        private void UpdateLightDirection()
        {
            RotateTransform3D rotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), DirectionSlider.Value));

            Vector3D direction = rotate.Transform(new Vector3D(0, -0.5, -1));

            DirectionalLight1.Direction = direction;
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            string fileName = ShowOpenFileDialog("Select texture file");

            if (fileName != null)
                OpenHeightMapDataFile(fileName);
        }

        private void OpenTextureFileButton_Click(object sender, RoutedEventArgs e)
        {
            string fileName = ShowOpenFileDialog("Select texture file");

            if (fileName != null)
                OpenHeightMapTextureFile(fileName);
        }

        private void ClearTextureButon_Click(object sender, RoutedEventArgs e)
        {
            OpenHeightMapTextureFile(null);
        }


        private string ShowOpenFileDialog(string title)
        {
            string openedFile;
            Microsoft.Win32.OpenFileDialog openFileDialog;

            openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.DefaultExt = "GIF";
            openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*";
            openFileDialog.Multiselect = false;
            openFileDialog.InitialDirectory = _baseFolder;
            openFileDialog.Title = title;

            if (openFileDialog.ShowDialog() ?? false)
            {
                _baseFolder = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
                openedFile = openFileDialog.FileName;
            }
            else
            {
                openedFile = null;
            }

            return openedFile;
        }

        private void OpenHeightMapTextureFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                HeightMap1.Material = new DiffuseMaterial(Brushes.Silver);

                TextureImage.Source = null;
            }
            else
            {
                BitmapImage texture = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute));
                ImageBrush imageBrush = new ImageBrush(texture);

                HeightMap1.Material = new DiffuseMaterial(imageBrush);

                TextureImage.Source = texture;
            }
        }

        private void OpenHeightMapDataFile(string fileName)
        {
            BitmapImage heightImage = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute));

            // Create height data from bitmap
            double[,] heightData = Ab3d.PowerToys.Samples.Objects3D.HeightMapSample.OpenHeightMapDataFile(heightImage, false); // false: invertData

            if (heightData != null)
            {
                HeightMap1.HeightData = heightData;

                HeightMapImage.Source = heightImage;

                _openedFileName = fileName;
            }
        }
    }
}
