using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.EventManager3D
{
    /// <summary>
    /// Interaction logic for TouchManipulationsSample.xaml
    /// </summary>
    public partial class TouchManipulationsSample : Page
    {
        private Ab3d.Utilities.EventManager3D _eventManager;

        public TouchManipulationsSample()
        {
            InitializeComponent();
            

            // First, create an instace of EventManager3D for MainViewport
            _eventManager = new Ab3d.Utilities.EventManager3D(MainViewport);

            // Setting CustomEventsSourceElement to a Border that has Background defind (can also be set to Transparent, but should not be null)
            // allows EventManager3D to get events even if touch is not over any 3D model.
            // For example when moving the model with touch, the model may move away from the fingers
            _eventManager.CustomEventsSourceElement = ViewportBorder;
            
            // IMPORTANT:
            // We need to manually enable the manipulation events.
            // Otherwise we will only get mouse events (also those mouse events that are created from touch events)
            //
            // NOTE:
            // When IsManipulationEnabled, the mouse events will not be created from touch events.
            _eventManager.IsManipulationEnabled = true;


            // Now create an instance of VisualEventSource3D
            // It will be used to define the TeapotVisual3D as source of touch events
            var visualEventSource3D = new VisualEventSource3D(TeapotVisual3D);

            // Now we can subscribe to manipulation events (we could also subscribe to mouse events)
            visualEventSource3D.ManipulationStarted += delegate(object o, Manipulation3DEventArgs<ManipulationStartedEventArgs> e)
            {
                TeapotVisual3D.DefaultMaterial = new DiffuseMaterial(Brushes.Red);
            };

            visualEventSource3D.ManipulationCompleted += delegate(object o, Manipulation3DEventArgs<ManipulationCompletedEventArgs> e)
            {
                TeapotVisual3D.DefaultMaterial = new DiffuseMaterial(Brushes.Silver);
            };

            visualEventSource3D.ManipulationDelta += VisualEventSource3DOnManipulationDelta;

            // Register the teapot as an event source on EventManager3D
            _eventManager.RegisterEventSource3D(visualEventSource3D);



            // Finally, we need to disable touch events on MouseCameraController 
            // Otherwise both MouseCameraController and EventManager3D will handle the same events (also zooming and rotating the camera)
            MouseCameraController1.IsTouchMoveEnabled = false;
            MouseCameraController1.IsTouchRotateEnabled = false;
            MouseCameraController1.IsTouchZoomEnabled = false;
        }

        private void VisualEventSource3DOnManipulationDelta(object sender, Manipulation3DEventArgs<ManipulationDeltaEventArgs> e)
        {
            bool isHandled = false;


            if ((IsScaleEnabledCheckBox.IsChecked ?? false))
            {
                // scale by the same amount on all x,y and z axis
                // calculate average scale
                double scaleFactor = (e.ManipulationData.DeltaManipulation.Scale.X + e.ManipulationData.DeltaManipulation.Scale.Y) / 2;
                double newScale = TeapotScale.ScaleX * scaleFactor;

                TeapotScale.ScaleX = newScale;
                TeapotScale.ScaleY = newScale;
                TeapotScale.ScaleZ = newScale;

                isHandled = true;
            }

            if ((IsRotateEnabledCheckBox.IsChecked ?? false))
            {
                // We multiply the rotation angle delta by 3
                // This simplifies rotation by touch allowing user to do a full 360 degree rotation with rotation only 120 degrees with touch
                // (with touch it is very unpractical to rotate for more than 180 degrees)
                TeapotRotation.Angle -= e.ManipulationData.DeltaManipulation.Rotation * 3;
                
                isHandled = true;
            }

            // NOTE:
            // I was not able to get correct coordinates from manipulation translation. The following did not work correctly (I also tried to adjust by dpi scale):
            //Point currentMovePositions = new Point(e.ManipulationData.ManipulationOrigin.X + e.ManipulationData.CumulativeManipulation.Translation.X,
            //                                       e.ManipulationData.ManipulationOrigin.Y + e.ManipulationData.CumulativeManipulation.Translation.Y);
            // I wanted to get position so that it could be turned into 3D coordinate on the XZ plane
            // This 3D coordinate would then be used to move the object
            // But because the coordinates were not correct, I have removed the code that used the translation.

            e.ManipulationData.Handled = isHandled;
        }
    }
}
