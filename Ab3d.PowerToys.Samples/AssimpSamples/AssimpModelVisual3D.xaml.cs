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
    /// Interaction logic for AssimpModelVisual3D.xaml
    /// </summary>
    public partial class AssimpModelVisual3D : Page
    {
        public AssimpModelVisual3D()
        {
            // Use helper class (defined in this sample project) to load the native assimp libraries
            // IMPORTANT: See commend in the AssimpLoader class for details on how to prepare your project to use assimp library.
            AssimpLoader.LoadAssimpNativeLibrary();

            InitializeComponent();
        }
    }
}
