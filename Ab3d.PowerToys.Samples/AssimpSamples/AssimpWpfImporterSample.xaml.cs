﻿using System;
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
using Ab.Common;
using Ab3d.Assimp;
using Ab3d.Common.Cameras;
using Ab3d.Utilities;
using Ab3d.Visuals;
using Assimp;

namespace Ab3d.PowerToys.Samples.AssimpSamples
{
    /// <summary>
    /// Interaction logic for AssimpWpfImporterSample.xaml
    /// </summary>
    public partial class AssimpWpfImporterSample : Page
    {
        private string _fileName;

        private bool _isWireframeWarningShown;

        public AssimpWpfImporterSample()
        {
            InitializeComponent();

            LineThicknessComboBox.ItemsSource = new double[] { 0.1, 0.2, 0.5, 1, 2 };
            LineThicknessComboBox.SelectedIndex = 3;


            // Use helper class (defined in this sample project) to load the native assimp libraries.
            // IMPORTANT: See commend in the AssimpLoader class for details on how to prepare your project to use assimp library.
            AssimpLoader.LoadAssimpNativeLibrary();

            // It is recommended to set the TriangulatorFunc static property to provide direct access to Triangulator from Ab3d.PowerToys.
            // If this is not done, then Reflection is used to get the same Triangulator object.
            AssimpWpfConverter.TriangulatorFunc = positions =>
            {
                var triangulator3D = new Ab3d.Utilities.Triangulator(positions);
                return triangulator3D.CreateTriangleIndices();
            };


            var assimpWpfImporter = new AssimpWpfImporter();
            string[] supportedImportFormats = assimpWpfImporter.SupportedImportFormats;

            var assimpWpfExporter = new AssimpWpfExporter();
            string[] supportedExportFormats = assimpWpfExporter.ExportFormatDescriptions.Select(f => f.FileExtension).ToArray();


            string assimpVersionText = assimpWpfImporter.AssimpVersion.ToString();

            // When Assimp library is complied from a source that if get the GutHub, then GitCommitHash is set to the source's last commit hash.
            // When Assimp library is complied from a zip file in GitHub's releases, then the GitCommitHash is zero. In this case do not show the hash.
            if (assimpWpfImporter.GitCommitHash != 0)
                assimpVersionText += string.Format("; Git commit hash: {0:x7}", assimpWpfImporter.GitCommitHash);

            FileFormatsTextBlock.Text = string.Format("Using native Assimp library version {0}.\r\n\r\nSupported import formats:\r\n{1}\r\n\r\nSupported export formats:\r\n{2}",
                assimpVersionText,
                string.Join(", ", supportedImportFormats),
                string.Join(", ", supportedExportFormats));


            var dragAndDropHelper = new DragAndDropHelper(this, ".*");
            dragAndDropHelper.FileDropped += (sender, args) => LoadModel(args.FileName);


            string startUpFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Collada\duck.dae");
            LoadModel(startUpFileName);
        }

