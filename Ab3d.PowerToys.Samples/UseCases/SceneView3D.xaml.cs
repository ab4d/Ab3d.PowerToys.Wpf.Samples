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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab3d.Cameras;
using Ab3d.Controls;
using Ab3d.PowerToys.Samples.Common;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for SceneView3D.xaml
    /// </summary>
    [ContentProperty("Model3D")] // All content of this UserControl is set to the Model3D property
    public partial class SceneView3D : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isContextMenuInitialized;

        private SceneViewType _selectedSceneViewType;

        public SceneViewType SelectedSceneViewType
        {
            get { return _selectedSceneViewType; }
            set
            {
                _selectedSceneViewType = value;

                if (_selectedSceneViewType != null && _selectedSceneViewType != SceneViewType.StandardCustomSceneView)
                {
                    Camera1.BeginInit(); // We use BeginInit and EndInit to update the camera only once

                    Camera1.Heading = _selectedSceneViewType.Heading;
                    Camera1.Attitude = _selectedSceneViewType.Attitude;

                    Camera1.EndInit();
                }

                OnPropertyChanged("SelectedSceneViewType");
            }
        }

        public Viewport3D Viewport3D
        {
            get { return MainViewport; }
        }

        public TargetPositionCamera Camera
        {
            get { return Camera1; }
        }

        public Controls.MouseCameraController MouseCameraController
        {
            get { return MouseCameraController1; }
        }

        public WireframeVisual3D WireframeVisual3D
        {
            get { return WireframeVisual; }
        }

        public Model3D Model3D
        {
            get { return WireframeVisual.OriginalModel; }
            set { WireframeVisual.OriginalModel = value; }
        }

        public SceneView3D()
        {
            InitializeComponent();

            SetupViews();

            this.Loaded += delegate (object sender, RoutedEventArgs args)
            {
                UpdateViewType();
            };
        }

        private void SetupViews()
        {
            ViewTypeComboBox.ItemsSource = SceneViewType.StandardViews;

            SelectedSceneViewType = SceneViewType.StandardCustomSceneView;

            // When view type is set to perspective we do not update the camera so we do it here manually
            Camera1.Heading = SceneViewType.StandardCustomSceneView.Heading;
            Camera1.Attitude = SceneViewType.StandardCustomSceneView.Attitude;
        }

        // Checks correct RadioButtons in ContextMenu based on the current state of Camera and WireframeVisual
        private void InitializeContextMenu()
        {
            if (Camera1.CameraType == BaseCamera.CameraTypes.PerspectiveCamera)
                PerspectiveCameraCheckBox.IsChecked = true;
            else
                OrthographicCameraCheckBox.IsChecked = true;

            if (WireframeVisual.WireframeType == WireframeVisual3D.WireframeTypes.OriginalSolidModel)
            {
                SolidModelCheckBox.IsChecked = true;
            }
            else if (WireframeVisual.WireframeType == WireframeVisual3D.WireframeTypes.Wireframe)
            {
                if (WireframeVisual.UseModelColor)
                    WireframeOriginalColorsCheckBox.IsChecked = true;
                else
                    WireframeCheckBox.IsChecked = true;
            }
            else if (WireframeVisual.WireframeType == WireframeVisual3D.WireframeTypes.WireframeWithSingleColorSolidModel)
            {
                WireframeHiddenLinesCheckBox.IsChecked = true;
            }
            else if (WireframeVisual.WireframeType == WireframeVisual3D.WireframeTypes.WireframeWithOriginalSolidModel)
            {
                WireframeOriginalColorsCheckBox.IsChecked = true;
            }

            _isContextMenuInitialized = true;
        }

        private void UpdateViewType()
        {
            // Create local values (accessing DependencyProperties can be slow)
            double cameraHeading = Camera1.Heading;
            double cameraAttitude = Camera1.Attitude;

            // Check if the current camera match any view type 
            var matchedViewType = SceneViewType.StandardViews.FirstOrDefault(v => Math.Abs(v.Heading - cameraHeading) < 0.01 &&
                                                                                  Math.Abs(v.Attitude - cameraAttitude) < 0.01);

            if (matchedViewType == null)
                matchedViewType = SceneViewType.StandardCustomSceneView;

            if (SelectedSceneViewType != matchedViewType)
                SelectedSceneViewType = matchedViewType;
        }

        private void CameraChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateViewType();
        }

        private void SettingsButton_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isContextMenuInitialized) // We need to initialize context menu here (if this is done in OnLoaded the settings are not preserved)
                InitializeContextMenu();

            SettingsMenu.Placement = PlacementMode.Bottom;
            SettingsMenu.PlacementTarget = SettingsButton;
            SettingsMenu.IsOpen = true;

            e.Handled = true;
        }

        private void CameraTypeCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var newCameraTypes = (PerspectiveCameraCheckBox.IsChecked ?? false) ? BaseCamera.CameraTypes.PerspectiveCamera : BaseCamera.CameraTypes.OrthographicCamera;

            Camera1.CameraType = newCameraTypes;

            SettingsMenu.IsOpen = false;
        }

        private void RenderingTypeCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            WireframeVisual3D.WireframeTypes selectedWireframeType;


            if (WireframeCheckBox.IsChecked ?? false)
            {
                selectedWireframeType = WireframeVisual3D.WireframeTypes.Wireframe;
                WireframeVisual.UseModelColor = false;
            }
            else if (WireframeHiddenLinesCheckBox.IsChecked ?? false)
            {
                selectedWireframeType = WireframeVisual3D.WireframeTypes.WireframeWithSingleColorSolidModel;
                WireframeVisual.UseModelColor = false;
            }
            else if (WireframeSolidModelCheckBox.IsChecked ?? false)
            {
                selectedWireframeType = WireframeVisual3D.WireframeTypes.WireframeWithOriginalSolidModel;
                WireframeVisual.UseModelColor = false;
            }
            else if (WireframeOriginalColorsCheckBox.IsChecked ?? false)
            {
                selectedWireframeType = WireframeVisual3D.WireframeTypes.Wireframe;
                WireframeVisual.UseModelColor = true;
            }
            else // default and if SolidModelCheckBox.IsChecked ?? false
            {
                selectedWireframeType = WireframeVisual3D.WireframeTypes.OriginalSolidModel;
                WireframeVisual.UseModelColor = true;
            }



            WireframeVisual.WireframeType = selectedWireframeType;

            SettingsMenu.IsOpen = false;
        }


        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
