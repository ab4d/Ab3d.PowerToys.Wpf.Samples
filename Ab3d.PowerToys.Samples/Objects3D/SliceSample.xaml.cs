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
using Ab3d.Utilities;
using Ab3d.Visuals;
using Window = System.Windows.Window;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for SliceSample.xaml
    /// </summary>
    public partial class SliceSample : Page
    {
        // If position's distance from the slice plane is less then planeDistanceEpsilon
        // then consider this position on the plane (use some epsilon value to account for floating point imprecision)
        private const double PlaneDistanceEpsilon = 0.0000001;


        private MeshGeometry3D _originalMesh3D;
        private Model3D _originalModel3D;
        private ModelVisual3D _originalModelVisual3D;

        private Model3D _model3DFor2DSlice;

        private Rect3D _modelBounds;

        private Rect _projectedModelBounds;

        private double _rootModelSize;

        public SliceSample()
        {
            InitializeComponent();

            SetupPlanes();

            UpdateScene();

            SliceScene();
            UpdateSlice2DLines();
        }

        private void UpdateScene()
        {
            _originalModelVisual3D = null;
            _originalModel3D = null;
            _originalMesh3D = null;
            _model3DFor2DSlice = null;

            RootModelVisual3D.Children.Clear();

            WireframeModels1.OriginalModel = null;
            WireframeModels2.OriginalModel = null;
            

            if (Model1RadioButton.IsChecked ?? false)
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
            else if (Model2RadioButton.IsChecked ?? false)
            {
                _originalMesh3D = new Ab3d.Meshes.SphereMesh3D(new Point3D(0, 0, 0), 50, 20).Geometry;
                _modelBounds = _originalMesh3D.Bounds;
            }
            else if (Model3RadioButton.IsChecked ?? false)
            {
                _originalModelVisual3D = new ModelVisual3D();

                _originalModelVisual3D.Children.Add(new Ab3d.Visuals.PyramidVisual3D() { BottomCenterPosition = new Point3D(-280, -60, 0), Size = new Size3D(120, 120, 120), Material = new DiffuseMaterial(Brushes.Green) });
                _originalModelVisual3D.Children.Add(new Ab3d.Visuals.BoxVisual3D() { CenterPosition = new Point3D(-100, 0, 0), Size = new Size3D(120, 120, 60), Material = new DiffuseMaterial(Brushes.Green) });
                _originalModelVisual3D.Children.Add(new Ab3d.Visuals.TubeVisual3D() { BottomCenterPosition = new Point3D(80, -60, 0), InnerRadius = 40, OuterRadius = 60, Height = 120, Material = new DiffuseMaterial(Brushes.Green) });
                _originalModelVisual3D.Children.Add(new Ab3d.Visuals.SphereVisual3D() { CenterPosition = new Point3D(240, 0, 0), Radius = 60, Material = new DiffuseMaterial(Brushes.Green) });

                _modelBounds = new Rect3D();
                foreach (var visual3D in _originalModelVisual3D.Children.OfType<ModelVisual3D>())
                    _modelBounds.Union(visual3D.Content.Bounds);
            }

            _rootModelSize = Math.Sqrt(_modelBounds.SizeX * _modelBounds.SizeX + _modelBounds.SizeY * _modelBounds.SizeY + _modelBounds.SizeZ * _modelBounds.SizeZ);
            Camera1.Distance = 2 * _rootModelSize;
        }

        private void SliceScene()
        {
            _projectedModelBounds = Rect.Empty; // Reset the bounds of the model that is projected to the plane (used by showing 2D sliced lines)

            var plane = GetSelectedPlane();
            var planeTransform = GetSelectedPlaneTransform(updateTransformationAmount: true);

            // Transform the plane that will be used to slice the model
            plane.Transform(planeTransform);


            double slicedModelsSeparation = _rootModelSize * 0.1;
            var frontModelTransform = new TranslateTransform3D(plane.A * slicedModelsSeparation, plane.B * slicedModelsSeparation, plane.C * slicedModelsSeparation);
            var backModelTransform = new TranslateTransform3D(plane.A * -slicedModelsSeparation, plane.B * -slicedModelsSeparation, plane.C * -slicedModelsSeparation); ;


            // When _originalModelVisual3D is defined we demonstrate the use of SliceModelVisual3D method.
            if (_originalModelVisual3D != null)
            {
                ModelVisual3D frontModelVisual3D, backModelVisual3D;

                // Each SliceXXXX method returns two new object:
                // one that lies in front of the plane (as defined with plane's normal vector (A, B, C value in Plane object),
                // one that lines in the back of the plane.
                // 
                // If we only need the front object, we can use the SliceXXXX method that only takes 1 or 2 parameters and return the front object - for example:
                //frontModelVisual3D = _plane.SliceModelVisual3D(_originalModelVisual3D, transform);
                //backModelVisual3D = null;

                plane.SliceModelVisual3D(_originalModelVisual3D, out frontModelVisual3D, out backModelVisual3D);


                RootModelVisual3D.Children.Clear();

                if (frontModelVisual3D != null)
                {
                    foreach (var modelVisual3D in frontModelVisual3D.Children.OfType<ModelVisual3D>())
                    {
                        if (modelVisual3D.Content != null)
                            Ab3d.Utilities.ModelUtils.ChangeBackMaterial(modelVisual3D.Content, new DiffuseMaterial(Brushes.Red));
                    }

                    frontModelVisual3D.Transform = frontModelTransform;
                    RootModelVisual3D.Children.Add(frontModelVisual3D);
                }

                if (backModelVisual3D != null)
                {
                    foreach (var modelVisual3D in backModelVisual3D.Children.OfType<ModelVisual3D>())
                    {
                        if (modelVisual3D.Content != null)
                            Ab3d.Utilities.ModelUtils.ChangeBackMaterial(modelVisual3D.Content, new DiffuseMaterial(Brushes.Red));
                    }

                    backModelVisual3D.Transform = backModelTransform;
                    RootModelVisual3D.Children.Add(backModelVisual3D);
                }

                return;
            }


            Model3D frontModel3D, backModel3D;

            // When _originalModel3D is defined we demonstrate the use of SliceModel3D method (can slice GeometryModel3D or Model3DGroup objects)
            if (_originalModel3D != null)
            {
                plane.SliceModel3D(_originalModel3D, frontModel3D: out frontModel3D, backModel3D: out backModel3D);

                // To get only front objects, we can use the following method:
                //frontModel3D = _plane.SliceModel3D(_originalModel3D, parentTransform: transform);
                //backModel3D = null;
            }
            // When _originalModel3D is defined we demonstrate the use of SliceMeshGeometry3D method.
            else if (_originalMesh3D != null)
            {
                MeshGeometry3D frontMesh, backMash;
                plane.SliceMeshGeometry3D(_originalMesh3D, frontMeshGeometry3D: out frontMesh, backMeshGeometry3D: out backMash);

                // To get only front objects, we can use the following method:
                //frontMesh = plane.SliceMeshGeometry3D(originalMesh3D, transform: transform);
                //backMash = null;

                var frontGeometryModel3D = new GeometryModel3D(frontMesh, new DiffuseMaterial(Brushes.Gold));
                frontModel3D = frontGeometryModel3D;
                
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

            // Set back material to red so we will easily see the inner parts of the model
            if (frontModel3D != null)
                Ab3d.Utilities.ModelUtils.ChangeBackMaterial(frontModel3D, new DiffuseMaterial(Brushes.Red));

            if (backModel3D != null)
                Ab3d.Utilities.ModelUtils.ChangeBackMaterial(backModel3D, new DiffuseMaterial(Brushes.Red));


            // When the sliced 3D models is not ModelVisual3D, we show both sliced parts in WireframeVisual3D object (with all 3D lines)
            WireframeModels1.OriginalModel = frontModel3D;
            WireframeModels1.Transform = frontModelTransform;

            WireframeModels2.OriginalModel = backModel3D;
            WireframeModels2.Transform = backModelTransform;
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

                newTransformationAmount = string.Format("{0:0}", offset);
            }
            else if (RotateTransformRadioButton.IsChecked ?? false)
            {
                double angle = ((SlicePlaneSlider.Value / SlicePlaneSlider.Maximum) - 0.5) * 360.0;
                transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(plane.B, plane.C, plane.A), angle));

                newTransformationAmount = string.Format("{0:0}", angle);
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
            // Plane is created with defining plane's normal vector (first 3 numbers) and an offset d (forth number):
            var planes = new Plane[]
            {
                new Plane(1, 0, 0, 0),
                new Plane(0, 1, 0, 0),
                new Plane(0, 0, 1, 0),
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
                    UpdateSlice2DLines();
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

        private void OnTransformSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            SliceScene();
            UpdateSlice2DLines();
        }

        private void OnModelTypeChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateScene();
            SliceScene();
            UpdateSlice2DLines();
        }

        //private void Show2DSliceButton_OnClick(object sender, RoutedEventArgs e)
        //{
        //}

        private void UpdateSlice2DLines()
        {
            // NOTE:
            // If you want to project all 3D points to a 2D plane you can use the following method:
            // Ab3d.Utilities.MeshUtils.Project3DPointsTo2DPlane(...)

            // Here we will create 2D lines from only the sliced part of the 3D model


            Slice2DCanvas.Children.Clear();

            if (!Show2DSliceCheckBox.IsChecked ?? false)
                return;


            Model3D originalModel3D = GetModel3DFor2DSlice();

            if (originalModel3D == null)
                return;


            var slicePlane = GetSelectedPlane();

            var planeTransform = GetSelectedPlaneTransform(updateTransformationAmount: true);
            slicePlane.Transform(planeTransform);


            var slicedModel3D = slicePlane.SliceModel3D(originalModel3D, parentTransform: null);






            var upDirection = Ab3d.Utilities.CameraUtils.CalculateUpDirection(slicePlane.Normal);
            var planeWidthVector = Vector3D.CrossProduct(upDirection, slicePlane.Normal);


            if (_projectedModelBounds.IsEmpty)
            {
                var boundPoints = originalModel3D.Bounds.GetCorners();
                slicePlane.Project3DPointsToPlane(boundPoints, planeWidthVector, out _projectedModelBounds);
            }


            var pointCollection = new PointCollection();

            Ab3d.Utilities.ModelIterator.IterateGeometryModel3DObjects(
               model3D: slicedModel3D, 
               parentTransform3D: null, 
               callback: delegate(GeometryModel3D geometryModel3D, Transform3D parentTransform3D)
               {
                   var mesh = geometryModel3D.Geometry as MeshGeometry3D;

                   if (mesh == null)
                       return;


                   pointCollection.Clear();

                   var positions = mesh.Positions;
                   var triangleIndices = mesh.TriangleIndices;

                   var totalTransform3D = parentTransform3D;
                   if (geometryModel3D.Transform != null && !geometryModel3D.Transform.Value.IsIdentity)
                       totalTransform3D = Ab3d.Utilities.TransformationsHelper.CombineTransform3D(totalTransform3D, geometryModel3D.Transform);

                   bool useTransform = totalTransform3D != null && !totalTransform3D.Value.IsIdentity;

                   // Go through each triangle and check if any triangle edge lies on the slice plane
                   for (int i = 0; i < triangleIndices.Count; i += 3)
                   {
                       // Get triangle positions
                       var p1 = positions[triangleIndices[i]];
                       var p2 = positions[triangleIndices[i + 1]];
                       var p3 = positions[triangleIndices[i + 2]];

                       if (useTransform)
                       {
                           p1 = totalTransform3D.Transform(p1);
                           p2 = totalTransform3D.Transform(p2);
                           p3 = totalTransform3D.Transform(p3);
                       }


                       // Get distances of each position from the plane
                       var d1 = slicePlane.GetDistance(p1);
                       var d2 = slicePlane.GetDistance(p2);
                       var d3 = slicePlane.GetDistance(p3);

                       // If distance is almost zero, then consider this position on the slice plane
                       if (d1 <= PlaneDistanceEpsilon || d2 <= PlaneDistanceEpsilon || d3 <= PlaneDistanceEpsilon)
                       {
                           if (d1 <= PlaneDistanceEpsilon && d2 <= PlaneDistanceEpsilon)
                           {
                               // Convert 3D position to a 2D position on the slice plane
                               var planePos1 = slicePlane.Project3DPointToPlane(p1, planeWidthVector);
                               var planePos2 = slicePlane.Project3DPointToPlane(p2, planeWidthVector);

                               pointCollection.Add(planePos1);
                               pointCollection.Add(planePos2);
                           }

                           if (d2 <= PlaneDistanceEpsilon && d3 <= PlaneDistanceEpsilon)
                           {
                               // Convert 3D position to a 2D position on the slice plane
                               var planePos1 = slicePlane.Project3DPointToPlane(p2, planeWidthVector);
                               var planePos2 = slicePlane.Project3DPointToPlane(p3, planeWidthVector);

                               pointCollection.Add(planePos1);
                               pointCollection.Add(planePos2);
                           }

                           if (d3 <= PlaneDistanceEpsilon && d1 <= PlaneDistanceEpsilon)
                           {
                               // Convert 3D position to a 2D position on the slice plane
                               var planePos1 = slicePlane.Project3DPointToPlane(p3, planeWidthVector);
                               var planePos2 = slicePlane.Project3DPointToPlane(p1, planeWidthVector);

                               pointCollection.Add(planePos1);
                               pointCollection.Add(planePos2);
                           }
                       }
                   }

                   if (pointCollection.Count > 0)
                   {
                       var lineColor = Ab3d.Utilities.ModelUtils.GetMaterialDiffuseColor(geometryModel3D.Material, defaultColor: Colors.Black);
                       var lineBrush = new SolidColorBrush(lineColor);
                       lineBrush.Freeze();

                       for (int i = 0; i < pointCollection.Count; i += 2)
                       {
                           var line = new Line()
                           {
                               X1     = pointCollection[i].X,
                               Y1     = pointCollection[i].Y,
                               X2     = pointCollection[i + 1].X,
                               Y2     = pointCollection[i + 1].Y,
                               Stroke = lineBrush,
                               StrokeThickness = 1
                           };

                           Slice2DCanvas.Children.Add(line);
                       }

                   }
               });



            double resultWindowSize = Math.Min(Slice2DCanvas.Width, Slice2DCanvas.Height);

            double scaleFactor = Math.Max(_projectedModelBounds.Width, _projectedModelBounds.Height);
            scaleFactor = (resultWindowSize * 0.8) / scaleFactor; // 0.8 is used to add some margin around lines

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(scaleFactor, scaleFactor));

            transformGroup.Children.Add(new TranslateTransform(resultWindowSize / 2 - (_projectedModelBounds.X + _projectedModelBounds.Width / 2) * scaleFactor,
                                                               resultWindowSize / 2 - (_projectedModelBounds.Y + _projectedModelBounds.Height / 2) * scaleFactor));

            Slice2DCanvas.RenderTransform = transformGroup;
        }

        // This sample demonstrates multiple ways to create slice - from Visual3D, Model3D and MeshGeometry3D
        // To get 2D slice, we require Model3D.
        // This method provides that
        private Model3D GetModel3DFor2DSlice()
        {
            Model3D originalModel3D = _model3DFor2DSlice;

            if (originalModel3D == null)
            {
                if (_originalModel3D != null)
                {
                    originalModel3D = _originalModel3D;
                }
                else if (_originalMesh3D != null)
                {
                    // In case _originalMesh3D is used to test creating slice from MeshGeometry3D, we need to create a new GeometryModel3D
                    originalModel3D = new GeometryModel3D(_originalMesh3D, null);
                }
                else if (_originalModelVisual3D != null)
                {
                    // In case _originalModelVisual3D is used to test SliceModelVisual3D method we need to 
                    // convert children in ModelVisual3D to Model3DGroup (this is not a generic code - it works only for this sample)
                    var model3DGroup = new Model3DGroup();

                    foreach (var oneChild in _originalModelVisual3D.Children.OfType<ModelVisual3D>())
                    {
                        var childModel3DGroup = new Model3DGroup();
                        childModel3DGroup.Transform = oneChild.Transform;
                        childModel3DGroup.Children.Add(oneChild.Content);

                        model3DGroup.Children.Add(childModel3DGroup);
                    }

                    originalModel3D = model3DGroup;
                }

                _model3DFor2DSlice = originalModel3D; // Save that for the next time
            }

            return originalModel3D;
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
    }
}