        private void LoadModel(string fileName)
        {
            bool isNewFile = false;

            // Create an instance of AssimpWpfImporter
            var assimpWpfImporter = new AssimpWpfImporter();

            string fileExtension = System.IO.Path.GetExtension(fileName);
            if (!assimpWpfImporter.IsImportFormatSupported(fileExtension))
            {
                MessageBox.Show("Assimp does not support importing files file extension: " + fileExtension);
                return;
            }

            var fileInfo = new System.IO.FileInfo(fileName);

            if (!fileInfo.Exists)
            {
                MessageBox.Show(string.Format("File {0} does not exist", fileName));
                return;
            }

            // When file is bigger then 1MB then show warning about wireframe rendering and uncheck the CheckBox
            if (fileInfo.Length > 1000000 && (ShowWireframeCheckBox.IsChecked ?? false) && !_isWireframeWarningShown)
            {
                MessageBox.Show("Showing a complex 3D file with 'Show wireframe' CheckBox checked can be very slow because rendering many 3D lines is slow when using only WPF 3D and Ab3d.PowerToys (without using Ab3d.DXEngine).\r\nDisabling wireframe rendering to show the models faster. To show the wireframe model, check the 'Show wireframe' CheckBox.", "Wireframe rendering", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                ShowWireframeCheckBox.IsChecked = false;
                _isWireframeWarningShown = true;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Before reading the file we can set the default material (used when no other material is defined - here we set the default value again)
                assimpWpfImporter.DefaultMaterial = new DiffuseMaterial(Brushes.Silver);

                // After assimp importer reads the file, it can execute many post processing steps to transform the geometry.
                // See the possible enum values to see what post processes are available.
                // Here we just set the AssimpPostProcessSteps to its default value - execute the Triangulate step to convert all polygons to triangles that are needed for WPF 3D.
                // Note that if ReadPolygonIndices is set to true in the next line, then the assimpWpfImporter will not use assimp's triangulation because it needs original polygon data.
                assimpWpfImporter.AssimpPostProcessSteps = PostProcessSteps.Triangulate;

                // When ReadPolygonIndices is true, assimpWpfImporter will read PolygonIndices collection that can be used to show polygons instead of triangles.
                assimpWpfImporter.ReadPolygonIndices = ReadPolygonIndicesCheckBox.IsChecked ?? false;


                // Set importer configuration properties:
                // You can get the possible setting class by using intelli sense and checking the classes available in the global::Assimp.Configs namespace
                // or get property names from config.h.in file in Assimp source (https://github.com/assimp/assimp/blob/master/include/assimp/config.h.in)
                //assimpWpfImporter.SetConfig(new global::Assimp.Configs.FBXPreservePivotsConfig(preservePivots: false));
                //// this is the same as: 
                //assimpWpfImporter.SetConfig(new global::Assimp.Configs.BooleanPropertyConfig("IMPORT_FBX_PRESERVE_PIVOTS", false));


                // Read model from file
                Model3D readModel3D;

                try
                {
                    readModel3D = assimpWpfImporter.ReadModel3D(fileName, texturesPath: null); // we can also define a textures path if the textures are located in some other directory (this is parameter can be skipped, but is defined here so you will know that you can use it)

                    // To read 3D model from stream, use the following code:
                    //var extension = System.IO.Path.GetExtension(fileName); // extension is needed as a format hint so assimp will know which importer to use
                    //var directoryName = System.IO.Path.GetDirectoryName(fileName);

                    //using (var fileStream = System.IO.File.OpenRead(fileName))
                    //{
                    //    readModel3D = assimpWpfImporter.ReadModel3D(fileStream, extension, resolveResourceFunc: resourceName =>
                    //    {
                    //        // when reading models with texture, we will need to define the resolveResourceFunc to read additional resources
                    //        // (textures or additional files, like material .mtl that is associated with .obj file)
                    //        if (string.IsNullOrEmpty(directoryName))
                    //            return null;
                                
                    //        string resourceFileName = System.IO.Path.Combine(directoryName, resourceName);
                    //        if (System.IO.File.Exists(resourceFileName))
                    //            return System.IO.File.OpenRead(resourceFileName);

                    //        return null;
                    //    }); 
                    //}

                    isNewFile = (_fileName != fileName);
                    _fileName = fileName;
                }
                catch (Exception ex)
                {
                    readModel3D = null;
                    MessageBox.Show("Error importing file:\r\n" + ex.Message);
                }

                // After the model is read and if the object names are defined in the file,
                // you can get the model names by assimpWpfImporter.ObjectNames
                // or get object by name with assimpWpfImporter.NamedObjects

                // Note: to get the original Assimp's Scene object, check the assimpWpfImporter.ImportedAssimpScene

                // Show the model
                ShowModel(readModel3D, updateCamera: isNewFile); // If we just reloaded the previous file, we preserve the current camera TargetPosition and Distance
            }
            finally
            {
                // Dispose unmanaged resources
                assimpWpfImporter.Dispose();

                Mouse.OverrideCursor = null;
            }
        }

        private void ShowModel(Model3D model3D, bool updateCamera)
        {
            ContentVisual.Content = model3D;

            if (model3D == null)
            {
                ContentWireframeVisual.OriginalModel = null;
                return;
            }

            
            // IMPORTANT:
            // Some imported files may define the models in actual units (meters or millimeters) and
            // this may make the objects very big (for example, objects bounds are bigger than 100000).
            // For such big models the camera rotation may become irregular (not smooth) because
            // of floating point precision errors on the graphics card.
            //
            // Therefore it is recommended to prevent such big models by scaling them to a more common size.
            // This can be done by the ModelUtils.CenterAndScaleModel3D method:
            // Put the model to the center of coordinate axis and scale it to 100 x 100 x 100.
            Ab3d.Utilities.ModelUtils.CenterAndScaleModel3D(model3D, new Point3D(0, 0, 0), new Size3D(100, 100, 100));


            // NOTE:
            // We could show both solid model and wireframe in WireframeVisual3D (ContentWireframeVisual) with using WireframeWithOriginalSolidModel for WireframeType.
            // But in this sample we show solid frame is separate ModelVisual3D and therefore we show only wireframe in WireframeVisual3D.
            ContentWireframeVisual.BeginInit();
            ContentWireframeVisual.ShowPolygonLines = ReadPolygonIndicesCheckBox.IsChecked ?? false;
            ContentWireframeVisual.OriginalModel = model3D;
            ContentWireframeVisual.EndInit();


            // Calculate the center of the model and its size
            // This will be used to position the camera

            if (updateCamera)
            {
                var bounds = model3D.Bounds;

                var modelCenter = new Point3D(bounds.X + bounds.SizeX / 2,
                    bounds.Y + bounds.SizeY / 2,
                    bounds.Z + bounds.SizeZ / 2);

                var modelSize = Math.Sqrt(bounds.SizeX * bounds.SizeX +
                                          bounds.SizeY * bounds.SizeY +
                                          bounds.SizeZ * bounds.SizeZ);


                Camera1.TargetPosition = modelCenter;
                Camera1.Distance = modelSize * 2;
            }

            // If the read model already define some lights, then do not show the Camera's light
            if (ModelUtils.HasAnyLight(model3D))
                Camera1.ShowCameraLight = ShowCameraLightType.Never;
            else
                Camera1.ShowCameraLight = ShowCameraLightType.Always;

            ShowInfoButton.IsEnabled = true;
        }


        private void LoadButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

            openFileDialog.Filter = "3D model file (*.*)|*.*";
            openFileDialog.Title = "Open 3D model file";

            if ((openFileDialog.ShowDialog() ?? false) && !string.IsNullOrEmpty(openFileDialog.FileName))
                LoadModel(openFileDialog.FileName);
        }

        private void OnShowWireframeCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (ShowWireframeCheckBox.IsChecked ?? false)
                ContentWireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.Wireframe;
            else
                ContentWireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.None;
        }

        private void OnReadPolygonIndicesCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_fileName == null)
                return;

            if ((ReadPolygonIndicesCheckBox.IsChecked ?? false) && !(ShowWireframeCheckBox.IsChecked ?? false))
                ShowWireframeCheckBox.IsChecked = true; // Turn on showing wireframe if it was off and if ReadPolygonIndicesCheckBox is checked

            // Read file again
            LoadModel(_fileName);
        }

        private void ShowInfoButton_OnClick(object sender, RoutedEventArgs e)
        {
            var shownModel = ContentVisual.Content;

            if (shownModel == null)
                return;

            string objectInfo = Ab3d.Utilities.Dumper.GetObjectHierarchyString(shownModel);


            var textBox = new TextBox()
            {
                Margin = new Thickness(10, 10, 10, 10),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                Text = objectInfo
            };

            var window = new Window()
            {
                Title = "3D Object info"
            };

            window.Content = textBox;
            window.Show();
        }
    }
}
