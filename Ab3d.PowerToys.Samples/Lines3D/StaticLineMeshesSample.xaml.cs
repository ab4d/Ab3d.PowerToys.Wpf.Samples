using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Ab3d.Cameras;
using Ab3d.Common.Cameras;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    /// <summary>
    /// Interaction logic for StaticLineMeshesSample.xaml
    /// </summary>
    public partial class StaticLineMeshesSample : Page
    {
        private GeometryModel3D _shownLineModel3D;

        public StaticLineMeshesSample()
        {
            InitializeComponent();

            CreateOffline3DScene();

            Camera1.CameraChanged += delegate(object sender, CameraChangedRoutedEventArgs args)
            {
                if (BillboardCheckbox.IsChecked ?? false)
                    ApplyBillboardMatrix();
            };
        }

        private void CreateOffline3DScene()
        {
            var viewport3D = new Viewport3D();

            // IMPORTANT !!!
            // Because this Viewport3D will not be actually shown, we need to manually set its size
            // because this is needed for projection matrix.
            viewport3D.Width = 800;
            viewport3D.Height = 600;

            var targetPositionCamera = new TargetPositionCamera()
            {
                Heading          = 0,
                Attitude         = 0,
                CameraType       = BaseCamera.CameraTypes.OrthographicCamera,
                CameraWidth      = viewport3D.Width, // Use same width as viewport3D so we get 1:1 scale
                TargetViewport3D = viewport3D
            };

            var polyLineVisual3D = new PolyLineVisual3D()
            {
                Positions = new Point3DCollection(new Point3D[]
                {
                    //new Point3D(-100,   0, -50),
                    //new Point3D(100, 0, -50),
                    //new Point3D(100, 0, 50),
                    //new Point3D(50, 0, 0),
                    //new Point3D(-100,   0, 50),
                    
                    new Point3D(-100, -50, 0),
                    new Point3D(100,  -50, 0),
                    new Point3D(100,  50,  0),
                    new Point3D(50,   0,    0),
                    new Point3D(-100, 50, 0),
                }),
                LineColor     = Colors.Green,
                LineThickness = 30,

                // NOTE:
                // If you require that each line segment uses exactly 4 positions,
                // then you need to disable turning mitered joints into beveled joints
                // (beveled joints have 3 additional positions that cut the sharp joint).
                // It is not possible to know in advance how many bevel joints will be
                // because this also depends on the angle of the camera.
                // To disable creating beveled joints set MiterLimit to some high number
                // (for example the following will create a beveled joint when the joint length is 100 times the line thickness).
                //MiterLimit = 100
            };

            viewport3D.Children.Add(polyLineVisual3D);


            targetPositionCamera.Refresh();
            Ab3d.Utilities.LinesUpdater.Instance.Refresh(); // Force regeneration of all 3D lines


            var geometryModel3D = polyLineVisual3D.Content as GeometryModel3D;

            if (geometryModel3D != null)
            {
                var lineMesh = (MeshGeometry3D)geometryModel3D.Geometry;

                _shownLineModel3D             = new GeometryModel3D();
                _shownLineModel3D.Geometry     = lineMesh;
                _shownLineModel3D.Material     = new DiffuseMaterial(Brushes.LightGray);
                _shownLineModel3D.BackMaterial = new DiffuseMaterial(Brushes.Black);

                if (BillboardCheckbox.IsChecked ?? false)
                    ApplyBillboardMatrix();

                MainViewport.Children.Add(_shownLineModel3D.CreateContentVisual3D());

                MeshInspector.MeshGeometry3D = lineMesh;
            }
        }

        // Billboard effect orients the mesh so that it is always turned towards the camera
        private void ApplyBillboardMatrix()
        {
            Matrix3D view, proj;
            bool isMatrixValid = Camera1.GetCameraMatrices(out view, out proj);

            if (!isMatrixValid)
                return;

            // To create a billboard effect, we invert the camera's view matrix and reset the offset components
            view.Invert();
            view.OffsetX = 0;
            view.OffsetY = 0;
            view.OffsetZ = 0;

            var matrixTransform3D = _shownLineModel3D.Transform as MatrixTransform3D;
            if (matrixTransform3D != null && !matrixTransform3D.IsFrozen)
            {
                matrixTransform3D.Matrix = view;
            }
            else
            {
                _shownLineModel3D.Transform = new MatrixTransform3D(view);
                MeshInspector.Transform     = _shownLineModel3D.Transform;
            }
        }

        private void OnBillboardCheckboxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (BillboardCheckbox.IsChecked ?? false)
            {
                ApplyBillboardMatrix();
            }
            else
            {
                _shownLineModel3D.Transform = null;
                MeshInspector.Transform     = null;
            }
        }
    }
}
