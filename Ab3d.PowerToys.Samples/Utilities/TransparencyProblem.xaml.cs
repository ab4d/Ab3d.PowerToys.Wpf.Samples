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

namespace Ab3d.PowerToys.Samples.Utilities
{
    /*

    The reason for transparency problem in computer graphics
    ========================================================

    To understand why the transparency problem occurs, we need to know how graphics cards render the 3D scene.

    When rendering the 3D scene, the graphics card uses two buffers.Both buffers have the size of the Viewport and 
    represent each shown pixel. One buffer is called frame buffer and contains colors for each pixel. The other buffer
    is called depth buffer and contains float values that define depth values of the pixels (distances to the camera's near plane).

    For each triangle in the scene the graphics card goes through all pixels between three vertexes. For each pixel, a color and 
    distance from the camera are calculated.First, the distance to the camera is compared to the value at the same position 
    in the depth buffer.If the distance to the camera is bigger then the current value in the depth buffer, then this means that
    there is already a pixel that is closer to the camera and in this case this pixel is rejected.But if the distance to the camera
    is smaller than the existing value in the depth buffer, then this pixel is currently the closest to the camera - this means that
    the distance to the camera is written to the depth buffer and the color of the pixel is written to the frame buffer.

    This works very well for non-transparent object. There only the pixels that are the closest to the camera are shown to the user.

    But for transparent objects this algorithm is problematic.Transparent objects are rendered in a slightly different way that
    the non-transparent objects. There the color of the transparent object is blended with the existing color at the same 
    pixel location - simply speaking this means that both colors are added together (actually this is not 100% correct but to understand this problem this is ok).

    So when a transparent pixel is rendered, it is first checked if it is the closest pixel to the camera so far.
    If this is true, then the color of the existing pixel is read and blended with the color of the transparent object. 
    Then the blended color is written to the frame buffer.Also, the distance to the camera is written to the depth buffer.

    The last operation that writes the depth value can cause problems for the objects that are rendered after the transparent object. 
    If any of that object lies behind the transparent object, it should be also seen through the transparent object. 
    But the problem is that the transparent object has already written its depth value to the depth buffer and therefore the objects 
    that are behind transparent object will be rejected because of depth test.

    This means that all the objects that should be visible through the transparent object need to be rendered before the transparent object. 
    If they are rendered later, they will be rejected because the transparent object is closer to the camera.

    The solution to this problem is to render transparent objects are the last objects in the scene and ideally they should be sorted 
    so that those that are farther away from the camera are rendered first.

     */
    /// <summary>
    /// Interaction logic for TransparencyProblem.xaml
    /// </summary>
    public partial class TransparencyProblem : Page
    {
        public TransparencyProblem()
        {
            InitializeComponent();

            ShowOriginalObjects();
        }

        private void ManuallyMoveButton_Click(object sender, RoutedEventArgs e)
        {
            // Removing and adding GlassPlane model moves it to the last position in the __AllModelsGroup.Children
            __AllModelsGroup.Children.Remove(GlassPlane);
            __AllModelsGroup.Children.Add(GlassPlane);

            ShowSortedObjects();
        }

        private void TransparencySorterButton_Click(object sender, RoutedEventArgs e)
        {
            // We can also check if there are any transparent objects in the scene (for this sample this would not be needed but it is added to show the use of GetTransparentObjectsCount method).
            if (Ab3d.Utilities.TransparencyHelper.GetTransparentObjectsCount(__AllModelsGroup) == 0)
                return;

            // Static SimpleSort method re-arranges the objects in such a way that the transparent objects are moved to the back.
            // The possible parameters for SimpleSort method are: Model3DGroup, ModelVisual3D, Visual3DCollection, Viewport3D.

            Ab3d.Utilities.TransparencySorter.SimpleSort(__AllModelsGroup);
            //Ab3d.Utilities.TransparencySorter.SimpleSort(MainViewport3D);

            // When there are more transparent objects on the scene, it is not always enough to move transparent objects after non-transparent objects.
            // In this case the transparent objects also need to be sorted by their distance from the camera's position. The sorting is done in such a way
            // that the objects that are father away from the camera are drawn before objects that are closer to the camera.
            // The most simple way to do the sorting by camera distance is to use the following:
            // Ab3d.Utilities.TransparencySorter.SortByCameraDistance(__AllModelsGroup, cameraPosition);
            // To use a more complex example with many transparent objects see the "Model3D transparency sorting" or "Visual3D transparency sorting"

            ShowSortedObjects();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Reposition the GlassPlane to its initial position before red boxes
            __AllModelsGroup.Children.Remove(GlassPlane);
            __AllModelsGroup.Children.Insert(4, GlassPlane);

            ShowOriginalObjects();
        }


        private void ShowOriginalObjects()
        {
            // Because this sample is very simple, the objects names and their position inside the Model3DGroup are hardcoded
            ObjectsTextBox.Text = "Box01 (blue)\r\nBox02 (blue)\r\nBox03 (blue)\r\nBox04 (blue)\r\nGlassPlane (semi-transparent)\r\nBox05 (red)\r\nBox06 (red)\r\nBox07 (red)\r\nBox08 (red)";
        }

        private void ShowSortedObjects()
        {
            ObjectsTextBox.Text = "Box01 (blue)\r\nBox02 (blue)\r\nBox03 (blue)\r\nBox04 (blue)\r\nBox05 (red)\r\nBox06 (red)\r\nBox07 (red)\r\nBox08 (red)\r\nGlassPlane (semi-transparent)";
        }
    }
}
