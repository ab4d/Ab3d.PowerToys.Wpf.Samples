using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
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
using Ab3d.Common.EventManager3D;
using Ab3d.Common.Models;
using Ab3d.Visuals;
using Microsoft.Win32;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for DistanceMeasurement.xaml
    /// </summary>
    public partial class DistanceMeasurement : Page
    {
        private Ab3d.Utilities.EventManager3D _eventManager;

        private bool _isStartPositionSet;
        private bool _isEndPositionSet;

        private Point3D _startPosition, _endPosition;
        private bool _isTextPlaneNormalFlipped;

        private double _contentSize;

        private enum MeasureType
        {
            Distance,
            HorizontalDistance
        }

        public DistanceMeasurement()
        {
            InitializeComponent();

            this.Loaded += OnLoaded;
            MainCamera.CameraChanged += MainCameraChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            LoadSelectedScene();

            _eventManager = new Ab3d.Utilities.EventManager3D(MainViewport);

            // Exclude some Visuals from hit testing - without this we would not get MouseUp events
            _eventManager.RegisterExcludedVisual3D(StartLineVisual3D, EndLineVisual3D, DistanceLineVisual3D, TextPlaneVisual3D);

            var eventSource3D = new Ab3d.Utilities.VisualEventSource3D();
            eventSource3D.TargetVisual3D = ContentVisual;
            eventSource3D.MouseMove += EventSource3DOnMouseMove;
            eventSource3D.MouseUp += EventSource3DOnMouseUp;

            _eventManager.RegisterEventSource3D(eventSource3D);
        }
        
        private void SetContent(ModelVisual3D newContent, Size3D contentSize)
        {
            ContentVisual.Children.Clear();
            ContentVisual.Content = null;

            ContentVisual.Children.Add(newContent);

            if (newContent != null)
                _contentSize = Math.Sqrt(contentSize.X * contentSize.X + contentSize.Y * contentSize.Y + contentSize.Z + contentSize.Z);

            Reset();
        }
        
        private void SetContent(Model3D newContent)
        {
            ContentVisual.Children.Clear();
            ContentVisual.Content = newContent;

            if (newContent != null)
                _contentSize = Math.Sqrt(newContent.Bounds.X * newContent.Bounds.X + newContent.Bounds.Y * newContent.Bounds.Y + newContent.Bounds.Z + newContent.Bounds.Z);

            Reset();
        }

        private void LoadSelectedScene()
        {
            if (EuropeMapRadioButton.IsChecked ?? false)
                CreateEuropeHeightMap();
            else if (LandscapeRadioButton.IsChecked ?? false)
                CreateLandscapeModel();
            else if (File3dsRadioButton.IsChecked ?? false)
                Selected3dsFile();
        }

        private void CreateEuropeHeightMap()
        {
            var baseFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\HeightMaps\\");

            var texture = new BitmapImage(new Uri(baseFolder + "europe.png", UriKind.RelativeOrAbsolute));
            var imageBrush = new ImageBrush(texture);


            var heightImage = new BitmapImage(new Uri(baseFolder + "europe_height.png", UriKind.RelativeOrAbsolute));

            // Create height data from bitmap
            double[,] heightData = Ab3d.PowerToys.Samples.Objects3D.HeightMapSample.OpenHeightMapDataFile(heightImage, false); // false: invertData



            var heightMapVisual3D = new HeightMapVisual3D()
            {
                Size = new Size3D(3000, 200, 3500), // The Europe map is appropriately 3000 x 3500 km (200 km height is exaggerated)
                Material = new DiffuseMaterial(imageBrush),
                HeightData = heightData
            };

            SetContent(heightMapVisual3D, heightMapVisual3D.Size);

            MainCamera.Heading = 30;
            MainCamera.Distance = _contentSize * 1.2;
            MainCamera.CameraWidth = _contentSize * 1.2; // In case we have OrthographicCamera we also set the CameraWidth
        }

        private void CreateLandscapeModel()
        {
            // All wind generator models are stored in ModelsDictionary.xaml ResourceDictionary
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("/Resources/ModelsDictionary.xaml", UriKind.RelativeOrAbsolute);

            // Get Landscape model
            var landscapeModel = dictionary["LandscapeModel"] as GeometryModel3D;

            SetContent(landscapeModel);

            MainCamera.Heading = -125;
            MainCamera.Distance = _contentSize * 1.7;
            MainCamera.CameraWidth = _contentSize * 1.7; // In case we have OrthographicCamera we also set the CameraWidth
        }

        private void EventSource3DOnMouseUp(object sender, MouseButton3DEventArgs mouseButton3DEventArgs)
        {
            // Toggle between setting the start and end position
            if (!_isStartPositionSet)
                _isStartPositionSet = true;
            else
                _isEndPositionSet = true;
        }

        private void EventSource3DOnMouseMove(object sender, Mouse3DEventArgs e)
        {
            // NOTE: 
            // We use e.FinalPointHit instead of e.RayHitResult.PointHit because the FinalPointHit is already transformed by any transformation that the Visual3D object have

            if (!_isStartPositionSet)
                _startPosition = e.FinalPointHit;
            else if (!_isEndPositionSet)
                _endPosition = e.FinalPointHit;
            else
                return;

            UpdateDisplayedDistanceMeasurement(); // Recreate the lines and text that show the distance
        }

        private void ResetButton_OnClick(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void OnMeasureTypeChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateDisplayedDistanceMeasurement(); // Recreate the lines and text that show the distance
        }

        private void OnColorChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateColor();
        }

        private void LineHeightSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            UpdateDisplayedDistanceMeasurement(); // Recreate the lines and text that show the distance
        }

        private void OpenFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            Selected3dsFile();
        }

        private void Reset()
        {
            // UH: There are some problems with using IsVisual - for now we prevent drawing lines and planes in other ways (setting StartPoint to EndPoint)
            StartLineVisual3D.IsVisible = false;
            EndLineVisual3D.IsVisible = false;
            DistanceLineVisual3D.IsVisible = false;
            TextPlaneVisual3D.IsVisible = false;

            TextPlaneVisual3D.Material = null;

            StartPointTextBlock.Text = "";
            EndPointTextBlock.Text = "";

            _isStartPositionSet = false;
            _isEndPositionSet = false;
        }

        private MeasureType GetSelectedMeasureType()
        {
            if (HorizontalDistanceRadioButton.IsChecked ?? false)
                return MeasureType.HorizontalDistance;
            else
                return MeasureType.Distance;
        }

        private void UpdateColor()
        {
            if (!this.IsLoaded)
                return;

            Color newColor;

            if (BlackRadioButton.IsChecked ?? false)
                newColor = Colors.Black;
            else
                newColor = Colors.White;

            StartLineVisual3D.LineColor = newColor;
            EndLineVisual3D.LineColor = newColor;
            DistanceLineVisual3D.LineColor = newColor;

            UpdateDisplayedDistanceMeasurement(); // This will also update the rendered text
        }

        private void UpdateDisplayedDistanceMeasurement()
        {
            double distance;
            double startLineTop, endLineTop;
            LineVisual3D currentLineVisual3D;
            LineVisual3D otherLineVisual3D;
            Point3D lastChangedPosition;

            if (_isStartPositionSet)
            {
                // This means that we are changing the end point
                lastChangedPosition = _endPosition;

                currentLineVisual3D = EndLineVisual3D;
                otherLineVisual3D = StartLineVisual3D;

                EndPointTextBlock.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "End point: {0:0}", _endPosition);
            }
            else
            {
                // We are changing start point
                lastChangedPosition = _startPosition;

                currentLineVisual3D = StartLineVisual3D;
                otherLineVisual3D = EndLineVisual3D;

                StartPointTextBlock.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Start point: {0:0}", _startPosition);
            }


            // LineHeightSlider value is in % - the final height is dependent on camera distance
            double selectionLinesTotalHeight = MainCamera.CameraType == BaseCamera.CameraTypes.PerspectiveCamera ? MainCamera.Distance * 0.08 : MainCamera.CameraWidth * 0.08;
            double selectionLinesHeight = selectionLinesTotalHeight * (LineHeightSlider.Value / 100);


            MeasureType currentMeasureType = GetSelectedMeasureType();

            if (currentMeasureType == MeasureType.HorizontalDistance)
            {
                double dx = (_endPosition.X - _startPosition.X);
                double dz = (_endPosition.Z - _startPosition.Z);

                distance = Math.Sqrt(dx * dx + dz * dz);

                startLineTop = Math.Max(_startPosition.Y, _endPosition.Y) + selectionLinesHeight;
                endLineTop = startLineTop;
            }
            else // if (currentMeasureType == MeasureType.Distance)
            {
                distance = (_endPosition - _startPosition).Length;

                startLineTop = _startPosition.Y + selectionLinesHeight;
                endLineTop = _endPosition.Y + selectionLinesHeight;
            }

            string distanceText = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} km", distance);


            Point3D startPosition = lastChangedPosition;
            Point3D endPosition = new Point3D(startPosition.X, _isStartPositionSet ? endLineTop : startLineTop, startPosition.Z);


            // Update current vertical line (first or second - based on _isStartPositionSet)
            currentLineVisual3D.BeginInit(); // Using BeginInit and EndInit updates the backend geometry just once

            currentLineVisual3D.EndPosition = startPosition;
            currentLineVisual3D.StartPosition = endPosition;
            currentLineVisual3D.IsVisible = true;

            currentLineVisual3D.EndInit();


            // If we are measuring horizontal distance than the top y position of both lines must be the same
            if (_isStartPositionSet && 
                otherLineVisual3D.StartPosition.Y != startLineTop)
            {
                otherLineVisual3D.StartPosition = new Point3D(otherLineVisual3D.StartPosition.X, startLineTop, otherLineVisual3D.StartPosition.Z);
            }


            if (_isStartPositionSet)
            {
                // If we have both start and end points
                // Than we need to display the text and the arrow line under the text
                var textPosition1 = new Point3D(StartLineVisual3D.EndPosition.X, StartLineVisual3D.StartPosition.Y - (selectionLinesTotalHeight * 0.15), StartLineVisual3D.EndPosition.Z);
                var textPosition2 = new Point3D(EndLineVisual3D.EndPosition.X, EndLineVisual3D.StartPosition.Y - (selectionLinesTotalHeight * 0.15), EndLineVisual3D.EndPosition.Z);

                // Convert the 3d positions to 2d screen positions
                Point screenPosition1 = MainCamera.Point3DTo2D(textPosition1);
                Point screenPosition2 = MainCamera.Point3DTo2D(textPosition2);

                // We can check the 2d positions now - if start position in on the right to the end position, than we need to swap start and end position - otherwise the text would appear flipped
                if (screenPosition1.X > screenPosition2.X)
                {
                    Point3D tempPoint = textPosition1;
                    textPosition1 = textPosition2;
                    textPosition2 = tempPoint;
                }

                // Update the line under the text
                DistanceLineVisual3D.BeginInit();
                DistanceLineVisual3D.StartPosition = textPosition1;
                DistanceLineVisual3D.EndPosition = textPosition2;
                DistanceLineVisual3D.IsVisible = true;
                DistanceLineVisual3D.EndInit();


                // Now update the plane that will show the distance text
                Vector3D planeTextDirection = textPosition2 - textPosition1;

                // Create orthogonal vector to text direction and up vector - this will be the normal for the plane
                Vector3D planeNormal = Vector3D.CrossProduct(planeTextDirection, Ab3d.Common.Constants.UpVector);
                planeNormal.Normalize();

                // Because the text is not always horizontal, we cannot always use UpVector for plane's height direction
                // We calculate the height direction with creating orthogonal vector to planeTextDirection and planeNormal

                Vector3D planeHeightDirection = Vector3D.CrossProduct(planeNormal, planeTextDirection);
                planeHeightDirection.Normalize();


                Brush textForegroundBrush;

                if (BlackRadioButton.IsChecked ?? false)
                    textForegroundBrush = Brushes.Black;
                else
                    textForegroundBrush = Brushes.White;

                // Create the texture with distance text
                BitmapSource textBitmap = RenderTextToBitmap(distanceText, textForegroundBrush);

                var imageBrush = new ImageBrush()
                {
                    ImageSource = textBitmap,
                    Stretch = Stretch.Fill,
                    TileMode = TileMode.None,
                    Viewport = new Rect(0, 0, 1, 1),
                    ViewportUnits = BrushMappingMode.RelativeToBoundingBox
                };


                double textHeight = selectionLinesTotalHeight * 0.4;

                // The height of the plane that will display the text is 30
                // From that and the size of the texture we calculate the width of the plane
                double maxTextWidth = (textHeight * textBitmap.Width) / textBitmap.Height;

                double textRectangleWidth = Math.Min(distance, maxTextWidth);
                if (textRectangleWidth < 0.1)
                    textRectangleWidth = 0.1;

                
                // Now we have all the data for the plane
                TextPlaneVisual3D.BeginInit();
                
                TextPlaneVisual3D.CenterPosition = new Point3D(textPosition1.X + planeTextDirection.X * 0.5,
                                                               textPosition1.Y + planeTextDirection.Y * 0.5 + (textHeight * 0.5),
                                                               textPosition1.Z + planeTextDirection.Z * 0.5);

                TextPlaneVisual3D.Size = new Size(textRectangleWidth, textHeight);
                TextPlaneVisual3D.HeightDirection = planeHeightDirection;
                TextPlaneVisual3D.Normal = planeNormal;
                TextPlaneVisual3D.Material = new DiffuseMaterial(imageBrush);
                TextPlaneVisual3D.IsVisible = true;
                
                TextPlaneVisual3D.EndInit();

                _isTextPlaneNormalFlipped = false;
            }
        }


        private void MainCameraChanged(object sender, CameraChangedRoutedEventArgs cameraChangedRoutedEventArgs)
        {
            // If line with text and text plane are not visible than we can exit this method
            if (TextPlaneVisual3D.Material != null)
            {
                // Else we need to convert the 3D start and end positions into 2D screen position
                Point screenPosition1 = MainCamera.Point3DTo2D(DistanceLineVisual3D.StartPosition);
                Point screenPosition2 = MainCamera.Point3DTo2D(DistanceLineVisual3D.EndPosition);

                // Now we check if the start position has moved to the right of the end position
                // This means that because of rotated camera, we are not watching the text from behind - it appears flipped
                if (screenPosition1.X > screenPosition2.X)
                {
                    if (!_isTextPlaneNormalFlipped)
                    {
                        // To flip the rendered text texture, we just flip the normal of the plane
                        TextPlaneVisual3D.Normal = -1 * TextPlaneVisual3D.Normal;
                        _isTextPlaneNormalFlipped = true;
                    }
                }
                else if (_isTextPlaneNormalFlipped)
                {
                    // Uh, the normal was previously flipped - we need to flip it again to make it as original value
                    TextPlaneVisual3D.Normal = -1 * TextPlaneVisual3D.Normal;
                    _isTextPlaneNormalFlipped = false;
                }
            }

            UpdateDisplayedDistanceMeasurement();
        }

        private void Selected3dsFile()
        {
            // Ab3d.PowerToys samples by default does not have reference to Ab3d.Reader3ds
            // If you want to use this sample with custom 3ds mode, uncomment the content of the next method (Read3dsFile)

            MessageBox.Show("To read 3ds file, please add reference to Ab3d.Reader3ds\r\nand than uncomment the code in Read3dsFile method.");
            return;

            Microsoft.Win32.OpenFileDialog openFileDialog;

            openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.DefaultExt = "3ds";
            openFileDialog.Filter = "3ds files (*.3ds)|*.3ds";
            openFileDialog.Multiselect = false;
            openFileDialog.Title = "Select 3ds file to open";
            openFileDialog.ValidateNames = true;

            if (openFileDialog.ShowDialog() ?? false)
                Read3dsFile(openFileDialog.FileName);
        }

        private void Read3dsFile(string fileName)
        {
            //var reader3ds = new Ab3d.Reader3ds();
            //Model3DGroup readModel = reader3ds.ReadFile(fileName);

            //SetContent(readModel);
            //MainCamera.Distance = _contentSize * 2.0;

            //SizeXTextBlock.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Size X: {0:0} m", readModel.Bounds.SizeX);
            //SizeYTextBlock.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Size Y: {0:0} m", readModel.Bounds.SizeY);
            //SizeZTextBlock.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Size Z: {0:0} m", readModel.Bounds.SizeZ);

            //Reset();

            //File3dsRadioButton.IsChecked = true;
        }

        private BitmapSource RenderTextToBitmap(string text, Brush textForegroundBrush)
        {
            var textBlock = new TextBlock()
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = textForegroundBrush,
                FontSize = 40
            };

            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            textBlock.Arrange(new Rect(new Point(0, 0), textBlock.DesiredSize));

            // Render distancesPanel to Bitmap
            var bmp = new RenderTargetBitmap((int)textBlock.ActualWidth, (int)textBlock.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(textBlock);

            return bmp;
        }

        private void OnSceneTypeChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            LoadSelectedScene();
        }
    }
}
