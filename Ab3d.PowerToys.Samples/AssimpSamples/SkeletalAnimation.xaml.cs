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
using Ab.Common;
using Ab3d.Assimp;
using Ab3d.Common.Models;
using Ab3d.Utilities;
using Ab3d.Visuals;
using Assimp;

namespace Ab3d.PowerToys.Samples.AssimpSamples
{
    /// <summary>
    /// Interaction logic for SkeletalAnimation.xaml
    /// </summary>
    public partial class SkeletalAnimation : Page
    {
        private AssimpAnimationController _assimpAnimationController;

        private Model3D _lastLoadedModel3D;

        private double _lastUIFrameNumber;

        private bool _isAnimationStarted;
        private Scene _assimpScene;

        public SkeletalAnimation()
        {
            InitializeComponent();

            // Use helper class (defined in this sample project) to load the native assimp libraries
            // IMPORTANT: See commend in the AssimpLoader class for details on how to prepare your project to use assimp library.
            AssimpLoader.LoadAssimpNativeLibrary();

            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\soldier.X");

            LoadFileWithSkinnedAnimation(fileName);

            var dragAndDropHelper = new DragAndDropHelper(ViewportBorder, ".*");
            dragAndDropHelper.FileDroped += (sender, e) =>
            {
                LoadFileWithSkinnedAnimation(e.FileName);
            };

            // Stop the animation when user leaves this sample.
            this.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                if (_assimpAnimationController != null)
                {
                    _assimpAnimationController.StopAnimation();
                    _assimpAnimationController = null;
                }
            };
        }

        private void LoadFileWithSkinnedAnimation(string fileName)
        {
            InfoTextBox.Text = "";

            // Create an instance of AssimpWpfImporter
            var assimpWpfImporter = new AssimpWpfImporter();


            Model3D readModel3D;

            try
            {
                readModel3D = assimpWpfImporter.ReadModel3D(fileName,  texturesPath: null); // we can also define a textures path if the textures are located in some other directory (this is parameter can be skipped, but is defined here so you will know that you can use it)
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error reading file:\r\n{0}\r\n\r\n{1}", fileName, ex.Message));
                return;
            }

            _lastLoadedModel3D = readModel3D;

            ModelSilhueteVisual3D.Children.Clear();
            ModelSilhueteVisual3D.Children.Add(readModel3D.CreateModelVisual3D());

            // Set camera to show the whole model
            Camera1.TargetPosition = readModel3D.Bounds.GetCenterPosition();
            Camera1.Distance = readModel3D.Bounds.GetDiagonalLength() * 1.2;


            BoneMarkersVisual3D.Children.Clear();

            // Stop current animation if it was running
            if (_assimpAnimationController != null)
            {
                _assimpAnimationController.StopAnimation();
                _assimpAnimationController = null;
            }
            

            _assimpScene = assimpWpfImporter.ImportedAssimpScene;

            if (_assimpScene.AnimationCount == 0)
            {
                // No animation in the file
                AnimationSelectionComboBox.IsEnabled = false;
                AnimationSelectionComboBox.ItemsSource = new string[] { "(no animation defined)" };
                AnimationSelectionComboBox.SelectedIndex = 0;

                ShowBonesCheckBox.IsEnabled = false;
                ShowBonesCheckBox.IsChecked = false;

                AnimationSlider.IsEnabled = false;

                StartStopAnimationButton.IsEnabled = false;

                UpdateAnimationUI(0);

                return;
            }

            AnimationSlider.IsEnabled = true;
            StartStopAnimationButton.IsEnabled = true;


            try
            {
                // Create AssimpAnimationController - it will play the keyframe and skeletal animation
                _assimpAnimationController = new AssimpAnimationController(assimpWpfImporter);
                _assimpAnimationController.AutoReverse = false;
                _assimpAnimationController.AutoRepeat = true;

                _assimpAnimationController.AfterFrameUpdated += OnAssimpAnimationControllerOnAfterFrameUpdated;


                SetupAnimationUI();
                UpdateBoneMarkersAndUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting animation:\r\n" + ex.Message);
                return;
            }

            // Setup animation names
            var animationNames = _assimpScene.Animations.Select(a => a.Name).ToList();

            AnimationSelectionComboBox.IsEnabled = true;
            AnimationSelectionComboBox.ItemsSource = animationNames;
            
            int startupIndex = animationNames.IndexOf("Run"); // Start with "Run" animation if it exists
            if (startupIndex < 0)
                startupIndex = 0;

            AnimationSelectionComboBox.SelectedIndex = startupIndex; // This will call ChangeAnimationName method


            if (_assimpAnimationController.HasSkeletalAnimation)
            {
                ShowBonesCheckBox.IsEnabled = true;
                ShowBonesCheckBox.ToolTip = null;

                // In case ShowBonesCheckBox is checked then set model opacity to 0.8
                UpdateModelOpacity();
            }
            else
            {
                // No skeletal animation (probably only keyframe animation)
                ShowBonesCheckBox.IsEnabled = false;
                ShowBonesCheckBox.IsChecked = false;

                ShowBonesCheckBox.ToolTip = "This files does not define any skeletal animation.";
            }

            //StartAnimation();
        }

