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
using Ab3d.Common;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    /// <summary>
    /// Interaction logic for MiterLimitSample.xaml
    /// </summary>
    public partial class MiterLimitSample : Page
    {
        public MiterLimitSample()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            AddMiterLimitsSample(0, -100);
            AddMiterLimitsSample(2, 0);
            AddMiterLimitsSample(4, 100);
            AddMiterLimitsSample(10, 200);
        }

        private void AddMiterLimitsSample(double miterLimit, double zOffset)
        {
            var sampleModelVisual3D = new ModelVisual3D();
            sampleModelVisual3D.Transform = new TranslateTransform3D(0, 0, zOffset);


            var positions = CreateSnakePositions(new Point3D(-100, 0, 0), 50, 20, 80);
            var polyLineVisual3D = new Ab3d.Visuals.PolyLineVisual3D()
            {
                Positions = positions,
                LineColor = Colors.White,
                LineThickness = 15,
                MiterLimit = miterLimit
            };

            sampleModelVisual3D.Children.Add(polyLineVisual3D);


            var textBlockVisual3D = new TextBlockVisual3D()
            {
                Position = new Point3D(-120, 0, 0),
                PositionType = PositionTypes.Right,
                TextDirection = new Vector3D(1, 0, 0),
                UpDirection = new Vector3D(0, 0, -1),
                Size = new Size(200, 40),
                Foreground = Brushes.White,
                Text = string.Format("MiterLimit = {0}", miterLimit)
            };

            sampleModelVisual3D.Children.Add(textBlockVisual3D);


            MainViewport.Children.Add(sampleModelVisual3D);
        }

        private Point3DCollection CreateSnakePositions(Point3D startPosition, double segmentsLength, double startAngle, double maxAngle)
        {
            var point3DCollection = new Point3DCollection();
            point3DCollection.Add(startPosition);

            double angle = startAngle;
            bool negateAngle = false;

            var currentPosition = startPosition;

            var initialDirectionVector = new Vector3D(segmentsLength, 0, 0);

            var axisAngleRotation3D = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
            var rotateTransform3D = new RotateTransform3D(axisAngleRotation3D);

            while (angle <= maxAngle)
            {
                axisAngleRotation3D.Angle = negateAngle ? -angle : angle;
                var currentDirectionVector = rotateTransform3D.Transform(initialDirectionVector);

                currentPosition += currentDirectionVector;

                point3DCollection.Add(currentPosition);

                angle += 5;
                negateAngle = !negateAngle;
            }

            return point3DCollection;
        }
    }
}
