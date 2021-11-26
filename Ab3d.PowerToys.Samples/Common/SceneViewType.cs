using System;
using System.Linq;

namespace Ab3d.PowerToys.Samples.Common
{
    public class SceneViewType
    {
        public readonly string Name;
        public readonly double Heading;
        public readonly double Attitude;

        public SceneViewType(string name, double heading, double attitude)
        {
            Name = name;
            Heading = heading;
            Attitude = attitude;
        }

        #region static StandardViews, StandardCustomSceneView, Get

        private static SceneViewType[] _standerViews;

        private static SceneViewType _standardCustomSceneView;

        public static SceneViewType[] StandardViews
        {
            get
            {
                if (_standerViews == null)
                    SetupStandardViews();

                return _standerViews;
            }
        }

        public static SceneViewType StandardCustomSceneView
        {
            get
            {
                if (_standardCustomSceneView == null)
                    SetupStandardViews();

                return _standardCustomSceneView;
            }
        }

        public static SceneViewType Get(string name)
        {
            if (_standerViews == null)
                SetupStandardViews();

            return _standerViews.FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.CurrentCultureIgnoreCase));
        }

        private static void SetupStandardViews()
        {
            _standardCustomSceneView = new SceneViewType("Custom", 20, -20);

            _standerViews = new SceneViewType[]
            {
                    _standardCustomSceneView,
                    new SceneViewType("Top", 0, -90),
                    new SceneViewType("Front", 0, 0),
                    new SceneViewType("Left", 90, 0),
                    new SceneViewType("Right", -90, 0),
                    new SceneViewType("Back", 180, 0),
                    new SceneViewType("Bottom", 0, 90)
            };
        }

        public override bool Equals(object obj)
        {
            var sceneViewType = obj as SceneViewType;
            if (sceneViewType == null)
                return false;

            return this.Name.Equals(sceneViewType.Name) &&
                   this.Heading.Equals(sceneViewType.Heading) &&
                   this.Attitude.Equals(sceneViewType.Attitude);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() ^ this.Heading.GetHashCode() ^ this.Attitude.GetHashCode();
        }

        #endregion
    }
}