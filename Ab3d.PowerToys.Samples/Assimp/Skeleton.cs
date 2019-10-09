using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using Ab3d.Assimp;
using Assimp;

namespace Ab3d.Assimp
{
    /// <summary>
    /// Skeleton class represents a skeleton that is used for skeletal animation and contains a list of SkeletonNodes.
    /// </summary>
    public class Skeleton
    {
        private Point3DCollection _wpfOriginalPositions;

        private string[] _allBoneNames;

        /// <summary>
        /// Gets a list of SkeletonNode objects that define the skeleton.
        /// </summary>
        public List<SkeletonNode> SkeletonNodes { get; private set; }

        /// <summary>
        /// Gets a WPF's MeshGeometry3D object that is transformed with this skeleton. 
        /// </summary>
        public MeshGeometry3D MeshGeometry3D { get; private set; }

        /// <summary>
        /// Gets a Assimp's Mesh object that was used to generate the WPF's MeshGeometry3D.
        /// </summary>
        public Mesh AssimpMesh { get; private set; }

        /// <summary>
        /// Gets a root SkeletonNode
        /// </summary>
        public SkeletonNode RootSkeletonNode { get; private set; }
        
        ///// <summary>
        ///// Gets a SkeletonNode that contains the Mesh
        ///// </summary>
        //public SkeletonNode RootMeshSkeletonNode { get; private set; }

        ///// <summary>
        ///// Gets a Matrix3D of the root SkeletonNode
        ///// </summary>
        //public Matrix3D RootSkeletonNodeMatrix;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="assimpMesh">Assimp's Mesh</param>
        /// <param name="meshGeometry3D">WPF's MeshGeometry3D</param>
        /// <param name="assimpScene">Assimp's Scene</param>
        public Skeleton(Mesh assimpMesh, MeshGeometry3D meshGeometry3D, Scene assimpScene)
        {
            if (assimpMesh == null)
                throw new ArgumentNullException(nameof(assimpMesh));

            if (meshGeometry3D == null)
                throw new ArgumentNullException(nameof(meshGeometry3D));
            

            MeshGeometry3D = meshGeometry3D;
            AssimpMesh = assimpMesh;

            _wpfOriginalPositions = new Point3DCollection(MeshGeometry3D.Positions);

            if (!assimpMesh.HasBones)
                return;

            _allBoneNames = assimpMesh.Bones.Select(b => b.Name).ToArray();
            int boneNodesLeftToGenerate = _allBoneNames.Length;

            SkeletonNodes = new List<SkeletonNode>();


            // Find first node with the same name as any bone defined by skeleton
            var rootAssimpNode = FindRootSkeletonNode(assimpScene.RootNode);

            if (rootAssimpNode == null)
                return; // UH: We cannot find any bone in the Nodes hierarchy


            RootSkeletonNode = GenerateSkeleton(assimpScene.RootNode, null, ref boneNodesLeftToGenerate);

            // Clean the skeleton
            // When generating skeleton we create hierarchy for all the nodes from the rootSkeletonNode
            // Now remove the SkeletonNodes that do not have bone associate or that have ChildBonesCount equal to 0.
            RemoveNodesWithoutBones(RootSkeletonNode);


            RootSkeletonNode.UpdateFinalMatrix(currentFrameNumber: -1, updateChildren: true);
        }

        //public void UpdateNodeFinalMatrices()
        //{
        //    RootSkeletonNode.UpdateFinalMatrix(ref RootSkeletonNodeMatrix, updateChildren: true);
        //}

        /// <summary>
        /// Returns true is this skeleton defines a bone with specified name.
        /// </summary>
        /// <param name="boneName">name of the bone</param>
        /// <returns>true is this skeleton defines a bone with specified name</returns>
        public bool HasBone(string boneName)
        {
            if (_allBoneNames == null)
                return false;

            return _allBoneNames.Contains(boneName);
        }

        /// <summary>
        /// Returns SkeletonNode with specified bone name.
        /// </summary>
        /// <param name="boneName">bone name</param>
        /// <returns>SkeletonNode with specified bone name or null if bone does not exist</returns>
        public SkeletonNode GetSkeletonNode(string boneName)
        {
            if (_allBoneNames == null)
                return null;

            var skeletonNodesCount = SkeletonNodes.Count;
            for (int i = 0; i < skeletonNodesCount; i++)
            {
                if (SkeletonNodes[i].AssimpNode.Name == boneName)
                    return SkeletonNodes[i];
            }

            return null;
        }


