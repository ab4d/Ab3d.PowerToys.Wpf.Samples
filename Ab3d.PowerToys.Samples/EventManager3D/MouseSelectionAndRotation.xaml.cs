using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Ab3d.Common;
using Ab3d.Common.EventManager3D;
using Ab3d.Controls;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.EventManager3D
{
    /// <summary>
    /// Interaction logic for MouseSelectionAndRotation.xaml
    /// </summary>
    public partial class MouseSelectionAndRotation : Page
    {
        private DiffuseMaterial _standardBoxMaterial;
        private DiffuseMaterial _mouseOverBoxMaterial;
        private DiffuseMaterial _selectedBoxMaterial;

        private BoxVisual3D _currentMouseOverBoxVisual3D;
        private Material _savedMouseOverMaterial;

        private BoxVisual3D _currentlySelectedBoxVisual3D;

        public MouseSelectionAndRotation()
        {
            InitializeComponent();

            _standardBoxMaterial = new DiffuseMaterial(Brushes.Silver);
            _standardBoxMaterial.Freeze();

            _mouseOverBoxMaterial = new DiffuseMaterial(Brushes.Orange);
            _mouseOverBoxMaterial.Freeze();

            _selectedBoxMaterial = new DiffuseMaterial(Brushes.Red);
            _selectedBoxMaterial.Freeze();



            var eventManager3D = new Ab3d.Utilities.EventManager3D(MainViewport);

            // IMPORTANT !!!
            // To allow EventManager3D and MouseCameraController to both work with left mouse button,
            // we need to set UsePreviewEvents on EventManager3D to true.
            // This will make EventManager3D subscribe to Preview... events (PreviewMouseMove instead of MouseMove)
            // and therefore EventManager3D will get the mouse events before they can be handled by the MouseCameraController.
            eventManager3D.UsePreviewEvents = true;

            // 5 x 3 boxes
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var boxVisual3D = new BoxVisual3D()
                    {
                        CenterPosition = new Point3D(-40 + x * 20, 5, -30 + y * 30),
                        Size = new Size3D(10, 10, 10),
                        Material = _standardBoxMaterial
                    };

                    MainViewport.Children.Add(boxVisual3D);

                    // Register mouse events on boxVisual3D
                    RegisterMouseEventsOnBoxVisual(boxVisual3D, eventManager3D);
                }
            }


            MouseCameraControllerInfo1.AddCustomInfoLine(0, MouseCameraController.MouseAndKeyboardConditions.LeftMouseButtonPressed, "Select Box");
        }

        private void RegisterMouseEventsOnBoxVisual(BoxVisual3D boxVisual3D, Ab3d.Utilities.EventManager3D eventManager3D)
        {
            var visualEventSource3D = new VisualEventSource3D(boxVisual3D);
            visualEventSource3D.MouseEnter += delegate (object sender, Mouse3DEventArgs e)
            {
                ClearMouseOverBoxVisual();

                var hitBoxVisual3D = e.HitObject as BoxVisual3D;

                if (hitBoxVisual3D != null && !ReferenceEquals(hitBoxVisual3D, _currentlySelectedBoxVisual3D))
                {
                    _savedMouseOverMaterial = hitBoxVisual3D.Material;

                    hitBoxVisual3D.Material = _mouseOverBoxMaterial;
                    _currentMouseOverBoxVisual3D = hitBoxVisual3D;
                }

                Mouse.OverrideCursor = Cursors.Hand;
            };

            visualEventSource3D.MouseLeave += delegate (object sender, Mouse3DEventArgs e)
            {
                var hitBoxVisual3D = e.HitObject as BoxVisual3D;

                if (!ReferenceEquals(hitBoxVisual3D, _currentlySelectedBoxVisual3D))
                    ClearMouseOverBoxVisual();
                else
                    _currentMouseOverBoxVisual3D = null;

                Mouse.OverrideCursor = null;
            };

            visualEventSource3D.MouseClick += delegate (object sender, MouseButton3DEventArgs e)
            {
                ClearSelectedBoxVisual();

                var hitBoxVisual3D = e.HitObject as BoxVisual3D;

                if (hitBoxVisual3D != null)
                {
                    hitBoxVisual3D.Material = _selectedBoxMaterial;
                    _currentlySelectedBoxVisual3D = hitBoxVisual3D;

                    Camera1.RotationCenterPosition = hitBoxVisual3D.CenterPosition;
                }
            };


            eventManager3D.RegisterEventSource3D(visualEventSource3D);
        }

        private void ClearMouseOverBoxVisual()
        {
            if (_currentMouseOverBoxVisual3D != null)
            {
                _currentMouseOverBoxVisual3D.Material = _savedMouseOverMaterial ?? _standardBoxMaterial;
                _currentMouseOverBoxVisual3D = null;
                _savedMouseOverMaterial = null;
            }
        }

        private void ClearSelectedBoxVisual()
        {
            if (_currentlySelectedBoxVisual3D != null)
            {
                _currentlySelectedBoxVisual3D.Material = _standardBoxMaterial;
                _currentlySelectedBoxVisual3D = null;
            }
        }
    }
}
