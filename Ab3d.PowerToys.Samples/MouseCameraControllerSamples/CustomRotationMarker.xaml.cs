using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ab3d.Common;
using Ab3d.Controls;

namespace Ab3d.PowerToys.Samples.MouseCameraControllerSamples
{
    /// <summary>
    /// Interaction logic for CustomRotationMarker.xaml
    /// </summary>
    public partial class CustomRotationMarker : Page
    {
        private CustomCameraTargetPositionAdorner _customCameraTargetPositionAdorner;
        private CameraTargetPositionAdorner _adjustedCameraTargetPositionAdorner;

        private MouseCameraController _standardMouseCameraController;

        public CustomRotationMarker()
        {
            InitializeComponent();


            _standardMouseCameraController = new MouseCameraController
            {
                TargetCamera = Camera1,
                EventsSourceElement = ViewportBorder,
                RotateCameraConditions = MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed,
                MoveCameraConditions = MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseCameraController.MouseAndKeyboardConditions.ControlKey,
                ShowRotationCenterMarker = true
            };

            RootGrid.Children.Add(_standardMouseCameraController);


            // To change the setting that are used to create the standard rotation marker,
            // we need to create a new instance of CameraTargetPositionAdorner and change its properties before the marker is shown.
            _adjustedCameraTargetPositionAdorner = new CameraTargetPositionAdorner(ViewportBorder)
            {
                // Default values are commented:
                //Radius = 15,
                //InnerRadius = 11,
                //MainCircleThickness = 2,
                //OuterCircleThickness = 0.4,
                //LinesLength = 10,
                //MainBrush = Brushes.White,
                //InnerBrush = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)), // 50% transparent gray     

                // Changed properties
                Radius = 10,
                InnerRadius = 8,
                MainCircleThickness = 2,
                OuterCircleThickness = 1,
                LinesLength = 8,
                MainBrush = Brushes.Yellow,
                InnerBrush = new SolidColorBrush(Color.FromArgb(128, 255, 255, 128)) // 50% transparent yellow                
            };

            // To provide custom rendering logic for rotation marker, 
            // we need to create a new class that is derived from CameraTargetPositionAdorner and override the OnRender method
            _customCameraTargetPositionAdorner = new CustomCameraTargetPositionAdorner(ViewportBorder)
            {
                Radius = 18,
                InnerRadius = 16,
                LinesLength = 10,
                OuterCircleThickness = 1.5,
            };


            _standardMouseCameraController.RotationCenterAdorner = _customCameraTargetPositionAdorner;

            // NOTE:
            // To show rotation marker, the ShowRotationCenterMarker property on MouseCameraController must be set to true (here this is done in XAML)


            // Additional customization:
            // It is possible to fully customize how the rotation marker is shown with creating a class that is derived from MouseCameraController
            // and then override the ShowRotationAdorner(Point rotationCenterPosition) and HideRotationAdorner() methods. 
            // This way it would be possible to show an object in an overlay Canvas, a 3D axis or something else.
        }

        private void OnRotationMarkerRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            CameraTargetPositionAdorner newCameraTargetPositionAdorner;
            
            if (ChangedStandardRadioButton.IsChecked ?? false)
            {
                newCameraTargetPositionAdorner = _adjustedCameraTargetPositionAdorner;
            }
            else if (CustomRadioButton.IsChecked ?? false)
            {
                newCameraTargetPositionAdorner = _customCameraTargetPositionAdorner;
            }
            else // if (StandardRadioButton.IsChecked ?? false)
            {
                newCameraTargetPositionAdorner = new CameraTargetPositionAdorner(ViewportBorder); // Create a standard marker
            }

            // Set the RotationCenterAdorner for MouseCameraController
            _standardMouseCameraController.RotationCenterAdorner = newCameraTargetPositionAdorner;
        }


        class CustomCameraTargetPositionAdorner : CameraTargetPositionAdorner
        {
            public CustomCameraTargetPositionAdorner(UIElement adornedElement) 
                : base(adornedElement)
            {
            }

            /// <inheritdoc />
            protected override void OnRender(DrawingContext dc)
            {
                double innerThickness = Math.Abs(Radius - InnerRadius);
                double usedInnerRadius = InnerRadius + (innerThickness / 2);

                var mainPen = new Pen(MainBrush, MainCircleThickness);
                var outerPen = new Pen(MainBrush, OuterCircleThickness);
                var innerPen = new Pen(InnerBrush, innerThickness);

                Point position = Position; // get local accessor for DependencyProperty

                // Draw outer circle
                dc.DrawEllipse(null, mainPen, position, Radius, Radius);

                // Draw inner circle (usually used to show marker on bright background)
                dc.DrawEllipse(null, innerPen, position, usedInnerRadius, usedInnerRadius);


                double crossSize = LinesLength;


                // draw three lines from the center position
                dc.DrawLine(outerPen,
                    new Point(position.X, position.Y),
                    new Point(position.X, position.Y - crossSize));

                double dx = Math.Cos(30 * Math.PI / 180.0) * crossSize;
                double dy = Math.Sin(30 * Math.PI / 180.0) * crossSize;

                dc.DrawLine(outerPen,
                    new Point(position.X, position.Y),
                    new Point(position.X + dx, position.Y + dy));

                dc.DrawLine(outerPen,
                    new Point(position.X, position.Y),
                    new Point(position.X - dx, position.Y + dy));
            }
        }
    }
}
