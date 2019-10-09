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
using Ab3d.Animation;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for WindPowerGeneratorsSample.xaml
    /// </summary>
    public partial class WindPowerGeneratorsSample : Page
    {
        private List<WindGenerator> _windGenerators;
        private GeometryModel3D _landscapeModel;

        public WindPowerGeneratorsSample()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CreateWindGenerators();
            PopulateCameraComboBox();
            SubscribeEvents();

            Mouse.OverrideCursor = null;
        }

        private void CreateWindGenerators()
        {
            var startPositions = new [] { new Point3D(258.3,182.1,288.0), 
                                          new Point3D(136.1,162.1,280.7),
                                          new Point3D(19.4,182.8,141.4), 
                                          new Point3D(-108.4,189.9,163.6), 
                                          new Point3D(-215.6,173.8,134.8) };


            // All wind generator models are stored in ModelsDictionary.xaml ResourceDictionary
            var dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("/Resources/ModelsDictionary.xaml", UriKind.RelativeOrAbsolute);

            // Get Landscape model
            _landscapeModel = dictionary["LandscapeModel"] as GeometryModel3D;

            if (_landscapeModel != null)
                GroundModelGroup.Children.Add(_landscapeModel);

            // Add Wind Generator models
            _windGenerators = new List<WindGenerator>();
            for (int i = 0; i < startPositions.Length; i++)
            {
                var oneWindGenerator = new WindGenerator();
                oneWindGenerator.Height = 100;
                oneWindGenerator.Position = startPositions[i];

                GeneratorsGroup.Children.Add(oneWindGenerator.Model);

                _windGenerators.Add(oneWindGenerator);

                // Start rotation based on the wind (controlled by WindSpeed property)
                oneWindGenerator.StartBladesRotation(); 
            }
        }

        // Populates the Combobox with possibilities to center the camera
        private void PopulateCameraComboBox()
        {
            CenterObjectComboBox.BeginInit();

            var oneComboBoxItem = new ComboBoxItem();
            oneComboBoxItem.Content = "Landscape";

            CenterObjectComboBox.Items.Add(oneComboBoxItem);


            for (int i = 0; i < _windGenerators.Count; i++)
            {
                oneComboBoxItem = new ComboBoxItem();
                oneComboBoxItem.Content = string.Format("Generator {0}", i + 1);

                CenterObjectComboBox.Items.Add(oneComboBoxItem);             
            }

            CenterObjectComboBox.EndInit();

            CenterObjectComboBox.SelectedIndex = 0;
        }

        // Creates EventManager3D and subscribes the 3d objects to events
        private void SubscribeEvents()
        {
            // Create EventManager3D with MainViewport as constructor parameter
            var eventManager = new Ab3d.Utilities.EventManager3D(MainViewport);


            // Create EventSource3D for the Landscape
            // Each 3D object that needs to participate in the events needs to be registered by EventSource3D
            // The Landscape object will not be subscribed to any event, but instead it is only registered as DragSurface
            // This means that other objects can be drag over this 3D model.
            var eventSource = new Ab3d.Utilities.ModelEventSource3D();
            eventSource.TargetModel3D = _landscapeModel;
            eventSource.IsDragSurface = true;

            // Register the Landscape's EventSource3D
            eventManager.RegisterEventSource3D(eventSource);


            // Now for each wind generator create its own EventSource3D object
            for (int i = 0; i < _windGenerators.Count; i++)
            {
                // Create new EventSource3D
                eventSource = new Ab3d.Utilities.ModelEventSource3D();

                // Set the target object to the 3D model
                eventSource.TargetModel3D = _windGenerators[i].Model;

                // We can assign any custom data (object) to the EventSource3D
                // This is very useful because in event handler we can access this custom data
                eventSource.CustomData = _windGenerators[i];
                
                // Each wind generator is subscribed to the following events:
                eventSource.MouseEnter += OnWindGeneratorMouseEnter;
                eventSource.MouseLeave += OnWindGeneratorMouseLeave;
                
                eventSource.BeginMouseDrag += OnWindGeneratorBeginMouseDrag;
                eventSource.MouseDrag      += OnWindGeneratorMouseDrag;
                eventSource.EndMouseDrag   += OnWindGeneratorEndMouseDrag;

                // Not used events:
                //eventSource.MouseClick += new eventSource_MouseClick;
                //eventSource.MouseDown += new eventSource_MouseDown;
                //eventSource.MouseUp += eventSource_MouseUp;


                // Register the EventSource3D to the EventManager3D
                eventManager.RegisterEventSource3D(eventSource);
            }
        }

        void OnWindGeneratorEndMouseDrag(object sender, Ab3d.Common.EventManager3D.Mouse3DEventArgs e)
        {
            this.Cursor = null;

            // Check if the moved wind generator is also camera centered
            var movedWindGenerator = e.HitEventSource3D.CustomData as WindGenerator;
            var centeredWindGenerator = GetCenteredWindGenerator();

            if (centeredWindGenerator != null && centeredWindGenerator == movedWindGenerator)
            {
                // Update camera's TargetPosition to reflect the moved wind generator position
                Camera1.TargetPosition = new Point3D(movedWindGenerator.Position.X, movedWindGenerator.Position.Y + movedWindGenerator.Height / 2, movedWindGenerator.Position.Z);
            }
        }

        void OnWindGeneratorBeginMouseDrag(object sender, Ab3d.Common.EventManager3D.Mouse3DEventArgs e)
        {
            this.Cursor = Cursors.ScrollAll;
        }

        void OnWindGeneratorMouseLeave(object sender, Ab3d.Common.EventManager3D.Mouse3DEventArgs e)
        {
            this.Cursor = null;
        }

        void OnWindGeneratorMouseEnter(object sender, Ab3d.Common.EventManager3D.Mouse3DEventArgs e)
        {
            this.Cursor = Cursors.ScrollAll;
        }

        void OnWindGeneratorMouseDrag(object sender, Ab3d.Common.EventManager3D.MouseDrag3DEventArgs e)
        {
            if (e.HitSurface == null) // return if we do not hit Landscape (only Landscape is registered as DragSurface so HitSurface for all other objects is null)
                return;

            var hitWindGenerator = e.HitEventSource3D.CustomData as WindGenerator;

            if (hitWindGenerator != null)
                hitWindGenerator.Position = new Point3D(e.CurrentSurfaceHitPoint.X, e.CurrentSurfaceHitPoint.Y, e.CurrentSurfaceHitPoint.Z);
        }

        private void WindSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Set WindSpeed on all wind generators
            foreach (var oneGenerator in _windGenerators)
                oneGenerator.WindSpeed = e.NewValue;
        }

        private void CenterObjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!CenterObjectComboBox.IsInitialized)
                return;

            // Get the WindGenerator that is selected with the CenterObjectComboBox
            // If Landscape is selected than return null
            var windGenerator = GetCenteredWindGenerator();

            Point3D newTargetPosition;
            double newDistance;

            if (windGenerator == null)
            {
                // Landscape
                newTargetPosition = new Point3D(0, 180, 150);

                if (Camera1.Distance < 700) // Set default camera distance
                    newDistance = 900;
                else
                    newDistance = Camera1.Distance; // Preserve the current distance
            }
            else
            {
                // One of the wind generators
                newTargetPosition = new Point3D(windGenerator.Position.X, windGenerator.Position.Y + windGenerator.Height / 2, windGenerator.Position.Z);

                if (Camera1.Distance > 700) // Set default camera distance
                    newDistance = 500;
                else
                    newDistance = Camera1.Distance; // Preserve the current distance
            }

            AnimateCamera(newTargetPosition, newDistance);
        }

        private void AnimateCamera(Point3D newTargetPosition, double newDistance)
        {
            // See samples in Animations folder for more info about animations

            // Create a new CameraAnimationNode that will animate the Camera1
            var cameraAnimationNode = new CameraAnimationNode(Camera1);

            if ((Camera1.TargetPosition - newTargetPosition).Length > 5) // We animate the TargetPosition if the difference is bigger the 5
            {
                cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(0, Camera1.TargetPosition)); // Start with current TargetPosition
                cameraAnimationNode.PositionTrack.Keys.Add(new Position3DKeyFrame(100, newTargetPosition));    // End at newTargetPosition 

                // Set easing function to smooth the transition
                cameraAnimationNode.PositionTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);
            }

            if (Math.Abs(Camera1.Distance - newDistance) > 5) // We animate the Distance if the difference is bigger the 5
            {
                cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 0, doubleValue: Camera1.Distance)); // Start with current Distance
                cameraAnimationNode.DistanceTrack.Keys.Add(new DoubleKeyFrame(frameNumber: 100, doubleValue: newDistance));    // End with newDistance 

                cameraAnimationNode.DistanceTrack.SetEasingFunctionToAllKeys(Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);
            }

            Camera1.AnimationController.AnimationNodes.Clear();
            Camera1.AnimationController.AnimationNodes.Add(cameraAnimationNode);

            Camera1.AnimationController.AutoRepeat = false;
            Camera1.AnimationController.AutoReverse = false;
            Camera1.AnimationController.FramesPerSecond = 100; // Take 1 second for the whole animation

            Camera1.AnimationController.StartAnimation();
        }


        // Get the WindGenerator that is selected with the CenterObjectComboBox
        // If Landscape is selected than return null
        private WindGenerator GetCenteredWindGenerator()
        {
            int index = CenterObjectComboBox.SelectedIndex;

            WindGenerator windGenerator;
            if (index < 1 || index > _windGenerators.Count)
                windGenerator = null;
            else
                windGenerator = _windGenerators[index - 1];

            return windGenerator;
        }
    }
}
