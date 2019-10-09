using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Ab3d.PowerToys.Samples.SceneEditor
{
    public class SceneEditorContext : INotifyPropertyChanged
    {
        private bool _showCameraAxis = true;
        private bool _showViewCubeCameraController = true;
        private bool _showMouseCameraControllerInfo = true;
        private bool _showWireGrid = true;
        private bool _snapToGrid = false;


        private static SceneEditorContext _singleInstance;

        public static SceneEditorContext Current
        {
            get
            {
                if (_singleInstance == null)
                    _singleInstance = new SceneEditorContext();

                return _singleInstance;
            }
        }


        public Color HighlightedColor { get; private set; }
        public Color SelectedColor { get; private set; }

        public Brush HighlightedBrush { get; private set; }
        public Brush SelectedBrush { get; private set; }


        public bool ShowCameraAxis
        {
            get { return _showCameraAxis; }
            set
            {
                if (value == _showCameraAxis) return;
                _showCameraAxis = value;
                OnPropertyChanged();
            }
        }

        public bool ShowViewCubeCameraController
        {
            get { return _showViewCubeCameraController; }
            set
            {
                if (value == _showViewCubeCameraController) return;
                _showViewCubeCameraController = value;
                OnPropertyChanged();
            }
        }

        public bool ShowMouseCameraControllerInfo
        {
            get { return _showMouseCameraControllerInfo; }
            set
            {
                if (value == _showMouseCameraControllerInfo) return;
                _showMouseCameraControllerInfo = value;
                OnPropertyChanged();
            }
        }

        public bool ShowWireGrid
        {
            get { return _showWireGrid; }
            set
            {
                if (value == _showWireGrid) return;
                _showWireGrid = value;
                OnPropertyChanged();
            }
        }

        public bool SnapToGrid
        {
            get { return _snapToGrid; }
            set
            {
                if (value == _snapToGrid) return;
                _snapToGrid = value;
                OnPropertyChanged();
            }
        }


        private SceneEditorContext()
        {
            HighlightedColor = Colors.Orange;
            SelectedColor    = Colors.Red;

            HighlightedBrush = new SolidColorBrush(HighlightedColor);
            HighlightedBrush.Freeze();

            SelectedBrush = new SolidColorBrush(SelectedColor);
            SelectedBrush.Freeze();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}