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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab.Common;
using Ab3d.Assimp;
using Ab3d.Common.Cameras;
using Ab3d.Utilities;
using Assimp;

namespace Ab3d.PowerToys.Samples.AssimpSamples
{

    // This sample shows how to export WPF 3D scene or individual object to a file that is supported by assimp (see below for a list)
    // The sample is using AssimpWpfExporter that internally uses native assimp library.
    //
    // Exporting 3D lines:
    // 3D lines in Ab3d.PowerToys are created with triangles that are updated to always face the camera.
    // When the 3D scene with 3D lines is exported, the current MeshGeometry3D objects of the 3D lines is also exported.
    // But this means that because after exporting the MeshGeometry3D for the 3D line is fixed, it will not look good under some other camera angle.
    // Therefore it is recommended to use TubeLineVisual3D or TubePathVisual3D objects instead of 3D lines. Those two objects are correctly exported to files.


    // The following export formats are supported with the current version of assimp.
    // Note that some file formats are better implemented than other:

    // FormatId | Extension | Description
    // collada  | .dae      | COLLADA - Digital Asset Exchange Schema
    // x        | .x        | X Files
    // stp      | .stp      | Step Files
    // obj      | .obj      | Wavefront OBJ format
    // objnomtl | .obj      | Wavefront OBJ format without material file
    // stl      | .stl      | Stereolithography
    // stlb     | .stl      | Stereolithography (binary)
    // ply      | .ply      | Stanford Polygon Library
    // plyb     | .ply      | Stanford Polygon Library (binary)
    // 3ds      | .3ds      | Autodesk 3DS (legacy)
    // gltf     | .gltf     | GL Transmission Format
    // glb      | .glb      | GL Transmission Format (binary)
    // gltf2    | .gltf     | GL Transmission Format v. 2
    // glb2     | .glb      | GL Transmission Format v. 2 (binary)
    // assbin   | .assbin   | Assimp Binary
    // assxml   | .assxml   | Assxml Document
    // x3d      | .x3d      | Extensible 3D
    // fbx      | .fbx      | Autodesk FBX (binary)
    // fbxa     | .fbx      | Autodesk FBX (ascii)
    // 3mf      | .3mf      | The 3MF-File-Format 

    // List of file formats was get with the following line:
    //var assimpWpfExporter = new AssimpWpfExporter();
    //_exportFormatDescriptions = assimpWpfExporter.ExportFormatDescriptions;
    //System.Diagnostics.Debug.WriteLine(string.Join("\r\n", _exportFormatDescriptions.Select(f => f.FormatId.PadRight(8) + " | ." + f.FileExtension.PadRight(8) + " | " + f.Description).ToArray()));



    /// <summary>
    /// Interaction logic for AssimpWpfExporterSample.xaml
    /// </summary>
    public partial class AssimpWpfExporterSample : Page
    {
        private AssimpWpfImporter _assimpWpfImporter;

        private ExportFormatDescription[] _exportFormatDescriptions;

        private bool _isInternalCameraChange;

        private string _selectedExportFormatId;

        private Dictionary<string, object> _namedObjects;

        public AssimpWpfExporterSample()
        {
            InitializeComponent();

            AssimpWpfImporter.LoadAssimpNativeLibrary(AppDomain.CurrentDomain.BaseDirectory);


            var assimpWpfExporter = new AssimpWpfExporter();
            _exportFormatDescriptions = assimpWpfExporter.ExportFormatDescriptions;


            for (int i = 0; i < _exportFormatDescriptions.Length; i++)
            {
                var comboBoxItem = new ComboBoxItem()
                {
                    Content = string.Format("{0} (.{1})", _exportFormatDescriptions[i].Description, _exportFormatDescriptions[i].FileExtension),
                    Tag     = _exportFormatDescriptions[i].FormatId
                };

                ExportTypeComboBox.Items.Add(comboBoxItem);
            }


            ExportTypeComboBox.SelectedIndex = 0; // Use Collada file format by default
            _selectedExportFormatId = _exportFormatDescriptions[ExportTypeComboBox.SelectedIndex].FormatId;


            // Use helper class (defined in this sample project) to load the native assimp libraries
            // IMPORTANT: See commend in the AssimpLoader class for details on how to prepare your project to use assimp library.
            AssimpLoader.LoadAssimpNativeLibrary();

            _assimpWpfImporter = new AssimpWpfImporter();
            _assimpWpfImporter.AssimpPostProcessSteps = PostProcessSteps.Triangulate;

            CreateTestScene();

            // Set initial output file name
            OutputFileName.Text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AssimpExport.dae");

            // Add drag and drop handler for all file extensions
            var dragAndDropHelper = new DragAndDropHelper(ViewportBorder, "*");
            dragAndDropHelper.FileDroped += (sender, e) => LoadModel(e.FileName);
        }

