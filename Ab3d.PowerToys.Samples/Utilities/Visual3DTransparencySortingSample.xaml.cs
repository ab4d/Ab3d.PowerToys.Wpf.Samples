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
using Ab3d.Utilities;
using System.Windows.Media.Animation;
using Ab3d.Common.Utilities;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for Visual3DTransparencySortingSample.xaml
    /// </summary>
    public partial class Visual3DTransparencySortingSample : Page
    {
        private bool _isAnimationStarted;

        private Model3DGroup _rootModel3DGroup;
        private Dictionary<object, string> _objectNames;
        private TransparencySorter _transparencySorter;

        private StringBuilder _indexChangedStringBuilder;

        public Visual3DTransparencySortingSample()
        {
            InitializeComponent();

            _indexChangedStringBuilder = new StringBuilder();

            RecreateBoxes();
            FillObjectsList();

            _transparencySorter = new TransparencySorter(_rootModel3DGroup);
            _transparencySorter.UsedCamera = Camera1;
            _transparencySorter.CameraAngleChange = 10; // when the camera is changed for more than 10 degrees we do a check if objects need to be rearranged
            _transparencySorter.SortingMode = TransparencySorter.SortingModeTypes.ByCameraDistance;

            _transparencySorter.TransparentModelIndexChanged += new TransparentModelIndexChangedEventHandler(_transparencySorter_TransparentModelIndexChanged);
            _transparencySorter.SortingCompleted += new EventHandler(_transparencySorter_SortingCompleted);

            // Do an initial sorting
            _transparencySorter.Sort();

            _transparencySorter.StartSortingOnCameraChanged();
        }

        void _transparencySorter_SortingCompleted(object sender, EventArgs e)
        {
            if (!(LoggingCheckBox.IsChecked ?? false))
                return;

            FillObjectsList();

            AddEventText("Sorted:\r\n" + _indexChangedStringBuilder.ToString());
            _indexChangedStringBuilder = new StringBuilder();
        }

        void _transparencySorter_TransparentModelIndexChanged(object sender, TransparentModelIndexChangedEventArgs e)
        {
            if (!(LoggingCheckBox.IsChecked ?? false) || !(e.Changed3DObject is GeometryModel3D))
                return;

            string objectName = _objectNames[e.Changed3DObject];

            _indexChangedStringBuilder.AppendLine(string.Format("{0}: {1} -> {2}", objectName, e.OldIndex, e.NewIndex));
        }

        private void AddEventText(string eventText)
        {
            if (EventsTextBox.Text.Length > 10000)
                EventsTextBox.Text = String.Format("{0}{1}{2}...", eventText, Environment.NewLine, EventsTextBox.Text.Substring(0, 10000));
            else
                EventsTextBox.Text = String.Format("{0}{1}{2}", eventText, Environment.NewLine, EventsTextBox.Text);

            // To slow:
            //EventsTextBox.Text = EventsTextBox.Text + eventText + Environment.NewLine;
            //EventsTextBox.ScrollToEnd();
        }

        private void FillObjectsList()
        {
            string objectsText = "";

            for (int i = 0; i < _rootModel3DGroup.Children.Count; i++)
                objectsText += string.Format("{0,2} {1}\r\n", i, _objectNames[_rootModel3DGroup.Children[i]]);

            ObjectsTextBox.Text = objectsText;
        }

        private void OnSortingModeChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            RecreateBoxes();

            _transparencySorter.SortingMode = GetSelectedSortingMode();
            _transparencySorter.Sort();

            if (_transparencySorter.SortingMode == TransparencySorter.SortingModeTypes.ByCameraDistance)
                _transparencySorter.StartSortingOnCameraChanged();
            else
                _transparencySorter.StopSortingOnCameraChanged();
        }

        private TransparencySorter.SortingModeTypes GetSelectedSortingMode()
        {
            TransparencySorter.SortingModeTypes sortingMode;

            if (DisabledRadioButton.IsChecked ?? false)
                sortingMode = TransparencySorter.SortingModeTypes.Disabled;
            else if (SimpleRadioButton.IsChecked ?? false)
                sortingMode = TransparencySorter.SortingModeTypes.Simple;
            else // (ByCameraDistanceRadioButton.IsChecked ?? false)
                sortingMode = TransparencySorter.SortingModeTypes.ByCameraDistance;

            return sortingMode;
        }

        private void AnimationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimationStarted)
                StopAnimation();
            else
                StartAnimation();
        }

        private void StopAnimation()
        {
            AnimationButton.Content = "Start camera rotation";
            Camera1.StopRotation();

            _isAnimationStarted = false;
        }

        private void StartAnimation()
        {
            AnimationButton.Content = "Stop camera rotation";

            // start rotating the camera with changing heading by 30 degrees per second.
            // Note that while the camera is rotating, it is still possible to rotate the camera with the mouse (during mouse rotation the animation is suspended).           
            Camera1.StartRotation(30, 0);

            _isAnimationStarted = true;
        }

        private void RecreateButton_Click(object sender, RoutedEventArgs e)
        {
            RecreateBoxes();
        }


        private void RecreateBoxes()
        {
            _rootModel3DGroup = new Model3DGroup();
            _objectNames = new Dictionary<object, string>();

            CreateBoxes(_rootModel3DGroup, _objectNames);
            FillObjectsList();

            EventsTextBox.Text = "";

            // Because now we have different models we need to RecollectTransparentModels - collect the models that have transparency
            if (_transparencySorter != null)
            {
                _transparencySorter.SortingMode = GetSelectedSortingMode();

                if (_transparencySorter.SortingMode != TransparencySorter.SortingModeTypes.ByCameraDistance)
                    _transparencySorter.StopSortingOnCameraChanged();

                _transparencySorter.RecollectTransparentModels(_rootModel3DGroup);

                if (GetSelectedSortingMode() != TransparencySorter.SortingModeTypes.Disabled)
                    _transparencySorter.Sort(); // Resort the new objects
            }

            MainModelVisual3D.Content = _rootModel3DGroup;
        }

        private void CreateBoxes(Model3DGroup rootGroup, Dictionary<object, string> objectNames)
        {
            GeometryModel3D geometryModel3D;
            DiffuseMaterial material;
            Color color;

            Ab3d.Meshes.BoxMesh3D boxMesh = new Ab3d.Meshes.BoxMesh3D(new Point3D(0, 0, 0),
                                                                      new Size3D(80, 80, 80),
                                                                      1, 1, 1);

            string objectName;
            string colorName;
            double offsetX, offsetY, offsetZ;

            for (int x = 0; x < 3; x++)
            {
                offsetX = -100 + x * 100;

                if (x == 0)
                {
                    color = Color.FromArgb(255, 255, 0, 0);
                    colorName = "red";
                }
                else if (x == 1)
                {
                    color = Color.FromArgb(255, 0, 255, 0);
                    colorName = "green";
                }
                else
                {
                    color = Color.FromArgb(255, 0, 0, 255);
                    colorName = "blue";
                }

                for (int y = 0; y < 3; y++)
                {
                    offsetY = -100 + y * 100;

                    for (int z = 0; z < 3; z++)
                    {
                        offsetZ = -100 + z * 100;

                        if (z == 0)
                            color.A = 80;
                        else if (z == 1)
                            color.A = 160;
                        else
                            color.A = 255;

                        material = new DiffuseMaterial(new SolidColorBrush(color));

                        geometryModel3D = new GeometryModel3D(boxMesh.Geometry, material);
                        if (z != 2)
                            geometryModel3D.BackMaterial = material; // Set BackMaterial to transparent models

                        geometryModel3D.Transform = new TranslateTransform3D(offsetX, offsetY, offsetZ);

                        rootGroup.Children.Add(geometryModel3D);

                        objectName = string.Format("Box_{0}_{1}_{2}", colorName, color.A, y + 1);
                        objectNames.Add(geometryModel3D, objectName);
                    }
                }
            }
        }
    }
}
