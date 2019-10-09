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
using Ab3d.Cameras;
using Ab3d.Common;
using Ab3d.Common.Cameras;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.SceneEditor
{
    // This is a simple scene editor that shows a simple way to create 3D boxes and spheres.
    // It also supports moving created objects around and moving the individual positions of the 3D objects.
    //
    // This is not a final version of the sample.
    // In the future there will be support for importing and exporting any 3D model from 3D files
    // and better editing support with support for rotation and scaling of the objects.


    /// <summary>
    /// Interaction logic for SceneEditor.xaml
    /// </summary>
    public partial class SceneEditor : Page
    {
        private const double HALF_MOUSE_LINE_LENGTH = 500;

        private const double NEW_OBJECT_HEIGHT = 1;

        private enum ObjectCreationStates
        {
            None = 0,
            SelectStartPoint,
            SelectBaseSize,
            SelectHeight
        }

        private enum EditorModes
        {
            None = 0,
            Create,
            Edit
        }

        private EditorModes _currentEditorMode;

        private ObjectCreationStates _currentObjectCreationState;


        private Material _standardMaterial;
        private Material _standardBackMaterial;

        private Ab3d.Visuals.LineVisual3D _horizontalMouseLine;
        private Ab3d.Visuals.LineVisual3D _verticalMouseLine;

        private Point3D _lastIntersectionPoint;


        private Point3D _objectStartPosition;
        private Point _selectHeightStartMousePosition;

        private ModelVisual3D _selectedModelVisual3D;
        private ModelVisual3D _highlightedModelVisual3D;

        private CornerWireBoxVisual3D _selectionWireBoxVisual3D;
        private CornerWireBoxVisual3D _highlightWireBoxVisual3D;

        private ScaleTransform3D _selectedObjectScaleTransform;
        private TranslateTransform3D _selectedObjectTranslateTransform3D;

        private double _screenTo3DFactor;

        private Point3D _startMovePosition;
        private Point3D _startMoveOffset;

        private RayMeshGeometry3DHitTestResult _lastHitTestResult;


        public SceneEditor()
        {
            InitializeComponent();

            _currentEditorMode = EditorModes.Create;

            var materialGroup = new MaterialGroup();
            materialGroup.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(200, 36, 117, 137)))); // #247589
            materialGroup.Children.Add(new SpecularMaterial(Brushes.White, 16));
            materialGroup.Freeze();
            _standardMaterial = materialGroup;

            _standardBackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(36, 117, 137)));
            _standardBackMaterial.Freeze();


            UpdateEditorModeUI();

            SetupOverlayViewport3D();


            // We need to synchronize the Lights in OverlayViewport with the camera in the MainViewport
            Camera1.CameraChanged += delegate (object s, CameraChangedRoutedEventArgs args)
            {
                // When camera is changed, we need to update the 2D positions that are get from 3D points
                OverlayCanvas.UpdateScreenPositions();
            };

            MainViewport.SizeChanged += delegate(object sender, SizeChangedEventArgs args)
            {
                // When MainViewport's size is changed, we need to update the 2D positions that are get from 3D points
                OverlayCanvas.UpdateScreenPositions();
            };


            this.Loaded += SceneEditor_Loaded;
        }

        private void SetupOverlayViewport3D()
        {
            // Setup event handlers on ModelMoverVisual3D
            OverlayViewport.ModelMoveStarted = () => 
            {
                if (OverlayCanvas.GetSelectedScreenPositionsCount() > 0)
                {
                    _startMovePosition = OverlayCanvas.GetCenterWorldPosition(p => p.IsSelected);
                }
                else if (_selectedModelVisual3D != null)
                {
                    var selectedModelBounds = ModelUtils.GetBounds(_selectedModelVisual3D, _selectedModelVisual3D.Transform, checkOnlyBounds: true);
                    _startMovePosition = selectedModelBounds.GetCenterPosition();

                    if (_selectedObjectTranslateTransform3D != null)
                        _startMoveOffset = new Point3D(_selectedObjectTranslateTransform3D.OffsetX, _selectedObjectTranslateTransform3D.OffsetY, _selectedObjectTranslateTransform3D.OffsetZ);
                    else
                        _startMoveOffset = _startMovePosition;
                }
            };

            OverlayViewport.ModelMoved = delegate(Vector3D moveVector3D)
            {
                var newPosition = _startMovePosition + moveVector3D;

                OverlayViewport.ModelMoverPosition = newPosition;

                if (_selectedObjectTranslateTransform3D != null)
                {
                    _selectedObjectTranslateTransform3D.OffsetX = _startMoveOffset.X + moveVector3D.X;
                    _selectedObjectTranslateTransform3D.OffsetY = _startMoveOffset.Y + moveVector3D.Y;
                    _selectedObjectTranslateTransform3D.OffsetZ = _startMoveOffset.Z + moveVector3D.Z;
                }

                if (_selectionWireBoxVisual3D != null)
                {
                    _selectionWireBoxVisual3D.CenterPosition = newPosition;
                }

                if (OverlayCanvas.GetSelectedScreenPositionsCount() > 0)
                {
                    var meshGeometry3D = GetMeshGeometry3D(_selectedModelVisual3D);
                    if (meshGeometry3D != null)
                    {
                        // The positions defined in MeshGeometry3D are transformed by the _selectedModelVisual3D.Transform.
                        // The newPosition is also based on the transformed position.
                        // To change the original position in the MeshGeometry3D, we need to invert the transformation
                        // and then transform the newPosition with the inverted transformation.
                        var transformMatrix = _selectedModelVisual3D.Transform.Value;

                        var invertedTransformMatrix = transformMatrix;
                        invertedTransformMatrix.Invert();

                        var meshMoveVector3D = invertedTransformMatrix.Transform(moveVector3D);

                        for (var i = 0; i < OverlayCanvas.ShownScreenPositions.Count; i++)
                        {
                            var screenPositionData = OverlayCanvas.ShownScreenPositions[i];

                            if (!screenPositionData.IsSelected)
                                continue;

                            // MeshLocalPosition is the same as when the move was stared (it is updated when the move is completed)
                            var newMeshPosition = screenPositionData.MeshLocalPosition + meshMoveVector3D;
                            meshGeometry3D.Positions[i] = newMeshPosition;

                            screenPositionData.WorldPosition = transformMatrix.Transform(newMeshPosition);

                            OverlayCanvas.UpdateScreenPosition(screenPositionData);
                        }
                    }
                }
            };

            OverlayViewport.ModelMoveEnded = () =>
            {
                // When move is completed and if we changed some positions,
                // we need to update the MeshLocalPosition to the new position.
                if (OverlayCanvas.GetSelectedScreenPositionsCount() > 0)
                {
                    var meshGeometry3D = GetMeshGeometry3D(_selectedModelVisual3D);
                    if (meshGeometry3D != null)
                    {
                        for (var i = 0; i < OverlayCanvas.ShownScreenPositions.Count; i++)
                        {
                            if (OverlayCanvas.ShownScreenPositions[i].IsSelected)
                                OverlayCanvas.ShownScreenPositions[i].MeshLocalPosition = meshGeometry3D.Positions[i];
                        }
                    }
                }
            };
        }

        void SceneEditor_Loaded(object sender, RoutedEventArgs e)
        {
            ResetCamera();

            _currentObjectCreationState = ObjectCreationStates.SelectStartPoint;
        }


        #region UI event handlers

        private void FitViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectsVisual.Children.Count == 0)
            {
                ResetCamera();
            }
            else
            { 
                Camera1.FitIntoView(visuals: ObjectsVisual.Children, 
                                    fitIntoViewType: FitIntoViewType.CheckAllPositions,
                                    adjustTargetPosition: true, 
                                    adjustmentFactor: 1.05); // have some margin (5%) around the objects before the edge
            }
        }

        private void ResetCameraCenterButton_Click(object sender, RoutedEventArgs e)
        {
            ResetCamera();
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            ObjectsVisual.Children.Clear();

            SelectObject(null);
            HighlightObject(null);
            OverlayViewport.HideModelMover();
            OverlayCanvas.ClearScreenPositions();
        }


        private void CreateBoxButton_OnClick(object sender, RoutedEventArgs e)
        {
            CreateSphereButton.IsChecked = !(CreateBoxButton.IsChecked ?? false);
        }

        private void CreateSphereButton_OnClick(object sender, RoutedEventArgs e)
        {
            CreateBoxButton.IsChecked = !(CreateSphereButton.IsChecked ?? false);
        }

        private void EditObjectToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            EditPositionsToggleButton.IsChecked = !(EditObjectToggleButton.IsChecked ?? false);

            UpdateEditorModeUI(); // This will reset the UI
        }

        private void EditPositionsToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            EditObjectToggleButton.IsChecked = !(EditPositionsToggleButton.IsChecked ?? false);

            UpdateEditorModeUI(); // This will reset the UI
        }

        private void OnEditorModeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            _currentEditorMode = (CreateRadioButton.IsChecked ?? false) ? EditorModes.Create : 
                                                                          EditorModes.Edit;

            _currentObjectCreationState = (_currentEditorMode == EditorModes.Create) ? ObjectCreationStates.SelectStartPoint :
                                                                                       ObjectCreationStates.None;

            UpdateEditorModeUI();
        }

        #endregion

        #region Mouse handling

        private void ViewportBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentEditorMode == EditorModes.Create)
                ProcessCreateMouseMove(e);

            else if (_currentEditorMode == EditorModes.Edit)
                ProcessEditMouseMove(e);
        }

        private void ViewportBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentEditorMode == EditorModes.Create)
                ProcessCreateMouseButtonPressed(e);

            else if (_currentEditorMode == EditorModes.Edit)
                ProcessEditMouseButtonPressed(e);
        }

        private void ViewportBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_currentObjectCreationState != ObjectCreationStates.SelectBaseSize)
                return;


            double boxWidth = _selectedObjectScaleTransform.ScaleX;
            double boxDepth = _selectedObjectScaleTransform.ScaleZ;

            if (boxWidth * boxDepth < 1)
            {
                // Box too small - user probably only clicked with the mouse and did not make a drag to define the base size
                // Remove the added object
                _currentObjectCreationState = ObjectCreationStates.SelectStartPoint;
                ObjectsVisual.Children.Remove(_selectedModelVisual3D);

                return;
            }


            _selectHeightStartMousePosition = e.GetPosition(MainViewport);

            // Now we will measure how much is one pixel on screen in 3D
            // This will be used in setting the height of the object with moving the mouse up
            Point3D p1 = _lastIntersectionPoint;
            Point3D p2 = new Point3D(p1.X, p1.Y + 1, p1.Z);

            Point p1_screen = Camera1.Point3DTo2D(p1);
            Point p2_screen = Camera1.Point3DTo2D(p2);


            // We have the base object - now define the height
            _screenTo3DFactor = 1.0 / Math.Abs(p2_screen.Y - p1_screen.Y);

            if (CreateBoxButton.IsChecked ?? false)
                _currentObjectCreationState = ObjectCreationStates.SelectHeight;
            else
                _currentObjectCreationState = ObjectCreationStates.SelectStartPoint;
        }


        private void ProcessCreateMouseMove(MouseEventArgs e)
        { 
            if (_currentObjectCreationState == ObjectCreationStates.None)
                return;


            Point mousePosition = e.GetPosition(MainViewport);


            if (_currentObjectCreationState == ObjectCreationStates.SelectStartPoint ||
                _currentObjectCreationState == ObjectCreationStates.SelectBaseSize)
            {
                Point3D intersectionPoint;

                // Get a 3D intersection point of the 3D ray created from current mouse position
                // and a XZ plane (the plane where the wire grid is drawn).
                bool hasIntersection = GetMousePositionOnXZPlane(mousePosition, out intersectionPoint);


                if (hasIntersection)
                {
                    double x = intersectionPoint.X;
                    double y = intersectionPoint.Z;

                    // Snap to grid
                    if (SceneEditorContext.Current.SnapToGrid)
                        SnapToWireGrid(ref x, ref y);


                    _lastIntersectionPoint = new Point3D(x, intersectionPoint.Y, y);


                    if (_currentObjectCreationState == ObjectCreationStates.SelectStartPoint &&
                        e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released &&
                        _horizontalMouseLine != null && _verticalMouseLine != null)
                    {
                        if (Math.Abs(_horizontalMouseLine.StartPosition.Z - y) > 0.1) // Update StartPosition and EndPosition only when the change is significant (>0.1)
                        {
                            _horizontalMouseLine.StartPosition = new Point3D(-HALF_MOUSE_LINE_LENGTH, 0.1, y);
                            _horizontalMouseLine.EndPosition   = new Point3D(HALF_MOUSE_LINE_LENGTH,  0.1, y);
                        }

                        if (Math.Abs(_verticalMouseLine.StartPosition.X - x) > 0.1)
                        {
                            _verticalMouseLine.StartPosition = new Point3D(x, 0.1, -HALF_MOUSE_LINE_LENGTH);
                            _verticalMouseLine.EndPosition   = new Point3D(x, 0.1, HALF_MOUSE_LINE_LENGTH);
                        }

                        _horizontalMouseLine.IsVisible = true;
                        _verticalMouseLine.IsVisible   = true;
                    }

                    if (_currentObjectCreationState == ObjectCreationStates.SelectBaseSize)
                    {
                        if (CreateBoxButton.IsChecked ?? false)
                            UpdateBoxBasePositionAndSize();
                        else
                            UpdateSphereRadius();
                    }
                }
                else
                {
                    // No intersection of the mouse and the plane (hide the lines)
                    _horizontalMouseLine.IsVisible = false;
                    _verticalMouseLine.IsVisible = false;
                }
            }
            else if (_currentObjectCreationState == ObjectCreationStates.SelectHeight)
            {
                double newHeight = _selectHeightStartMousePosition.Y - mousePosition.Y;

                if (newHeight > 0)
                {
                    newHeight *= _screenTo3DFactor;

                    if (_selectedObjectScaleTransform != null)
                        _selectedObjectScaleTransform.ScaleY = newHeight;

                    if (_selectedObjectTranslateTransform3D != null)
                        _selectedObjectTranslateTransform3D.OffsetY = newHeight / 2;

                    // NOTE: We do not change the Size or CenterPosition because this would regenerate the geometry
                    //_newBoxVisual.Size = new Size3D(_newBoxVisual.Size.X, newHeight, _newBoxVisual.Size.Z);
                    //_newBoxVisual.CenterPosition = new Point3D(_newBoxVisual.CenterPosition.X, newHeight / 2, _newBoxVisual.CenterPosition.Z);
                }
            }
        }

        private void ProcessEditMouseMove(MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(MainViewport);

            // If we are editing positions, check if we are close to any 2D position
            if (EditPositionsToggleButton.IsChecked ?? false)
            {
                // Highlight all screen positions that are withing POSITION_SELECTION_MIN_MOUSE_DISTANCE from the mouse position
                OverlayCanvas.HighlightScreenPositions(mousePosition, OverlayCanvas.PositionSelectionMinMouseDistance);
            }

            var hitModelVisual3D = GetHitObject(mousePosition);

            if (hitModelVisual3D != null)
            {
                // If we hit already highlighted or selected object, we do not need to do anything.
                if (hitModelVisual3D == _highlightedModelVisual3D)
                    return;

                if (hitModelVisual3D == _selectedModelVisual3D)
                {
                    HighlightObject(null);
                    return;
                }

                HighlightObject(hitModelVisual3D);
            }
            else
            {
                HighlightObject(null);
            }
        }


        private void ProcessEditMouseButtonPressed(MouseButtonEventArgs e)
        {
            Point mousePosition = e.GetPosition(MainViewport);


            if ((EditPositionsToggleButton.IsChecked ?? false))
            {
                // Get all screen positions that are near mousePosition
                var intersectingScreenPositions = OverlayCanvas.GetIntersectingScreenPositions(mousePosition, OverlayCanvas.PositionSelectionMinMouseDistance);

                if (intersectingScreenPositions != null && intersectingScreenPositions.Count > 0)
                {
                    bool updateModelMover = false;

                    foreach (var intersectingScreenPosition in intersectingScreenPositions)
                    {
                        if (intersectingScreenPosition.IsSelected) // If we clicked on selected position, unselect it
                        {
                            OverlayCanvas.ClearSelectedScreenPosition(intersectingScreenPosition);
                            updateModelMover = true;
                        }
                        else if (intersectingScreenPosition.IsHighlighted) // select highlighted position
                        {
                            OverlayCanvas.ClearHighlightedScreenPosition(intersectingScreenPosition);
                            OverlayCanvas.SelectScreenPosition(intersectingScreenPosition);
                            updateModelMover = true;
                        }
                    }

                    if (updateModelMover)
                    {
                        if (OverlayCanvas.GetSelectedScreenPositionsCount() == 0)
                        {
                            // No more selected positions
                            OverlayViewport.HideModelMover();
                        }
                        else
                        {
                            OverlayViewport.SetupModelMover();

                            // Get center position of all highlighted positions
                            _startMovePosition = OverlayCanvas.GetCenterWorldPosition(p => p.IsSelected);
                            OverlayViewport.ModelMoverPosition = _startMovePosition;

                            OverlayViewport.UpdateModelMoverSize(desiredScreenLength: 100);
                        }
                    }

                    // If we have clicked on a position, do not proceed with checking which object was clicked
                    return;
                }
            }


            var hitModelVisual3D = GetHitObject(mousePosition);

            if (hitModelVisual3D != null)
            {
                if (_selectedModelVisual3D == hitModelVisual3D)
                    return;

                if (EditObjectToggleButton.IsChecked ?? false)
                {
                    // Show selection CornerWireBoxVisual3D around selected object
                    var selectedModelBounds = ModelUtils.GetBounds(hitModelVisual3D, hitModelVisual3D.Transform, checkOnlyBounds: true);
                    SelectObject(hitModelVisual3D, selectedModelBounds);

                    OverlayViewport.SetupModelMover();

                    _startMovePosition = selectedModelBounds.GetCenterPosition();
                    OverlayViewport.ModelMoverPosition = _startMovePosition;

                    OverlayViewport.UpdateModelMoverSize(desiredScreenLength: 100);
                }
                else
                {
                    OverlayCanvas.ClearScreenPositions(); // Clear positions on previously selected model

                    var meshGeometry3D = GetMeshGeometry3D(hitModelVisual3D);
                    OverlayCanvas.AddScreenPositions(meshGeometry3D, hitModelVisual3D.Transform);

                    _selectedModelVisual3D = hitModelVisual3D;
                }
            }


            if (_selectedModelVisual3D == null)
            {
                SelectObject(null);
                OverlayViewport.HideModelMover();
            }

            HighlightObject(null);
        }

        private void ProcessCreateMouseButtonPressed(MouseButtonEventArgs e)
        { 
            if (_currentObjectCreationState == ObjectCreationStates.SelectStartPoint)
            {
                _objectStartPosition = _lastIntersectionPoint;

                if (CreateBoxButton.IsChecked ?? false)
                {
                    var newBoxVisual = new BoxVisual3D()
                    {
                        Material = _standardMaterial,
                        BackMaterial = _standardBackMaterial,

                        FreezeMeshGeometry3D = false, // Do not freeze and cache the MeshGeometry3D because we can change its positions
                        UseCachedMeshGeometry3D = false
                    };

                    // NOTE:
                    // We create the Box with its default size (1,1,1) and at its default position (0,0,0)
                    // We will not change those two properties because each of them triggers regeneration of the geometry.
                    // Instead we will define scale and translate transform that will update the 3D Box to the correct size and position.
                    // This is much better for performance and much more garbage collection friendly

                    // By default the box is already created on (0,0,0) and with size (1,1,1) so we can comment the following 2 lines:
                    //_newBoxVisual.CenterPosition = new Point3D(0, 0, 0);
                    //_newBoxVisual.Size = new Size3D(1, 1, 1);

                    _selectedObjectScaleTransform       = new ScaleTransform3D(0.0, NEW_OBJECT_HEIGHT, 0.0);
                    _selectedObjectTranslateTransform3D = new TranslateTransform3D(_objectStartPosition.X, _objectStartPosition.Y, _objectStartPosition.Z);

                    var transform3DGroup = new Transform3DGroup();
                    transform3DGroup.Children.Add(_selectedObjectScaleTransform);
                    transform3DGroup.Children.Add(_selectedObjectTranslateTransform3D);

                    newBoxVisual.Transform = transform3DGroup;

                    ObjectsVisual.Children.Add(newBoxVisual);

                    _selectedModelVisual3D = newBoxVisual;
                }
                else
                {
                    var newSphereVisual3D = new SphereVisual3D()
                    {
                        Material = _standardMaterial,
                        BackMaterial = _standardBackMaterial,

                        FreezeMeshGeometry3D    = false, // Do not freeze and cache the MeshGeometry3D because we can change its positions
                        UseCachedMeshGeometry3D = false
                    };

                    _selectedObjectScaleTransform       = new ScaleTransform3D(NEW_OBJECT_HEIGHT, NEW_OBJECT_HEIGHT, NEW_OBJECT_HEIGHT);
                    _selectedObjectTranslateTransform3D = new TranslateTransform3D(_objectStartPosition.X, _objectStartPosition.Y, _objectStartPosition.Z);

                    var transform3DGroup = new Transform3DGroup();
                    transform3DGroup.Children.Add(_selectedObjectScaleTransform);
                    transform3DGroup.Children.Add(_selectedObjectTranslateTransform3D);

                    newSphereVisual3D.Transform = transform3DGroup;

                    ObjectsVisual.Children.Add(newSphereVisual3D);

                    _selectedModelVisual3D = newSphereVisual3D;
                }

                // Change state from SelectStartPoint => SelectBaseSize
                _currentObjectCreationState = ObjectCreationStates.SelectBaseSize;
            }
            else if (_currentObjectCreationState == ObjectCreationStates.SelectHeight)
            {
                // Finish box creation
                // Change state from SelectBaseSize => SelectStartPoint
                _currentObjectCreationState = ObjectCreationStates.SelectStartPoint;
            }
        }

        #endregion

        #region Hit-testing
        private ModelVisual3D GetHitObject(Point mousePosition)
        {
            ModelVisual3D hitModelVisual3D = null;


            // We will use VisualTreeHelper.HitTest with a BeginHitTestResultHandler 
            // because we need to filter out the helper 3D objects (3D lines, wire grid, etc) - so objects that are not children of ObjectsVisual.
            // If we would not need that filter, we could simply use:
            //var hitTestResult = VisualTreeHelper.HitTest(MainViewport, mousePosition) as RayMeshGeometry3DHitTestResult;

            _lastHitTestResult = null; // This field will be set in the BeginHitTestResultHandler

            var pointParams = new PointHitTestParameters(mousePosition);
            VisualTreeHelper.HitTest(MainViewport, null, BeginHitTestResultHandler, pointParams);


            if (_lastHitTestResult != null)
            {
                hitModelVisual3D = _lastHitTestResult.VisualHit as ModelVisual3D;

                // proceed only if we hit an object from ObjectsVisual (and not wire grid or some other helper object)
                if (!ObjectsVisual.Children.Contains(hitModelVisual3D))
                    hitModelVisual3D = null;
            }

            return hitModelVisual3D;
        }

        private HitTestResultBehavior BeginHitTestResultHandler(HitTestResult result)
        {
            var rayHitResult = result as RayMeshGeometry3DHitTestResult;

            if (rayHitResult == null)
                return HitTestResultBehavior.Stop;

            // Check if the hit object is a child of ObjectsVisual.
            // If not then continue with hit testing because we have probably hit some 3D line or wire grid.
            if (!ObjectsVisual.Children.Contains(rayHitResult.VisualHit))
                return HitTestResultBehavior.Continue;


            // If we came here then we have a hit on a visual from ObjectsVisual
            _lastHitTestResult = rayHitResult;

            return HitTestResultBehavior.Stop;
        }
        #endregion


        #region Object selection and highlighting
        private void SelectObject(ModelVisual3D newSelectedObject)
        {
            SelectObject(newSelectedObject, Rect3D.Empty);
        }

        private void SelectObject(ModelVisual3D newSelectedObject, Rect3D selectedObjectBounds)
        {
            if (newSelectedObject == null)
            {
                _selectedModelVisual3D              = null;
                _selectedObjectScaleTransform       = null;
                _selectedObjectTranslateTransform3D = null;

                if (_selectionWireBoxVisual3D != null)
                {
                    if (LinesVisual.Children.Contains(_selectionWireBoxVisual3D))
                        LinesVisual.Children.Remove(_selectionWireBoxVisual3D);

                    _selectionWireBoxVisual3D = null;
                }

                OverlayCanvas.ClearScreenPositions();
            }
            else
            {
                if (_selectionWireBoxVisual3D == null)
                {
                    _selectionWireBoxVisual3D = new CornerWireBoxVisual3D()
                    {
                        IsLineLengthPercent = false,
                        LineLength          = 5,
                        LineThickness       = 2,
                        LineColor           = SceneEditorContext.Current.SelectedColor
                    };
                }

                if (selectedObjectBounds.IsEmpty)
                    selectedObjectBounds = ModelUtils.GetBounds(newSelectedObject, newSelectedObject.Transform, checkOnlyBounds: true);

                double standardSelectionLineLength = 5;
                double minSize                     = Math.Min(selectedObjectBounds.SizeX, Math.Min(selectedObjectBounds.SizeY, selectedObjectBounds.SizeZ));

                if (minSize > standardSelectionLineLength * 3)
                    _selectionWireBoxVisual3D.LineLength = standardSelectionLineLength;
                else
                    _selectionWireBoxVisual3D.LineLength = minSize / 3;

                
                _selectionWireBoxVisual3D.CenterPosition = selectedObjectBounds.GetCenterPosition();
                _selectionWireBoxVisual3D.Size           = selectedObjectBounds.Size;


                if (!LinesVisual.Children.Contains(_selectionWireBoxVisual3D))
                    LinesVisual.Children.Add(_selectionWireBoxVisual3D);

                _selectedModelVisual3D              = newSelectedObject;
                _selectedObjectTranslateTransform3D = GetFirstTranslateTransform3D(_selectedModelVisual3D.Transform);
                _selectedObjectScaleTransform       = GetFirstScaleTransform3D(_selectedModelVisual3D.Transform);
            }
        }

        private void HighlightObject(ModelVisual3D newHighlightedObject)
        {
            HighlightObject(newHighlightedObject, Rect3D.Empty);
        }

        private void HighlightObject(ModelVisual3D newHighlightedObject, Rect3D highlightedObjectBounds)
        {
            if (newHighlightedObject == null)
            {
                _highlightedModelVisual3D = null;

                if (_highlightWireBoxVisual3D != null)
                {
                    if (LinesVisual.Children.Contains(_highlightWireBoxVisual3D))
                        LinesVisual.Children.Remove(_highlightWireBoxVisual3D);

                    _highlightWireBoxVisual3D = null;
                }
            }
            else if (newHighlightedObject == _selectedModelVisual3D)
            {
                // Do nothing - do not highlight selected object
            }
            else
            {
                if (_highlightWireBoxVisual3D == null)
                {
                    _highlightWireBoxVisual3D = new CornerWireBoxVisual3D()
                    {
                        IsLineLengthPercent = false,
                        LineLength          = 5,
                        LineThickness       = 2,
                        LineColor           = SceneEditorContext.Current.HighlightedColor
                    };
                }

                if (highlightedObjectBounds.IsEmpty)
                    highlightedObjectBounds = ModelUtils.GetBounds(newHighlightedObject, newHighlightedObject.Transform, checkOnlyBounds: true);

                double standardSelectionLineLength = 5;
                double minSize = Math.Min(highlightedObjectBounds.SizeX, Math.Min(highlightedObjectBounds.SizeY, highlightedObjectBounds.SizeZ));

                if (minSize > standardSelectionLineLength * 3)
                    _highlightWireBoxVisual3D.LineLength = standardSelectionLineLength;
                else
                    _highlightWireBoxVisual3D.LineLength = minSize / 3;

                _highlightWireBoxVisual3D.CenterPosition = highlightedObjectBounds.GetCenterPosition();
                _highlightWireBoxVisual3D.Size           = highlightedObjectBounds.Size;


                if (!LinesVisual.Children.Contains(_highlightWireBoxVisual3D))
                    LinesVisual.Children.Add(_highlightWireBoxVisual3D);

                _highlightedModelVisual3D = newHighlightedObject;
            }
        }
        #endregion


        #region Other helper methods

        private void UpdateEditorModeUI()
        {
            if (_currentEditorMode == EditorModes.Create)
            {
                CreateOptionsPanel.Visibility = Visibility.Visible;
                EditOptionsPanel.Visibility   = Visibility.Collapsed;

                ViewportBorder.Cursor = Cursors.Arrow;
            }
            else if (_currentEditorMode == EditorModes.Edit)
            {
                CreateOptionsPanel.Visibility = Visibility.Collapsed;
                EditOptionsPanel.Visibility   = Visibility.Visible;

                ViewportBorder.Cursor = Cursors.Hand;
            }

            SelectObject(null);
            HighlightObject(null);
            OverlayViewport.HideModelMover();
            OverlayCanvas.ClearScreenPositions();

            UpdateHorizontalMouseLines();
        }


        private void UpdateHorizontalMouseLines()
        {
            if (_currentEditorMode == EditorModes.Create)
            {
                if (_horizontalMouseLine == null)
                {
                    _horizontalMouseLine = new Visuals.LineVisual3D
                    {
                        LineColor     = Colors.Green,
                        LineThickness = 2,
                        IsVisible     = false
                    };

                    LinesVisual.Children.Add(_horizontalMouseLine);
                }

                if (_verticalMouseLine == null)
                {
                    _verticalMouseLine = new Visuals.LineVisual3D
                    {
                        LineColor     = Colors.Green,
                        LineThickness = 2,
                        IsVisible     = false
                    };

                    LinesVisual.Children.Add(_verticalMouseLine);
                }
            }
            else
            {
                if (_horizontalMouseLine != null)
                {
                    LinesVisual.Children.Remove(_horizontalMouseLine);
                    _horizontalMouseLine = null;
                }

                if (_verticalMouseLine != null)
                {
                    LinesVisual.Children.Remove(_verticalMouseLine);
                    _verticalMouseLine = null;
                }
            }
        }

        private void ResetCamera()
        {
            Camera1.BeginInit();

            Camera1.Heading  = 30;
            Camera1.Attitude = -30;
            Camera1.Distance = 300;

            Camera1.Offset         = new Vector3D(0, 0, 0);
            Camera1.TargetPosition = new Point3D(0, 0, 0);
            Camera1.EndInit();

            Camera1.Refresh();
        }

        private void UpdateSphereRadius()
        {
            double radius = (_objectStartPosition - _lastIntersectionPoint).Length;

            _selectedObjectScaleTransform.ScaleX = radius;
            _selectedObjectScaleTransform.ScaleY = radius;
            _selectedObjectScaleTransform.ScaleZ = radius;
        }

        private void UpdateBoxBasePositionAndSize()
        {
            double x, z, width, depth;

            if (_objectStartPosition.X < _lastIntersectionPoint.X)
            {
                width = _lastIntersectionPoint.X - _objectStartPosition.X;
                x = _objectStartPosition.X + width / 2;
            }
            else
            {
                width = _objectStartPosition.X - _lastIntersectionPoint.X;
                x = _lastIntersectionPoint.X + width / 2;
            }

            if (_objectStartPosition.Z < _lastIntersectionPoint.Z)
            {
                depth = _lastIntersectionPoint.Z - _objectStartPosition.Z;
                z = _objectStartPosition.Z + depth / 2;
            }
            else
            {
                depth = _objectStartPosition.Z - _lastIntersectionPoint.Z;
                z = _lastIntersectionPoint.Z + depth / 2;
            }

            if (width >= 0.0001 && depth >= 0.0001)
            {
                _selectedObjectScaleTransform.ScaleX = width;
                _selectedObjectScaleTransform.ScaleZ = depth;

                _selectedObjectTranslateTransform3D.OffsetX = x;
                _selectedObjectTranslateTransform3D.OffsetZ = z;

                // NOTE: We do not change the Size or CenterPosition because this would regenerate the geometry (instead we use transformations to position and define the size of the geometry)
                //_newBoxVisual.Size = new Size3D(width, _newBoxVisual.Size.Y, depth);
                //_newBoxVisual.CenterPosition = new Point3D(x, _newBoxVisual.CenterPosition.Y, z);
            }
        }


        private void SnapToWireGrid(ref double x, ref double y)
        {
            double xGridSize = MainWireGrid.Size.Width / MainWireGrid.WidthCellsCount;
            double yGridSize = MainWireGrid.Size.Height / MainWireGrid.HeightCellsCount;

            double halfXGridSize = xGridSize * 0.5;
            double halfYGridSize = yGridSize * 0.5;


            double xAdjust = ((Math.Abs(x) + halfXGridSize) % xGridSize);

            if ((MainWireGrid.WidthCellsCount % 2) == 0)
                xAdjust -= halfXGridSize;

            if (x > 0)
                x -= xAdjust;
            else
                x += xAdjust;


            double yAdjust = ((Math.Abs(y) + halfYGridSize) % yGridSize);

            if ((MainWireGrid.HeightCellsCount % 2) == 0)
                yAdjust -= halfYGridSize;

            if (y > 0)
                y -= yAdjust;
            else
                y += yAdjust;
        }


        private MeshGeometry3D GetMeshGeometry3D(ModelVisual3D modelVisual3D)
        {
            MeshGeometry3D meshGeometry3D;

            var geometryModel3D = modelVisual3D.Content as GeometryModel3D;

            if (geometryModel3D != null)
                meshGeometry3D = (MeshGeometry3D)geometryModel3D.Geometry;
            else
                meshGeometry3D = null;

            return meshGeometry3D;
        }

        // Returns false is there is no intersection
        private bool GetMousePositionOnXZPlane(Point mousePosition, out Point3D intersectionPoint)
        {
            Point3D rayOrigin;
            Vector3D rayDirection;

            // Calculate the 3D ray that goes from the mouse position into the 3D scene
            bool success = Camera1.CreateMouseRay3D(mousePosition, out rayOrigin, out rayDirection);

            bool hasIntersection;
            intersectionPoint = new Point3D();

            if (success && rayDirection.Y != 0)
            {
                // Get intersection of ray and xz plane - defined by N=(0,1,0), P=(0,0,0)
                // Because we use the xz plane the intersection formula is very simple:
                double t = (-rayOrigin.Y) / rayDirection.Y;

                if (t > 0) // we have an intersection
                {
                    // Get the 3d point of intersection
                    intersectionPoint = rayOrigin + t * rayDirection;

                    // Skip intersections that are far away
                    if (intersectionPoint.Z > HALF_MOUSE_LINE_LENGTH || intersectionPoint.Z < -HALF_MOUSE_LINE_LENGTH || intersectionPoint.X > HALF_MOUSE_LINE_LENGTH || intersectionPoint.X < -HALF_MOUSE_LINE_LENGTH)
                        hasIntersection = false;
                    else
                        hasIntersection = true;
                }
                else
                {
                    hasIntersection = false;
                }
            }
            else
            {
                hasIntersection = false;
            }

            return hasIntersection;
        }

        /// <summary>
        /// Checks the specified transform3D or its children (in case of Transform3DGroup)
        /// and returns the first occurence of the TranslateTransform3D
        /// or null if the TranslateTransform3D is not found.
        /// </summary>
        /// <param name="transform3D">transform3D as Transform3D</param>
        /// <returns>the first occurence of the TranslateTransform3D or null if the TranslateTransform3D is not found.</returns>
        public static TranslateTransform3D GetFirstTranslateTransform3D(Transform3D transform3D)
        {
            return GetFirstTransform3D<TranslateTransform3D>(transform3D);
        }

        /// <summary>
        /// Checks the specified transform3D or its children (in case of Transform3DGroup)
        /// and returns the first occurence of the ScaleTransform3D
        /// or null if the ScaleTransform3D is not found.
        /// </summary>
        /// <param name="transform3D">transform3D as Transform3D</param>
        /// <returns>the first occurence of the ScaleTransform3D or null if the ScaleTransform3D is not found.</returns>
        public static ScaleTransform3D GetFirstScaleTransform3D(Transform3D transform3D)
        {
            return GetFirstTransform3D<ScaleTransform3D>(transform3D);
        }

        /// <summary>
        /// Checks the specified transform3D or its children (in case of Transform3DGroup)
        /// and returns the first occurence of the RotateTransform3D
        /// or null if the RotateTransform3D is not found.
        /// </summary>
        /// <param name="transform3D">transform3D as Transform3D</param>
        /// <returns>the first occurence of the RotateTransform3D or null if the RotateTransform3D is not found.</returns>
        public static RotateTransform3D GetFirstRotateTransform3D(Transform3D transform3D)
        {
            return GetFirstTransform3D<RotateTransform3D>(transform3D);
        }

        /// <summary>
        /// Checks the specified transform3D or its children (in case of Transform3DGroup)
        /// and returns the first occurence of the specified 3D transformation type (T)
        /// or null if the specified transformation type is not found.
        /// </summary>
        /// <typeparam name="T">TranslateTransform3D, ScaleTransform3D or RotateTransform3D</typeparam>
        /// <param name="transform3D">transform3D as Transform3D</param>
        /// <returns>the first occurence of the specified 3D transformation type (T) or null if the specified transformation type is not found.</returns>
        public static T GetFirstTransform3D<T>(Transform3D transform3D) where T : Transform3D
        {
            if (transform3D == null)
                return null;

            var translateTransform3D = transform3D as T;

            if (translateTransform3D != null)
                return translateTransform3D;

            var transform3DGroup = transform3D as Transform3DGroup;

            if (transform3DGroup != null)
            {
                foreach (var childTransform in transform3DGroup.Children)
                {
                    translateTransform3D = GetFirstTransform3D<T>(childTransform);

                    if (translateTransform3D != null)
                        return translateTransform3D;
                }
            }

            return null;
        }

        #endregion
    }
}