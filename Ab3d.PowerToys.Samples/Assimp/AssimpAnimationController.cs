using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using Ab3d.Animation;
using Ab3d.Assimp;
using Assimp;

namespace Ab3d.Assimp
{
    /// <summary>
    /// AssimpAnimationController is an AnimationController that can play keyframe or skeletal (skinned) animations from 3D files read with Assimp importer.
    /// </summary>
    public class AssimpAnimationController : AnimationController
    {
        private readonly AssimpWpfImporter _assimpWpfImporter;
        private readonly Scene _assimpScene;

        /// <summary>
        /// Gets a list of Skeleton objects that define the skinned animation skeleton.
        /// When there is no skinned animation defined, this list contains no items.
        /// </summary>
        public List<Skeleton> Skeletons { get; private set; }

        /// <summary>
        /// Gets a Boolean that specifies if the Assimp scene defines any skeletal animation.
        /// </summary>
        public bool HasSkeletalAnimation
        {
            get { return Skeletons.Count > 0; }
        }

        /// <summary>
        /// Gets an Assimp.Animation object that represents the currently selected animation.
        /// </summary>
        public global::Assimp.Animation SelectedAnimation { get; private set; }


        // NOTE: We need assimpWpfImporter because of GetGeometryModel3DForAssimpMesh method

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="assimpWpfImporter">AssimpWpfImporter that was used to read the Assimp object (needed because of GetGeometryModel3DForAssimpMesh method)</param>
        public AssimpAnimationController(AssimpWpfImporter assimpWpfImporter)
        {
            if (assimpWpfImporter == null)
                throw new ArgumentNullException(nameof(assimpWpfImporter));

            _assimpWpfImporter = assimpWpfImporter;

            _assimpScene = assimpWpfImporter.ImportedAssimpScene;

            if (_assimpScene == null)
                throw new Exception("AssimpScene not yet read with the specified AssimpWpfImporter");


            Skeletons = new List<Skeleton>();


            if (!_assimpScene.HasAnimations)
                return;

            foreach (var assimpMesh in _assimpScene.Meshes)
            {
                if (!assimpMesh.HasBones)
                    continue;

                // If we are here then this mesh has Bones
                var geometryModel3D = assimpWpfImporter.GetGeometryModel3DForAssimpMesh(assimpMesh);

                if (geometryModel3D == null)
                    continue;

                var wpfMeshGeometry3D = (MeshGeometry3D)geometryModel3D.Geometry;
                var skeleton = new Skeleton(assimpMesh, wpfMeshGeometry3D, _assimpScene);

                geometryModel3D.Transform = null; // We need to remove the transformation of the whole GeometryModel3D because we will transform individual positions instead

                Skeletons.Add(skeleton);
            }

            // By default select the first animation
            SelectAnimation(_assimpScene.Animations[0]);
        }

        /// <summary>
        /// Selects the animation by its name. 
        /// If the animation with this name is not defined, an exception is thrown.
        /// To get a list of animation names, check the Animations property on AssimpScene.
        /// </summary>
        /// <param name="animationName"></param>
        public void SelectAnimation(string animationName)
        {
            if (animationName == null)
                throw new ArgumentNullException(nameof(animationName));

            var assimpAnimation = _assimpScene.Animations.FirstOrDefault(a => a.Name == animationName);

            if (assimpAnimation == null)
                throw new Exception("Cannot find animation with name " + animationName);

            SelectAnimation(assimpAnimation);
        }

        /// <summary>
        /// Selects the animation.
        /// </summary>
        /// <param name="assimpAnimation">Assimp.Animation</param>
        public void SelectAnimation(global::Assimp.Animation assimpAnimation)
        {
            if (assimpAnimation == null)
            {
                this.AnimationNodes.Clear();
                SelectedAnimation = null;

                return;
            }

            if (!_assimpScene.Animations.Contains(assimpAnimation))
                throw new Exception("The animation specified to the StartAnimation is not defined by the Assimp scene used to create this AssimpAnimationController");

            if (SelectedAnimation != null && SelectedAnimation != assimpAnimation)
                StopAnimation();


            var namedObjects = _assimpWpfImporter.NamedObjects;

            this.AnimationNodes.Clear();
            foreach (var nodeAnimationChannel in assimpAnimation.NodeAnimationChannels)
            {
                var nodeName = nodeAnimationChannel.NodeName;

                bool isSkeletonFound = false;

                if (Skeletons.Count > 0)
                {
                    foreach (var skeleton in Skeletons)
                    {
                        //if (this.AnimationNodes.OfType<AssimpNodeAnimationNode>().Any(n => n.NodeName == nodeName)) 
                        //    continue; // This node was already added to AnimationNodes - do not add it twice (for example if some parent nodes are used for two or more skeletons)

                        var skeletonNode = skeleton.GetSkeletonNode(nodeName);
                        if (skeletonNode != null)
                        {
                            var assimpNodeAnimationNode = new AssimpNodeAnimationNode(nodeAnimationChannel, skeletonNode);
                            this.AnimationNodes.Add(assimpNodeAnimationNode);

                            isSkeletonFound = true;

                            // NOTE: We do not break the loop here because multiple Skeletons can share the same nodes
                            // In this case we need to create multiple AssimpNodeAnimationNode because SkeletonNodes in different Skeletons are not shared (they have different hierarchies)
                        }
                    }
                }

                if (!isSkeletonFound)
                {
                    object animatedObject;
                    if (namedObjects.TryGetValue(nodeName, out animatedObject))
                    {
                        var animatedModel3D = animatedObject as Model3D;

                        if (animatedModel3D != null)
                        {
                            var assimpNodeAnimationNode = new AssimpNodeAnimationNode(nodeAnimationChannel, animatedModel3D);
                            this.AnimationNodes.Add(assimpNodeAnimationNode);
                        }
                    }
                }
            }

            FramesPerSecond = (int)assimpAnimation.TicksPerSecond == 0 ? 25 : (int)assimpAnimation.TicksPerSecond; // Use TicksPerSecond if defined else default to 25

            SelectedAnimation = assimpAnimation;

            // Set up positions to be at the first frame
            GoToFrame(FirstFrameNumber);
        }

        /// <inheritdoc />
        protected override void OnAfterFrameUpdated()
        {
            foreach (var assimpNodeAnimationNode in this.AnimationNodes.OfType<AssimpNodeAnimationNode>())
                assimpNodeAnimationNode.UpdateAnimatedObject();

            var skeletonsCount = Skeletons.Count;
            for (int i = 0; i < skeletonsCount; i++)
            {
                //Skeletons[i].UpdateNodeFinalMatrices();
                Skeletons[i].RootSkeletonNode.UpdateFinalMatrix(this.CurrentFrameNumber, updateChildren: true);
                Skeletons[i].UpdateVertexPositions();
            }

            base.OnAfterFrameUpdated();
        }
    }
}