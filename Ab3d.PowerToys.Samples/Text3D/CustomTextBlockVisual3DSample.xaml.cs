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
    /// Interaction logic for CustomTextBlockVisual3DSample.xaml
    /// </summary>
    public partial class CustomTextBlockVisual3DSample : Page
    {
        public CustomTextBlockVisual3DSample()
        {
            InitializeComponent();

            // In case TextBlockVisual1.Text property is set in XAML, we need to clear the Inline created from the Text.
            // In our case this is not needed but is here just in case XAML is changed.
            TextBlockVisual1.Inlines.Clear(); 

            // When using Inlines you can show different parts of the text with different settings.
            // TextBlockVisual.Inlines property gets access to the Inlines property of the TextBlock.
            TextBlockVisual1.Inlines.Add(new Run("Simple text\r\n"));
            TextBlockVisual1.Inlines.Add(new Run("customization") { FontWeight = FontWeights.Bold });
            TextBlockVisual1.Inlines.Add(new Run("\r\nis possible\r\nwith using"));
            TextBlockVisual1.Inlines.Add(new LineBreak());
            TextBlockVisual1.Inlines.Add(new Run("Inlines" ) { FontWeight = FontWeights.Bold, Foreground = Brushes.Red });
            TextBlockVisual1.Inlines.Add(new Run("." ));
            TextBlockVisual1.Inlines.Add(new LineBreak());
            TextBlockVisual1.Inlines.Add(new Run("(see code behind)" ) { FontSize = 6 });

            // It is important to call Refresh method after setting Inlines.
            // This will measure the size of used TextBlock and update used elements and models.
            TextBlockVisual1.Refresh();
        }
    }
    
    public class RoundedTextBlockVisual3D : TextBlockVisual3D
    {
        // We can reuse the Border.CornerRadiusProperty.
        // Set default value to new CornerRadius(3).
        // On every change we call the OnBorderPropertyChanged method from parent TextBlockVisual3D - this will update the border and render to bitmap if needed.

        /// <summary>
        /// CornerRadiusProperty
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner(
                                                                                        typeof(RoundedTextBlockVisual3D), 
                                                                                        new FrameworkPropertyMetadata(new CornerRadius(3), OnBorderPropertyChanged));

        /// <summary>Gets or sets a value that represents the degree to which the corners of a <see cref="T:System.Windows.Controls.Border" /> are rounded.  </summary>
        /// <returns>The <see cref="T:System.Windows.CornerRadius" /> that describes the degree to which corners are rounded. This property has no default value.</returns>
        public CornerRadius CornerRadius
        {
            get
            {
                return (CornerRadius)this.GetValue(Border.CornerRadiusProperty);
            }
            set
            {
                this.SetValue(Border.CornerRadiusProperty, (object)value);
            }
        }

        // To update the Border element with the value of CornerRadius property,
        // we need to override the UpdateBorder method and set the CornerRadius on the textBorder element.

        protected override void UpdateBorder()
        {
            // textBorder is a protected field that is set to the Border element that is used to render the Border.
            if (textBorder != null)
                textBorder.CornerRadius = this.CornerRadius;

            base.UpdateBorder();
        }

        // To add some customization to how TextBlock is rendered,
        // you can override the UpdateTextBlock
        protected override void UpdateTextBlock()
        {
            // here you can update the properties on the protected textBlock field

            base.UpdateTextBlock();
        }

        // To change the MeshGeometry that is used to render the text,
        // you can override the UpdatePlaneMesh
        protected override void UpdatePlaneMesh()
        {
            // update textGeometryModel3D
            // here you can update the properties on the protected textGeometryModel3D field

            base.UpdatePlaneMesh();
        }
    }
}
