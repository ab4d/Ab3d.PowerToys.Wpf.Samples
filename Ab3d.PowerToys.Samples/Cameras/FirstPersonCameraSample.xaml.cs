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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab3d.Common.EventManager3D;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for FirstPersonCameraSample.xaml
    /// </summary>
    public partial class FirstPersonCameraSample : Page
    {
        private Material _standardMaterial;
        private Material _selectedMaterial;

        private Ab3d.Visuals.BaseModelVisual3D _selectedModel;

        public FirstPersonCameraSample()
        {
            InitializeComponent();

            this.Focusable = true; // by default Page is not focusable and therefore does not recieve keyDown event
            this.PreviewKeyDown += OnPreviewKeyDown; // Use PreviewKeyDown to get arrow keys also (KeyDown event does not get them)
            this.Focus();


            _standardMaterial = new DiffuseMaterial(Brushes.DeepSkyBlue);
            _standardMaterial.Freeze();

            _selectedMaterial = new DiffuseMaterial(Brushes.Yellow);
            _selectedMaterial.Freeze();

            CreateSceneObjects();
        }

        private void CreateSceneObjects()
        {
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    var boxVisual3D = new Ab3d.Visuals.BoxVisual3D()
                    {
                        CenterPosition = new Point3D(-100 + 20 * x, 50 + y * 30, 0),
                        Size = new Size3D(10, 10, 10),
                        Material = _standardMaterial
                    };

                    if (x == 4 && y == 2)
                    {
                        boxVisual3D.Material = _selectedMaterial;
                        _selectedModel = boxVisual3D;
                    }

                    SelectionRootModelVisual3D.Children.Add(boxVisual3D);
                }
            }

            var eventManager3D = new Ab3d.Utilities.EventManager3D(MainViewport);

            var multiVisualEventSource3D = new MultiVisualEventSource3D(SelectionRootModelVisual3D.Children);

            multiVisualEventSource3D.MouseEnter += delegate(object sender, Mouse3DEventArgs args)
            {
                Mouse.OverrideCursor = Cursors.Hand;
            };

            multiVisualEventSource3D.MouseLeave += delegate(object sender, Mouse3DEventArgs args)
            {
                Mouse.OverrideCursor = null;
            };

            multiVisualEventSource3D.MouseClick += delegate (object sender, MouseButton3DEventArgs e)
            {
                if (_selectedModel != null)
                    _selectedModel.Material = _standardMaterial;

                var baseModelVisual3D = e.HitObject as Ab3d.Visuals.BaseModelVisual3D;
                if (baseModelVisual3D != null)
                {
                    baseModelVisual3D.Material = _selectedMaterial;
                    _selectedModel = baseModelVisual3D;
                }
                else
                {
                    _selectedModel = null;
                }

                string logText = string.Format("Camera1.TurnTo(new Point3D({0:0}, {1:0}, {2:0})", e.FinalPointHit.X, e.FinalPointHit.Y, e.FinalPointHit.Z);

                if (AnimateCheckBox.IsChecked ?? false)
                {
                    // Animate the camera turn. Animation duration is 0.5 second and use QuadraticEaseInOutFunction for easing.
                    Camera1.TurnTo(position: e.FinalPointHit, 
                                   animationDurationInMilliseconds: 500, 
                                   easingFunction: Ab3d.Animation.EasingFunctions.QuadraticEaseInOutFunction);

                    logText += ", 500, QuadraticEaseInOutFunction)";
                }
                else
                {
                    Camera1.TurnTo(e.FinalPointHit); // Turn immediately
                    logText += ")";
                }

                LogCommandText(logText);

                // You can also call this with direction vector - for example:

                //var directionVector = e.FinalPointHit - Camera1.Position;
                //Camera1.TurnTo(directionVector);
            };

            eventManager3D.RegisterEventSource3D(multiVisualEventSource3D);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                case Key.Left:
                    // left
                    if (IsMoveAllowed(0, -5))
                    {
                        Camera1.MoveLeft(5);
                        LogCommandText("Camera1.MoveLeft(5);");
                        e.Handled = true;
                    }

                    break;

                case Key.D:
                case Key.Right:
                    // right
                    if (IsMoveAllowed(0, 5))
                    {
                        Camera1.MoveRight(5);
                        LogCommandText("Camera1.MoveRight(5);");
                        e.Handled = true;
                    }

                    break;

                case Key.W:
                case Key.Up:
                    // forward
                    if (IsMoveAllowed(10, 0))
                    {
                        Camera1.MoveForward(10);
                        LogCommandText("Camera1.MoveForward(10);");
                        e.Handled = true;
                    }

                    break;

                case Key.S:
                case Key.Down:
                    // backward
                    if (IsMoveAllowed(-10, 0))
                    {
                        Camera1.MoveBackward(10);
                        LogCommandText("Camera1.MoveBackward(10);");
                        e.Handled = true;
                    }

                    break;
            }
        }

        private bool IsMoveAllowed(double forwardDistance, double strafeDistance)
        {
            // Check if the camera will be moved over the border of RoomBoxVisual3D

            // First we need to get the strafe vector
            var lookDirection = Camera1.LookDirection;
            lookDirection.Normalize();

            var strafeDirection = Vector3D.CrossProduct(Camera1.LookDirection, Camera1.UpDirection);
            strafeDirection.Normalize();

            var cameraMoveVector = lookDirection * forwardDistance + strafeDirection * strafeDistance;
            var newCameraPosition = Camera1.Position + cameraMoveVector;

            var roomBoundingBox = RoomBoxVisual3D.Content.Bounds;

            bool isCameraInRoom = roomBoundingBox.Contains(newCameraPosition);

            return isCameraInRoom;
        }

        private void LogCommandText(string commandText)
        {
            CommandsTextBox.Text = commandText + Environment.NewLine + CommandsTextBox.Text;
        }
    }
}
