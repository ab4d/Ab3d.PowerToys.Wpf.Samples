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
    /// <summary>
    /// Interaction logic for Plot3DSample.xaml
    /// </summary>
    public partial class Plot3DSample : Page
    {
        private double _minYValue, _maxYValue;

        private double[,] _data;

        public Plot3DSample()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Plot3DSample_Loaded);
        }

        #region Event handlers
        void Plot3DSample_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAll();
        }

        private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            UpdateSize();

            UpdateWireBox();
        }

        private void GradientRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateGradientTexture();
        }

        private void Rectangle1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Gradient1RadioButton.IsChecked = true;
        }

        private void Rectangle2_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Gradient2RadioButton.IsChecked = true;
        }

        private void Rectangle3_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Gradient3RadioButton.IsChecked = true;
        }

        private void FunctionRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateAll();
        }

        private void ArraySizeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            int arraySize = GetSelectedArraySize();
            BottomWireGrid.WidthCellsCount = arraySize;
            BottomWireGrid.HeightCellsCount = arraySize;

            UpdateAll();
        }

        private void AxisCountRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateAll();     
        }

        private void ExportToImageButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog fileDialog;

            fileDialog = new Microsoft.Win32.SaveFileDialog();
            fileDialog.DefaultExt = "png";
            fileDialog.Filter = "Png files(*.png)|*.png";
            fileDialog.Title = "Select output png file";
            fileDialog.FileName = "Plot3D.png";

            if (fileDialog.ShowDialog() ?? false)
            {
                BitmapSource plotImage = Camera1.RenderToBitmap(backgroundBrush: MainGrid.Background); // We are calling RenderToBitmap, but it is possible to specify many parameters - background brush, dpi, custom image size and antialiasing level

                if (plotImage != null)
                {
                    using (var fileStream = new System.IO.FileStream(fileDialog.FileName, System.IO.FileMode.Create))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(plotImage));
                        encoder.Save(fileStream);
                    }
                }
            }
        }
        #endregion

        private void UpdateAll()
        {
            int arraySize = GetSelectedArraySize();
            
            if (Function1RadioButton.IsChecked ?? false)
                _data = CreateGraphData1(arraySize, arraySize, out _minYValue, out _maxYValue);
            else if (Function2RadioButton.IsChecked ?? false)
                _data = CreateGraphData2(arraySize, arraySize, out _minYValue, out _maxYValue);
            else if (Function3RadioButton.IsChecked ?? false)
                _data = CreateGraphData3(arraySize, arraySize, out _minYValue, out _maxYValue);
            else if (Function4RadioButton.IsChecked ?? false)
                _data = CreateGraphData4(arraySize, arraySize, out _minYValue, out _maxYValue);

            HeightMap1.HeightData = _data;

            UpdateGradientTexture();

            UpdateSize();

            UpdateWireBox();
        }

        private void UpdateWireBox()
        {
            // Get local values
            Size3D heightMapSize = HeightMap1.Size;
            
            // First we have to get the position and size of the WireBoxVisual3
            // It is not the same as the HeightMapVisual3D because we start and stop the axis on the whole part of the _minYValue and _maxYValue
            // For example if the function's min is 0.8 and its max is 0.9 we would show axis from -1 to +1.

            double axisMinY = Math.Floor(_minYValue);
            double axisMaxY = Math.Ceiling(_maxYValue);

            double dataValuesRange = _maxYValue - _minYValue;
            double axisValuesRange = axisMaxY - axisMinY;

            double heightMapYSize = heightMapSize.Y; // This is defined by the height slider

            // First get the center
            double centerY = ((_maxYValue + _minYValue) / 2) * heightMapYSize;

            // Now calculate for how much we extend the y size to go from _minYValue to axisMinY and from _maxYValue to axisMaxY

            double axisSizeY;

            if (dataValuesRange > 0)
            {
                axisSizeY = heightMapYSize * dataValuesRange +
                            (heightMapYSize * Math.Abs(_minYValue - axisMinY)) + // extent for the axisMinY - _minYValue
                            (heightMapYSize * Math.Abs(axisMaxY - _maxYValue)); // extent for the axisMaxY - _maxYValue
            }
            else
            {
                axisSizeY = 0;
            }

            int axisItemsCount = GetSelectedAxisValuesCount();
            double axisItemStep = 1 / (double)(axisItemsCount - 1);

            var axisValues = new List<double>(axisItemsCount);

            for (int i = 0; i < axisItemsCount; i++)
                axisValues.Add(axisMinY + i * axisItemStep * axisValuesRange);


            AxisWireBox.MinYValue = axisMinY;
            AxisWireBox.MaxYValue = axisMaxY;
            AxisWireBox.YAxisValues = axisValues;


            // Set the format string that defines how the axis values are shown
            // Note that is this value is set in XAML, you need to use the {} to be able to set curly brackets: AxisValuesStringFormat="{}{0:0.#}"
            AxisWireBox.AxisValuesStringFormat = GetAxisValuesStringFormat();

            // We can set the values now
            AxisWireBox.CenterPosition = new Point3D(0, centerY, 0);
            AxisWireBox.Size = new Size3D(100, axisSizeY, 100);

            BottomWireGridTransform.OffsetY = centerY - axisSizeY / 2;
        }

        private void UpdateGradientTexture()
        {
            Rectangle selectedRectangle;

            if (Gradient1RadioButton.IsChecked ?? false)
                selectedRectangle = Rectangle1;
            else if (Gradient2RadioButton.IsChecked ?? false)
                selectedRectangle = Rectangle2;
            else // if (Gradient1RadioButton.IsChecked ?? false)
                selectedRectangle = Rectangle3;


            // The following call will create the magic:
            // It will use the height data and the LinearGradientBrush on the selected rectangle
            // and create the texture bitmap from the data
            HeightMap1.CreateHeightTextureFromGradient((LinearGradientBrush)selectedRectangle.Fill);
            
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

        private void UpdateSize()
        {
            HeightMap1.Size = new System.Windows.Media.Media3D.Size3D(HeightMap1.Size.X, HeightSlider.Value, HeightMap1.Size.Z);

            AxisWireBox.CenterPosition = new System.Windows.Media.Media3D.Point3D(AxisWireBox.CenterPosition.X, (_maxYValue + _minYValue) * HeightSlider.Value / 2, AxisWireBox.CenterPosition.Z);
            AxisWireBox.Size = new System.Windows.Media.Media3D.Size3D(AxisWireBox.Size.X, (_maxYValue - _minYValue) * HeightSlider.Value, AxisWireBox.Size.Z);
        }

        private int GetSelectedArraySize()
        {
            int arraySize;

            if (ArraySize1RadioButton.IsChecked ?? false)
                arraySize = 20;
            else if (ArraySize2RadioButton.IsChecked ?? false)
                arraySize = 40;
            else // (ArraySize3RadioButton.IsChecked ?? false)
                arraySize = 80;

            return arraySize;
        }

        private int GetSelectedAxisValuesCount()
        {
            int count;

            if (Axis5RadioButton.IsChecked ?? false)
                count = 5;
            else if (Axis7RadioButton.IsChecked ?? false)
                count = 7;
            else // (Axis9RadioButton.IsChecked ?? false)
                count = 9;

            return count;
        }

        private string GetAxisValuesStringFormat()
        {
            string axisValuesStringFormat;

            if (Axis5RadioButton.IsChecked ?? false)
                axisValuesStringFormat = "{0:0.#}"; // Default value is "{0:0}" - no decimals
            else if (Axis7RadioButton.IsChecked ?? false)
                axisValuesStringFormat = "{0:0.#}"; // Show one decimal if the number is not a whole number
            else // (Axis9RadioButton.IsChecked ?? false)
                axisValuesStringFormat = "{0:0.00}"; // Always show 2 decimals

            return axisValuesStringFormat;
        }

        private void ChangeDataButton_OnClick(object sender, RoutedEventArgs e)
        {
            // This method demonstrates how to update the graph 3D mesh after the data are changed
            // Similar code can be used to create dynamic height graphs.
            // But be careful about performance especially when showing 3D lines in height map.
            // Much better performance can be achieved when 3D lines are not shown.

            int arraySize = GetSelectedArraySize();

            double[,] heightData = HeightMap1.HeightData;

            for (int z = 0; z < arraySize; z++)
            {
                for (int x = 0; x < arraySize; x++)
                {
                    heightData[z, x] *= 0.95;
                }
            }

            // UpdateContent will recreate the 3D mesh
            HeightMap1.UpdateContent();
        }

        #region Math functions

        // cos(x*z)*(x^2-z^2)/2
        private static double[,] CreateGraphData1(int arrayWidth, int arrayHeight, out double minYValue, out double maxYValue)
        {
            double[,] data = new double[arrayWidth, arrayHeight];


            double xMin = -2;
            double xMax = 2;
            double zMin = -2;
            double zMax = 2;

            double xStep = (xMax - xMin) / arrayWidth;
            double zStep = (zMax - zMin) / arrayHeight;

            double xValue;
            double zValue = zMin;


            double yValue;
            minYValue = double.MaxValue;
            maxYValue = double.MinValue;


            for (int z = 0; z < arrayHeight; z++)
            {
                xValue = xMin;

                for (int x = 0; x < arrayWidth; x++)
                {
                    // cos(x*y)*(x^2-y^2)
                    yValue = Math.Cos(xValue * zValue) * (xValue * xValue - zValue * zValue) / 2;

                    data[x, z] = yValue;

                    if (yValue > maxYValue)
                        maxYValue = yValue;

                    if (yValue < minYValue)
                        minYValue = yValue;

                    xValue += xStep;
                }

                zValue += zStep;
            }

            return data;
        }

        // sin(sqrt(x*x + z*z))
        private static double[,] CreateGraphData2(int arrayWidth, int arrayHeight, out double minYValue, out double maxYValue)
        {
            double[,] data = new double[arrayWidth, arrayHeight];


            double xMin = -10;
            double xMax = 10;
            double zMin = -10;
            double zMax = 10;

            double xStep = (xMax - xMin) / arrayWidth;
            double zStep = (zMax - zMin) / arrayHeight;

            double xValue;
            double zValue = zMin;


            double yValue;
            minYValue = double.MaxValue;
            maxYValue = double.MinValue;


            for (int z = 0; z < arrayHeight; z++)
            {
                xValue = xMin;

                for (int x = 0; x < arrayWidth; x++)
                {
                    yValue = Math.Sin(Math.Sqrt(xValue * xValue + zValue * zValue));

                    data[x, z] = yValue;

                    if (yValue > maxYValue)
                        maxYValue = yValue;

                    if (yValue < minYValue)
                        minYValue = yValue;

                    xValue += xStep;
                }

                zValue += zStep;
            }

            return data;
        }

        // (x * z^3 - z * x^3) * 2
        private static double[,] CreateGraphData3(int arrayWidth, int arrayHeight, out double minYValue, out double maxYValue)
        {
            double[,] data = new double[arrayWidth, arrayHeight];


            double xMin = -1;
            double xMax = 1;
            double zMin = -1;
            double zMax = 1;

            double xStep = (xMax - xMin) / arrayWidth;
            double zStep = (zMax - zMin) / arrayHeight;

            double xValue;
            double zValue = zMin;


            double yValue;
            minYValue = double.MaxValue;
            maxYValue = double.MinValue;


            for (int z = 0; z < arrayHeight; z++)
            {
                xValue = xMin;

                for (int x = 0; x < arrayWidth; x++)    
                {
                    yValue = (xValue * zValue * zValue * zValue - zValue * xValue * xValue * xValue) * 2;

                    data[x, z] = yValue;

                    if (yValue > maxYValue)
                        maxYValue = yValue;

                    if (yValue < minYValue)
                        minYValue = yValue;

                    xValue += xStep;
                }

                zValue += zStep;
            }

            return data;
        }

        // cos(abs(x)+abs(z))*(abs(x)+abs(z))
        private static double[,] CreateGraphData4(int arrayWidth, int arrayHeight, out double minYValue, out double maxYValue)
        {
            double[,] data = new double[arrayWidth, arrayHeight];


            double xMin = -1;
            double xMax = 1;
            double zMin = -1;
            double zMax = 1;

            double xStep = (xMax - xMin) / arrayWidth;
            double zStep = (zMax - zMin) / arrayHeight;

            double xValue;
            double zValue = zMin;


            double yValue;
            minYValue = double.MaxValue;
            maxYValue = double.MinValue;


            for (int z = 0; z < arrayHeight; z++)
            {
                xValue = xMin;

                for (int x = 0; x < arrayWidth; x++)
                {
                    yValue = Math.Cos(Math.Abs(xValue) + Math.Abs(zValue)) * (Math.Abs(xValue) + Math.Abs(zValue));

                    data[x, z] = yValue;

                    if (yValue > maxYValue)
                        maxYValue = yValue;

                    if (yValue < minYValue)
                        minYValue = yValue;

                    xValue += xStep;
                }

                zValue += zStep;
            }

            return data;
        }
        #endregion
    }
}
