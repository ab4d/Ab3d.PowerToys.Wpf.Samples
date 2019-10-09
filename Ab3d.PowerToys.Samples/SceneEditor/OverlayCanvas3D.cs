using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Ab3d.Cameras;
using Ab3d.Common.Cameras;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.SceneEditor
{
    /// <summary>
    /// OverlayCanvas3D is a Canvas that can be added over the Viewport3D object and can show various 2D elements on top of 3D objects.
    /// When the camera or size of the Viewport3D is changed, the OverlayCanvas3D can automatically update the positions of the objects to "follow" the 3D objects.
    /// </summary>
    public class OverlayCanvas3D : Canvas
    {
        public class ScreenPositionData
        {
            public Point ScreenCenterPosition;
            public Point3D WorldPosition;
            public Point3D MeshLocalPosition;

            public bool IsHighlighted;
            public bool IsSelected;

            public Rectangle PositionRectangle;
            public Rectangle SelectionRectangle;

            public ScreenPositionData(Point screenCenterPosition, Point3D worldPosition, Point3D meshLocalPosition, Rectangle positionRectangle = null)
            {
                ScreenCenterPosition = screenCenterPosition;
                WorldPosition        = worldPosition;
                MeshLocalPosition    = meshLocalPosition;
                PositionRectangle    = positionRectangle;

                IsSelected    = false;
                IsHighlighted = false;
            }
        }


        private Viewport3D _subscribedViewport3D;
        private BaseCamera _subscribedCamera;


        public List<ScreenPositionData> ShownScreenPositions;

        public double PositionRectangleSize { get; set; }

        public double PositionSelectionMinMouseDistance { get; set; }


        #region ParentViewport3DProperty
        /// <summary>
        /// ParentViewport3DProperty
        /// </summary>
        public static readonly DependencyProperty ParentViewport3DProperty = DependencyProperty.Register("ParentViewport3D", typeof(Viewport3D), typeof(OverlayCanvas3D), new FrameworkPropertyMetadata(null, OnParentViewport3DPropertyChanged));

        /// <summary>
        /// Gets or sets a Viewport3D that is showing the 3D scene.
        /// </summary>
        public Viewport3D ParentViewport3D
        {
            get
            {
                return (Viewport3D)base.GetValue(ParentViewport3DProperty);
            }
            set
            {
                base.SetValue(ParentViewport3DProperty, value);
            }
        }

        private static void OnParentViewport3DPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overlayCanvas3D = (OverlayCanvas3D)d;
            var viewport3D = e.NewValue as Viewport3D;

            overlayCanvas3D.SubscribeViewport3DChanges(viewport3D);
        }
        #endregion

        #region CameraProperty
        /// <summary>
        /// CameraProperty
        /// </summary>
        public static readonly DependencyProperty CameraProperty = DependencyProperty.Register("Camera", typeof(Ab3d.Cameras.BaseCamera), typeof(OverlayCanvas3D), new FrameworkPropertyMetadata(null, OnTargetCameraPropertyChanged), ValidateTargetCameraValue);

        /// <summary>
        /// Gets or sets the Ab3d.Camera that is used to view the 3D scene and is required in OverlayCanvas3D to correctly update the overlayed 2D controls.
        /// </summary>
        public Ab3d.Cameras.BaseCamera Camera
        {
            get
            {
                return (Ab3d.Cameras.BaseCamera)base.GetValue(CameraProperty);
            }
            set
            {
                base.SetValue(CameraProperty, value);
            }
        }

        private static bool ValidateTargetCameraValue(object value)
        {
            return (value == null || value is Ab3d.Cameras.BaseCamera);
        }

        private static void OnTargetCameraPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overlayCanvas3D = (OverlayCanvas3D)d;
            var camera = e.NewValue as BaseCamera;

            overlayCanvas3D.SubscribeCameraChanges(camera);
        }
        #endregion

        #region EventsSourceElementProperty
        /// <summary>
        /// EventsSourceElementProperty
        /// </summary>
        public static readonly DependencyProperty EventsSourceElementProperty = DependencyProperty.Register("EventsSourceElement", typeof(FrameworkElement), typeof(OverlayCanvas3D), new FrameworkPropertyMetadata(null, OnEventsSourceElementPropertyChanged), ValidateEventsSourceElementValue);

        /// <summary>
        /// Gets or sets the element where the mouse events are subscribed.
        /// If the property is not set, the Viewport3D that is specified to ParentViewport3D property is used as event source.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Gets or sets the element where the mouse events are subscribed.
        /// </para>
        /// <para>
        /// If the property is not set, the Viewport3D that is specified to ParentViewport3D property is used as event source.
        /// </para>
        /// </remarks>
        public FrameworkElement EventsSourceElement
        {
            get
            {
                return (FrameworkElement)base.GetValue(EventsSourceElementProperty);
            }
            set
            {
                base.SetValue(EventsSourceElementProperty, value);
            }
        }

        private static void OnEventsSourceElementPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var cameraController = (OverlayCanvas3D)d;

            //// In design time there is no need to be subscribed to events
            //if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(cameraController))
            //    return;


            //var newEventsSourceElement = (FrameworkElement)e.NewValue;

            //if (ReferenceEquals(cameraController.subscribedEventsSourceElement, newEventsSourceElement))
            //    return;

            //cameraController.UnsubscribeEvents();

            //if (newEventsSourceElement != null)
            //    cameraController.SubscribeEvents(newEventsSourceElement);
        }

        private static bool ValidateEventsSourceElementValue(object value)
        {
            return (value == null || value is FrameworkElement);
        }
        #endregion

        public OverlayCanvas3D()
        {
            this.IsHitTestVisible = false;

            PositionRectangleSize = 4;
            PositionSelectionMinMouseDistance = 8;

            ShownScreenPositions = new List<ScreenPositionData>();

            this.Loaded += delegate(object sender, RoutedEventArgs args) { UpdateCanvasSize(); };
        }

        public void UpdateCanvasSize()
        {
            var parentViewport3D = ParentViewport3D;

            if (parentViewport3D == null)
                return;

            double width = parentViewport3D.ActualWidth;
            double height = parentViewport3D.ActualHeight;

            if (double.IsNaN(width) || MathUtils.IsZero(width))
            {
                width = parentViewport3D.Width;
                height = parentViewport3D.Height;
            }

            if (double.IsNaN(width) || MathUtils.IsZero(width))
                return; // No valid size

            this.Width = width;
            this.Height = height;
        }

        private void SubscribeViewport3DChanges(Viewport3D viewport3D)
        {
            if (_subscribedViewport3D != null)
                _subscribedViewport3D.SizeChanged -= Viewport3DOnSizeChanged;

            if (viewport3D != null)
                viewport3D.SizeChanged += Viewport3DOnSizeChanged;

            _subscribedViewport3D = viewport3D;
        }

        private void SubscribeCameraChanges(BaseCamera camera)
        {
            if (_subscribedCamera != null)
                _subscribedCamera.CameraChanged -= CameraOnCameraChanged;

            if (camera != null)
                camera.CameraChanged += CameraOnCameraChanged;

            _subscribedCamera = camera;
        }

        private void CameraOnCameraChanged(object sender, CameraChangedRoutedEventArgs e)
        {
            UpdateCanvasSize();
            
            // When camera is changed, we need to update the 2D positions that are get from 3D points
            UpdateScreenPositions();
        }

        private void Viewport3DOnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCanvasSize();

            // When MainViewport's size is changed, we need to update the 2D positions that are get from 3D points
            UpdateScreenPositions();
        }

        public int GetSelectedScreenPositionsCount()
        {
            if (ShownScreenPositions == null)
                return 0;

            return ShownScreenPositions.Count(p => p.IsSelected);
        }

        public int GetHighlightedScreenPositionsCount()
        {
            if (ShownScreenPositions == null)
                return 0;

            return ShownScreenPositions.Count(p => p.IsHighlighted);
        }

        public void ClearScreenPositions()
        {
            this.Children.Clear();

            if (ShownScreenPositions != null)
                ShownScreenPositions.Clear();
        }

        public void AddScreenPositions(MeshGeometry3D meshGeometry3D, Transform3D transform)
        {
            if (meshGeometry3D == null)
                return;

            bool useTransform = transform != null && !transform.Value.IsIdentity;

            var positions = meshGeometry3D.Positions;
            for (int i = 0; i < positions.Count; i++)
            {
                var meshLocalPosition = positions[i];

                Point3D worldPosition;
                if (useTransform)
                    worldPosition = transform.Transform(meshLocalPosition);
                else
                    worldPosition = meshLocalPosition;

                AddScreenPosition(worldPosition, meshLocalPosition);
            }
        }

        public void AddScreenPosition(Point3D worldPosition, Point3D meshLocalPosition)
        {
            Point screenPosition;

            if (this.Camera != null)
                screenPosition = this.Camera.Point3DTo2D(worldPosition);
            else
                screenPosition = new Point();

            var rectangle = CreatePositionRectangle(screenPosition, SceneEditorContext.Current.HighlightedBrush);

            this.Children.Add(rectangle);


            // Store data into _shownScreenPositions
            if (ShownScreenPositions == null)
                ShownScreenPositions = new List<ScreenPositionData>();

            ShownScreenPositions.Add(new ScreenPositionData(screenPosition, worldPosition, meshLocalPosition, rectangle));
        }

        // Updates the 2D positions of the rectangles that are showing 3D positions because the camera has changed
        public void UpdateScreenPositions()
        {
            if (ShownScreenPositions == null)
                return;

            for (var i = 0; i < ShownScreenPositions.Count; i++)
                UpdateScreenPosition(ShownScreenPositions[i]);
        }

        // Updates shown screen positions on a 2D Canvas - called after camera or size change
        public void UpdateScreenPosition(ScreenPositionData screenPositionData)
        {
            if (screenPositionData == null || this.Camera == null)
                return;

            var position3D = screenPositionData.WorldPosition;
            var screenPosition = this.Camera.Point3DTo2D(position3D);

            screenPositionData.ScreenCenterPosition = screenPosition;


            var rectangle = screenPositionData.PositionRectangle;

            if (rectangle != null)
            {
                Canvas.SetLeft(rectangle, screenPosition.X - rectangle.Width * 0.5);
                Canvas.SetTop(rectangle, screenPosition.Y - rectangle.Height * 0.5);
            }


            rectangle = screenPositionData.SelectionRectangle;

            if (rectangle != null)
            {
                Canvas.SetLeft(rectangle, screenPosition.X - rectangle.Width * 0.5);
                Canvas.SetTop(rectangle, screenPosition.Y - rectangle.Height * 0.5);
            }
        }

        public void ClearHighlightedScreenPositions()
        {
            if (ShownScreenPositions == null)
                return;

            foreach (var screenPositionData in ShownScreenPositions.Where(p => p.IsHighlighted))
                ClearHighlightedScreenPosition(screenPositionData);
        }

        public void ClearHighlightedScreenPosition(ScreenPositionData screenPositionData)
        {
            if (screenPositionData == null || !screenPositionData.IsHighlighted)
                return;

            if (!screenPositionData.IsSelected && screenPositionData.SelectionRectangle != null) // Preserve the rectangle if this position is also selected
            {
                this.Children.Remove(screenPositionData.SelectionRectangle);
                screenPositionData.SelectionRectangle = null;
            }

            screenPositionData.IsHighlighted = false;
        }

        public void HighlightScreenPositions(Point centerPosition, double rectWidth, double rectHeight = 0)
        {
            if (rectHeight <= 0)
                rectHeight = rectWidth;

            var screenRect = new Rect(new Point(centerPosition.X - rectWidth * 0.5, centerPosition.Y - rectHeight * 0.5),
                                      new Size(rectWidth, rectHeight));

            HighlightScreenPositions(screenRect);
        }

        public void HighlightScreenPositions(Rect screenRect)
        {
            if (ShownScreenPositions == null)
                return;

            foreach (var shownScreenPosition in ShownScreenPositions)
            {
                // If this position is inside the specified screenRect
                if (screenRect.Contains(shownScreenPosition.ScreenCenterPosition))
                {
                    if (!shownScreenPosition.IsSelected && !shownScreenPosition.IsHighlighted)
                        HighlightScreenPosition(shownScreenPosition);
                }
                else
                {
                    if (!shownScreenPosition.IsSelected && shownScreenPosition.IsHighlighted)
                        ClearHighlightedScreenPosition(shownScreenPosition);
                }
            }
        }

        public List<ScreenPositionData> GetIntersectingScreenPositions(Point centerPosition, double rectWidth, double rectHeight = 0)
        {
            if (rectHeight <= 0)
                rectHeight = rectWidth;

            var screenRect = new Rect(new Point(centerPosition.X - rectWidth * 0.5, centerPosition.Y - rectHeight * 0.5),
                                      new Size(rectWidth, rectHeight));

            return GetIntersectingScreenPositions(screenRect);
        }

        public List<ScreenPositionData> GetIntersectingScreenPositions(Rect screenRect)
        {
            if (ShownScreenPositions == null)
                return new List<ScreenPositionData>();

            var screenPositions = ShownScreenPositions.Where(p => screenRect.Contains(p.ScreenCenterPosition)).ToList();

            return screenPositions;
        }

        public ScreenPositionData GetClosestScreenPosition(Point screenPosition, double positionSelectionMinDistance)
        {
            if (ShownScreenPositions == null)
                return null;


            ScreenPositionData closestScreenPositionData = null;
            double closestDistance = double.MaxValue;

            for (int i = 0; i < ShownScreenPositions.Count; i++)
            {
                var rectangleDistance = (screenPosition - ShownScreenPositions[i].ScreenCenterPosition).Length;

                if (rectangleDistance < closestDistance)
                {
                    closestDistance = rectangleDistance;
                    closestScreenPositionData = ShownScreenPositions[i];
                }
            }

            if (closestDistance <= positionSelectionMinDistance) // Do we have a position that is within accepted distance from the specified screenPosition?
                return closestScreenPositionData;

            return null;
        }


        // Converts all Highlighted screen positions to selected
        public void SelectHighlightedScreenPositions()
        {
            if (ShownScreenPositions == null)
                return;

            foreach (var shownScreenPosition in ShownScreenPositions)
            {
                if (!shownScreenPosition.IsSelected && shownScreenPosition.IsHighlighted)
                {
                    ClearHighlightedScreenPosition(shownScreenPosition);
                    SelectScreenPosition(shownScreenPosition);
                }
            }
        }

        public void HighlightScreenPosition(ScreenPositionData screenPositionData)
        {
            if (screenPositionData == null || screenPositionData.IsHighlighted)
                return;

            var rectangle = CreateSelectionRectangle(screenPositionData.ScreenCenterPosition, SceneEditorContext.Current.HighlightedBrush);
            this.Children.Add(rectangle);

            screenPositionData.IsHighlighted = true;
            screenPositionData.SelectionRectangle = rectangle;
        }

        public void ClearSelectedScreenPositions()
        {
            if (ShownScreenPositions == null)
                return;

            foreach (var screenPositionData in ShownScreenPositions.Where(p => p.IsSelected))
                ClearSelectedScreenPosition(screenPositionData);
        }

        public void ClearSelectedScreenPosition(ScreenPositionData screenPositionData)
        {
            if (screenPositionData == null || !screenPositionData.IsSelected)
                return;

            if (!screenPositionData.IsHighlighted && screenPositionData.SelectionRectangle != null) // Preserve the rectangle if this position is also highlighted
            {
                this.Children.Remove(screenPositionData.SelectionRectangle);
                screenPositionData.SelectionRectangle = null;
            }

            screenPositionData.IsSelected = false;
        }

        public void SelectScreenPosition(ScreenPositionData screenPositionData)
        {
            if (screenPositionData == null || screenPositionData.IsSelected)
                return;

            var rectangle = CreateSelectionRectangle(screenPositionData.ScreenCenterPosition, SceneEditorContext.Current.SelectedBrush);
            this.Children.Add(rectangle);

            screenPositionData.IsSelected = true;
            screenPositionData.SelectionRectangle = rectangle;
        }

        public Rectangle CreateSelectionRectangle(Point centerPosition, Brush strokeBrush)
        {
            var rectangle = new Rectangle()
            {
                Width = PositionRectangleSize * 2,
                Height = PositionRectangleSize * 2,
                Stroke = strokeBrush,
                StrokeThickness = 1,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(rectangle, centerPosition.X - PositionRectangleSize);
            Canvas.SetTop(rectangle, centerPosition.Y - PositionRectangleSize);

            return rectangle;
        }

        public Rectangle CreatePositionRectangle(Point centerPosition, Brush strokeBrush)
        {
            var rectangle = new Rectangle()
            {
                Width = PositionRectangleSize,
                Height = PositionRectangleSize,
                Fill = strokeBrush,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(rectangle, centerPosition.X - PositionRectangleSize * 0.5);
            Canvas.SetTop(rectangle, centerPosition.Y - PositionRectangleSize * 0.5);

            return rectangle;
        }



        // Usage: var centerPosition = GetCenterWorldPosition(p => p.IsHighlighted);
        public Point3D GetCenterWorldPosition(Func<ScreenPositionData, bool> predicate)
        {
            double x     = 0;
            double y     = 0;
            double z     = 0;
            int    count = 0;

            foreach (var screenPositionData in ShownScreenPositions.Where(predicate))
            {
                x += screenPositionData.WorldPosition.X;
                y += screenPositionData.WorldPosition.Y;
                z += screenPositionData.WorldPosition.Z;

                count++;
            }

            if (count > 0)
                return new Point3D(x / count, y / count, z / count);

            return new Point3D();
        }
    }
}