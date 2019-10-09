using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Threading;
using Ab3d.Common.EventManager3D;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for ModelDecoratorSample.xaml
    /// </summary>
    public partial class ModelDecoratorSample : Page
    {
        private Ab3d.Utilities.EventManager3D _eventManager3D;

        public ModelDecoratorSample()
        {
            InitializeComponent();

            // Use EventManager3D to subscribe to MouseMove and MouseLeave events on RobotArmModel 
            _eventManager3D = new Ab3d.Utilities.EventManager3D(MainViewport);

            var visualEventSource3D = new VisualEventSource3D(RobotArmModel); // RobotArmModel is a source of events
            visualEventSource3D.MouseMove += VisualEventSource3DOnMouseMove;
            visualEventSource3D.MouseLeave += VisualEventSource3DOnMouseLeave;

            _eventManager3D.RegisterEventSource3D(visualEventSource3D);

            // !!! IMPORTANT !!!
            // We need to exclude ModelDecorator from events 
            // otherwise when mouse would be over line drawin with ModelDecorator we would get a MouseLeave event
            _eventManager3D.RegisterExcludedVisual3D(ModelDecorator);
        }

        private void VisualEventSource3DOnMouseMove(object sender, Mouse3DEventArgs mouse3DEventArgs)
        {
            // Get the actually hit Model3D object
            var hitModel = mouse3DEventArgs.RayHitResult.ModelHit;

            // ModelDecoratorVisual3D shows bounding box, triangles or normals for the 3D models that is set to the TargetModel3D property.
            // It is also recommended to set the RootModelVisual3D that specifies a parent ModelVisual3D for TargetModel3D (here this is done in XAML).
            // This way ModelDecoratorVisual3D can take into account any transformations that are used on parent Visual3D objects.

            // !!! IMPORTANT !!! 
            // When the TargetModel3D is defined in a hierarchy of Model3DGroup object with their own transformations, 
            // we need to set the ModelDecoratorVisual3D.RootModelVisual3D property to the ModelVisual3D that contains the selected model. 
            // In this sample this is done in XAML - RootModelVisual3D is set to the RobotArmModel (ObjModelVisual3D)

            if (ModelDecorator.TargetModel3D != hitModel)
                ModelDecorator.TargetModel3D = hitModel;
        }

        private void VisualEventSource3DOnMouseLeave(object sender, Mouse3DEventArgs mouse3DEventArgs)
        {
            ModelDecorator.TargetModel3D = null;
        }

        private void NormalsLengthComboBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (NormalsLengthComboBox.SelectedIndex == 0)
            {
                // When NormalsLineLength is set to double.NaN then the 
                // normals line length is automatically calculated from the size of the TargetModel3D (this is the default value)
                ModelDecorator.NormalsLineLength = double.NaN; 
            }
            else
            {
                var comboBoxItem = NormalsLengthComboBox.SelectedItem as ComboBoxItem;
                string lengthText = comboBoxItem.Content as string;

                if (!string.IsNullOrEmpty(lengthText))
                    ModelDecorator.NormalsLineLength = double.Parse(lengthText, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
