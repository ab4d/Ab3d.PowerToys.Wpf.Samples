//#define TEST

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    // It is possible to store edge lines into wpf3d file format (this way it is not needed to generate the edge lines after the file is read).
    // The source code to read and write wpf3d file is available with Ab3d.PowerToys library.


    /// <summary>
    /// Interaction logic for StaticEdgeLinesCreationSample.xaml
    /// </summary>
    public partial class StaticEdgeLinesCreationSample : Page
    {
        private string _fileName;
        private MultiLineVisual3D _multiLineVisual3D;

        public StaticEdgeLinesCreationSample()
        {
            InitializeComponent();

            AssimpLoader.LoadAssimpNativeLibrary();

            var dragAndDropHelper = new DragAndDropHelper(this, ".*");
            dragAndDropHelper.FileDropped += (sender, args) => LoadModelWithEdgeLines(args.FileName);

            string startupFileName = AppDomain.CurrentDomain.BaseDirectory + @"Resources\ObjFiles\house with trees.obj";
            LoadModelWithEdgeLines(startupFileName);
        }

        private void LoadModelWithEdgeLines(string fileName)
        {
            MainViewport.Children.Clear();

            // Create an instance of AssimpWpfImporter
            var assimpWpfImporter = new AssimpWpfImporter();
            var readModel3D = assimpWpfImporter.ReadModel3D(fileName, texturesPath: null); // we can also define a textures path if the textures are located in some other directory (this is parameter can be skipped, but is defined here so you will know that you can use it)

            MainViewport.Children.Add(readModel3D.CreateModelVisual3D());


            // NOTES:
            // 1)
            // The source code for EdgeLinesFactory class is written below (in a comment at the end of this file).
            // You can change its source and create a new class that would suite your needs.
            //
            // 2)
            // With using AddEdgeLinePositions we will create STATIC lines from the current readModel3D.
            // If the readModel3D would be changed (any child transformation would be changed),
            // then the lines would not be correct any more.
            // See the DynamicEdgeLinesSample to see how to create dynamic edge lines.
            // If your object will not change, then it is better to create static edge lines for performance reasons
            // (having single MultiLineVisual3D for the whole instead of one MultiLineVisual3D for each GeometryModel3D).
            //
            // 3)
            // Multi-threading:
            // If you want to call AddEdgeLinePositions method on background threads, you will need to call Freeze on the readModel3D 
            // (this will make the model and meshes immutable and this way they can be read on other threads).
            // You will also need to create Point3DCollection for each thread and after it is filled in AddEdgeLinePositions
            // call Freeze on it so it can be "send" to the main UI thread.
            // Another option (better) is to create a MeshAnalyzer class in each thread and for each MeshGeometry3D call CreateEdgeLines
            // (see source of the EdgeLinesFactory.GenerateEdgeLineIndices method).
            //
            // 4)
            // You can use wpf3d file format to save 3D models with embedded EdgeLineIndices data (and also with PolygonIndices data).
            // The source code for wpf3d file format is available in this project under Wpf3DFile folder (see sample in this folder for more info).
            // This way you can calculate model edges in a separate application and then save that into the wpf3d file
            // so that the application that requires models with EdgeLineIndices data will not need to calculate that.

            var edgeLinePositions = new Point3DCollection();
            EdgeLinesFactory.AddEdgeLinePositions(readModel3D, EdgeStartAngleSlider.Value, edgeLinePositions);

            _multiLineVisual3D = new MultiLineVisual3D()
            {
                Positions     = edgeLinePositions,
                LineColor     = Colors.Black,
                LineThickness = LineThicknessSlider.Value,
            };

            MainViewport.Children.Add(_multiLineVisual3D);


            if (_fileName != fileName) // Reset camera only when the file is loaded for the first time
            {
                _fileName = fileName;

                Camera1.TargetPosition = readModel3D.Bounds.GetCenterPosition();
                Camera1.Distance       = readModel3D.Bounds.GetDiagonalLength() * 1.2;
            }

            // Add ambient light
            var ambientLight = new AmbientLight(Color.FromRgb(100, 100, 100));
            MainViewport.Children.Add(ambientLight.CreateModelVisual3D());
        }

        private void EdgeStartAngleSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            LoadModelWithEdgeLines(_fileName);
        }

        private void LineThicknessSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            _multiLineVisual3D.LineThickness = LineThicknessSlider.Value;
        }
    }


    /*

    // ----------------------------------------------------------------
    // <copyright file="EdgeLinesFactory.cs" company="AB4D d.o.o.">
    //     Copyright (c) AB4D d.o.o.  All Rights Reserved
    // </copyright>
    // -----------------------------------------------------------------

    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using Ab3d.Visuals;

    namespace Ab3d.Utilities
    {
        /// <summary>
        /// EdgeLinesFactory class simplifies creation of edge lines for Model3D and Visual3D objects.
        /// </summary>
        public static class EdgeLinesFactory
        {
            /// <summary>
            /// EdgeMultiLineVisual3DProperty is a DependencyProperty that can be set to the GeometryModel3D object and specifies a MultiLineVisual3D object that is used to show edge lines.
            /// </summary>
            public static readonly DependencyProperty EdgeMultiLineVisual3DProperty = DependencyProperty.Register("EdgeMultiLineVisual3D", typeof(MultiLineVisual3D), typeof(EdgeLinesFactory));

            /// <summary>
            /// CreateEdgeLinesForEachGeometryModel3D method goes through all GeometryModel3D objects in the specified model3D hierarchy and
            /// creates one MultiLineVisual3D for each GeometryModel3D.
            /// The edge lines are created if angle in degrees between two adjacent triangles is bigger then the specified edgeStartAngleInDegrees, then an edge line is created between triangles.
            /// </summary>
            /// <param name="model3D">Model3D</param>
            /// <param name="edgeStartAngleInDegrees">if angle in degrees between two adjacent triangles is bigger then the specified edgeStartAngleInDegrees, then an edge line is created between triangles.</param>
            /// <param name="lineThickness">line thickness</param>
            /// <param name="lineColor">line color</param>
            /// <param name="parentModelVisual3D">ModelVisual3D where the created MultiLineVisual3D objects will be added to</param>
            /// <param name="parentTransform3D">Transform3D that is added to the model3D (null by default)</param>
            /// <param name="setEdgeMultiLineVisual3DProperty">when true (by default) the <see cref="EdgeMultiLineVisual3DProperty"/> is set to each GeometryModel3D (if not frozen) and it is set to the created MultiLineVisual3D</param>
            public static void CreateEdgeLinesForEachGeometryModel3D(Model3D model3D, 
                                                                     double edgeStartAngleInDegrees, 
                                                                     double lineThickness, 
                                                                     Color lineColor, 
                                                                     ModelVisual3D parentModelVisual3D, 
                                                                     Transform3D parentTransform3D = null,
                                                                     bool setEdgeMultiLineVisual3DProperty = true)
            {
                Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
                    model3D,
                    parentTransform3D,
                    delegate (GeometryModel3D geometryModel3D, Transform3D transform3D)
                    {
                        var linePositions = new Point3DCollection();
                        AddEdgeLinePositions(geometryModel3D, null, edgeStartAngleInDegrees, linePositions);

                        if (linePositions.Count > 0)
                        {
                            var multiLineVisual3D = new MultiLineVisual3D()
                            {
                                Positions = linePositions,
                                LineThickness = lineThickness,
                                LineColor = lineColor,
                                Transform = transform3D
                            };

                            if (parentModelVisual3D != null)
                                parentModelVisual3D.Children.Add(multiLineVisual3D);

                            if (setEdgeMultiLineVisual3DProperty && !geometryModel3D.IsFrozen)
                                geometryModel3D.SetValue(EdgeMultiLineVisual3DProperty, multiLineVisual3D);
                        }
                    });
            }

            /// <summary>
            /// AddEdgeLinePositions method goes through all GeometryModel3D objects in the specified model3D hierarchy and
            /// collects the edge lines and adds their start and end positions to the linePositions collection.
            /// The edge lines are created if angle in degrees between two adjacent triangles is bigger then the specified edgeStartAngleInDegrees, then an edge line is created between triangles.
            /// </summary>
            /// <param name="model3D">Model3D</param>
            /// <param name="edgeStartAngleInDegrees">if angle in degrees between two adjacent triangles is bigger then the specified edgeStartAngleInDegrees, then an edge line is created between triangles.</param>
            /// <param name="linePositions">Point3DCollection collection where edge line positions will be added to</param>
            /// <param name="parentTransform3D">Transform3D that is added to the model3D (null by default)</param>
            public static void AddEdgeLinePositions(Model3D model3D, 
                                                    double edgeStartAngleInDegrees, 
                                                    Point3DCollection linePositions, 
                                                    Transform3D parentTransform3D = null)
            {
                Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
                    model3D,
                    parentTransform3D,
                    delegate (GeometryModel3D geometryModel3D, Transform3D transform3D)
                    {
                        AddEdgeLinePositions(geometryModel3D, transform3D, edgeStartAngleInDegrees, linePositions);
                    });
            }

            /// <summary>
            /// GenerateEdgeLineIndices method goes through all GeometryModel3D objects in the specified model3D hierarchy and for each MeshGeometry3D
            /// sets the <see cref="MeshUtils.EdgeLineIndicesProperty"/> DependencyProperty to the list of edge lines that is created by the <see cref="MeshAnalyzer.CreateEdgeLines"/> method.
            /// </summary>
            /// <param name="model3D"></param>
            /// <param name="edgeStartAngleInDegrees"></param>
            public static void GenerateEdgeLineIndices(Model3D model3D, double edgeStartAngleInDegrees)
            {
                Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
                    model3D,
                    null,
                    delegate (GeometryModel3D geometryModel3D, Transform3D transform3D)
                    {
                        var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;
                        if (meshGeometry3D != null)
                        {
                            GenerateEdgeLineIndices(meshGeometry3D, edgeStartAngleInDegrees);
                        }
                    });
            }

            public static void ClearEdgeLineIndices(Model3D model3D)
            {
                Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
                    model3D,
                    null,
                    delegate (GeometryModel3D geometryModel3D, Transform3D transform3D)
                    {
                        var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;
                        if (meshGeometry3D != null && !meshGeometry3D.IsFrozen)
                        {
                            meshGeometry3D.ClearValue(MeshUtils.EdgeLineIndicesProperty);
                        }
                    });
            }

            private static void AddEdgeLinePositions(GeometryModel3D geometryModel3D, Transform3D parentTransform3D, double edgeStartAngleInDegrees, Point3DCollection linePositions)
            {
                var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;
                if (meshGeometry3D != null)
                {
                    var edgeLines = GenerateEdgeLineIndices(meshGeometry3D, edgeStartAngleInDegrees);

                    if (edgeLines != null)
                    {
                        var thisTransform3D = geometryModel3D.Transform;

                        Transform3D finalTransform3D;

                        if (parentTransform3D == null || parentTransform3D.Value.IsIdentity)
                        {
                            if (thisTransform3D == null || thisTransform3D.Value.IsIdentity)
                                finalTransform3D = null;
                            else
                                finalTransform3D = thisTransform3D;
                        }
                        else
                        {
                            if (thisTransform3D == null || thisTransform3D.Value.IsIdentity)
                                finalTransform3D = parentTransform3D;
                            else
                                finalTransform3D = new MatrixTransform3D(thisTransform3D.Value * parentTransform3D.Value); // First apply child and than parent transform
                        }

                        if (finalTransform3D != null) // Moved "if" out of performance critical loop
                        {
                            for (var i = 0; i < edgeLines.Count; i++)
                            {
                                var position = meshGeometry3D.Positions[edgeLines[i]];
                                position = finalTransform3D.Transform(position);

                                linePositions.Add(position);
                            }
                        }
                        else
                        {
                            for (var i = 0; i < edgeLines.Count; i++)
                            {
                                var position = meshGeometry3D.Positions[edgeLines[i]];
                                linePositions.Add(position);
                            }
                        }
                    }
                }
            }

            private static List<int> GenerateEdgeLineIndices(MeshGeometry3D meshGeometry3D, double edgeStartAngleInDegrees)
            {
                List<int> edgeLines;

                if (meshGeometry3D != null)
                {
                    // Check if edge lines are already created
                    edgeLines = (List<int>)meshGeometry3D.GetValue(MeshUtils.EdgeLineIndicesProperty);

                    if (edgeLines == null)
                    {
                        var meshAnalyzer = new MeshAnalyzer(meshGeometry3D);
                        edgeLines = meshAnalyzer.CreateEdgeLines(edgeStartAngleInDegrees, useNewPositions: false);

                        if (!meshGeometry3D.IsFrozen)
                            meshGeometry3D.SetValue(MeshUtils.EdgeLineIndicesProperty, edgeLines);
                    }
                }
                else
                {
                    edgeLines = null;
                }

                return edgeLines;
            }
        }
    }

     */
}
