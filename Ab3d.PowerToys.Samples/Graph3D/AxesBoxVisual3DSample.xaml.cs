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
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Graph3D
{
    /// <summary>
    /// Interaction logic for AxesBoxVisual3DSample.xaml
    /// </summary>
    public partial class AxesBoxVisual3DSample : Page
    {
        private Random _rnd = new Random();

        public AxesBoxVisual3DSample()
        {
            InitializeComponent();


            // Customize the shown axis to show the XY axes horizontally and Z axis up (by default WPF as Y axis up).
            AxisPanel.CustomizeAxes(new Vector3D(1, 0, 0), "X", Colors.Red,
                                    new Vector3D(0, 1, 0), "Z", Colors.Blue,
                                    new Vector3D(0, 0, -1), "Y", Colors.Green);

            AxisShowingStrategyComboBox.ItemsSource = Enum.GetValues(typeof(AxesBoxVisual3D.AxisShowingStrategies));
            AxisShowingStrategyComboBox.SelectedIndex = 1;


            // Axes colors and line thicknesses are set in XAML

            // Set axes data ranges:
            AxesBox.SetAxisDataRange(AxesBoxVisual3D.AxisTypes.XAxis, minimumValue: 0, maximumValue: 100, majorTicksStep: 10, minorTicksStep: 5, snapMaximumValueToMajorTicks: true);
            AxesBox.SetAxisDataRange(AxesBoxVisual3D.AxisTypes.YAxis, minimumValue: 0, maximumValue: 100, majorTicksStep: 10, minorTicksStep: 5, snapMaximumValueToMajorTicks: true);
            AxesBox.SetAxisDataRange(AxesBoxVisual3D.AxisTypes.ZAxis, minimumValue: -20, maximumValue: 200, majorTicksStep: 20, minorTicksStep: 5, snapMaximumValueToMajorTicks: true);


            // Set axes names:
            AxesBox.XAxis1.AxisTitle = "XAxis1";
            AxesBox.XAxis2.AxisTitle = null;

            AxesBox.YAxis1.AxisTitle = "YAxis1";
            AxesBox.YAxis2.AxisTitle = null;

            AxesBox.ZAxis1.AxisTitle = "ZAxis1";
            AxesBox.ZAxis2.AxisTitle = null;

            AxesBox.OverlayCanvas = AxisOverlayCanvas;

            this.Unloaded += delegate(object sender, RoutedEventArgs args)
            {
                // It is recommended to call Dispose method when the AxesBox is no longer used.
                // This releases the native memory that is used by the RenderTargetBitmap objects that can be created by the TextBlockVisual3D object.
                AxesBox.Dispose();
            };
        }

        private void OnAxesVisibilityChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            AxesBox.IsXAxis1Visible = ShowXAxis1CheckBox.IsChecked ?? false;
            AxesBox.IsXAxis2Visible = ShowXAxis2CheckBox.IsChecked ?? false;

            AxesBox.IsYAxis1Visible = ShowYAxis1CheckBox.IsChecked ?? false;
            AxesBox.IsYAxis2Visible = ShowYAxis2CheckBox.IsChecked ?? false;

            if ((ReferenceEquals(sender, ShowZAxis1CheckBox) || 
                 Equals(sender, ShowZAxis2CheckBox)) &&
                (AxesBox.AxisShowingStrategy == AxesBoxVisual3D.AxisShowingStrategies.LeftmostAxis ||
                 AxesBox.AxisShowingStrategy == AxesBoxVisual3D.AxisShowingStrategies.RightmostAxis))
            {
                MessageBox.Show("When using LeftmostAxis or RightmostAxis, then we cannot change visibility of ZAxes with IsZAxis1Visible or IsZAxis2Visible properties.");
            }
            else
            {
                AxesBox.IsZAxis1Visible = ShowZAxis1CheckBox.IsChecked ?? false;
                AxesBox.IsZAxis2Visible = ShowZAxis2CheckBox.IsChecked ?? false;
            }

            // we could also change the visibility directly on an axis object - for example:
            //AxesBox.ZAxis2.IsVisible = ShowZAxis2CheckBox.IsChecked ?? false;
        }

        private void CustomizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            AxesBox.AxisTitleBrush = GetRandomBrush();
            AxesBox.AxisTitleFontSize = _rnd.Next(5) + 5;

            if (!AxesBox.Is3DTextShown)
                AxesBox.AxisTitleFontSize *= 2; // Increase size of title when the size is specified in 2D coordinates


            AxesBox.AxisLineColor = GetRandomColor();
            AxesBox.AxisLineThickness = _rnd.Next(3) + 0.5;

            AxesBox.TicksLineColor = GetRandomColor();
            AxesBox.TicksLineThickness = _rnd.Next(3) + 0.5;

            AxesBox.ConnectionLinesColor = GetRandomColor();
            AxesBox.ConnectionLinesThickness = _rnd.Next(3) + 0.5;

            AxesBox.MinorTicksLength = _rnd.Next(3) + 2;
            AxesBox.MajorTicksLength = AxesBox.MinorTicksLength * 2;

            AxesBox.SetAxisDataRange(AxesBoxVisual3D.AxisTypes.XAxis, _rnd.Next(50), _rnd.Next(50) + 60, 10, 5, true);
            AxesBox.SetAxisDataRange(AxesBoxVisual3D.AxisTypes.YAxis, _rnd.Next(50), _rnd.Next(50) + 60, 10, 5, true);
            AxesBox.SetAxisDataRange(AxesBoxVisual3D.AxisTypes.ZAxis, _rnd.Next(50), _rnd.Next(50) + 60, 10, 5, true);
        }

        private Color GetRandomColor()
        {
            return Color.FromRgb((byte) _rnd.Next(255), (byte) _rnd.Next(255), (byte) _rnd.Next(255));
        }
        
        private Brush GetRandomBrush()
        {
            return new SolidColorBrush(GetRandomColor());
        }
    }
}
