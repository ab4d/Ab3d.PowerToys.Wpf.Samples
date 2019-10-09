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
using Ab3d.Common.EventManager3D;
using Ab3d.ObjFile;
using Ab3d.Utilities;
using Path = System.IO.Path;

namespace Ab3d.PowerToys.Samples.UseCases
{
    /// <summary>
    /// Interaction logic for WireframeSelection.xaml
    /// </summary>
    public partial class WireframeSelection : Page
    {
        private Model3D _rootModel;
        private string _loadedFileName;
        private Ab3d.Utilities.EventManager3D _eventManager3D;

        public WireframeSelection()
        {
            InitializeComponent();

            _eventManager3D = new Ab3d.Utilities.EventManager3D(MainViewport);

            this.Loaded += (sender, args) => LoadDefaultModel();
        }

        private void LoadDefaultModel()
        {
            //// PersonModel and HouseWithTreesModel are defined in App.xaml
            //var houseWithTreesModel = this.FindResource("HouseWithTreesModel") as Model3D;
            //_rootModel = houseWithTreesModel;

            //SetModel(houseWithTreesModel);

            //string fileName = AppDomain.CurrentDomain.BaseDirectory + @"Resources\ObjFiles\ship_boat.obj";
            string fileName = AppDomain.CurrentDomain.BaseDirectory + @"Resources\ObjFiles\house with trees.obj";
            LoadModel(fileName);
        }

        private void LoadModel(string fileName)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            _loadedFileName = fileName;

            var readerObj = new Ab3d.ReaderObj();
            var rootModel = readerObj.ReadModel3D(fileName);

            SetModel(rootModel);

            Mouse.OverrideCursor = null;
        }

        private void SetModel(Model3D model)
        {
            Point3D center;
            double size;

            GetModelCenterAndSize(model, out center, out size);

            Camera1.TargetPosition = center;
            Camera1.Distance = size * 2;

            if (!(PreserveModelColorCheckBox.IsChecked ?? false))
            {
                if (model.IsFrozen)
                    model = model.Clone();

                Ab3d.Utilities.ModelUtils.ChangeMaterial(model, new DiffuseMaterial(Brushes.White), newBackMaterial: null);
            }

            _rootModel = model;

            OriginalModelVisual.Content= model;
            SelectObject(null);

            var modelEventSource3D = new ModelEventSource3D(model);
            modelEventSource3D.MouseLeave += ModelEventSource3DOnMouseLeave;
            modelEventSource3D.MouseMove += ModelEventSource3DOnMouseMove;

            _eventManager3D.ResetEventSources3D();
            _eventManager3D.RegisterEventSource3D(modelEventSource3D);

            // Exclude WireframeVisual from hit testing
            _eventManager3D.RegisterExcludedVisual3D(WireframeVisual);
        }

        private void ModelEventSource3DOnMouseMove(object sender, Mouse3DEventArgs mouse3DEventArgs)
        {
            var modelHit = mouse3DEventArgs.RayHitResult.ModelHit as GeometryModel3D;

            //((GeometryModel3D)modelHit).Material = new DiffuseMaterial(Brushes.Red);

            SelectObject(modelHit);
        }

        private void ModelEventSource3DOnMouseLeave(object sender, Mouse3DEventArgs mouse3DEventArgs)
        {
            SelectObject(null);
        }

        private void SelectObject(Model3D model)
        {
            if (ReferenceEquals(WireframeVisual.OriginalModel, model))
                return;

            WireframeVisual.OriginalModel = model;

            if (model != null)
            {
                var modelTotalTransform = Ab3d.Utilities.TransformationsHelper.GetModelTotalTransform(_rootModel, model, addFinalModelTransformation: false);
                WireframeVisual.Transform = modelTotalTransform;
            }
        }

        private void GetModelCenterAndSize(Model3D model, out Point3D center, out double size)
        {
            Rect3D bounds;

            bounds = model.Bounds;

            center = new Point3D(bounds.X + bounds.SizeX / 2,
                                 bounds.Y + bounds.SizeY / 2,
                                 bounds.Z + bounds.SizeZ / 2);

            size = Math.Sqrt(bounds.SizeX * bounds.SizeX +
                             bounds.SizeY * bounds.SizeY +
                             bounds.SizeZ * bounds.SizeZ);
        }

        private void PreserveModelColorCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_loadedFileName != null)
                LoadModel(_loadedFileName);
            else
                LoadDefaultModel();
        }
    }
}
