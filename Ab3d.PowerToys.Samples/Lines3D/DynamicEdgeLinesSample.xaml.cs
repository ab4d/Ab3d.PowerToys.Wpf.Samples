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
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    /// <summary>
    /// Interaction logic for DynamicEdgeLinesSample.xaml
    /// </summary>
    public partial class DynamicEdgeLinesSample : Page, ICompositionRenderingSubscriber
    {
        private AxisAngleRotation3D _baseAxisAngleRotation3D;
        private AxisAngleRotation3D _joint2AxisAngleRotation3D;

        private DateTime _startTime;
        private Model3D _robotArmModel3D;

        public DynamicEdgeLinesSample()
        {
            InitializeComponent();

            AssimpLoader.LoadAssimpNativeLibrary();

            LoadRobotArm();

            this.Unloaded += delegate(object sender, RoutedEventArgs args)
            {
                Ab3d.Utilities.CompositionRenderingHelper.Instance.Unsubscribe(this);
            };
        }

        private void LoadRobotArm()
        {
            string fileName = AppDomain.CurrentDomain.BaseDirectory + @"Resources\robotarm.3ds";

            // Create an instance of AssimpWpfImporter
            var assimpWpfImporter = new AssimpWpfImporter();
            _robotArmModel3D = assimpWpfImporter.ReadModel3D(fileName, texturesPath: null);

            // In VS Immediate window call "readModel3D.DumpHierarchy()" to get hierarchy and names of the objects
            var baseModel3D = assimpWpfImporter.NamedObjects["Base"] as Model3D;
            var joint2Model3D = assimpWpfImporter.NamedObjects["Joint2"] as Model3D;

            _baseAxisAngleRotation3D   = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
            _joint2AxisAngleRotation3D = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);

            // Add RotateTransform3D to existing transformations
            var transform3DGroup = new Transform3DGroup();
            transform3DGroup.Children.Add(new RotateTransform3D(_baseAxisAngleRotation3D));
            transform3DGroup.Children.Add(baseModel3D.Transform);

            baseModel3D.Transform = transform3DGroup;
            
            transform3DGroup = new Transform3DGroup();
            transform3DGroup.Children.Add(new RotateTransform3D(_joint2AxisAngleRotation3D));
            transform3DGroup.Children.Add(joint2Model3D.Transform);

            joint2Model3D.Transform = transform3DGroup;

            ModelRootVisual3D.Children.Add(_robotArmModel3D.CreateModelVisual3D());


            // NOTES:
            // Call SetEdgeLinesForEachGeometryModel3D or static CreateEdgeLinesForEachGeometryModel3D to generate MultiLineVisual3D
            // with edge lines for each GeometryModel3D in the _robotArmModel3D hierarchy.
            // The MultiLineVisual3D are added to the EdgeLinesRootVisual3D.
            // The created MultiLineVisual3D will be set as EdgeLinesFactory.EdgeMultiLineVisual3DProperty to each GeometryModel3D.
            // This way we will be able to update the Transformation in the MultiLineVisual3D after the transformation in the model is changed.
            // This is done in the OnRendering method.
            // 
            // If the model is not animated or otherwise transformed, then it is recommended to call GetEdgeLines method
            // that will create static edge lines. See StaticEdgeLinesCreationSample for more info.
            
            var edgeLinesFactory = new EdgeLinesFactory();
            edgeLinesFactory.SetEdgeLinesForEachGeometryModel3D(_robotArmModel3D, edgeStartAngleInDegrees: 25, lineThickness: 2, lineColor: Colors.Black, parentModelVisual3D: EdgeLinesRootVisual3D);

            // You can also use a static CreateEdgeLinesForEachGeometryModel3D:
            //EdgeLinesFactory.CreateEdgeLinesForEachGeometryModel3D(_robotArmModel3D, edgeStartAngleInDegrees: 25, lineThickness: 2, lineColor: Colors.Black, parentModelVisual3D: EdgeLinesRootVisual3D);

            SetupAnimation();
        }

        private void SetupAnimation()
        {
            _startTime = DateTime.Now;
            Ab3d.Utilities.CompositionRenderingHelper.Instance.Subscribe(this);
        }

        public void OnRendering(EventArgs e)
        {
            double elapsedSeconds = (DateTime.Now - _startTime).TotalSeconds;

            _baseAxisAngleRotation3D.Angle   = Math.Sin(elapsedSeconds) * 180;
            _joint2AxisAngleRotation3D.Angle = Math.Sin(elapsedSeconds * 2) * 30;
            
            // Iterate through all child GeometryModel3D in _robotArmModel3D
            Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
                _robotArmModel3D,
                null,
                delegate (GeometryModel3D geometryModel3D, Transform3D parentTransform3D)
                {
                    // Get MultiLineVisual3D from the geometryModel3D
                    // (MultiLineVisual3D is created CreateEdgeLinesForEachGeometryModel3D and stored into EdgeLinesFactory.EdgeMultiLineVisual3DProperty DependencyProperty).
                    var multiLineVisual3D = (MultiLineVisual3D)geometryModel3D.GetValue(EdgeLinesFactory.EdgeMultiLineVisual3DProperty);
                    if (multiLineVisual3D != null)
                    {
                        multiLineVisual3D.Transform = parentTransform3D; // update the transformation
                    }
                });
        }
    }
}
