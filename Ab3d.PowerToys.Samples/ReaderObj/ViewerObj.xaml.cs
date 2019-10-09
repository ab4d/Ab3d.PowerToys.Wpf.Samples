using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Ab.Common;
using Ab3d.Common.Cameras;
using Ab3d.ObjFile;
using Ab3d.Utilities;
using Ab3d.Visuals;
using Path = System.IO.Path;

namespace Ab3d.PowerToys.Samples.ReaderObj
{
    /// <summary>
    /// Interaction logic for ViewerObj.xaml
    /// </summary>
    public partial class ViewerObj : Page
    {
        private DragAndDropHelper _dragAndDropHelper;
        private string _fileName;
        private volatile bool _isLoading;
        private Model3D _wpf3DModel;
        private Exception _lastException;
        private Ab3d.ReaderObj _readerObj;

        public ViewerObj()
        {
            InitializeComponent();
            
            _dragAndDropHelper = new DragAndDropHelper(this, ".obj");
            _dragAndDropHelper.FileDroped += (sender, args) => LoadObj(args.FileName);

            _readerObj = new Ab3d.ReaderObj();
            _readerObj.IgnoreErrors = true; // If error is found in obj file this will not throw exception but instead continue reading obj file. The error will be written to _readerObj.Errors list.

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            // Read the robotarm.obj from the project's source files (the file is embedded into the project to be also used in the ReadObjFromResources sample)
            string fileName = AppDomain.CurrentDomain.BaseDirectory + @"..\..\Resources\ObjFiles\robotarm.obj";
            LoadObj(fileName);           
        }

        private void LoadButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            openFileDialog.Filter = "Obj file (*.obj)|*.obj";
            openFileDialog.Title = "Open obj file";

            if ((openFileDialog.ShowDialog() ?? false) && !string.IsNullOrEmpty(openFileDialog.FileName))
                LoadObj(openFileDialog.FileName);
        }

