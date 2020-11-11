using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Ab.Common;
using Ab3d.Cameras;
using Ab3d.Common.Cameras;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.Wpf3DFile
{
    /// <summary>
    /// Interaction logic for Wpf3DFileImporterSample.xaml
    /// </summary>
    public partial class Wpf3DFileImporterSample : Page
    {
        private Model3D _rootModel3D;

        private string _fileName;

        public Wpf3DFileImporterSample()
        {
            InitializeComponent();

            // Add drag and drop handler for all file extensions
            var dragAndDropHelper = new DragAndDropHelper(ViewportBorder, ".wpf3d");
            dragAndDropHelper.FileDropped += (sender, e) => LoadWpf3DFile(e.FileName);
        }


        private void LoadWpf3DFile(string fileName)
        {
            string fileExtension = System.IO.Path.GetExtension(fileName);

            Model3D readModel3D = null;
            object  readCamera  = null;

            if (!fileExtension.Equals(".wpf3d", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("This sample can import only wpf3d files");
                return;
            }

            InfoTextBlock.Visibility = Visibility.Collapsed;


            // Use Wpf3DFile importer to read wpf3d file format
            var wpf3DFile = new Ab3d.Utilities.Wpf3DFile();

            // We could read only header (description and thumbnail without any model data - see importer sample):
            //wpf3DFile.ReadFileHeader(fileName);

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                readModel3D = wpf3DFile.ReadFile(fileName);
                readCamera  = wpf3DFile.Camera;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading file with Wpf3DFile:\r\n" + ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
      

            // Show the model
            if (readModel3D != null)
            {
                ShowModel(readModel3D, readCamera);
                _fileName = fileName;
            }
        }

        private void ShowModel(Model3D model3D, object readCamera)
        {
            ContentModelVisual3D.Children.Clear();
            ContentModelVisual3D.Content = model3D;


            // If the read camera is TargetPositionCamera, then we can use its settings
            var readTargetPositionCamera = readCamera as TargetPositionCamera;
            if (readTargetPositionCamera != null)
            {
                Camera1.Heading        = readTargetPositionCamera.Heading;
                Camera1.Attitude       = readTargetPositionCamera.Attitude;
                Camera1.Distance       = readTargetPositionCamera.Distance;
                Camera1.TargetPosition = readTargetPositionCamera.TargetPosition;
                Camera1.Offset         = readTargetPositionCamera.Offset;

                // Do not read CameraType, CameraWidth, FieldOfView, Bank, NearPlaneDistance and FarPlaneDistance
            }
            else
            {
                // Otherwise set camera based on the models center and size
                Camera1.TargetPosition = model3D.Bounds.GetCenterPosition();
                Camera1.Distance       = model3D.Bounds.GetDiagonalLength() * 1.2;

                Camera1.Offset   = new Vector3D(0, 0, 0);
                Camera1.Heading  = 30;
                Camera1.Attitude = -30;
            }

            // If the read model already define some lights, then do not show the Camera's light
            if (ModelUtils.HasAnyLight(model3D))
                Camera1.ShowCameraLight = ShowCameraLightType.Never;
            else
                Camera1.ShowCameraLight = ShowCameraLightType.Always;

            _rootModel3D = model3D;
        }

        private void ImportButton_OnClick(object sender, RoutedEventArgs e)
        {
            var importWpf3DFileWindow = new ImportWpf3DFileWindow()
            {
                SelectedFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\wpf3d\\")
            };

            importWpf3DFileWindow.ShowDialog();

            if (!string.IsNullOrEmpty(importWpf3DFileWindow.SelectedFileName))
                LoadWpf3DFile(importWpf3DFileWindow.SelectedFileName);
        }
    }
}
