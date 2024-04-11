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
using Ab3d.Common;
using Ab3d.Common.EventManager3D;
using Ab3d.Controls;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for ModelRotatorWithStandardTransformSample.xaml
    /// </summary>
    public partial class ModelRotatorWithStandardTransformSample : Page
    {

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // !!!  IMPORTANT for using this sample inside Ab3d.DXEngine  !!!
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //
        // When MouseRotatorVisual3D is used inside DXEngine, the mouse events on UIElement3D objects that are used inside MouseMoverVisual3D will not work.
        // See the ModelMoverInsideObjectSample sample that comes with Ab3d.DXEngine samples project on how to use MouseMoverVisual3D inside DXEngine.
        //

        private static Random _rnd = new Random();

        private readonly Ab3d.Utilities.EventManager3D _eventManager;

        private readonly DiffuseMaterial _normalMaterial;
        private readonly DiffuseMaterial _selectedMaterial;

        private Ab3d.UIElements.BoxUIElement3D _selectedBoxModel;

        private StandardTransform3D _standardTransform3D;

        private double _startRotateX, _startRotateY, _startRotateZ;


        public ModelRotatorWithStandardTransformSample()
        {
            InitializeComponent();

            _normalMaterial = new DiffuseMaterial(Brushes.Silver);
            _selectedMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(150, 192, 192, 192))); // semi-transparent Silver

            _eventManager = new Ab3d.Utilities.EventManager3D(MainViewport);



            // Setup events on ModelRotatorVisual3D
            SelectedModelRotator.ModelRotateStarted += delegate (object sender, ModelRotatedEventArgs args)
            {
                if (_selectedBoxModel == null)
                    return;

                // Read the current rotation angles
                _startRotateX = _standardTransform3D.RotateX;
                _startRotateY = _standardTransform3D.RotateY;
                _startRotateZ = _standardTransform3D.RotateZ;

                // When MouseCameraController uses left mouse button to rotate the camera,
                // then we need to disable it when we start rotating the SelectedModelRotator otherwise we would also rotate the camera.
                if (MouseCameraController1.RotateCameraConditions == MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed)
                    MouseCameraController1.IsEnabled = false;
            };

            SelectedModelRotator.ModelRotated += delegate (object sender, ModelRotatedEventArgs args)
            {
                if (_selectedBoxModel == null || _standardTransform3D == null)
                    return;

                // Make the rotation based on the RotationAxis
                if (args.RotationAxis == ModelRotatorVisual3D.XRotationAxis)
                    _standardTransform3D.RotateX = _startRotateX + args.RotationAngle;

                else if (args.RotationAxis == ModelRotatorVisual3D.YRotationAxis)
                    _standardTransform3D.RotateY = _startRotateY + args.RotationAngle;

                else if (args.RotationAxis == ModelRotatorVisual3D.ZRotationAxis)
                    _standardTransform3D.RotateZ = _startRotateZ + args.RotationAngle;
            };

            SelectedModelRotator.ModelRotateEnded += delegate (object sender, ModelRotatedEventArgs args)
            {
                // Enable the MouseCameraController after we finished rotating the SelectedModelRotator
                MouseCameraController1.IsEnabled = true;
            };

            CreateRandomScene();
        }

        private void CreateRandomScene()
        {
            SceneObjectsContainer.Children.Clear();

            for (int i = 0; i < 10; i++)
            {
                // Create simple box that user will be able to rotate
                // In order to support rotation, we need to create the box at (0,0,0)
                // and then after performing rotation, translate the object to its final location.
                // If we would create the object at its final rotation (basically applying translation before rotation), 
                // then the box would not be rotated around its center but around the coordinate axes center.
                var boxModel = new Ab3d.UIElements.BoxUIElement3D()
                {
                    CenterPosition = new Point3D(0, 0, 0),
                    Size = new Size3D(50, 20, 50),
                    Material = _normalMaterial
                };


                // Create a StandardTransform3D
                // StandardTransform3D is a class that generates a MatrixTransform3D based on the translate, rotate and scale transform.
                // Note that because it is not possible to derive from WPF's Transform3D, the StandardTransform3D is a standalone class
                // that provides its own MatrixTransform3D object that can be assigned to the 3D model.
                // Its static SetStandardTransform3D and GetStandardTransform3D use a custom StandardTransform3DProperty
                // to "attach" the StandardTransform3D object to Model3D or Visual3D. But this does not automatically set the
                // object's transformation. 
                var standardTransform3D = new StandardTransform3D()
                {
                    TranslateX = _rnd.NextDouble() * 400 - 200,
                    TranslateY = _rnd.NextDouble() * 40 - 20,
                    TranslateZ = _rnd.NextDouble() * 400 - 200,
                };

                // SetStandardTransform3D method will set the StandardTransform3DProperty to the standardTransform3D
                // The updateTransform3D argument will also set the boxModel.Transform to standardTransform3D.Transform
                StandardTransform3D.SetStandardTransform3D(boxModel, standardTransform3D, updateTransform3D: true);


                SceneObjectsContainer.Children.Add(boxModel);


                // Use EventManager from Ab3d.PowerToys to add support for click event on the box model
                var visualEventSource3D = new Ab3d.Utilities.VisualEventSource3D(boxModel);
                visualEventSource3D.MouseClick += delegate (object sender, MouseButton3DEventArgs e)
                {
                    var selectedBoxModel = e.HitObject as Ab3d.UIElements.BoxUIElement3D;
                    SelectObject(selectedBoxModel);
                };

                _eventManager.RegisterEventSource3D(visualEventSource3D);


                // Automatically select first box
                if (_selectedBoxModel == null)
                {
                    boxModel.Refresh(); // Force creating the model
                    SelectObject(boxModel);
                }
            }
        }

        public void SelectObject(Ab3d.UIElements.BoxUIElement3D selectedBox)
        {
            // Deselect currently selected model
            if (_selectedBoxModel != null)
            {
                // Set material back to normal
                _selectedBoxModel.Material = _normalMaterial;
                _selectedBoxModel.BackMaterial = null;

                // Allow hit testing again - so user can select that object again
                _selectedBoxModel.IsHitTestVisible = true;

                _selectedBoxModel = null;
            }

            _selectedBoxModel = selectedBox;
            if (_selectedBoxModel == null)
                return;


            // Prevent hit-testing in selected model
            // This will allow clicking on the parts of move arrows that are inside the selected model
            // Note that IsHitTestVisible is available only on models derived from UIElement3D (if you need that on GeometryModel3D or ModelVisual3D, then use ModelUIElement3D as parent of your model)
            _selectedBoxModel.IsHitTestVisible = false;


            // Change material to semi-transparent Silver
            _selectedBoxModel.Material = _selectedMaterial;
            _selectedBoxModel.BackMaterial = _selectedMaterial; // We also set BackMaterial so the inner side of boxes will be visible

            // To render the transparent object correctly, we need to sort the objects so that the transparent objects are rendered after other objects
            // We can use the TransparencySorter from Ab3d.PowerToys
            // Note that it is also possible to use TransparencySorter with many advanced features - see the Model3DTransparencySortingSample for more info
            TransparencySorter.SimpleSort(SceneObjectsContainer.Children);

            // In our simple case (we have only one transparent object), we could also manually "sort" the objects with moving the transparent object to the back of the Children collection:
            //SceneObjectsContainer.Children.Remove(_selectedBoxVisual3D);
            //SceneObjectsContainer.Children.Add(_selectedBoxVisual3D);

            _standardTransform3D = StandardTransform3D.GetStandardTransform3D(_selectedBoxModel);
            SelectedModelRotator.Transform = _standardTransform3D.Transform;
        }
    }
}
