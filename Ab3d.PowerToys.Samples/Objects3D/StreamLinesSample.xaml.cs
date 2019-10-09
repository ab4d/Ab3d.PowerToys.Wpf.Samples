using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab3d.Common.Models;
using Ab3d.PowerToys.Samples.Common;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for StreamLinesSample.xaml
    /// </summary>
    public partial class StreamLinesSample : Page
    {
        public StreamLinesSample()
        {
            InitializeComponent();


            // We will set the tube path position color based on the Density
            string colorColumnName  = "Density"; // "AngularVelocity"
            bool   invertColorValue = false;     // This needs to be set to true when using AngularVelocity


            // First create a gradient legend and texture
            var gradientStopCollection = new GradientStopCollection();

            gradientStopCollection.Add(new GradientStop(Colors.Red,    1));
            gradientStopCollection.Add(new GradientStop(Colors.Yellow, 0.75));
            gradientStopCollection.Add(new GradientStop(Colors.Lime,   0.5));
            gradientStopCollection.Add(new GradientStop(Colors.Aqua,   0.25));
            gradientStopCollection.Add(new GradientStop(Colors.Blue,   0));

            var linearGradientBrush = new LinearGradientBrush(gradientStopCollection,
                                                              new System.Windows.Point(0, 1),  // startPoint (offset == 0) - note that y axis is down (so 1 is bottom)
                                                              new System.Windows.Point(0, 0)); // endPoint (offset == 1)

            // Create Legend control
            var gradientColorLegend = new GradientColorLegend()
            {
                Width  = 70,
                Height = 200,
                Margin = new Thickness(5, 5, 5, 5)
            };

            gradientColorLegend.GradientBrush = linearGradientBrush;
            
            var gradientTexture = gradientColorLegend.RenderToTexture(size: 256, isHorizontal: true);

            var imageBrush = new ImageBrush(gradientTexture);

            // IMPORTANT:
            // When texture coordinates have one components (for example y) always set to 0,
            // we need to change the ViewportUnits from the default RelativeToBoundingBox to Absolute.
            imageBrush.ViewportUnits = BrushMappingMode.Absolute; 

            var gradientMaterial = new DiffuseMaterial(imageBrush);


            // Now load sample data
            // Sample data was created by using ParaView application and exporting the streamlines into csv file.
            string sampleDataFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Streamlines.csv");


            // Create csv file reader that can read data from a csv file
            var csvDataReader = new CsvDataReader();
            csvDataReader.ReadFile(sampleDataFileName);


            float minValue, maxValue;
            csvDataReader.GetValuesRange(colorColumnName, out minValue, out maxValue);

            float dataRange = maxValue - minValue;


            var streamlineIndexes = csvDataReader.IndividualObjectIndexes;


            // Create the streamlines
            var allStreamlineBounds = new Rect3D();

            for (var i = 0; i < csvDataReader.IndividualObjectIndexes.Length - 1; i++)
            {
                Rect3D bounds;

                int startIndex = streamlineIndexes[i];
                int endIndex = streamlineIndexes[i + 1] - 1;

                int dataCount = endIndex - startIndex;

                if (dataCount < 2) // Skip streamlines without any positions or with less then 2 positions
                    continue;

                var positions = csvDataReader.GetPositions(startIndex, dataCount, out bounds);

                allStreamlineBounds.Union(bounds);

                var pathPositions = new Point3DCollection(positions);


                float[] dataValues = csvDataReader.GetValues(colorColumnName, startIndex, dataCount);

                // Generate texture coordinates for each path position
                // Because our texture is one dimensional gradient image (size 256 x 1)
                // we set the x coordinate in range from 0 to 1 (0 = first gradient color; 1 = last gradient color).
                // 
                // Note:
                // If we would only set x texture coordinate and preserve y at 0,
                // WPF would not render the texture because the y size would be 0 
                // and because by default the ViewportUnits is set to RelativeToBoundingBox,
                // WPF "thinks" the texture is empty. 
                // Therefore we need to set the imageBrush.ViewportUnits to Absolute.
                var positionsCount     = pathPositions.Count;
                var textureCoordinates = new PointCollection(positionsCount);
                for (int j = 0; j < dataCount; j++)
                {
                    float relativeDataValue = (dataValues[j] - minValue) / dataRange;

                    if (invertColorValue)
                        relativeDataValue = 1.0f - relativeDataValue;

                    textureCoordinates.Add(new Point(relativeDataValue, 0));
                }


                var tubePathMesh3D = new Ab3d.Meshes.TubePathMesh3D(
                    pathPositions: pathPositions,
                    pathPositionTextureCoordinates: textureCoordinates,
                    radius: 0.03,
                    isTubeClosed: true,
                    isPathClosed: false,
                    segments: 8);


                var geometryModel3D = new GeometryModel3D(tubePathMesh3D.Geometry, gradientMaterial);
                //var geometryModel3D = new GeometryModel3D(tubePathMesh3D.Geometry, new DiffuseMaterial(Brushes.Red));
                geometryModel3D.BackMaterial = new DiffuseMaterial(Brushes.DimGray);

                var modelVisual3D = new ModelVisual3D()
                {
                    Content = geometryModel3D
                };

                MainViewport.Children.Add(modelVisual3D);
            }

            Camera1.TargetPosition = allStreamlineBounds.GetCenterPosition();
            Camera1.Distance = allStreamlineBounds.GetDiagonalLength();


            // Add legend control:
            int legendValuesCount = 5;
            for (int i = 0; i < legendValuesCount; i++)
            {
                float t = (float) i / (float) (legendValuesCount - 1);
                float oneValue = minValue + t * (maxValue - minValue);

                string valueLegendText = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00}", oneValue);
                gradientColorLegend.LegendLabels.Add(new GradientColorLegend.LegendLabel(t, valueLegendText));
            }


            var legendTitleTextBlock = new TextBlock()
            {
                Text                = colorColumnName,
                FontSize            = 14,
                FontWeight          = FontWeights.Bold,
                Foreground          = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center
            };


            var stackPanel = new StackPanel()
            {
                Orientation         = Orientation.Vertical,
                VerticalAlignment   = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin              = new Thickness(0, 0, 5, 5)
            };

            stackPanel.Children.Add(legendTitleTextBlock);
            stackPanel.Children.Add(gradientColorLegend);

            RootGrid.Children.Add(stackPanel);
        }
    }
}
