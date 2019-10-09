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

namespace Ab3d.PowerToys.Samples.EventManager3D
{
    /// <summary>
    /// Interaction logic for EventManagerDragSample.xaml
    /// </summary>
    public partial class EventManagerDragSample : Page
    {
        private bool _isSelected;
        private Ab3d.Utilities.EventManager3D _eventManager;

        private DiffuseMaterial _selectedMaterial;
        private DiffuseMaterial _unSelectedMaterial;

        public EventManagerDragSample()
        {
            InitializeComponent();

            _selectedMaterial = new DiffuseMaterial(Brushes.Red);
            _unSelectedMaterial = new DiffuseMaterial(Brushes.Blue);

            this.Loaded += new RoutedEventHandler(EventManagerDragSample_Loaded);
        }

        void EventManagerDragSample_Loaded(object sender, RoutedEventArgs e)
        {
            Ab3d.Utilities.VisualEventSource3D eventSource3D;

            _eventManager = new Ab3d.Utilities.EventManager3D(Viewport3D1);

            // Exclude TransparentPlaneVisual3D from hit testing
            _eventManager.RegisterExcludedVisual3D(TransparentPlaneVisual3D);

            

            //eventSource3D = new Ab3d.Utilities.VisualEventSource3D();
            //eventSource3D.TargetVisual3D = LowerBoxVisual3D;
            //eventSource3D.Name = "Lower";
            //eventSource3D.IsDragSurface = true;

            //eventManager.RegisterEventSource3D(eventSource3D);


            //eventSource3D = new Ab3d.Utilities.VisualEventSource3D();
            //eventSource3D.TargetVisual3D = PassageBoxVisual3D;
            //eventSource3D.Name = "Passage";
            //eventSource3D.IsDragSurface = true;

            //eventManager.RegisterEventSource3D(eventSource3D);


            //eventSource3D = new Ab3d.Utilities.VisualEventSource3D();
            //eventSource3D.TargetVisual3D = UpperBoxVisual3D;
            //eventSource3D.Name = "Upper";
            //eventSource3D.IsDragSurface = true;

            //eventManager.RegisterEventSource3D(eventSource3D);


            Ab3d.Utilities.MultiVisualEventSource3D multiEventSource3D;

            multiEventSource3D = new Ab3d.Utilities.MultiVisualEventSource3D();
            multiEventSource3D.TargetVisuals3D = new Visual3D[] { LowerBoxVisual3D, PassageBoxVisual3D, UpperBoxVisual3D };
            multiEventSource3D.IsDragSurface = true;

            _eventManager.RegisterEventSource3D(multiEventSource3D);


            eventSource3D = new Ab3d.Utilities.VisualEventSource3D();
            eventSource3D.TargetVisual3D = MovableBoxVisual3D;
            eventSource3D.Name = "Movable";
            eventSource3D.MouseEnter += new Ab3d.Common.EventManager3D.Mouse3DEventHandler(eventSource3D_MouseEnter);
            eventSource3D.MouseLeave += new Ab3d.Common.EventManager3D.Mouse3DEventHandler(eventSource3D_MouseLeave);
            eventSource3D.MouseClick += new Ab3d.Common.EventManager3D.MouseButton3DEventHandler(movableEventSource3D_MouseClick);
            eventSource3D.MouseDrag += new Ab3d.Common.EventManager3D.MouseDrag3DEventHandler(movableEventSource3D_MouseDrag);

            _eventManager.RegisterEventSource3D(eventSource3D);
        }

        void eventSource3D_MouseLeave(object sender, Ab3d.Common.EventManager3D.Mouse3DEventArgs e)
        {
            Viewport3D1.Cursor = null;

            MovableBoxVisual3D.Material = _unSelectedMaterial;
            ArrowLineVisual3D.LineColor = Colors.Blue;
        }

        void eventSource3D_MouseEnter(object sender, Ab3d.Common.EventManager3D.Mouse3DEventArgs e)
        {
            Viewport3D1.Cursor = Cursors.Hand;

            MovableBoxVisual3D.Material = _selectedMaterial;
            ArrowLineVisual3D.LineColor = Colors.Red;
        }

        void movableEventSource3D_MouseDrag(object sender, Ab3d.Common.EventManager3D.MouseDrag3DEventArgs e)
        {
            if (e.HitSurface != null)
            {
                // Change the position of the box and arrow line

                // This can be done in a couple of ways:
                // The most easy is to change the CenterPosition of the box and StartPosition and EndPostition of the arrow line.
                // But this would recreate the geometry
                
                // Therfore it is better to add a transformation to the box and line and change that
                // Even better is to create a parent ModelVisual3D that holds box and arrow line and transform the this ModelVisual3D

                // Commented:
                //MovableBoxVisual3D.CenterPosition = new Point3D(e.CurrentSurfaceHitPoint.X,
                //                                                e.CurrentSurfaceHitPoint.Y + MovableBoxVisual3D.Size.Y / 2,
                //                                                e.CurrentSurfaceHitPoint.Z);

                //MovableBoxTranslate.OffsetX = e.CurrentSurfaceHitPoint.X;
                //MovableBoxTranslate.OffsetY = e.CurrentSurfaceHitPoint.Y + MovableBoxVisual3D.Size.Y / 2;
                //MovableBoxTranslate.OffsetZ = e.CurrentSurfaceHitPoint.Z;

                //ArrowLineTranslate.OffsetX = e.CurrentSurfaceHitPoint.X;
                //ArrowLineTranslate.OffsetY = e.CurrentSurfaceHitPoint.Y + MovableBoxVisual3D.Size.Y / 2;
                //ArrowLineTranslate.OffsetZ = e.CurrentSurfaceHitPoint.Z;

                MovableVisualTranslate.OffsetX = e.CurrentSurfaceHitPoint.X;
                MovableVisualTranslate.OffsetY = e.CurrentSurfaceHitPoint.Y + MovableBoxVisual3D.Size.Y / 2;
                MovableVisualTranslate.OffsetZ = e.CurrentSurfaceHitPoint.Z;
            }
        }

        void movableEventSource3D_MouseClick(object sender, Ab3d.Common.EventManager3D.MouseButton3DEventArgs e)
        {
            Material newMaterial;

            if (_isSelected)
                newMaterial = new DiffuseMaterial(Brushes.Blue);
            else
                newMaterial = new DiffuseMaterial(Brushes.Gold);

            MovableBoxVisual3D.Material = newMaterial;

            _isSelected = !_isSelected;
        }
    }
}
