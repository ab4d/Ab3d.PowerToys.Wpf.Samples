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

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for CamerasPropertiesSample.xaml
    /// </summary>
    public partial class CamerasPropertiesSample : Page
    {
        private double MOVEMENT_STEP = 10.0; // how much the person model is moved when an arrow key is pressed

        private TranslateTransform3D _personTranslate;

        private Model3DGroup _personModel;
        private Model3DGroup _rootModel;

        public CamerasPropertiesSample()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // PersonModel and HouseWithTreesModel are defined in App.xaml
            var originalPersonModel = this.FindResource("PersonModel") as Model3D;

            // originalPersonModel is frozen so its Transform cannot be changed - therefore we create a new _personModel that could be changed
            _personModel = new Model3DGroup();
            _personModel.Children.Add(originalPersonModel);

            _personTranslate = new TranslateTransform3D();
            _personModel.Transform = _personTranslate;

            var houseWithTreesModel = this.FindResource("HouseWithTreesModel") as Model3DGroup;

            _rootModel = new Model3DGroup();
            _rootModel.Children.Add(houseWithTreesModel);
            _rootModel.Children.Add(_personModel);

            Camera1Model.Content = _rootModel;
            Camera2Model.Content = _rootModel;
            Camera3Model.Content = _rootModel;

            MovePerson(0, 0); // sets mans position
            ThirdPersonCamera1.CenterObject = _personModel;

            this.Focusable = true; // by default Page is not focusable and therefore does not recieve keyDown event
            this.PreviewKeyDown += new KeyEventHandler(CamerasSample_PreviewKeyDown); // Use PreviewKeyDown to get arrow keys also (KeyDown event does not get them)
            this.Focus();

            Mouse.OverrideCursor = null;
        }

        void CamerasSample_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                case Key.Left:
                    // left
                    MovePerson(0, -MOVEMENT_STEP);
                    e.Handled = true;
                    break;

                case Key.D:
                case Key.Right:
                    // right
                    MovePerson(0, MOVEMENT_STEP);
                    e.Handled = true;
                    break;

                case Key.W:
                case Key.Up:
                    // forward
                    MovePerson(-MOVEMENT_STEP, 0);
                    e.Handled = true;
                    break;

                case Key.S:
                case Key.Down:
                    // backward
                    MovePerson(MOVEMENT_STEP, 0);
                    e.Handled = true;
                    break;
            }            
        }

        private void MovePerson(double dx, double dy)
        {
            _personTranslate.OffsetZ += dx;
            _personTranslate.OffsetX += dy; // y axis in 3d is pointing up

            Point3D position;

            position = new Point3D(_personModel.Bounds.X + _personModel.Bounds.SizeX / 2,
                                   _personModel.Bounds.Y + _personModel.Bounds.SizeY + 20,
                                   _personModel.Bounds.Z + _personModel.Bounds.SizeZ);

            FirstPersonCamera1.Position = position;
        }
    }
}
