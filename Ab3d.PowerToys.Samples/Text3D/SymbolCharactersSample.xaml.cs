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
using Ab3d.Common.EventManager3D;
using Ab3d.Utilities;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Text3D
{
    /// <summary>
    /// Interaction logic for SymbolCharactersSample.xaml
    /// </summary>
    public partial class SymbolCharactersSample : Page
    {
        private Ab3d.Utilities.EventManager3D _eventManager3D;

        public SymbolCharactersSample()
        {
            InitializeComponent();

            _eventManager3D = new Ab3d.Utilities.EventManager3D(MainViewport);
            _eventManager3D.UsePreviewEvents = true; // This allows using left mouse button for camera rotation and for getting mouse click on TextBlockVisual3D objects


            // The texts in quotes was copied from Microsoft Work - after using Insert Symbol function.
            AddTextBlockVisuals(new Point3D(-40, 50, 0), "", fontFamily: "Wingdings");
            AddTextBlockVisuals(new Point3D(-40, 0, 0), "", fontFamily: "Wingdings");
            AddTextBlockVisuals(new Point3D(-40, -50, 0), "①②③④⑤", fontFamily: null);
        }

        private void AddTextBlockVisuals(Point3D startPosition, string text, string fontFamily = null)
        {
            Point3D position = startPosition;
            Vector3D charAdvancementVector = new Vector3D(30, 0, 0);

            foreach (var oneChar in text)
            {
                var textBlockVisual3D = new TextBlockVisual3D()
                {
                    Text = oneChar.ToString(),
                    Position = position,
                    PositionType = PositionTypes.Center,
                    TextDirection = new Vector3D(1, 0, 0),
                    UpDirection = new Vector3D(0, 1, 0),
                    Size = new Size(20, 40),
                    Foreground = Brushes.Black,
                    IsTwoSidedText = true,
                    IsBackSidedTextFlipped = false
                };

                if (!string.IsNullOrEmpty(fontFamily))
                    textBlockVisual3D.FontFamily = new FontFamily(fontFamily);

                MainViewport.Children.Add(textBlockVisual3D);


                var visualEventSource3D = new VisualEventSource3D(textBlockVisual3D);
                visualEventSource3D.MouseEnter += delegate(object sender, Mouse3DEventArgs e)
                {
                    var hitTextBlockVisual3D = e.HitObject as TextBlockVisual3D;
                    if (hitTextBlockVisual3D != null && hitTextBlockVisual3D.Foreground != Brushes.Red)
                        hitTextBlockVisual3D.Foreground = Brushes.Orange;

                    Mouse.OverrideCursor = Cursors.Hand;
                };

                visualEventSource3D.MouseLeave += delegate(object sender, Mouse3DEventArgs e)
                {
                    var hitTextBlockVisual3D = e.HitObject as TextBlockVisual3D;
                    if (hitTextBlockVisual3D != null && hitTextBlockVisual3D.Foreground != Brushes.Red)
                        hitTextBlockVisual3D.Foreground = Brushes.Black;

                    Mouse.OverrideCursor = null;
                };

                visualEventSource3D.MouseClick += delegate(object sender, MouseButton3DEventArgs e)
                {
                    var hitTextBlockVisual3D = e.HitObject as TextBlockVisual3D;
                    if (hitTextBlockVisual3D != null)
                    {
                        if (hitTextBlockVisual3D.Foreground != Brushes.Red)
                            hitTextBlockVisual3D.Foreground = Brushes.Red;
                        else
                            hitTextBlockVisual3D.Foreground = Brushes.Orange;
                    }
                };

                _eventManager3D.RegisterEventSource3D(visualEventSource3D);


                position += charAdvancementVector;
            }
        }
    }
}
