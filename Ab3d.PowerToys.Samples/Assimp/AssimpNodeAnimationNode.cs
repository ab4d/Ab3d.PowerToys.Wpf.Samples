using System;
using System.Diagnostics;
using System.Windows.Media.Media3D;
using Ab3d.Animation;
using Ab3d.Assimp;
using Assimp;

namespace Ab3d.Assimp
{
    /// <summary>
    /// AssimpNodeAnimationNode is an animation node that can transform WPF Model3D based on the Assimp's NodeAnimationChannel.
    /// </summary>
    [DebuggerDisplay("AssimpNodeAnimationNode: {NodeName}")]
    public class AssimpNodeAnimationNode : Transform3DAnimationNode
    {
        private readonly Model3D _animatedModel3D;
        private readonly SkeletonNode _animatedSkeletonNode;

        /// <summary>
        /// Assimp's NodeAnimationChannel
        /// </summary>
        public NodeAnimationChannel NodeAnimationChannel { get; private set; }

        /// <summary>
        /// Gets a name of the animation node
        /// </summary>
        public string NodeName
        {
            get { return NodeAnimationChannel.NodeName; }
        }

        /// <summary>
        /// Gets a Transform3D from the animation node.
        /// </summary>
        public Transform3D Transform
        {
            get { return rootTransform3DGroup; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeAnimationChannel">Assimp NodeAnimationChannel</param>
        /// <param name="animatedModel3D">WPF Model3D</param>
        public AssimpNodeAnimationNode(NodeAnimationChannel nodeAnimationChannel, Model3D animatedModel3D)
            : this(nodeAnimationChannel)
        {
            if (animatedModel3D == null)
                throw new ArgumentNullException(nameof(animatedModel3D));

            _animatedModel3D = animatedModel3D;
            _animatedSkeletonNode = null;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeAnimationChannel">Assimp NodeAnimationChannel</param>
        /// <param name="animatedSkeletonNode">SkeletonNode</param>
        public AssimpNodeAnimationNode(NodeAnimationChannel nodeAnimationChannel, SkeletonNode animatedSkeletonNode)
            : this(nodeAnimationChannel)
        {
            if (animatedSkeletonNode == null)
                throw new ArgumentNullException(nameof(animatedSkeletonNode));

            //if (nodeAnimationChannel.NodeName != animatedSkeletonNode.AssimpNode.Name)
            //    throw new Exception("nodeAnimationChannel and animatedSkeletonNode that were used to create an instance of AssimpNodeAnimationNode are not connected to each other (do not have the same name)");

            _animatedModel3D = null;
            _animatedSkeletonNode = animatedSkeletonNode;
        }

        /// <summary>
        /// protected Constructor
        /// </summary>
        /// <param name="nodeAnimationChannel">Assimp NodeAnimationChannel</param>
        protected AssimpNodeAnimationNode(NodeAnimationChannel nodeAnimationChannel)
        {
            if (nodeAnimationChannel == null)
                throw new ArgumentNullException(nameof(nodeAnimationChannel));

            NodeAnimationChannel = nodeAnimationChannel;

            if (nodeAnimationChannel.HasPositionKeys)
            {
                foreach (var positionKey in nodeAnimationChannel.PositionKeys)
                    this.PositionTrack.Keys.Add(new Position3DKeyFrame(positionKey.Time, positionKey.Value.ToWpfPoint3D()));
            }

            if (nodeAnimationChannel.HasScalingKeys)
            {
                foreach (var scalingKeys in nodeAnimationChannel.ScalingKeys)
                    this.ScaleTrack.Keys.Add(new Vector3DKeyFrame(scalingKeys.Time, scalingKeys.Value.ToWpfVector3D()));
            }

            if (nodeAnimationChannel.HasRotationKeys)
            {
                foreach (var quaternionKey in nodeAnimationChannel.RotationKeys)
                    this.RotationTrack.Keys.Add(new QuaternionRotationKeyFrame(quaternionKey.Time, new System.Windows.Media.Media3D.Quaternion(quaternionKey.Value.X, quaternionKey.Value.Y, quaternionKey.Value.Z, quaternionKey.Value.W)));
            }
        }


        /// <summary>
        /// UpdateAnimatedObject method updates the animatedModel3D or animatedSkeletonNode based on the current Transform.
        /// </summary>
        public void UpdateAnimatedObject()
        {
            if (_animatedModel3D != null)
                _animatedModel3D.Transform = rootTransform3DGroup;
            else if (_animatedSkeletonNode != null)
                _animatedSkeletonNode.CurrentNodeMatrix = rootTransform3DGroup.Value;
        }
    }
}