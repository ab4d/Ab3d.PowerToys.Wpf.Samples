using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace Ab3d.PowerToys.Samples.OtherCameraControllers
{
    public class CustomControlPanel : Ab3d.Controls.CameraControlPanel
    {
        public CustomControlPanel()
            : base()
        { 
        }

        protected override System.Windows.Media.Imaging.BitmapSource GetUnSelectedBitmapForImageName(string imageName)
        {
            return GetBitmapForImageName(imageName, false);
        }

        protected override System.Windows.Media.Imaging.BitmapSource GetSelectedBitmapForImageName(string imageName)
        {
            return GetBitmapForImageName(imageName, true);
        }

        protected virtual BitmapSource GetBitmapForImageName(string imageName, bool isSelected)
        {
            BitmapImage bitmap;
            string selectedString;
            string imageUri;

            if (isSelected)
                selectedString = "_selected";
            else
                selectedString = "";

            // NOTE: The images build action is set to Resource
            imageUri = string.Format("/Resources/CustomControlPanel/{0}{1}.png", imageName, selectedString);

            bitmap = new BitmapImage(new Uri(imageUri, UriKind.RelativeOrAbsolute));

            return bitmap;
        }
    }
}
