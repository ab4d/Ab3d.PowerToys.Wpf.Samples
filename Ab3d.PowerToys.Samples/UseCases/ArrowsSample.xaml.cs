using System.Runtime.InteropServices;
using System.Windows.Media.Media3D;
using Ab3d.Meshes;
using Ab3d.Visuals;
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
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for ArrowsSample.xaml
    /// </summary>
    public partial class ArrowsSample : Page, ICompositionRenderingSubscriber
    {
        private ArrowVisual3D[,] _arrows;
        
        private int _xCount;
        private int _yCount;
        private double _xSize;
        private double _ySize;
        private double _arrowsLength;

        private double _minDistance;
        private double _maxDistance;

        private Vector3D _sphereStartPosition;
        private Vector3D _spherePosition;

        private DiffuseMaterial[] _gradientMaterials;

        private DateTime _startTime;
        private TranslateTransform3D _sphereTranslate;

        private int _lastSecond;
        private int _framesPerSecond;

        public ArrowsSample()
        {
            InitializeComponent();

            _xCount = 12;
            _yCount = 12;
            _xSize = 600;
            _ySize = 600;

            _arrowsLength = 40;

            _sphereStartPosition = new Vector3D(0, 200, 0);

            Camera1.TargetPosition = new Point3D(0, _sphereStartPosition.Y * 0.3, 0); // target y = 1/3 of the sphere start height


            // Min and max distance will be used to get the color from the current arrow distance
            _minDistance = _sphereStartPosition.Y;

            double dx = Math.Abs(_sphereStartPosition.X) + (_xSize / 2);
            double dz = Math.Abs(_sphereStartPosition.Z) + (_ySize / 2);

            _maxDistance = Math.Sqrt(dx * dx + _sphereStartPosition.Y * _sphereStartPosition.Y + dz * dz);

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            CreateArrows();

            _startTime = DateTime.Now;

            // Use CompositionRenderingHelper to subscribe to CompositionTarget.Rendering event
            // This is much safer because in case we forget to unsubscribe from Rendering, the CompositionRenderingHelper will unsubscribe us automatically
            // This allows to collect this class will Grabage collector and prevents infinite calling of Rendering handler.
            // After subscribing the ICompositionRenderingSubscriber.OnRendering method will be called on each CompositionTarget.Rendering event
            CompositionRenderingHelper.Instance.Subscribe(this);
        }

        private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            CompositionRenderingHelper.Instance.Unsubscribe(this);
        }

        void ICompositionRenderingSubscriber.OnRendering(EventArgs e)
        {
            double elapsedSeconds = (DateTime.Now - _startTime).TotalSeconds;

            if (DateTime.Now.Second != _lastSecond)
            {
                //System.Diagnostics.Debug.WriteLine(string.Format("FPS: {0}", _framesPerSecond));

                _lastSecond = DateTime.Now.Second;
                _framesPerSecond = 0;
            }
            else
            {
                _framesPerSecond ++;
            }

            double x, y, z;

            x = _sphereStartPosition.X;
            y = _sphereStartPosition.Y;
            z = _sphereStartPosition.Z;

            // Rotate on xz plane
            x += Math.Sin(elapsedSeconds * 3) * _xSize * 0.1;
            z += Math.Cos(elapsedSeconds * 3) * _ySize * 0.1;

            // Rotate on xy plane
            x += Math.Sin(elapsedSeconds) * _xSize * 0.2;
            y += Math.Cos(elapsedSeconds) * 90;
            
            // Rotate on yz plane
            y += Math.Sin(elapsedSeconds * 5) * 50;
            z += Math.Cos(elapsedSeconds * 0.3) * _ySize * 0.2;

            
            _sphereTranslate.OffsetX = x;
            _sphereTranslate.OffsetY = y;
            _sphereTranslate.OffsetZ = z;

            _spherePosition = new Vector3D(x, y, z);


            for (int xi = 0; xi < _xCount; xi++)
            {
                for (int yi = 0; yi < _yCount; yi++)
                {
                    var arrowPosition = GetArrowPosition(xi, yi);
                    double distance = GetDistance(arrowPosition.X, arrowPosition.Y);

                    var oneArrow = _arrows[xi, yi];

                    oneArrow.Material = GetMaterialForDistance(distance);

                    var arrowDirection = GetArrowDirection(arrowPosition.X, arrowPosition.Y);
                    oneArrow.EndPosition = oneArrow.StartPosition + arrowDirection;
                }
            }
        }


        private void CreateArrows()
        {
            double xStep = _xSize / _xCount;
            double yStep = _ySize / _yCount;

            double x, y;

            _arrows = new ArrowVisual3D[_xCount,_yCount];

            MainViewport.Children.Clear();


            var sphereVisual3D = new SphereVisual3D()
            {
                CenterPosition = new Point3D(0, 0, 0),
                Radius = 10,
                Material = new DiffuseMaterial(Brushes.Gold)
            };

            _sphereTranslate = new TranslateTransform3D(_sphereStartPosition);
            sphereVisual3D.Transform = _sphereTranslate;


            MainViewport.Children.Add(sphereVisual3D);


            x = -(_xSize / 2);
            for (int xi = 0; xi < _xCount; xi++)
            {
                y = -(_ySize / 2);

                for (int yi = 0; yi < _yCount; yi++)
                {
                    double distance = GetDistance(x, y);

                    var arrowVisual3D = new ArrowVisual3D();

                    arrowVisual3D.BeginInit();
                    arrowVisual3D.StartPosition = new Point3D(x, 0, y);
                    arrowVisual3D.Radius = _arrowsLength / 15;
                    arrowVisual3D.Material = GetMaterialForDistance(distance);
                    arrowVisual3D.ArrowAngle = 45;
                    arrowVisual3D.Segments = 10;

                    var arrowDirection = GetArrowDirection(x, y);
                    arrowVisual3D.EndPosition = arrowVisual3D.StartPosition + arrowDirection;
                    arrowVisual3D.EndInit();

                    _arrows[xi, yi] = arrowVisual3D;

                    MainViewport.Children.Add(arrowVisual3D);

                    y += yStep;
                }

                x += xStep;
            }
        }

        private Point GetArrowPosition(int xi, int yi)
        {
            double xStep = _xSize / _xCount;
            double yStep = _ySize / _yCount;

            return new Point(-(_xSize / 2) + xStep * xi,
                             -(_ySize / 2) + yStep * yi);
        }

        private Vector3D GetArrowDirection(double x, double y)
        {
            var direction = new Vector3D(_spherePosition.X - x, _spherePosition.Y, _spherePosition.Z - y);
            direction.Normalize();
            direction *= _arrowsLength;

            return direction;
        }

        private double GetDistance(double x, double y)
        {
            double dx = _spherePosition.X - x;
            double dz = _spherePosition.Z - y;

            double distance = Math.Sqrt(dx * dx + _spherePosition.Y * _spherePosition.Y + dz * dz);

            return distance;
        }

        private DiffuseMaterial GetMaterialForDistance(double distance)
        {
            if (_gradientMaterials == null)
                _gradientMaterials = CreateDistanceMaterials();

            int materialIndex;

            if (distance <= _minDistance)
                materialIndex = 0;
            else if (distance >= _maxDistance)
                materialIndex = _gradientMaterials.Length - 1;
            else
                materialIndex = Convert.ToInt32((distance - _minDistance) * (_gradientMaterials.Length - 1) / (_maxDistance - _minDistance));

            return _gradientMaterials[materialIndex];
        }

        private DiffuseMaterial[] CreateDistanceMaterials()
        {
            // Here we prepare list of materials that will be used to for arrows on different distances from the gold sphere

            // We use HeightMapMesh3D.GetGradientColorsArray to create an array with color values created from the gradient. The array size is 30.
            var gradientStopCollection = new GradientStopCollection();
            gradientStopCollection.Add(new GradientStop(Colors.Red, 0));          // uses with min distance - closest to the object
            gradientStopCollection.Add(new GradientStop(Colors.Orange, 0.2));     // uses with min distance - closest to the object
            gradientStopCollection.Add(new GradientStop(Colors.Yellow, 0.4));     // uses with min distance - closest to the object
            gradientStopCollection.Add(new GradientStop(Colors.Green, 0.6));      // uses with min distance - closest to the object
            gradientStopCollection.Add(new GradientStop(Colors.DarkBlue, 0.8));   // used with max distance
            gradientStopCollection.Add(new GradientStop(Colors.DodgerBlue, 1));   // used with max distance

            var linearGradientBrush = new LinearGradientBrush(gradientStopCollection, new Point(0, 0), new Point(0, 1));

            // Use linearGradientBrush to create an array with 30 Colors
            var gradientColorsArray = HeightMapMesh3D.GetGradientColorsArray(linearGradientBrush, 30);

            var gradientMaterials = new DiffuseMaterial[gradientColorsArray.Length];

            for (int i = 0; i < gradientColorsArray.Length; i++)
            {
                var diffuseMaterial = new DiffuseMaterial(new SolidColorBrush(gradientColorsArray[i]));
                gradientMaterials[i] = diffuseMaterial;
            }

            return gradientMaterials;
        }
    }
}
