using Silk.NET.Assimp;
using Phoenix.Rendering.Geometry;
using System.Numerics;
namespace Phoenix.AssetTool.Core.Model.Animation
{
    public class AnimationLoader
    {
        public static Matrix4x4 InverseGlobalTransform = Matrix4x4.Identity;
        public static AnimatorNode[] AnimatorNodes = default!;
        private static List<AnimatorNode> _animatorNodes = default!;
        private static bool _modelHierarchySet = false;
        private static Dictionary<string, BoneInfo> _boneInfoMap = default!;
        public static List<Animation> ProcessAnimations(List<string> files, Dictionary<string, BoneInfo> boneInfoMap)
        {
            _boneInfoMap = boneInfoMap;
            return files.Select(LoadAnimation).ToList();
        }
        
        private static unsafe Animation LoadAnimation(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var assimp = Assimp.GetApi();
            var scene = assimp.ImportFile(path, (uint)(0));

            var sceneNull = scene == null;
            var sceneNullRootNode = scene == null;
            var rootNode = scene->MRootNode;
            if (scene == null || rootNode == null)
            {
                var error = assimp.GetErrorStringS();
                throw new Exception(error);
            }
            if (!_modelHierarchySet)
            {
                var globalTransform = Matrix4x4.Transpose(rootNode->MTransformation);
                InverseGlobalTransform = Matrix4x4.Invert(globalTransform, out var inverse) ? inverse : Matrix4x4.Identity;

                //Log.Debug("-------------");
                //Log.Debug("HIERACHY");
                //_nCount = 0;
                //PrintHierarchy(rootNode, 0);

                //ReadHierarchy(rootNode, -1, 0);
                var rootFolded = ReadHierarchy(rootNode);
                //Log.Debug("-------------");
                //Log.Debug("FOLDED HIERACHY");
                //_nCount = 0;
                //PrintFoldedHierachy(_rootBoneHierarchyNode, -1);
                //Log.Debug("-------------");
                //Log.Debug("FLATTENED");
                FlattenHierarchy(rootFolded, -1);
                AnimatorNodes = _animatorNodes.ToArray();
                //PrintFlattened();

                _modelHierarchySet = true;
            }

            return new Animation(name, scene, _boneInfoMap);
        }

        private unsafe static void FlattenHierarchy(ModelBoneHierarchyNode node, int parentID, int level = -1)
        {
            int currentIndex = _animatorNodes.Count; // index that will be assigned to this node in the flat array

            if (node.IsBone)
            {
                var boneInfoMap = _boneInfoMap;
                if (boneInfoMap.TryGetValue(node.Name, out var info))
                {
                    var an = new AnimatorNode(node.Transform, parentID, info.ID, info.Offset);
                    an.Name = TrimBoneName(node.Name);
                    an.Level = level;
                    _animatorNodes.Add(an);
                }
            }
            else
            {
                var an = new AnimatorNode(node.Transform, parentID);
                an.Name = TrimBoneName(node.Name);
                an.Level = level;
                _animatorNodes.Add(an);
            }

            foreach (var child in node.Children)
            {
                FlattenHierarchy(child, currentIndex, level + 1);
            }
        }

