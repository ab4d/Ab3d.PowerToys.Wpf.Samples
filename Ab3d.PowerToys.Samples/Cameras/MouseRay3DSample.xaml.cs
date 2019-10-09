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

namespace Ab3d.PowerToys.Samples.Cameras
{
    /// <summary>
    /// Interaction logic for MouseRay3DSample.xaml
    /// </summary>
    public partial class MouseRay3DSample : Page
    {
        private Point3D _pointOnPlane;
        private Vector3D _planeNormal;

        public MouseRay3DSample()
        {
            InitializeComponent();

            //ChangePlane(pointOnPlane: new Point3D(0, 0, 0), planeNormal: new Vector3D(0, 1, 0), rectangleHeightDirection: new Vector3D(0, 0, 1));
        }

        private void OverlayBorder_MouseMove(object sender, MouseEventArgs e)
        {
            UpdateSpherePosition(e);
        }

        private void OverlayBorder_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            UpdateSpherePosition(e);
        }

        private void UpdateSpherePosition(MouseEventArgs e)
        { 
            Point mousePosition = e.GetPosition(MainViewport);
            MousePositionValueTextBlock.Text = string.Format("{0:0}", mousePosition);

            Point3D intersectionPoint;

            // Get intersection of ray created from mouse position and the current plane
            bool hasIntersection = Camera1.GetMousePositionOnPlane(mousePosition, _pointOnPlane, _planeNormal, out intersectionPoint);

            // The GetMousePositionOnPlane uses the CreateMouseRay3D that creates a ray from a current camera and mouse position.
            // You can also use that method file the following code:
            //Point3D rayOrigin;
            //Vector3D rayDirection;

            //// Calculate the 3D ray that goes from the mouse position into the 3D scene
            //bool success = Camera1.CreateMouseRay3D(mousePosition, out rayOrigin, out rayDirection);


            if (hasIntersection)
            {
                double planeLimits = PlaneVisual.Size.Width / 2;

                // We limit the area where we can position the sphere to the area defined by PlaneVisual
                if (Math.Abs(intersectionPoint.Z) > planeLimits || Math.Abs(intersectionPoint.X) > planeLimits)
                {
                    PlanePositionValueTextBlock.Text = "(out of bounds)";
                    Sphere1.IsVisible = false;
                }
                else
                {
                    // Position the sphere
                    // NOTE:
                    // We are using transform to position the sphere
                    // This is much more efficient than changing the Sphere's CenterPosition - this would recreate the whole geometry
                    SphereTransform.OffsetX = intersectionPoint.X;
                    SphereTransform.OffsetY = intersectionPoint.Y;
                    SphereTransform.OffsetZ = intersectionPoint.Z;

                    Sphere1.IsVisible = true;

                    PlanePositionValueTextBlock.Text = string.Format("{0:0}", intersectionPoint);
                }
            }
            else
            {
                Sphere1.IsVisible = false;
                PlanePositionValueTextBlock.Text = "(no intersection)";
            }
        }

        // We also need rectangelHeightDirection because we are actually rendering a 2D rectangle and not a real infinitive plane
        // and the rectangle need a Vector3D that specifies the direction of its height (defined in Size)
        private void ChangePlane(Point3D pointOnPlane, Vector3D planeNormal, Vector3D rectangelHeightDirection)
        {
            _pointOnPlane = pointOnPlane;
            _planeNormal = planeNormal;

            PlaneVisual.BeginInit();
            
            PlaneVisual.CenterPosition = pointOnPlane;
            PlaneVisual.Normal = planeNormal;
            PlaneVisual.HeightDirection = rectangelHeightDirection;

            PlaneVisual.EndInit();
        }

        // P = (0, 0, 0); N = (0, 1, 0)
        private void Plane1RadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            ChangePlane(pointOnPlane: new Point3D(0, 0, 0), planeNormal: new Vector3D(0, 1, 0), rectangelHeightDirection: new Vector3D(0, 0, 1));
        }

        // P = (0, 50, 0); N = (0, 1, 0)
        private void Plane2RadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            ChangePlane(pointOnPlane: new Point3D(0, 50, 0), planeNormal: new Vector3D(0, 1, 0), rectangelHeightDirection: new Vector3D(0, 0, 1));
        }

        // P = (0, 100, 0); N = (0, 1, 0)
        private void Plane3RadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            ChangePlane(pointOnPlane: new Point3D(0, 100, 0), planeNormal: new Vector3D(0, 1, 0), rectangelHeightDirection: new Vector3D(0, 0, 1));
        }

        // P = (0, 30, 0); N = (0, 0.71, 0.71)
        private void Plane4RadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            ChangePlane(pointOnPlane: new Point3D(0, 30, 0), planeNormal: new Vector3D(0, 0.71, 0.71), rectangelHeightDirection: new Vector3D(0, 0.71, -0.71));
        }

        // P = (0, 0, 0); N = (0, 0, 1)
        private void Plane5RadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            ChangePlane(pointOnPlane: new Point3D(0, 0, 0), planeNormal: new Vector3D(0, 0, 1), rectangelHeightDirection: new Vector3D(0, 1, 0));
        }
    }
}
