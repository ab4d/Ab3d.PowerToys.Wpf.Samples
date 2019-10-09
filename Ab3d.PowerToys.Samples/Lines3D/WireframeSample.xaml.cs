using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    /// <summary>
    /// Interaction logic for WireframeSample.xaml
    /// </summary>
    public partial class WireframeSample : Page
    {
        private double MOVEMENT_STEP = 10.0; // how much the person model is moved when an arrow key is pressed

        private TranslateTransform3D _personTranslate;

        private Model3DGroup _personModel;

        public WireframeSample()
        {
            InitializeComponent();

            FillWireframeTypesPanel();


            // PersonModel and HouseWithTreesModel are defined in App.xaml
            var houseWithTreesModel = this.FindResource("HouseWithTreesModel") as Model3D;

            SceneModel1.Content = houseWithTreesModel;
            HouseWithThreesWireframeVisual.OriginalModel = houseWithTreesModel;


            var originalPersonModel = this.FindResource("PersonModel") as Model3D;

            // originalPersonModel is frozen so its Transform cannot be changed - therefore we create a new _personModel that could be changed
            _personModel = new Model3DGroup();
            _personModel.Children.Add(originalPersonModel);

            _personTranslate = new TranslateTransform3D();
            _personModel.Transform = _personTranslate;

            SceneModel2.Content = _personModel;
            PersonWireframeVisual.OriginalModel = _personModel;


            this.Focusable = true; // by default Page is not focusable and therefore does not recieve keyDown event
            this.PreviewKeyDown += WireframeSample_PreviewKeyDown; // Use PreviewKeyDown to get arrow keys also (KeyDown event does not get them)
            this.Focus();


            this.Loaded += WireframeSample_Loaded;
        }

        private void FillWireframeTypesPanel()
        {
            // Create RadioButtons for each WireframeTypes enum value:
            // None
            // Wireframe
            // OriginalSolidModel
            // WireframeWithOriginalSolidModel
            // SingleColorSolidModel
            // WireframeWithSingleColorSolidModel

            var wireframeTypesTexts = Enum.GetNames(typeof(WireframeVisual3D.WireframeTypes));
            foreach (var wireframeTypesText in wireframeTypesTexts)
            {
                var radioButton = new RadioButton()
                {
                    Content = wireframeTypesText,
                    GroupName = "WireframeTypes"
                };

                if (wireframeTypesText == WireframeVisual3D.WireframeTypes.WireframeWithSingleColorSolidModel.ToString())
                    radioButton.IsChecked = true;

                radioButton.Checked += WireframeTypesRadioButtonChanged;

                WireframeTypePanel.Children.Add(radioButton);
            }
        }

        void WireframeSample_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateWireframe();
        }

        private void UpdateWireframe()
        {
            double lineThickness = (double)LineThicknessComboBox.SelectedIndex;
            if (lineThickness == 0) // Line thickness is equal to selected index except for the first index that represent 0.5 thickness.
                lineThickness = 0.5;


            // IMPORTANT:
            // To prevent regeneration of wireframe (that can be long running task) we use BeginInit and EndInit to create the wireframe only once after all the properties are set
            // Otherwise every change can lead to wireframe creation
            // Another option is to set the AutoUpdate property to false and then manually call the RecreateWireframeModel method.
            HouseWithThreesWireframeVisual.BeginInit();

                HouseWithThreesWireframeVisual.LineThickness = lineThickness;

                // If UseModelColor is true, than the line color is get from the color of the model's material (in case of DiffuseMaterial with SolidColorBrush)
                HouseWithThreesWireframeVisual.UseModelColor = PreserveColorRadioButton.IsChecked ?? false;
                HouseWithThreesWireframeVisual.LineColor = GetColorFromComboBox(LineColorComboBox);

                // SolidModelColor is used for SingleColorSolidModel or WireframeWithSingleColorSolidModel
                // In those modes the solid model will be shown with this color
                HouseWithThreesWireframeVisual.SolidModelColor = GetColorFromComboBox(SolidModelColorComboBox);
                HouseWithThreesWireframeVisual.IsEmissiveSolidModelColor = IsEmissiveSolidModelColorCheckBox.IsChecked ?? false;

                // Finally set the Model3D that will be used to create wireframe
                HouseWithThreesWireframeVisual.OriginalModel = SceneModel1.Content;

            HouseWithThreesWireframeVisual.EndInit();

            // OLD CODE - use WireframeFactory.CreateWireframe
            // Create wireframe Model3D without WireframeVisual3D - with WireframeFactory:

            //Model3D wireframe = Ab3d.Models.WireframeFactory.CreateWireframe(SceneModel1, 1, preserveLineColor, customColor, SceneCameraViewport2);

            //WireframeModelGroup1.Children.Clear();
            //WireframeModelGroup1.Children.Add(wireframe);
        }

        private void OnWireframeSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateWireframe();
        }


        void WireframeSample_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                case Key.Left:
                    // left
                    MovePerson(0, -MOVEMENT_STEP);
                    e.Handled = true;
                    break;

                case Key.D:
                case Key.Right:
                    // right
                    MovePerson(0, MOVEMENT_STEP);
                    e.Handled = true;
                    break;

                case Key.W:
                case Key.Up:
                    // forward
                    MovePerson(-MOVEMENT_STEP, 0);
                    e.Handled = true;
                    break;

                case Key.S:
                case Key.Down:
                    // backward
                    MovePerson(MOVEMENT_STEP, 0);
                    e.Handled = true;
                    break;
            }
        }

        private void MovePerson(double dx, double dy)
        {
            _personTranslate.OffsetZ += dx;
            _personTranslate.OffsetX += dy; // y axis in 3d is pointing up

            // No need to manually call RecreateWireframeModel because WireframeVisual3D is automatically updated when the child property of OriginalObject is changed
            // PersonWireframeVisual.RecreateWireframeModel();
        }


        private void WireframeTypesRadioButtonChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            if (!this.IsLoaded)
                return;

            var radioButton = (RadioButton)sender;
            HouseWithThreesWireframeVisual.WireframeType = (WireframeVisual3D.WireframeTypes) Enum.Parse(typeof(WireframeVisual3D.WireframeTypes), (string) radioButton.Content);

            SolidModelColorComboBox.IsEnabled = (HouseWithThreesWireframeVisual.WireframeType == WireframeVisual3D.WireframeTypes.SingleColorSolidModel ||
                                                 HouseWithThreesWireframeVisual.WireframeType == WireframeVisual3D.WireframeTypes.WireframeWithSingleColorSolidModel);
        }

        private void SceneCamera2_CameraChanged(object sender, Ab3d.Common.Cameras.CameraChangedRoutedEventArgs e)
        {
            // Manually sync the camera in Viewport3D in upper right corner with the main camera
            // Enclose changes in BeginInit and EndInit to prevent multiple updates
            SceneCamera1.BeginInit();
            
            SceneCamera1.Heading = SceneCamera2.Heading;
            SceneCamera1.Attitude = SceneCamera2.Attitude;
            SceneCamera1.Distance = SceneCamera2.Distance;
            
            SceneCamera1.EndInit();
        }

        private Color GetColorFromComboBox(ComboBox checkbox)
        {
            Color color;

            switch (checkbox.SelectedIndex)
            {
                case 0:
                    color = Colors.White;
                    break;

                case 1:
                    color = Colors.Red;
                    break;

                default:
                case 2:
                    color = Colors.Black;
                    break;
            }

            return color;
        }
    }
}
