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

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for PositionAndScaleModel3DSample.xaml
    /// </summary>
    public partial class PositionAndScaleModel3DSample : Page
    {
        public PositionAndScaleModel3DSample()
        {
            InitializeComponent();

            CreateScene();
        }

        private void CreateScene()
        {
            var dummyModel = new Model3DGroup();

            // Let's say that we do not know the size and position of the loaded model3D.
            //
            // If we just want to show the model, the it is recommended to adjust the camera position and distance.
            // In case of using TargetPositionCamera we can use the following code to do that:

            // var model3DBounds = model3D.Bounds; // Get local accessor to avoid multiple call to DependencyProperty getters

            // double modelSize = Math.Sqrt(model3DBounds.SizeX * model3DBounds.SizeX + model3DBounds.SizeY * model3DBounds.SizeY + model3DBounds.SizeZ + model3DBounds.SizeZ);

            // var modelCenter = new Point3D(model3DBounds.X + model3DBounds.SizeX / 2,
            //                               model3DBounds.Y + model3DBounds.SizeY / 2,
            //                               model3DBounds.Z + model3DBounds.SizeZ / 2);

            // Camera1.Distance = modelSize * 2;
            // Camera1.TargetPosition = modelCenter;


            // But if you want to add the model to existing 3D objects,
            // then we need to adjust the position and size of the read model.
            //
            // The easiest way to do that is to use PositionAndScaleModel3D method from Ab3d.Utilities.ModelUtils class:
            Ab3d.Utilities.ModelUtils.PositionAndScaleModel3D(dummyModel, position: new Point3D(0, 0, 0), positionType: PositionTypes.Bottom, finalSize: new Size3D(100, 100, 100), preserveAspectRatio: true);

            // If you want to center the model, you can also use the CenterAndScaleModel3D method:
            Ab3d.Utilities.ModelUtils.CenterAndScaleModel3D(dummyModel, centerPosition: new Point3D(0, 0, 0), finalSize: new Size3D(100, 100, 100), preserveAspectRatio: true);

            // If you only need to position or scale the mode, then you can also use CenterModel3D or ScaleModel3D methods:
            Ab3d.Utilities.ModelUtils.CenterModel3D(dummyModel, centerPosition: new Point3D(0, 0, 0));
            Ab3d.Utilities.ModelUtils.ScaleModel3D(dummyModel, finalSize: new Size3D(100, 100, 100));


            // Load dragon model and show it with 3 different positions and scales
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\ObjFiles\\dragon_vrip_res3.obj");

            var readerObj = new Ab3d.ReaderObj();
            var model3D = readerObj.ReadModel3D(fileName);

            AddModel(model3D, new Point3D(-100, 0, 0), PositionTypes.Bottom, new Size3D(80, 80, 80));
            AddModel(model3D, new Point3D(-40, 60, 0), PositionTypes.Left | PositionTypes.Top, new Size3D(80, 60, 80));
            AddModel(model3D, new Point3D(100, 15, 0), PositionTypes.Center, new Size3D(80, 30, 60), preserveAspectRatio: false);
        }

        private void AddModel(Model3D originalModel3D, Point3D position, PositionTypes positionType, Size3D size, bool preserveAspectRatio = true)
        {
            // Create a new Model3DGroup that will hold the originalModel3D.
            // This allows us to have different transformation for each originalModel3D (transformation is on Model3DGroup)
            var model3DGroup = new Model3DGroup();
            model3DGroup.Children.Add(originalModel3D);

            Ab3d.Utilities.ModelUtils.PositionAndScaleModel3D(model3DGroup, position, positionType, size, preserveAspectRatio);

            // Add the model
            var modelVisual3D = new ModelVisual3D()
            {
                Content = model3DGroup
            };

            SolidObjectsVisual3D.Children.Add(modelVisual3D);



            // Now add red WireCrossVisual3D at the specified position
            var wireCrossVisual3D = new Ab3d.Visuals.WireCrossVisual3D()
            {
                Position = position,
                LinesLength = 30,
                LineColor = Colors.Red
            };

            SolidObjectsVisual3D.Children.Add(wireCrossVisual3D);


            // Now show a WireBoxVisual3D (box from 3D lines) that would represent the position, positionType and size.

            // To get the correct CenterPosition of the WireBoxVisual3D,
            // we start with creating a bounding box (Rect3D) that would be used when CenterPosition would be set to (0, 0, 0):
            var wireboxInitialBounds = new Point3D(-size.X * 0.5, -size.Y * 0.5, -size.Z * 0.5);

            // Then we use that bounding box and call GetModelTranslationVector3D method
            // that will tell us how much we need to move the bounding box so that it will be positioned at position and for positionType:
            var wireboxCenterOffset = Ab3d.Utilities.ModelUtils.GetModelTranslationVector3D(new Rect3D(wireboxInitialBounds, size), position, positionType);

            // Now we can use the result wireboxCenterOffset as a CenterPosition or a WireBoxVisual3D

            var wireBoxVisual3D = new WireBoxVisual3D()
            {
                CenterPosition = new Point3D(wireboxCenterOffset.X, wireboxCenterOffset.Y, wireboxCenterOffset.Z),
                Size = size,
                LineColor = Colors.Green,
                LineThickness = 1
            };

            SolidObjectsVisual3D.Children.Add(wireBoxVisual3D);


            // Finally we add TextBlockVisual3D to show position and size information for this model
            // Note that the TextBlockVisual3D is added to the TransparentObjectsVisual3D.
            // The reason for this is that TextBlockVisual3D uses semi-transparent background.
            // To correctly show other object through semi-transparent, the semi-transparent must be added to the scene after solid objects.
            var infoText = string.Format("Position: {0:0}\r\nPositionType: {1}\r\nSize: {2:0}", position, positionType, size);
            if (!preserveAspectRatio)
                infoText += "\r\npreserveAspectRatio: false";

            var textBlockVisual3D = new TextBlockVisual3D()
            {
                Position = new Point3D(model3DGroup.Bounds.GetCenterPosition().X, -15, 55), // Show so that X center position is the same as model center position
                PositionType = PositionTypes.Center,
                TextDirection = new Vector3D(1, 0, 0),
                UpDirection = new Vector3D(0, 1, -1), // angled at 45 degrees
                Size = new Size(80, 0), // y size will be calculated automatically based on x size (80) and size of the text

                Text = infoText,
                BorderBrush = Brushes.DimGray,
                BorderThickness = new Thickness(1, 1, 1, 1),
                Background = new SolidColorBrush(Color.FromArgb(180, 200, 200, 200)),
                TextPadding = new Thickness(5, 2, 5, 2)
            };

            TransparentObjectsVisual3D.Children.Add(textBlockVisual3D);
        }
    }
}
