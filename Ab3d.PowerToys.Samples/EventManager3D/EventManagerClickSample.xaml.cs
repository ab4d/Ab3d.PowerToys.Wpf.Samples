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

namespace Ab3d.PowerToys.Samples.EventManager3D
{
    /// <summary>
    /// Interaction logic for EventManagerClickSample.xaml
    /// </summary>
    public partial class EventManagerClickSample : Page
    {
        private Ab3d.Utilities.EventManager3D _eventManager3D;

        private bool _isSelectedBoxClicked;

        private double _totalClickedHeight;

        private DiffuseMaterial _normalMaterial = new DiffuseMaterial(System.Windows.Media.Brushes.Silver);
        private DiffuseMaterial _selectedMaterial = new DiffuseMaterial(System.Windows.Media.Brushes.Orange);
        private DiffuseMaterial _clickedMaterial = new DiffuseMaterial(System.Windows.Media.Brushes.Red);


        public EventManagerClickSample()
        {
            InitializeComponent();

            Setup3DObjects();

            // EventManager3D checks the hit 3D object only on mouse events.
            // But when camera is changed, this can also change the 3D object that is behind the mouse.
            // To support that can subscribe to CameraChanged and in event handler call UpdateHitObjects method.
            Camera1.CameraChanged += delegate(object sender, CameraChangedRoutedEventArgs args)
            {
                _eventManager3D.UpdateHitObjects();
            };
        }

        private void AnimateButton_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleCameraAnimation();
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var boxVisual3D in MainViewport.Children.OfType<Ab3d.Visuals.BoxVisual3D>())
                boxVisual3D.Material = _normalMaterial;

            _totalClickedHeight = 0;
            UpdateTotalClickedHeightText();
        }

        private void ToggleCameraAnimation()
        {
            if (Camera1.IsRotating)
            {
                Camera1.StopRotation();
                AnimateButton.Content = "Start animation";
            }
            else
            {
                Camera1.StartRotation(10, 0); // animate with changing heading for 10 degrees in one second
                AnimateButton.Content = "Stop animation";
            }
        }


        private void Setup3DObjects()
        {
            _eventManager3D = new Ab3d.Utilities.EventManager3D(MainViewport);


            var wireGridVisual3D = new Ab3d.Visuals.WireGridVisual3D()
            {
                Size = new System.Windows.Size(1000, 1000),
                HeightCellsCount = 10,
                WidthCellsCount = 10,
                LineThickness = 3
            };

            MainViewport.Children.Add(wireGridVisual3D);


            // Create 7 x 7 boxes with different height
            for (int y = -3; y <= 3; y++)
            {
                for (int x = -3; x <= 3; x ++)
                {
                    // Height is based on the distance from the center
                    double height = (5 - Math.Sqrt(x * x + y * y)) * 60;

                    // Create the 3D Box visual element

                    var boxVisual3D = new Ab3d.Visuals.BoxVisual3D()
                    {
                        CenterPosition = new Point3D(x * 100, height / 2, y * 100),
                        Size = new Size3D(80, height, 80),
                        Material = _normalMaterial
                    };

                    MainViewport.Children.Add(boxVisual3D);


                    var visualEventSource3D = new VisualEventSource3D(boxVisual3D);
                    visualEventSource3D.MouseEnter += BoxOnMouseEnter;
                    visualEventSource3D.MouseLeave += BoxOnMouseLeave;
                    visualEventSource3D.MouseClick += BoxOnMouseClick;

                    _eventManager3D.RegisterEventSource3D(visualEventSource3D);
                }
            }

            ToggleCameraAnimation(); // Start camer animation
        }

        private void BoxOnMouseClick(object sender, MouseButton3DEventArgs mouseButton3DEventArgs)
        {
            var boxVisual3D = mouseButton3DEventArgs.HitObject as Ab3d.Visuals.BoxVisual3D;
            if (boxVisual3D == null)
                return; // This should not happen

            // Toggle clicked and normal material
            if (!_isSelectedBoxClicked)
            {
                boxVisual3D.Material = _clickedMaterial;
                _isSelectedBoxClicked = true;

                _totalClickedHeight += boxVisual3D.Size.Y;
            }
            else
            {
                boxVisual3D.Material = _normalMaterial;
                _isSelectedBoxClicked = false;

                _totalClickedHeight -= boxVisual3D.Size.Y;
            }

            UpdateTotalClickedHeightText();
        }

        private void UpdateTotalClickedHeightText()
        {
            InfoTextBox.Text = string.Format("Total clicked height: {0:0}\r\n{1}", _totalClickedHeight, InfoTextBox.Text);
        }

        private void BoxOnMouseEnter(object sender, Mouse3DEventArgs mouse3DEventArgs)
        {
            var boxVisual3D = mouse3DEventArgs.HitObject as Ab3d.Visuals.BoxVisual3D;
            if (boxVisual3D == null)
                return; // This should not happen

            // Set _isSelectedBoxClicked to true if the selected box is clicked (red) - this will be used on MouseLeave
            _isSelectedBoxClicked = ReferenceEquals(boxVisual3D.Material, _clickedMaterial);

            boxVisual3D.Material = _selectedMaterial;
        }

        private void BoxOnMouseLeave(object sender, Mouse3DEventArgs mouse3DEventArgs)
        {
            var boxVisual3D = mouse3DEventArgs.HitObject as Ab3d.Visuals.BoxVisual3D;
            if (boxVisual3D == null)
                return; // This should not happen

            if (_isSelectedBoxClicked)
                boxVisual3D.Material = _clickedMaterial;
            else
                boxVisual3D.Material = _normalMaterial;
        }
    }
}
