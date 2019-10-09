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
using System.Windows.Media.Media3D;
using Ab3d.Common.Models;
using System.Runtime.InteropServices;
using Ab3d.Common.Cameras;

namespace Ab3d.PowerToys.Samples.Lines3D
{
    /// <summary>
    /// Interaction logic for CurvesSample.xaml
    /// </summary>
    public partial class CurvesSample : Page
    {
        // See also the following curve samples:
        // http://www.antigrain.com/research/bezier_interpolation/index.html
        // http://www.devmag.org.za/downloads/bezier_curves/BezierPath.cs
        // http://devmag.org.za/2011/04/05/bzier-curves-a-tutorial/

        private enum CurveType
        { 
            CurveThroughPoints,
            BezierCurve,
            BSpline,
            NURBSCurve
        }

        private Random _rnd;

        private List<Point3D> _controlPoints;
        private List<Point3D> _bezierControlPoints;

        private List<double> _weights;

        private int _pointsCount;

        private CurveType _selectedCurveType;

        Ab3d.Visuals.WireCrossVisual3D _positionCross;

        Ab3d.Utilities.BSpline _bspline;

        Ab3d.Utilities.BezierCurve _bezierCurve;


        public CurvesSample()
        {
            InitializeComponent();

            _rnd = new Random();


            Camera1.CameraChanged += delegate (object sender, CameraChangedRoutedEventArgs args)
            {
                // Update the positions of the TextBlocks on every camera change
                UpdateControlPointNumbers();
            };

            MainViewport.SizeChanged += delegate (object sender, SizeChangedEventArgs args)
            {
                // Update the positions of the TextBlocks when the size of Viewport3D is changed
                UpdateControlPointNumbers();
            };


            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                CreateRandomPositions();
                RecreateCurve();
            };
        }
        
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            RecreateCurveDataFromTextBox();
            UpdateCurvesData();
            RecreateCurve();
        }

        private void CreateRandomPositionsButton_Click(object sender, RoutedEventArgs e)
        {
            CreateRandomPositions();
            RecreateCurve();
        }

        private void CurveTypeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;


            if (CurveThrougPointsRadioButton.IsChecked ?? false)
                _selectedCurveType = CurveType.CurveThroughPoints;

            else if (BezierCurveRadioButton.IsChecked ?? false)
                _selectedCurveType = CurveType.BezierCurve;

            else if (BSplineRadioButton.IsChecked ?? false)
                _selectedCurveType = CurveType.BSpline;  

            else // if (NURBSCurveRadioButton.IsChecked ?? false)
                _selectedCurveType = CurveType.NURBSCurve;


            UpdateCurvesData();
            ShowCurvesData();
            RecreateCurve();
        }


        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded)
                return;

            UpdatePositionCross();
        }

        private void UpdatePositionCross()
        {
            double t = PositionSlider.Value / 100.0;
            Point3D position;

            // Get position on the curve defined by _controlPoints and _weights
            // t is on interval from 0 to 1 - 0 meaning the first control point and 1 the last control point

            if (_selectedCurveType == CurveType.BezierCurve || _selectedCurveType == CurveType.CurveThroughPoints)
                position = _bezierCurve.GetPositionOnCurve(t);
            else if (_selectedCurveType == CurveType.BSpline)
                position = _bspline.GetPositionOnBSpline(t);
            else
                position = _bspline.GetPositionOnNURBSCurve(t);
                        

            if (_positionCross == null || !PositionsVisual.Children.Contains(_positionCross))
            {
                _positionCross = new Visuals.WireCrossVisual3D();
                _positionCross.LineColor = Colors.Yellow;
                _positionCross.LineThickness = 2;
                _positionCross.LinesLength = 15;

                _positionCross.Position = position;

                PositionsVisual.Children.Add(_positionCross);
            }
            else
            {
                // Just update the position
                _positionCross.Position = position;
            }

            PositionTextBlock.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Position on curve (t = {0:0.00})", t);
        }

        private void RecreateCurve()
        { 
            // First update the positions
            PositionsVisual.Children.Clear();
            CurvesVisual.Children.Clear();

            // Add red crosses for each control point
            foreach (var onePosition in _controlPoints)
            {
                Ab3d.Visuals.WireCrossVisual3D cross = new Visuals.WireCrossVisual3D();
                cross.LineColor = Colors.Red;
                cross.LineThickness = 1;
                cross.LinesLength = 5;

                cross.Position = onePosition;

                PositionsVisual.Children.Add(cross);
            }


            RecreateControlPointNumbers();
            UpdatePositionCross();



            int positionsPerSegment = Int32.Parse(PointsCountTextBox.Text);

   
            // Create the curve
            // Because we already have the BezierCurve or BSpline class instance we can use them to create the curves.
            // First get the positions that will be used to create the PolyLine3D
            // Than we create the PolyLine3D
            // Note that we could also create the curves directly by the following methods:
            //curveModel = Ab3d.Models.Line3DFactory.CreateCurveThroughPoints3D(usedPointCollection, positionsPerSegment, 2, Colors.White, LineCap.Flat, LineCap.Flat, CurvesVisual);
            //curveModel = Ab3d.Models.Line3DFactory.CreateBezierCurve3D(usedPointCollection, positionsPerSegment, 2, Colors.White, LineCap.Flat, LineCap.Flat, CurvesVisual);
            //curveModel = Ab3d.Models.Line3DFactory.CreateBSpline3D(usedPointCollection, positionsPerSegment, 2, Colors.White, LineCap.Flat, LineCap.Flat, CurvesVisual);
            //curveModel = Ab3d.Models.Line3DFactory.CreateNURBSCurve3D(usedPointCollection, _weights, positionsPerSegment, 2, Colors.White, LineCap.Flat, LineCap.Flat, CurvesVisual);

            Point3DCollection finalPointsCollection;

            if (_selectedCurveType == CurveType.BezierCurve || _selectedCurveType == CurveType.CurveThroughPoints)
                finalPointsCollection = _bezierCurve.CreateBezierCurve(positionsPerSegment);
            else if (_selectedCurveType == CurveType.BSpline)
                finalPointsCollection = _bspline.CreateBSpline(positionsPerSegment);
            else
                finalPointsCollection = _bspline.CreateNURBSCurve(positionsPerSegment);

            Model3D curveModel = Ab3d.Models.Line3DFactory.CreatePolyLine3D(finalPointsCollection, 2, Colors.White, false, LineCap.Flat, LineCap.Flat, CurvesVisual);

            CurvesVisual.Content = curveModel;


            if (_selectedCurveType == CurveType.BezierCurve)
            {
                // Show bezier control points that represent tangent positions
                for (int i = 0; i < _bezierControlPoints.Count; i++)
                {
                    if ((i % 3) == 0) // Show only tanget positions - skip position on the curve - they were already shown
                        continue;

                    Ab3d.Visuals.WireCrossVisual3D cross = new Visuals.WireCrossVisual3D();

                    cross.LineColor = Colors.Green;
                    cross.LineThickness = 1;
                    cross.LinesLength = 5;
                    cross.Position = _bezierControlPoints[i];
                    PositionsVisual.Children.Add(cross);
                }
            }
        }

        private void RecreateControlPointNumbers()
        {
            if (_controlPoints == null)
                return;

            OverlayCanvas.Children.Clear();

            for (int i = 0; i < _controlPoints.Count; i++)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.FontSize = 10;
                textBlock.Foreground = Brushes.Red;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.Text = i.ToString();

                // Calculate the 2D position of the 3D control point
                Point point2D = Camera1.Point3DTo2D(_controlPoints[i]);

                Canvas.SetLeft(textBlock, point2D.X + 10);
                Canvas.SetTop(textBlock, point2D.Y - 7);

                OverlayCanvas.Children.Add(textBlock);
            }
        }

        private void UpdateControlPointNumbers()
        {
            if (OverlayCanvas.Children.Count == 0)
                return;

            for (int i = 0; i < OverlayCanvas.Children.Count; i++)
            {
                TextBlock textBlock = (TextBlock)OverlayCanvas.Children[i];

                // Calculate the 2D position of the 3D control point
                Point point2D = Camera1.Point3DTo2D(_controlPoints[i]);

                Canvas.SetLeft(textBlock, point2D.X + 10);
                Canvas.SetTop(textBlock, point2D.Y - 7);
            }
        }

        private void RecreateCurveDataFromTextBox()
        {
            string[] lines = PositionsTextBox.Text.Split(new string [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            int pointsCount = lines.Length;
            List<Point3D> controlPoints = new List<Point3D>(pointsCount);
            List<double> weights = new List<double>(pointsCount);

            for (int i = 0; i < pointsCount; i++)
            {
                string[] lineValues = lines[i].Split(new char[] { ';', ' ' });

                if (lineValues.Length != 4 && lineValues.Length != 5)
                {
                    MessageBox.Show(string.Format("Invalid line: {0}", lines[i]));
                    return;
                }

                try
                {
                    Point3D newPoint = new Point3D(double.Parse(lineValues[1], System.Globalization.CultureInfo.InvariantCulture),
                                                   double.Parse(lineValues[2], System.Globalization.CultureInfo.InvariantCulture),
                                                   double.Parse(lineValues[3], System.Globalization.CultureInfo.InvariantCulture));

                    controlPoints.Add(newPoint);

                    if (lineValues.Length == 5)
                        weights.Add(double.Parse(lineValues[4], System.Globalization.CultureInfo.InvariantCulture));
                    else
                        weights.Add(1.0);

                }
                catch (FormatException)
                {
                    MessageBox.Show(string.Format("Invalid line: {0}", lines[i]));
                    return;
                }
            }

            _pointsCount = pointsCount;
            _weights = weights;

            if (_selectedCurveType == CurveType.BezierCurve)
                _bezierControlPoints = controlPoints;
            else
                _controlPoints = controlPoints;
        }

        private void CreateRandomPositions()
        {
            Point3D point;
            Vector3D vector;

            _pointsCount = Int32.Parse(PositionsCountTextBox.Text);
            _bezierControlPoints = null;

            _controlPoints = new List<Point3D>(_pointsCount);
            _weights = new List<double>(_pointsCount);

            point = new Point3D(-(20 * _pointsCount / 2), 0, 0);

            for (int i = 0; i < _pointsCount; i++)
            {
                vector = new Vector3D(20, _rnd.NextDouble() * 60 - 20, _rnd.NextDouble() * 60 - 20);
                point += vector;

                _controlPoints.Add(point);

                _weights.Add(1.0); // weight is always set to 1 - user can change it
            }

            UpdateCurvesData();
            ShowCurvesData();
        }

        private void UpdateCurvesData()
        {
            switch (_selectedCurveType)
            {
                case CurveType.CurveThroughPoints:
                    _bezierCurve = Ab3d.Utilities.BezierCurve.CreateFromCurvePositions(_controlPoints);
                    _bezierControlPoints = null;
                    _bspline = null;

                    break;

                case CurveType.BezierCurve:
                    if (_bezierControlPoints == null)
                    {
                        _bezierCurve = Ab3d.Utilities.BezierCurve.CreateFromCurvePositions(_controlPoints);

                        // We will show all the control points that are created from the _controlPoints (the curve will go though _controlPoints)
                        // For each _controlPoints two additional control points will be created - they define the tangents for the control points
                        _bezierControlPoints = _bezierCurve.ControlPoints.ToList();
                    }
                    else
                    {
                        _bezierCurve = new Ab3d.Utilities.BezierCurve(_bezierControlPoints);
                    }

                    _bspline = null;

                    break;

                case CurveType.BSpline:
                    _bspline = new Ab3d.Utilities.BSpline(_controlPoints);
                    _bezierCurve = null;
                    _bezierControlPoints = null;

                    break;

                case CurveType.NURBSCurve:
                    _bspline = new Ab3d.Utilities.BSpline(_controlPoints, _weights);
                    _bezierCurve = null;
                    _bezierControlPoints = null;
                    break;
            }
        }

        private void ShowCurvesData()
        {
            StringBuilder sb = new StringBuilder();

            if (_selectedCurveType == CurveType.BezierCurve)
            {
                SchemaTextBox.Text = "Control points (i.j x y z)\r\n j = 1 - left tangent\r\n j = 2 - point on the curve\r\n j = 3 - right tangent:";

                sb.AppendFormat("0.2 {0:0} {1:0} {2:0}\r\n", _bezierControlPoints[0].X, _bezierControlPoints[0].Y, _bezierControlPoints[0].Z);
                sb.AppendFormat("0.3 {0:0} {1:0} {2:0}\r\n", _bezierControlPoints[1].X, _bezierControlPoints[1].Y, _bezierControlPoints[1].Z);

                for (int i = 2; i < _bezierControlPoints.Count - 2; i++)
                {
                    int segment = (i + 1) / 3;
                    int fraction = (i - 2) % 3;

                    //if (fraction == 0)
                    //    sb.AppendFormat("{0}.1", segment);
                    //else if (fraction == 2)
                    //    sb.AppendFormat("{0}.2", segment);
                    //else
                    //    sb.AppendFormat("{0}  ", segment);

                    sb.AppendFormat("{0}.{1} {2:0} {3:0} {4:0}\r\n", segment, fraction + 1, _bezierControlPoints[i].X, _bezierControlPoints[i].Y, _bezierControlPoints[i].Z);
                }

                int pos = _bezierControlPoints.Count - 2;

                sb.AppendFormat("{0}.1 {1:0} {2:0} {3:0}\r\n", (pos + 1) / 3, _bezierControlPoints[pos].X, _bezierControlPoints[pos].Y, _bezierControlPoints[pos].Z);
                sb.AppendFormat("{0}.2 {1:0} {2:0} {3:0}\r\n", (pos + 2) / 3, _bezierControlPoints[pos + 1].X, _bezierControlPoints[pos + 1].Y, _bezierControlPoints[pos + 1].Z);

            }
            else
            {
                switch (_selectedCurveType)
                {
                    case CurveType.CurveThroughPoints:
                        SchemaTextBox.Text = "Curve points (i x y z):";
                        break;

                    // alrady handled
                    //case CurveType.BezierCurve:
                    //    SchemaTextBox.Text = "Control points (i.j x y z)\r\n3 points for one segment:";
                    //    break;

                    case CurveType.BSpline:
                        SchemaTextBox.Text = "Control points (i x y z):";
                        break;

                    case CurveType.NURBSCurve:
                        SchemaTextBox.Text = "Control points (i x y z weight):";
                        break;
                }

                for (int i = 0; i < _controlPoints.Count; i++)
                {
                    sb.AppendFormat("{0} {1:0} {2:0} {3:0}", i, _controlPoints[i].X, _controlPoints[i].Y, _controlPoints[i].Z);

                    if (_selectedCurveType == CurveType.NURBSCurve)
                        sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, " {0:0.0}", _weights[i]);

                    sb.AppendLine();
                }
            }

            PositionsTextBox.Text = sb.ToString();
        }
    }
}
