using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Ab.Common;
using Ab3d.Assimp;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    // This sample shows how to create static edge lines for 3D models.

    // NOTE:
    // Rendering many 3D lines with Ab3d.PowerToys and WPF 3D can be very slow because the lines need to be regenerated on the CPU on each camera or line change.
    // To efficiently render many 3D lines use Ab3d.DXEngine.
    //
    // With Ab3d.DXEngine it is also possible to render object silhouette with using edge detection post-process -
    // this renders thick edges around each object and separates the objects also on areas where there are no edges because the object is curved.
    //
    // What is more, with Ab3d.DXEngine it is possible to use depth bias that offset the 3D lines from the solid 3D models and prevents that 3D lines
    // are partially hidden by the solid model - see the "DXEngineVisuals/LineDepthBiasSample.xaml" in the Ab3d.DXEngine samples project.
    //
    //
    // ADDITIONAL NOTE: 
    // This sample also shows how to store the generated edges into binary file, so it is not needed to generate the edge lines each time the file is read.
    // The code to do that is commented in the LoadModelWithEdgeLines method below.
    //
    // It is possible to store edge lines into wpf3d file format.
    // The source code to read and write wpf3d file is available in this samples project in the Wpf3DFile folder.


    /// <summary>
    /// Interaction logic for StaticEdgeLinesCreationSample.xaml
    /// </summary>
    public partial class StaticEdgeLinesCreationSample : Page
    {
        private string _fileName;

        private Model3D _readModel3D;
        private MultiLineVisual3D _multiLineVisual3D;

        private ContentVisual3D _readModelVisual3D;
        private EdgeLinesFactory _edgeLinesFactory;


        public StaticEdgeLinesCreationSample()
        {
            InitializeComponent();


            CacheIntermediateObjectsInfoControl.InfoText = 
@"When checked then intermediate objects that are required for edge lines generation are preserved from processing of the previous meshes.
This reduces the memory requirements and number of garbage collections.";

            ProcessDuplicatePositionsInfoControl.InfoText = 
@"When checked then the mesh is first processed so that all duplicate positions (positions that have same x, y and z coordinates) are combined.
Duplicate positions are very common because they are required to create sharp edges by defining different normals for each position.
If you are sure that your mesh does not have duplicate positions, then you can set ProcessDuplicatePositions to false to significantly improve the performance of edge generation.";
            
            ProcessPartiallyCoveredEdgesInfoControl.InfoText = 
@"When checked then the EdgeLinesFactory executes an algorithm to processes meshes that have triangle with edges 
that may have multiple connected triangles that fully or only partially covert the triangle edges.
If you are sure that the mesh have nicely defined triangles where each triangle edge is fully covered by any adjacent triangle edge (except on the outer mesh borders),
then you can set ProcessPartiallyCoveredEdges to false to significantly improve the performance of edge generation.

This CheckBox can be unchecked for the initially loaded sample model because it has only nicely generated meshes.";

            AddMeshOuterEdgesInfoControl.InfoText = @"When checked then edge lines are generated for edges that do not have any adjacent triangle and define the mesh outer edge.";


            AssimpLoader.LoadAssimpNativeLibrary();

            var dragAndDropHelper = new DragAndDropHelper(this, ".*");
            dragAndDropHelper.FileDropped += (sender, args) => LoadModelWithEdgeLines(args.FileName);

            string startupFileName = AppDomain.CurrentDomain.BaseDirectory + @"Resources\ObjFiles\house with trees.obj";
            LoadModelWithEdgeLines(startupFileName);
        }

        private void LoadModelWithEdgeLines(string fileName)
        {
            // Create an instance of AssimpWpfImporter
            var assimpWpfImporter = new AssimpWpfImporter();
            var readModel3D = assimpWpfImporter.ReadModel3D(fileName, texturesPath: null); // we can also define a textures path if the textures are located in some other directory (this is parameter can be skipped, but is defined here so you will know that you can use it)

            bool resetCamera = (_fileName != fileName); // Reset camera only when the file is loaded for the first time
            _fileName = fileName;

            LoadModelWithEdgeLines(readModel3D, resetCamera);
        }

        private void LoadModelWithEdgeLines(Model3D readModel3D, bool resetCamera)
        {
            MainViewport.Children.Clear();

            _readModelVisual3D = readModel3D.CreateContentVisual3D();
            _readModelVisual3D.IsVisible = ShowModelCheckBox.IsChecked ?? false;

            MainViewport.Children.Add(_readModelVisual3D);


            // NOTES:
            // 1)
            // By using EdgeLinesFactory.GetEdgeLines we will create STATIC lines from the current readModel3D.
            // If the readModel3D is changed (any child transformation would be changed),
            // then the lines would not be correct anymore.
            // See the DynamicEdgeLinesSample to see how to create dynamic edge lines.
            // If your object will not change, then it is better to create static edge lines for performance reasons
            // (having single MultiLineVisual3D for the all the models instead of one MultiLineVisual3D for each GeometryModel3D).
            //
            // 2)
            // Multi-threading:
            // If you want to call GetEdgeLines method on background threads, you will need to call Freeze on the readModel3D 
            // (this will make the model and meshes immutable and this way they can be read on other threads).
            // You will also need to create a new EdgeLinesFactory for each thread.
            //
            // 3)
            // Below is a code that you can use to save and load the generated edge lines into a file.
            // 
            // You can use wpf3d file format to save 3D models with embedded EdgeLineIndices data (and also with PolygonIndices data).
            // The source code for wpf3d file format is available in this project under Wpf3DFile folder (see sample in this folder for more info).
            // This way you can calculate model edges in a separate application and then save that into the wpf3d file
            // so that the application that requires models with EdgeLineIndices data will not need to calculate that.
            // 
            // 4)
            // If using EdgeLinesFactory does not produce the correct edge lines, then you can try to use the old method
            // of calculating the edge lines (this may be more than 100x slower).
            // To do this uncomment the EdgeLinesFactory_old that is provided at the end of this file and use that class.


            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Create EdgeLinesFactory if it was not yet created
            // Preserving EdgeLinesFactory objects will also preserve intermediate objects and lists that are used when generating the edge lines (when CacheIntermediateObjects is true).
            if (_edgeLinesFactory == null)
                _edgeLinesFactory = new EdgeLinesFactory();
            
            // See tooltips at the beginning of this file for more information about the following properties.
            _edgeLinesFactory.CacheIntermediateObjects     = CacheIntermediateObjectsCheckBox.IsChecked ?? false;
            _edgeLinesFactory.ProcessDuplicatePositions    = ProcessDuplicatePositionsCheckBox.IsChecked ?? false;
            _edgeLinesFactory.ProcessPartiallyCoveredEdges = ProcessPartiallyCoveredEdgesCheckBox.IsChecked ?? false;
            _edgeLinesFactory.AddMeshOuterEdges            = AddMeshOuterEdgesCheckBox.IsChecked ?? false;

            var edgeLinePositions = _edgeLinesFactory.GetEdgeLines(readModel3D, EdgeStartAngleSlider.Value);

            stopwatch.Stop();
            TimeTextBlock.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Edges generation time: {0:0.00} ms", stopwatch.Elapsed.TotalMilliseconds);


            // Because for complex files it may take a long time to calculate edge lines, 
            // we can use the following code to save the edge lines so next time we can load the data:
            //string edgeLinesFileName = System.IO.Path.ChangeExtension(fileName, ".edgelines");

            //// Save edge lines:
            //using (var stream = File.Open(edgeLinesFileName, FileMode.Create))
            //{
            //    using (var writer = new BinaryWriter(stream))
            //    {
            //        writer.Write(edgeLinePositions.Count);

            //        foreach (var edgeLinePosition in edgeLinePositions)
            //        {
            //            writer.Write(edgeLinePosition.X);
            //            writer.Write(edgeLinePosition.Y);
            //            writer.Write(edgeLinePosition.Z);
            //        }
            //    }
            //}

            //// Read edge lines:
            //using (var stream = File.Open(edgeLinesFileName, FileMode.Open))
            //{
            //    using (var reader = new BinaryReader(stream))
            //    {
            //        int count = reader.ReadInt32();

            //        var edgeLines = new Point3D[count];

            //        for (int i = 0; i < count; i++)
            //            edgeLines[i] = new Point3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
            //    }
            //}


            _multiLineVisual3D = new MultiLineVisual3D()
            {
                Positions     = edgeLinePositions,
                LineColor     = Colors.Black,
                LineThickness = LineThicknessSlider.Value,
            };

            _multiLineVisual3D.IsVisible = ShowEdgeLinesCheckBox.IsChecked ?? false;

            MainViewport.Children.Add(_multiLineVisual3D);


            if (resetCamera)
            {
                Camera1.TargetPosition = readModel3D.Bounds.GetCenterPosition();
                Camera1.Distance       = readModel3D.Bounds.GetDiagonalLength() * 1.2;
            }

            // Add ambient light
            var ambientLight = new AmbientLight(Color.FromRgb(100, 100, 100));
            MainViewport.Children.Add(ambientLight.CreateModelVisual3D());


            _readModel3D = readModel3D;
        }
        
        private void ShowModelCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_readModelVisual3D == null)
                return;

            _readModelVisual3D.IsVisible = ShowModelCheckBox.IsChecked ?? false;
        }

        private void ShowEdgeLinesCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_multiLineVisual3D == null)
                return;

            _multiLineVisual3D.IsVisible = ShowEdgeLinesCheckBox.IsChecked ?? false;
        }

        private void EdgeStartAngleSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            // Clear the generated edge lines that are saved to MeshUtils.EdgeLineIndicesProperty property of each MeshGeometry3D
            EdgeLinesFactory.ClearEdgeLineIndices(_readModel3D);

            // The following code would do that manually instead of ClearEdgeLineIndices
            //Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(_readModel3D, null, (geometryModel3D, transform3D) =>
            //{
            //    var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;
            //    if (meshGeometry3D != null && !meshGeometry3D.IsFrozen)
            //        meshGeometry3D.ClearValue(MeshUtils.EdgeLineIndicesProperty);

            //    if (!geometryModel3D.IsFrozen)
            //        geometryModel3D.ClearValue(EdgeLinesFactory.EdgeMultiLineVisual3DProperty);
            //});

            LoadModelWithEdgeLines(_readModel3D, resetCamera: false);
        }

        private void LineThicknessSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            _multiLineVisual3D.LineThickness = LineThicknessSlider.Value;
        }

        private void OnReloadModelChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            // Clear the generated edge lines that are saved to MeshUtils.EdgeLineIndicesProperty property of each MeshGeometry3D
            EdgeLinesFactory.ClearEdgeLineIndices(_readModel3D);

            LoadModelWithEdgeLines(_readModel3D, resetCamera: false);
        }
    }


    // Old EdgeLinesFactory that uses an obsolete MeshAnalyzer.CreateEdgeLines to calculating the edge lines (this may be more than 100x slower).
    #region old EdgeLinesFactory

    ///// <summary>
    ///// EdgeLinesFactory class simplifies creation of edge lines for Model3D and Visual3D objects.
    ///// </summary>
    //public static class EdgeLinesFactory_old
    //{
    //    /// <summary>
    //    /// CreateEdgeLinesForEachGeometryModel3D method goes through all GeometryModel3D objects in the specified model3D hierarchy and
    //    /// creates one MultiLineVisual3D for each GeometryModel3D.
    //    /// The edge lines are created if angle in degrees between two adjacent triangles is bigger then the specified edgeStartAngleInDegrees, then an edge line is created between triangles.
    //    /// </summary>
    //    /// <param name="model3D">Model3D</param>
    //    /// <param name="edgeStartAngleInDegrees">if angle in degrees between two adjacent triangles is bigger then the specified edgeStartAngleInDegrees, then an edge line is created between triangles.</param>
    //    /// <param name="lineThickness">line thickness</param>
    //    /// <param name="lineColor">line color</param>
    //    /// <param name="parentModelVisual3D">ModelVisual3D where the created MultiLineVisual3D objects will be added to</param>
    //    /// <param name="parentTransform3D">Transform3D that is added to the model3D (null by default)</param>
    //    /// <param name="setEdgeMultiLineVisual3DProperty">when true (by default) the <see cref="EdgeMultiLineVisual3DProperty"/> is set to each GeometryModel3D (if not frozen) and it is set to the created MultiLineVisual3D</param>
    //    public static void CreateEdgeLinesForEachGeometryModel3D(Model3D model3D,
    //                                                             double edgeStartAngleInDegrees,
    //                                                             double lineThickness,
    //                                                             Color lineColor,
    //                                                             ModelVisual3D parentModelVisual3D,
    //                                                             Transform3D parentTransform3D = null,
    //                                                             bool setEdgeMultiLineVisual3DProperty = true)
    //    {
    //        Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
    //            model3D,
    //            parentTransform3D,
    //            delegate (GeometryModel3D geometryModel3D, Transform3D transform3D)
    //            {
    //                var linePositions = new Point3DCollection();
    //                AddEdgeLinePositions(geometryModel3D, null, edgeStartAngleInDegrees, linePositions);

    //                if (linePositions.Count > 0)
    //                {
    //                    var multiLineVisual3D = new MultiLineVisual3D()
    //                    {
    //                        Positions = linePositions,
    //                        LineThickness = lineThickness,
    //                        LineColor = lineColor,
    //                        Transform = transform3D
    //                    };

    //                    if (parentModelVisual3D != null)
    //                        parentModelVisual3D.Children.Add(multiLineVisual3D);

    //                    if (setEdgeMultiLineVisual3DProperty && !geometryModel3D.IsFrozen)
    //                        geometryModel3D.SetValue(EdgeLinesFactory.EdgeMultiLineVisual3DProperty, multiLineVisual3D);
    //                }
    //            });
    //    }

    //    /// <summary>
    //    /// AddEdgeLinePositions method goes through all GeometryModel3D objects in the specified model3D hierarchy and
    //    /// collects the edge lines and adds their start and end positions to the linePositions collection.
    //    /// The edge lines are created if angle in degrees between two adjacent triangles is bigger then the specified edgeStartAngleInDegrees, then an edge line is created between triangles.
    //    /// </summary>
    //    /// <param name="model3D">Model3D</param>
    //    /// <param name="edgeStartAngleInDegrees">if angle in degrees between two adjacent triangles is bigger then the specified edgeStartAngleInDegrees, then an edge line is created between triangles.</param>
    //    /// <param name="linePositions">Point3DCollection collection where edge line positions will be added to</param>
    //    /// <param name="parentTransform3D">Transform3D that is added to the model3D (null by default)</param>
    //    public static void AddEdgeLinePositions(Model3D model3D,
    //                                            double edgeStartAngleInDegrees,
    //                                            Point3DCollection linePositions,
    //                                            Transform3D parentTransform3D = null)
    //    {
    //        Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
    //            model3D,
    //            parentTransform3D,
    //            delegate (GeometryModel3D geometryModel3D, Transform3D transform3D)
    //            {
    //                AddEdgeLinePositions(geometryModel3D, transform3D, edgeStartAngleInDegrees, linePositions);
    //            });
    //    }

    //    /// <summary>
    //    /// GenerateEdgeLineIndices method goes through all GeometryModel3D objects in the specified model3D hierarchy and for each MeshGeometry3D
    //    /// sets the <see cref="MeshUtils.EdgeLineIndicesProperty"/> DependencyProperty to the list of edge lines that is created by the <see cref="MeshAnalyzer.CreateEdgeLines(double, bool)"/> method.
    //    /// </summary>
    //    /// <param name="model3D">Model3D</param>
    //    /// <param name="edgeStartAngleInDegrees">if angle in degrees between two adjacent triangles is bigger then the specified edgeStartAngleInDegrees, then an edge line is created between triangles</param>
    //    public static void GenerateEdgeLineIndices(Model3D model3D, double edgeStartAngleInDegrees)
    //    {
    //        Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
    //            model3D,
    //            null,
    //            delegate (GeometryModel3D geometryModel3D, Transform3D transform3D)
    //            {
    //                var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;
    //                if (meshGeometry3D != null)
    //                {
    //                    GenerateEdgeLineIndices(meshGeometry3D, edgeStartAngleInDegrees);
    //                }
    //            });
    //    }

    //    /// <summary>
    //    /// ClearEdgeLineIndices method clears the <see cref="MeshUtils.EdgeLineIndicesProperty"/> DependencyProperty values in the specified model3D and all its children.
    //    /// </summary>
    //    /// <param name="model3D">Model3D</param>
    //    public static void ClearEdgeLineIndices(Model3D model3D)
    //    {
    //        Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
    //            model3D,
    //            null,
    //            delegate (GeometryModel3D geometryModel3D, Transform3D transform3D)
    //            {
    //                var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;
    //                if (meshGeometry3D != null && !meshGeometry3D.IsFrozen)
    //                {
    //                    meshGeometry3D.ClearValue(MeshUtils.EdgeLineIndicesProperty);
    //                }
    //            });
    //    }

    //    private static void AddEdgeLinePositions(GeometryModel3D geometryModel3D, Transform3D parentTransform3D, double edgeStartAngleInDegrees, Point3DCollection linePositions)
    //    {
    //        var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;
    //        if (meshGeometry3D != null)
    //        {
    //            var edgeLines = GenerateEdgeLineIndices(meshGeometry3D, edgeStartAngleInDegrees);

    //            if (edgeLines != null)
    //            {
    //                var thisTransform3D = geometryModel3D.Transform;

    //                Transform3D finalTransform3D;

    //                if (parentTransform3D == null || parentTransform3D.Value.IsIdentity)
    //                {
    //                    if (thisTransform3D == null || thisTransform3D.Value.IsIdentity)
    //                        finalTransform3D = null;
    //                    else
    //                        finalTransform3D = thisTransform3D;
    //                }
    //                else
    //                {
    //                    if (thisTransform3D == null || thisTransform3D.Value.IsIdentity)
    //                        finalTransform3D = parentTransform3D;
    //                    else
    //                        finalTransform3D = new MatrixTransform3D(thisTransform3D.Value * parentTransform3D.Value); // First apply child and than parent transform
    //                }

    //                if (finalTransform3D != null) // Moved "if" out of performance critical loop
    //                {
    //                    for (var i = 0; i < edgeLines.Count; i++)
    //                    {
    //                        var position = meshGeometry3D.Positions[edgeLines[i]];
    //                        position = finalTransform3D.Transform(position);

    //                        linePositions.Add(position);
    //                    }
    //                }
    //                else
    //                {
    //                    for (var i = 0; i < edgeLines.Count; i++)
    //                    {
    //                        var position = meshGeometry3D.Positions[edgeLines[i]];
    //                        linePositions.Add(position);
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    private static List<int> GenerateEdgeLineIndices(MeshGeometry3D meshGeometry3D, double edgeStartAngleInDegrees)
    //    {
    //        List<int> edgeLines;

    //        if (meshGeometry3D != null)
    //        {
    //            // Check if edge lines are already created
    //            edgeLines = (List<int>)meshGeometry3D.GetValue(MeshUtils.EdgeLineIndicesProperty);

    //            if (edgeLines == null)
    //            {
    //                var meshAnalyzer = new MeshAnalyzer(meshGeometry3D);
    //                edgeLines = meshAnalyzer.CreateEdgeLines(edgeStartAngleInDegrees, useNewPositions: false);

    //                if (!meshGeometry3D.IsFrozen)
    //                    meshGeometry3D.SetValue(MeshUtils.EdgeLineIndicesProperty, edgeLines);
    //            }
    //        }
    //        else
    //        {
    //            edgeLines = null;
    //        }

    //        return edgeLines;
    //    }
    //}
    #endregion
}
