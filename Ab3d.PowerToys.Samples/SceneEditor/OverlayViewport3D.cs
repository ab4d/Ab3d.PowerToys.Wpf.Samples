using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using Ab3d.Cameras;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.SceneEditor
{
    /// <summary>
    /// OverlayViewport3D is a Viewport3D that can be added over the Viewport3D object and can show MouseMoverVisual3D and other 3D objects
    /// that are rendered on top of 3D objects. The OverlayViewport3D has its camera and size synchronized with the Viewport3D. 
    /// </summary>
    public class OverlayViewport3D : Viewport3D
    {
        #region ParentViewport3DProperty
        /// <summary>
        /// ParentViewport3DProperty
        /// </summary>
        public static readonly DependencyProperty ParentViewport3DProperty = DependencyProperty.Register("ParentViewport3D", typeof(Viewport3D), typeof(OverlayViewport3D));

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
        #endregion


        private DirectionalLight _directionalLight;

        private ModelMoverVisual3D _modelMoverVisual3D;

        public Action ModelMoveStarted { get; set; }
        public Action<Vector3D> ModelMoved { get; set; }
        public Action ModelMoveEnded { get; set; }

        public Point3D ModelMoverPosition
        {
            get
            {
                if (_modelMoverVisual3D == null)
                    throw new Exception("Cannot get ModelMoverPosition because ModelMoverVisual3D is not setup.");

                return _modelMoverVisual3D.Position;
            }

            set
            {
                if (_modelMoverVisual3D == null)
                    throw new Exception("Cannot set ModelMoverPosition because ModelMoverVisual3D is not setup.");

                _modelMoverVisual3D.Position = value;
            }
        }


        public OverlayViewport3D()
        {

        }

        private void UpdateDirectionalLight()
        {
            if (_directionalLight == null)
            {
                _directionalLight = new DirectionalLight();

                var modelVisual3D = new ModelVisual3D();
                modelVisual3D.Content = _directionalLight;

                this.Children.Add(modelVisual3D);
            }


            var parentViewport3DCamera = this.ParentViewport3D.Camera;
            
            var projectionCamera = parentViewport3DCamera as ProjectionCamera;

            if (projectionCamera != null) // MatrixCamera is currently not supported
                _directionalLight.Direction = projectionCamera.LookDirection;



            // Update currently subscribed camera if changed
            var currentCamera = this.Camera;
            if (!ReferenceEquals(parentViewport3DCamera, currentCamera))
            {
                if (currentCamera != null && !currentCamera.IsFrozen)
                    currentCamera.Changed -= OnCameraChanged;

                if (parentViewport3DCamera != null && !parentViewport3DCamera.IsFrozen)
                    parentViewport3DCamera.Changed += OnCameraChanged;

                this.Camera = parentViewport3DCamera;
            }
        }

        private void OnCameraChanged(object sender, EventArgs e)
        {
            UpdateDirectionalLight();
        }

        public void HideModelMover()
        {
            this.Children.Clear();

            _directionalLight = null;
            _modelMoverVisual3D = null;
        }

        public void SetupModelMover()
        {
            if (_modelMoverVisual3D == null)
            {
                _modelMoverVisual3D = new ModelMoverVisual3D();

                // Setup event handlers on ModelMoverVisual3D
                _modelMoverVisual3D.ModelMoveStarted += delegate (object o, EventArgs eventArgs)
                {
                    if (ModelMoveStarted != null)
                        ModelMoveStarted();
                };

                _modelMoverVisual3D.ModelMoved += delegate (object o, Ab3d.Common.ModelMovedEventArgs e)
                {
                    if (ModelMoved != null)
                        ModelMoved(e.MoveVector3D);
                };

                _modelMoverVisual3D.ModelMoveEnded += delegate (object sender, EventArgs args)
                {
                    if (ModelMoveEnded != null)
                        ModelMoveEnded();
                };

                this.Children.Add(_modelMoverVisual3D);
            }

            UpdateDirectionalLight();
        }

        public void UpdateModelMoverSize(double desiredScreenLength)
        {
            if (_modelMoverVisual3D == null)
                return;

            Size requiredSize;

            var projectionCamera = this.Camera as ProjectionCamera;

            if (projectionCamera == null) // MatrixCamera is currently not supported
                return;

            double modelMoverDistance = (projectionCamera.Position - _modelMoverVisual3D.Position).Length;

            Size viewport3DSize = new Size(this.ActualWidth, this.ActualHeight);

            var perspectiveCamera = projectionCamera as PerspectiveCamera;

            if (perspectiveCamera != null)
            {
                requiredSize = CameraUtils.GetPerspectiveWorldSize(new Size(desiredScreenLength, 0), modelMoverDistance, perspectiveCamera.FieldOfView, viewport3DSize);
            }
            else
            {
                var orthographicCamera = projectionCamera as OrthographicCamera;
                if (orthographicCamera != null)
                    requiredSize = CameraUtils.GetOrthographicWorldSize(new Size(desiredScreenLength, 0), orthographicCamera.Width, viewport3DSize);
                else
                    requiredSize = new Size(1, 1);
            }

            _modelMoverVisual3D.AxisLength = requiredSize.Width;
            _modelMoverVisual3D.AxisRadius = _modelMoverVisual3D.AxisLength * 0.02; // 2% of the length
            _modelMoverVisual3D.AxisArrowRadius = _modelMoverVisual3D.AxisRadius * 3;
        }
    }
}