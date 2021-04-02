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
using System.Windows.Threading;
using Ab3d.Cameras;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Graph3D
{
    /// <summary>
    /// Interaction logic for ContourLinesSample.xaml
    /// </summary>
    public partial class ContourLinesSample : Page
    {
        private string _heightMapFileName;

        private double[] _contourLineValues;

        private Point3DCollection[] _individualContourLinePositions;
        private List<MultiLineVisual3D> _individualContourLines;

        private AmbientLight _ambientLight;

        public ContourLinesSample()
        {
            InitializeComponent();

            _contourLineValues = SetupContourLineValues(minValue: 0, maxValue: 1, step: 0.05);

            _heightMapFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\HeightMaps\\simpleHeightMap.png");

            _ambientLight = new AmbientLight(Colors.Black);
            MainViewport.Children.Add(_ambientLight.CreateModelVisual3D());

            OpenHeightMapDataFile(_heightMapFileName);

            AxesBox.SetAxisDataRange(AxesBoxVisual3D.AxisTypes.ZAxis, minimumValue: 0, maximumValue: 1, majorTicksStep: 0.25, minorTicksStep: 0, snapMaximumValueToMajorTicks: true);

            CreateContourLines();

            ResetCamera(isTopDownOrthographicCamera: false);
        }

        private double[] SetupContourLineValues(double minValue, double maxValue, double step)
        {
            if (maxValue < minValue || step < 0)
                return null;

            int count = (int)((maxValue - minValue) / step);
            var contourLineValues = new List<double>(count + 1);

            double oneValue = minValue;
            while (oneValue <= maxValue)
            {
                contourLineValues.Add(oneValue);
                oneValue += step;
            }

            return contourLineValues.ToArray();
        }

        private void CreateContourLines()
        {
            ContourLinesRootVisual3D.Children.Clear();

            // NOTE:
            // Here we use CreateContourLinePositions and CreateMultiContourLinePositions methods on the HeightMapVisual3D class.
            // Those methods use the MeshGeometry3D of the height map and also scale the contour lines by the size of the HeightMapVisual3D.
            //
            // If you want to create contour lines based on some other geometry, then use the Ab3d.Utilities.ContourLinesFactory class (internally used by HeightMapVisual3D).

            if (CombineAllContourLinesRadioButton.IsChecked ?? false)
            {
                // CreateContourLinePositions creates positions for all contour lines and returns them in one Point3DCollection.
                var contourLinePositions = HeightMap1.CreateContourLinePositions(_contourLineValues, yOffset: 0.05); // offset the contour lines so they are drawn just slightly on top of the 3D height map

                var multiLineVisual3D = new MultiLineVisual3D()
                {
                    Positions = contourLinePositions,
                    LineColor = Colors.Black,
                    LineThickness = 1
                };

                ContourLinesRootVisual3D.Children.Add(multiLineVisual3D);

                _individualContourLinePositions = null;
                _individualContourLines = null;
            }
            else
            {
                _individualContourLines = new List<MultiLineVisual3D>(_contourLineValues.Length);

                // CreateContourLinePositions creates positions for individual contour lines and returns them in an array of Point3DCollection
                // (each element in the array represents positions for contour lines each value in _contourLineValues).
                _individualContourLinePositions = HeightMap1.CreateMultiContourLinePositions(_contourLineValues, yOffset: 0.05); // offset the contour lines so they are drawn just slightly on top of the 3D height map

                for (var i = 0; i < _individualContourLinePositions.Length; i++)
                {
                    var multiLineVisual3D = new MultiLineVisual3D()
                    {
                        Positions = _individualContourLinePositions[i],
                    };

                    // Set line thickness (each 5th line is thicker)
                    if ((i % 5) == 0)
                        multiLineVisual3D.LineThickness = 2;
                    else
                        multiLineVisual3D.LineThickness = 0.7;

                    // Set line color
                    if (ColoredContourLinesRadioButton.IsChecked ?? false)
                    {
                        var color = HeightMap1.GetHeightTextureColor(_contourLineValues[i]);
                        multiLineVisual3D.LineColor = color;
                    }
                    else
                    {
                        multiLineVisual3D.LineColor = Colors.Black;
                    }


                    ContourLinesRootVisual3D.Children.Add(multiLineVisual3D);

                    _individualContourLines.Add(multiLineVisual3D);
                }
            }
        }

        private void OpenHeightMapDataFile(string fileName)
        {
            BitmapImage heightImage = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute));

            // Create height data from bitmap
            // Returns value in range from 0 to 1
            double[,] heightData = Ab3d.PowerToys.Samples.Objects3D.HeightMapSample.OpenHeightMapDataFile(heightImage, invertData: false);

            //heightData = CreateRandomData(10, 10);

            if (heightData != null)
            {
                HeightMap1.HeightData = heightData;

                bool useGeographicalColors = GeographicalSmoothColorsRadioButton.IsChecked ?? false;
                SetGradientMaterial(useGeographicalColors);
            }
        }

        private double[,] CreateRandomData(int xCount, int yCount)
        {
            double[,] heightData = new double[xCount, yCount];
            var rnd = new Random();

            for (int x = 0; x < xCount; x++)
            {
                for (int y = 0; y < yCount; y++)
                {
                    double v = rnd.Next(20) / 20.0;
                    heightData[x, y] = v;
                }
            }

            return heightData;
        }

        private void SetGradientMaterial(bool useGeographicalColors)
        {
            GradientStopCollection stops = new GradientStopCollection();

            if (GeographicalSmoothColorsRadioButton.IsChecked ?? false)
            {
                stops.Add(new GradientStop(Colors.White, 1));
                stops.Add(new GradientStop(Colors.Gray, 0.8));
                stops.Add(new GradientStop(Colors.SandyBrown, 0.6));
                stops.Add(new GradientStop(Colors.LightGreen, 0.4));
                stops.Add(new GradientStop(Colors.Aqua, 0.2));
                stops.Add(new GradientStop(Colors.Blue, 0));
            }
            else if (GeographicalHardColorsRadioButton.IsChecked ?? false)
            {
                // The gradient with hard transition is defined by making the transition from one color to another very small (for example from 0.799 to 0.8)
                stops.Add(new GradientStop(Colors.White, 1));
                stops.Add(new GradientStop(Colors.White, 0.8));
                stops.Add(new GradientStop(Colors.SandyBrown, 0.799));
                stops.Add(new GradientStop(Colors.SandyBrown, 0.6));
                stops.Add(new GradientStop(Colors.LightGreen, 0.599));
                stops.Add(new GradientStop(Colors.LightGreen, 0.400));
                stops.Add(new GradientStop(Colors.Aqua, 0.399));
                stops.Add(new GradientStop(Colors.Aqua, 0.2));
                stops.Add(new GradientStop(Colors.Blue, 0.199));
                stops.Add(new GradientStop(Colors.Blue, 0));
            }
            else
            {
                stops.Add(new GradientStop(Colors.Red, 1));
                stops.Add(new GradientStop(Colors.Yellow, 0.75));
                stops.Add(new GradientStop(Colors.LightGreen, 0.5));
                stops.Add(new GradientStop(Colors.Aqua, 0.25));
                stops.Add(new GradientStop(Colors.Blue, 0));
            }

            // NOTE: We do not have to specify the StartPoint and EndPoint
            // It will be used in the CreateHeightTextureFromGradient method 
            // to create the texture from the actual height map data and with the specified LinearGradientBrush
            LinearGradientBrush gradient = new LinearGradientBrush(stops);

            // When using gradient texture, we get better results when UseHeightValuesAsTextureCoordinates is true.
            // In this case height values are used for texture coordinates - texture coordinate (0, 0.5) is set the minimum height value and texture coordinate (1, 0.5) is set to the maximum height value.
            // This requires a one dimensional gradient texture and usually produces more accurate results than when UseHeightValuesAsTextureCoordinates is false.
            // This should not be used for cases when a bitmap is shown on the height map.
            // See HeightMapSample for more info.
            // Set this value to false to see the difference.
            HeightMap1.UseHeightValuesAsTextureCoordinates = true;

            HeightMap1.CreateHeightTextureFromGradient(gradient);


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

        private void TopDownCameraButton_OnClick(object sender, RoutedEventArgs e)
        {
            ResetCamera(isTopDownOrthographicCamera: true);
        }

        private void SideCameraButton_OnClick(object sender, RoutedEventArgs e)
        {
            ResetCamera(isTopDownOrthographicCamera: false);
        }

        private void ResetCamera(bool isTopDownOrthographicCamera)
        {
            if (isTopDownOrthographicCamera)
            {
                Camera1.CameraType = BaseCamera.CameraTypes.OrthographicCamera;
                Camera1.Heading = 0;
                Camera1.Attitude = -90;
            }
            else
            {
                Camera1.CameraType = BaseCamera.CameraTypes.PerspectiveCamera;
                Camera1.Heading = 30;
                Camera1.Attitude = -20;
            }


            Camera1.Offset = new Vector3D(0, 0, 0);
            Camera1.Distance = 260;
            Camera1.CameraWidth = 260;

            // Offset TargetPosition to the right to show the height map more on the left side of the screen so the settings menu does not occlude the 3D scene 
            Camera1.TargetPosition = new Point3D(20, 15, 0);
            Camera1.RotationCenterPosition = new Point3D(0, 0, 0);
        }

        private void OnContourLinesTypeChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            //_contourLineValues = SetupContourLineValues(minValue: 0.1, maxValue: 0.9, step: 0.1);
            _contourLineValues = SetupContourLineValues(minValue: 0, maxValue: 1, step: 0.05);

            CreateContourLines();

            if (ColoredContourLinesRadioButton.IsChecked ?? false)
            {
                ShowSolidSurfaceCheckBox.IsChecked = false;
            }
            else
            {
                ShowSolidSurfaceCheckBox.IsChecked = true;
            }
        }

        private void OnGradientTypeChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            // Recreate the height map and contour lines again
            OpenHeightMapDataFile(_heightMapFileName);
            CreateContourLines();
        }

        private void AmbientSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            byte c = (byte) AmbientSlider.Value;
            _ambientLight.Color = Color.FromRgb(c, c, c);
        }
    }
}
