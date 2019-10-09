using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab.Common;
using Ab3d.Utilities;
using Path = System.IO.Path;

namespace Ab3d.PowerToys.Samples.Utilities
{
    /// <summary>
    /// Interaction logic for ModelOptimizerTest.xaml
    /// </summary>
    public partial class ModelOptimizerTest : Page, ICompositionRenderingSubscriber
    {
        private bool _isRenderingTimerSubscribed;

        private int _lastSecond;
        private int _framesCounter;

#if !NETCORE // PerformanceCounter is not supported in Net Core 3
        private volatile PerformanceCounter _cpuCounter;

        private List<float> _cpuUsageSamples;
#endif

        private System.Threading.Timer _timer;

        private Model3D _originalModel3D;
        private Model3D _optimizedModel3D;

        public ModelOptimizerTest()
        {
            InitializeComponent();

            var dragAndDropHelper = new DragAndDropHelper(this, ".obj");
            dragAndDropHelper.FileDroped += (sender, e) => LoadFile(e.FileName);

            this.Loaded += delegate (object sender, RoutedEventArgs args)
            {
                LoadFile(AppDomain.CurrentDomain.BaseDirectory + @"..\..\Resources\ObjFiles\ship_boat.obj");

                StartCameraRotation();

                StartFpsMonitor();
                StartCpuMonitor();
            };

            this.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                StopFpsMonitor();
                StopCpuMonitor();
            };
        }

        private void LoadFile(string fileName)
        {
            var readerObj = new Ab3d.ReaderObj();

            // Read the model
            _originalModel3D = readerObj.ReadModel3D(fileName);

            if (_originalModel3D == null)
            {
                InfoTextBox.Text = "Cannot read " + fileName;
                return;
            }

            // Optimize the model with ModelOptimizer
            // This will combine meshes that have the same material
            // Check the Ab3d.PowerToys help file for more details about ModelOptimizer
            var modelOptimizer = new Ab3d.Utilities.ModelOptimizer()
            {
                // Use default settings - set there only for your information
                CompareMaterialsByHash = true,

                // CompareMaterialsByHash specifies how the materials are compared
                // If true, the the actual material data are compared, if false than materials are compared by reference
                CombineModelsWithSameMaterial = true,

                // Note that this will prevent any furher changes of the model (but on the other hand this would allow to read the model on another thread)
                FreezeAll = true
            };

            // Optimize
            _optimizedModel3D = modelOptimizer.Optimize(_originalModel3D);



            // Update the camera for new model
            Point3D center;
            double size;

            GetModelCenterAndSize(_originalModel3D, out center, out size);

            if (double.IsNaN(center.X))
                center = new Point3D(); // 0, 0, 0 

            if (double.IsInfinity(size))
                size = 1;

            Camera1.Distance = size * 1.2;
            Camera1.CameraWidth = size * 1.2; // In case we have OrthographicCamera we also set the CameraWidth
            Camera1.TargetPosition = center;


            // Show the model - start with optimized model
            ContentVisual3D.Content = _optimizedModel3D;


            // Update statistics (show differences between original and optimized model)
            string modelStatistics = GetModelStatistics();
            InfoTextBox.Text = string.Format("Opened file: {0}\r\n\r\n{1}\r\n\r\n{2}", System.IO.Path.GetFileName(fileName), modelStatistics, InfoTextBox.Text);
        }

