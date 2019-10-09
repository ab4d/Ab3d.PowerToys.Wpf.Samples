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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using Ab3d.Common.Cameras;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for ObjectInspector.xaml
    /// </summary>
    public partial class ObjectInspector : Page
    {
        private MeshGeometry3D _rootMesh;
        private GeometryModel3D _rootModel;

        private Ab3d.Utilities.EventManager3D _eventsManager;

        private int _selectedTriangleIndex;
        private bool _isSelectionChangedInternally;

        private double _selectedTriangleOffset = 0.5;

        private bool _isShowing3DLines;



        public ObjectInspector()
        {
            InitializeComponent();

            _eventsManager = new Ab3d.Utilities.EventManager3D(MainViewport);
            _selectedTriangleIndex = -1;

            Camera1.CameraChanged += delegate (object sender, CameraChangedRoutedEventArgs args)
            {
                // Update the positions of the TextBlocks on every camera change
                UpdateSelectedTriangleIndexes();
            };

            MainViewport.SizeChanged += delegate (object sender, SizeChangedEventArgs args)
            {
                // Update the positions of the TextBlocks when the size of Viewport3D is changed
                UpdateSelectedTriangleIndexes();
            };

            this.Loaded += new RoutedEventHandler(ObjectInspector_Loaded);
        }

        private void UpdateSelectedTriangleIndexes()
        {
            if (_selectedTriangleIndex != -1 && (ShowSelectedIndexesCheckBox.IsChecked ?? false))
            {
                int[]   indexes;
                Point[] screenPositions;

                CalculateTextBlockPositions(_selectedTriangleIndex * 3, out screenPositions, out indexes);
                UpdatePositionIndexTextBlockPositions(screenPositions);
            }
        }

        void ObjectInspector_Loaded(object sender, RoutedEventArgs e)
        {
            RecreateAll();
        }

        private void ObjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            RecreateAll();
        }

        private void RecreateAll()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                CreateRootModel();
                ShowObjectInfo();
                UpdateMaterial();
                UpdateTrianglesAndNormals();

                SelectTriangle(-1); // Remove selection
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
                    
        private void CreateRootModel()
        {
            bool show3DLine = false;

            switch (ObjectComboBox.SelectedIndex)
            { 
                case 0:
                    Ab3d.Meshes.SphereMesh3D sphere = new Ab3d.Meshes.SphereMesh3D(new Point3D(0, 0, 0), 80, 10);
                    _rootMesh = sphere.Geometry;
                    break;

                case 1:
                    Ab3d.Meshes.SphereMesh3D sphere2 = new Ab3d.Meshes.SphereMesh3D(new Point3D(0, 0, 0), 80, 5);
                    _rootMesh = sphere2.Geometry;
                    break;

                case 2:
                    // NOTE: Here we create an optimized version of sphere but it does not have texture coordinates
                    // If we do not need texture coordinates (do not need to show texture), than we can slightly simplify the sphere model.
                    // When we also need to create texture coordinates, the top most position that represent the same point in space is defined multiple times
                    // - each time with slightly different texture coordinate (so that the whole top line on the texture is shown around the top sphere position).
                    // Also there are x + 1 position around the sphere (in one row) - one additional position is added that the first and last position in the "row" are in the same space position,
                    // but the first position has x texture coordinate 0, the last position has x texture coordinate 1 - this shows the whole texture nicely around the sphere.
                    // If texture coordinates are not needed, than we do not need to define more than one upper and bottom positions, 
                    // and also do not need to add another position to the row.
                    // Optimized sphere can be created only with SphereMesh3D object (not with SphereVisual3D)
                    Ab3d.Meshes.SphereMesh3D sphere3 = new Ab3d.Meshes.SphereMesh3D(new Point3D(0, 0, 0), 80, 12, false); // false: generateTextureCoordinates
                    _rootMesh = sphere3.Geometry;
                    break;

                case 3:
                    Ab3d.Meshes.BoxMesh3D box = new Ab3d.Meshes.BoxMesh3D(new Point3D(0, 0, 0), new Size3D(130, 60, 100), 1, 1, 1);
                    _rootMesh = box.Geometry;
                    break;

                case 4:
                    Ab3d.Meshes.BoxMesh3D box2 = new Ab3d.Meshes.BoxMesh3D(new Point3D(0, 0, 0), new Size3D(130, 60, 100), 4, 4, 4);
                    _rootMesh = box2.Geometry;
                    break;

                case 5:
                    Ab3d.Meshes.CylinderMesh3D cylinder = new Ab3d.Meshes.CylinderMesh3D(new Point3D(0, -50, 0), 60, 100, 12, true);
                    _rootMesh = cylinder.Geometry;
                    break;

                case 6:
                    Ab3d.Meshes.ConeMesh3D cone = new Ab3d.Meshes.ConeMesh3D(new Point3D(0, -50, 0), 30, 60, 100, 12, true);
                    _rootMesh = cone.Geometry;
                    break;

                case 7:
                    Ab3d.Meshes.ConeMesh3D cone2 = new Ab3d.Meshes.ConeMesh3D(new Point3D(0, -50, 0), 30, 60, 100, 6, false);
                    _rootMesh = cone2.Geometry;
                    break;

                case 8:
                    var readerObj = new Ab3d.ReaderObj();
                    var teapotModel = readerObj.ReadModel3D(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\ObjFiles\Teapot.obj")) as GeometryModel3D;

                    if (teapotModel == null)
                        return;

                    // Get the teapot MeshGeometry3D
                    _rootMesh = (MeshGeometry3D)teapotModel.Geometry;

                    break;

                case 9:
                    var line = new Ab3d.Visuals.LineVisual3D()
                    {
                        StartPosition = new Point3D(0, 0, 0),
                        EndPosition = new Point3D(100, 0, 0),
                        LineColor = Colors.Silver,
                        LineThickness = 20
                    };

                    Show3DLines(line);
                    show3DLine = true;
                    break;

                case 10:
                    var polyLineVisual3D = new Ab3d.Visuals.PolyLineVisual3D()
                    {
                        Positions = new Point3DCollection(new Point3D[] { new Point3D(-75, 50, 0), new Point3D(-25, 0, 0), new Point3D(25, 50, 0), new Point3D(75, 0, 0) }),
                        LineThickness = 20
                    };

                    Show3DLines(polyLineVisual3D);
                    show3DLine = true;
                    break;

                case 11:
                    // This is the same line as in the previous sample (PolyLineVisual3D), but this time 
                    // it is created as diconnected list of lines - note that this requires that the positions are duplicated.
                    var multiLineVisual3D = new Ab3d.Visuals.MultiLineVisual3D()
                    {
                        Positions = new Point3DCollection(new Point3D[] { new Point3D(-75, 50, 0), new Point3D(-25, 0, 0),
                                                                          new Point3D(-25, 0, 0), new Point3D(25, 50, 0),
                                                                          new Point3D(25, 50, 0), new Point3D(75, 0, 0) }),
                        LineThickness = 20
                    };

                    Show3DLines(multiLineVisual3D);
                    show3DLine = true;
                    break;

                default:
                    _rootMesh = null;
                    break;
            }

            // If we were looking at 3D lines before and now we are looking an standard 3D models, 
            // we adjust the camera back to the side view (from direct front view)
            if (_isShowing3DLines && !show3DLine)
            {
                Camera1.Heading = 30;
                Camera1.Attitude = -20;
                Camera1.Distance = 300;
            }

            _isShowing3DLines = show3DLine;


            _rootModel = new GeometryModel3D();
            _rootModel.Geometry = _rootMesh;
            _rootModel.Material = new DiffuseMaterial(Brushes.DarkBlue);

            ObjectModelVisual.Content = _rootModel;

            Ab3d.Utilities.ModelEventSource3D modelEventSource = new Ab3d.Utilities.ModelEventSource3D();
            modelEventSource.TargetModel3D = _rootModel;
            modelEventSource.MouseClick += new Ab3d.Common.EventManager3D.MouseButton3DEventHandler(modelEventSource_MouseClick);

            _eventsManager.ResetEventSources3D();
            _eventsManager.RegisterEventSource3D(modelEventSource);

            MainViewport.Cursor = Cursors.Hand;
        }

        private void Show3DLines(BaseLineVisual3D lineVisual3D)
        {
            // To better view how 3D line is created from triangles,
            // we adjust the camera to direct front view
            Camera1.Heading = 0;
            Camera1.Attitude = 0;
            Camera1.Distance = 500;
            Camera1.Offset = new Vector3D(0, 0, 0);
            Camera1.Refresh();

            // This sample is a detailed inspector for MeshGeometry3D objects.
            // To show MeshGeometry3D from 3D lines, we need to do a little trick:
            // 1) We need to create a new Viewport3D, sets its size (very important because the Viewport3D will not be shown and size is needed to get PerspectiveMatrix)
            // 2) In the new Viewport3D set the camera to the same camera as out main camera.
            // 3) Add line to Viewport3D.
            // 4) Force line geometry generation with Ab3d.Utilities.LinesUpdater (register Viewport3D and then call Refresh).
            // 5) After that we can access the MeshGeometry3D for the 3D line.

            var mainViewportCamera = (PerspectiveCamera)MainViewport.Camera;
            var perspectiveCamera = new PerspectiveCamera()
            {
                LookDirection = mainViewportCamera.LookDirection,
                Position = mainViewportCamera.Position,
                UpDirection = mainViewportCamera.UpDirection,
                FieldOfView = mainViewportCamera.FieldOfView
            };

            var viewport3D = new Viewport3D();
            viewport3D.Camera = perspectiveCamera;
            viewport3D.Width = MainViewport.ActualWidth;
            viewport3D.Height = MainViewport.ActualHeight;


            viewport3D.Children.Add(lineVisual3D);

            //var geometryModel3D = line.Content as GeometryModel3D;
            var geometryModel3D = lineVisual3D.Content as GeometryModel3D;

            if (geometryModel3D != null)
                _rootMesh = (MeshGeometry3D)geometryModel3D.Geometry;
        }

        void modelEventSource_MouseClick(object sender, Ab3d.Common.EventManager3D.MouseButton3DEventArgs e)
        {
            if (!ReferenceEquals(e.RayHitResult.MeshHit, _rootMesh)) // UH: we did not hit the mesh of the current object
                return;

            bool isFound = false;
            int index1 = e.RayHitResult.VertexIndex1;
            int index2 = e.RayHitResult.VertexIndex2;
            int index3 = e.RayHitResult.VertexIndex3;

            int j = 0;
            for (int i = 0; i < _rootMesh.TriangleIndices.Count; i += 3)
            {
                if (_rootMesh.TriangleIndices[i] == index1 &&
                    _rootMesh.TriangleIndices[i + 1] == index2 &&
                    _rootMesh.TriangleIndices[i + 2] == index3)
                {
                    isFound = true;
                    break;
                }

                j++;
            }

            
            if (isFound)
                SelectTriangle(j);
            else
                SelectTriangle(-1);
        }

        private void SelectTriangle(int triangleIndex)
        {
            if (_selectedTriangleIndex == triangleIndex)
                return;

            _isSelectionChangedInternally = true;

            OverlayCanvas.Children.Clear();

            if (triangleIndex >= 0)
            {
                OverviewListBox.SelectedIndex = triangleIndex;
                OverviewListBox.ScrollIntoView(OverviewListBox.Items[triangleIndex]);

                OverviewListBox.Focus(); // Without calling focus the blue selection is not visible

                int i = triangleIndex * 3;
                Point3D p1, p2, p3;

                int index1 = _rootMesh.TriangleIndices[i];
                int index2 = _rootMesh.TriangleIndices[i + 1];
                int index3 = _rootMesh.TriangleIndices[i + 2];

                p1 = _rootMesh.Positions[index1];
                p2 = _rootMesh.Positions[index2];
                p3 = _rootMesh.Positions[index3];

                if (_rootMesh.Normals != null && _rootMesh.Normals.Count == _rootMesh.Positions.Count)
                {
                    // we can offset the Polyline so it will not be fighting for Z-depth with the object
                    // This way the selection lines will be drawn on top of the selected object
                    Vector3D n1 = _rootMesh.Normals[index1];
                    n1.Normalize();

                    Vector3D n2 = _rootMesh.Normals[index2];
                    n2.Normalize();

                    Vector3D n3 = _rootMesh.Normals[index2];
                    n3.Normalize();

                    p1 += n1 * _selectedTriangleOffset;
                    p2 += n2 * _selectedTriangleOffset;
                    p3 += n3 * _selectedTriangleOffset;
                }

                SelectedTrianglePolyline.Positions = new Point3DCollection(new Point3D[] { p1, p2, p3 });
                SelectedTrianglePolyline.IsVisible = true;


                if (ShowSelectedIndexesCheckBox.IsChecked ?? false)
                    AddPositionIndexTextBlocks(i);
            }
            else
            {
                OverviewListBox.SelectedIndex = -1;
                SelectedTrianglePolyline.IsVisible = false;
            }

            _isSelectionChangedInternally = false;

            _selectedTriangleIndex = triangleIndex;
        }

        private void AddPositionIndexTextBlocks(int startTriangleIndiceIndex)
        {
            int[] indexes;
            Point[] screenPositions;

            CalculateTextBlockPositions(startTriangleIndiceIndex, out screenPositions, out indexes);

            // Add TextBlocks
            for (int i = 0; i < indexes.Length; i++)
                AddPositionIndexTextBlock(screenPositions[i], indexes[i]);
        }

        private void CalculateTextBlockPositions(int startTriangleIndiceIndex, out Point[] screenPositions, out int[] indexes)
        {
            indexes = new int[3];

            indexes[0] = _rootMesh.TriangleIndices[startTriangleIndiceIndex];
            indexes[1] = _rootMesh.TriangleIndices[startTriangleIndiceIndex + 1];
            indexes[2] = _rootMesh.TriangleIndices[startTriangleIndiceIndex + 2];

            double sumX = 0;
            double sumY = 0;
            screenPositions = new Point[indexes.Length];

            for (int i = 0; i < indexes.Length; i++)
            {
                Point3D onePosition = _rootMesh.Positions[indexes[i]];

                Point screenPosition = Camera1.Point3DTo2D(onePosition);

                sumX += screenPosition.X;
                sumY += screenPosition.Y;

                screenPositions[i] = screenPosition;
            }

            Point centerScreenPosition = new Point(sumX / indexes.Length, sumY / indexes.Length);

            // Get direction vectors
            for (int i = 0; i < indexes.Length; i++)
            {
                Vector vector = screenPositions[i] - centerScreenPosition;
                vector.Normalize();

                screenPositions[i] += vector * 10; // move text center 10 pixels away from the vertex position
            }
        }

        private void UpdatePositionIndexTextBlockPositions(Point[] screenPositions)
        {
            int i = 0;

            foreach (var control in OverlayCanvas.Children)
            {
                if (!(control is TextBlock))
                    continue;

                TextBlock textBlock = (TextBlock)control;

                Canvas.SetLeft(textBlock, screenPositions[i].X - textBlock.DesiredSize.Width / 2);
                Canvas.SetTop(textBlock, screenPositions[i].Y - textBlock.DesiredSize.Height / 2);

                i++;
            }
        }

        private void AddPositionIndexTextBlock(Point textCenterPosition, int index)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.FontSize = 9;
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.Foreground = Brushes.Red;
            textBlock.Text = index.ToString();

            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Canvas.SetLeft(textBlock, textCenterPosition.X - textBlock.DesiredSize.Width / 2);
            Canvas.SetTop(textBlock, textCenterPosition.Y - textBlock.DesiredSize.Height / 2);

            OverlayCanvas.Children.Add(textBlock);
        }

        private void ShowObjectInfo()
        {
            OverviewListBox.Items.Clear();

            if (_rootMesh == null)
            {
                TriangleIndicesTextBox.Text = "";
                PositionsTextBox.Text = "";
                NormalsTextBox.Text = "";
                TextureCoordinatesTextBox.Text = "";
                XamlTextBox.Text = "";
            }
            else
            {
                StringBuilder sb;
                OverviewListBox.BeginInit();

                sb = new StringBuilder();
                for (int i = 0; i < _rootMesh.Positions.Count; i++)
                    sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0,3}: {1:0.#}\r\n", i, _rootMesh.Positions[i]);

                PositionsTextBox.Text = sb.ToString();


                sb = new StringBuilder();
                string oneTriangleInfo;
                int j = 0;
                for (int i = 0; i < _rootMesh.TriangleIndices.Count; i += 3)
                {
                    try
                    {
                        oneTriangleInfo = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "{0,3} ({1,2}): {2} {3} {4} ({5:0.#}) ({6:0.#}) ({7:0.#})",
                            j, i,
                            _rootMesh.TriangleIndices[i], _rootMesh.TriangleIndices[i + 1], _rootMesh.TriangleIndices[i + 2],
                            _rootMesh.Positions[_rootMesh.TriangleIndices[i]], _rootMesh.Positions[_rootMesh.TriangleIndices[i + 1]], _rootMesh.Positions[_rootMesh.TriangleIndices[i + 2]]);
                    }
                    catch (Exception ex)
                    {
                        oneTriangleInfo = "Error: " + ex.Message;
                    }

                    OverviewListBox.Items.Add(oneTriangleInfo);

                    sb.AppendLine(oneTriangleInfo);
                    
                    j++;
                }

                TriangleIndicesTextBox.Text = sb.ToString();


                if (_rootMesh.Normals != null && _rootMesh.Normals.Count > 0)
                {
                    sb = new StringBuilder();
                    for (int i = 0; i < _rootMesh.Normals.Count; i++)
                        sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0,3}: {1:0.###}\r\n", i, _rootMesh.Normals[i]);

                    NormalsTextBox.Text = sb.ToString();
                }
                else
                {
                    NormalsTextBox.Text = "No Normals defined";
                }


                if (_rootMesh.TextureCoordinates != null && _rootMesh.TextureCoordinates.Count > 0)
                {
                    sb = new StringBuilder();
                    for (int i = 0; i < _rootMesh.TextureCoordinates.Count; i++)
                        sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0,3}: {1:0.###}\r\n", i, _rootMesh.TextureCoordinates[i]);

                    TextureCoordinatesTextBox.Text = sb.ToString();
                }
                else
                {
                    TextureCoordinatesTextBox.Text = "No TextureCoordinates defined";
                }


                using (System.IO.StringWriter stringWriter = new System.IO.StringWriter())
                {
                    System.Xml.XmlWriterSettings settings;
                    settings = new System.Xml.XmlWriterSettings();
                    settings.Indent = true;

                    using (System.Xml.XmlWriter xmlWriter = System.Xml.XmlWriter.Create(stringWriter, settings))
                    {
                        System.Windows.Markup.XamlWriter.Save(_rootModel, xmlWriter);
                    }

                    XamlTextBox.Text = stringWriter.ToString();
                }

                OverviewListBox.EndInit();
            }
        }


        private void UpdateMaterial()
        {
            DiffuseMaterial material;
            ImageBrush imageBrush;

            if (_rootModel == null)
                return;


            material = new DiffuseMaterial();

            if (TextureMaterialCheckBox.IsChecked ?? false)
            {
                imageBrush = new ImageBrush();
                imageBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/10x10-texture.png"));

                if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                    imageBrush.Opacity = 0.8;

                material.Brush = imageBrush;
            }
            else
            {
                material.Brush = new SolidColorBrush(Color.FromRgb(39, 126, 147));

                if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                    material.Brush.Opacity = 0.8;
            }

            _rootModel.Material = material;

            if ((SemiTransparentMaterialCheckBox.IsChecked ?? false) || (TextureMaterialCheckBox.IsChecked ?? false))
                _rootModel.BackMaterial = material;
        }

        private void UpdateTrianglesAndNormals()
        {
            GeometryModel3D wireframeModel;
            GeometryModel3D normalsModel;
            
            if (_rootModel == null)
                return;


            TrianglesGroup.Children.Clear();

            if (ShowTrianglesCheckBox.IsChecked ?? false)
            {
                wireframeModel = Ab3d.Models.WireframeFactory.CreateWireframe((MeshGeometry3D)_rootModel.Geometry, 2, Color.FromRgb(47, 72, 57), MainViewport);

                if (SemiTransparentMaterialCheckBox.IsChecked ?? false)
                    wireframeModel.BackMaterial = wireframeModel.Material;

                TrianglesGroup.Children.Add(wireframeModel);
            }


            NormalsGroup.Children.Clear();

            if (ShowNormalsCheckBox.IsChecked ?? false)
            {
                normalsModel = Ab3d.Models.WireframeFactory.CreateNormals((MeshGeometry3D)_rootModel.Geometry, 10, 2, Color.FromRgb(179, 140, 57), true, MainViewport);
                NormalsGroup.Children.Add(normalsModel);
            }
        }

        private void OnMaterialSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateMaterial();
        }

        private void OnWireSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateTrianglesAndNormals();
        }

        private void OverviewListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded || _isSelectionChangedInternally)
                return;

            SelectTriangle(OverviewListBox.SelectedIndex);
        }

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            SelectTriangle(-1);
        }

        private void ShowSelectedIndexesCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            if (_selectedTriangleIndex != -1)
            {
                if (ShowSelectedIndexesCheckBox.IsChecked ?? false)
                    AddPositionIndexTextBlocks(_selectedTriangleIndex * 3);
                else
                    OverlayCanvas.Children.Clear();
            }
        }
    }
}
