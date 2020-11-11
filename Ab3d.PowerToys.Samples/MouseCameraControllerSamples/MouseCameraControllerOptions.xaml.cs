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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using Ab3d.Controls;

namespace Ab3d.PowerToys.Samples.MouseCameraControllerSamples
{
    /// <summary>
    /// Interaction logic for MouseCameraControllerOptions.xaml
    /// </summary>
    public partial class MouseCameraControllerOptions : Page
    {
        private string[] _rotateAndMoveCursorNames;
        private Cursor[] _rotateAndMoveCursors;
        
        private string[] _quickZoomCursorNames;
        private Cursor[] _quickZoomCursors;

        public MouseCameraControllerOptions()
        {
            InitializeComponent();

            _rotateAndMoveCursorNames = new string[] { "SizeAll (default)", "ScrollAll", "Cross", "Hand", "OpenedHandCursor", "ClosedHandCursor", "RotateCursorRight", "RotateCursorLeft" };
            _rotateAndMoveCursors     = new Cursor[] { Cursors.SizeAll, Cursors.ScrollAll, Cursors.Cross, Cursors.Hand, MouseCameraController1.OpenedHandCursor, MouseCameraController1.ClosedHandCursor, MouseCameraController1.RotateCursorRight, MouseCameraController1.RotateCursorLeft };

            RotationCursorComoBox.ItemsSource   = _rotateAndMoveCursorNames;
            RotationCursorComoBox.SelectedIndex = 0;

            MovementCursorComoBox.ItemsSource   = _rotateAndMoveCursorNames;
            MovementCursorComoBox.SelectedIndex = 0;


            _quickZoomCursorNames = new string[] { "ScrollNS", "SizeNS", "Hand" };
            _quickZoomCursors     = new Cursor[] { Cursors.ScrollNS, Cursors.SizeNS, Cursors.Hand };

            QuickZoomCursorComoBox.ItemsSource   = _quickZoomCursorNames;
            QuickZoomCursorComoBox.SelectedIndex = 0;

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                UpdateCursor();
            };
        }

        private void UpdateCursor()
        {
            MouseCameraController1.RotationCursor = _rotateAndMoveCursors[RotationCursorComoBox.SelectedIndex];

            MouseCameraController1.MovementCursor = _rotateAndMoveCursors[MovementCursorComoBox.SelectedIndex];

            MouseCameraController1.QuickZoomCursor = _quickZoomCursors[QuickZoomCursorComoBox.SelectedIndex];
        }

        private void RotationInertiaRatioSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            MouseCameraController1.RotationInertiaRatio = RotationInertiaRatioSlider.Value;

            // It is also possible to change the RotationEasingFunction
            // The CubicEaseOut method (defined in CameraAnimationSample) is the function that is used by default 
            //MouseCameraController1.RotationEasingFunction = Ab3d.PowerToys.Samples.Cameras.CameraAnimationSample.CubicEaseOut;
        }

        private void OnIsRotateCursorShownOnMouseOverCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            MouseCameraController1.IsRotateCursorShownOnMouseOver = IsRotateCursorShownOnMouseOverCheckBox.IsChecked ?? false;
        }

        private void OnCursorComoBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateCursor();
        }

        private void CustomCursorsButton_OnClick(object sender, RoutedEventArgs e)
        {
            // This buttons shows how to set up open hand and closed hand cursors.
            // To do this we first disable setting RotateCursor by the MouseCameraController.
            // This way we can manually set our own cursor (opened hand) to the EventsSourceElement:

            IsRotateCursorShownOnMouseOverCheckBox.IsChecked = false;
            //MouseCameraController1.IsRotateCursorShownOnMouseOver = false;


            // Then we set RotationCursor to ClosedHandCursor - this cursor is shown when user is rotating the camera:

            RotationCursorComoBox.SelectedIndex = 5; // 5 = ClosedHandCursor
             // MouseCameraController1.RotationCursor = MouseCameraController1.ClosedHandCursor;


            // And finally set the OpenedHandCursor to the EventsSourceElement:

            MouseCameraController1.EventsSourceElement.Cursor = MouseCameraController1.OpenedHandCursor;
        }
    }
}
