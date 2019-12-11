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
using Ab3d.Assimp;
using Ab3d.Common;
using Ab3d.Common.Cameras;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for PlanarShadows.xaml
    /// </summary>
    public partial class PlanarShadows : Page
    {
        private PointLight _shadowPointLight;
        private DirectionalLight _shadowDirectionalLight;

        private Light _currentShadowLight;

        private Model3D _loadedModel3D;
        
        private double _lightVerticalAngle;
        private double _lightHorizontalAngle;
        private double _lightDistance;
        private ModelVisual3D _shadowVisual3D;

        private PlanarShadowMeshCreator _planarShadowMeshCreator;
        private GeometryModel3D _shadowModel3D;
        private AxisAngleRotation3D _axisAngleRotation3D;

        private DiffuseMaterial _solidShadowMaterial;
        private DiffuseMaterial _transparentShadowMaterial;

        public PlanarShadows()
        {
            InitializeComponent();


            //var bitmapImage = new BitmapImage(new Uri("pack://application:,,,/Resources/10x10-texture.png"));
            //GroundPlane.Material = new DiffuseMaterial(new ImageBrush(bitmapImage));

            _solidShadowMaterial = new DiffuseMaterial(Brushes.Black);
            _transparentShadowMaterial = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 0, 0, 0)));

            _loadedModel3D = LoadModel();

            MainViewport.Children.Add(_loadedModel3D.CreateModelVisual3D());

            // Create PlanarShadowMeshCreator
            // If your model is very complex, then it is recommended to use a simplified 3D model for shadow generation
            _planarShadowMeshCreator = new PlanarShadowMeshCreator(_loadedModel3D);
            _planarShadowMeshCreator.SetPlane(GroundPlane.CenterPosition, GroundPlane.Normal, GroundPlane.HeightDirection, GroundPlane.Size);


            _lightHorizontalAngle = -25;
            _lightVerticalAngle = 22;
            _lightDistance = 500;

            _shadowPointLight       = new PointLight();
            _shadowDirectionalLight = new DirectionalLight();

            Camera1.ShowCameraLight = ShowCameraLightType.Never;

            SetShadowLight(isDirectionalLight: true);

            UpdateLights();
            
            UpdateShadowModel();

            this.PreviewKeyDown += OnPreviewKeyDown;

            // This will allow receiving keyboard events
            this.Focusable = true;
            this.Focus();

        }

        private void UpdateShadowModel()
        {
            if (_planarShadowMeshCreator == null)
                return;

            // PlanarShadowMeshCreator generates a MeshGeometry3D that represents a shadow that is flattened to the plane.
            MeshGeometry3D shadowMesh;
            if (_currentShadowLight == _shadowDirectionalLight)
                shadowMesh = _planarShadowMeshCreator.ApplyShadowMatrix(_shadowDirectionalLight.Direction);
            else
                shadowMesh = _planarShadowMeshCreator.ApplyShadowMatrix(_shadowPointLight.Position);

            if (_shadowModel3D == null)
            {
                var shadowMaterial = (TransparentShadowCheckBox.IsChecked ?? false) ? _transparentShadowMaterial : _solidShadowMaterial;
                _shadowModel3D = new GeometryModel3D(shadowMesh, shadowMaterial);

                if (_shadowVisual3D != null)
                    MainViewport.Children.Remove(_shadowVisual3D);

                _shadowVisual3D = _shadowModel3D.CreateModelVisual3D();
                _shadowVisual3D.Transform = new TranslateTransform3D(0, 0.01, 0); // Lift the shadow 3D model slightly above the ground

                MainViewport.Children.Add(_shadowVisual3D);
            }
            else
            {
                _shadowModel3D.Geometry = shadowMesh;
            }
        }

        
        private Model3D LoadModel()
        {
            AssimpLoader.LoadAssimpNativeLibrary();

            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\robotarm-upper-part.3ds");

            var assimpWpfImporter = new AssimpWpfImporter();
            var robotModel3D = assimpWpfImporter.ReadModel3D(fileName);

            var hand2Group = assimpWpfImporter.NamedObjects["Hand2__Group"] as Model3DGroup;
            _axisAngleRotation3D = new AxisAngleRotation3D(new Vector3D(0, 0, -1), 0);
            var rotateTransform3D = new RotateTransform3D(_axisAngleRotation3D);

            Ab3d.Utilities.TransformationsHelper.AddTransformation(hand2Group, rotateTransform3D);

            return robotModel3D;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            bool isChanged = false;
            double stepSize = 5;

            switch (keyEventArgs.Key)
            {
                case Key.Up:
                    if (_lightVerticalAngle - stepSize > 15)
                    {
                        _lightVerticalAngle += stepSize;
                        isChanged = true;
                    }
                    break;

                case Key.Down:
                    if (_lightVerticalAngle + stepSize < 165)
                    {
                        _lightVerticalAngle -= stepSize;
                        isChanged = true;
                    }
                    break;


                case Key.Left:
                    _lightHorizontalAngle += stepSize;
                    isChanged = true;
                    break;

                case Key.Right:
                    _lightHorizontalAngle -= stepSize;
                    isChanged = true;
                    break;


                case Key.PageUp:
                    _lightDistance += stepSize;
                    isChanged = true;
                    break;

                case Key.PageDown:
                    _lightDistance -= stepSize;
                    isChanged = true;
                    break;
            }

            if (isChanged)
            {
                UpdateLights();
                UpdateShadowModel();

                keyEventArgs.Handled = true;
            }
            else
            {
                keyEventArgs.Handled = false;
            }
        }

        private void UpdateLights()
        {
            var position = CalculateLightPosition();


            // Create direction from position - target position = (0,0,0)
            var lightDirection = new Vector3D(-position.X, -position.Y, -position.Z);
            lightDirection.Normalize();

            _shadowPointLight.Position = position;
            _shadowDirectionalLight.Direction = lightDirection;
        }

        private Point3D CalculateLightPosition()
        {
            double xRad = _lightHorizontalAngle * Math.PI / 180.0;
            double yRad = _lightVerticalAngle * Math.PI / 180.0;

            double x = (Math.Sin(xRad) * Math.Cos(yRad)) * _lightDistance;
            double y = Math.Sin(yRad) * _lightDistance;
            double z = (Math.Cos(xRad) * Math.Cos(yRad)) * _lightDistance;

            return new Point3D(x, y, z);
        }

        private void SetShadowLight(bool isDirectionalLight)
        {
            if (isDirectionalLight)
            {
                if (_currentShadowLight == _shadowDirectionalLight)
                    return;

                _currentShadowLight = _shadowDirectionalLight;
            }
            else
            {
                if (_currentShadowLight == _shadowPointLight)
                    return;

                _currentShadowLight = _shadowPointLight;
            }

            LightPlaceholder.Content = _currentShadowLight;
        }


        private void DirectionalLightRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            SetShadowLight(isDirectionalLight: true);
            UpdateShadowModel();
        }

        private void PointLightRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            SetShadowLight(isDirectionalLight: false);
            UpdateShadowModel();
        }

        private void ChangeModelButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_axisAngleRotation3D != null)
            {
                _axisAngleRotation3D.Angle += 10;

                _planarShadowMeshCreator.UpdateModel3D();
                UpdateShadowModel();
            }
        }

        private void OnClipToPlaneCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            _planarShadowMeshCreator.ClipToPlane = ClipToPlaneCheckBox.IsChecked ?? false;

            _planarShadowMeshCreator.UpdateModel3D();
            UpdateShadowModel();
        }

        private void OnTransparentShadowCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded || _shadowModel3D == null)
                return;

            TransparentShadowInfoTextBlock.Visibility = (TransparentShadowCheckBox.IsChecked ?? false) ? Visibility.Visible : Visibility.Collapsed;

            var shadowMaterial = (TransparentShadowCheckBox.IsChecked ?? false) ? _transparentShadowMaterial : _solidShadowMaterial;
            _shadowModel3D.Material = shadowMaterial;
        }
    }
}
