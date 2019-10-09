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

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for SceneWithGlassesSample.xaml
    /// </summary>
    public partial class SceneWithGlassesSample : Page
    {
        private bool _isAnimationStarted;
        private TransparencySorter _transparencySorter;
        private List<Visual3D> _originalModelsOrder;

        public SceneWithGlassesSample()
        {
            InitializeComponent();

            // Store the initial order of objects into List so it can be restored later
            _originalModelsOrder = new List<Visual3D>();
            foreach (var oneChild in RootModelVisual.Children)
                _originalModelsOrder.Add(oneChild);

            _transparencySorter = new TransparencySorter(MainViewport3D);
            _transparencySorter.UsedCamera = SceneCamera1;
            _transparencySorter.CameraAngleChange = 10; // when the camera is changed for more than 10 degrees we do a check if objects need to be rearranged
            _transparencySorter.SortingMode = TransparencySorter.SortingModeTypes.ByCameraDistance;

            // Do initial sorting
            _transparencySorter.Sort();

            _transparencySorter.StartSortingOnCameraChanged();

            this.Loaded += new RoutedEventHandler(TransparencySorting_Loaded);
        }

        void TransparencySorting_Loaded(object sender, RoutedEventArgs e)
        {
            StartAnimation();
        }

        private void OnSortingModeChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            ResetObjectsOrder();

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
            AnimationButton.Content = "START camera rotation";
            SceneCamera1.StopRotation();

            _isAnimationStarted = false;
        }

        private void StartAnimation()
        {
            AnimationButton.Content = "STOP camera rotation";

            // start rotating the camera with changing heading by 30 degrees per second.
            // Note that while the camera is rotating, it is still possible to rotate the camera with the mouse (during mouse rotation the animation is suspended).
            SceneCamera1.StartRotation(30, 0); 

            _isAnimationStarted = true;
        }

        private void ResetObjectsOrder()
        {
            // Re-populate Children of RootModelVisual so they will have the initial indexes
            RootModelVisual.Children.Clear();

            foreach (var oneObject in _originalModelsOrder)
                RootModelVisual.Children.Add(oneObject);
        }
    }
}
