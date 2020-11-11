using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab.Common;
using Ab3d.Cameras;
using Ab3d.Common.Cameras;
using Ab3d.Utilities;
using Assimp;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace Ab3d.PowerToys.Samples.Wpf3DFile
{
    /// <summary>
    /// Interaction logic for Wpf3DFileExporterSample.xaml
    /// </summary>
    public partial class Wpf3DFileExporterSample : Page
    {
        private Model3D _rootModel3D;

        private string _fileName;


        public Wpf3DFileExporterSample()
        {
            InitializeComponent();

            CreateTestScene();

            // Add drag and drop handler for all file extensions
            var dragAndDropHelper = new DragAndDropHelper(ViewportBorder, "*");
            dragAndDropHelper.FileDropped += (sender, e) => LoadModel(e.FileName);
        }

        private void CreateTestScene()
        {
            var rootModel3DGroup = new Model3DGroup();

            var greenMaterial = new DiffuseMaterial(Brushes.Green);
            var redMaterial = new DiffuseMaterial(Brushes.Red);

            var boxMesh3D = new Ab3d.Meshes.BoxMesh3D(new Point3D(0, 0, 0), new Size3D(1, 1, 1), 1, 1, 1).Geometry;

            // Create 7 x 7 boxes with different height
            for (int y = -3; y <= 3; y++)
            {
                for (int x = -3; x <= 3; x++)
                {
                    // Height is based on the distance from the center
                    double height = (5 - Math.Sqrt(x * x + y * y)) * 60;

                    // Instead of creating MeshGeometry3D for each box, we reuse the standard box mesh
                    //var boxGeometryModel3D = Ab3d.Models.Model3DFactory.CreateBox(centerPosition: new Point3D(x * 100, height / 2, y * 100),
                    //                                                              size: new Size3D(80, height, 80),
                    //                                                              material: height > 200 ? redMaterial : greenMaterial);

                    // Create the 3D Box 
                    var boxGeometryModel3D = new GeometryModel3D(boxMesh3D, material: height > 200 ? redMaterial : greenMaterial);

                    // Scale: 80, height, 80
                    // Translate: x * 100, height / 2, y * 100
                    boxGeometryModel3D.Transform = new MatrixTransform3D(new Matrix3D(80, 0, 0, 0,
                                                                                      0, height, 0, 0,
                                                                                      0, 0, 80, 0, 
                                                                                      x * 100, height / 2, y * 100, 1));

                    // To preserve the object names we can fill the names into the _namedObjects dictionary
                    boxGeometryModel3D.SetName(string.Format("Box_{0}_{1}", x + 4, y + 4));

                    rootModel3DGroup.Children.Add(boxGeometryModel3D);
                }
            }

            ContentModelVisual3D.Content = rootModel3DGroup;
            _rootModel3D = rootModel3DGroup;
        }

        private void LoadModel(string fileName)
        {
            // Create an instance of AssimpWpfImporter
            var assimpWpfImporter = new Ab3d.Assimp.AssimpWpfImporter();

            string fileExtension = System.IO.Path.GetExtension(fileName);

            Model3D readModel3D = null;
            object  readCamera  = null;

            if (fileExtension.Equals(".wpf3d", StringComparison.OrdinalIgnoreCase))
            {
                // Use Wpf3DFile importer to read wpf3d file format

                var wpf3DFile = new Ab3d.Utilities.Wpf3DFile();

                // We could read only header (description and thumbnail without any model data):
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
            }
            else if (assimpWpfImporter.IsImportFormatSupported(fileExtension))
            {
                // Use assimp importer to read many other file formats

                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    assimpWpfImporter.DefaultMaterial        = new DiffuseMaterial(Brushes.Silver);
                    assimpWpfImporter.AssimpPostProcessSteps = PostProcessSteps.Triangulate;
                    assimpWpfImporter.ReadPolygonIndices     = true;

                    // Read model from file
                    readModel3D = assimpWpfImporter.ReadModel3D(fileName, texturesPath: null); // we can also define a textures path if the textures are located in some other directory (this is parameter can be skipped, but is defined here so you will know that you can use it)
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading file with Assimp importer:\r\n" + ex.Message);
                }
                finally
                {
                    // Dispose unmanaged resources
                    assimpWpfImporter.Dispose();

                    Mouse.OverrideCursor = null;
                }
            }
            else
            {
                MessageBox.Show("Not supported file extension: " + fileExtension);
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
        
        private void ExportButton_OnClick(object sender, RoutedEventArgs e)
        {
            string fileName;

            if (_rootModel3D == null)
                return;

            if (_fileName != null)
                fileName = System.IO.Path.ChangeExtension(_fileName, ".wpf3d");
            else
                fileName = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\model3D.wpf3d";

            var thumbnail = Camera1.RenderToBitmap(128, 128, 4);

            
            var wpf3DFileExportUserControl = new Wpf3DFileExportUserControl
            {
                FileName = fileName, 
                RootModel = _rootModel3D, 
                Camera = Camera1,
                Thumbnail = thumbnail,
                SourceFileName = _fileName,
                Margin = new Thickness(10, 0, 10, 0)
            };

            var window = new Window
            {
                Width   = 450,
                Height  = 595,
                Title   = "Export 3D scene",
                Content = wpf3DFileExportUserControl,
                Owner   = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            wpf3DFileExportUserControl.SaveButtonClicked += delegate(object o, EventArgs args)
            {
                window.Close();
            };

            wpf3DFileExportUserControl.CancelButtonClicked += delegate(object o, EventArgs args)
            {
                window.Close();
            };

            window.ShowDialog();
        }
    }
}
