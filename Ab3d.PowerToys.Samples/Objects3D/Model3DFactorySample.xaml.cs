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

namespace Ab3d.PowerToys.Samples.Objects3D
{
    /// <summary>
    /// Interaction logic for Model3DFactorySample.xaml
    /// </summary>
    public partial class Model3DFactorySample : Page
    {
        public Model3DFactorySample()
        {
            InitializeComponent();

            CreateModels();
        }

        private void CreateModels()
        {
            var material = this.FindResource("ObjectsMaterial") as Material;

            //<visuals:WireGridVisual3D CenterPosition="0 0 0" Size="100 100" WidthCellsCount="10" HeightCellsCount="10" LineColor="#555555" LineThickness="2"/>
            MainModel3DGroup.Children.Add(Ab3d.Models.Line3DFactory.CreateHorizontalWireGrid(new Point3D(0,0,0), new Size(100, 100), 10, 10, 2.0, Color.FromRgb(85, 85, 85), MainViewport));

            //<visuals:ConeVisual3D BottomCenterPosition="-30 0 -30" BottomRadius="10" TopRadius="0" Height="20" Material="{StaticResource ObjectsMaterial}"/>
            //<visuals:ConeVisual3D BottomCenterPosition="0 0 -30" BottomRadius="10" TopRadius="5" Height="20" Material="{StaticResource ObjectsMaterial}"/>
            //<visuals:ConeVisual3D BottomCenterPosition="30 0 -30" BottomRadius="10" TopRadius="5" Height="20" Segments="6" IsSmooth="False" Material="{StaticResource ObjectsMaterial}"/>
            MainModel3DGroup.Children.Add(Ab3d.Models.Model3DFactory.CreateCone(new Point3D(-30, 0, -30), 0, 10, 20, 30, true, material));
            MainModel3DGroup.Children.Add(Ab3d.Models.Model3DFactory.CreateCone(new Point3D(0, 0, -30), 5, 10, 20, 30, true, material));
            MainModel3DGroup.Children.Add(Ab3d.Models.Model3DFactory.CreateCone(new Point3D(30, 0, -30), 5, 10, 20, 6, false, material));

            //<visuals:SphereVisual3D CenterPosition="-30 10 0" Radius="10" Material="{StaticResource ObjectsMaterial}"/>
            //<visuals:CylinderVisual3D BottomCenterPosition="0 0 0" Radius="10" Height="20" Material="{StaticResource ObjectsMaterial}"/>
            //<visuals:CylinderVisual3D BottomCenterPosition="30 0 0" Radius="10" Height="20" Segments="6" IsSmooth="False" Material="{StaticResource ObjectsMaterial}"/>
            MainModel3DGroup.Children.Add(Ab3d.Models.Model3DFactory.CreateSphere(new Point3D(-30, 10, 0), 10, 30, material));
            MainModel3DGroup.Children.Add(Ab3d.Models.Model3DFactory.CreateCylinder(new Point3D(0, 0, 0), 10, 20, 30, true, material));
            MainModel3DGroup.Children.Add(Ab3d.Models.Model3DFactory.CreateCylinder(new Point3D(30, 0, 0), 10, 20, 6, false, material));

            //<visuals:PlaneVisual3D CenterPosition="-30 1 30" Size="20 20" Normal="0 1 0" HeightDirection="0 0 -1" Material="{StaticResource ObjectsMaterial}" BackMaterial="{StaticResource ObjectsMaterial}"/>
            //<visuals:PyramidVisual3D BottomCenterPosition="0 0 30" Size="20 20 20" Material="{StaticResource ObjectsMaterial}"/>
            //<visuals:BoxVisual3D CenterPosition="30 10 30" Size="20 20 20" Material="{StaticResource ObjectsMaterial}"/>
            MainModel3DGroup.Children.Add(Ab3d.Models.Model3DFactory.CreatePlane(new Point3D(-30, 1, 30), new Vector3D(0, 1, 0), new Vector3D(0, 0, -1), new Size(20, 20), 1, 1, material));
            MainModel3DGroup.Children.Add(Ab3d.Models.Model3DFactory.CreatePyramid(new Point3D(0, 0, 30), new Size3D(20, 20, 20), material));
            MainModel3DGroup.Children.Add(Ab3d.Models.Model3DFactory.CreateBox(new Point3D(30, 10, 30), new Size3D(20, 20, 20), material));

            //<visuals:MultiMaterialBoxVisual3D CenterPosition="60 10 30" Size="20 20 20" FallbackMaterial="{StaticResource ObjectsMaterial}" TopMaterial="Blue" LeftMaterial="Gray"/>
            MainModel3DGroup.Children.Add(Ab3d.Models.Model3DFactory.CreateMultiMaterialBox(new Point3D(60, 10, 30), new Size3D(20, 20, 20),
                                            new DiffuseMaterial(Brushes.Blue), material, new DiffuseMaterial(Brushes.Gray), material, material, material, false));
        }
    }
}
