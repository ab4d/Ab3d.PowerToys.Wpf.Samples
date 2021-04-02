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
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for StandardTransformSample.xaml
    /// </summary>
    public partial class StandardTransformSample : Page
    {
        // StandardTransform3D is a class that generates a MatrixTransform3D based on the translate, rotate and scale transform.
        // It can be used to have only a single transformation object for translate, rotate and scale.
        // The MatrixTransform3D is available in the StandardTransform3D.Transform property.
        //
        // Because WPF does not allow deriving your own classes from Transform3D object,
        // it is not possible to create our own class that could be set to the Model3D.Transform or Visual3D.Transform property.
        // This means that you cannot use something like:
        // model3d.Transform = new StandardTransform3D(); // not possible
        // 
        // Instead the usage patter for StandardTransform3D is to use its static StandardTransform3D.SetStandardTransform3D method to assign
        // the StandardTransform3D to Model3D or Visual3D.
        // Behind the scenes the method adds a StandardTransform3D.StandardTransform3DProperty to the Model3D or Visual3D.
        // This way we store the StandardTransform3D to the object that is using it and this way we can read the assigned
        // StandardTransform3D by using the static StandardTransform3D.GetStandardTransform3D method.
        //
        // The SetStandardTransform3D by default (default value of updateTransform3D attribute is true) also 
        // sets the Transform property of the Model3D or Visual3D to the StandardTransform3D.Transform property.
        // This property is a MatrixTransform3D that is created based on the translate, rotate and scale transform.
        //
        // When the properties of StandardTransform3D change, then this also updates the MatrixTransform3D.
        //
        // Example:
        //
        // var standardTransform3D = new StandardTransform3D()
        // {
        //     RotateY = 30,
        //     TranslateX = 100
        // };
        // 
        // // Assign standardTransform3D to model3d.Transform and store it in the model3d object
        // StandardTransform3D.SetStandardTransform3D(model3d, standardTransform3D); 
        //
        // // ...
        // 
        // // Change rotation:
        // standardTransform3D.RotateY += 10; // This will update the MatrixTransform3D (standardTransform3D.Transform) and also the model3d because its Transform is set to the same MatrixTransform3D.
        //
        // // ...
        // 
        // // Somewhere else in the application we can read the StandardTransform3D from a Model3D or Visual3D:
        // var standardTransform3D = StandardTransform3D.GetStandardTransform3D(model3);


        private StandardTransform3D _standardTransform3D;

        private Random _rnd;

        public StandardTransformSample()
        {
            InitializeComponent();

            // Read a teapot from wpf3d file
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\wpf3d\teapot-hires.wpf3d");

            var wpf3DFile = new Ab3d.Utilities.Wpf3DFile();
            var teapotModel3D = wpf3DFile.ReadFile(fileName);

            Ab3d.Utilities.ModelUtils.ChangeMaterial(teapotModel3D, new DiffuseMaterial(Brushes.Gold), newBackMaterial: null);


            // Create a single StandardTransform3D that will be able to translate, rotate and scale the read teapot.

            _standardTransform3D = new StandardTransform3D();

            // We could also create a new StandardTransform3D with some initial transformations:
            //_standardTransform3D = new StandardTransform3D()
            //{
            //    RotateY = 30,
            //    TranslateX = 100,
            //    ScaleX = 1.3
            //};

            // To assign the _standardTransform3D to teapotModel3D we use the static SetStandardTransform3D method
            StandardTransform3D.SetStandardTransform3D(teapotModel3D, _standardTransform3D, updateTransform3D: true);

            // In some other location in the application we can read the StandardTransform3D from the teapotModel3D by:
            //var standardTransform3D = StandardTransform3D.GetStandardTransform3D(teapotModel3D);


            // Assign the _standardTransform3D to the editor control:
            TransformEditor.StandardTransform3D = _standardTransform3D;


            var contentVisual3D = teapotModel3D.CreateContentVisual3D();
            MainViewport.Children.Add(contentVisual3D);


            _rnd = new Random();
        }

        private void TransformEditor_OnChanged(object sender, EventArgs e)
        {
            // Here we can add some code that we want to execute when the setting in the StandardTransform3DEditor have changed.
        }

        private void TranslateButton_OnClick(object sender, RoutedEventArgs e)
        {
            _standardTransform3D.TranslateX += _rnd.Next(40) - 20;
        }

        private void RotateButton_OnClick(object sender, RoutedEventArgs e)
        {
            _standardTransform3D.RotateY += _rnd.Next(40) - 20;
        }

        private void ScaleButton_OnClick(object sender, RoutedEventArgs e)
        {
            var scale = _rnd.NextDouble() * 0.4 + 0.8;

            // Use BeginInit / EndInit to change the transformation only once
            _standardTransform3D.BeginInit();
                _standardTransform3D.ScaleX *= scale;
                _standardTransform3D.ScaleY *= scale;
                _standardTransform3D.ScaleZ *= scale;
            _standardTransform3D.EndInit();
        }

        private void ResetButton_OnClick(object sender, RoutedEventArgs e)
        {
            _standardTransform3D.Reset();
        }
    }
}
