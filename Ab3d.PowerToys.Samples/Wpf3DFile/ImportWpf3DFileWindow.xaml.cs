using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace Ab3d.PowerToys.Samples.Wpf3DFile
{
    /// <summary>
    /// Interaction logic for ImportWpf3DFileWindow.xaml
    /// </summary>
    public partial class ImportWpf3DFileWindow : Window
    {
        public string SelectedFileName { get; private set; }

        public string SelectedFolder
        {
            get { return FolderTextBox.Text; }
            set { FolderTextBox.Text = value; }
        }

        public ImportWpf3DFileWindow()
        {
            InitializeComponent();

            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                CollectFiles();
            };
        }

        private void CollectFiles()
        {
            var selectedFolder = SelectedFolder;
            if (string.IsNullOrEmpty(selectedFolder) || !System.IO.Directory.Exists(selectedFolder))
                selectedFolder = Environment.CurrentDirectory;

            var wpf3dFileNames = System.IO.Directory.GetFiles(selectedFolder, "*.wpf3d", SearchOption.TopDirectoryOnly);

            if (wpf3dFileNames == null || wpf3dFileNames.Length == 0)
            {
                FilesListBox.ItemsSource = null;
                return;
            }

            // Here we read only wpf3d file header that contains description, number of objects and thumbnail.
            // Note: Calling ReadFileHeader can be also done in a background thread.

            var wpf3dFiles = new List<Ab3d.Utilities.Wpf3DFile>(wpf3dFileNames.Length);
            foreach (var wpf3dFileName in wpf3dFileNames)
            {
                try
                {
                    var wpf3DFile = new Ab3d.Utilities.Wpf3DFile();
                    wpf3DFile.ReadFileHeader(wpf3dFileName);

                    wpf3dFiles.Add(wpf3DFile);
                }
                catch
                {
                    // pass
                    //MessageBox.Show(string.Format("Cannot read file\r\n{0}\r\n\r\nError: {1}", wpf3dFileName, ex.Message));
                }
            }

            FilesListBox.ItemsSource = wpf3dFiles;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedFileName = null;
            this.Close();
        }

        private void FilesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            var wpf3DFile = FilesListBox.SelectedItem as Ab3d.Utilities.Wpf3DFile;

            if (wpf3DFile != null)
            {
                SelectedFileName = wpf3DFile.SourceFileName;
                this.Close();
            }
        }

        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            CollectFiles();
        }
    }
}
