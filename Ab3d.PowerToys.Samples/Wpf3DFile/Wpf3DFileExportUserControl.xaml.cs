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
using System.Windows.Shapes;
using System.Windows.Media.Media3D;

namespace Ab3d.PowerToys.Samples.Wpf3DFile
{
    /// <summary>
    /// Interaction logic for Wpf3DFileExportUserControl.xaml
    /// </summary>
    public partial class Wpf3DFileExportUserControl : UserControl
    {
        // Set this constant to false to prevent checking if the file was correctly saved
        private const bool CheckSavedFile = false;


        public string OriginalFileName { get; set; }

        public BitmapSource Thumbnail { get; set; }

        public Model3D RootModel { get; set; }

        public Dictionary<string, object> NamedObjects { get; set; }

        public object Camera { get; set; }

        public string SourceFileName { get; set; }


        public bool ShowCancelButton {
            get
            {
                return CancelButton.Visibility == Visibility.Visible;
            }

            set
            {
                CancelButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        public bool ShowSaveButton {
            get
            {
                return SaveButton.Visibility == Visibility.Visible;
            }

            set
            {
                SaveButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public string FileName
        {
            get { return FileNameTextBox.Text; }
            set { FileNameTextBox.Text = value; }
        }


        public event EventHandler SaveButtonClicked;

        public event EventHandler CancelButtonClicked;


        public Wpf3DFileExportUserControl()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Wpf3DFileExportUserControl_Loaded);
        }

        void Wpf3DFileExportUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            string fileName = OriginalFileName;
            if (string.IsNullOrEmpty(fileName))
                fileName = "file";

            fileName = System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(fileName), "wpf3d");

            fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            FileNameTextBox.Text = fileName;

            ThumbnailImage.Source = Thumbnail;

            if (Thumbnail == null)
            {
                SaveThumbnailCheckBox.IsChecked = false;
                SaveThumbnailCheckBox.IsEnabled = false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Ab3d.Utilities.Wpf3DFile.IsLogging = true;

                Ab3d.Utilities.Wpf3DFile wpf3DFile = new Ab3d.Utilities.Wpf3DFile();
                wpf3DFile.Description = DescriptionTextBox.Text;
                wpf3DFile.Comment = CommentTextBox.Text;
                wpf3DFile.SourceFileName = this.SourceFileName;

                wpf3DFile.SaveNormals            = SaveNormalsCheckBox.IsChecked ?? false;
                wpf3DFile.SaveTextureCoordinates = SaveTextureCoordinatesCheckBox.IsChecked ?? false;

                if (SaveThumbnailCheckBox.IsChecked ?? false)
                    wpf3DFile.Thumbnail = Thumbnail;


                Ab3d.Utilities.Wpf3DFile.DataPrecisionType dataPrecession;

                switch (PrecisionComboBox.SelectedIndex)
                { 
                    case 0:
                        dataPrecession = Ab3d.Utilities.Wpf3DFile.DataPrecisionType.Double;
                        break;

                    default:
                        dataPrecession = Ab3d.Utilities.Wpf3DFile.DataPrecisionType.Float;
                        break;
                }

                wpf3DFile.WriteFile(FileNameTextBox.Text, RootModel, NamedObjects, Camera, dataPrecession);

                if (CheckSavedFile && wpf3DFile.SaveNormals && wpf3DFile.SaveTextureCoordinates)
                {
                    var model = wpf3DFile.ReadFile(FileNameTextBox.Text);

                    if (model == null)
                        throw new Exception("model == null");

                    string s1 = Ab3d.Utilities.Dumper.GetModelInfoString(RootModel);
                    string s2 = Ab3d.Utilities.Dumper.GetModelInfoString(model);

                    if (s1 != s2)
                        MessageBox.Show("3D model not correctly saved - loaded models are different as the original models!");
                    //else
                    //    MessageBox.Show("3D models successfully saved to\r\n" + FileNameTextBox.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error writing wpf3d file\r\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            OnSaveButtonClicked();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            OnCancelButtonClicked();
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog;

            saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.AddExtension = true;
            saveFileDialog.CheckFileExists = false;
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.ValidateNames = false;

            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string originalFileName = OriginalFileName;
            if (string.IsNullOrEmpty(originalFileName))
                originalFileName = "file";

            saveFileDialog.FileName = System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(originalFileName), "wpf3d");
            saveFileDialog.DefaultExt = "wpf3d";
            saveFileDialog.Filter = "WPF 3D files (*.wpf3d)|*.wpf3d";
            saveFileDialog.Title = "Select output WPF 3D file";

            if (saveFileDialog.ShowDialog() ?? false)
                FileNameTextBox.Text = saveFileDialog.FileName;
        }


        protected void OnSaveButtonClicked()
        {
            if (SaveButtonClicked != null)
                SaveButtonClicked(this, null);
        }

        protected void OnCancelButtonClicked()
        {
            if (CancelButtonClicked != null)
                CancelButtonClicked(this, null);
        }
    }
}
