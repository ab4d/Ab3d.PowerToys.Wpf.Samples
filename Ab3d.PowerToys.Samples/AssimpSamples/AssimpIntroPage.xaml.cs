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
using Ab3d.Assimp;

namespace Ab3d.PowerToys.Samples.AssimpSamples
{
    /// <summary>
    /// Interaction logic for AssimpIntroPage.xaml
    /// </summary>
    public partial class AssimpIntroPage : Page
    {
        public AssimpIntroPage()
        {
            InitializeComponent();

            // We create an instance of AssimpWpfImporter to read all supported file formats
            // and set that to the FileFormatsTextBlock

            // Use helper class (defined in this sample project) to load the native assimp libraries
            // IMPORTANT: See commend in the AssimpLoader class for details on how to prepare your project to use assimp library.
            AssimpLoader.LoadAssimpNativeLibrary();

            var assimpWpfImporter = new AssimpWpfImporter();

            string[] supportedImportFormats = assimpWpfImporter.SupportedImportFormats;
            FileFormatsTextBlock.Text = string.Join(", ", supportedImportFormats);
        }
    }
}
