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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ab3d.PowerToys.Samples.Wpf3DFile
{
    /// <summary>
    /// Interaction logic for Wpf3DFileInfoControl.xaml
    /// </summary>
    public partial class Wpf3DFileInfoControl : UserControl
    {
        public Wpf3DFileInfoControl()
        {
            InitializeComponent();

            this.DataContextChanged += delegate(object sender, DependencyPropertyChangedEventArgs args)
            {
                // Get only file name without path from wpf3DFile.SourceFileName.
                // This can be surely done in a nicer way, but I am not an expert for DataBinding...

                var wpf3DFile = this.DataContext as Ab3d.Utilities.Wpf3DFile;

                if (wpf3DFile != null)
                {
                    if (wpf3DFile.SourceFileName != null)
                        FileNameTextBlock.Text = System.IO.Path.GetFileName(wpf3DFile.SourceFileName);

                    TrianglesTextBlock.Text = string.Format("Triangles: {0:#,##0}", wpf3DFile.TotalTriangleIndices / 3);
                }
            };
        }
    }
}
