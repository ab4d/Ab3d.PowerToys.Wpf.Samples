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
using Ab3d.Controls;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.EventManager3D
{
    /// <summary>
    /// Interaction logic for StandardWpfHitTestingDemo.xaml
    /// </summary>
    public partial class StandardWpfHitTestingDemo : Page
    {
        private GeometryModel3D _selectedGeometryModel3D;
        private GeometryModel3D _mouseDownModel3D;

        private RayMeshGeometry3DHitTestResult _lastHitResult;

        private Material _savedSelectedMaterial;

        private Material _selectedMaterial;
        private Material _clickedMaterial;
        private PlaneVisual3D _glassPlaneVisual3D;
        private ModelVisual3D _sceneVisual3D;

        public StandardWpfHitTestingDemo()
        {
            InitializeComponent();

            _selectedMaterial = new DiffuseMaterial(Brushes.Yellow);
            _clickedMaterial = new DiffuseMaterial(Brushes.Red);

            CreateTestScene();

            MouseCameraController1.RotationCursor = null;

            MainViewport.MouseDown  += OnMouseDown;
            MainViewport.MouseUp    += OnMouseUp;
            MainViewport.MouseMove  += OnMouseMove;
            MainViewport.MouseLeave += OnMouseLeave;

            MouseCameraControllerInfo1.AddCustomInfoLine(0, MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed, "3D object click");
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            GeometryModel3D hitGeometryModel3D;
            Visual3D hitVisual3D;

            var mousePosition = e.GetPosition(ViewportBorder);
            HitTest(mousePosition, out hitGeometryModel3D, out hitVisual3D);

            if (hitGeometryModel3D != null)
            {
                LogMessage("MouseDown on " + (hitGeometryModel3D.GetName() ?? "[GeometryModel3D]"));
                _mouseDownModel3D = hitGeometryModel3D;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            GeometryModel3D hitGeometryModel3D;
            Visual3D hitVisual3D;

            var mousePosition = e.GetPosition(ViewportBorder);
            HitTest(mousePosition, out hitGeometryModel3D, out hitVisual3D);

            if (hitGeometryModel3D != null)
            {
                LogMessage("MouseUp on " + (hitGeometryModel3D.GetName() ?? "[GeometryModel3D]"));

                if (hitGeometryModel3D == _mouseDownModel3D)
                {
                    if (ReferenceEquals(hitVisual3D, _glassPlaneVisual3D))
                    {
                        LogMessage("Clicking on GlassPlaneVisual3D is prevented");
                    }
                    else
                    {
                        LogMessage("CLICK on " + (hitGeometryModel3D.GetName() ?? "[GeometryModel3D]"));

                        hitGeometryModel3D.Material = _clickedMaterial;
                        _savedSelectedMaterial      = _clickedMaterial;
                    }
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            GeometryModel3D hitGeometryModel3D;
            Visual3D hitVisual3D;

            var mousePosition = e.GetPosition(ViewportBorder);

            HitTest(mousePosition, out hitGeometryModel3D, out hitVisual3D);

            if (hitGeometryModel3D != null && hitGeometryModel3D != _selectedGeometryModel3D)
               SelectModel3D(hitGeometryModel3D);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            UnSelectModel3D();
        }


        private void HitTest(Point mousePosition, out GeometryModel3D hitGeometryModel3D, out Visual3D hitVisual3D)
        {
            if (SimpleHitTestRadioButton.IsChecked ?? false)
            {
                // The most simple hit testing is done with calling HitTest method.
                // This is the easiest option and can be used when we want to get the first hit object and do not need to filter hit objects.
                _lastHitResult = VisualTreeHelper.HitTest(MainViewport, mousePosition) as RayMeshGeometry3DHitTestResult;
            }
            else if (CallbackRadioButton.IsChecked ?? false)
            {
                // A more advanced hit testing is done with using hit result callback.
                // There we decide if we want to continue hit testing or stop the hit testing.
                // This way it is possible to get all 3D objects under the mouse or do hit test filtering.
                PointHitTestParameters pointParams = new PointHitTestParameters(mousePosition);
                VisualTreeHelper.HitTest(MainViewport, null, HitTestResultHandler, pointParams);
            }
            else if (FilterAndCallbackRadioButton.IsChecked ?? false)
            {
                // When using hit result callback we can filter the hit objects in the HitTestResultHandler.
                // But a better way to filer hit objects is to filter with using HitTestFilterCallback.
                // This way we can skip hit testing on entire parts of the 3D hierarchy.
                PointHitTestParameters pointParams = new PointHitTestParameters(mousePosition);
                VisualTreeHelper.HitTest(MainViewport, HitTestFilterCallback, HitTestResultHandler, pointParams);
            }

            if (_lastHitResult != null)
            {
                hitGeometryModel3D = _lastHitResult.ModelHit as GeometryModel3D;
                hitVisual3D = _lastHitResult.VisualHit;
            }
            else
            {
                hitGeometryModel3D = null;
                hitVisual3D = null;
            }
        }

        private HitTestFilterBehavior HitTestFilterCallback(DependencyObject potentialHitTestTarget)
        {
            // Check if we are on _glassPlaneVisual3D and in this case skip hit testing on this and all child objects
            if (ReferenceEquals(potentialHitTestTarget, _glassPlaneVisual3D))
                return HitTestFilterBehavior.ContinueSkipSelfAndChildren;

            return HitTestFilterBehavior.Continue;
        }

        private HitTestResultBehavior HitTestResultHandler(HitTestResult hitResult)
        {
            _lastHitResult = hitResult as RayMeshGeometry3DHitTestResult;

            // Instead of filtering with HitTestFilterCallback
            // we could also filter here:
            // In case we hit a _glassPlaneVisual3D, then continue hit testing (get the next hit object)
            // This would be done with the following code
            //if (ReferenceEquals(hitResult.VisualHit, _glassPlaneVisual3D))
            //    return HitTestResultBehavior.Continue;

            // We can return Stop to stop hit testing,
            // or Continue to continue hit testing (get the next hit object).
            // This can be used to get all hit objects under the mouse (for example with adding hit objects to a List of hit objects).
            return HitTestResultBehavior.Stop;
        }


        private void SelectModel3D(Model3D model3D)
        {
            UnSelectModel3D();

            var geometryModel3D = model3D as GeometryModel3D;

            if (geometryModel3D != null)
            {
                _selectedGeometryModel3D = geometryModel3D;
                _savedSelectedMaterial   = geometryModel3D.Material;

                geometryModel3D.Material = _selectedMaterial;

                LogMessage("Selected " + (geometryModel3D.GetName() ?? "[GeometryModel3D]"));
            }
        }

        private void UnSelectModel3D()
        {
            if (_selectedGeometryModel3D == null)
                return;

            _selectedGeometryModel3D.Material = _savedSelectedMaterial;

            _selectedGeometryModel3D = null;
            _savedSelectedMaterial = null;
        }


        private void CreateTestScene()
        {
            MainViewport.Children.Clear();

            string fileName = AppDomain.CurrentDomain.BaseDirectory + @"Resources\ObjFiles\house with trees.obj";

            var readerObj    = new Ab3d.ReaderObj();
            var sceneModel3D = readerObj.ReadModel3D(fileName);

            LogMessage("Loaded:\r\n" + Ab3d.Utilities.Dumper.GetObjectHierarchyString(sceneModel3D));

            Ab3d.Utilities.ModelUtils.CenterAndScaleModel3D(sceneModel3D,
                centerPosition: new Point3D(0, 0, 0),
                finalSize: new Size3D(100, 100, 100),
                preserveAspectRatio: true);

            _sceneVisual3D = new ModelVisual3D();
            _sceneVisual3D.Content = sceneModel3D;
            _sceneVisual3D.SetName("SceneVisual3D"); // Set Name dependency properties so that we can read then when getting hit test result

            MainViewport.Children.Add(_sceneVisual3D);


            _glassPlaneVisual3D = new Ab3d.Visuals.PlaneVisual3D()
            {
                CenterPosition  = new Point3D(0, 0, 45),
                Size            = new Size(70, 10),
                Normal          = new Vector3D(0, 0, 1),
                HeightDirection = new Vector3D(0, 1, 0),
                Material        = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(100, 200, 200, 255)))
            };

            _glassPlaneVisual3D.BackMaterial = _glassPlaneVisual3D.Material;
            _glassPlaneVisual3D.SetName("GlassPlaneVisual3D");

            MainViewport.Children.Add(_glassPlaneVisual3D);


            Camera1.Refresh(); // This will recreate camera's light that was removed when we called MainViewport.Children.Clear()
        }

        private void LogMessage(string message)
        {
            InfoTextBox.Text = message + Environment.NewLine + InfoTextBox.Text;
        }
    }
}
