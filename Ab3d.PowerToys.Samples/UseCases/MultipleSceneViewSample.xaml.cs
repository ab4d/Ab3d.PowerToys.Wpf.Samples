using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;
using Ab3d.Cameras;
using Ab3d.ObjFile;
using Ab3d.PowerToys.Samples.Common;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.UseCases
{
    // IMPORTANT NOTE:
    // Drawing 3D lines in WPF 3D is very inefficient (we need to convert each line to 2 triangles and update the triangle each time camera is changed - this is done on CPU)
    // Therefore it is not recommended to show very complex 3D models in multiple views with wireframe
    // To do this efficientls it will be possible to use Ab3d.DXEngine (DirectX 11 rendering engine for WPF 3D) that will be able to render 3D lines with full hardware acceleration (it will be released soon)

    /// <summary>
    /// Interaction logic for MultipleSceneViewSample.xaml
    /// </summary>
    public partial class MultipleSceneViewSample : Page
    {
        private SceneLayout[] _allSceneLayouts;

        private SceneLayout _selectedLayout;

        private Model3D _loadedModel3D;
        private ObjModelVisual3D _objModelVisual3D;

        public MultipleSceneViewSample()
        {
            InitializeComponent();

            SetupViews();
            UpdateLayoutSchemas();

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                LoadObjFile();
            };
        }

        private void SetupViews()
        {
            // Define all possible layouts
            _allSceneLayouts = new SceneLayout[]
            {
                //  * | **
                // ---| **
                //  * | **
                new PerspectiveTopFrontViewLayout(),

                //  * | *
                // ---|---
                //  * | *
                new TopLeftFrontPerspectiveViewLayout(),

                // ** | **
                // ** | **
                // ** | **
                new TwoColumnsViewLayout(), 

                // *******
                // *******
                // *******
                new PerspectiveViewLayout()
            };

            SelectLayout(_allSceneLayouts[0]);
        }

        private void UpdateLayoutSchemas()
        {
            // Create layout schemas for all layouts and show them as ToggleButtons on top of the sample
            foreach (var oneSceneLayout in _allSceneLayouts)
            {
                var layoutSchema = oneSceneLayout.CreateLayoutSchema();
                layoutSchema.SnapsToDevicePixels = true;
                layoutSchema.Width = 70;
                layoutSchema.Height = 36;
                layoutSchema.Margin = new Thickness(5, 5, 5, 5);

                var toggleButton = new ToggleButton();
                toggleButton.VerticalAlignment = VerticalAlignment.Center;
                toggleButton.Margin = new Thickness(0, 0, 10, 0);

                toggleButton.Tag = oneSceneLayout;
                toggleButton.Content = layoutSchema;

                if (oneSceneLayout == _selectedLayout)
                    toggleButton.IsChecked = true;

                toggleButton.Checked += delegate(object sender, RoutedEventArgs args)
                {
                    var checkedToggleButton = (ToggleButton) sender;
                    var layout = (SceneLayout) checkedToggleButton.Tag;

                    SelectLayout(layout);
                };

                LayoutsPanel.Children.Add(toggleButton);
            }
        }

        public void SelectLayout(int layoutIndex)
        {
            SelectLayout(_allSceneLayouts[layoutIndex]);
        }

        private void SelectLayout(SceneLayout layout)
        {
            if (_selectedLayout == layout)
                return;

            layout.ActivateLayout(SceneViewsGrid);
            _selectedLayout = layout;

            foreach (var toggleButton in LayoutsPanel.Children.OfType<ToggleButton>())
            {
                if (toggleButton.Tag != _selectedLayout)
                    toggleButton.IsChecked = false;
            }

            ShowModel(_loadedModel3D);
        }

        private void LoadObjFile()
        {
            // Use ObjModelVisual3D to load robotarm.obj and then set objModelVisual3D.Content (as Model3D) to WireframeVisual
            _objModelVisual3D = new ObjModelVisual3D()
            {
                Source = new Uri("pack://application:,,,/Ab3d.PowerToys.Samples;component/Resources/ObjFiles/robotarm.obj", UriKind.Absolute),
                SizeX = 50,
                Position = new Point3D(0, 0, 0),
                PositionType = ObjModelVisual3D.VisualPositionType.BottomCenter
            };

            _loadedModel3D = _objModelVisual3D.Content;
            ShowModel(_loadedModel3D);

            // We could also use Ab3d.ReaderObj, but this does not give us option to specify object size and position
        }

        private void ShowModel(Model3D model)
        {
            // We need to set the model to all SceneView3D objects
            foreach (var sceneView3D in SceneViewsGrid.Children.OfType<SceneView3D>())
                sceneView3D.Model3D = model;
        }
    }
}
