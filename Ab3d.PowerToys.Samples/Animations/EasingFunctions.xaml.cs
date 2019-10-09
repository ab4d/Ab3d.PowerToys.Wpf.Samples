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

namespace Ab3d.PowerToys.Samples.Animations
{
    /// <summary>
    /// Interaction logic for EasingFunctions.xaml
    /// </summary>
    public partial class EasingFunctions : Page
    {
        public EasingFunctions()
        {
            InitializeComponent();

            int graphWidth = 200;
            int graphHeight = 180;

            GraphsWrapPanel.Width = graphWidth * 3 + 50; // Allow 3 graphs in a row


            // First add no easing (linear interpolation) graph
            var noEasingGraph = CreateEasingFunctionGraph("No easing (linear interpolation)", new Func<double, double>(t => t), graphWidth, graphHeight);
            GraphsWrapPanel.Children.Add(noEasingGraph);

            // Add 2 empty graphs
            var dummyGraph = CreateEasingFunctionGraph("", null, graphWidth, graphHeight);
            GraphsWrapPanel.Children.Add(dummyGraph);

            dummyGraph = CreateEasingFunctionGraph("", null, graphWidth, graphHeight);
            GraphsWrapPanel.Children.Add(dummyGraph);


            // Now get all static easing functions from Ab3d.Animation.EasingFunctions class
            // and create graph for each of the function.
            var easingFunctionsType = typeof(Ab3d.Animation.EasingFunctions);
            var easingFunctionsMethodInfos = easingFunctionsType.GetMethods();

            foreach (var methodInfo in easingFunctionsMethodInfos)
            {
                if (!methodInfo.IsStatic)
                    continue;

                var easingFunction = new Func<double, double>(t => (double) methodInfo.Invoke(null, new object[] {t}));

                var graph = CreateEasingFunctionGraph(methodInfo.Name, easingFunction, graphWidth, graphHeight);

                GraphsWrapPanel.Children.Add(graph);
            }


            EasingExampleTextBox.Text =
@"/// <summary>
/// CubicEaseInOutFunction
/// </summary>
/// <param name=""t"">input value in range from 0 to 1</param>
/// <returns>returned eased value in range from 0 to 1</returns>
public static double CubicEaseInOutFunction(double t)
{
    t *= 2;

    if (t < 1)
        return t * t * t * 0.5;

    t -= 2;
    return 0.5 * (t * t * t + 2);
}";
        }

        private FrameworkElement CreateEasingFunctionGraph(string title, Func<double, double> easingFunction, double width, double height)
        {
            double borderPadding = 3;
            double borderThickness = 2;


            var border = new Border()
            {
                BorderThickness = new Thickness(2, 2, 2, 2),
                Padding = new Thickness(borderPadding, borderPadding, borderPadding, borderPadding),
                Width = width,
                Height = height
            };

            if (easingFunction != null)
            {
                double canvasWidth  = width  - 2 * borderPadding - borderThickness;
                double canvasHeight = height - 2 * borderPadding - borderThickness;

                var canvas = new Canvas()
                {
                    Width = canvasWidth,
                    Height = canvasHeight
                };



                var line = new Line()
                {
                    X1 = 0,
                    Y1 = canvasHeight,
                    X2 = canvasWidth,
                    Y2 = 0,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };

                canvas.Children.Add(line);


                Polyline graph = new Polyline();
                graph.Stroke = Brushes.Red;
                graph.StrokeThickness = 1;

                for (double progress = 0; progress <= 1.001; progress += 0.025)
                {
                    double x = progress * canvasWidth;
                    double y = (1 - easingFunction(progress)) * canvasHeight; // y = 0 => top of canvas; so we need to invert the y value

                    graph.Points.Add(new Point(x, y));
                }

                canvas.Children.Add(graph);


                border.Child = canvas;

                border.BorderBrush = Brushes.Black;
            }

            var textBlock = new TextBlock()
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 3)
            };


            var stackPanel = new StackPanel()
            {
                Margin = new Thickness(5, 10, 5, 5),
                Orientation = Orientation.Vertical
            };

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(border);


            return stackPanel;
        }
    }
}
