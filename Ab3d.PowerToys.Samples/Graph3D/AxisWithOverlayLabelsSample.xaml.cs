using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Graph3D
{
    /// <summary>
    /// Interaction logic for AxisWithOverlayLabelsSample.xaml
    /// </summary>
    public partial class AxisWithOverlayLabelsSample : Page
    {
        public AxisWithOverlayLabelsSample()
        {
            InitializeComponent();

            ShowAllAxes();
        }

        private void ShowAllAxes()
        {
            MainViewport.Children.Clear();

            // Add z axis
            var zAxis = new AxisWithOverlayLabelsVisual3D();
            zAxis.BeginInit(); // It is faster to initialize TextBlockVisual3D with using BeginInit / EndInit - this way the inner objects are not updated on each set property but only once when calling EndInit
            zAxis.Camera = Camera1;
            zAxis.OverlayCanvas = AxisOverlayCanvas;
            zAxis.AxisStartPosition = new Point3D(-50, 0, -50);
            zAxis.AxisEndPosition = new Point3D(-50, 100, -50);
            zAxis.MajorTicksStep = 20;
            zAxis.MinorTicksStep = 10;
            zAxis.IsRenderingOnRightSideOfAxis = false;
            zAxis.AxisTitle = "Z axis [°C]";
            zAxis.AxisTitleBrush = Brushes.Orange;
            zAxis.AxisTitleFontWeight = FontWeights.Bold;
            zAxis.FontFamily = new FontFamily("Consolas");
            zAxis.AxisTitlePadding = -15; // By default the title is positioned for AxisTitlePadding (=5 by default) away from the longest data label. In out case the "100 (max)" label (defined below) is very long, so we need to move the title inwards so it is not to far away. 
            zAxis.EndInit();

            // Customize the value labels:
            var valueLabels = zAxis.GetValueLabels();         // first get default values
            valueLabels[0] = "0 (min)";                       
            valueLabels[valueLabels.Length - 1] += " (max)";  // add custom text to existing label; Note that this will move the axis title to the left (max value label length is used to offset the position of the title)
            zAxis.SetCustomValueLabels(valueLabels);

            MainViewport.Children.Add(zAxis);


            // Clone the axis
            var zAxis2 = zAxis.Clone();
            zAxis2.AxisStartPosition = new Point3D(50, 0, 50);
            zAxis2.AxisEndPosition = new Point3D(50, 100, 50);
            zAxis2.AxisTitle = null;
            zAxis2.IsRenderingOnRightSideOfAxis = !zAxis.IsRenderingOnRightSideOfAxis; // flip side on which the ticks and labels are rendered

            MainViewport.Children.Add(zAxis2);



            // Add x axis
            var xAxis = new AxisWithOverlayLabelsVisual3D();
            xAxis.BeginInit();
            xAxis.Camera = Camera1;
            xAxis.OverlayCanvas = AxisOverlayCanvas;
            xAxis.AxisStartPosition = new Point3D(-50, 0, 50);
            xAxis.AxisEndPosition = new Point3D(50, 0, 50);
            xAxis.RightDirectionVector3D = new Vector3D(0, 0, 1);
            xAxis.MinimumValue = -5;
            xAxis.MaximumValue = 5;
            xAxis.MajorTicksStep = 1;
            xAxis.MinorTicksStep = 0.25;
            xAxis.IsRenderingOnRightSideOfAxis = true;
            xAxis.AxisTitle = "X axis [m]";
            xAxis.EndInit();

            MainViewport.Children.Add(xAxis);


            // Clone the axis
            var xAxis2 = xAxis.Clone();
            xAxis2.AxisStartPosition = new Point3D(-50, 100, -50);
            xAxis2.AxisEndPosition = new Point3D(50, 100, -50);
            xAxis2.AxisTitle = null;
            xAxis2.IsRenderingOnRightSideOfAxis = !xAxis.IsRenderingOnRightSideOfAxis; // flip side on which the ticks and labels are rendered

            MainViewport.Children.Add(xAxis2);



            // Add z axis
            var yAxis = new AxisWithOverlayLabelsVisual3D();
            yAxis.BeginInit();
            yAxis.Camera = Camera1;
            yAxis.OverlayCanvas = AxisOverlayCanvas;
            yAxis.AxisStartPosition = new Point3D(-50, 0, 50);
            yAxis.AxisEndPosition = new Point3D(-50, 0, -50);
            yAxis.RightDirectionVector3D = new Vector3D(1, 0, 0);
            yAxis.MinimumValue = 0;
            yAxis.MaximumValue = 100;
            yAxis.IsRenderingOnRightSideOfAxis = false;
            yAxis.AxisTitle = "Y axis: log(10)";
            yAxis.EndInit();

            yAxis.SetCustomMajorTickValues(new double[] { 0.0, 33.3, 66.6, 100.0 });
            yAxis.SetCustomValueLabels(new string[] { "1", "10", "100", "1000" });

            // Set minor ticks to show log values from 1 to 10
            var minorValues = new List<double>();
            for (int i = 0; i <= 10; i++)
                minorValues.Add(Math.Log10(i) * 33.3); // multiply by 33.3 as this is the "position" of the value 10 on the axis (see code a few lines back)

            yAxis.SetCustomMinorTickValues(minorValues.ToArray());

            // To Hide the minor ticks we could set MinorTicksLength to 0 or call:
            //zAxis.SetCustomMinorTickValues(null); 

            MainViewport.Children.Add(yAxis);


            // Clone the axis
            var yAxis2 = yAxis.Clone();
            yAxis2.AxisStartPosition = new Point3D(50, 100, 50);
            yAxis2.AxisEndPosition = new Point3D(50, 100, -50);
            yAxis2.AxisTitle = null;
            yAxis2.IsRenderingOnRightSideOfAxis = !yAxis.IsRenderingOnRightSideOfAxis; // flip side on which the ticks and labels are rendered

            //// Remove value label "1" because it overlaps with top y axis
            //valueLabels = yAxis2.GetValueLabels();
            //valueLabels[0] = "";
            //yAxis2.SetCustomValueLabels(valueLabels);

            MainViewport.Children.Add(yAxis2);

            UpdateAdjustFirstAndLastLabelPosition();
        }

        private void OnIsRenderingTickLinesOnOverlayCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            var isRenderingTickLinesOnOverlay = IsRenderingTickLinesOnOverlayCheckBox.IsChecked ?? false;

            foreach (var axisWithOverlayLabelsVisual3D in MainViewport.Children.OfType<AxisWithOverlayLabelsVisual3D>())
            {
                axisWithOverlayLabelsVisual3D.IsRenderingTickLinesOnOverlay = isRenderingTickLinesOnOverlay;
                
                // We also need to adjust the lengths because the units in 3D and 2D are different.
                // We use twice as big values for 2D (the units in 3D as meant for axis length of 100 units).
                axisWithOverlayLabelsVisual3D.MajorTicksLength   = isRenderingTickLinesOnOverlay ? 10 : 5;
                axisWithOverlayLabelsVisual3D.MinorTicksLength   = isRenderingTickLinesOnOverlay ? 5  : 2.5;
                axisWithOverlayLabelsVisual3D.ValueLabelsPadding = isRenderingTickLinesOnOverlay ? 6  : 3;
            }
        }
        
        private void UpdateAdjustFirstAndLastLabelPosition()
        {
            foreach (var axisWith3DLabelsVisual3D in MainViewport.Children.OfType<AxisWithOverlayLabelsVisual3D>())
            {
                axisWith3DLabelsVisual3D.AdjustFirstLabelPosition = AdjustFirstLabelPositionCheckBox.IsChecked ?? false;
                axisWith3DLabelsVisual3D.AdjustLastLabelPosition = AdjustLastLabelPositionCheckBox.IsChecked ?? false;
            }
        }

        private void OnAdjustFirstOrLastLabelPositionCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateAdjustFirstAndLastLabelPosition();
        }
    }
}


