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
using Ab3d.Meshes;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for TextureCoordinatesGeneratorSample.xaml
    /// </summary>
    public partial class TextureCoordinatesGeneratorSample : Page
    {
        public TextureCoordinatesGeneratorSample()
        {
            Ab3d.Utilities.MeshUtils.CreatePolygonIndicesByDefault = true; // This will generate polygon indices for 3D objects used in preview Viewport3D (see WireframePolygonLines for more info)

            InitializeComponent();

            CreateScene();
            RegenerateAllTextureCoordinates();
        }

        private void RegenerateAllTextureCoordinates()
        {
            foreach (var modelVisual3D in MainViewport.Children.OfType<ModelVisual3D>())
            {
                GenerateTextureCoordinates(modelVisual3D);
            }
        }

        private void GenerateTextureCoordinates(ModelVisual3D visual3D)
        {
            GenerateTextureCoordinates(visual3D.Content);
        }

        private void GenerateTextureCoordinates(Model3D model3D)
        {
            if (model3D == null)
                return;

            var geometryModel3D = model3D as GeometryModel3D;

            if (geometryModel3D != null)
            {
                var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;

                if (meshGeometry3D != null)
                {
                    PointCollection textureCoordinates;
                    Model3D previewModel3D;

                    if (Planar1RadioButton.IsChecked ?? false)
                    {
                        // Generate texture coordinates by projecting the 3D positions to a 2D plane
                        // That is defined by planeNormalVector and planeHeightVector.

                        // This sample uses a horizontal plane (normal = 0,1,0)
                        textureCoordinates = Ab3d.Utilities.MeshUtils.GeneratePlanarTextureCoordinates(
                            mesh: meshGeometry3D,
                            planeNormalVector: new Vector3D(0, 1, 0),
                            planeHeightVector: new Vector3D(0, 0, -1),
                            flipXCoordinate: false,
                            flipYCoordinate: false,
                            flipBackwardFacingXCoordinate: false);

                        // It is also possible to call an override GeneratePlanarTextureCoordinates that also takes customBounds as parameter.
                        // This allows specifying which part of the texture will be shown.
                        // 
                        // To get the bounds that are used by default, you can use the following code:
                        //Rect bounds;
                        //List<Point> projectedPositions = Ab3d.Utilities.MeshUtils.Project3DPointsTo2DPlane(meshGeometry3D.Positions, planeNormalVector, planeHeightVector, out bounds);
                        //
                        // Note that when using custom bounds, the ImageBrush.ViewportUnits must be changed to Absolute.


                        // previewModel3D is used to show the 3D model that represents the projection used in texture coordinates generation
                        previewModel3D = Ab3d.Models.Model3DFactory.CreatePlane(new Point3D(0, 0, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, -1), new Size(50, 50), 1, 1, null);
                    }
                    else if (Planar2RadioButton.IsChecked ?? false)
                    {
                        // This sample uses a vertical plane (normal = 0,0,1)
                        textureCoordinates = Ab3d.Utilities.MeshUtils.GeneratePlanarTextureCoordinates(
                            mesh: meshGeometry3D,
                            planeNormalVector: new Vector3D(0, 0, 1),
                            planeHeightVector: new Vector3D(0, 1, 0),
                            flipXCoordinate: false,
                            flipYCoordinate: false,
                            flipBackwardFacingXCoordinate: false);

                        previewModel3D = Ab3d.Models.Model3DFactory.CreatePlane(new Point3D(0, 0, 0), new Vector3D(0, 0, 1), new Vector3D(0, 1, 0), new Size(50, 50), 1, 1, null);
                    }
                    else if (Planar3RadioButton.IsChecked ?? false)
                    {
                        // This sample uses a sloped plane (normal is angled at 45 degrees)
                        textureCoordinates = Ab3d.Utilities.MeshUtils.GeneratePlanarTextureCoordinates(
                            mesh: meshGeometry3D,
                            planeNormalVector: new Vector3D(0, 0.71, 0.71),
                            planeHeightVector: new Vector3D(0, 0.71, -0.71),
                            flipXCoordinate: false,
                            flipYCoordinate: false,
                            flipBackwardFacingXCoordinate: false);

                        previewModel3D = Ab3d.Models.Model3DFactory.CreatePlane(new Point3D(0, 0, 0), new Vector3D(0, 0.71, 0.71), new Vector3D(0, 0.71, -0.71), new Size(50, 50), 1, 1, null);
                    }
                    else if (CubicRadioButton.IsChecked ?? false)
                    {
                        // Generate texture coordinates for meshGeometry3D with 
                        // vertical cylinder (cylinderDirectionVector = up) and 
                        // plane Height vector into the screen (the texture on top of the cylinder will be oriented so that its top will be directed into the screen (0,0,-1))
                        textureCoordinates = Ab3d.Utilities.MeshUtils.GenerateCubicTextureCoordinates(meshGeometry3D);

                        previewModel3D = Ab3d.Models.Model3DFactory.CreateBox(new Point3D(0, 0, 0), new Size3D(50, 50, 50), null);
                    }
                    else if (CylinderRadioButton.IsChecked ?? false)
                    {
                        // Generate texture coordinates for meshGeometry3D with 
                        // vertical cylinder (cylinderDirectionVector = up) and 
                        // plane Height vector into the screen (the texture on top of the cylinder will be oriented so that its top will be directed into the screen (0,0,-1))
                        textureCoordinates = Ab3d.Utilities.MeshUtils.GenerateCylindricalTextureCoordinates(
                                                                        mesh: meshGeometry3D,
                                                                        cylinderDirectionVector: new Vector3D(0, 1, 0), // up
                                                                        cylinderPlaneHeightVector: new Vector3D(0, 0, -1)); // into the screen

                        previewModel3D = Ab3d.Models.Model3DFactory.CreateCylinder(new Point3D(0, -25, 0), 20, 50, 10, false, null);
                    }
                    else
                    {
                        textureCoordinates = null;
                        previewModel3D = null;
                    }

                    meshGeometry3D.TextureCoordinates = textureCoordinates;

                    PreviewWireframeVisual3D.OriginalModel = previewModel3D;
                }
            }
            else
            {
                var model3DGroup = model3D as Model3DGroup;
                if (model3DGroup != null)
                {
                    foreach (var childModel3D in model3DGroup.Children)
                        GenerateTextureCoordinates(childModel3D);
                }
            }
        }

        private void CreateScene()
        {
            // Create material with 10 x 10 numbers grid
            var imageBrush = new ImageBrush(new BitmapImage(new Uri(@"pack://application:,,,/Resources/10x10-texture.png")));

            var material = new DiffuseMaterial(imageBrush);
            //var material = new DiffuseMaterial(Brushes.Silver);


            // Create box with 10 x 10 x 10 cells
            var boxVisual3D = new BoxVisual3D()
            {
                UseCachedMeshGeometry3D = false, // This will generate a new MeshGeometry3D for this BoxVisual3D,
                FreezeMeshGeometry3D = false,    // This will allow us to change the TextureCoordinates
                CenterPosition = new Point3D(-150, 0, -50),
                Size = new Size3D(60, 60, 60),
                XCellsCount = 10,
                YCellsCount = 10,
                ZCellsCount = 10,
                Material = material,
            };

            MainViewport.Children.Add(boxVisual3D);


            var cylinderVisual3D = new CylinderVisual3D()
            {
                BottomCenterPosition = new Point3D(-50, -30, -50),
                Radius = 30,
                Height = 60,
                Segments = 10,
                IsSmooth = false,
                Material = material
            };

            MainViewport.Children.Add(cylinderVisual3D);


            var sphereVisual3D = new SphereVisual3D()
            {
                UseCachedMeshGeometry3D = false, // This will generate a new MeshGeometry3D for this BoxVisual3D,
                FreezeMeshGeometry3D = false,    // This will allow us to change the TextureCoordinates
                CenterPosition = new Point3D(50, 0, -50),
                Radius = 30,
                Material = material
            };

            MainViewport.Children.Add(sphereVisual3D);


            // Add dragon model
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\ObjFiles\\dragon_vrip_res3.obj");
            var objModelVisual3D = new ObjModelVisual3D()
            {
                Source = new Uri(fileName, UriKind.Absolute),
                SizeX = 70,
                Position = new Point3D(-150, 0, 100),
                PositionType = ObjModelVisual3D.VisualPositionType.Center,
                DefaultMaterial = material,
            };

            Ab3d.Utilities.ModelUtils.ChangeMaterial(objModelVisual3D.Content, material, newBackMaterial: null); 

            MainViewport.Children.Add(objModelVisual3D);


            // Add simple extruded shape
            var extrudePositions = new Point[]
            {
                new Point(-1, 54),
                new Point(13, 35),
                new Point(8, 32),
                new Point(18, 13),
                new Point(11, 9),
                new Point(23, -13),
                new Point(6, -14),
                new Point(6, -29),
                new Point(-8, -30),
                new Point(-7, -13),
                new Point(-25, -9),
                new Point(-12, 9),
                new Point(-25, 16),
                new Point(-8, 31),
                new Point(-16, 38)
            };

            var extrudedMesh = Mesh3DFactory.CreateExtrudedMeshGeometry(positions: extrudePositions.ToList(),
                                                                        isSmooth: false,
                                                                        modelOffset: new Vector3D(-50, -15, 100),
                                                                        extrudeVector: new Vector3D(0, 30, 0),
                                                                        shapeYVector: new Vector3D(0, 0, -1),
                                                                        textureCoordinatesGenerationType: ExtrudeTextureCoordinatesGenerationType.AddAdditionalPositions);

            var geometryModel3D = new GeometryModel3D(extrudedMesh, material);

            var modelVisual3D = new ModelVisual3D();
            modelVisual3D.Content = geometryModel3D;

            MainViewport.Children.Add(modelVisual3D);
        }

        private void OnGeneratorTypeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            RegenerateAllTextureCoordinates();
        }
    }
}
