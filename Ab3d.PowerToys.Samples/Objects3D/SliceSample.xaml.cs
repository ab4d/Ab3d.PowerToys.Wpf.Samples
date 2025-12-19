using Ab3d.Utilities;
using Ab3d.Visuals;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Ab3d.Models;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for SliceSample.xaml
    /// </summary>
    public partial class SliceSample : Page
    {
        private MeshGeometry3D _originalMesh3D;
        private Model3D _originalModel3D;
        private ModelVisual3D _originalModelVisual3D;
        private Model3DGroup _slicesOnPlaneGroup;

        private Rect3D _modelBounds;

        private double _rootModelSize;

        private Slicer _slicer;

        public SliceSample()
        {
            InitializeComponent();

            
            // Create the Slicer helper class that can slice 3D models
            _slicer = new Slicer()
            {
                // When we use Slicer only to slice 3D models and we do not need the 2D slice shape or to close the mesh, 
                // then we can set the CollectIntersectionPoints to false.
                //CollectIntersectionPoints = false 
            };


            SetupPlanes();

            UpdateScene();

            SliceScene();
        }

        private void UpdateScene()
        {
            _originalModelVisual3D = null;
            _originalModel3D = null;
            _originalMesh3D = null;

            if (SphereModelRadioButton.IsChecked ?? false)
            {
                _originalMesh3D = new Ab3d.Meshes.SphereMesh3D(centerPosition: new Point3D(0, 0, 0), radius: 50, segments: 20).Geometry;
                _modelBounds = _originalMesh3D.Bounds;
            }
            else if (TeapotModelRadioButton.IsChecked ?? false)
            {
                string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/ObjFiles/Teapot.obj");

                var readerObj = new Ab3d.ReaderObj();
                var readModel3D = (GeometryModel3D)readerObj.ReadModel3D(fileName); // We assume that a single GeometryModel3D is returned
                
                _originalMesh3D = (MeshGeometry3D)readModel3D.Geometry;
                _modelBounds = _originalMesh3D.Bounds;
            }
            else if (MultiVisualsModelRadioButton.IsChecked ?? false)
            {
                _originalModelVisual3D = new ModelVisual3D();

                _originalModelVisual3D.Children.Add(new Ab3d.Visuals.PyramidVisual3D() { BottomCenterPosition = new Point3D(-360, -60, 0), Size = new Size3D(120, 120, 120), Material = new DiffuseMaterial(Brushes.Green) });
                _originalModelVisual3D.Children.Add(new Ab3d.Visuals.BoxVisual3D() { CenterPosition = new Point3D(-180, 0, 0), Size = new Size3D(120, 120, 60), Material = new DiffuseMaterial(Brushes.Green) });
                _originalModelVisual3D.Children.Add(new Ab3d.Visuals.TorusKnotVisual3D() { CenterPosition = new Point3D(0, 0, 0), P = 3, Q = 4, Radius1 = 40, Radius2 = 20, Radius3 = 10, USegments = 150, VSegments = 20, Material = new DiffuseMaterial(Brushes.Green) });
                _originalModelVisual3D.Children.Add(new Ab3d.Visuals.TubeVisual3D() { BottomCenterPosition = new Point3D(160, -60, 0), Segments = 4, InnerRadius = 40, OuterRadius = 60, Height = 120, Material = new DiffuseMaterial(Brushes.Green), Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)) { CenterX = 160 } });
                _originalModelVisual3D.Children.Add(new Ab3d.Visuals.TubeVisual3D() { BottomCenterPosition = new Point3D(320, -60, 0), InnerRadius = 40, OuterRadius = 60, Height = 120, Material = new DiffuseMaterial(Brushes.Green) });

                _modelBounds = new Rect3D();
                foreach (var visual3D in _originalModelVisual3D.Children.OfType<ModelVisual3D>())
                    _modelBounds.Union(visual3D.Content.Bounds);
            }
            else if (RobotarmModelRadioButton.IsChecked ?? false)
            {
                // Use ObjModelVisual3D to load robotarm.obj and then read its Content to get Model3D
                var objModelVisual3D = new ObjModelVisual3D()
                {
                    Source = new Uri("pack://application:,,,/Ab3d.PowerToys.Samples;component/Resources/ObjFiles/robotarm.obj", UriKind.Absolute),
                    SizeX = 100,
                    Position = new Point3D(0, 0, 0),
                    PositionType = ObjModelVisual3D.VisualPositionType.BottomCenter
                };

                _originalModel3D = objModelVisual3D.Content;
                _modelBounds = _originalModel3D.Bounds;
            }

            _rootModelSize = Math.Sqrt(_modelBounds.SizeX * _modelBounds.SizeX + _modelBounds.SizeY * _modelBounds.SizeY + _modelBounds.SizeZ * _modelBounds.SizeZ);
            Camera1.Distance = 2 * _rootModelSize;
        }

        private void SliceScene()
        {
            // Clear all existing slices and related models
            FrontVisual3D.Children.Clear();
            FrontVisual3D.Content = null;

            BackVisual3D.Children.Clear();
            BackVisual3D.Content = null;

            WireframeVisual3D.Positions.Clear();


            // Get selected 3D plane that will slice the models
            var plane = GetSelectedPlane();
            var planeTransform = GetSelectedPlaneTransform(updateTransformationAmount: true);

            // Transform the plane that will be used to slice the model
            plane.Transform(planeTransform);


            // Update the used Plane in the Slicer
            _slicer.Plane = plane;

            _slicer.CloseSlicedMeshes = CloseMeshesCheckBox.IsChecked ?? false;


            ModelVisual3D frontModelVisual3D, backModelVisual3D;

            // When _originalModelVisual3D is defined we demonstrate the use of SliceModelVisual3D method.
            if (_originalModelVisual3D != null)
            {
                // Each SliceXXXX method returns two new object:
                // one that lies in front of the plane (as defined with plane's normal vector (A, B, C value in Plane object),
                // one that lines in the back of the plane.
                // 
                // If we only need the front object, we can use the SliceXXXX method that only takes 1 or 2 parameters and return the front object - for example:
                //frontModelVisual3D = _slicer.SliceModelVisual3D(_originalModelVisual3D, optionalTransform);
                //backModelVisual3D = null;

                _slicer.SliceModelVisual3D(_originalModelVisual3D, out frontModelVisual3D, out backModelVisual3D);
            }
            else
            {
                Model3D frontModel3D, backModel3D;

                // When _originalModel3D is defined we demonstrate the use of SliceModel3D method (can slice GeometryModel3D or Model3DGroup objects)
                if (_originalModel3D != null)
                {
                    _slicer.SliceModel3D(_originalModel3D, frontModel3D: out frontModel3D, backModel3D: out backModel3D);

                    // To get only front objects, we can use the following method:
                    //frontModel3D = _slicer.SliceModel3D(_originalModel3D, parentTransform: transform);
                    //backModel3D = null;
                }
                // When _originalMesh3D is defined we demonstrate the use of SliceMeshGeometry3D method.
                else if (_originalMesh3D != null)
                {
                    MeshGeometry3D frontMesh, backMash;
                    _slicer.SliceMeshGeometry3D(_originalMesh3D, frontMeshGeometry3D: out frontMesh, backMeshGeometry3D: out backMash);

                    // To get only front objects, we can use the following method:
                    //frontMesh = _slicer.SliceMeshGeometry3D(originalMesh3D, transform: transform);
                    //backMash = null;

                    if (frontMesh != null)
                    {
                        var frontGeometryModel3D = new GeometryModel3D(frontMesh, new DiffuseMaterial(Brushes.Gold));
                        frontModel3D = frontGeometryModel3D;
                    }
                    else
                    {
                        frontModel3D = null;
                    }

                    if (backMash != null)
                    {
                        var backGeometryModel3D = new GeometryModel3D(backMash, new DiffuseMaterial(Brushes.Gold));
                        backModel3D = backGeometryModel3D;
                    }
                    else
                    {
                        backModel3D = null;
                    }
                }
                else
                {
                    frontModel3D = null;
                    backModel3D = null;
                }

                if (frontModel3D != null)
                    frontModelVisual3D = frontModel3D.CreateModelVisual3D();
                else
                    frontModelVisual3D = null;

                if (backModel3D != null)
                    backModelVisual3D = backModel3D.CreateModelVisual3D();
                else
                    backModelVisual3D = null;
            }


            ShowSlice(frontModelVisual3D, isFront: true);
            ShowSlice(backModelVisual3D, isFront: false);

            UpdateSlice2DLines();
        }

        private void ShowSlice(ModelVisual3D modelVisual3D, bool isFront)
        {
            if (modelVisual3D == null ||
                (isFront && !(ShowFrontCheckBox.IsChecked ?? false)) ||
                (!isFront && !(ShowBackCheckBox.IsChecked ?? false)))
            {
                return;
            }


            // Set BackMaterial to red color to show the inner parts of the models
            Ab3d.Utilities.ModelUtils.ChangeBackMaterial(modelVisual3D, new DiffuseMaterial(Brushes.Red));

            
            // Define separationTransform that separates the front and back models
            var plane = _slicer.Plane;
            double slicedModelsSeparation = _rootModelSize * 0.2;

            if (!isFront)
                slicedModelsSeparation *= -1;

            var separationTransform = new TranslateTransform3D(plane.A * slicedModelsSeparation, 
                                                               plane.B * slicedModelsSeparation, 
                                                               plane.C * slicedModelsSeparation);

            modelVisual3D.Transform = separationTransform;


            // Add to scene
            if (isFront)
                FrontVisual3D.Children.Add(modelVisual3D);
            else
                FrontVisual3D.Children.Add(modelVisual3D);


            // Add wireframe
            if (ShowWireframeCheckBox.IsChecked ?? false)
            {
                var wireframePositions = WireframeVisual3D.Positions;
                WireframeVisual3D.Positions = null; // Disconnect to prevent firing change events on each added position

                WireframeFactory.AddWireframeLinePositions(modelVisual3D, parentTransform: null, usePolygonIndices: false, removedDuplicates: true, wireframeLinePositions: wireframePositions);

                WireframeVisual3D.Positions = wireframePositions; // Added positions back
            }
        }

        private void UpdateSlice2DLines()
        {
            Slice2DCanvas.Children.Clear();

            if (_slicesOnPlaneGroup == null)
            {
                _slicesOnPlaneGroup = new Model3DGroup();
                SliceOnPlaneVisual3D.Content = _slicesOnPlaneGroup;
            }
            else
            {
                _slicesOnPlaneGroup.Children.Clear();
            }


            if (_originalModelVisual3D != null)
            {
                Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
                    rootVisual: _originalModelVisual3D,
                    callback: delegate (GeometryModel3D geometryModel3D, Transform3D parentTransform3D)
                    {
                        var mesh = geometryModel3D.Geometry as MeshGeometry3D;
                        if (mesh == null)
                            return;

                        ShowMeshSliceLines(mesh, geometryModel3D.Material);
                    });
            }
            else if (_originalModel3D != null)
            {
                Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
                    model3D: _originalModel3D,
                    parentTransform3D: null,
                    callback: delegate (GeometryModel3D geometryModel3D, Transform3D parentTransform3D)
                    {
                        var mesh = geometryModel3D.Geometry as MeshGeometry3D;
                        if (mesh == null)
                            return;

                        ShowMeshSliceLines(mesh, geometryModel3D.Material);
                    });
            }
            else if (_originalMesh3D != null)
            {
                ShowMeshSliceLines(_originalMesh3D, material: null);
            }


            // Setup Slice2DCanvas RenderTransform so that the 2D shape is fit into the Slice2DCanvas

            var boundingBox = GetSelectedModelBoundingBox();

            if (!boundingBox.IsEmpty)
            {
                var boundPoints = boundingBox.GetCorners();

                _slicer.Plane.Project3DPointsToPlane(boundPoints, out Rect projectedBoundingBox);
                
                double resultWindowSize = Math.Min(Slice2DCanvas.Width, Slice2DCanvas.Height);

                double scaleFactor = Math.Max(projectedBoundingBox.Width, projectedBoundingBox.Height);
                scaleFactor = (resultWindowSize * 0.8) / scaleFactor; // 0.8 is used to add some margin around lines

                var transformGroup = new TransformGroup();

                // We need to negate the yScale because on Slice2DCanvas the y-axis points down (in 3D view the y-axis is up).
                // Because we negate the y-axis, we also need to do the same for the x-axis to reserve the x orientation.
                transformGroup.Children.Add(new ScaleTransform(-scaleFactor, -scaleFactor));

                transformGroup.Children.Add(new TranslateTransform(resultWindowSize / 2 - (projectedBoundingBox.X + projectedBoundingBox.Width / 2) * scaleFactor,
                                                                   resultWindowSize / 2 - (projectedBoundingBox.Y + projectedBoundingBox.Height / 2) * scaleFactor));

                Slice2DCanvas.RenderTransform = transformGroup;
            }
            else
            {
                Slice2DCanvas.RenderTransform = null;
            }
        }

        private void ShowMeshSliceLines(MeshGeometry3D mesh, Material material)
        {
            // To get the intersection polygons, call the GetProjectedIntersectionPolylines
            var intersectionPolygons = _slicer.GetProjectedIntersectionPolylines(mesh, useMeshTransform: true, out var bounds);

            // When any of the polygon is not closed then we need to show individual intersection lines (instead of connected polylines)
            if (_slicer.HasBrokenIntersectionPolylines)
            {
                ShowIntersectionPoints(mesh);
                return;
            }


            if (intersectionPolygons == null || intersectionPolygons.Length == 0)
                return;


            var lineColor = Ab3d.Utilities.ModelUtils.GetMaterialDiffuseColor(material, defaultColor: Colors.Black);
            var lineBrush = new SolidColorBrush(lineColor);
            lineBrush.Freeze();

            foreach (var intersectionPolygon in intersectionPolygons)
            {
                var polyline = new Polyline()
                {
                    Points = new PointCollection(intersectionPolygon),
                    Stroke = lineBrush,
                    StrokeThickness = 1
                };

                Slice2DCanvas.Children.Add(polyline);

                //// Show orientation of the shape:
                //var line = new Line()
                //{
                //    X1 = intersectionPolygon[0].X,
                //    Y1 = intersectionPolygon[0].Y,
                //    X2 = intersectionPolygon[1].X,
                //    Y2 = intersectionPolygon[1].Y,
                //    Stroke = Brushes.Red,
                //    StrokeThickness = 6,
                //    StrokeEndLineCap = PenLineCap.Triangle
                //};

                //Slice2DCanvas.Children.Add(line);
            }


            if (ShowSliceMeshCheckBox.IsChecked ?? false)
            {
                var closedSliceMesh = _slicer.GetClosedSliceMesh(mesh, useMeshTransform: true);

                if (closedSliceMesh != null)
                {
                    var closedSliceMaterial = new DiffuseMaterial(Brushes.Silver);
                    var geometryModel3D = new GeometryModel3D(closedSliceMesh, closedSliceMaterial)
                    {
                        BackMaterial = closedSliceMaterial
                    };

                    _slicesOnPlaneGroup.Children.Add(geometryModel3D);


                    if (ShowWireframeCheckBox.IsChecked ?? false)
                    {
                        var wireframePositions = WireframeVisual3D.Positions;
                        WireframeVisual3D.Positions = null; // Disconnect to prevent firing change events on each added position

                        WireframeFactory.AddWireframeLinePositions(geometryModel3D, transform: null, usePolygonIndices: false, removedDuplicates: true, wireframeLinePositions: wireframePositions);

                        WireframeVisual3D.Positions = wireframePositions; // Added positions back
                    }
                }
            }
        }

        private void ShowIntersectionPoints(MeshGeometry3D mesh)
        {
            // When we cannot create a closed polygon from the mesh's slice (_slicer.HasBrokenIntersectionPolylines is true),
            // then we can call GetProjectedIntersectionPoints to get individual intersection lines
            var intersectionPoints = _slicer.GetProjectedIntersectionPoints(mesh, useMeshTransform: true, out var bounds);

            var redBrush = Brushes.Red;

            for (var i = 0; i < intersectionPoints.Count; i += 2)
            {
                var p1 = intersectionPoints[i];
                var p2 = intersectionPoints[i + 1];

                var line = new Line()
                {
                    X1 = p1.X,
                    Y1 = p1.Y,
                    X2 = p2.X,
                    Y2 = p2.Y,
                    Stroke = redBrush,
                    StrokeThickness = 1,
                };

                Slice2DCanvas.Children.Add(line);
            }
        }

        private Transform3D GetSelectedPlaneTransform(bool updateTransformationAmount)
        {
            Transform3D transform;
            string newTransformationAmount;

            var plane = GetSelectedPlane();

            if (TranslateTransformRadioButton.IsChecked ?? false)
            {
                double offset = SlicePlaneSlider.Value / SlicePlaneSlider.Maximum;
                offset = (plane.A * _modelBounds.X + plane.B * _modelBounds.Y + plane.C * _modelBounds.Z) + (offset * (plane.A * _modelBounds.SizeX + plane.B * _modelBounds.SizeY + plane.C * _modelBounds.SizeZ));

                transform = new TranslateTransform3D(plane.A * offset, plane.B * offset, plane.C * offset);

                newTransformationAmount = offset.ToString("N1");
            }
            else if (RotateTransformRadioButton.IsChecked ?? false)
            {
                double angle = ((SlicePlaneSlider.Value / SlicePlaneSlider.Maximum) - 0.5) * 360.0;
                transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(plane.B, plane.C, plane.A), angle));

                newTransformationAmount = angle.ToString("N1");
            }
            else
            {
                transform = null;
                newTransformationAmount = null;
            }

            if (updateTransformationAmount && TransformationAmountTextBlock.Text != newTransformationAmount) // Update only if needed (updating TextBlock is slow)
                TransformationAmountTextBlock.Text = newTransformationAmount;

            return transform;
        }


        private void SetupPlanes()
        {
            // Plane is created by defining plane's normal vector (first 3 numbers) and an offset d (forth number):
            var planes = new Plane[]
            {
                new Plane(0, 0, 1, 0),
                new Plane(1, 0, 0, 0),
                new Plane(0, 1, 0, 0),
            };

            foreach (var plane in planes)
            {
                var radioButton = new RadioButton()
                {
                    Content = string.Format("n:({0}, {1}, {2}) d:{3}", plane.A, plane.B, plane.C, plane.D),
                    GroupName = "Plane",
                    Margin = new Thickness(0, 0, 0, 3),
                    Tag = plane
                };

                radioButton.Checked += delegate (object sender, RoutedEventArgs args)
                {
                    if (!this.IsLoaded)
                        return;

                    SliceScene();
                };

                PlanesPanel.Children.Add(radioButton);

                if (PlanesPanel.Children.Count == 1)
                    radioButton.IsChecked = true;
            }
        }

        private Plane GetSelectedPlane()
        {
            var checkedRadioButton = PlanesPanel.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked ?? false);

            if (checkedRadioButton == null)
                return null;

            var plane = checkedRadioButton.Tag as Plane;

            if (plane == null)
                return null;

            // Clone the plane to preserve the original plane values in case this plane is later transformed
            plane = plane.Clone();
            plane.Normalize();

            return plane;
        }

        private Rect3D GetSelectedModelBoundingBox()
        {
            Rect3D boundingBox;

            if (_originalModel3D != null)
                boundingBox = _originalModel3D.Bounds;
            else if (_originalMesh3D != null)
                boundingBox = _originalMesh3D.Bounds;
            else if (_originalModelVisual3D != null)
                boundingBox = ModelUtils.GetBounds(_originalModelVisual3D);
            else
                boundingBox = Rect3D.Empty;

            return boundingBox;
        }

        private void OnModelTypeChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateScene();
            SliceScene();
        }

        private void OnShow2DSliceCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (Show2DSliceCheckBox.IsChecked ?? false)
            {
                Slice2DBorder.Visibility = Visibility.Visible;
                UpdateSlice2DLines();
            }
            else
            {
                Slice2DBorder.Visibility = Visibility.Collapsed;
                Slice2DCanvas.Children.Clear();
            }
        }

        private void OnUpdateSlicedModels(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            SliceScene();
        }
    }
}
