using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
using Ab3d.Common.EventManager3D;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for DrawOnTextureSample.xaml
    /// </summary>
    public partial class DrawOnTextureSample : Page
    {
        private double _lastXPos, _lastYPos;

        private PainterSettings _settings;
        private TypeConverter _stringToBrushConverter;
        private Ab3d.Utilities.EventManager3D _eventManager3D;

        public DrawOnTextureSample()
        {
            InitializeComponent();

            // Setup Painter settings (color and size of brush)
            // This code is taken from ZoomPanel samples
            this.CommandBindings.Add(new CommandBinding(PainterSettings.SetStrokeThicknessCommand, SetStrokeThicknessCommandExecuted));
            this.CommandBindings.Add(new CommandBinding(PainterSettings.SetStrokeColorCommand, SetStrokeColorCommandExecuted));

            _settings = new PainterSettings();
            _settings.CurrentStrokeBrush = Brushes.Red;
            _settings.CurrentStrokeThickness = 6;

            ColorsStackPanel.DataContext = _settings;
            ThicknessStackPanel.DataContext = _settings;


            // Create EventManager3D
            _eventManager3D = new Ab3d.Utilities.EventManager3D(MainViewport);

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                RecreateTargetModel();
            };
        }

        private void RecreateTargetModel()
        {
            MainViewport.Children.Clear();

            Visual3D targetModelVisual3D;

            // Setup VisualBrush as the material for the model
            var visualBrush = new VisualBrush(DrawingCanvas);
            var material = new DiffuseMaterial(visualBrush);


            if (HeightMapModelTypeRadioButton.IsChecked ?? false)
            {
                var heightMapVisual3D = new Ab3d.Visuals.HeightMapVisual3D()
                {
                    Size = new Size3D(100, 50, 100),
                    IsWireframeShown = false,
                    IsSolidSurfaceShown = true,
                    Material = material,
                    BackMaterial = new DiffuseMaterial(Brushes.DimGray)
                };

                // Create height data from bitmap
                string heightMapFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\HeightMaps\\simpleHeightMap.png");
                BitmapImage heightImage = new BitmapImage(new Uri(heightMapFileName, UriKind.RelativeOrAbsolute));

                double[,] heightData = Ab3d.PowerToys.Samples.Objects3D.HeightMapSample.OpenHeightMapDataFile(heightImage, invertData: false);

                if (heightData != null)
                    heightMapVisual3D.HeightData = heightData;

                targetModelVisual3D = heightMapVisual3D;
            }
            else if (BoxModelTypeRadioButton.IsChecked ?? false)
            {
                var boxVisual3D = new Ab3d.Visuals.BoxVisual3D()
                {
                    Size = new Size3D(100, 50, 100),
                    Material = material
                };

                targetModelVisual3D = boxVisual3D;
            }
            else //if (SphereModelTypeRadioButton.IsChecked ?? false)
            {
                var sphereVisual3D = new Ab3d.Visuals.SphereVisual3D()
                {
                    Radius = 40,
                    Material = material
                };

                targetModelVisual3D = sphereVisual3D;
            }

            MainViewport.Children.Add(targetModelVisual3D);

            // With clearing all childen we have also removed the camera's light. 
            // Refresh the camera now to recreate the light
            Camera1.Refresh();

            SetupMouseEventHandlers(targetModelVisual3D);
        }

        private void SetupMouseEventHandlers(Visual3D targetModelVisual3D)
        {
            var visualEventSource3D = new VisualEventSource3D(targetModelVisual3D);

            visualEventSource3D.MouseEnter += delegate(object sender, Mouse3DEventArgs e)
            {
                Mouse.OverrideCursor = Cursors.Cross;
            };

            visualEventSource3D.MouseLeave += delegate(object sender, Mouse3DEventArgs e)
            {
                Mouse.OverrideCursor = null;
            };

            visualEventSource3D.MouseMove += delegate(object sender, Mouse3DEventArgs e)
            {
                if (e.MouseData.LeftButton != MouseButtonState.Pressed)
                    return;

                // Calculte the position inside texture where the mouse is
                Point hitPosition;
                var success = GetRelativeHitTexturePosition(e.RayHitResult, out hitPosition);

                if (!success)
                    return; // Cannot get the position


                // The position was now relative (1 was bottom right)
                // Now adjust the position for the size of target Canvas
                double xPos = hitPosition.X * DrawingCanvas.Width;
                double yPos = hitPosition.Y * DrawingCanvas.Height;


                double strokeThickness = _settings.CurrentStrokeThickness;
                double halfStrokeThickness = strokeThickness / 2;

                // Prevent drawing over the "edge" of Canvas
                // This would increase the size of Canvas and produce white areas on the model
                // Set xPos to be between halfStrokeThickness and DrawingCanvas.Width - halfStrokeThickness
                xPos = Math.Max(halfStrokeThickness, Math.Min(xPos, DrawingCanvas.Width - halfStrokeThickness));
                yPos = Math.Max(halfStrokeThickness, Math.Min(yPos, DrawingCanvas.Height - halfStrokeThickness));

                // Check if the change of position is significant
                if (Math.Abs(_lastXPos - xPos) < 1 && Math.Abs(_lastYPos - yPos) < 1)
                    return;


                _lastXPos = xPos;
                _lastYPos = yPos;

                var ellipse = new Ellipse()
                {
                    Width = strokeThickness,
                    Height = strokeThickness,
                    Fill = _settings.CurrentStrokeBrush
                };

                Canvas.SetLeft(ellipse, xPos - halfStrokeThickness);
                Canvas.SetTop(ellipse, yPos - halfStrokeThickness);

                DrawingCanvas.Children.Add(ellipse);
            };

            _eventManager3D.ResetEventSources3D(); // Clear all previously registered event sources
            _eventManager3D.RegisterEventSource3D(visualEventSource3D);
        }


        private bool GetRelativeHitTexturePosition(RayMeshGeometry3DHitTestResult rayHitTestResult, out Point hitPosition)
        {
            var meshGeometry3D = rayHitTestResult.MeshHit;

            if (meshGeometry3D.TextureCoordinates == null || meshGeometry3D.TextureCoordinates.Count == 0)
            {
                hitPosition = new Point();
                return false;
            }

            // Get texture coordinates for 3 vertextes that are closest to the hit point
            Point texture1 = meshGeometry3D.TextureCoordinates[rayHitTestResult.VertexIndex1];
            Point texture2 = meshGeometry3D.TextureCoordinates[rayHitTestResult.VertexIndex2];
            Point texture3 = meshGeometry3D.TextureCoordinates[rayHitTestResult.VertexIndex3];

            // Get x and y position in the texture with using 3 different verexes and their weights
            double xPos = (texture1.X * rayHitTestResult.VertexWeight1) +
                          (texture2.X * rayHitTestResult.VertexWeight2) +
                          (texture3.X * rayHitTestResult.VertexWeight3);

            double yPos = (texture1.Y * rayHitTestResult.VertexWeight1) +
                          (texture2.Y * rayHitTestResult.VertexWeight2) +
                          (texture3.Y * rayHitTestResult.VertexWeight3);


            hitPosition = new Point(xPos, yPos);
            return true;
        }

        void SetStrokeThicknessCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _settings.CurrentStrokeThickness = double.Parse((string)e.Parameter);
        }

        void SetStrokeColorCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (_stringToBrushConverter == null)
                _stringToBrushConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Brush));

            _settings.CurrentStrokeBrush = (Brush)_stringToBrushConverter.ConvertFromInvariantString((string)e.Parameter);
        }

        private void OnModelTypeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            RecreateTargetModel();
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Children.Clear();
        }
    }

    public class PainterSettings : INotifyPropertyChanged
    {
        public static readonly RoutedCommand SetStrokeThicknessCommand = new RoutedCommand("SetStrokeThicknessCommand", typeof(PainterSettings));
        public static readonly RoutedCommand SetStrokeColorCommand    = new RoutedCommand("SetStrokeColorCommand", typeof(PainterSettings));

        private Brush _currentStrokeBrush;
        public Brush CurrentStrokeBrush
        {
            get { return _currentStrokeBrush; }
            set
            {
                _currentStrokeBrush = value;
                NotifyPropertyChanged("CurrentStrokeBrush");
            }
        }

        private double _currentStrokeThickness;
        public double CurrentStrokeThickness
        {
            get { return _currentStrokeThickness; }
            set
            {
                _currentStrokeThickness = value;
                NotifyPropertyChanged("CurrentStrokeThickness");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
