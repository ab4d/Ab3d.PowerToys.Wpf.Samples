using System;
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
using Ab3d.Meshes;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    /// <summary>
    /// Interaction logic for WireframePolygonLines.xaml
    /// </summary>
    public partial class WireframePolygonLines : Page
    {
        public WireframePolygonLines()
        {
            InitializeComponent();

            // This sample shows how to show polygon lines for the 3D models created in Ab3d.PowerToys library.
            //
            // To enable creation of polygon indices (position indexes that define polygon lines),
            // you can set the CreatePolygonIndices property on the Mesh3D objects that is used to created 3D object.
            //
            // For example:
            // var boxMesh3D = new Ab3d.Meshes.BoxMesh3D(new Point3D(0, 0, 0), new Size3D(10, 10, 10), 1, 1, 1);
            // boxMesh3D.CreatePolygonIndices = true;
            // var boxMeshGeometry = boxMesh3D.Geometry;
            //
            // The CreatePolygonIndices property is actually nullable Boolean.
            // When its value is not set (is null), then a global static Ab3d.Utilities.MeshUtils.CreatePolygonIndicesByDefault value is used.
            //
            // This way it is easy to enable creation of polygon indices for all objects.
            // This is also done in this sample:

            Ab3d.Utilities.MeshUtils.CreatePolygonIndicesByDefault = true;

            // Create sample objects
            Model3DGroup sceneModel3DGroup = CreateScene();

            // We will show the polygon lines with using WireframeVisual3D.
            // To show polygon lines instead of triangle wireframe, we need to set ShowPolygonLines to true.
            WireframeVisual1.ShowPolygonLines = true;

            // Now assign Model3DGroup that contains the sample 3D objects
            WireframeVisual1.OriginalModel = sceneModel3DGroup;


            // IMPORTANT:
            // WireframeVisual3D is great for showing polygon lines for mostly static objects and for not very complex scenes.
            // But if you have a complex scene where the objects change often, using the WireframeVisual3D can become slow.
            // In this case it is recommended to show polygon lines manually with using MultiLineVisual3D or some similar object (see code below).

            // The following code shows how to show polygon lines manually (without WireframeVisual3D object)
            // Those lines will be shown as white to distinguish them from the lines created by WireframeVisual3D.

            // First create box model with polygon.
            var boxMesh3D = new Ab3d.Meshes.BoxMesh3D(centerPosition: new Point3D(-15, 4, 90), size: new Size3D(40, 8, 20), xSegments: 1, ySegments: 1, zSegments: 1);
            boxMesh3D.CreatePolygonIndices = true;

            var boxMeshGeometry3D = boxMesh3D.Geometry;


            // You can get polygon indices with GetPolygonIndices extension method on meshGeometry3D
            Int32Collection polygonIndices = boxMeshGeometry3D.GetPolygonIndices();

            // But instead of using polygon indices we need polygonpositions. This can be get with GetPolygonPositions
            Point3DCollection polygonPositions = Ab3d.Utilities.MeshUtils.GetPolygonPositions(boxMeshGeometry3D);

            // Now use the positions to create a MultiLineVisual3D object
            var multiLineVisual3D = new Ab3d.Visuals.MultiLineVisual3D()
            {
                Positions = polygonPositions,
                LineColor = Colors.White,
                LineThickness = 2
            };

            MainViewport.Children.Add(multiLineVisual3D);


            // We will also create a solid model from the box
            var material = this.FindResource("ObjectsMaterial") as Material;
            var geometryModel3D = new GeometryModel3D(boxMeshGeometry3D, material);

            var modelVisual3D = new ModelVisual3D();
            modelVisual3D.Content = geometryModel3D;

            MainViewport.Children.Add(modelVisual3D);
        }

        private Model3DGroup CreateScene()
        {
            var material = this.FindResource("ObjectsMaterial") as Material;

            var rootModel3DGroup = new Model3DGroup();

            var geometryModel3D = Ab3d.Models.Model3DFactory.CreateBox(centerPosition: new Point3D(30, 10, 30), 
                                                                       size: new Size3D(20, 20, 20), 
                                                                       material: material);
            rootModel3DGroup.Children.Add(geometryModel3D);


            geometryModel3D = Ab3d.Models.Model3DFactory.CreateBox(centerPosition: new Point3D(30, 10, 60), 
                                                                       size: new Size3D(20, 20, 20), 
                                                                       material: material,
                                                                       xCellsCount: 3,
                                                                       yCellsCount: 3,
                                                                       zCellsCount: 3);
            rootModel3DGroup.Children.Add(geometryModel3D);


            geometryModel3D = Ab3d.Models.Model3DFactory.CreateSphere(centerPosition: new Point3D(0, 10, 0), 
                                                                      radius: 10, 
                                                                      segments: 10, 
                                                                      material: material);
            rootModel3DGroup.Children.Add(geometryModel3D);


            geometryModel3D = Ab3d.Models.Model3DFactory.CreatePyramid(bottomCenterPosition: new Point3D(0, 0, 30),
                                                                      size: new Size3D(20, 20, 20),
                                                                      material: material);
            rootModel3DGroup.Children.Add(geometryModel3D);


            geometryModel3D = Ab3d.Models.Model3DFactory.CreateCone(bottomCenterPosition: new Point3D(-30, 0, -30),
                                                                    bottomRadius: 10,
                                                                    topRadius: 0,
                                                                    height: 20,
                                                                    material: material,
                                                                    segments: 30,
                                                                    isSmooth: true); // When Cone is smooth, the side polygon lines will not be created
            rootModel3DGroup.Children.Add(geometryModel3D);


            geometryModel3D = Ab3d.Models.Model3DFactory.CreateCone(bottomCenterPosition: new Point3D(0, 0, -30),
                                                                    bottomRadius: 10,
                                                                    topRadius: 5,
                                                                    height: 20,
                                                                    material: material,
                                                                    segments: 30,
                                                                    isSmooth: true);
            rootModel3DGroup.Children.Add(geometryModel3D);


            geometryModel3D = Ab3d.Models.Model3DFactory.CreateCone(bottomCenterPosition: new Point3D(30, 0, -30),
                                                                    bottomRadius: 10,
                                                                    topRadius: 5,
                                                                    height: 20,
                                                                    material: material,
                                                                    segments: 6,
                                                                    isSmooth: false);
            rootModel3DGroup.Children.Add(geometryModel3D);


            geometryModel3D = Ab3d.Models.Model3DFactory.CreateCylinder(bottomCenterPosition: new Point3D(30, 0, 0),
                                                                    radius: 10,
                                                                    height: 20,
                                                                    material: material,
                                                                    segments: 30,
                                                                    isSmooth: true);
            rootModel3DGroup.Children.Add(geometryModel3D);


            geometryModel3D = Ab3d.Models.Model3DFactory.CreateCylinder(bottomCenterPosition: new Point3D(60, 0, 0),
                                                                    radius: 10,
                                                                    height: 20,
                                                                    material: material,
                                                                    segments: 6,
                                                                    isSmooth: false);
            rootModel3DGroup.Children.Add(geometryModel3D);


            geometryModel3D = Ab3d.Models.Model3DFactory.CreateTrapezoid(bottomCenterPosition: new Point3D(60, 0, -30),
                                                                         bottomSize: new Size(20, 15), 
                                                                         topSize: new Size(10, 5),
                                                                         height: 20,
                                                                         material: material);
            rootModel3DGroup.Children.Add(geometryModel3D);


            geometryModel3D = Ab3d.Models.Model3DFactory.CreatePlane(centerPosition: new Point3D(-30, 0, 60),
                                                                     planeNormal: new Vector3D(0, 1, 0),
                                                                     planeHeightDirection: new Vector3D(0, 0, -1),
                                                                     size: new Size(20, 20),
                                                                     material: material,
                                                                     width_cells_count: 1,
                                                                     length_cells_count: 1);
            geometryModel3D.BackMaterial = geometryModel3D.Material;
            rootModel3DGroup.Children.Add(geometryModel3D);


            geometryModel3D = Ab3d.Models.Model3DFactory.CreatePlane(centerPosition: new Point3D(0, 0, 60),
                                                                     planeNormal: new Vector3D(0, 1, 0),
                                                                     planeHeightDirection: new Vector3D(0, 0, -1),
                                                                     size: new Size(20, 20),
                                                                     material: material,
                                                                     width_cells_count: 3,
                                                                     length_cells_count: 3);
            geometryModel3D.BackMaterial = geometryModel3D.Material;
            rootModel3DGroup.Children.Add(geometryModel3D);


            var multiMaterialBox = Ab3d.Models.Model3DFactory.CreateMultiMaterialBox(centerPosition: new Point3D(60, 10, 30),
                                                                                     size: new Size3D(20, 20, 20),
                                                                                     topMaterial: new DiffuseMaterial(Brushes.Blue), 
                                                                                     bottomMaterial: material,
                                                                                     leftMaterial: material,
                                                                                     rightMaterial: material,
                                                                                     frontMaterial: material,
                                                                                     backMaterial: material,
                                                                                     isBackMaterialSet: false);
            rootModel3DGroup.Children.Add(multiMaterialBox);


            multiMaterialBox = Ab3d.Models.Model3DFactory.CreateMultiMaterialBox(centerPosition: new Point3D(60, 10, 60),
                                                                                 size: new Size3D(20, 20, 20),
                                                                                 xCellsCount: 3,
                                                                                 yCellsCount: 3, 
                                                                                 zCellsCount: 3,
                                                                                 topMaterial: new DiffuseMaterial(Brushes.Blue), 
                                                                                 bottomMaterial: material,
                                                                                 leftMaterial: material,
                                                                                 rightMaterial: material,
                                                                                 frontMaterial: material,
                                                                                 backMaterial: material,
                                                                                 isBackMaterialSet: false);
            rootModel3DGroup.Children.Add(multiMaterialBox);


            var circleMesh3D = new Ab3d.Meshes.CircleMesh3D(new Point3D(-30, 0, 30), new Vector3D(0, 1, 0), new Vector3D(0, 0, -1), 10, 10);
            var model3D = new GeometryModel3D(circleMesh3D.Geometry, material);
            model3D.BackMaterial = material;

            rootModel3DGroup.Children.Add(model3D);


            var tubeMesh3D = new Ab3d.Meshes.TubeMesh3D(new Point3D(60, 0, 90), new Vector3D(0, 1, 0), 10, 8, 6, 4, 20, 10);
            model3D = new GeometryModel3D(tubeMesh3D.Geometry, material);
            model3D.BackMaterial = material;

            rootModel3DGroup.Children.Add(model3D);


            tubeMesh3D = new Ab3d.Meshes.TubeMesh3D(new Point3D(30, 0, 90), new Vector3D(0, 1, 0), 10, 8, 8, 6, 0, 10); // Tube with height == 0 is a special case
            model3D = new GeometryModel3D(tubeMesh3D.Geometry, material);
            model3D.BackMaterial = material;

            rootModel3DGroup.Children.Add(model3D);



            //var arrowMesh3D = new Ab3d.Meshes.ArrowMesh3D(new Point3D(-30, 0, 0), new Point3D(-30, 20, 0), 3, 6, 60, 30, false);
            //model3D = new GeometryModel3D(arrowMesh3D.Geometry, material);

            //rootModel3DGroup.Children.Add(model3D);

            var sections = new LatheSection[6];

            sections[0] = new LatheSection(offset: 0, radius: 5, isSharpEdge: true);
            sections[1] = new LatheSection(0.3, 10, false);
            sections[2] = new LatheSection(0.4, 12, true);
            sections[3] = new LatheSection(0.6, 9, false);
            sections[4] = new LatheSection(0.8, 6, false);
            sections[5] = new LatheSection(1, 5, true);

            var latheMesh3D = new LatheMesh3D(new Point3D(-30, 20, 0), new Point3D(-30, 0, 0), sections, 10,
                isStartPositionClosed: true, isEndPositionClosed: true, generateTextureCoordinates: false);
            
            model3D = new GeometryModel3D(latheMesh3D.Geometry, material);
            model3D.BackMaterial = new DiffuseMaterial(Brushes.Red);

            rootModel3DGroup.Children.Add(model3D);


            return rootModel3DGroup;
        }
    }
}
