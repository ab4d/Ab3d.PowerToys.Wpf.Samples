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
using Ab3d.Meshes;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for ModelScalarSample.xaml
    /// </summary>
    public partial class ModelScalarSample : Page
    {

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // !!!  IMPORTANT for using this sample inside Ab3d.DXEngine  !!!
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //
        // When ModelScalarVisual3D is used inside DXEngine, the mouse events on UIElement3D objects that are used inside ModelScalarVisual3D will not work.
        // See the ModelMoverInsideObjectSample sample that comes with Ab3d.DXEngine samples project on how to use ModelScalarVisual3D inside DXEngine.
        //

        private static Random _rnd = new Random();

        private readonly Ab3d.Utilities.EventManager3D _eventManager;

        private readonly DiffuseMaterial _normalMaterial;
        private readonly DiffuseMaterial _selectedMaterial;

        private Ab3d.UIElements.BoxUIElement3D _selectedBoxModel;

        private ScaleTransform3D _scaleTransform3D;

        private double _startScaleX;
        private double _startScaleY;
        private double _startScaleZ;


        public ModelScalarSample()
        {
            InitializeComponent();

            _normalMaterial = new DiffuseMaterial(Brushes.Silver);
            _selectedMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(150, 192, 192, 192))); // semi-transparent Silver

            _eventManager = new Ab3d.Utilities.EventManager3D(MainViewport);


            // Setup events on ModelScalarVisual3D
            SelectedModelScalar.ModelScaleStarted += delegate (object sender, EventArgs args)
            {
                if (_selectedBoxModel == null)
                    return;

                _scaleTransform3D = GetScaleTransform3D(_selectedBoxModel, ensureScaleTransform3D: true);

                if (_scaleTransform3D == null)
                    return;

                _startScaleX = _scaleTransform3D.ScaleX;
                _startScaleY = _scaleTransform3D.ScaleY;
                _startScaleZ = _scaleTransform3D.ScaleZ;
            };

            SelectedModelScalar.ModelScaled += delegate (object sender, ModelScaledEventArgs args)
            {
                if (_selectedBoxModel == null || _scaleTransform3D == null)
                    return;

                _scaleTransform3D.ScaleX = _startScaleX * args.ScaleX;
                _scaleTransform3D.ScaleY = _startScaleY * args.ScaleY;
                _scaleTransform3D.ScaleZ = _startScaleZ * args.ScaleZ;
            };

            SelectedModelScalar.ModelScaleEnded += delegate (object sender, EventArgs args)
            {
                // Nothing to do here in this sample
                // The event handler is here only for description purposes
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

                // Create Transform3DGroup that will hold the 
                var transform3DGroup = new Transform3DGroup();
                transform3DGroup.Children.Add(new TranslateTransform3D(_rnd.NextDouble() * 400 - 200, _rnd.NextDouble() * 40 - 20, _rnd.NextDouble() * 400 - 200));

                boxModel.Transform = transform3DGroup;

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

            var boxPosition = GetBoxPosition(_selectedBoxModel);
            SelectedModelScalar.Position = boxPosition;
        }

        // Get position from the last TranslateTransform3D in Transform3DGroup
        private Point3D GetBoxPosition(Visual3D visual3D)
        {
            var transform3DGroup = visual3D.Transform as Transform3DGroup;
            if (transform3DGroup != null)
            {
                var translateTransform3D = transform3DGroup.Children[transform3DGroup.Children.Count - 1] as TranslateTransform3D;

                if (translateTransform3D != null)
                    return new Point3D(translateTransform3D.OffsetX, translateTransform3D.OffsetY, translateTransform3D.OffsetZ);
            }

            return new Point3D();
        }

        private ScaleTransform3D GetScaleTransform3D(Visual3D visual3D, bool ensureScaleTransform3D = true)
        {
            var scaleTransform3D = visual3D.Transform as ScaleTransform3D;

            if (scaleTransform3D != null)
                return scaleTransform3D;

            if (visual3D.Transform == null)
            {
                if (ensureScaleTransform3D)
                {
                    scaleTransform3D = new ScaleTransform3D();
                    visual3D.Transform = scaleTransform3D;
                }
                else
                {
                    scaleTransform3D = null;
                }
            }
            else
            {
                var transformGroup = visual3D.Transform as Transform3DGroup;
                if (transformGroup != null)
                {
                    if (ensureScaleTransform3D)
                    {
                        scaleTransform3D = new ScaleTransform3D();
                        transformGroup.Children.Insert(0, scaleTransform3D); // Insert scale transform before other transformations (especially before translate because if translete is before scale, then scale also scales the translate amount)
                    }
                    else
                    {
                        scaleTransform3D = null;
                    }
                }
                else
                {
                    if (ensureScaleTransform3D)
                    {
                        scaleTransform3D = new ScaleTransform3D();

                        var transform3DGroup = new Transform3DGroup();

                        transform3DGroup.Children.Add(scaleTransform3D);
                        transform3DGroup.Children.Add(visual3D.Transform);

                        visual3D.Transform = transform3DGroup;
                    }
                }
            }

            return scaleTransform3D;
        }
    }
}