        private void OnAssimpAnimationControllerOnAfterFrameUpdated(object sender, EventArgs args)
        {
            UpdateBoneMarkersAndUI();
        }

        private void ChangeAnimationName(string animationName)
        {
            if (_assimpAnimationController == null)
                return;

            if (animationName == null)
                _assimpAnimationController.SelectAnimation((global::Assimp.Animation)null);
            else
                _assimpAnimationController.SelectAnimation(animationName); // NOTE that if animation does not exist, this method will throw exception

            if (!_assimpAnimationController.IsAnimating)
            {
                // In case we change animation to some other animation, the current animation is stopped
                _isAnimationStarted = false;
                StartStopAnimationButton.Content = "Start animation";

                SetupAnimationUI();
            }
        }

        private void UpdateBoneMarkersAndUI()
        {
            if (_assimpAnimationController == null)
                return;

            UpdateAnimationUI(_assimpAnimationController.CurrentFrameNumber);

            if (_assimpAnimationController.HasSkeletalAnimation)
            {
                BoneMarkersVisual3D.Children.Clear();

                if (ShowBonesCheckBox.IsChecked ?? false)
                {
                    foreach (var skeleton in _assimpAnimationController.Skeletons)
                    {
                        //AddBoneMarkers(skeleton.RootSkeletonNode, 1, new MatrixTransform3D(skeleton.RootSkeletonNodeMatrix));
                        AddBoneMarkers(skeleton.RootSkeletonNode, 1, null);
                    }
                }
            }
        }

        private void AddBoneMarkers(SkeletonNode skeletonNode, int level, Transform3D globalTransform)
        {
            if (skeletonNode.Parent != null)
            {
                var startPositionMatrix = skeletonNode.Parent.CurrentWorldMatrix;
                var endPositionMatrix   = skeletonNode.CurrentWorldMatrix;

                var lineVisual3D = new LineVisual3D()
                {
                    StartPosition = new Point3D(startPositionMatrix.OffsetX, startPositionMatrix.OffsetY, startPositionMatrix.OffsetZ),
                    EndPosition = new Point3D(endPositionMatrix.OffsetX, endPositionMatrix.OffsetY, endPositionMatrix.OffsetZ),
                    LineThickness = Math.Max(1, (5 - level) * 2), // level 1 => Thickness = 8; level 2 => Thickness = 6; ... Level 4+ => Thickness = 2
                    LineColor = Colors.Red,
                    EndLineCap = LineCap.ArrowAnchor
                };

                lineVisual3D.Transform = globalTransform;

                BoneMarkersVisual3D.Children.Add(lineVisual3D);
            }

            foreach (var childBone in skeletonNode.Children)
                AddBoneMarkers(childBone, level + 1, globalTransform);
        }

        private void SetupAnimationUI()
        {
            if (_assimpAnimationController == null)
                return;

            AnimationSlider.Minimum = 0;
            AnimationSlider.Maximum = _assimpAnimationController.LastFrameNumber;

            _lastUIFrameNumber = double.MinValue; // make sure we update the UI
            UpdateAnimationUI(0);
        }

        private void UpdateAnimationUI(double currentFrameNumber)
        {
            double framesToUpdate;

            if (_assimpAnimationController != null)
                framesToUpdate = _assimpAnimationController.FramesPerSecond / 10; // update 10 times per second
            else
                framesToUpdate = 1;

            if (Math.Abs(_lastUIFrameNumber - currentFrameNumber) < framesToUpdate) // Do not report to many times to prevent slowing down the animation
                return;

            AnimationInfoTextBlock.Text = $"Frame {currentFrameNumber:0.0} / {AnimationSlider.Maximum:0}";

            if (_assimpAnimationController != null && _assimpAnimationController.FramesPerSecond > 0)
                AnimationInfoTextBlock.Text += $" ({currentFrameNumber / _assimpAnimationController.FramesPerSecond:0.0} s / {AnimationSlider.Maximum / _assimpAnimationController.FramesPerSecond:0.0} s)";

            AnimationSlider.Value = currentFrameNumber;

            _lastUIFrameNumber = currentFrameNumber;
        }

