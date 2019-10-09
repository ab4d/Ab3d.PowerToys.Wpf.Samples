using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Threading;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for ModelingWithBooleanOperations.xaml
    /// </summary>
    public partial class ModelingWithBooleanOperations : Page
    {
        private ModelVisual3D _secondModelVisual3D;
        private volatile MeshGeometry3D _lastShownMeshGeometry3D;

        private MaterialGroup _orangeMaterial;
        private MaterialGroup _textureMaterial;

        // See also:
        // - Objects3D/BooleanOperationsSample to see other boolean operations
        // - Utilities/TextureCoordinatesGeneratorSample to see other options to define TextureCoordinates that are needed to show texture images

        public ModelingWithBooleanOperations()
        {
            InitializeComponent();

            _orangeMaterial = new MaterialGroup();
            _orangeMaterial.Children.Add(new DiffuseMaterial(Brushes.Orange));
            _orangeMaterial.Children.Add(new SpecularMaterial(Brushes.White, 16));

            // We will start with a simple box
            // Create it manually with MeshGeometry3D
            MeshGeometry3D initialBoxMesh3D = new Ab3d.Meshes.BoxMesh3D(new Point3D(-100, 25, 0), new Size3D(200, 50, 100), 1, 1, 1).Geometry;

            // Without boolean operations, we could simply create the box with:
            //var boxVisual3D = new BoxVisual3D()
            //{
            //    CenterPosition = new Point3D(-100, 25, 0),
            //    Size = new Size3D(200, 50, 100),
            //    UseCachedMeshGeometry3D = false, // we should not use the cached 
            //    Material = _orangeMaterial
            //};


            // Define MeshGeometry3D objects that will be used for subtractions
            var centerSphereMesh = new Ab3d.Meshes.SphereMesh3D(new Point3D(-100, 50, 0), 40, 16).Geometry;

            var box1Mesh = new Ab3d.Meshes.BoxMesh3D(new Point3D(-180, 0, -30), new Size3D(10, 200, 10), 1, 1, 1).Geometry;
            var box2Mesh = new Ab3d.Meshes.BoxMesh3D(new Point3D(-180, 0, 0),   new Size3D(10, 200, 10), 1, 1, 1).Geometry;
            var box3Mesh = new Ab3d.Meshes.BoxMesh3D(new Point3D(-180, 0, 30),  new Size3D(10, 200, 10), 1, 1, 1).Geometry;

            var cylinder1Mesh3D = new Ab3d.Meshes.CylinderMesh3D(new Point3D(-20, 0, -30), 8, 200, segments: 16, isSmooth: false).Geometry;
            var cylinder2Mesh3D = new Ab3d.Meshes.CylinderMesh3D(new Point3D(-20, 0, 0),   8, 200, segments: 16, isSmooth: false).Geometry;
            var cylinder3Mesh3D = new Ab3d.Meshes.CylinderMesh3D(new Point3D(-20, 0, 30),  8, 200, segments: 16, isSmooth: false).Geometry;


            // When doing multiple boolean operations one after another,
            // it is recommended (and mush faster) to use BooleanMesh3D object.

            var booleanMesh3D = new Ab3d.Meshes.BooleanMesh3D(initialBoxMesh3D);

            booleanMesh3D.Subtract(centerSphereMesh);

            booleanMesh3D.Subtract(box1Mesh);
            booleanMesh3D.Subtract(box2Mesh);
            booleanMesh3D.Subtract(box3Mesh);

            booleanMesh3D.Subtract(cylinder1Mesh3D);
            booleanMesh3D.Subtract(cylinder2Mesh3D);
            booleanMesh3D.Subtract(cylinder3Mesh3D);


            // It is also possible to use Subtract method on BoxVisual3D object:
            //boxVisual3D.Subtract(centerSphereMesh);
            //boxVisual3D.Subtract(box1Mesh);
            //boxVisual3D.Subtract(box2Mesh);
            //boxVisual3D.Subtract(box3Mesh);
            //boxVisual3D.Subtract(cylinder1Mesh3D);
            //boxVisual3D.Subtract(cylinder2Mesh3D);
            //boxVisual3D.Subtract(cylinder3Mesh3D);


            // We could also use the static methods on Ab3d.Utilities.MeshBooleanOperations class:
            //Ab3d.Utilities.MeshBooleanOperations.Subtract(boxVisual3D, sphereMesh);
            //Ab3d.Utilities.MeshBooleanOperations.Subtract(boxVisual3D, boxMesh);


            // When calling the Geometry getter, the BooleanMesh3D converts the internal object into MeshGeometry3D.
            var meshGeometry3D = booleanMesh3D.Geometry;

            var initialGeometryModel3D = new GeometryModel3D(meshGeometry3D, _orangeMaterial);
            var boxVisual3D            = initialGeometryModel3D.CreateModelVisual3D();

            // Now add the changed box to the scene
            MainViewport.Children.Add(boxVisual3D);



            // When working with more complex meshes, the boolean operations can take some time.
            // To prevent blocking UI thread, we can do the boolean operations in the background thread.
            // To do this we need to freeze the MeshGeometry3D that are send to the background thread and back to UI thread.
            //
            // The following code is using spheres with 30 segments to demonstrate a longer boolean operations.
            // 

            // First define the original MeshGeometry3D
            initialBoxMesh3D = new Ab3d.Meshes.BoxMesh3D(new Point3D(0, 25, 0), new Size3D(100, 50, 100), 1, 1, 1).Geometry;

            ShowNewOriginalMesh(initialBoxMesh3D);


            var initialBooleanMesh3D = new Ab3d.Meshes.BooleanMesh3D(initialBoxMesh3D);

            var sphereCenters = new Point3D[]
            {
                new Point3D(-50, 50, 50),
                new Point3D(50, 50, 50),
                new Point3D(50, 50, -50),
                new Point3D(-50, 50, -50),
                new Point3D(-50, 0, 50),
                new Point3D(50, 0, 50),
                new Point3D(50, 0, -50),
                new Point3D(-50, 0, -50)
            };

            // We can create the MeshGeometry3D and prepare the BooleanMesh3D in parallel - each can be created in its own thread
            var sphereBooleanMeshes = sphereCenters.AsParallel().Select(sphereCenter =>
            {
                //System.Diagnostics.Debug.WriteLine($"Start creating SphereMesh3D with {sphereCenter} on {System.Threading.Thread.CurrentThread.ManagedThreadId}");

                var sphereMeshGeometry3D = new Ab3d.Meshes.SphereMesh3D(sphereCenter, radius: 15, segments: 30).Geometry;
                var sphereBooleanMesh3D  = new Ab3d.Meshes.BooleanMesh3D(sphereMeshGeometry3D);

                return sphereBooleanMesh3D;
            });

            // After that we can subtract each of the sphere meshes from the initialBooleanMesh3D.
            // This can be done in background thread (we use Dispatcher.Invoke to "send" the mesh to the UI thread)
            Task.Factory.StartNew(() =>
            {
                // Note the order in which the sphereBooleanMesh are created is not determined (as the BooleanMesh3D are created, they get available to the foreach)
                foreach (var sphereBooleanMesh in sphereBooleanMeshes) 
                {
                    // Subtract the sphereBooleanMesh from the initialBooleanMesh3D
                    initialBooleanMesh3D.Subtract(sphereBooleanMesh);

                    // Get the MeshGeometry3D
                    var mesh = initialBooleanMesh3D.Geometry;

                    // Freeze the MeshGeometry3D
                    // This will allow using the mesh in UI thread (different thread as the mesh was created)
                    mesh.Freeze(); 

                    // Send it to UI thread
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ShowNewOriginalMesh(mesh);
                    }));
                }
            })
            // After all the boolean operations are completed, we generate texture coordinates
            // so we will be able to use texture image as material (meshes that are created with boolean operations do not have texture coordinates defined)
            .ContinueWith(_ => GenerateTextureCoordinates(), TaskScheduler.FromCurrentSynchronizationContext()); // The last one is run on UI thread
        }

        private void ShowNewOriginalMesh(MeshGeometry3D mesh)
        {
            // NOTE: This code must be executed in the UI thread

            // Ensure we have parent ModelVisual3D that will hold the _originalMeshGeometry3D
            if (_secondModelVisual3D == null)
            {
                _secondModelVisual3D = new ModelVisual3D();
                _secondModelVisual3D.Transform = new TranslateTransform3D(100, 0, 0);

                MainViewport.Children.Add(_secondModelVisual3D);
            }

            // If we have TextureCoordinates defined, then we can use material with texture image
            Material material;
            if (_textureMaterial != null && mesh.TextureCoordinates != null && mesh.TextureCoordinates.Count > 0)
                material = _textureMaterial;
            else
                material = _orangeMaterial;

            var geometryModel3D = new GeometryModel3D(mesh, material);

            _secondModelVisual3D.Content = geometryModel3D;

            _lastShownMeshGeometry3D = mesh;
        }

        private void GenerateTextureCoordinates()
        {
            // Boolean operation do not generate TextureCoordinates.
            // This means that you cannot show texture image as material on such object.
            //
            // But it is possible to generate TextureCoordinates with utilities from Ab3d.PowerToys.
            // In this sample we will use GenerateCubicTextureCoordinates method that will generate TextureCoordinates based on the cubic projection.
            // To see other possible ways to generate texture coordinates, see the Utilities/TextureCoordinatesGeneratorSample

            var meshGeometry3D = _lastShownMeshGeometry3D.Clone(); // Get a modifiable copy (from frozen object)

            var textureCoordinates = Ab3d.Utilities.MeshUtils.GenerateCubicTextureCoordinates(meshGeometry3D);
            meshGeometry3D.TextureCoordinates = textureCoordinates;


            // Create material with texture
            _textureMaterial = new MaterialGroup();
            //_textureMaterial.Children.Add(new DiffuseMaterial(new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/10x10-texture.png")))));
            _textureMaterial.Children.Add(new DiffuseMaterial(new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/PowerToysTexture.png")))));
            _textureMaterial.Children.Add(new SpecularMaterial(Brushes.White, 16));


            // Show the new mesh
            ShowNewOriginalMesh(meshGeometry3D);
        }
    }
}
