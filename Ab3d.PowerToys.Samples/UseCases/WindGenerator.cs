using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.UseCases
{
    public class WindGenerator : ICompositionRenderingSubscriber
    {
        private DateTime _lastTime;

        private static GeometryModel3D _baseModelStatic;
        private static GeometryModel3D _turbineModelStatic;
        private static Model3DGroup _bladesModelGroupStatic;

        private Model3DGroup _model;
        private Model3D _base;
        private Model3DGroup _turbine;
        private Model3DGroup _blades;

        private TranslateTransform3D _baseTranslate;
        private ScaleTransform3D _baseScale;
        private AxisAngleRotation3D _baseRotation;
        private AxisAngleRotation3D _turbineRotation;
        private AxisAngleRotation3D _bladesRotation;

        public bool IsAnimated { get; set; }

        public double WindSpeed { get; set; }

        public double RotationVelocity { get; private set; }

        public double Acceleration { get; private set; }

        public double Drag { get; private set; }


        public Model3DGroup Model
        {
            get
            {
                EnsureModel();
                return _model;
            }
        }

        public Model3D BaseModel
        {
            get
            {
                EnsureModel();
                return _base;
            }
        }

        public Model3D TurbineModel
        {
            get
            {
                EnsureModel();
                return _turbine;
            }
        }

        public Model3D BladesModel
        {
            get
            {
                EnsureModel();
                return _blades;
            }
        }

        public Point3D Position
        {
            get
            {
                EnsureModel();
                return new Point3D(_baseTranslate.OffsetX, _baseTranslate.OffsetY, _baseTranslate.OffsetZ);
            }

            set
            {
                EnsureModel();

                _baseTranslate.OffsetX = value.X;
                _baseTranslate.OffsetY = value.Y;
                _baseTranslate.OffsetZ = value.Z;
            }
        }

        /// <summary>
        /// Gets or sets the height of the Wind Generator base
        /// </summary>
        public double Height
        {
            get
            {
                EnsureModel();
                return _model.Bounds.SizeY;
            }

            set
            {
                ScaleModelToSize(value);
            }
        }

        public WindGenerator()
        {
        }

        private void EnsureModel()
        {
            if (_model != null)
                return;

            // Ensure that the static 3d models are created
            EnsureStaticModels();

            
            // Construct the wind generator from its componets


            // Start with the blades and add AxisAngleRotation3D to them
            _blades = new Model3DGroup();
            _blades.Children.Add(_bladesModelGroupStatic);

            _bladesRotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
            _blades.Transform = new RotateTransform3D(_bladesRotation, GetObjectCenter(_turbineModelStatic));


            // Create turbine model
            _turbine = new Model3DGroup();
            _turbine.Children.Add(_turbineModelStatic);

            // Add blades model as child of turbine
            _turbine.Children.Add(_blades);

            _turbineRotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
            _turbine.Transform = new RotateTransform3D(_turbineRotation);

            
            // Create base model (the tower)

            // IMPORTANT !!!
            // We need to clone the wind generator's base model so the events go to the propert EventSource3D
            // If all the wind generators would use the same 3d model all events would go to the first EventSource3D
            _base = _baseModelStatic.Clone();


            // Create a new Model3DGroup and add base and turbine models to it
            _model = new Model3DGroup();
            _model.Children.Add(_base);
            _model.Children.Add(_turbine);

            // Create the base transformations
            _baseTranslate = new TranslateTransform3D();
            _baseScale = new ScaleTransform3D(1, 1, 1);
            _baseRotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);

            Transform3DGroup baseTransform;
            baseTransform = new Transform3DGroup();
            baseTransform.Children.Add(new RotateTransform3D(_baseRotation));
            baseTransform.Children.Add(_baseScale);
            baseTransform.Children.Add(_baseTranslate);

            _model.Transform = baseTransform;
        }

        // Get the models of Wind Generator and store them into static fields
        private static void EnsureStaticModels()
        {
            if (_baseModelStatic == null)
            {
                ResourceDictionary dictionary;

                dictionary = new ResourceDictionary();
                dictionary.Source = new Uri("/Resources/ModelsDictionary.xaml", UriKind.RelativeOrAbsolute);

                _baseModelStatic = dictionary["WindGenerator_Base"] as GeometryModel3D;
                _turbineModelStatic = dictionary["WindGenerator_Turbine"] as GeometryModel3D;
                _bladesModelGroupStatic = dictionary["WindGenerator_Blades"] as Model3DGroup;
            }
        }

        // Start the CompositionTarget.Rendering
        public void StartBladesRotation()
        {
            if (this.IsAnimated)
                return;

            _lastTime = DateTime.Now;

            // Use CompositionRenderingHelper to subscribe to CompositionTarget.Rendering event
            // This is much safer because in case we forget to unsubscribe from Rendering, the CompositionRenderingHelper will unsubscribe us automatically
            // This allows to collect this class will Grabage collector and prevents infinite calling of Rendering handler.
            // After subscribing the ICompositionRenderingSubscriber.OnRendering method will be called on each CompositionTarget.Rendering event
            CompositionRenderingHelper.Instance.Subscribe(this);

            IsAnimated = true;
        }

        // Stop the CompositionTarget.Rendering
        public void StopBladesRotation()
        {
            if (!this.IsAnimated)
                return;

            CompositionRenderingHelper.Instance.Unsubscribe(this);

            IsAnimated = false;
        }

        // Do the blades rotation based on the current WindSpeed
        void ICompositionRenderingSubscriber.OnRendering(EventArgs e)
        {
            double fractionOfSecond;
            double totalAcceleration;

            fractionOfSecond = (DateTime.Now - _lastTime).TotalSeconds; // fraction of second between this and previous Tick event
            _lastTime = DateTime.Now;

            Acceleration = WindSpeed / 3;
            Drag = RotationVelocity / 2;

            totalAcceleration = (Acceleration - Drag) * fractionOfSecond;

            RotationVelocity += totalAcceleration;

            if (RotationVelocity < 1.0 && totalAcceleration < 0)
                RotationVelocity = 0; // if we were deceleration and the current velocity is less than 1 degree per second than stop

            _bladesRotation.Angle += RotationVelocity * fractionOfSecond;
        }

        // Scales model to the newSize height
        private void ScaleModelToSize(double newSize)
        {
            double newScale;

            EnsureModel();

            newScale = newSize / _baseModelStatic.Bounds.SizeY;

            _baseScale.ScaleX = newScale;
            _baseScale.ScaleY = newScale;
            _baseScale.ScaleZ = newScale;
        }

        // Get the center of oneObject
        private Point3D GetObjectCenter(Model3D oneObject)
        {
            Point3D viewportCenter;
            Rect3D viewportBounds;

            if (oneObject == null)
                return new Point3D();

            viewportBounds = oneObject.Bounds;

            viewportCenter = new Point3D();

            viewportCenter.X = viewportBounds.X + viewportBounds.SizeX / 2;
            viewportCenter.Y = viewportBounds.Y + viewportBounds.SizeY / 2;
            viewportCenter.Z = viewportBounds.Z + viewportBounds.SizeZ / 2;

            return viewportCenter;
        }
    }
}
