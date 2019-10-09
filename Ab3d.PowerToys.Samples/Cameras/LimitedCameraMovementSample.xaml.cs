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

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for LimitedCameraMovementSample.xaml
    /// </summary>
    public partial class LimitedCameraMovementSample : Page
    {
        private int _previewCameraChangedCounter;
        private int _cameraChangedCounter;
        private int _preventedChangeCounter;

        public LimitedCameraMovementSample()
        {
            InitializeComponent();

            _previewCameraChangedCounter = 0;
            _cameraChangedCounter = 0;
            _preventedChangeCounter = 0;
        }

        private void SceneCamera1_PreviewCameraChanged(object sender, Ab3d.Common.Cameras.PreviewCameraChangedRoutedEventArgs e)
        {
            double newValue;

            if (!this.IsLoaded)
                return;

            _previewCameraChangedCounter++;

            // First check Attitude
            // This can be done by comparing the Property with the AttitudeProperty (see class diagram to check on which class the property is defined)
            // or by comparing the Property.Name with "Attitude" (done in the Distance check)
            if (e.Property == Ab3d.Cameras.SphericalCamera.AttitudeProperty)
            {
                newValue = (double)e.NewValue;

                if (newValue < GetComboBoxSelectedValue(MinAttituideComboBox))
                    e.Handled = true; // this prevent changing the camera

                if (newValue > GetComboBoxSelectedValue(MaxAttituideComboBox))
                    e.Handled = true; // this prevent changing the camera
            }

            if (e.Property.Name == "Distance")
            {
                newValue = (double)e.NewValue;

                if (newValue < GetComboBoxSelectedValue(MinDistanceComboBox))
                    e.Handled = true; // this prevent changing the camera

                if (newValue > GetComboBoxSelectedValue(MaxDistanceComboBox))
                    e.Handled = true; // this prevent changing the camera
            }

            if (e.Handled)
                _preventedChangeCounter++;

            UpdateCounters();
        }

        private void SceneCamera1_CameraChanged(object sender, Ab3d.Common.Cameras.CameraChangedRoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            _cameraChangedCounter++;

            UpdateCounters();
        }

        private double GetComboBoxSelectedValue(ComboBox checkBox)
        {
            ComboBoxItem comboBoxItem;
            double selectedValue;

            comboBoxItem = checkBox.SelectedItem as ComboBoxItem;

            selectedValue = double.Parse(((string)comboBoxItem.Content));

            return selectedValue;
        }

        private void UpdateCounters()
        {
            PreviewCameraChangedTextBlock.Text = _previewCameraChangedCounter.ToString();
            PreventedTextBlock.Text            = _preventedChangeCounter.ToString();
            CameraChangedTextBlock.Text        = _cameraChangedCounter.ToString();
        }

        private void OnDistanceComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            double minDistance = GetComboBoxSelectedValue(MinDistanceComboBox);
            double maxDistance = GetComboBoxSelectedValue(MaxDistanceComboBox);

            if (Camera1.Distance < minDistance)
                Camera1.Distance = minDistance;
            else if (Camera1.Distance > maxDistance)
                Camera1.Distance = maxDistance;
        }

        private void OnAttituideComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            double minAttitude = GetComboBoxSelectedValue(MinAttituideComboBox);
            double maxAttitude = GetComboBoxSelectedValue(MaxAttituideComboBox);

            if (Camera1.Attitude < minAttitude)
                Camera1.Attitude = minAttitude;
            else if (Camera1.Attitude > maxAttitude)
                Camera1.Attitude = maxAttitude;
        }
    }
}