        // Export specified model3D (we could also export Visual3D or entire Viewport3D - see commented code below)
        private bool ExportViewport3D(string fileName, string exportFormatId, Viewport3D viewport3D, Dictionary<string, object> namedObjects)
        {
            // First create an instance of AssimpWpfExporter
            var assimpWpfExporter = new AssimpWpfExporter();

            // To export objects with names, we can use one of the following methods:
            // 1) Set object names with SetName extension method that is added by Ab3d.PowerToys (for example: boxVisual3D.SetName("BoxVisual1");
            // 2) Set assimpWpfExporter.NamedObjects to a Dictionary<string, object> dictionary with set names as keys and objects as values
            // 3) Set assimpWpfExporter.ObjectNames to a Dictionary<object, string> dictionary with set objects as keys and names as values

            // Here we use NamedObjects because NamedObjects dictionary is also set when the 3D models are read from file
            assimpWpfExporter.NamedObjects = _namedObjects;


            // We can export Model3D, Visual3D or entire Viewport3D:
            //assimpWpfExporter.AddModel(model3D);
            //assimpWpfExporter.AddVisual3D(ContentModelVisual3D);
            //assimpWpfExporter.AddViewport3D(MainViewport);

            // Here we export Viewport3D:
            assimpWpfExporter.AddViewport3D(viewport3D);

            bool isExported;

            try
            {
                isExported = assimpWpfExporter.Export(fileName, exportFormatId);

                if (!isExported)
                    MessageBox.Show("Not exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting:\r\n" + ex.Message);
                isExported = false;
            }

            return isExported;
        }

        private void LoadExportedScene(string fileName)
        {
            // Now read the exported file and show in the right Viewport3D

            Model3D readModel3D;

            try
            {
                // With uncommenting the following few lines we would use Ab3d.ReaderObj from Ab3d.PowerToys to read obj files instead of Assimp
                //if (fileName.EndsWith(".obj", ignoreCase: true, culture: CultureInfo.InvariantCulture))
                //{
                //    var readerObj = new Ab3d.ReaderObj();
                //    readModel3D = readerObj.ReadModel3D(fileName);
                //}
                //else
                //{
                    var assimpWpfImporter = new AssimpWpfImporter();
                    readModel3D = assimpWpfImporter.ReadModel3D(fileName);
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading file:\r\n" + ex.Message);
                return;
            }


            var modelVisual3D = new ModelVisual3D();
            modelVisual3D.Content = readModel3D;

            MainViewport2.Children.Clear();
            MainViewport2.Children.Add(modelVisual3D);

            // Set Camera2 from Camera1
            Camera2.TargetPosition = Camera1.TargetPosition;
            Camera2.Heading = Camera1.Heading;
            Camera2.Attitude = Camera1.Attitude;
            Camera2.Distance = Camera1.Distance;

            Camera2.Refresh(); // This will regenerate light that was cleared with MainViewport2.Children.Clear()

            ExportedSceneTitleTextBlock.Text = "Scene imported from " + fileName;
        }

        private void CreateTestScene()
        {
            ContentModelVisual3D.Children.Clear();
            ContentModelVisual3D.Content = null;


            var greenMaterial = new DiffuseMaterial(Brushes.Green);
            var redMaterial = new DiffuseMaterial(Brushes.Red);

            _namedObjects = new Dictionary<string, object>();

            // Create 7 x 7 boxes with different height
            for (int y = -3; y <= 3; y++)
            {
                for (int x = -3; x <= 3; x++)
                {
                    // Height is based on the distance from the center
                    double height = (5 - Math.Sqrt(x * x + y * y)) * 60;

                    // Create the 3D Box visual element

                    var boxVisual3D = new Ab3d.Visuals.BoxVisual3D()
                    {
                        CenterPosition = new Point3D(x * 100, height / 2, y * 100),
                        Size = new Size3D(80, height, 80),
                    };

                    if (height > 200)
                        boxVisual3D.Material = redMaterial;
                    else
                        boxVisual3D.Material = greenMaterial;

                    // To preserve the object names we can fill the names into the _namedObjects dictionary
                    _namedObjects.Add(string.Format("Box_{0}_{1}", x + 4, y + 4), boxVisual3D);

                    // We could also use the SetName method (extension added by Ab3d.PowerToys)
                    //boxVisual3D.SetName(string.Format("Box_{0}_{1}", x + 4, y + 4));

                    ContentModelVisual3D.Children.Add(boxVisual3D);
                }
            }
        }

