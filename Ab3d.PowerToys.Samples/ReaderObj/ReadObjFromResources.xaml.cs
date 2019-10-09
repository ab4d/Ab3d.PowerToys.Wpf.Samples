using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;

namespace Ab3d.PowerToys.Samples.ReaderObj
{
    /// <summary>
    /// Interaction logic for ReadObjFromResources.xaml
    /// </summary>
    public partial class ReadObjFromResources : Page
    {
        private const string _baseResourceUrl = "pack://application:,,,/Ab3d.PowerToys.Samples;component/Resources/ObjFiles/";

        public ReadObjFromResources()
        {
            InitializeComponent();

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            LoadFromResource("robotarm.obj");
        }


        private void LoadFromResource(string resourceName)
        {
            var objImporter = new Ab3d.ReaderObj();
            Stream resourceStream = GetResourceStream(resourceName);

            try
            {
                // Read 3D models from obj file into wpf3DModel
                // We also use GetResourceStream method to allow reading any additional resources (.mtf file with definition for materials and texture image files)
                var wpf3DModel = objImporter.ReadModel3D(resourceStream, GetResourceStream);

                // Show the model
                ContentVisual.Content = wpf3DModel;

                // Refresh the SceneCamera to measure the scene again and adjust itself accordingly
                Camera1.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\r\n" + ex.Message);
            }
        }

        // OLD version that first read obj file into ObjFileData and than uses ObjFileToWpfModel3DConverter to convert that into WPF Model3D
        //private void LoadFromResource(string resourceName)
        //{
        //    var objImporter = new Ab3d.ReaderObj();
        //    var objMeshToWpfModel3DConverter = new ObjFileToWpfModel3DConverter();

        //    // To read materila (.mtl) files and textures from stream we need to specify a function that resolves the file names into stream.
        //    // This need to be applied to both Ab3d.ReaderObj and Ab3d.ObjFileToWpfModel3DConverter
        //    objImporter.ResolveResourceFunc = GetResourceStream;
        //    objMeshToWpfModel3DConverter.ResolveResourceFunc = GetResourceStream;

        //    Stream resourceStream = GetResourceStream(resourceName);

        //    try
        //    {
        //        // First read data from obj and material (.mtl) files into ObjFileData
        //        Ab3d.ObjFile.ObjFileData readObjFileData = objImporter.ReadStream(resourceStream);

        //        // Than convert the ObjFileData into Model3D
        //        var wpf3DModel = objMeshToWpfModel3DConverter.Convert(readObjFileData);

        //        // Show the model
        //        ContentVisual.Content = wpf3DModel;

        //        // Refresh the SceneCamera to measure the scene again and adjust itself accordingly
        //        Camera1.Refresh();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("ERROR:\r\n" + ex.Message);
        //    }
        //}

        private Stream GetResourceStream(string resourceName)
        {
            StreamResourceInfo streamResourceInfo;

            try
            {
                streamResourceInfo = Application.GetResourceStream(new Uri(_baseResourceUrl + resourceName, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                streamResourceInfo = null;
            }

            if (streamResourceInfo != null)
                return streamResourceInfo.Stream;

            return null;
        }
    }
}
