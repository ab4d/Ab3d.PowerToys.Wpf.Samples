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
using Ab3d.Cameras;
using Ab3d.Controls;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.MouseCameraControllerSamples
{
    /// <summary>
    /// Interaction logic for CustomRotation3DMarker.xaml
    /// </summary>
    public partial class CustomRotation3DMarker : Page
    {
        private CustomMouseCameraController _customMouseCameraController;

        public CustomRotation3DMarker()
        {
            InitializeComponent();


            // The easiest way to show highly customized rotation marker
            // is to derive new class from MouseCameraController and  
            // override the ShowRotationAdorner and the HideRotationAdorner methods.
            //
            // In this sample we show WireBoxVisual3D around selected box (when there is a box selected).
            // If no box is selected, we show WireCrossVisual3D at the RotationCenterPosition (if set).
            _customMouseCameraController = new CustomMouseCameraController()
            {
                TargetCamera = Camera1,
                EventsSourceElement = ViewportBorder,
                RotateCameraConditions = MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed,
                MoveCameraConditions = MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseCameraController.MouseAndKeyboardConditions.ControlKey,

                ShowRotationCenterMarker = true, // IMPORTANT: To show rotation marker, the ShowRotationCenterMarker must be set to true
                RotateAroundMousePosition = true
            };

            // Our CustomMouseCameraController have 2 additional properties:
            _customMouseCameraController.Viewport3D = MainViewport;
            _customMouseCameraController.SelectedBoxVisual3D = null; // No box selected at startup - we start with RotateAroundMousePosition set to true

            MainGrid.Children.Add(_customMouseCameraController);
        }

        private void RotationCenterPositionRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            Point3D? rotationCenterPosition;
            BoxVisual3D selectedBoxVisual3D;

            if (YellowBoxRadioButton.IsChecked ?? false)
                selectedBoxVisual3D = YellowBox;
            else if (OrangeBoxRadioButton.IsChecked ?? false)
                selectedBoxVisual3D = OrangeBox;
            else
                selectedBoxVisual3D = null;

            bool rotateAroundMousePosition = (MousePositionAutoBoxRadioButton.IsChecked ?? false);

            if (selectedBoxVisual3D != null)
                rotationCenterPosition = selectedBoxVisual3D.CenterPosition;
            else
                rotationCenterPosition = null;

            _customMouseCameraController.SelectedBoxVisual3D = selectedBoxVisual3D;
            _customMouseCameraController.RotateAroundMousePosition = rotateAroundMousePosition;

            Camera1.RotationCenterPosition = rotationCenterPosition;
        }
    }


    public class CustomMouseCameraController : MouseCameraController
    {
        private WireBoxVisual3D _wireBoxVisual3D;
        private WireCrossVisual3D _wireCrossVisual3D;

        public Viewport3D Viewport3D { get; set; }

        public BoxVisual3D SelectedBoxVisual3D { get; set; }

        public override void ShowRotationAdorner(Point rotationCenterPosition)
        {
            if (Viewport3D != null)
            {
                // In this sample we show WireBoxVisual3D around selected box (when there is a box selected)
                if (SelectedBoxVisual3D != null) 
                {
                    _wireBoxVisual3D = new WireBoxVisual3D()
                    {
                        LineColor = Colors.Red,
                        LineThickness = 3,
                        CenterPosition = SelectedBoxVisual3D.CenterPosition,
                        Size = SelectedBoxVisual3D.Size
                    };

                    Viewport3D.Children.Add(_wireBoxVisual3D);

                    return;
                }

                // If no box is selected, we show WireCrossVisual3D at the RotationCenterPosition (if set)

                // When MouseCameraController.RotateAroundMousePosition is set to true,
                // MouseCameraController calculates the RotationCenterPosition with hit testing current mouse position on the 3D scene.
                var targetPositionCamera = TargetCamera as TargetPositionCamera;
                if (targetPositionCamera != null && targetPositionCamera.RotationCenterPosition != null)
                {
                    _wireCrossVisual3D = new WireCrossVisual3D()
                    {
                        LineColor = Colors.Red,
                        LineThickness = 3,
                        LinesLength = 30,
                        Position = targetPositionCamera.RotationCenterPosition.Value
                    };

                    Viewport3D.Children.Add(_wireCrossVisual3D);

                    return;
                }
            }

            // If we came here then Viewport3D and SelectedBoxVisual3D are not set or there is no 3D object behind the mouse position (RotationCenterPosition is null)
            // In this case we show standard rotation marker
            base.ShowRotationAdorner(rotationCenterPosition);
        }

        public override void HideRotationAdorner()
        {
            if (Viewport3D != null)
            {
                if (_wireBoxVisual3D != null)
                {
                    Viewport3D.Children.Remove(_wireBoxVisual3D);
                    _wireBoxVisual3D = null;

                    return;
                }

                if (_wireCrossVisual3D != null)
                {
                    Viewport3D.Children.Remove(_wireCrossVisual3D);
                    _wireCrossVisual3D = null;

                    return;
                }
            }

            base.HideRotationAdorner();
        }
    }
}
