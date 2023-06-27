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
using Ab3d.Common;
using Ab3d.Controls;
using Ab3d.Meshes;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.OtherCameraControllers
{
    /// <summary>
    /// Interaction logic for CameraNavigationCirclesSample.xaml
    /// </summary>
    public partial class CameraNavigationCirclesSample : Page
    {
        private int _navigationCirclesCount;
        private LineVisual3D _zAxisLineVisual3D;

        private bool _isZAxisUp;

        public CameraNavigationCirclesSample()
        {
            InitializeComponent();

            // CameraNavigationCircles with default values
            var cameraNavigator1 = new CameraNavigationCircles()
            {
                TargetCamera = Camera1,

                // The following are the default values:
                //AxisType = CameraNavigationCircles.AxisTypes.Lines,
                //PositiveAxisCircleVisibility = CameraNavigationCircles.NavigationCircleVisibility.Always,
                //NegativeAxisCircleVisibility = CameraNavigationCircles.NavigationCircleVisibility.Always,
                //PositiveAxisNameVisibility = CameraNavigationCircles.NavigationCircleVisibility.Always,
                //NegativeAxisNameVisibility = CameraNavigationCircles.NavigationCircleVisibility.OnMouseOver,
                //PositiveAxisCircleFillBrush = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
                //NegativeAxisCircleFillBrush = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
                //PositiveAxisTextBrush = null,
                //NegativeAxisTextBrush = null
            };

            AddCameraNavigationCircles(cameraNavigator1);


            // CameraNavigationCircles with filled positive axis circles
            var cameraNavigator2 = new CameraNavigationCircles()
            {
                TargetCamera = Camera1,
                AxisType = CameraNavigationCircles.AxisTypes.Lines,
                PositiveAxisCircleVisibility = CameraNavigationCircles.NavigationCircleVisibility.Always,
                NegativeAxisCircleVisibility = CameraNavigationCircles.NavigationCircleVisibility.Always,
                PositiveAxisNameVisibility = CameraNavigationCircles.NavigationCircleVisibility.Always,
                NegativeAxisNameVisibility = CameraNavigationCircles.NavigationCircleVisibility.OnMouseOver,
                PositiveAxisCircleFillBrush = null,
                NegativeAxisCircleFillBrush = new SolidColorBrush(Colors.White) { Opacity = 0.8 },
                PositiveAxisTextBrush = Brushes.Black,
                NegativeAxisTextBrush = Brushes.Black
            };

            AddCameraNavigationCircles(cameraNavigator2);


            // CameraNavigationCircles with line with arrows. Axis circles are shown on mouse over.
            var cameraNavigator3 = new CameraNavigationCircles()
            {
                TargetCamera = Camera1,
                AxisType = CameraNavigationCircles.AxisTypes.LinesWithArrows,
                PositiveAxisCircleVisibility = CameraNavigationCircles.NavigationCircleVisibility.OnMouseOver,
                NegativeAxisCircleVisibility = CameraNavigationCircles.NavigationCircleVisibility.OnMouseOver,
                PositiveAxisNameVisibility = CameraNavigationCircles.NavigationCircleVisibility.Always,
                NegativeAxisNameVisibility = CameraNavigationCircles.NavigationCircleVisibility.OnMouseOver,
            };

            AddCameraNavigationCircles(cameraNavigator3);



            // CameraNavigationCircles with a minified 3D box in the center
            var cameraNavigator4 = new CameraNavigationCircles()
            {
                TargetCamera = Camera1,
                AxisType = CameraNavigationCircles.AxisTypes.Lines,
                PositiveAxisCircleVisibility = CameraNavigationCircles.NavigationCircleVisibility.Always,
                NegativeAxisCircleVisibility = CameraNavigationCircles.NavigationCircleVisibility.OnMouseOver,
                PositiveAxisNameVisibility = CameraNavigationCircles.NavigationCircleVisibility.Always,
                NegativeAxisNameVisibility = CameraNavigationCircles.NavigationCircleVisibility.OnMouseOver,
                PositiveAxisCircleFillBrush = Brushes.White, // Set circle fill color to White (from semi transparent white) to prevent seeing axis line through circle
                NegativeAxisCircleFillBrush = Brushes.White
            };

            // Get the model of the box with AB3D text
            var boxModel = Application.Current.TryFindResource("Ab3d_Box_Model") as Model3D;

            // Scale the model
            double modelSize = cameraNavigator4.AxisLength3D * 0.6; // half of the axis length
            var modelTransform3D = Ab3d.Utilities.ModelUtils.GetPositionedAndScaledModelTransform3D(boxModel, new Point3D(0, 0, 0), PositionTypes.Center, new Size3D(modelSize, modelSize, modelSize));

            var modelVisual3D = new ModelVisual3D()
            {
                Content = boxModel,
                Transform = modelTransform3D
            };

            var viewport3D = new Viewport3D();

            viewport3D.Children.Add(modelVisual3D);

            // Add axis lines to the minified 3D scene
            // This way the axis lines with go through the 3D model and will not be only rendered behind the model (when using only AxisTypes.Lines)
            // Note that it is not possible to extend the following 3D lines exactly to the beginning of the 2D circle, so we just show them at 80% of the length
            // the remaining part will be rendered by the CameraNavigationCircles 2D lines.
            viewport3D.Children.Add(new LineVisual3D()
            {
                StartPosition = new Point3D(0, 0, 0),
                EndPosition = new Point3D(cameraNavigator4.AxisLength3D * 0.8, 0, 0),
                LineColor = cameraNavigator4.XAxisColor,
                LineThickness = cameraNavigator4.AxisLineThickness
            });

            viewport3D.Children.Add(new LineVisual3D()
            {
                StartPosition = new Point3D(0, 0, 0),
                EndPosition = new Point3D(0, cameraNavigator4.AxisLength3D * 0.8, 0),
                LineColor = cameraNavigator4.YAxisColor,
                LineThickness = cameraNavigator4.AxisLineThickness
            });

            // We need to save the z axis to a field so we can swap its direction when we customize the axes
            _zAxisLineVisual3D = new LineVisual3D()
            {
                StartPosition = new Point3D(0, 0, 0),
                EndPosition = new Point3D(0, 0, cameraNavigator4.AxisLength3D * 0.8),
                LineColor = cameraNavigator4.ZAxisColor,
                LineThickness = cameraNavigator4.AxisLineThickness
            };
            viewport3D.Children.Add(_zAxisLineVisual3D);


            // Create a TargetPositionCamera that will be synchronized with the main scene camera
            var targetPositionCamera = new TargetPositionCamera()
            {
                TargetViewport3D = viewport3D,
                Heading = Camera1.Heading,
                Attitude = Camera1.Attitude,
                Distance = 400
            };

            targetPositionCamera.Refresh();

            Camera1.CameraChanged += (sender, args) =>
            {
                targetPositionCamera.Heading = Camera1.Heading;
                targetPositionCamera.Attitude = Camera1.Attitude;
            };

            // Add the Viewport3D that shows the minified 3D box to the content of the CameraNavigationCircles
            cameraNavigator4.Content = viewport3D;

            AddCameraNavigationCircles(cameraNavigator4);



            // ADVANCED CUSTOMIZATION:
            // Advanced customization is possible by deriving your own class from CameraNavigationCircles.
            // Then you can override the following methods:
            // OnMouseEnter, OnMouseLeave, OnAxisSelected, OnAxisDeselected, OnCreateAxisNameTextBlock, OnCreateAxisLine and OnCreateAxisCircle
        }

        private void AddCameraNavigationCircles(CameraNavigationCircles cameraNavigationCircles)
        {
            // Add CameraNavigationCircles to the grid
            NavigationCirclesGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            Grid.SetRow(cameraNavigationCircles, 0);
            Grid.SetColumn(cameraNavigationCircles, _navigationCirclesCount + 1);

            cameraNavigationCircles.Margin = new Thickness(10, 0, 10, 0);

            NavigationCirclesGrid.Children.Add(cameraNavigationCircles);

            _navigationCirclesCount++;


            // Add settings controls:
            double columnWidth = 130;

            AddComboBox(1, typeof(CameraNavigationCircles.AxisTypes), cameraNavigationCircles.AxisType.ToString(), columnWidth, 1, (selectedItemName, selectedItemIndex) => cameraNavigationCircles.AxisType = (CameraNavigationCircles.AxisTypes)Enum.Parse(typeof(CameraNavigationCircles.AxisTypes), selectedItemName));

            AddComboBox(2, typeof(CameraNavigationCircles.NavigationCircleVisibility), cameraNavigationCircles.PositiveAxisCircleVisibility.ToString(), columnWidth, 3, (selectedItemName, selectedItemIndex) => cameraNavigationCircles.PositiveAxisCircleVisibility = (CameraNavigationCircles.NavigationCircleVisibility)Enum.Parse(typeof(CameraNavigationCircles.NavigationCircleVisibility), selectedItemName));
            AddComboBox(3, typeof(CameraNavigationCircles.NavigationCircleVisibility), cameraNavigationCircles.PositiveAxisNameVisibility.ToString(), columnWidth, 1, (selectedItemName, selectedItemIndex) => cameraNavigationCircles.PositiveAxisNameVisibility = (CameraNavigationCircles.NavigationCircleVisibility)Enum.Parse(typeof(CameraNavigationCircles.NavigationCircleVisibility), selectedItemName));

            AddComboBox(4, typeof(CameraNavigationCircles.NavigationCircleVisibility), cameraNavigationCircles.NegativeAxisCircleVisibility.ToString(), columnWidth, 3, (selectedItemName, selectedItemIndex) => cameraNavigationCircles.NegativeAxisCircleVisibility = (CameraNavigationCircles.NavigationCircleVisibility)Enum.Parse(typeof(CameraNavigationCircles.NavigationCircleVisibility), selectedItemName));
            AddComboBox(5, typeof(CameraNavigationCircles.NavigationCircleVisibility), cameraNavigationCircles.NegativeAxisNameVisibility.ToString(), columnWidth, 1, (selectedItemName, selectedItemIndex) => cameraNavigationCircles.NegativeAxisNameVisibility = (CameraNavigationCircles.NavigationCircleVisibility)Enum.Parse(typeof(CameraNavigationCircles.NavigationCircleVisibility), selectedItemName));
            
            AddComboBox(6, typeof(CameraNavigationCircles.NavigationCircleVisibility), cameraNavigationCircles.BackgroundCircleVisibility.ToString(), columnWidth, 3, (selectedItemName, selectedItemIndex) => cameraNavigationCircles.BackgroundCircleVisibility = (CameraNavigationCircles.NavigationCircleVisibility)Enum.Parse(typeof(CameraNavigationCircles.NavigationCircleVisibility), selectedItemName));



            var checkBox1 = new CheckBox()
            {
                IsChecked = cameraNavigationCircles.PositiveAxisTextBrush == null,
                Margin = new Thickness(10, 3, 0, 0)
            };

            checkBox1.Checked += delegate (object sender, RoutedEventArgs args)
            {
                // When PositiveAxisTextBrush or NegativeAxisTextBrush are null, then axis color is used instead of fixed color.
                cameraNavigationCircles.PositiveAxisTextBrush = null;
                cameraNavigationCircles.NegativeAxisTextBrush = null;

            };

            checkBox1.Unchecked += delegate (object sender, RoutedEventArgs args)
            {
                // Define a fixed color for PositiveAxisTextBrush and NegativeAxisTextBrush
                cameraNavigationCircles.PositiveAxisTextBrush = new SolidColorBrush(Colors.Black);
                cameraNavigationCircles.NegativeAxisTextBrush = new SolidColorBrush(Colors.Black);
            };

            Grid.SetRow(checkBox1, 7);
            Grid.SetColumn(checkBox1, _navigationCirclesCount);

            NavigationCirclesGrid.Children.Add(checkBox1);



            var checkBox2 = new CheckBox()
            {
                IsChecked = cameraNavigationCircles.PositiveAxisCircleFillBrush == null,
                Margin = new Thickness(10, 3, 0, 0)
            };

            checkBox2.Checked += delegate(object sender, RoutedEventArgs args)
            {
                cameraNavigationCircles.PositiveAxisCircleFillBrush = null;
            };

            checkBox2.Unchecked += delegate(object sender, RoutedEventArgs args)
            {
                cameraNavigationCircles.PositiveAxisCircleFillBrush = new SolidColorBrush(Colors.White) { Opacity = 0.8 };
            };

            Grid.SetRow(checkBox2, 8);
            Grid.SetColumn(checkBox2, _navigationCirclesCount);

            NavigationCirclesGrid.Children.Add(checkBox2);
            

            
            var checkBox3 = new CheckBox()
            {
                IsChecked = cameraNavigationCircles.NegativeAxisCircleFillBrush == null,
                Margin = new Thickness(10, 0, 0, 0)
            };

            checkBox3.Checked += delegate(object sender, RoutedEventArgs args)
            {
                cameraNavigationCircles.NegativeAxisCircleFillBrush = null;
            };

            checkBox3.Unchecked += delegate(object sender, RoutedEventArgs args)
            {
                cameraNavigationCircles.NegativeAxisCircleFillBrush = new SolidColorBrush(Colors.White) { Opacity = 0.8 };
            };

            Grid.SetRow(checkBox3, 9);
            Grid.SetColumn(checkBox3, _navigationCirclesCount);

            NavigationCirclesGrid.Children.Add(checkBox3);
        }

        private void AddComboBox(int rowIndex, Type enumType, string selectedName, double width, double topMargin, Action<string, int> itemSelectedAction)
        {
            var enumNames = Enum.GetNames(enumType);

            var comboBox = new ComboBox()
            {
                ItemsSource = enumNames,
                SelectedIndex = Array.IndexOf(enumNames, selectedName),
                Width = width,
                Margin = new Thickness(10, topMargin, 0, 0)
            };

            comboBox.SelectionChanged += delegate(object sender, SelectionChangedEventArgs args)
            {
                var changedComboBox = (ComboBox)sender;
                itemSelectedAction((string)changedComboBox.SelectedItem, changedComboBox.SelectedIndex);
            };

            Grid.SetRow(comboBox, rowIndex);
            Grid.SetColumn(comboBox, _navigationCirclesCount);

            NavigationCirclesGrid.Children.Add(comboBox);
        }

        private void ToggleBackgroundButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ViewportBorder.Background == Brushes.White)
                ViewportBorder.Background = Brushes.Black;
            else
                ViewportBorder.Background = Brushes.White;
        }

        private void OnSelectClosestAxisCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            foreach (var cameraNavigationCircles in NavigationCirclesGrid.Children.OfType<CameraNavigationCircles>())
                cameraNavigationCircles.SelectClosestAxis = SelectClosestAxisCheckBox.IsChecked ?? false;
        }
        
        private void OnIsCameraRotationEnabledCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            foreach (var cameraNavigationCircles in NavigationCirclesGrid.Children.OfType<CameraNavigationCircles>())
                cameraNavigationCircles.IsCameraRotationEnabled = IsCameraRotationEnabledCheckBox.IsChecked ?? false;
        }
        
        private void OnIsRotateToAxisEnabledCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            foreach (var cameraNavigationCircles in NavigationCirclesGrid.Children.OfType<CameraNavigationCircles>())
                cameraNavigationCircles.IsRotateToAxisEnabled = IsRotateToAxisEnabledCheckBox.IsChecked ?? false;
        }
        
        private void OnShowOppositeAxisWhenClickedOnCurrentAxisCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            foreach (var cameraNavigationCircles in NavigationCirclesGrid.Children.OfType<CameraNavigationCircles>())
                cameraNavigationCircles.ShowOppositeAxisWhenClickedOnCurrentAxis = ShowOppositeAxisWhenClickedOnCurrentAxisCheckBox.IsChecked ?? false;
        }

        private void ModifierKeyToPreserveAttitudeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBoxItem = (ComboBoxItem)ModifierKeyToPreserveAttitudeComboBox.SelectedItem;
            string selectedText = (string)comboBoxItem.Content;

            var modifierKeyToPreserveAttitude = (ModifierKeys)Enum.Parse(typeof(ModifierKeys), selectedText);

            foreach (var cameraNavigationCircles in NavigationCirclesGrid.Children.OfType<CameraNavigationCircles>())
                cameraNavigationCircles.ModifierKeyToPreserveAttitude = modifierKeyToPreserveAttitude;
        }

        private void ModifierKeyForOppositeAxisComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBoxItem = (ComboBoxItem)ModifierKeyForOppositeAxisComboBox.SelectedItem;
            string selectedText = (string)comboBoxItem.Content;

            var modifierKeyForOppositeAxis = (ModifierKeys)Enum.Parse(typeof(ModifierKeys), selectedText);

            foreach (var cameraNavigationCircles in NavigationCirclesGrid.Children.OfType<CameraNavigationCircles>())
                cameraNavigationCircles.ModifierKeyForOppositeAxis = modifierKeyForOppositeAxis;
        }

        private void CameraRotationAmountComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBoxItem = (ComboBoxItem)CameraRotationAmountComboBox.SelectedItem;
            string selectedText = (string)comboBoxItem.Content;

            double cameraRotationAmount = double.Parse(selectedText);

            foreach (var cameraNavigationCircles in NavigationCirclesGrid.Children.OfType<CameraNavigationCircles>())
                cameraNavigationCircles.CameraRotationAmount = cameraRotationAmount;
        }
        
        private void AxisClickAnimationDurationComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            double axisClickAnimationDuration = AxisClickAnimationDurationComboBox.SelectedIndex * 0.1;

            foreach (var cameraNavigationCircles in NavigationCirclesGrid.Children.OfType<CameraNavigationCircles>())
                cameraNavigationCircles.AxisClickAnimationDuration = axisClickAnimationDuration;
        }

        private void AxisTextSizeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBoxItem = (ComboBoxItem)AxisTextSizeComboBox.SelectedItem;
            string selectedText = (string)comboBoxItem.Content;

            double axisTextSize = double.Parse(selectedText);
            
            foreach (var cameraNavigationCircles in NavigationCirclesGrid.Children.OfType<CameraNavigationCircles>())
            {
                cameraNavigationCircles.AxisTextSize = axisTextSize;

                // NOTE:
                // Changing the AxisTextSize will also change the radius of axis circles. 
                // This is done because AxisCircleRadius is set to 0. In this case the used circle radius is calculated as AxisTextSize * 0.7.
                // If you want to manually set the radius if axis circles, set the AxisCircleRadius, for example:
                //cameraNavigationCircles.AxisCircleRadius = axisTextSize * 0.5;
            }
        }

        private void CustomizeAxesButton_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var cameraNavigationCircles in NavigationCirclesGrid.Children.OfType<CameraNavigationCircles>())
            {
                if (_isZAxisUp)
                    cameraNavigationCircles.UseYUpAxis();
                else
                    cameraNavigationCircles.UseZUpAxis();

                // Calling UseZUpAxis is the same as:
                //
                //cameraNavigationCircles.CustomizeAxes(xAxisVector: new Vector3D(1, 0, 0), yAxisVector: new Vector3D(0, 0, -1), zAxisVector: new Vector3D(0, 1, 0));
                //
                // or:
                //
                //cameraNavigationCircles.CustomizeAxes("X", "-X", Colors.Red, new Vector3D(1, 0, 0),
                //                                      "Z", "-Z", Colors.ForestGreen, new Vector3D(0, 1, 0),
                //                                      "Y", "-Y", Colors.RoyalBlue, new Vector3D(0, 0, -1));

                // Calling UseYUpAxis (sets the default axes) is the same as:
                //
                //cameraNavigationCircles.CustomizeAxes(xAxisVector: new Vector3D(1, 0, 0), yAxisVector: new Vector3D(0, 1, 0), zAxisVector: new Vector3D(0, 0, 1));
            }

            // Swap the Z axis direction
            _zAxisLineVisual3D.EndPosition = new Point3D(-_zAxisLineVisual3D.EndPosition.X, -_zAxisLineVisual3D.EndPosition.Y, -_zAxisLineVisual3D.EndPosition.Z);

            if (_isZAxisUp)
            {
                CustomizeAxesButton.Content = "Customize axes (Z up)";
                _isZAxisUp = false;
            }
            else
            {
                CustomizeAxesButton.Content = "Customize axes (Y up)";
                _isZAxisUp = true;
            }
        }
    }
}
