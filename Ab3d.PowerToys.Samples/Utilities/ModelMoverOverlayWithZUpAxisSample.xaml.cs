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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab3d.Common.Cameras;
using Ab3d.Common.EventManager3D;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Utilities
{
    // To use ModelMoverVisual3D with custom coordinate system we need to change 4 things compared to a standard coordinate system:
    // 1) Create ModelMoverVisual3D in code with a constructor that takes takes axes direction as parameters
    // 2) In ModelMoved event handler transform the e.MoveVector3D with inverted ZUpMatrix matrix.
    // 3) In ModelMoved event handler transform the _modelMover.Position with ZUpMatrix matrix. 
    // 4) In SelectObject method transform the _modelMover.Position with ZUpMatrix matrix. 

    /// <summary>
    /// Interaction logic for ModelMoverOverlayWithZUpAxisSample.xaml
    /// </summary>
    public partial class ModelMoverOverlayWithZUpAxisSample : Page
    {
        // ZUpMatrix matrix transforms the standard Y up right-handed coordinate system into Z up right-handed coordinate system
        //
        // The transformation defines the new axis - defined in matrix columns in upper left 3x3 part of the matrix:
        // x axis - 1st column(use x value): 1 0 0 
        // y axis - 2nd column(use negative z value): 0 0 -1
        // z axis - 3rd column(use y value): 0 1 0 

        private static readonly Matrix3D ZUpMatrix = new Matrix3D(1,  0,  0,  0,
                                                                  0,  0, -1,  0,
                                                                  0,  1,  0,  0,
                                                                  0,  0,  0,  1);

        private Transform3D _zUpTransform3D;
        private Transform3D _invertedZUpTransform3D;


        private readonly Ab3d.Utilities.EventManager3D _eventManager;

        private readonly DiffuseMaterial _normalMaterial;

        private Ab3d.Visuals.BoxVisual3D _selectedBoxModel;

        private Point3D _startMovePosition;

        private ModelMoverVisual3D _modelMover;


        public ModelMoverOverlayWithZUpAxisSample()
        {
            // To use custom axes in ModelMoverVisual3D we could also change the default axes direction with the changing the following static fields:
            // This way we could define the ModelMoverVisual3D in XAML.
            // But here we will rather use a ModelMoverVisual3D constructor that takes custom axes as parameters.
            //ModelMoverVisual3D.DefaultXAxis = new Vector3D(1, 0, 0);
            //ModelMoverVisual3D.DefaultYAxis = new Vector3D(0, 0, -1);
            //ModelMoverVisual3D.DefaultZAxis = new Vector3D(0, 1, 0);


            InitializeComponent();


            // Calculate the inverted matrix at startup (used to convert from standard Y up to our Z up coordinate system)
            var invertedZUpMatrix = ZUpMatrix;
            invertedZUpMatrix.Invert();

            _zUpTransform3D         = new MatrixTransform3D(ZUpMatrix);
            _invertedZUpTransform3D = new MatrixTransform3D(invertedZUpMatrix);



            // To define custom axes directions for ModelMoverVisual3D, use the constructor that takes axes direction (note that this cannot be done in XAML)
            _modelMover = new ModelMoverVisual3D(xAxisVector3D: new Vector3D(ZUpMatrix.M11, ZUpMatrix.M12, ZUpMatrix.M13),
                                                 yAxisVector3D: new Vector3D(ZUpMatrix.M21, ZUpMatrix.M22, ZUpMatrix.M23),
                                                 zAxisVector3D: new Vector3D(ZUpMatrix.M31, ZUpMatrix.M32, ZUpMatrix.M33))
            {
                AxisLength      = 50,
                AxisRadius      = 1.5,
                AxisArrowRadius = 5
            };


            // Setup event handlers on ModelMoverVisual3D
            _modelMover.ModelMoveStarted += delegate(object o, EventArgs eventArgs)
            {
                if (_selectedBoxModel == null)
                    return;

                _startMovePosition = _selectedBoxModel.CenterPosition;
            };

            _modelMover.ModelMoved += delegate(object o, Ab3d.Common.ModelMovedEventArgs e)
            {
                if (_selectedBoxModel == null)
                    return;


                // When using custom coordinate system we need to transform the e.MoveVector3D.
                // Because the e.MoveVector3D is defined in standard Y up WPF 3D coordinate system, 
                // we need to convert that into out Z up coordinate system.
                // This is done with using the inverted YUpMatrix:
                var transformedMoveVector3D = _invertedZUpTransform3D.Transform(e.MoveVector3D);

                var newCenterPosition = _startMovePosition + transformedMoveVector3D;

                if (Math.Abs(newCenterPosition.X) > 2000 ||
                    Math.Abs(newCenterPosition.Y) > 2000 ||
                    Math.Abs(newCenterPosition.Z) > 2000)
                {
                    InfoTextBlock.Text = "Move out of range";
                    return;
                }

                _selectedBoxModel.CenterPosition = newCenterPosition;


                var position = GetSelectedModelWorldPosition(); // GetSelectedModelPosition gets the _selectedBoxModel.CenterPosition and transforms it with the transformations of parent ModelVisual3D objects

                // Because ModelMoverVisual3D is in a separate Viewport3D that does not use the ZUpMatrix transformation,
                // we need to transform the position from z up coordinate system into the standard WPF 3D coordinate system:
                _modelMover.Position = _zUpTransform3D.Transform(position);


                InfoTextBlock.Text = string.Format("MoveVector3D: {0:0}", e.MoveVector3D);
            };

            _modelMover.ModelMoveEnded += delegate(object sender, EventArgs args)
            {
                InfoTextBlock.Text = "";
            };

            RootOverlayModelVisual3D.Children.Add(_modelMover);

            

            // Update the axes for CameraAxisPanel
            CustomCameraAxisPanel1.CustomizeAxes(new Vector3D(1, 0, 0), "X", Colors.Red,
                                                 new Vector3D(0, 1, 0), "Z", Colors.Blue,
                                                 new Vector3D(0, 0, -1), "Y", Colors.Green);



            // Set the ZUpMatrix to transform all the shown objects.
            // This way the objects will use the Z up matrix, but before rendering this will be transformed into the standard WPF 3D y up coordinate system.
            ZUpRootVisual.Transform = _zUpTransform3D;


            // We need to synchronize the Camera and Lights in OverlayViewport with the camera in the MainViewport
            Camera1.CameraChanged += delegate (object s, CameraChangedRoutedEventArgs args)
            {
                OverlayViewport.Camera = MainViewport.Camera;
                OverlayViewportLight.Direction = ((DirectionalLight)Camera1.CameraLight).Direction;
            };

            
            _normalMaterial = new DiffuseMaterial(Brushes.Silver);
            _eventManager = new Ab3d.Utilities.EventManager3D(MainViewport);

            CreateRandomScene();
        }

        private void CreateRandomScene()
        {
            var rnd = new Random();

            SceneObjectsContainer.Children.Clear();

            for (int i = 0; i < 10; i++)
            {
                var boxModel = new Ab3d.Visuals.BoxVisual3D()
                {
                    // NOTE: y is into the screen any z is up
                    CenterPosition = new Point3D(rnd.NextDouble() * 400 - 200, rnd.NextDouble() * 400 - 200, rnd.NextDouble() * 40 - 20),
                    Size           = new Size3D(50, 50, 20),
                    Material       = _normalMaterial
                };

                SceneObjectsContainer.Children.Add(boxModel);


                // Use EventManager from Ab3d.PowerToys to add support for click event on the box model
                var visualEventSource3D = new Ab3d.Utilities.VisualEventSource3D(boxModel);
                visualEventSource3D.MouseClick += delegate(object sender, MouseButton3DEventArgs e)
                {
                    var selectedBoxModel = e.HitObject as Ab3d.Visuals.BoxVisual3D;
                    SelectObject(selectedBoxModel);
                };

                _eventManager.RegisterEventSource3D(visualEventSource3D);


                // Automatically select first box
                if (_selectedBoxModel == null)
                    SelectObject(boxModel);
            }
        }

        public void SelectObject(Ab3d.Visuals.BoxVisual3D selectedBox)
        {
            _selectedBoxModel = selectedBox;
            if (_selectedBoxModel == null)
                return;

            var position = GetSelectedModelWorldPosition(); // GetSelectedModelPosition gets the _selectedBoxModel.CenterPosition and transforms it with the transformations of parent ModelVisual3D objects

            // Because ModelMoverVisual3D is in a separate Viewport3D that does not use the ZUpMatrix transformation,
            // we need to transform the position from z up coordinate system into the standard WPF 3D coordinate system:
            _modelMover.Position = _zUpTransform3D.Transform(position);
            
            // Tell ModelDecoratorVisual3D which Model3D to show
            SelectedModelDecorator.TargetModel3D = _selectedBoxModel.Content;
            
            // NOTE:
            // When the 3D models are organized into hierarchy of models with using different ModelVisual3D or Model3DGroup objects, 
            // you also need to so specify the SelectedModelDecorator.RootModelVisual3D in order to get the correct position of the TargetModel3D
        }

        // Gets the position of the selected model in world coordinates (not local)
        private Point3D GetSelectedModelWorldPosition()
        {
            if (_selectedBoxModel == null)
                return new Point3D();

            // Start with _selectedBoxModel.CenterPosition (local coordinates)
            Point3D selectedModelPosition = _selectedBoxModel.CenterPosition;

            // Transform the position with the transformation of parent ModelVisual3D (world coordinates)
            // If there are more parent ModelVisual3D objects in the hierarchy, then you need to transform the position for each of the parent.
            if (RootModelVisual3D.Transform != null && !RootModelVisual3D.Transform.Value.IsIdentity)
                selectedModelPosition = RootModelVisual3D.Transform.Transform(selectedModelPosition);

            return selectedModelPosition;
        }
    }
}
