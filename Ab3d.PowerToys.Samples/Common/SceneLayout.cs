﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ab3d.Cameras;
using Ab3d.PowerToys.Samples.UseCases;
using Ab3d.Visuals;

namespace Ab3d.PowerToys.Samples.Common
{
    public abstract class SceneLayout
    {
        /// <summary>
        /// Gets name of the SceneLayout
        /// </summary>
        public string Name { get; private set; } 

        /// <summary>
        /// Gets or sets a margin between different SceneView3D borders. This value must be set before the layout is activated.
        /// </summary>
        public double BorderMargin { get; set; }

        /// <summary>
        /// Gets number of SceneView3D objects.
        /// </summary>
        public abstract int SceneViewsCount { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">name</param>
        protected SceneLayout(string name)
        {
            BorderMargin = 8;
            Name = name;
        }

        /// <summary>
        /// ActivateLayout sets up the columns and rows in the parentGrid and creates new SceneView3D objects and add them to the Grid.
        /// </summary>
        /// <param name="parentGrid">parent Grid</param>
        public abstract void ActivateLayout(Grid parentGrid);

        /// <summary>
        /// SetupGrid sets up the columns and rows in the parentGrid.
        /// </summary>
        /// <param name="parentGrid">parent Grid</param>
        protected abstract void SetupGrid(Grid parentGrid);

        /// <summary>
        /// AddUIElement adds the elementToAdd to the parentGrid and sets the required column, row and margin according to the elementIndex.
        /// </summary>
        /// <param name="parentGrid">parent Grid</param>
        /// <param name="elementIndex">index of this element</param>
        /// <param name="elementToAdd">FrameworkElement to add</param>
        protected abstract void AddUIElement(Grid parentGrid, int elementIndex, FrameworkElement elementToAdd);

        /// <summary>
        /// CreateLayoutSchema creates a Grid that contains Borders instead of SceneView3D objects.
        /// This represents a schema of the layout.
        /// </summary>
        /// <returns>FrameworkElement with schema of the layout</returns>
        public FrameworkElement CreateLayoutSchema()
        {
            var grid = new Grid();
            SetupGrid(grid);

            for (var i = 0; i < SceneViewsCount; i++)
            {
                var border = new Border()
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1, 1, 1, 1),
                    Background = Brushes.Silver
                };

                // Override margin to 2 for the schema
                double savedBorderMargin = this.BorderMargin;
                this.BorderMargin = 2;

                AddUIElement(grid, i, border);

                this.BorderMargin = savedBorderMargin;
            }