        // This method use original assimp bone structure where many VertexWeights (with VertexId and Weight) 
        // structs are assigned to each bone. This data structure is optimized for CPU updates.
        // The UpdateVertexPositions below requires that each vertex has its array of bones and weights - optimized for GPU updates.
        //
        // This method is based on sample code from: https://sourceforge.net/p/assimp/discussion/817654/thread/5462cbf5/

        /// <summary>
        /// UpdateVertexPositions updates the positions in the MeshGeometry3D
        /// </summary>
        public void UpdateVertexPositions()
        {
            if (SkeletonNodes == null)
                return;
            
            var originalPositions = _wpfOriginalPositions;

            var transformedPositions = new Point3D[originalPositions.Count];

            foreach (var skeletonNode in SkeletonNodes)
            {
                var assimpBone = skeletonNode.AssimpBone;
                if (assimpBone == null)
                    continue;

                var finalBoneMatrix = skeletonNode.FinalMatrix; // = skeletonNode.BoneOffsetMatrix * skeletonNode.CurrentWorldMatrix;

                for (int i = 0; i < assimpBone.VertexWeightCount; i++)
                {
                    VertexWeight boneWeight = assimpBone.VertexWeights[i];

                    int vertexId = boneWeight.VertexID;
                    double weightFactor = boneWeight.Weight;

                    var sourcePosition = originalPositions[vertexId];
                    var transformedPosition = finalBoneMatrix.Transform(sourcePosition);

                    transformedPositions[vertexId] += new System.Windows.Media.Media3D.Vector3D(transformedPosition.X * weightFactor,
                                                                                                transformedPosition.Y * weightFactor,
                                                                                                transformedPosition.Z * weightFactor);
                }
            }

            MeshGeometry3D.Positions = new Point3DCollection(transformedPositions);
        }

        // remove the SkeletonNodes that do not have bone associate and that have ChildBonesCount equal to 0.
        private void RemoveNodesWithoutBones(SkeletonNode skeletonNode)
        {
            if (skeletonNode.AssimpBone == null && skeletonNode.ChildBonesCount == 0)
            {
                // Delete this bone
                var parent = skeletonNode.Parent;
                if (parent != null)
                    parent.Children.Remove(skeletonNode);
            }
            else
            {
                for (var i = skeletonNode.Children.Count - 1; i >= 0; i--)
                    RemoveNodesWithoutBones(skeletonNode.Children[i]);
            }
        }

        // Returns the first assimp Node in the hierarchy that is names with the same name as any of the bones for this mesh
        private Node FindRootSkeletonNode(Node node)
        {
            string nodeName = node.Name;

            if (_allBoneNames.Contains(nodeName)) // Did we find a node that is connected to any bone for this mesh
                return node;

            if (!node.HasChildren)
                return null;

            foreach (var nodeChild in node.Children)
            {
                var foundNode = FindRootSkeletonNode(nodeChild);
                if (foundNode != null)
                    return foundNode;
            }

            return null;
        }

        private Node FindMeshNode(Node node, int assimpMeshIndex)
        {
            bool isMeshFound = node.Meshes.Contains(assimpMeshIndex);

            if (isMeshFound)
                return node;

            if (!node.HasChildren)
                return null;

            foreach (var nodeChild in node.Children)
            {
                var foundNode = FindMeshNode(nodeChild, assimpMeshIndex);
                if (foundNode != null)
                    return foundNode;
            }

            return null;
        }

        private SkeletonNode GenerateSkeleton(Node node, SkeletonNode parentSkeletonNode, ref int boneNodesLeftToGenerate)
        {
            if (node == null)
                return null;

            SkeletonNode newlyCreatedSkeletonNode;

            // Bones and Nodes are "connected" with having the same name
            var boneIndex = Array.IndexOf(_allBoneNames, node.Name);
            if (boneIndex >= 0)
            {
                var assimpBone = AssimpMesh.Bones[boneIndex];
                boneNodesLeftToGenerate--; // Reduce the number of SkeletonNode that we still need to create

                newlyCreatedSkeletonNode = new SkeletonNode(node, assimpBone, parentSkeletonNode, boneIndex: SkeletonNodes.Count);
            }
            else
            {
                newlyCreatedSkeletonNode = new SkeletonNode(node, null, parentSkeletonNode, -1);
            }

            SkeletonNodes.Add(newlyCreatedSkeletonNode);

            if (node.HasChildren && boneNodesLeftToGenerate > 0)
            {
                for (var i = 0; i < node.ChildCount; i++)
                {
                    var childBone = GenerateSkeleton(node.Children[i], newlyCreatedSkeletonNode, ref boneNodesLeftToGenerate);

                    if (childBone != null)
                        newlyCreatedSkeletonNode.Children.Add(childBone);
                }
            }

            return newlyCreatedSkeletonNode;
        }
    }
}