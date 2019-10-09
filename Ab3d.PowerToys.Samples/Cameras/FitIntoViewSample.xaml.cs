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
using Ab3d.Cameras;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for FitIntoViewSample.xaml
    /// </summary>
    public partial class FitIntoViewSample : Page
    {
        private bool _isAdjustingDistance;

        private Random _rnd = new Random();

        public FitIntoViewSample()
        {
            InitializeComponent();

            CreateRandomScene();

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                // We need to wait until Loaded event because the MainViewport needs to have its size defined for FitIntoView to work
                FitIntoView();

                // Subscribe to camera changes
                Camera1.CameraChanged += Camera1_CameraChanged;
            };
        }

        private void FitIntoView()
        {
            double selectedAdjustmentFactor = GetSelectedAdjustmentFactor();

            bool adjustTargetPosition = CenterCameraCheckBox.IsChecked ?? false;

            _isAdjustingDistance = true; // Prevent infinite recursion

            Camera1.FitIntoView(visuals: ObjectsRootVisual3D.Children,               // Do not use WireGrid for FitIntoView calculations (this parameter can be skipped if all shown objects should be checked)
                                fitIntoViewType: Ab3d.Common.FitIntoViewType.CheckAllPositions, // CheckAllPositions can take some time bigger scenes. In this case you can use the CheckBounds
                                adjustTargetPosition: adjustTargetPosition,                     // Adjust TargetPosition to better fit into view; set to false to preserve the current TargetPosition
                                adjustmentFactor: selectedAdjustmentFactor);                    // adjustmentFactor can be used to specify the margin

            // See help for more information about FitIntoView (defined in Ab3d.Cameras.BaseTargetPositionCamera)


            // NOTE:
            // If you want to call FitIntoView to show some manually specified area of the scene (not the actually shown objects)
            // you can create a BoxVisual3D and pass it to the FitIntoView call.
            //
            // For example the following code will make sure that the area defined by a 3D box with center at (0, 0, 0) and size (200, 50, 50) will be shown:
            //var dummyVisual3D = new Ab3d.Visuals.BoxVisual3D()
            //{
            //    CenterPosition = new Point3D(0, 0, 0),
            //    Size = new Size3D(200, 50, 50)
            //};

            //var dummyModelVisual3D = new ModelVisual3D();
            //dummyModelVisual3D.Children.Add(dummyVisual3D);

            // Then you can call
            // Camera1.FitIntoView(visual3DCollection: dummyVisual3D.Children, ...


            _isAdjustingDistance = false;
        }

        private void CreateRandomScene()
        {
            ObjectsRootVisual3D.Children.Clear();

            for (int i = 0; i < 6; i++)
            {
                var randomCenterPosition = new Point3D(_rnd.NextDouble() * 100 - 50, _rnd.NextDouble() * 60 - 30, _rnd.NextDouble() * 100 - 50);

                var boxVisual3D = new Ab3d.Visuals.BoxVisual3D()
                {
                    CenterPosition = randomCenterPosition,
                    Size = new Size3D(10, 8, 10),
                    Material = new DiffuseMaterial(Brushes.Silver)
                };

                ObjectsRootVisual3D.Children.Add(boxVisual3D);
            }
        }

        private void Camera1_CameraChanged(object sender, Ab3d.Common.Cameras.CameraChangedRoutedEventArgs e)
        {
            if (!_isAdjustingDistance && (AutoAdjustCheckBox.IsChecked ?? false))
                FitIntoView();
        }

        private void FitIntoViewButton_OnClick(object sender, RoutedEventArgs e)
        {
            FitIntoView();
        }

        private void CenterCameraCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            Camera1.TargetPosition = new Point3D(0, 0, 0);
        }
        
        private void RecreateSceneButton_OnClick(object sender, RoutedEventArgs e)
        {
            CreateRandomScene();
            FitIntoView();
        }

        private double GetSelectedAdjustmentFactor()
        {
            switch (AdjustmentFactorComboBox.SelectedIndex)
            {
                case 1:
                    return 1.1;

                case 2:
                    return 1.2;

                default:
                case 0:
                    return 1.0;
            }
        }

        private void AdjustmentFactorComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            FitIntoView();
        }
    }
}
