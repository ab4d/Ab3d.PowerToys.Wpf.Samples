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
using Ab3d.Common.Cameras;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for PerspectiveTransformationSample.xaml
    /// </summary>
    public partial class PerspectiveTransformationSample : Page
    {
        public PerspectiveTransformationSample()
        {
            InitializeComponent();

            // Uncomment that to show "HouseWithTreesModel":
            //// HouseWithTreesModel are defined in App.xaml
            //var houseWithTreesModel = this.FindResource("HouseWithTreesModel") as Model3D;

            //var modelVisual3D = new ModelVisual3D();
            //modelVisual3D.Content = houseWithTreesModel;

            // ObjectsRootVisual3D.Children.Clear();
            //ObjectsRootVisual3D.Children.Add(modelVisual3D);


            Camera1.CameraChanged += delegate(object sender, CameraChangedRoutedEventArgs args)
            {
                UpdateViewportCanvas();
            };

            this.SizeChanged += delegate(object sender, SizeChangedEventArgs args)
            {
                UpdateViewportCanvas();
            };
        }

        #region UpdateViewportCanvas and related methods

        private void UpdateViewportCanvas()
        {
            bool fillPolygon = FillPolygonsCheckBox.IsChecked ?? false;

            // UpdateViewportCanvas will render the 3D scene with 2D polygons with transforming all 3D points to 2D coordinates
            // It will render all children of ObjectsRootVisual3D (to render all children of Viewport3D, use MainViewport.Children)
            UpdateViewportCanvas(ObjectsRootVisual3D.Children, Camera1, ViewportCanvas, fillPolygon);
        }

        // UpdateViewportCanvas will render the 3D scene with 2D polygons with transforming all 3D points to 2D coordinates
        // It will render all children of ObjectsRootVisual3D (to render all children of Viewport3D, use MainViewport.Children)
        // The method is static so it can be easily copied to some other location for some other testing
        private static void UpdateViewportCanvas(Visual3DCollection visuals, BaseCamera camera, Canvas viewportCanvas, bool fillPolygon)
        {
            // Get camera's view and projection matrixes
            Matrix3D viewMatrix, projectionMatrix;
            camera.GetCameraMatrixes(out viewMatrix, out projectionMatrix);

            // Calculate combined viewProjection matrix
            Matrix3D viewProjectionMatrix3D = viewMatrix * projectionMatrix;

            // Read target canvas size and its center point (half the size)
            double viewportCanvasWidth = viewportCanvas.ActualWidth;
            double viewportCanvasHeight = viewportCanvas.ActualHeight;

            double viewportCanvasCenterX = viewportCanvasWidth * 0.5;
            double viewportCanvasCenterY = viewportCanvasHeight * 0.5;


            // Clear all existing Polygons
            viewportCanvas.Children.Clear();

            // IterateGeometryModel3DObjects will traverse the Visual3D and Model3DGroup objects 
            // and will call the callback delegate for each GeometryModel3D.
            // The callback delegate also receives the transformation that were applied to the parents of the GeometryModel3D.
            // If you want to be called for each ModelVisual3D, use the IterateModelVisualsObjects method
            Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(visuals: visuals, 
                                                                       parentTransform3D: null, // optionally we can specify the initial transformation
                                                                       callback: delegate(GeometryModel3D geometryModel3D, Transform3D parentTransform3D)
            {
                // This is called for each GeometryModel3D in the hierarchy
                var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;

                if (meshGeometry3D == null)
                    return;

                var positions = meshGeometry3D.Positions;
                var triangleIndices = meshGeometry3D.TriangleIndices;

                if (positions == null || positions.Count == 0 || triangleIndices == null || triangleIndices.Count == 0)
                    return;

                // Get fill brush from material
                var materialBrush = GetMaterialBrush(geometryModel3D.Material);

                // If this geometryModel3D has any transformation, combine it with the parentTransform3D - this will be the world transformation for this geometryModel3D
                var worldTransform = CombineTransform(parentTransform3D, geometryModel3D.Transform);

                Matrix3D worldViewProjectionMatrix3D;

                // Get final worldViewProjectionMatrix3D
                if (worldTransform == null || worldTransform.Value.IsIdentity)
                    worldViewProjectionMatrix3D = viewProjectionMatrix3D;
                else
                    worldViewProjectionMatrix3D = worldTransform.Value * viewProjectionMatrix3D;
                

                var triangleIndicesCount = triangleIndices.Count;

                // Go through all triangles
                for (int i = 0; i < triangleIndicesCount; i += 3)
                {
                    // Read positions for one triangle
                    Point3D p1 = positions[triangleIndices[i]];
                    Point3D p2 = positions[triangleIndices[i + 1]];
                    Point3D p3 = positions[triangleIndices[i + 2]];

                    // Transform to homogeneous coordinates (to get final x and y we need to divide them by w)
                    Point4D h1 = worldViewProjectionMatrix3D.Transform(new Point4D(p1.X, p1.Y, p1.Z, 1));
                    Point4D h2 = worldViewProjectionMatrix3D.Transform(new Point4D(p2.X, p2.Y, p2.Z, 1));
                    Point4D h3 = worldViewProjectionMatrix3D.Transform(new Point4D(p3.X, p3.Y, p3.Z, 1));

                    // Do a simple clip test - if any position is behind the camera we will not render this triangle
                    if (h1.Z < 0 || h2.Z < 0 || h3.Z < 0)
                        return;


                    // Convert to screen coordinates by dividing by w and then adjust to the size of canvas (we also invert y)
                    Point canvas1 = new Point(                         (h1.X * viewportCanvasWidth)  / (2 * h1.W) + viewportCanvasCenterX,
                                               viewportCanvasHeight - ((h1.Y * viewportCanvasHeight) / (2 * h1.W) + viewportCanvasCenterY)); // invert y

                    Point canvas2 = new Point(                         (h2.X * viewportCanvasWidth)  / (2 * h2.W) + viewportCanvasCenterX,
                                               viewportCanvasHeight - ((h2.Y * viewportCanvasHeight) / (2 * h2.W) + viewportCanvasCenterY));

                    Point canvas3 = new Point(                         (h3.X * viewportCanvasWidth)  / (2 * h3.W) + viewportCanvasCenterX,
                                               viewportCanvasHeight - ((h3.Y * viewportCanvasHeight) / (2 * h3.W) + viewportCanvasCenterY));


                    // Create triangle
                    // TODO: This creates many Polyline objects on each change of camera - to improve performance it would be possible to reuse the existing Polylines and just change the position's coordinates

                    var points = new PointCollection(3);
                    points.Add(canvas1);
                    points.Add(canvas2);
                    points.Add(canvas3);

                    var polyline = new Polygon()
                    {
                        Points = points,
                        StrokeThickness = 1,
                        StrokeMiterLimit = 1
                    };

                    if (fillPolygon)
                    {
                        polyline.Fill = materialBrush;
                        polyline.Stroke = Brushes.Black;
                    }
                    else
                    {
                        polyline.Fill = null;
                        polyline.Stroke = materialBrush;
                    }

                    viewportCanvas.Children.Add(polyline);
                }
            });
        }

        private static Brush GetMaterialBrush(Material material)
        {
            var diffuseMaterial = material as DiffuseMaterial;

            if (diffuseMaterial != null)
                return diffuseMaterial.Brush;

            var materialGroup = material as MaterialGroup;
            if (materialGroup != null)
            {
                foreach (var child in materialGroup.Children)
                {
                    var childDiffuseMaterial = child as DiffuseMaterial;
                    if (childDiffuseMaterial != null)
                        return childDiffuseMaterial.Brush;
                }
            }

            return null;
        }

        private static Transform3D CombineTransform(Transform3D parent, Transform3D child)
        {
            Transform3D finalTransform;

            if (parent == null || parent.Value.IsIdentity)
            {
                if (child == null || child.Value.IsIdentity)
                    finalTransform = null;
                else
                    finalTransform = child;
            }
            else
            {
                if (child == null || child.Value.IsIdentity)
                    finalTransform = parent;
                else
                    finalTransform = new MatrixTransform3D(child.Value * parent.Value); // First apply child and than parent transform
            }

            return finalTransform;
        }
        #endregion

        private void OnFillPolygonsCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            bool fillPolygon = FillPolygonsCheckBox.IsChecked ?? false;
            UpdateViewportCanvas(ObjectsRootVisual3D.Children, Camera1, ViewportCanvas, fillPolygon);
        }
    }
}
