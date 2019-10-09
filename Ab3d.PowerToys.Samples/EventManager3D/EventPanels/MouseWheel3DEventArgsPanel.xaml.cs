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
using Ab3d.Common.EventManager3D;
using System.Windows.Media.Media3D;
using Ab3d.Utilities;

namespace Ab3d.PowerToys.Samples.EventManager3D.EventPanels
{
    /// <summary>
    /// Interaction logic for MouseWheel3DEventArgsPanel.xaml
    /// </summary>
    public partial class MouseWheel3DEventArgsPanel : UserControl
    {
        public MouseWheel3DEventArgsPanel()
        {
            InitializeComponent();

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(MouseWheel3DEventArgsPanel_DataContextChanged);
        }

        void MouseWheel3DEventArgsPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Manually check if hit object was MultiModelEventSource3D or MultiVisualEventSource3D
            // and in this case show HitModelName / HitVisualName

            HitNameTitleTextBlock.Visibility = Visibility.Collapsed;
            HitNameValueTextBlock.Visibility = Visibility.Collapsed;

            if (this.DataContext is BaseMouse3DEventArgs)
            {
                BaseMouse3DEventArgs mouse3DEventArgs = this.DataContext as BaseMouse3DEventArgs;

                if (mouse3DEventArgs.HitEventSource3D is MultiModelEventSource3D)
                {
                    MultiModelEventSource3D multiModelEventSource3D = mouse3DEventArgs.HitEventSource3D as MultiModelEventSource3D;

                    if (!string.IsNullOrEmpty(multiModelEventSource3D.HitModelName))
                    {
                        HitNameValueTextBlock.Text = multiModelEventSource3D.HitModelName;

                        HitNameTitleTextBlock.Visibility = Visibility.Visible;
                        HitNameValueTextBlock.Visibility = Visibility.Visible;
                    }
                }
                else if (mouse3DEventArgs.HitEventSource3D is MultiVisualEventSource3D)
                {
                    MultiVisualEventSource3D multiVisualEventSource3D = mouse3DEventArgs.HitEventSource3D as MultiVisualEventSource3D;

                    if (!string.IsNullOrEmpty(multiVisualEventSource3D.HitVisualName))
                    {
                        HitNameValueTextBlock.Text = multiVisualEventSource3D.HitVisualName;

                        HitNameTitleTextBlock.Visibility = Visibility.Visible;
                        HitNameValueTextBlock.Visibility = Visibility.Visible;
                    }
                }
            }            
        }
    }
}