        private unsafe static ModelBoneHierarchyNode ReadHierarchy(Node* node)
        {
            // Try to detect if this node is the head of a helper-chain that ends in a bone node.
            // (Mixamo pre-scale_bone, pre-rotation_bone, pre-translation_bone, bone, style)
            // If it is, fold the transforms along the single-child chain up to the bone and
            // create a single AnimationNode entry for that bone (with BaseTransform = folded transform).

            var boneInfoMap = _boneInfoMap;
            // Attempt to collect a chain starting at 'node' that leads to a bone node
            if (TryCollectChainThatEndsInBone(node, boneInfoMap, out var foldedTransform, out Node* boneNode, out string boneName))
            {
                // We found a chain that ends in a bone node (boneNode). Add a single AnimationNode
                // using the folded transform and skip the intermediate helper nodes in the
                // flattened AnimationNodes list (but still recurse children of the bone node).

                if (boneInfoMap.TryGetValue(boneName, out var info))
                {
                    var children = new List<ModelBoneHierarchyNode>();
                    for (int i = 0; i < boneNode->MNumChildren; i++)
                    {
                        var child = ReadHierarchy(boneNode->MChildren[i]);
                        if (child != null)
                            children.Add(child);
                    }
                    return new ModelBoneHierarchyNode(boneName, foldedTransform, children, info.Offset);
                }
            }

            // Normal path: this node is not the head of helper chain ending in a bone.
            string name = node->MName;

            var nodeTransform = node->MTransformation;
            var nodeChildren = new List<ModelBoneHierarchyNode>();

            for (var i = 0; i < node->MNumChildren; i++)
            {
                var child = ReadHierarchy(node->MChildren[i]);
                if (child != null)
                    nodeChildren.Add(child);
            }

            if (boneInfoMap.TryGetValue(name, out var directInfo))
            {
                return new ModelBoneHierarchyNode(name, nodeTransform, nodeChildren, directInfo.Offset);
            }
            else
            {
                if (nodeChildren.Count == 0) // skip non-bone with no children
                    return null;

                return new ModelBoneHierarchyNode(name, nodeTransform, nodeChildren);
            }
        }

        // Helper: tries to walk a single-child chain starting at 'start' and sees if it ends in a bone node.
        // It accumulates each node's transform into 'accumulated' (in System.Numerics row-major space using Transpose).
        private unsafe static bool TryCollectChainThatEndsInBone(Node* start, Dictionary<string, BoneInfo> boneInfoMap, out Matrix4x4 accumulated, out Node* boneNodeOut, out string boneNameOut)
        {
            accumulated = Matrix4x4.Identity;
            Node* cur = start;
            boneNodeOut = null;
            boneNameOut = null;

            // Walk while there's exactly one child (linear chain) and stop if we find a bone node
            while (cur != null)
            {
                var t = cur->MTransformation;
                accumulated = accumulated * t;
                string curName = cur->MName;

                // If this node maps directly to a bone, we finished the chain
                if (boneInfoMap.ContainsKey(curName))
                {
                    boneNodeOut = cur;
                    boneNameOut = curName;
                    return true;
                }

                // if there's not exactly one child, we can't continue folding safely
                if (cur->MNumChildren != 1)
                {
                    boneNodeOut = cur; // caller may still recurse from here
                    return false;
                }

                // Step into the single child
                cur = cur->MChildren[0];
            }
            return false;
        }


        int _nCount = 0;
        private unsafe void PrintHierarchy(Node* node, int level)
        {
            var str = $"{_nCount} ";
            for (var i = 0; i < level; i++)
            {
                str += "-";
            }
            //str += ((string)node->MName).TrimBoneName();
            //Log.Debug(str);
            _nCount++;

            for (var i = 0; i < node->MNumChildren; i++)
            {
                PrintHierarchy(node->MChildren[i], level + 1);
            }
        }
        private unsafe void PrintFlattened()
        {
            for (var i = 0; i < _animatorNodes.Count; i++)
            {
                var node = _animatorNodes[i];
                var str = node.IsBone ? "B" : "N";
                var spc = "";
                for (var j = 0; j < node.Level; j++)
                {
                    spc += "-";
                }
                str += $"{i}{spc} PID {node.ParentID}, MID {node.ModelBoneID}, {node.Name}";
                //Log.Debug(str);
            }
        }
        private unsafe void PrintFoldedHierachy(ModelBoneHierarchyNode node, int parentID)
        {

            var str = $"{_nCount}";

            str += node.IsBone ? "B" : " ";

            for (var i = 0; i < parentID; i++)
            {
                str += "-";
            }
            //str += $"PID {parentID} " + (node.Name).TrimBoneName();
            //Log.Debug(str);
            for (var i = 0; i < node.Children.Count; i++)
            {
                _nCount++;
                PrintFoldedHierachy(node.Children[i], parentID + 1);
            }
        }
        public static string TrimBoneName(string name)
        {
            if (name.StartsWith("mixamo"))
                return name.Substring(11);
            return name;
        }
    }
}
