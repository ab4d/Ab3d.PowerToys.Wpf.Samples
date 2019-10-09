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
using Ab3d.PowerToys.Samples.UseCases;
using System.Collections.ObjectModel;
using Ab3d.Common.EventManager3D;
using Ab3d.PowerToys.Samples.EventManager3D.EventPanels;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.EventManager3D
{
    /// <summary>
    /// Interaction logic for EventManagerSample.xaml
    /// </summary>
    public partial class EventManagerSample : Page
    {
        private WindGenerator _windGenarator;
        private GeometryModel3D _landscapeModel;

        Ab3d.Utilities.EventManager3D _eventManager;

        private Mouse3DEventArgsPanel _mouse3DEventArgsPanel;
        private MouseButton3DEventArgsPanel _mouseButton3DEventArgsPanel;
        private MouseDrag3DEventArgsPanel _mouseDrag3DEventArgsPanel;
        private MouseWheel3DEventArgsPanel _mouseWheel3DEventArgsPanel;
        private Touch3DEventArgsPanel _touch3DEventArgsPanel;

        private ObservableCollection<EventData> _events;

        public EventManagerSample()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            InitializeComponent();

            // _events will hold all the triggered events
            _events = new ObservableCollection<EventData>();

            // Create models and subscribe to events
            CreateModels();
            SubscribeEvents();

            // Bind all events to ListBox
            EventsListBox.ItemsSource = _events;

            // Pre-create the panels to show event args details
            _mouse3DEventArgsPanel = new Mouse3DEventArgsPanel();
            _mouseButton3DEventArgsPanel = new MouseButton3DEventArgsPanel();
            _mouseDrag3DEventArgsPanel = new MouseDrag3DEventArgsPanel();
            _mouseWheel3DEventArgsPanel = new MouseWheel3DEventArgsPanel();
            _touch3DEventArgsPanel = new Touch3DEventArgsPanel();

            this.Loaded += EventManagerSample_Loaded;
        }

        void EventManagerSample_Loaded(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }

        private void CreateModels()
        {
            ResourceDictionary dictionary;

            // All wind generator models are stored in ModelsDictionary.xaml ResourceDictionary
            dictionary = new ResourceDictionary();
            dictionary.Source = new Uri("/Resources/ModelsDictionary.xaml", UriKind.RelativeOrAbsolute);

            _landscapeModel = dictionary["LandscapeModel"] as GeometryModel3D;

            GroundModelGroup.Children.Add(_landscapeModel);

            // Create one Wind Generator
            _windGenarator = new WindGenerator();
            _windGenarator.Height = 100;
            _windGenarator.Position = new Point3D(19.4, 182.8, 141.4);

            GeneratorsGroup.Children.Add(_windGenarator.Model);

            // Out ThirdPersonCamera will look at the Wind Generator
            Camera1.CenterObject = _windGenarator.Model;
        }

        private void SubscribeEvents()
        {
            // Create an instance of EventManager3D for out Viewport3D

            // Important:
            // It is highly recommended not to have more than one EventManager3D object per Viewport3D. 
            // Having multiple EventManager3D object can greatly reduce the performance because each time the Viewport3D camera is changed, 
            // each EventManager3D must perform a full 3D hit testing from the current mouse position. 
            // This operation is very CPU intensive and can affect performance when there are many 3D objects in the scene.
            // When multiple EventManager3D object are defined, then the 3D hit testing is performed multiple times.
            // Therefore it is recommended to have only one EventManager3D object per Viewport3D.
            //
            // It is also recommended to remove registered event sources after they are not used any more.
            // This can be done with RemoveEventSource3D method. 

            _eventManager = new Ab3d.Utilities.EventManager3D(MainViewport);

            // Create a NamedObjects dictionary and set it to eventManager.
            // This way we can use names on EventSource objects to identify Model3D or Visual3D.
            // This is very usefull if we read the 3d objects with Reader3ds or ReaderObj and already have the NamedObjects dictionry.
            // When names are used in EventSource objects, we get the HitModelName or HitVisualName property set so we can know which object was hit.
            var namedObjects = new Dictionary<string, object>();
            namedObjects.Add("Landscape", _landscapeModel);
            namedObjects.Add("Base", _windGenarator.BaseModel);
            namedObjects.Add("Blades", _windGenarator.BladesModel);
            namedObjects.Add("Turbine", _windGenarator.TurbineModel);

            _eventManager.NamedObjects = namedObjects;



            // subscribe the Landscape as the drag surface
            var modelEventSource = new Ab3d.Utilities.ModelEventSource3D();

            // Instead of using TargetModelName and NamedObjects, we could also specify the model with TargetModel3D
            //modelEventSource.TargetModel3D = _landscapeModel; 

            modelEventSource.TargetModelName = "Landscape";
            modelEventSource.IsDragSurface = true;

            _eventManager.RegisterEventSource3D(modelEventSource);


            // subscribe other objects
            var multiModelEventSource = new Ab3d.Utilities.MultiModelEventSource3D();

            // Instead of using TargetModelNames and NamedObjects, we could specify the models with model instances
            //multiModelEventSource.TargetModels3D = new Model3D[] { _windGenarator.BaseModel, 
            //                                                       _windGenarator.BladesModel, 
            //                                                       _windGenarator.TurbineModel };

            // Because we have set the NamedObjects dictionary to _eventManager we can simply specify the models by their names:
            multiModelEventSource.TargetModelNames = "Base, Blades, Turbine";
            multiModelEventSource.IsDragSurface = false;

            // Subscribe to all events
            multiModelEventSource.MouseEnter       += eventSource_MouseEnter;
            multiModelEventSource.MouseLeave       += eventSource_MouseLeave;
            multiModelEventSource.MouseMove        += eventSource_MouseMove;
            multiModelEventSource.MouseUp          += eventSource_MouseUp;
            multiModelEventSource.MouseDown        += eventSource_MouseDown;
            multiModelEventSource.MouseClick       += eventSource_MouseClick;
            multiModelEventSource.MouseWheel       += multiModelEventSource_MouseWheel;
            multiModelEventSource.MouseDoubleClick += eventSource_MouseDoubleClick;
            multiModelEventSource.BeginMouseDrag   += eventSource_BeginMouseDrag;
            multiModelEventSource.MouseDrag        += eventSource_MouseDrag;
            multiModelEventSource.EndMouseDrag     += eventSource_EndMouseDrag;

            multiModelEventSource.TouchEnter += eventSource_TouchEnter;
            multiModelEventSource.TouchDown += eventSource_TouchDown;
            multiModelEventSource.TouchMove += eventSource_TouchMove;
            multiModelEventSource.TouchUp += eventSource_TouchUp;
            multiModelEventSource.TouchLeave += eventSource_TouchLeave;

            // NOTE:
            // It is also possible to subscribe to touch manipulations events (pinch scale and rotate)
            // But this requires to set IsManipulationEnabled to true and that disables some other events
            // See the TouchManipulationsSample for more information

            _eventManager.RegisterEventSource3D(multiModelEventSource);
        }

        private void eventSource_TouchEnter(object sender, Touch3DEventArgs e)
        {
            AddEvent("TouchEnter", e);
        }

        private void eventSource_TouchDown(object sender, Touch3DEventArgs e)
        {
            AddEvent("TouchDown", e);
        }

        private void eventSource_TouchMove(object sender, Touch3DEventArgs e)
        {
            AddEvent("TouchMove", e);
        }

        private void eventSource_TouchUp(object sender, Touch3DEventArgs e)
        {
            AddEvent("TouchUp", e);
        }

        private void eventSource_TouchLeave(object sender, Touch3DEventArgs e)
        {
            AddEvent("TouchLeave", e);
        }

        void eventSource_EndMouseDrag(object sender, Ab3d.Common.EventManager3D.Mouse3DEventArgs e)
        {
            AddEvent("EndMouseDrag", e);
        }

        void eventSource_MouseDrag(object sender, Ab3d.Common.EventManager3D.MouseDrag3DEventArgs e)
        {
            AddEvent("MouseDrag", e);
        }

        void eventSource_BeginMouseDrag(object sender, Ab3d.Common.EventManager3D.Mouse3DEventArgs e)
        {
            AddEvent("BeginMouseDrag", e);
        }

        void eventSource_MouseDown(object sender, MouseButton3DEventArgs e)
        {
            AddEvent("MouseDown", e);
        }

        void eventSource_MouseUp(object sender, MouseButton3DEventArgs e)
        {
            AddEvent("MouseUp", e);
        }

        void eventSource_MouseClick(object sender, Ab3d.Common.EventManager3D.MouseButton3DEventArgs e)
        {
            AddEvent("MouseClick", e);
        }

        void multiModelEventSource_MouseWheel(object sender, MouseWheel3DEventArgs e)
        {
            AddEvent("MouseWheel", e);
        }

        void eventSource_MouseDoubleClick(object sender, MouseButton3DEventArgs e)
        {
            AddEvent("MouseDoubleClick", e);
        }

        void eventSource_MouseLeave(object sender, Ab3d.Common.EventManager3D.Mouse3DEventArgs e)
        {
            AddEvent("MouseLeave", e);
        }

        private void eventSource_MouseMove(object sender, Ab3d.Common.EventManager3D.Mouse3DEventArgs e)
        {
            AddEvent("MouseMove", e);
        }

        void eventSource_MouseEnter(object sender, Ab3d.Common.EventManager3D.Mouse3DEventArgs e)
        {
            AddEvent("MouseEnter", e);
        }

        private void AddEvent(string eventName, EventArgs args)
        {
            _events.Insert(0, new EventData(eventName, args));
            EventsListBox.SelectedIndex = 0;
        }

        private void EventsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EventData selectedEvent;

            if (!EventsListBox.IsInitialized)
                return;

            selectedEvent = EventsListBox.SelectedItem as EventData;

            if (selectedEvent == null)
                return;

            ShowPanelForEventArgs(selectedEvent.Args);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _events.Clear();
        }


        private void ShowPanelForEventArgs(EventArgs args)
        {
            UserControl eventUserControl;

            if (args is MouseDrag3DEventArgs)
            {
                eventUserControl = _mouseDrag3DEventArgsPanel;
            }
            else if (args is MouseButton3DEventArgs)
            {
                eventUserControl = _mouseButton3DEventArgsPanel;
            }
            else if (args is Mouse3DEventArgs)
            {
                eventUserControl = _mouse3DEventArgsPanel;
            }
            else if (args is MouseWheel3DEventArgs)
            {
                eventUserControl = _mouseWheel3DEventArgsPanel;
            }
            else if (args is Touch3DEventArgs)
            {
                eventUserControl = _touch3DEventArgsPanel;
            }
            else
            {
                eventUserControl = null;
            }

            EventArgsPlaceholder.Children.Clear();
            if (eventUserControl != null)
            {
                eventUserControl.DataContext = args;
                EventArgsPlaceholder.Children.Add(eventUserControl);
            }
        }
    }
}