        private void StartAnimation()
        {
            if (!_isAnimationStarted)
            {
                if (_assimpAnimationController != null)
                    _assimpAnimationController.StartAnimation();

                StartStopAnimationButton.Content = "Stop animation";
                _isAnimationStarted = true;
            }
        }

        private void StopAnimation()
        {
            if (_isAnimationStarted)
            {
                if (_assimpAnimationController != null)
                    _assimpAnimationController.StopAnimation();

                StartStopAnimationButton.Content = "Start animation";
                _isAnimationStarted = false;
            }
        }

        private void StartStopAnimationButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isAnimationStarted)
                StopAnimation();
            else
                StartAnimation();
        }

        private void AnimationSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!this.IsLoaded || _isAnimationStarted)
                return;

            if (_assimpAnimationController != null && Math.Abs(_assimpAnimationController.CurrentFrameNumber - AnimationSlider.Value) > 0.1)
                _assimpAnimationController.GoToFrame(AnimationSlider.Value);
        }

        private void OnShowBonesCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            UpdateModelOpacity();
            UpdateBoneMarkersAndUI();
        }

        private void UpdateModelOpacity()
        {
            if (_lastLoadedModel3D == null)
                return;

            double newMaterialOpacity = (ShowBonesCheckBox.IsChecked ?? false) ? 0.7 : 1.0;
            Ab3d.Utilities.ModelUtils.SetMaterialOpacity(_lastLoadedModel3D, newMaterialOpacity);
        }

        private void AnimationSelectionComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AnimationSelectionComboBox.IsEnabled)
                return;

            string animationName = (string)AnimationSelectionComboBox.SelectedItem;
            ChangeAnimationName(animationName);
        }

        private void ShowInfoButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_assimpAnimationController == null || _assimpAnimationController.Skeletons == null)
            {
                InfoTextBox.Text = "";
                return;
            }


            var sb = new StringBuilder();

            var assimpSceneInfoText = AssimpDumper.GetDumpString(_assimpScene);
            sb.AppendLine(assimpSceneInfoText);

            foreach (var skeleton in _assimpAnimationController.Skeletons)
            {
                if (skeleton.AssimpMesh != null)
                {
                    //sb.AppendFormat("Skeleton MeshName: '{0}'\r\nRootSkeletonNodeMatrix:\r\n", skeleton.AssimpMesh.Name);
                    //sb.Append(Ab3d.Utilities.Dumper.GetMatrix3DText(skeleton.RootSkeletonNodeMatrix, "", "\r\n", 2));

                    sb.AppendFormat("Skeleton MeshName: '{0}'", skeleton.AssimpMesh.Name);
                    sb.AppendLine();

                    AddBonesInfo(skeleton.RootSkeletonNode, sb, 0, skipNodesWithoutName: false);

                    sb.AppendLine();
                }
            }

            InfoTextBox.Text = sb.ToString();

            if (InfoTextBox.Visibility != Visibility.Visible)
            {
                InfoRow.Height         = new GridLength(160, GridUnitType.Pixel);
                InfoTextBox.Visibility = Visibility.Visible;
            }
        }

        private void AddBonesInfo(SkeletonNode skeletonNode, StringBuilder sb, int indent, bool skipNodesWithoutName)
        {
            if (skipNodesWithoutName && string.IsNullOrEmpty(skeletonNode.AssimpNode.Name))
                return;

            string indentString = indent == 0 ? "" : new string(' ', indent);

            
            sb.Append(indentString)
              .Append('"' + skeletonNode.AssimpNode.Name + '"')
              .AppendLine();


            var matrices = new List<Matrix3D>(4);
            var matrixTitles = new List<string>(4);

            matrices.Add(skeletonNode.AssimpNode.Transform.ToWpfMatrix3D());
            matrixTitles.Add("NodeMatrix:");

            matrices.Add(skeletonNode.CurrentWorldMatrix);
            matrixTitles.Add("CurrentWorldMatrix:");

            if (skeletonNode.AssimpBone != null)
            {
                matrices.Add(skeletonNode.BoneOffsetMatrix);
                matrixTitles.Add("BoneMatrix:");
            }

            matrices.Add(skeletonNode.FinalMatrix);
            matrixTitles.Add("FinalMatrix:");


            var allMatricesText = Ab3d.Utilities.Dumper.FormatMatricesHorizontally(matrices.ToArray(), matrixTitles.ToArray(), new string(' ', indent));
            sb.AppendLine(allMatricesText);

            foreach (var skeletonNodeChild in skeletonNode.Children)
                AddBonesInfo(skeletonNodeChild, sb, indent + 4, skipNodesWithoutName);
        }
    }
}
