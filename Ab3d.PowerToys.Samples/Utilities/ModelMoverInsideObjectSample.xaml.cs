﻿//#define USE_GENERIC_MODEL3D // Uncomment define to use the more generic 3D models instead of BoxUIElement3D

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
using Ab3d.Common.EventManager3D;
using Ab3d.Controls;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for ModelMoverInsideObjectSample.xaml
    /// </summary>
    public partial class ModelMoverInsideObjectSample : Page
    {

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // !!!  IMPORTANT for using this sample inside Ab3d.DXEngine  !!!
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //
        // When MouseMoverVisual3D is used inside DXEngine, the mouse events on UIElement3D objects that are used inside MouseMoverVisual3D will not work.
        // See the ModelMoverInsideObjectSample sample that comes with Ab3d.DXEngine samples project on how to use MouseMoverVisual3D inside DXEngine.
        //

        private static Random _rnd = new Random();

        private readonly Ab3d.Utilities.EventManager3D _eventManager;

        private readonly DiffuseMaterial _normalMaterial;
        private readonly DiffuseMaterial _selectedMaterial;

        private ModelMoverVisual3D _modelMover;

        private Ab3d.UIElements.BoxUIElement3D _selectedBoxModel;

        private Point3D _startMovePosition;


        public ModelMoverInsideObjectSample()
        {
            InitializeComponent();

            _normalMaterial = new DiffuseMaterial(Brushes.Silver);
            _selectedMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(150, 192, 192, 192))); // semi-transparent Silver

            _eventManager = new Ab3d.Utilities.EventManager3D(MainViewport);
            
            CreateRandomScene();
        }

        private void CreateRandomScene()
        {
            SceneObjectsContainer.Children.Clear();
            //SceneObjectsContainer.Transform = new ScaleTransform3D(0.5, 0.5, 0.5);

            for (int i = 0; i < 10; i++)
            {
                var boxModel = new Ab3d.UIElements.BoxUIElement3D()
                {
                    CenterPosition = new Point3D(_rnd.NextDouble() * 400 - 200, _rnd.NextDouble() * 40 - 20, _rnd.NextDouble() * 400 - 200),
                    Size = new Size3D(50, 20, 50),
                    Material = _normalMaterial
                };

                SceneObjectsContainer.Children.Add(boxModel);


                // Use EventManager from Ab3d.PowerToys to add support for click event on the box model
                var visualEventSource3D = new Ab3d.Utilities.VisualEventSource3D(boxModel);
                visualEventSource3D.MouseClick += delegate(object sender, MouseButton3DEventArgs e)
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

            // To render the transpant object correctly, we need to sort the objects so that the transparent objects are rendered after other objects
            // We can use the TransparencySorter from Ab3d.PowerToys
            // Note that it is also possible to use TransparencySorter with many advanced features - see the Model3DTransparencySortingSample for more info
            TransparencySorter.SimpleSort(SceneObjectsContainer.Children);

            // In our simple case (we have only one transparent object), we could also manually "sort" the objects with moving the transparent object to the back of the Children collection:
            //SceneObjectsContainer.Children.Remove(_selectedBoxVisual3D);
            //SceneObjectsContainer.Children.Add(_selectedBoxVisual3D);


            if (_modelMover == null)
                SetupModelMover();
            
#if !USE_GENERIC_MODEL3D
            _modelMover.Position = _selectedBoxModel.CenterPosition;
#else
            _startMovePosition = GetSelectedModelCenter();
            _modelMover.Position = _startMovePosition;
#endif


            // Tell ModelDecoratorVisual3D which Model3D to show
            SelectedModelDecorator.TargetModel3D = _selectedBoxModel.Model;

            // NOTE:
            // When the 3D models are organized into hierarchy of models with using different ModelVisual3D or Model3DGroup objects, 
            // you also need to so specify the SelectedModelDecorator.RootModelVisual3D in order to get the correct position of the TargetModel3D
        }



        // Called when a 3D model is selected
        // It add ModelMover to the scene and sets its event handlers
        private void SetupModelMover()
        {
            // Creat a new ModelMover
            _modelMover = new ModelMoverVisual3D();


            // Position ModelMover at the center of selected model
#if !USE_GENERIC_MODEL3D            
            _modelMover.Position = _selectedBoxModel.CenterPosition;
#else
            _modelMover.Position = GetSelectedModelCenter();
#endif

            // Calculate axis length from model size
            var modelBounds = _selectedBoxModel.Model.Bounds; // Because we know that we are using BoxVisual3D, we could also use _selectedBoxVisual3D.Size; But using Bounds is more generic
            double axisLength = Math.Max(modelBounds.Size.X, Math.Max(modelBounds.Size.Y, modelBounds.Size.Z));

            _modelMover.AxisLength = axisLength;

            // Set AxisRadius and AxisArrowRadius based on axis length
            _modelMover.AxisRadius = axisLength / 30;
            _modelMover.AxisArrowRadius = _modelMover.AxisRadius * 3;

            UpdatedShownAxes();


            // Setup event handlers
            _modelMover.ModelMoveStarted += delegate(object o, EventArgs eventArgs)
            {
                if (_selectedBoxModel == null)
                    return;

#if !USE_GENERIC_MODEL3D
                _startMovePosition = _selectedBoxModel.CenterPosition;
                _modelMover.Position = _startMovePosition;
#else
                // When the move starts we create a new TranslateTransform3D that will be used to move the model.
                _currentTranslateTransform3D = new TranslateTransform3D();

                // The new TranslateTransform3D is added to existing transformations on the object (if they exist)

                var currentTransform = _selectedBoxModel.Transform;

                if (currentTransform == null)
                {
                    _selectedBoxModel.Transform = _currentTranslateTransform3D;
                }
                else
                {
                    // transformation already exist

                    // Check if we already have Transform3DGroup
                    var currentTransformGroup = currentTransform as Transform3DGroup;
                    if (currentTransformGroup == null)
                    {
                        // Create new Transform3DGroup and add existing Transformation to the new Group
                        currentTransformGroup = new Transform3DGroup();
                        currentTransformGroup.Children.Add(_selectedBoxModel.Transform);

                        _selectedBoxModel.Transform = currentTransformGroup;
                    }

                    currentTransformGroup.Children.Add(_currentTranslateTransform3D);
                }

                // Move the ModelMover to current model center
                _startMovePosition = GetSelectedModelCenter();
                _modelMover.Position = _startMovePosition;
#endif

                // When MouseCameraController uses left mouse button to rotate the camera,
                // then we need to disable it when we start moving the ModelMover otherwise we would also rotate the camera.
                if (MouseCameraController1.RotateCameraConditions == MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed)
                    MouseCameraController1.IsEnabled = false;
            };

            _modelMover.ModelMoved += delegate(object o, Ab3d.Common.ModelMovedEventArgs e)
            {
                if (_selectedBoxModel == null)
                    return;

                var newCenterPosition = _startMovePosition + e.MoveVector3D;

                if (Math.Abs(newCenterPosition.X) > 2000 ||
                    Math.Abs(newCenterPosition.Y) > 2000 ||
                    Math.Abs(newCenterPosition.Z) > 2000)
                {
                    InfoTextBlock.Text = "Move out of range";
                    return;
                }

#if !USE_GENERIC_MODEL3D
                _selectedBoxModel.CenterPosition = newCenterPosition;
                _modelMover.Position = newCenterPosition;
#else
                // When model is moved we get the updated MoveVector3D
                // We use MoveVector3D to change the _currentTranslateTransform3D that is used on the currently selected model and on the ModelMover object
                _currentTranslateTransform3D.OffsetX = moveVector3D.X;
                _currentTranslateTransform3D.OffsetY = moveVector3D.Y;
                _currentTranslateTransform3D.OffsetZ = moveVector3D.Z;

                _modelMover.Position = newCenterPosition;
#endif

                InfoTextBlock.Text = string.Format("MoveVector3D: {0:0}", e.MoveVector3D);
            };

            _modelMover.ModelMoveEnded += delegate(object sender, EventArgs args)
            {
                InfoTextBlock.Text = "";

                // Enable the MouseCameraController after we finished moving the ModelMover
                MouseCameraController1.IsEnabled = true;
            };


            // Add ModelMover to Viewport3D 
            // We need to insert it before other objects so that the transparent objects are correctly visible (transparent objects must be shown after other objects)
            SceneObjectsContainer.Children.Insert(0, _modelMover); 
        }

        private void OnAxisCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded || _modelMover == null)
                return;

            UpdatedShownAxes();
        }

        private void UpdatedShownAxes()
        {
            _modelMover.IsXAxisShown      = XAxisCheckBox.IsChecked ?? false;
            _modelMover.IsYAxisShown      = YAxisCheckBox.IsChecked ?? false;
            _modelMover.IsZAxisShown      = ZAxisCheckBox.IsChecked ?? false;
            _modelMover.ShowMovablePlanes = ShowMovablePlanesCheckBox.IsChecked ?? false;
        }

        private void OnRotateModelMoverCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (RotateModelMoverCheckBox.IsChecked ?? false)
                _modelMover.SetRotation(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 45)));
            else
                _modelMover.SetRotation(null);
        }

#if USE_GENERIC_MODEL3D
        private Point3D GetSelectedModelCenter()
        {
            var modelBounds = _selectedBoxModel.Model.Bounds; // Because we know that we are using BoxVisual3D, we could also use _selectedBoxVisual3D.Size; But using Bounds is more generic

            var modelCenter = new Point3D(modelBounds.X + modelBounds.SizeX / 2, modelBounds.Y + modelBounds.SizeY / 2, modelBounds.Z + modelBounds.SizeZ / 2);

            if (_selectedBoxModel.Transform != null)
                modelCenter = _selectedBoxModel.Transform.Transform(modelCenter);

            return modelCenter;
        }
#endif
    }
}
