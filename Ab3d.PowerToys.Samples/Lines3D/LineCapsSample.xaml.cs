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
using System.Windows.Shapes;
using Ab3d.Common;
using Ab3d.Common.Models;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    /// <summary>
    /// Interaction logic for LineCapsSample.xaml
    /// </summary>
    public partial class LineCapsSample : Page
    {
        private readonly Vector3D[] _multiLinePositions = new Vector3D[]
        {
            new Vector3D(10, 0, 0),
            new Vector3D(10, 60, 0),
            new Vector3D(30, 20, 0),
            new Vector3D(30, 80, 0),
        };

        private readonly Vector3D[] _polyLinePositions = new Vector3D[]
        {
            new Vector3D(10, 0, 0),
            new Vector3D(10, 60, 0),
            new Vector3D(30, 20, 0),
            new Vector3D(30, 80, 0),
        };

        private double _selectedLineThickness = 2;

        private bool _isRandomized;


        public LineCapsSample()
        {
            InitializeComponent();

            ArrowLengthInfoControl.InfoText =
@"Specifies the maximum arrow length set as fraction of the line length.
For example: 0.2 means that the maximum arrow length will be 1 / 5 (=0.2) of the line length.
If the line is short so that the arrow length exceeds the amount defined by MaxLineArrowLength,
the arrow is shortened (the arrow angle is increased).";

            CreateSampleLines();
        }

        private void CreateSampleLines()
        {
            MainViewport.Children.Clear();


            var lineCapNames = Enum.GetNames(typeof(LineCap));

            double lineOffset = 20;
            double startX = -lineCapNames.Length * lineOffset * 4 / 2;

            var position = new Point3D(startX, -100, 0);
            var lineVector = new Vector3D(0, 100, 0);
            var lineOffsetVector = new Vector3D(20, 0, 0);

            for (var i = 0; i < lineCapNames.Length; i++)
            {
                var lineCapName = lineCapNames[i];
                var lineCapValue = (LineCap)Enum.Parse(typeof(LineCap), lineCapName);


                string displayText = lineCapName.Replace("Anchor", "");

                double isOffset = (i % 2) == 0 ? 0 : 1;

                var textBlockVisual3D = new TextBlockVisual3D()
                {
                    Position = position + lineOffsetVector + new Vector3D(0, isOffset * -20, 40 + isOffset * 20),
                    PositionType = PositionTypes.Center,
                    UpDirection = new Vector3D(0, 0.4, -1),
                    Size = new Size(0, 30),
                    TextPadding = new Thickness(5, 2, 5, 2),
                    Foreground = Brushes.Yellow,
                    Background = Brushes.Black,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    Text = displayText
                };

                MainViewport.Children.Add(textBlockVisual3D);


                var line = new LineVisual3D()
                {
                    StartPosition = position,
                    EndPosition = position + lineVector,
                    StartLineCap = lineCapValue,
                    LineColor = Colors.Silver,
                    LineThickness = _selectedLineThickness
                };

                MainViewport.Children.Add(line);


                position += lineOffsetVector;

                line = new LineVisual3D()
                {
                    StartPosition = position,
                    EndPosition = position + lineVector,
                    EndLineCap = lineCapValue,
                    LineColor = Colors.Silver,
                    LineThickness = _selectedLineThickness
                };

                MainViewport.Children.Add(line);


                position += lineOffsetVector;

                line = new LineVisual3D()
                {
                    StartPosition = position,
                    EndPosition = position + lineVector,
                    StartLineCap = lineCapValue,
                    EndLineCap = lineCapValue,
                    LineColor = Colors.Silver,
                    LineThickness = _selectedLineThickness
                };

                MainViewport.Children.Add(line);

                position += lineOffsetVector;
                position += lineOffsetVector;


                var offset = new Vector3D(-4 * lineOffset, 120, 0);

                if (ShowMultiLinesCheckBox.IsChecked ?? false)
                {
                    var multiLineVisual3D = new MultiLineVisual3D()
                    {
                        Positions = new Point3DCollection(_multiLinePositions.Select(p => position + p + new Vector3D(-4 * lineOffset, 120, 0)).ToArray()),
                        LineColor = Colors.Yellow,
                        LineThickness = _selectedLineThickness,
                        StartLineCap = lineCapValue,
                        EndLineCap = lineCapValue,
                    };

                    MainViewport.Children.Add(multiLineVisual3D);

                    offset += new Vector3D(0, 100, 0);
                }


                if (ShowPolyLinesCheckBox.IsChecked ?? false)
                {
                    var polyLineVisual3D = new PolyLineVisual3D()
                    {
                        Positions = new Point3DCollection(_polyLinePositions.Select(p => position + p + offset).ToArray()),
                        LineColor = Colors.Orange,
                        LineThickness = _selectedLineThickness,
                        StartLineCap = lineCapValue,
                        EndLineCap = lineCapValue,
                    };

                    MainViewport.Children.Add(polyLineVisual3D);


                    //var lineArcVisual3D = new LineArcVisual3D()
                    //{
                    //    CircleCenterPosition = position + offset + new Vector3D(40, 130, 0),
                    //    Radius = 30,
                    //    StartAngle = 0,
                    //    EndAngle = 90,
                    //    CircleNormal = new Vector3D(0, 0, 1),
                    //    ZeroAngleDirection = new Vector3D(0, -1, 0),
                    //    LineColor = Colors.Red,
                    //    LineThickness = _selectedLineThickness,
                    //    StartLineCap = lineCapValue,
                    //    EndLineCap = lineCapValue,
                    //};

                    //MainViewport.Children.Add(lineArcVisual3D);
                }
            }

            if (_isRandomized)
                RandomizeLineCaps();
        }
        
        private void RandomizeLineCaps()
        {
            var rnd = new Random();

            int maxValue = Enum.GetValues(typeof(LineCap)).Length;

            foreach (var lineCapVisual3D in MainViewport.Children.OfType<ILineCapVisual3D>())
            {
                lineCapVisual3D.StartLineCap = (LineCap)rnd.Next(maxValue);
                lineCapVisual3D.EndLineCap = (LineCap)rnd.Next(maxValue);
            }

            // Hide TextBlockVisual3D as they are not valid anymore
            foreach (var textBlockVisual3D in MainViewport.Children.OfType<TextBlockVisual3D>())
                textBlockVisual3D.IsVisible = false;
        }


        private void AngleComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var comboBoxItem = AngleComboBox.SelectedItem as ComboBoxItem;

            if (comboBoxItem != null)
            {
                LinesUpdater.Instance.LineArrowAngle = double.Parse((string)comboBoxItem.Content, System.Globalization.CultureInfo.InvariantCulture);
                LinesUpdater.Instance.Refresh();
            }
        }

        private void LengthComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var comboBoxItem = LengthComboBox.SelectedItem as ComboBoxItem;

            if (comboBoxItem != null)
            {
                LinesUpdater.Instance.MaxLineArrowLength = double.Parse((string)comboBoxItem.Content, System.Globalization.CultureInfo.InvariantCulture);
                LinesUpdater.Instance.Refresh();
            }
        }

        private void OnLineSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            CreateSampleLines();
        }

        private void LineThicknessComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var comboBoxItem = LineThicknessComboBox.SelectedItem as ComboBoxItem;

            if (comboBoxItem != null)
            {
                _selectedLineThickness = double.Parse((string)comboBoxItem.Content, System.Globalization.CultureInfo.InvariantCulture);
                CreateSampleLines();
            }
        }

        private void RandomizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            RandomizeLineCaps();
            _isRandomized = true;
        }
    }
}
