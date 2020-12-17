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
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Text3D
{
    /// <summary>
    /// Interaction logic for LineWithTextSample.xaml
    /// </summary>
    public partial class LineWithTextSample : Page
    {
        public LineWithTextSample()
        {
            InitializeComponent();

            UseTextBlockVisual3DInfoControl.InfoText =
@"When checked, then LineWithTextVisual3D is using a TextBlockVisual3D to show 3D text. When unchecked then 3D lines are used to show the text (using an old plotter font).

Using TextBlockVisual3D provides much more text rendering options and allows using any font but may produce some problems because of showing semi-transparent texture (sorting objects by camera distance; or using Ab3d.DXEngine and alpha-clip threshold).";
        }

        private void OnUseTextBlockVisual3DCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;


            var useTextBlockVisual3D = UseTextBlockVisual3DCheckBox.IsChecked ?? false;
            
            foreach (var lineWithTextVisual3D in MainViewport.Children.OfType<LineWithTextVisual3D>())
            {
                lineWithTextVisual3D.UseTextBlockVisual3D = useTextBlockVisual3D;

                if (useTextBlockVisual3D)
                {
                    // When UseTextBlockVisual3D is set to true, we can change text rendering options on UsedTextBlockVisual3D property:
                    // The following properties are already set by the LineWithTextVisual3D:
                    //Text          = this.Text,
                    //Position      = textCenterPosition,
                    //PositionType  = PositionTypes.Center,
                    //TextDirection = lineDirection,
                    //UpDirection   = this.TextUpDirection,
                    //Size          = new Size(0, this.FontSize), // Set only height; width will be set automatically by the TextBlockVisual3D
                    //Foreground    = new SolidColorBrush(this.LineColor),

                    lineWithTextVisual3D.UsedTextBlockVisual3D.FontFamily = new FontFamily("Arial");
                    lineWithTextVisual3D.UsedTextBlockVisual3D.RenderBitmapSize = new Size(256, 64);
                }
            }
        }
    }
}