        private void LoadObj(string fileName)
        {
            if (_isLoading)
                return;

            bool isNewFile = (_fileName != fileName);

            _fileName = fileName;
            _wpf3DModel = null;


            // Set ReadPolygonIndices in UI thread
            // When true ReaderObj will read PolygonIndices collection that can be used to show polygons instead of triangles
            _readerObj.ReadPolygonIndices = ReadPolygonIndicesCheckBox.IsChecked ?? false;

            // To read polygon indices from the read models, 
            // iterating through GeometryModel3D objects (children of Model3DGroup) and
            // then converting its Geometry object into MeshGeometry3D.
            // The MeshGeometry3D defines the Positions, TriangleIndices, Normals and TextureCoordinates collections. 
            //
            // On top of that the Ab3d.PowerToys and ReaderObj can also add the polygon indices collection.
            // It is set to MeshGeometry3D as a PolygonIndices dependency property (defined in Ab3d.Utilities.MeshUtils).
            // You can read the PolygonIndices directly with the following:
            //
            // var polygonIndices = meshGeometry3D.GetValue(Ab3d.Utilities.MeshUtils.PolygonIndicesProperty) as Int32Collection;
            // 
            // You can also use the GetPolygonIndices, SetPolygonIndices and GetPolygonPositions extension methods on MeshGeometry3D,
            // or use GetPolygonPositions from MeshUtils.
            //
            // See also the following to get know how the polygon positions are organized:
            // https://www.ab4d.com/help/PowerToys/html/F_Ab3d_Utilities_MeshUtils_PolygonIndicesProperty.htm 



            // UH: The following code is much much easier with async and await from .net 4.5
            // But here we want to use only .net 4.0 

            // Read obj file and convert to wpf 3d objects in background thread
            var readObjFileTask = new Task(() =>
            {
                _lastException = null;

                try
                {
                    string texturesPath = Path.GetDirectoryName(_fileName);

                    // Read obj file from _fileName
                    // We read the object names into objectNames dictionary (Dictionary<string, Model3D>)
                    // This allows us to quickly get objects from their name.
                    // ReaderObj also set name to all objects and materials with using SetName extension method
                    // This way you can get object name or material name with GetName method.
                    // For example:
                    // _wpf3DModel.GetName();

                    var defaultMaterial = new DiffuseMaterial(Brushes.Silver);
                    
                    _wpf3DModel = _readerObj.ReadModel3D(_fileName, texturesPath, defaultMaterial);

                    if (_wpf3DModel != null)
                    {
                        // We need to freeze the model because it was created on another thread.
                        // After the model is forzen, it cannot be changed any more.
                        // if you want to change the model, than Convert method must be called on UI thread 
                        // or you must clone the read object in the UI thread
                        _wpf3DModel.Freeze();
                    }

                    // NOTE:
                    // If we wanted more control over reading obj files, we could read them in two steps
                    // 1) Read obj file into ObjFileData object
                    // 2) Convert read ObjFileData object into WPF Model3D
                    // The following code does that (uncomment it to see it in action):

                    //var objFileData = _readerObj.ReadFile(_fileName);
                    ////var objFileData = _readerObj.ReadStream(fileStream);

                    //var objMeshToWpfModel3DConverter = new ObjFileToWpfModel3DConverter();
                    //objMeshToWpfModel3DConverter.InvertYTextureCoordinate = false;
                    //objMeshToWpfModel3DConverter.TexturesDirectory = Path.GetDirectoryName(_fileName);

                    //// DefaultMaterial is a Material that is used when no other material is specified. 
                    //// Actually with leaving DefaultMaterial as null, the same Silver material would be used (setting it here only to show the user what properties exist)
                    //objMeshToWpfModel3DConverter.DefaultMaterial = new DiffuseMaterial(Brushes.Silver);

                    //// ReuseMaterials specified if one material instance defined in obj file can be reused for all Model3D objects that use that material.
                    //// If false than a new material instance will be created for each usage
                    //// This is useful when user wants to change material on individual objects without applying the change to all other objects that use that material.
                    //// Default value is true.
                    //objMeshToWpfModel3DConverter.ReuseMaterials = true;

                    //_wpf3DModel = _objMeshToWpfModel3DConverter.Convert(objFileData);
                }
                catch (Exception ex)
                {
                    _lastException = ex;
                }
            });

            // After reading the obj file and converting it to WPF 3D objects we need to show the objects or errors - this needs to be done on UI thread
            var showObjectTask = readObjFileTask.ContinueWith(_ =>
            {
                try
                {
                    if (_lastException != null)
                    {
                        ResultTextBlock.Text = "ERROR reading obj file:\r\n" + _lastException.Message;
                    }
                    else if (_wpf3DModel != null)
                    {
                        string objectsDescriptions = Ab3d.Utilities.Dumper.GetObjectHierarchyString(_wpf3DModel);

                        if (_readerObj.Errors != null && _readerObj.Errors.Count > 0)
                        {
                            var sb = new StringBuilder();

                            sb.AppendLine("Obj file errors:");
                            foreach (var error in _readerObj.Errors)
                                sb.AppendLine(error);

                            ResultTextBlock.Text = objectsDescriptions + Environment.NewLine + Environment.NewLine + sb.ToString();
                        }
                        else
                        {
                            ResultTextBlock.Text = objectsDescriptions;
                        }

                        //ResultTextBlock.Text = Ab3d.Utilities.Dumper.GetMeshInitializationCode(model);
                        //ResultTextBlock.Text += "\r\n\n" + Ab3d.Utilities.Dumper.Dump(model);

                        ContentVisual.Content = _wpf3DModel;

                        // NOTE:
                        // We could show both solid model and wireframe in WireframeVisual3D (ContentWireframeVisual) with using WireframeWithOriginalSolidModel for WireframeType.
                        // But in this sample we show solid frame is separate ModelVisual3D and therefore we show only wireframe in WireframeVisual3D.
                        ContentWireframeVisual.BeginInit();
                        ContentWireframeVisual.ShowPolygonLines = ReadPolygonIndicesCheckBox.IsChecked ?? false;
                        ContentWireframeVisual.OriginalModel = _wpf3DModel;
                        ContentWireframeVisual.EndInit();



                        // Calculate the center of the model and its size
                        // This will be used to position the camera

                        if (isNewFile) // If we just reloaded the previous file, we preserve the current camera TargetPosition and Distance
                        {
                            var bounds = _wpf3DModel.Bounds;

                            var modelCenter = new Point3D(bounds.X + bounds.SizeX / 2,
                                bounds.Y + bounds.SizeY / 2,
                                bounds.Z + bounds.SizeZ / 2);

                            var modelSize = Math.Sqrt(bounds.SizeX * bounds.SizeX +
                                                      bounds.SizeY * bounds.SizeY +
                                                      bounds.SizeZ * bounds.SizeZ);


                            Camera1.TargetPosition = modelCenter;
                            Camera1.Distance = modelSize * 1.5;
                        }

                        // If the read model already define some lights, then do not show the Camera's light
                        if (ModelUtils.HasAnyLight(_wpf3DModel))
                            Camera1.ShowCameraLight = ShowCameraLightType.Never;
                        else
                            Camera1.ShowCameraLight = ShowCameraLightType.Always;
                    }

                    _isLoading = false;
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            },
            TaskScheduler.FromCurrentSynchronizationContext()); // Run on UI thread


            _isLoading = true;
            Mouse.OverrideCursor = Cursors.Wait;

            // Start tasks
            readObjFileTask.Start();
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
            LoadObj(_fileName);
        }
    }
}

