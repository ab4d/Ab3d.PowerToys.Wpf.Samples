// ----------------------------------------------------------------
// <copyright file="AxisWireBoxVisual3D.cs" company="AB4D d.o.o.">
//     Copyright (c) AB4D d.o.o.  All Rights Reserved
// </copyright>
// ----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Ab3d.Common.Cameras;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Common
{
    /// <summary>
    /// AxisWireBoxVisual3D shows a wireframe box with shown Y axis lines and values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>AxisWireBoxVisual3D</b> shows a wireframe box with shown Y axis lines and values.
    /// The axis values are shown as TextBlocks on the Canvas that is specified to <see cref="OverlayCanvas"/> property.
    /// </para>
    /// <para>
    /// When camera is rotated, the AxisWireBoxVisual3D changes the axis lines so that only the back two planes show the lines.
    /// Also the axis values are always shown only on left-most y axis.
    /// </para>
    /// <para>
    /// The axis values are controlled by <see cref="MinYValue"/>, <see cref="MaxYValue"/> and <see cref="YAxisValues"/> list. 
    /// The <see cref="MinYValue"/> and <see cref="MaxYValue"/> specify the y values at the bottom and at the top of y axis.
    /// The <see cref="YAxisValues"/> specifies a list of all values that will be shown on the axis.
    /// With <see cref="AxisValuesStringFormat"/> it is possible to specify the format of the displayed values.
    /// </para>
    /// <para>
    /// To correctly show the AxisWireBoxVisual3D, user needs to also set the <see cref="Camera"/> and <see cref="OverlayCanvas"/> properties.
    /// The <see cref="Camera"/> is used to subscribe to camera changes and automatically update the axis.
    /// The <see cref="OverlayCanvas"/> is a Canvas that is a placeholder to TextBlock elements that show the axis values. 
    /// This canvas should be positioned in the same space as Viewport3D and should be defined after the Viewport3D.
    /// It is also recommended to set its IsHitTestVisible to false.
    /// </para>
    /// </remarks>
    public class AxisWireBoxVisual3D : BaseVisual3D
    {
        private WireBoxVisual3D _wireBoxVisual3D;
        private int _lastMinXIndex;

        private Ab3d.Cameras.BaseCamera _subscribedCamera;
        private Viewport3D _subscribedViewport3D;

        #region CenterPosition
        /// <summary>
        /// Gets or sets the center position of the 3D box used to create the lines
        /// </summary>
        public Point3D CenterPosition
        {
            get { return (Point3D)GetValue(CenterPositionProperty); }
            set { SetValue(CenterPositionProperty, value); }
        }

        /// <summary>
        /// CenterPositionProperty
        /// </summary>
        public static readonly DependencyProperty CenterPositionProperty =
            DependencyProperty.Register("CenterPosition", typeof(Point3D), typeof(AxisWireBoxVisual3D),
                new PropertyMetadata(new Point3D(0, 0, 0), OnPropertyChanged));
        #endregion

        #region Size
        /// <summary>
        /// SizeProperty
        /// </summary>
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof(Size3D), typeof(AxisWireBoxVisual3D),
                new PropertyMetadata(new Size3D(1, 1, 1), OnPropertyChanged), ValidateSize3DOneZeroAllowedPropertyValue);

        /// <summary>
        /// Gets or sets the size of the 3D box used to create the lines
        /// </summary>
        public Size3D Size
        {
            get { return (Size3D)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }
        #endregion

        #region MinYValue
        /// <summary>
        /// MinYValueProperty
        /// </summary>
        public static readonly DependencyProperty MinYValueProperty =
            DependencyProperty.Register("MinYValue", typeof(double), typeof(AxisWireBoxVisual3D),
                new PropertyMetadata(double.NaN, OnPropertyChanged));

        /// <summary>
        /// Gets or sets a double value that represents the value at the bottom of y axis (min y value).
        /// </summary>
        public double MinYValue
        {
            get { return (double)GetValue(MinYValueProperty); }
            set { SetValue(MinYValueProperty, value); }
        }
        #endregion

        #region MaxYValue
        /// <summary>
        /// MaxYValueProperty
        /// </summary>
        public static readonly DependencyProperty MaxYValueProperty =
            DependencyProperty.Register("MaxYValue", typeof(double), typeof(AxisWireBoxVisual3D),
                new PropertyMetadata(double.NaN, OnPropertyChanged));

        /// <summary>
        /// Gets or sets a double value that represents the value at the top of y axis (max y value).
        /// </summary>
        public double MaxYValue
        {
            get { return (double)GetValue(MaxYValueProperty); }
            set { SetValue(MaxYValueProperty, value); }
        }
        #endregion

        #region YAxisValues
        /// <summary>
        /// YAxisValuesProperty
        /// </summary>
        public static readonly DependencyProperty YAxisValuesProperty =
            DependencyProperty.Register("YAxisValues", typeof(IList<double>), typeof(AxisWireBoxVisual3D),
                new PropertyMetadata(null, OnPropertyChanged));

        /// <summary>
        /// Gets or sets a list of double value that represents the value shown on y axis.
        /// </summary>
        public IList<double> YAxisValues
        {
            get { return (IList<double>)GetValue(YAxisValuesProperty); }
            set { SetValue(YAxisValuesProperty, value); }
        }
        #endregion

        #region AxisValuesStringFormat
        /// <summary>
        /// AxisValuesStringFormatProperty
        /// </summary>
        public static readonly DependencyProperty AxisValuesStringFormatProperty =
            DependencyProperty.Register("AxisValuesStringFormat", typeof(string), typeof(AxisWireBoxVisual3D),
                new PropertyMetadata("{0:0}", OnPropertyChanged));

        /// <summary>
        /// Gets or sets a string that defines the format of the displayed axis values.
        /// Default value is "{0:0}" which displays only whole numbers (no decimals).
        /// Examples: "{0:0.00}" always shows 2 decimals; "{0:0.#}" shows 1 decimal when the number is not whole number.
        /// </summary>
        public string AxisValuesStringFormat
        {
            get { return (string)GetValue(AxisValuesStringFormatProperty); }
            set { SetValue(AxisValuesStringFormatProperty, value); }
        }
        #endregion

        #region WireBoxLineColor
        /// <summary>
        /// WireBoxLineColorProperty
        /// </summary>
        public static readonly DependencyProperty WireBoxLineColorProperty =
            DependencyProperty.Register("WireBoxLineColor", typeof(Color), typeof(AxisWireBoxVisual3D),
                new PropertyMetadata(Colors.Black, OnPropertyChanged));


        /// <summary>
        /// Gets or sets the color of the wire box line
        /// </summary>
        public Color WireBoxLineColor
        {
            get { return (Color)GetValue(WireBoxLineColorProperty); }
            set { SetValue(WireBoxLineColorProperty, value); }
        }
        #endregion

        #region AxisLinesColor
        /// <summary>
        /// LineColorProperty
        /// </summary>
        public static readonly DependencyProperty AxisLinesColorProperty =
            DependencyProperty.Register("AxisLinesColor", typeof(Color), typeof(AxisWireBoxVisual3D),
                new PropertyMetadata(Colors.DimGray, OnPropertyChanged));


        /// <summary>
        /// Gets or sets the color of the lines that are drawn for the axis values
        /// </summary>
        public Color AxisLinesColor
        {
            get { return (Color)GetValue(AxisLinesColorProperty); }
            set { SetValue(AxisLinesColorProperty, value); }
        }
        #endregion

        #region OverlayCanvas
        /// <summary>
        /// OverlayCanvasProperty
        /// </summary>
        public static readonly DependencyProperty OverlayCanvasProperty =
            DependencyProperty.Register("OverlayCanvas", typeof(Canvas), typeof(AxisWireBoxVisual3D),
                new PropertyMetadata(null, OnPropertyChanged));

        /// <summary>
        /// Gets or sets a Canvas that will show the axis numbers
        /// </summary>
        public Canvas OverlayCanvas
        {
            get { return (Canvas)GetValue(OverlayCanvasProperty); }
            set { SetValue(OverlayCanvasProperty, value); }
        }
        #endregion

        #region Camera
        /// <summary>
        /// CameraProperty
        /// </summary>
        public static readonly DependencyProperty CameraProperty =
            DependencyProperty.Register("Camera", typeof(Ab3d.Cameras.BaseCamera), typeof(AxisWireBoxVisual3D),
                new PropertyMetadata(null, OnCameraPropertyChanged));

        /// <summary>
        /// Gets or sets an Ab3d Camera that is used by this Viewport3D
        /// </summary>
        public Ab3d.Cameras.BaseCamera Camera
        {
            get { return (Ab3d.Cameras.BaseCamera)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        protected static void OnCameraPropertyChanged(DependencyObject obj,
                                                DependencyPropertyChangedEventArgs args)
        {
            ((AxisWireBoxVisual3D)obj).SubscribeCameraChanges();
        }
        #endregion

        protected override void CreateModel()
        {
            EnsureWireBox();
            UpdateAxis(recreateEverything: true);
        }

        private void EnsureWireBox()
        {
            if (_wireBoxVisual3D == null)
            {
                _wireBoxVisual3D = new WireBoxVisual3D();
                this.Children.Add(_wireBoxVisual3D);
            }
        }

        // if recreateEverything == true, than everything will be recreated
        // if false than just the positions of TextBlocks are updated
        private void UpdateAxis(bool recreateEverything)
        {
            if (Camera == null || OverlayCanvas == null || !this.IsVisible)
                return;

            if (_subscribedViewport3D == null)
                SubscribeViewportSizeChanged();

            OverlayCanvas.Children.Clear();

            EnsureWireBox();

            _wireBoxVisual3D.LineColor = this.WireBoxLineColor;
            _wireBoxVisual3D.CenterPosition = this.CenterPosition;
            _wireBoxVisual3D.Size = this.Size;


            var yAxisValues = YAxisValues;
            if (yAxisValues == null || yAxisValues.Count == 0)
                return;

            // Get local values
            Point3D wireBoxCenter = this.CenterPosition;
            Size3D wireBoxSize = this.Size;

            // Now when we have the 3D positions of the wire box we can calculate the needed data for the axis.
            // The axis is shown along the part of the wire box that is shown on the left side of the screen.
            // First determine which position is shown as the left most

            Point3D[] positions = new Point3D[4];
            Point[] positionsOnScreen = new Point[4];

            // Those are the positions of the top corners of the wire box
            positions[0] = new Point3D(wireBoxCenter.X - wireBoxSize.X / 2, wireBoxCenter.Y + wireBoxSize.Y / 2, wireBoxCenter.Z + wireBoxSize.Z / 2);
            positions[1] = new Point3D(wireBoxCenter.X - wireBoxSize.X / 2, wireBoxCenter.Y + wireBoxSize.Y / 2, wireBoxCenter.Z - wireBoxSize.Z / 2);
            positions[2] = new Point3D(wireBoxCenter.X + wireBoxSize.X / 2, wireBoxCenter.Y + wireBoxSize.Y / 2, wireBoxCenter.Z - wireBoxSize.Z / 2);
            positions[3] = new Point3D(wireBoxCenter.X + wireBoxSize.X / 2, wireBoxCenter.Y + wireBoxSize.Y / 2, wireBoxCenter.Z + wireBoxSize.Z / 2);

            double minX = double.MaxValue;
            int minXIndex = 0;


            for (int i = 0; i < 4; i++)
            {
                // Convert from 3D position to 2D position on the screen (inside the Viewport3D)
                Point onePoint = Camera.Point3DTo2D(positions[i]);

                positionsOnScreen[i] = onePoint;

                if (onePoint.X < minX)
                {
                    minX = onePoint.X;
                    minXIndex = i;
                }
            }

            // Now the corner with index i is shown on the left side on the screen
            // The top positions were used for that calculation
            // Now calculate the bottom position


            int axisValuesCount = yAxisValues.Count;

            Point3D lowerPosition = positions[minXIndex];
            lowerPosition.Y -= wireBoxSize.Y;

            var axisYPositions = new List<Point3D>(axisValuesCount);
            var axisYPositionsOnScreen = new List<Point>(axisValuesCount);
            var axisValues = new List<double>(axisValuesCount);

            // Get the 3D and 2D positions of the axis items

            // We already have all the data for the top (last) item

            double minYValue = this.MinYValue;
            if (double.IsNaN(minYValue)) // If MinYValue is not set, use min value from yAxisValues
                minYValue = yAxisValues.Min();

            double maxYValue = this.MaxYValue;
            if (double.IsNaN(maxYValue)) // If MaxYValue is not set, use max value from yAxisValues
                maxYValue = yAxisValues.Max();

            double axisValuesRange = maxYValue - minYValue;
            double worldYFactor = this.Size.Y / axisValuesRange;

            //axisYPositions[countMinusOne] = positions[minXIndex];
            //axisYPositionsOnScreen[countMinusOne] = positionsOnScreen[minXIndex];
            //axisValues[countMinusOne] = axisMaxY;


            Camera.Refresh();

            for (int i = 0; i < axisValuesCount; i++)
            {
                double oneYAxisValue = yAxisValues[i];

                if (oneYAxisValue < minYValue || oneYAxisValue > maxYValue) // Skip out of axis values
                    continue;

                double yWorld = lowerPosition.Y + (oneYAxisValue - minYValue) * worldYFactor;
                Point3D onePosition = new Point3D(lowerPosition.X, yWorld, lowerPosition.Z);

                Point screenPosition = Camera.Point3DTo2D(onePosition);

                if (double.IsNaN(screenPosition.X) || double.IsNaN(screenPosition.Y))
                    continue;

                axisValues.Add(oneYAxisValue);
                axisYPositions.Add(onePosition);
                axisYPositionsOnScreen.Add(screenPosition);
            }

            // Now update the values count to get actual values count (without values that are out of range)
            axisValuesCount = axisValues.Count;

            if (axisValuesCount == 0)
                return;

            // Now we have all the data and can display the axis values

            TextBlock textBlock;
            Line line;

            var lineBrush = new SolidColorBrush(AxisLinesColor);

            if (recreateEverything || OverlayCanvas.Children.Count == 0)
            {
                if (OverlayCanvas.Children.Count > 0)
                    OverlayCanvas.Children.Clear();

                // For each axis item we create one TextBlock and one Line
                for (int i = 0; i < axisValuesCount; i++)
                {
                    textBlock = new TextBlock();
                    //textBlock.FontSize = 12;
                    textBlock.Foreground = lineBrush;
                    textBlock.Text = string.Format(AxisValuesStringFormat, axisValues[i]);

                    // We need to measure the string to get the required x offset
                    textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                    OverlayCanvas.Children.Add(textBlock);


                    line = new Line();
                    line.StrokeThickness = 1;
                    line.Stroke = lineBrush;

                    OverlayCanvas.Children.Add(line);
                }
            }

            // Set the size
            double halfTextHeight = OverlayCanvas.Children[0].DesiredSize.Height / 2;
            double x, y;

            // Update TextBlock and Line positions
            for (int i = 0; i < axisValuesCount; i++)
            {
                textBlock = (TextBlock)OverlayCanvas.Children[i * 2];
                line = (Line)OverlayCanvas.Children[i * 2 + 1];

                // x and y are 2D positions on the 3D wire grid line
                x = axisYPositionsOnScreen[i].X;
                y = axisYPositionsOnScreen[i].Y;

                Canvas.SetLeft(textBlock, x - textBlock.DesiredSize.Width - 15);
                Canvas.SetTop(textBlock, y - halfTextHeight);

                line.X1 = x - 10;
                line.X2 = x;
                line.Y1 = y;
                line.Y2 = y;
            }


            if (recreateEverything || _lastMinXIndex != minXIndex)
            {
                List<Point3D> linePositions = new List<Point3D>((axisValuesCount - 2) * 4);

                int startLineIndex;
                if (Math.Abs(axisValues[0] - minYValue) < (axisValuesRange * 0.02))
                    startLineIndex = 1; // If the first line is almost the same as the the top of the WireBox, then we do not need to show it
                else
                    startLineIndex = 0;

                int endLineIndex = axisValuesCount;
                if (Math.Abs(axisValues[axisValues.Count - 1] - maxYValue) < (axisValuesRange * 0.02))
                    endLineIndex --; // If the last line is almost the same as the the bottom of the WireBox, then we do not need to show it

                for (int j = startLineIndex; j < endLineIndex; j++)
                {
                    int i = minXIndex;

                    Point3D p1, p2;

                    p1 = new Point3D(positions[i].X, axisYPositions[j].Y, positions[i].Z);

                    i++;
                    if (i >= 4)
                        i = 0;

                    p2 = new Point3D(positions[i].X, axisYPositions[j].Y, positions[i].Z);


                    linePositions.Add(p1);
                    linePositions.Add(p2);

                    p1 = p2;
                    i++;
                    if (i >= 4)
                        i = 0;

                    p2 = new Point3D(positions[i].X, axisYPositions[j].Y, positions[i].Z);


                    linePositions.Add(p1);
                    linePositions.Add(p2);
                }

                Model3D backLinesModel = Ab3d.Models.Line3DFactory.CreateMultiLine3D(linePositions, 1, AxisLinesColor, Ab3d.Common.Models.LineCap.Flat, Ab3d.Common.Models.LineCap.Flat, this);
                this.Content = backLinesModel;

                // Save the minXIndex - if this is changed than we need to recreate the lines
                _lastMinXIndex = minXIndex;
            }
        }


        #region SubscribeCameraChanges, SubscribeViewportSizeChanged, CameraOnCameraChanged, OnViewport3DSizeChanged, ...
        // When camera or Viewport3D size is changed we need to update the axis
        private void SubscribeCameraChanges()
        {
            if (_subscribedCamera != null)
            {
                if (_subscribedCamera == Camera)
                    return;

                UnsubscribeCameraChanges();
            }

            if (Camera != null)
            {
                Camera.CameraChanged += CameraOnCameraChanged;
                _subscribedCamera = Camera;

                SubscribeViewportSizeChanged();
            }
        }

        private void UnsubscribeCameraChanges()
        {
            if (_subscribedCamera == null)
                return;

            UnsubscribeViewportSizeChanged();

            _subscribedCamera.CameraChanged -= CameraOnCameraChanged;
            _subscribedCamera = null;
        }

        private void EnsureViewportSizeChangedSubscribed()
        {
            if (_subscribedViewport3D == null)
                SubscribeViewportSizeChanged();
        }

        private void SubscribeViewportSizeChanged()
        {
            if (_subscribedViewport3D != null)
                UnsubscribeViewportSizeChanged();

            if (Camera != null)
            {
                _subscribedViewport3D = Camera.TargetViewport3D;
                if (_subscribedViewport3D != null)
                    _subscribedViewport3D.SizeChanged += OnViewport3DSizeChanged;
            }
        }

        private void UnsubscribeViewportSizeChanged()
        {
            if (_subscribedViewport3D == null)
                return;

            _subscribedViewport3D.SizeChanged -= OnViewport3DSizeChanged;
            _subscribedViewport3D = null;
        }

        private void OnViewport3DSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            // Because we do not know if Camera got the SizeChanged first we need to 
            // manually call Refresh in case we get the event first - we need to update the camera
            // so the 3d to 2d convertion methods will work correctly
            Camera.Refresh();

            UpdateAxis(recreateEverything: false);
        }

        private void CameraOnCameraChanged(object sender, CameraChangedRoutedEventArgs cameraChangedRoutedEventArgs)
        {
            UpdateAxis(recreateEverything: false);
        }

        /// <summary>
        /// OnIsVisibleChanged is called when the IsVisible property is changed
        /// </summary>
        /// <param name="newIsVisibleValue">newIsVisibleValue as bool</param>
        protected override void OnIsVisibleChanged(bool newIsVisibleValue)
        {
             base.OnIsVisibleChanged(newIsVisibleValue);

            if (_wireBoxVisual3D != null)
                _wireBoxVisual3D.IsVisible = newIsVisibleValue;
        }
    #endregion
    }
}