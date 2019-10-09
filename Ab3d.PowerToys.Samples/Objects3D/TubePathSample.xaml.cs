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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab3d.Common.Models;

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for TubePathSample.xaml
    /// </summary>
    public partial class TubePathSample : Page
    {
        public TubePathSample()
        {
            InitializeComponent();

            var pathPositions = CreateHelixPath(startCenter: new Point3D(0, 0, 0),
                                                radius: 30,
                                                height: 100,
                                                totalDegrees: 360 * 3,
                                                totalPathPositions: 100);


            var polyLineVisual3D = new Ab3d.Visuals.PolyLineVisual3D()
            {
                Positions = pathPositions,
                LineThickness = 3,
                LineColor = Colors.Red,
                EndLineCap = LineCap.ArrowAnchor,
                IsClosed = false
            };

            MainViewport.Children.Add(polyLineVisual3D);


            var tubePathMesh3D = new Ab3d.Meshes.TubePathMesh3D(pathPositions: pathPositions, 
                                                                radius: 10, 
                                                                isTubeClosed: false, 
                                                                isPathClosed: polyLineVisual3D.IsClosed,
                                                                generateTextureCoordinates: false,
                                                                segments: 10);

            var diffuseMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(150, 128, 255, 128)));

            var geometryModel3D = new GeometryModel3D(tubePathMesh3D.Geometry, diffuseMaterial);
            geometryModel3D.BackMaterial = geometryModel3D.Material;

            var modelVisual3D = new ModelVisual3D()
            {
                Content = geometryModel3D
            };

            MainViewport.Children.Add(modelVisual3D);


            var wireframeVisual3D = new Ab3d.Visuals.WireframeVisual3D()
            {
                OriginalModel = geometryModel3D,
                LineThickness = 1,
                LineColor = Colors.Black,
                UseModelColor = false
            };

            MainViewport.Children.Add(wireframeVisual3D);




            pathPositions = new Point3DCollection(new Point3D[]
            {
                new Point3D(0, 0, 0),
                new Point3D(0, 10, 0),
                new Point3D(0, 20, 0),
                new Point3D(0, 30, 0),
                new Point3D(-20, 60, 0),
                new Point3D(-50, 100, 0),
                new Point3D(-120, 100, 0),
            });

            var openedTubePathVisual3D = CreateOpenedTubePathVisual3D(pathPositions: pathPositions,
                                                                      outerRadius: 16,
                                                                      innerRadius: 14,
                                                                      segmentsCount: 20,
                                                                      outerMaterial: new DiffuseMaterial(Brushes.Green),
                                                                      innerMaterial: new DiffuseMaterial(Brushes.DimGray));

            openedTubePathVisual3D.Transform = new TranslateTransform3D(100, 0, 100);

            MainViewport.Children.Add(openedTubePathVisual3D);
        }

        // Opened tube is a tube that has different outer and inner radius.
        public static ModelVisual3D CreateOpenedTubePathVisual3D(Point3DCollection pathPositions, double outerRadius, double innerRadius, int segmentsCount, Material outerMaterial, Material innerMaterial)
        {
            var rootModelVisual3D = new ModelVisual3D();

            // CreateOpenedTubePath is created from 4 different models:
            // 1) Outer tube: TubePathVisual3D with outer radius and Material set
            // 2) Inner tube: TubePathVisual3D with inner radius and BackMaterial - this means that triangles will be visible from inside the tube
            // 3) start tube: TubeVisual3D that will close the start of the tube - in the direction of the first path segment
            // 4) end tube: TubeVisual3D that will close the end of the tube - in the direction of the last path segment

            // 1) Outer tube: TubePathVisual3D with outer radius and Material set
            var outerTubePathVisual3D = new Ab3d.Visuals.TubePathVisual3D()
            {
                PathPositions = pathPositions,
                Radius = outerRadius,
                Segments = segmentsCount,
                Material = outerMaterial,
                IsTubeClosed = false,
                IsPathClosed = false
            };

            rootModelVisual3D.Children.Add(outerTubePathVisual3D);

            // 2) Inner tube: TubePathVisual3D with inner radius and BackMaterial - this means that triangles will be visible from inside the tube
            var innerTubePathVisual3D = new Ab3d.Visuals.TubePathVisual3D()
            {
                PathPositions = pathPositions,
                Radius = innerRadius,
                Segments = segmentsCount,
                BackMaterial = innerMaterial,
                IsTubeClosed = false,
                IsPathClosed = false
            };

            rootModelVisual3D.Children.Add(innerTubePathVisual3D);

            // 3) start tube: TubeVisual3D that will close the start of the tube - in the direction of the first path segment
            var startTubeVisual3D = new Ab3d.Visuals.TubeVisual3D()
            {
                BottomCenterPosition = pathPositions[0],
                Height = 0.01, // Current version (v7.4) does not allow to have 0 height - this will be improved in the next version
                OuterRadius = outerRadius,
                InnerRadius = innerRadius,
                Segments = segmentsCount,
                HeightDirection = pathPositions[1] - pathPositions[0], // direction of the first path segment
                Material = outerMaterial
            };

            rootModelVisual3D.Children.Add(startTubeVisual3D);

            // 4) end tube: TubeVisual3D that will close the end of the tube - in the direction of the last path segment
            var endTubeVisual3D = new Ab3d.Visuals.TubeVisual3D()
            {
                BottomCenterPosition = pathPositions[pathPositions.Count - 1],
                Height = 0.01,  // Current version (v7.4) does not allow to have 0 height - this will be improved in the next version
                OuterRadius = outerTubePathVisual3D.Radius,
                InnerRadius = innerTubePathVisual3D.Radius,
                Segments = segmentsCount,
                HeightDirection = pathPositions[pathPositions.Count - 1] - pathPositions[pathPositions.Count - 2], // direction of the last path segment
                Material = outerMaterial
            };

            rootModelVisual3D.Children.Add(endTubeVisual3D);

            return rootModelVisual3D;
        }

        // See: https://en.wikipedia.org/wiki/Helix
        public static Point3DCollection CreateHelixPath(Point3D startCenter, double radius, double height, int totalDegrees, int totalPathPositions)
        {
            double onePositionAngleRad = ((double) totalDegrees / (double) totalPathPositions) * Math.PI / 180.0;

            var positions = new Point3DCollection(totalPathPositions);

            Point3D currentCenterPoint = startCenter;
            double currentAngleRad = 0;

            Vector3D oneStepDirection = new Vector3D(0, 1, 0) * (height / (double)totalPathPositions);

            for (int i = 0; i < totalPathPositions; i++)
            {
                double x = currentCenterPoint.X + Math.Sin(currentAngleRad) * radius;
                double z = currentCenterPoint.Z + Math.Cos(currentAngleRad) * radius;

                positions.Add(new Point3D(x, currentCenterPoint.Y, z));

                currentAngleRad += onePositionAngleRad;
                currentCenterPoint += oneStepDirection;
            }

            return positions;
        }
    }
}