        private void LoadModel(string fileName)
        {
            // Create an instance of AssimpWpfImporter
            var assimpWpfImporter = new AssimpWpfImporter();

            string fileExtension = System.IO.Path.GetExtension(fileName);
            if (!assimpWpfImporter.IsImportFormatSupported(fileExtension))
            {
                MessageBox.Show("Assimp does not support importing files file extension: " + fileExtension);
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Before reading the file we can set the default material (used when no other material is defined - here we set the default value again)
                assimpWpfImporter.DefaultMaterial = new DiffuseMaterial(Brushes.Silver);

                // After assimp importer reads the file, it can execute many post processing steps to transform the geometry.
                // See the possible enum values to see what post processes are available.
                // By default we just execute the Triangulate step to convert all polygons to triangles that are needed for WPF 3D.
                assimpWpfImporter.AssimpPostProcessSteps = PostProcessSteps.Triangulate;

                // Read model from file
                Model3D readModel3D = assimpWpfImporter.ReadModel3D(fileName, texturesPath: null); // we can also define a textures path if the textures are located in some other directory (this is parameter can be skipped, but is defined here so you will know that you can use it)

                // Save the names of the objects - the same dictionary can be used when exporting the objects
                _namedObjects = assimpWpfImporter.NamedObjects;

                // Show the model
                ShowModel(readModel3D);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading file:\r\n" + ex.Message);
            }
            finally
            {
                // Dispose unmanaged resources
                assimpWpfImporter.Dispose();

                Mouse.OverrideCursor = null;
            }
        }

        private void ShowModel(Model3D model3D)
        {
            ContentModelVisual3D.Children.Clear();
            ContentModelVisual3D.Content = model3D;


            // Calculate the center of the model and its size
            // This will be used to position the camera

            var bounds = model3D.Bounds;

            var modelCenter = new Point3D(bounds.X + bounds.SizeX / 2,
                                          bounds.Y + bounds.SizeY / 2,
                                          bounds.Z + bounds.SizeZ / 2);

            var modelSize = Math.Sqrt(bounds.SizeX * bounds.SizeX +
                                      bounds.SizeY * bounds.SizeY +
                                      bounds.SizeZ * bounds.SizeZ);


            Camera1.TargetPosition = modelCenter;
            Camera1.Distance = modelSize * 2;

            // If the read model already define some lights, then do not show the Camera's light
            if (ModelUtils.HasAnyLight(model3D))
                Camera1.ShowCameraLight = ShowCameraLightType.Never;
            else
                Camera1.ShowCameraLight = ShowCameraLightType.Always;


            // Clear exported object preview
            MainViewport2.Children.Clear();
            ExportedSceneTitleTextBlock.Text = "No exported file";
        }



        private void ExportButton_OnClick(object sender, RoutedEventArgs e)
        {
            bool isExported = ExportViewport3D(OutputFileName.Text, _selectedExportFormatId, MainViewport, _namedObjects);

            if (isExported)
            {
                LoadExportedScene(OutputFileName.Text);
                OpenExportedButton.Visibility = Visibility.Visible;
            }
        }

        private void Camera1_OnCameraChanged(object sender, CameraChangedRoutedEventArgs e)
        {
            if (!this.IsLoaded || _isInternalCameraChange)
                return;

            //Synchronize Camera1 and Camera2
            _isInternalCameraChange = true;

            Camera2.Heading  = Camera1.Heading;
            Camera2.Attitude = Camera1.Attitude;
            Camera2.Distance = Camera1.Distance;

            _isInternalCameraChange = false;
        }

        private void Camera2_OnCameraChanged(object sender, CameraChangedRoutedEventArgs e)
        {
            if (!this.IsLoaded || _isInternalCameraChange)
                return;

            //Synchronize Camera1 and Camera2
            _isInternalCameraChange = true;

            Camera1.Heading  = Camera2.Heading;
            Camera1.Attitude = Camera2.Attitude;
            Camera1.Distance = Camera2.Distance;

            _isInternalCameraChange = false;
        }

        private void OpenExportedButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Open exported file in notepad
            if (System.IO.File.Exists(OutputFileName.Text))
                System.Diagnostics.Process.Start("notepad.exe", OutputFileName.Text);
        }

        private Dictionary<string, object> ConvertToNamedObjects(Dictionary<object, string> objectNames)
        {
            var namedObjects = new Dictionary<string, object>();

            foreach (KeyValuePair<object, string> keyValuePair in objectNames)
            {
                if (keyValuePair.Value == null)
                    continue;

                namedObjects[keyValuePair.Value] = keyValuePair.Key;
            }

            return namedObjects;
        }

        private void ExportTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int exportTypeIndex = ExportTypeComboBox.SelectedIndex;

            if (exportTypeIndex == -1)
                return;

            var selectedFileExtension = _exportFormatDescriptions[exportTypeIndex].FileExtension;

            _selectedExportFormatId = _exportFormatDescriptions[exportTypeIndex].FormatId;

            OutputFileName.Text = System.IO.Path.ChangeExtension(OutputFileName.Text, selectedFileExtension);
        }
    }
}
