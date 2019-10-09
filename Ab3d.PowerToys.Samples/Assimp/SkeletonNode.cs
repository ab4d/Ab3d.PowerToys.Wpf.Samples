using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Media3D;
using Ab3d.Assimp;
using Ab3d.PowerToys.Samples.Objects3D;
using Ab3d.Utilities;
using Assimp;

namespace Ab3d.Assimp
{
    /// <summary>
    /// SkeletonNode represents one bone in the skeleton representation.
    /// </summary>
    public class SkeletonNode
    {
        public readonly Node AssimpNode;

        public readonly Bone AssimpBone;

        public readonly SkeletonNode Parent;

        public readonly int BoneIndex;

        public Matrix3D BoneOffsetMatrix;

        public Matrix3D OriginalNodeMatrix;
        public Matrix3D CurrentNodeMatrix;

        public Matrix3D CurrentWorldMatrix; // = CurrentNodeMatrix * Parent.CurrentWorldMatrix;
        public Matrix3D FinalMatrix;        // = BoneMatrix * CurrentWorldMatrix;

        public List<SkeletonNode> Children;

        public int ChildBonesCount;

        public MeshGeometry3D MeshGeometry3D;

        public double CurrentFrameNumber { get; private set; }

        public SkeletonNode(Node assimpNode, Bone assimpBone, SkeletonNode parentNode, int boneIndex)
        {
            AssimpNode = assimpNode;
            AssimpBone = assimpBone;
            Parent = parentNode;
            BoneIndex = boneIndex;

            OriginalNodeMatrix = assimpNode.Transform.ToWpfMatrix3D();
            CurrentNodeMatrix  = OriginalNodeMatrix;

            if (assimpBone != null) // Is this node associated with a bone?
            {
                BoneOffsetMatrix = assimpBone.OffsetMatrix.ToWpfMatrix3D();

                // Increase the number of ChildBonesCount for all parent SkeletonNodes
                var oneParent = parentNode;
                while (oneParent != null)
                {
                    oneParent.ChildBonesCount++;
                    oneParent = oneParent.Parent;
                }
            }

            UpdateFinalMatrix(currentFrameNumber: -1, updateChildren: false);

            Children = new List<SkeletonNode>();
        }

        public void UpdateFinalMatrix(double currentFrameNumber, bool updateChildren = true)
        {
            CurrentFrameNumber = currentFrameNumber;


            if (Parent != null)
                CurrentWorldMatrix = this.CurrentNodeMatrix * Parent.CurrentWorldMatrix;
            else
                CurrentWorldMatrix = this.CurrentNodeMatrix;

            if (AssimpBone != null)
                FinalMatrix = BoneOffsetMatrix * CurrentWorldMatrix;
            else
                FinalMatrix = CurrentWorldMatrix;

            if (updateChildren)
            {
                foreach (var childSkeletonBone in Children)
                    childSkeletonBone.UpdateFinalMatrix(currentFrameNumber, updateChildren: true);
            }
        }

#if DEBUG
        // Dumps the hierarchy of SkeletonNodes to Visual Studio Immediate or Output window 
        public void DumpHierarchy(bool dumpMatrices = false)
        {
            var sb = new StringBuilder();
            AppendHierarchyInfo(sb, 0, dumpMatrices);

            System.Diagnostics.Debug.WriteLine(sb.ToString());
        }

        private void AppendHierarchyInfo(StringBuilder sb, int indent, bool dumpMatrices = false)
        {
            string indentString = new string(' ', indent);

            sb.Append(indentString)
                .Append("'")
                .Append(AssimpNode.Name)
                .Append("'")
                .AppendFormat(" (ChildBonesCount: {0})", ChildBonesCount);
            
            if (AssimpBone == null)
                sb.Append(" (no bone)");

            sb.AppendLine();

            if (dumpMatrices)
            {
                indentString += "  ";

                string matrixText = Ab3d.Utilities.Dumper.GetMatrix3DText(this.CurrentNodeMatrix);
                if (indent > 0)
                    matrixText = indentString + matrixText.Replace("\r\n", "\r\n" + indentString);

                sb.Append(indentString)
                    .AppendLine("CurrentNodeMatrix:")
                    .AppendLine(matrixText);

                if (AssimpBone != null)
                {
                    matrixText = Ab3d.Utilities.Dumper.GetMatrix3DText(this.BoneOffsetMatrix);
                    if (indent > 0)
                        matrixText = indentString + matrixText.Replace("\r\n", "\r\n" + indentString);

                    sb.Append(indentString)
                        .AppendLine("BoneMatrix:")
                        .AppendLine(matrixText);
                }
            }

            foreach (var skeletonNode in this.Children)
                skeletonNode.AppendHierarchyInfo(sb, indent + 8, dumpMatrices);
        }
#endif
    }
}