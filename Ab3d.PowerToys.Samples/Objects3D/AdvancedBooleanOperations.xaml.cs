using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Threading;
using Ab3d.Common.Models;
using Ab3d.Meshes;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for AdvancedBooleanOperations.xaml
    /// </summary>
    public partial class AdvancedBooleanOperations : Page
    {
        public AdvancedBooleanOperations()
        {
            InitializeComponent();

            var boxMesh = new Ab3d.Meshes.BoxMesh3D(new Point3D(0, 0, 0), new Size3D(200, 10, 200), 10, 1, 10).Geometry;

            // NOTE:
            // When doing the boolean operations it is very good to keep the meshes as simple as possible.
            // Here we use quite complex meshes because segments parameter is set to 30. This better demonstrates the use of processOnlyIntersectingTriangles parameter.
            // For a real world scenarios I would recommend to use lower segment values.
            var cylinder1Mesh = new Ab3d.Meshes.CylinderMesh3D(new Point3D(-50, -50, 90), radius: 8, height: 100, segments: 30, isSmooth: true).Geometry;
            var cylinder2Mesh = new Ab3d.Meshes.CylinderMesh3D(new Point3D(-20, -50, 90), radius: 8, height: 100, segments: 30, isSmooth: true).Geometry;
            
            // It is recommended to combine the cylinder meshes and then do one subtraction.
            var combinedMesh = Ab3d.Utilities.MeshUtils.CombineMeshes(cylinder1Mesh, cylinder2Mesh);


            // Process mesh subtraction only on triangles that intersect the combinedMesh.
            // Because in our case the combinedMesh is much smaller then boxMesh, this produces significantly less triangles.
            //
            // But when you know that most of the triangles in the meshes would intersect, then
            // it is worth setting processOnlyIntersectingTriangles to false to skip getting intersecting triangles.

            var subtractedMesh1 = Ab3d.Utilities.MeshBooleanOperations.Subtract(boxMesh, combinedMesh, processOnlyIntersectingTriangles: true); 
            ShowMesh(subtractedMesh1, -120);

            var subtractedMesh2 = Ab3d.Utilities.MeshBooleanOperations.Subtract(boxMesh, combinedMesh, processOnlyIntersectingTriangles: false);
            ShowMesh(subtractedMesh2, 120);

  
            TextBlockVisual1.Text += string.Format("\r\nFinal triangles count: {0}", subtractedMesh1.TriangleIndices.Count / 3);
            TextBlockVisual2.Text += string.Format("\r\nFinal triangles count: {0}", subtractedMesh2.TriangleIndices.Count / 3);


            // Deep dive:
            // When processOnlyIntersectingTriangles is set to true, then behind the scenes the 
            // MeshUtils.GetIntersectingTriangles, MeshUtils.SplitMeshByIndexesOfTriangles and MeshUtils.CombineMeshes methods
            // are used to split the original mesh into two meshes.
            // 
            // If you want to do that manually, here is the code from Subtract method:
            // (instead of SubtractInt method you can call Subtract method with processOnlyIntersectingTriangles set to false)
            //
            //public static MeshGeometry3D Subtract(MeshGeometry3D mesh1, MeshGeometry3D mesh2, bool processOnlyIntersectingTriangles = true)
            //{
            //    if (mesh1 == null)
            //        return null;

            //    if (mesh2 == null)
            //        return mesh1;


            //    MeshGeometry3D resultMesh;

            //    if (processOnlyIntersectingTriangles)
            //    {
            //        int originalTrianglesCount = mesh1.TriangleIndices.Count / 3;

            //        var intersectingTriangles = MeshUtils.GetIntersectingTriangles(mesh2.Bounds, mesh1, meshTransform: null);

            //        if (intersectingTriangles == null || intersectingTriangles.Count == 0)
            //            return mesh1; // No intersection - preserve the mesh1


            //        if (intersectingTriangles.Count > originalTrianglesCount * 0.8) // When more than 80 percent of triangles is inside the mesh2, then just use the original mesh1
            //        {
            //            resultMesh = SubtractInt(mesh1, mesh2);
            //        }
            //        else
            //        {
            //            MeshGeometry3D splitMesh1, splitMesh2;
            //            MeshUtils.SplitMeshByIndexesOfTriangles(mesh1, intersectingTriangles, false, out splitMesh1, out splitMesh2);

            //            var processedMesh = SubtractInt(splitMesh1, mesh2);

            //            if (processedMesh == null)
            //            {
            //                resultMesh = splitMesh2;
            //            }
            //            else
            //            {
            //                // Combine triangles that were not processed (not intersecting with mesh2) with the result of subtraction
            //                resultMesh = Ab3d.Utilities.MeshUtils.CombineMeshes(splitMesh2, processedMesh);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        if (!mesh2.Bounds.IntersectsWith(mesh1.Bounds))
            //        {
            //            // In case there is no intersection, then just return the original mesh1
            //            resultMesh = mesh1;
            //        }
            //        else
            //        {
            //            // Process whole meshes
            //            resultMesh = SubtractInt(mesh1, mesh2);
            //        }
            //    }

            //    return resultMesh;
            //}
        }

        private void ShowMesh(MeshGeometry3D meshGeometry3D, double xOffset)
        {
            var wireframeVisual3D = new Ab3d.Visuals.WireframeVisual3D()
            {
                WireframeType = Ab3d.Visuals.WireframeVisual3D.WireframeTypes.WireframeWithOriginalSolidModel,
                UseModelColor = false,
                LineColor = Colors.Black,
                LineThickness = 1,
                Transform = new TranslateTransform3D(xOffset, 0, 0)
            };

            var geometryModel = new GeometryModel3D(meshGeometry3D, new DiffuseMaterial(Brushes.Gold));
            geometryModel.BackMaterial = new DiffuseMaterial(Brushes.Red);

            wireframeVisual3D.OriginalModel = geometryModel;

            RootModelVisual.Children.Add(wireframeVisual3D);
        }
    }
}