        private string GetModelStatistics()
        {
            // CollectModelCounters returns a ModelGroupCounters class that contains various counters of the specifed model
            var originalModelGroupCounters = Ab3d.Utilities.Dumper.CollectModelCounters(_originalModel3D);
            var optimizedModelGroupCounters = Ab3d.Utilities.Dumper.CollectModelCounters(_optimizedModel3D);

            //                        Original Model | Optimized Model | Diff.
            //------------------------------------------------------------------
            //Model3DGroup objects:             1    |            1    | 0    
            //GeometryModel3D objects:       3381    |           30    | -3351         
            //Total Positions:            269,016    |      269,016    | 0        
            //Total TriangleIndices:      269,016    |      269,016    | 0   

            string infoText = string.Format(System.Globalization.CultureInfo.InvariantCulture,
@"                         Original Model | Optimized Model | Diff.
------------------------------------------------------------------
Model3DGroup objects:     {0,10}    |   {4,10}    | {8}    
GeometryModel3D objects:  {1,10}    |   {5,10}    | {9}         
Total Positions:          {2,10:#,##0}    |   {6,10:#,##0}    | {10}        
Total TriangleIndices:    {3,10:#,##0}    |   {7,10:#,##0}    | {11}   
",
                originalModelGroupCounters.ModelsGroups,
                originalModelGroupCounters.GeometryModels,
                originalModelGroupCounters.Positions,
                originalModelGroupCounters.TriangleIndices,

                optimizedModelGroupCounters.ModelsGroups,
                optimizedModelGroupCounters.GeometryModels,
                optimizedModelGroupCounters.Positions,
                optimizedModelGroupCounters.TriangleIndices,

                optimizedModelGroupCounters.ModelsGroups - originalModelGroupCounters.ModelsGroups,
                optimizedModelGroupCounters.GeometryModels - originalModelGroupCounters.GeometryModels,
                optimizedModelGroupCounters.Positions - originalModelGroupCounters.Positions,
                optimizedModelGroupCounters.TriangleIndices - originalModelGroupCounters.TriangleIndices);

            return infoText;
        }

        private void GetModelCenterAndSize(Model3D model, out Point3D center, out double size)
        {
            Rect3D bounds;

            bounds = model.Bounds;

            center = new Point3D(bounds.X + bounds.SizeX / 2,
                                 bounds.Y + bounds.SizeY / 2,
                                 bounds.Z + bounds.SizeZ / 2);

            size = Math.Sqrt(bounds.SizeX * bounds.SizeX +
                             bounds.SizeY * bounds.SizeY +
                             bounds.SizeZ * bounds.SizeZ);
        }

        private void StartFpsMonitor()
        {
            if (_isRenderingTimerSubscribed)
                return;

            _framesCounter = 0;

            // Use CompositionRenderingHelper to subscribe to CompositionTarget.Rendering event
            // This is much safer because in case we forget to unsubscribe from Rendering, the CompositionRenderingHelper will unsubscribe us automatically
            // This allows to collect this class will Grabage collector and prevents infinite calling of Rendering handler.
            // After subscribing the ICompositionRenderingSubscriber.OnRendering method will be called on each CompositionTarget.Rendering event
            CompositionRenderingHelper.Instance.Subscribe(this);

            _isRenderingTimerSubscribed = true;
        }

        private void StopFpsMonitor()
        {
            if (!_isRenderingTimerSubscribed)
                return;

            CompositionRenderingHelper.Instance.Unsubscribe(this);

            _isRenderingTimerSubscribed = false;
        }

        private void StartCpuMonitor()
        {
#if !NETCORE
            if (_timer != null)
                StopCpuMonitor();

            _cpuUsageSamples = new List<float>();

            _cpuCounter = new PerformanceCounter();

            // We will gather CPU Usage samples every 100 ms
            _cpuCounter.CategoryName = "Processor";
            _cpuCounter.CounterName = "% Processor Time";
            _cpuCounter.InstanceName = "_Total";

            _timer = new System.Threading.Timer(delegate (object state)
            {
                if (_cpuCounter == null) // Disposed?
                    return;

                float currentCpuUsage = _cpuCounter.NextValue();
                lock (_cpuUsageSamples)
                {
                    _cpuUsageSamples.Add(currentCpuUsage);
                }
            }, null, 100, 100); // Call every 100 ms 
#endif
        }

        private void StopCpuMonitor()
        {
#if !NETCORE
            if (_timer == null)
                return;

            _timer.Dispose();
            _timer = null;

            var cpuCounter = _cpuCounter; // Safe disposal
            _cpuCounter = null;
            cpuCounter.Close();
#endif
        }
        private void UpdatePerformanceStats()
        {
#if !NETCORE
            float cpuUsageTotal = 0;
            float maxCpuUsage = 0;
            int samplesCount;

            lock (_cpuUsageSamples)
            {
                samplesCount = _cpuUsageSamples.Count;

                for (int i = 0; i < samplesCount; i++)
                {
                    float oneSample = _cpuUsageSamples[i];

                    cpuUsageTotal += oneSample;
                    if (oneSample > maxCpuUsage)
                        maxCpuUsage = oneSample;
                }

                _cpuUsageSamples.Clear(); // Clear samples
            }

            string cpuUsageText;
            if (samplesCount == 0)
                cpuUsageText = "";
            else
                cpuUsageText = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                             "{0:0}% (max: {1:0}%)",
                                             cpuUsageTotal / (float)samplesCount, maxCpuUsage);

            string statisticsText = string.Format("FPS: {0}\r\nCPU Usage: {1}",
                                                  _framesCounter, cpuUsageText);

            StatisticsTextBlock.Text = statisticsText;
#endif
        }

        void ICompositionRenderingSubscriber.OnRendering(EventArgs e)
        {
            _framesCounter++;

            int currentSecond = DateTime.Now.Second;
            if (currentSecond != _lastSecond)
            {
                // Every second we update the performance statistics
                UpdatePerformanceStats();

                _lastSecond = currentSecond;
                _framesCounter = 0;
            }
        }

        private void StartStopCameraRotateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Camera1.IsRotating)
                StopCameraRotation();
            else
                StartCameraRotation();
        }

        private void StartCameraRotation()
        {
            Camera1.StartRotation(45, 0);
            StartStopCameraRotateButton.Content = "Stop camera rotate";
        }

        private void StopCameraRotation()
        {
            Camera1.StopRotation();
            StartStopCameraRotateButton.Content = "Start camera rotate";
        }

        private void OnShownObjectButtonChanged(object sender, RoutedEventArgs e)
        {
            if (ContentVisual3D.Content == null)
                return; // Nothing loaded

            // Switch the shown model
            if (ReferenceEquals(ContentVisual3D.Content, _originalModel3D))
                ContentVisual3D.Content = _optimizedModel3D;
            else
                ContentVisual3D.Content = _originalModel3D;
        }
    }
}