            return grid;
        }
    }

    class PerspectiveViewLayout : SceneLayout
    {
        public override int SceneViewsCount
        {
            get { return 1; }
        }

        public PerspectiveViewLayout() 
            : base("Perspective")
        {
        }

        public override void ActivateLayout(Grid parentGrid)
        {
            parentGrid.BeginInit();

            parentGrid.Children.Clear();

            SetupGrid(parentGrid);

            var sceneView3D = new SceneView3D();
            sceneView3D.SelectedSceneViewType = SceneViewType.StandardCustomSceneView;
            sceneView3D.Camera1.CameraType = BaseCamera.CameraTypes.PerspectiveCamera;
            sceneView3D.WireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.OriginalSolidModel;
            sceneView3D.WireframeVisual.UseModelColor = true;

            AddUIElement(parentGrid, 0, sceneView3D);

            parentGrid.EndInit();
        }

        protected override void SetupGrid(Grid parentGrid)
        {
            parentGrid.ColumnDefinitions.Clear();
            parentGrid.RowDefinitions.Clear();

            parentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            parentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
        }

        protected override void AddUIElement(Grid parentGrid, int elementIndex, FrameworkElement elementToAdd)
        {
            Grid.SetColumn(elementToAdd, 0);
            Grid.SetRow(elementToAdd, 0);

            parentGrid.Children.Add(elementToAdd);
        }
    }

    class TwoColumnsViewLayout : SceneLayout
    {
        public override int SceneViewsCount
        {
            get { return 2; }
        }

        public TwoColumnsViewLayout() 
            : base("TwoColumns")
        {
        }

        protected override void SetupGrid(Grid parentGrid)
        {
            parentGrid.ColumnDefinitions.Clear();
            parentGrid.RowDefinitions.Clear();

            // 2 x SceneView3D:
            //
            // ** | **
            // ** | **
            // ** | **

            parentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            parentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            parentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
        }

        protected override void AddUIElement(Grid parentGrid, int elementIndex, FrameworkElement elementToAdd)
        {
            double halfMargin = BorderMargin * 0.5;

            if (elementIndex == 0)
            {
                elementToAdd.Margin = new Thickness(0, 0, halfMargin, 0);
                Grid.SetColumn(elementToAdd, 0);
            }
            else
            {
                elementToAdd.Margin = new Thickness(halfMargin, 0, 0, 0);
                Grid.SetColumn(elementToAdd, 1);
            }

            Grid.SetRow(elementToAdd, 0);

            parentGrid.Children.Add(elementToAdd);
        }

        public override void ActivateLayout(Grid parentGrid)
        {
            parentGrid.BeginInit();

            parentGrid.Children.Clear();

            SetupGrid(parentGrid);

            var sceneView3D = new SceneView3D();
            sceneView3D.SelectedSceneViewType = SceneViewType.StandardCustomSceneView;
            sceneView3D.Camera1.CameraType = BaseCamera.CameraTypes.PerspectiveCamera;
            sceneView3D.WireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.OriginalSolidModel;
            sceneView3D.WireframeVisual.UseModelColor = true;

            AddUIElement(parentGrid, 0, sceneView3D);


            sceneView3D = new SceneView3D();
            sceneView3D.SelectedSceneViewType = SceneViewType.Get("top");
            sceneView3D.Camera1.CameraType = BaseCamera.CameraTypes.PerspectiveCamera;
            sceneView3D.WireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.Wireframe;
            sceneView3D.WireframeVisual.UseModelColor = false;

            AddUIElement(parentGrid, 1, sceneView3D);

            parentGrid.EndInit();
        }
    }

    class PerspectiveTopFrontViewLayout : SceneLayout
    {
        public override int SceneViewsCount
        {
            get { return 3; }
        }

        public PerspectiveTopFrontViewLayout() 
            : base("Perspective Top Front")
        {
        }

        protected override void SetupGrid(Grid parentGrid)
        {
            parentGrid.ColumnDefinitions.Clear();
            parentGrid.RowDefinitions.Clear();

            // 3 x SceneView3D:
            //
            //  * | **
            // ---| **
            //  * | **

            parentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }); // 1*
            parentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) }); // 2*

            parentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            parentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
        }

        protected override void AddUIElement(Grid parentGrid, int elementIndex, FrameworkElement elementToAdd)
        {
            double halfMargin = BorderMargin * 0.5;

            switch (elementIndex)
            {
                case 0:
                    elementToAdd.Margin = new Thickness(halfMargin, 0, 0, 0);

                    Grid.SetColumn(elementToAdd, 1);
                    Grid.SetRow(elementToAdd, 0);
                    Grid.SetRowSpan(elementToAdd, 2);

                    break;

                case 1:
                    elementToAdd.Margin = new Thickness(0, 0, halfMargin, halfMargin);

                    Grid.SetColumn(elementToAdd, 0);
                    Grid.SetRow(elementToAdd, 0);

                    break;

                case 2:
                    elementToAdd.Margin = new Thickness(0, halfMargin, halfMargin, 0);

                    Grid.SetColumn(elementToAdd, 0);
                    Grid.SetRow(elementToAdd, 1);

                    break;
            }

            parentGrid.Children.Add(elementToAdd);
        }

        public override void ActivateLayout(Grid parentGrid)
        {
            parentGrid.BeginInit();

            parentGrid.Children.Clear();

            // 3 x SceneView3D:
            //
            //  * | **
            // ---| **
            //  * | **

            SetupGrid(parentGrid);


            var sceneView3D = new SceneView3D();
            sceneView3D.SelectedSceneViewType = SceneViewType.StandardCustomSceneView;
            sceneView3D.Camera1.CameraType = BaseCamera.CameraTypes.PerspectiveCamera;
            sceneView3D.WireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.OriginalSolidModel;
            sceneView3D.WireframeVisual.UseModelColor = true;

            AddUIElement(parentGrid, 0, sceneView3D);



            sceneView3D = new SceneView3D();
            sceneView3D.SelectedSceneViewType = SceneViewType.Get("top");
            sceneView3D.Camera1.CameraType = BaseCamera.CameraTypes.OrthographicCamera;
            sceneView3D.WireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.Wireframe;
            sceneView3D.WireframeVisual.UseModelColor = false;

            AddUIElement(parentGrid, 1, sceneView3D);



            sceneView3D = new SceneView3D();
            sceneView3D.SelectedSceneViewType = SceneViewType.Get("front");
            sceneView3D.Camera1.CameraType = BaseCamera.CameraTypes.OrthographicCamera;
            sceneView3D.WireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.Wireframe;
            sceneView3D.WireframeVisual.UseModelColor = false;

            AddUIElement(parentGrid, 2, sceneView3D);

            parentGrid.EndInit();
        }
    }

    class TopLeftFrontPerspectiveViewLayout : SceneLayout
    {
        public override int SceneViewsCount
        {
            get { return 4; }
        }

        public TopLeftFrontPerspectiveViewLayout() 
            : base("Top Left Front Perspective")
        {
        }

        protected override void SetupGrid(Grid parentGrid)
        {
            parentGrid.ColumnDefinitions.Clear();
            parentGrid.RowDefinitions.Clear();

            // 4 x SceneView3D:
            //
            //  * | *
            // ---|---
            //  * | *

            parentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }); 
            parentGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }); 

            parentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            parentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
        }

        protected override void AddUIElement(Grid parentGrid, int elementIndex, FrameworkElement elementToAdd)
        {
            double halfMargin = BorderMargin * 0.5;

            switch (elementIndex)
            {
                case 0: // top left
                    elementToAdd.Margin = new Thickness(0, 0, halfMargin, halfMargin);

                    Grid.SetColumn(elementToAdd, 0);
                    Grid.SetRow(elementToAdd, 0);

                    break;

                case 1: // top right
                    elementToAdd.Margin = new Thickness(halfMargin, 0, 0, halfMargin);

                    Grid.SetColumn(elementToAdd, 1);
                    Grid.SetRow(elementToAdd, 0);

                    break;

                case 2: // bottom left
                    elementToAdd.Margin = new Thickness(0, halfMargin, halfMargin, 0);

                    Grid.SetColumn(elementToAdd, 0);
                    Grid.SetRow(elementToAdd, 1);

                    break;

                case 3: // bottom right
                    elementToAdd.Margin = new Thickness(halfMargin, halfMargin, 0, 0);

                    Grid.SetColumn(elementToAdd, 1);
                    Grid.SetRow(elementToAdd, 1);

                    break;
            }

            parentGrid.Children.Add(elementToAdd);
        }

        public override void ActivateLayout(Grid parentGrid)
        {
            parentGrid.BeginInit();

            parentGrid.Children.Clear();

            // 4 x SceneView3D:
            //
            //  * | *
            // ---|---
            //  * | *

            SetupGrid(parentGrid);


            var sceneView3D = new SceneView3D();
            sceneView3D.SelectedSceneViewType = SceneViewType.Get("top");
            sceneView3D.Camera1.CameraType = BaseCamera.CameraTypes.OrthographicCamera;
            sceneView3D.WireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.Wireframe;
            sceneView3D.WireframeVisual.UseModelColor = false;

            AddUIElement(parentGrid, 0, sceneView3D);



            sceneView3D = new SceneView3D();
            sceneView3D.SelectedSceneViewType = SceneViewType.Get("front");
            sceneView3D.Camera1.CameraType = BaseCamera.CameraTypes.OrthographicCamera;
            sceneView3D.WireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.Wireframe;
            sceneView3D.WireframeVisual.UseModelColor = false;

            AddUIElement(parentGrid, 1, sceneView3D);



            sceneView3D = new SceneView3D();
            sceneView3D.SelectedSceneViewType = SceneViewType.Get("left");
            sceneView3D.Camera1.CameraType = BaseCamera.CameraTypes.OrthographicCamera;
            sceneView3D.WireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.Wireframe;
            sceneView3D.WireframeVisual.UseModelColor = false;

            AddUIElement(parentGrid, 2, sceneView3D);



            sceneView3D = new SceneView3D();
            sceneView3D.SelectedSceneViewType = SceneViewType.StandardCustomSceneView;
            sceneView3D.Camera1.CameraType = BaseCamera.CameraTypes.PerspectiveCamera;
            sceneView3D.WireframeVisual.WireframeType = WireframeVisual3D.WireframeTypes.OriginalSolidModel;
            sceneView3D.WireframeVisual.UseModelColor = true;

            AddUIElement(parentGrid, 3, sceneView3D);


            parentGrid.EndInit();
        }
    }
}