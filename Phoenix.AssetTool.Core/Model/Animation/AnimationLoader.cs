#define DEBUG_ASSIMP
#define USE_COMPUTED_OFFSET
using Silk.NET.Assimp;
using Phoenix.Rendering.Geometry;
using System.Numerics;
namespace Phoenix.AssetTool.Core.Model.Animation
{
    public class AnimationLoader
    {
        public static (List<Animation> animations, AnimationLoadData loadData) 
            ProcessAnimations(List<string> files, Dictionary<string, BoneInfo> boneInfoMap)
        {
            var loadData = new AnimationLoadData { BoneInfoMap = boneInfoMap };
            return (files.Select(f => LoadAnimation(f, loadData)).ToList(), loadData);
        }
        
        private static unsafe Animation LoadAnimation(string path, AnimationLoadData loadData)
        {
            var name = Path.GetFileNameWithoutExtension(path);

            path = Path.Combine(Manifest.BaseDirectory , path).Replace("\\", "/");
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
            if (!loadData.ModelHierarchySet)
            {
#if DEBUG_ASSIMP
                Log.Debug("===== DEBUG_ASSIMP: AnimationLoader.LoadAnimation =====");
                Log.Debug($"Animation file: {path}");

                ref readonly var rawRootTx = ref rootNode->MTransformation;
                Log.Debug($"Root MTransformation (raw Matrix4x4 fields):");
                Log.Debug($"  M11={rawRootTx.M11:F6} M12={rawRootTx.M12:F6} M13={rawRootTx.M13:F6} M14={rawRootTx.M14:F6}");
                Log.Debug($"  M21={rawRootTx.M21:F6} M22={rawRootTx.M22:F6} M23={rawRootTx.M23:F6} M24={rawRootTx.M24:F6}");
                Log.Debug($"  M31={rawRootTx.M31:F6} M32={rawRootTx.M32:F6} M33={rawRootTx.M33:F6} M34={rawRootTx.M34:F6}");
                Log.Debug($"  M41={rawRootTx.M41:F6} M42={rawRootTx.M42:F6} M43={rawRootTx.M43:F6} M44={rawRootTx.M44:F6}");

                var rootTxBeforeTranspose = (Matrix4x4)rawRootTx;
                Log.Debug($"Root MTransformation (System.Numerics, BEFORE Transpose):\n{rootTxBeforeTranspose.ToStrF2()}");
#endif

                var globalTransform = Matrix4x4.Transpose(rootNode->MTransformation);
                loadData.InverseGlobalTransform = Matrix4x4.Invert(globalTransform, out var inverse) ? inverse : Matrix4x4.Identity;

#if DEBUG_ASSIMP
                Log.Debug($"Root MTransformation (System.Numerics, AFTER Transpose):\n{globalTransform.ToStrF2()}");
                Log.Debug($"InverseGlobalTransform:\n{loadData.InverseGlobalTransform.ToStrF2()}");
#endif

#if DEBUG_ASSIMP
                Log.Debug("----- RAW HIERARCHY -----");
                PrintHierarchy(rootNode, 0);
#endif

                var rootFolded = ReadHierarchy(rootNode, loadData);

#if DEBUG_ASSIMP
                Log.Debug("----- FOLDED HIERARCHY -----");
                PrintFoldedHierarchy(rootFolded, 0);
#endif

                FlattenHierarchy(rootFolded, -1, loadData);

#if DEBUG_ASSIMP
                Log.Debug("----- FLATTENED HIERARCHY -----");
                PrintFlattened(loadData.AnimatorNodes);
#endif

#if USE_COMPUTED_OFFSET
#if DEBUG_ASSIMP
                Log.Debug("----- COMPUTED OFFSETS (overwriting Assimp offsets) -----");
#endif
                ComputeOffsets(loadData);
#endif

                loadData.ModelHierarchySet = true;
                
            }

            return new Animation(name, scene, loadData.BoneInfoMap);
        }

        private unsafe static void FlattenHierarchy(ModelBoneHierarchyNode node, int parentID, AnimationLoadData loadData, int level = -1)
        {
            int currentIndex = loadData.AnimatorNodes.Count; // index that will be assigned to this node in the flat array

            if (node.IsBone)
            {
                if (loadData.BoneInfoMap.TryGetValue(node.Name, out var info))
                {
                    var an = new AnimatorNode(node.Transform, parentID, info.ID, info.Offset);
                    an.Name = TrimBoneName(node.Name);
                    an.Level = level;
                    loadData.AnimatorNodes.Add(an);
#if DEBUG_ASSIMP
                    Log.Debug($"Flatten: [{currentIndex}] BONE \"{an.Name}\" parent={parentID} modelBoneID={info.ID} level={level}");
                    Log.Debug($"  BindTransform:\n{an.BindTransform.ToStrF2()}");
                    Log.Debug($"  Offset:\n{an.Offset.ToStrF2()}");
#endif
                }
            }
            else
            {
                var an = new AnimatorNode(node.Transform, parentID);
                an.Name = TrimBoneName(node.Name);
                an.Level = level;
                loadData.AnimatorNodes.Add(an);
#if DEBUG_ASSIMP
                Log.Debug($"Flatten: [{currentIndex}] NODE \"{an.Name}\" parent={parentID} modelBoneID=-1 level={level}");
                Log.Debug($"  BindTransform:\n{an.BindTransform.ToStrF2()}");
#endif
            }

            foreach (var child in node.Children)
            {
                FlattenHierarchy(child, currentIndex, loadData, level + 1);
            }
        }

        private static void ComputeOffsets(AnimationLoadData loadData)
        {
            var nodes = loadData.AnimatorNodes;
            var boneInfoMap = loadData.BoneInfoMap;

            var meshWorldById = new Dictionary<int, Matrix4x4>();
            foreach (var kvp in boneInfoMap)
                meshWorldById[kvp.Value.ID] = kvp.Value.MeshWorld;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!node.IsBone)
                    continue;

                if (!meshWorldById.TryGetValue(node.ModelBoneID, out var meshWorld))
                    meshWorld = Matrix4x4.Identity;

                if (Matrix4x4.Invert(meshWorld, out var invMeshWorld))
                {
                    node.Offset = node.Offset * invMeshWorld;
#if DEBUG_ASSIMP
                    Log.Debug($"ComputeOffset BONE \"{node.Name}\" id={node.ModelBoneID}");
                    Log.Debug($"  meshWorld:\n{meshWorld.ToStrF2()}");
                    Log.Debug($"  invMeshWorld:\n{invMeshWorld.ToStrF2()}");
                    Log.Debug($"  offset (corrected):\n{node.Offset.ToStrF2()}");
#endif
                }
            }
        }

        private unsafe static ModelBoneHierarchyNode ReadHierarchy(Node* node, AnimationLoadData loadData)
        {
            string name = node->MName;

#if DEBUG_ASSIMP
            ref readonly var rawTx = ref node->MTransformation;
            var sysTx = (Matrix4x4)rawTx;
            string indent = new string(' ', 4);
            Log.Debug($"ReadHierarchy: node=\"{name}\" children={node->MNumChildren}");
            Log.Debug($"  MTransformation (raw fields): M11={rawTx.M11:F6} M12={rawTx.M12:F6} M13={rawTx.M13:F6} M14={rawTx.M14:F6}");
            Log.Debug($"    M21={rawTx.M21:F6} M22={rawTx.M22:F6} M23={rawTx.M23:F6} M24={rawTx.M24:F6}");
            Log.Debug($"    M31={rawTx.M31:F6} M32={rawTx.M32:F6} M33={rawTx.M33:F6} M34={rawTx.M34:F6}");
            Log.Debug($"    M41={rawTx.M41:F6} M42={rawTx.M42:F6} M43={rawTx.M43:F6} M44={rawTx.M44:F6}");
            Log.Debug($"  MTransformation (compact):\n{indent}{sysTx.ToStrF2()}");
#endif

            var boneInfoMap = loadData.BoneInfoMap;
            if (TryCollectChainThatEndsInBone(node, boneInfoMap, out var foldedTransform, out Node* boneNode, out string boneName))
            {
#if DEBUG_ASSIMP
                Log.Debug($"  ** CHAIN FOLDED ** → bone=\"{boneName}\", foldedTransform:\n{indent}{foldedTransform.ToStrF2()}");
                if (boneInfoMap.TryGetValue(boneName, out var foldedInfo))
                {
                    var offsetSys = (Matrix4x4)foldedInfo.Offset;
                    Log.Debug($"  Offset (raw fields):");
                    Log.Debug($"    M11={offsetSys.M11:F6} M12={offsetSys.M12:F6} M13={offsetSys.M13:F6} M14={offsetSys.M14:F6}");
                    Log.Debug($"    M21={offsetSys.M21:F6} M22={offsetSys.M22:F6} M23={offsetSys.M23:F6} M24={offsetSys.M24:F6}");
                    Log.Debug($"    M31={offsetSys.M31:F6} M32={offsetSys.M32:F6} M33={offsetSys.M33:F6} M34={offsetSys.M34:F6}");
                    Log.Debug($"    M41={offsetSys.M41:F6} M42={offsetSys.M42:F6} M43={offsetSys.M43:F6} M44={offsetSys.M44:F6}");
                    Log.Debug($"  Offset (compact):\n{indent}{offsetSys.ToStrF2()}");
                }
#endif

                if (boneInfoMap.TryGetValue(boneName, out var info))
                {
                    var children = new List<ModelBoneHierarchyNode>();
                    for (int i = 0; i < boneNode->MNumChildren; i++)
                    {
                        var child = ReadHierarchy(boneNode->MChildren[i], loadData);
                        if (child != null)
                            children.Add(child);
                    }
                    return new ModelBoneHierarchyNode(boneName, foldedTransform, children, info.Offset);
                }
            }

            var nodeTransform = node->MTransformation;
            var nodeChildren = new List<ModelBoneHierarchyNode>();

            for (var i = 0; i < node->MNumChildren; i++)
            {
                var child = ReadHierarchy(node->MChildren[i], loadData);
                if (child != null)
                    nodeChildren.Add(child);
            }

            if (boneInfoMap.TryGetValue(name, out var directInfo))
            {
#if DEBUG_ASSIMP
                var offsetSysDir = (Matrix4x4)directInfo.Offset;
                Log.Debug($"  → direct bone node \"{name}\", Offset (System.Numerics):\n{new string(' ', 4)}{offsetSysDir.ToStrF2()}");
#endif
                return new ModelBoneHierarchyNode(name, nodeTransform, nodeChildren, directInfo.Offset);
            }
            else
            {
                if (nodeChildren.Count == 0) // skip non-bone with no children
                {
#if DEBUG_ASSIMP
                    Log.Debug($"  → skipped: non-bone leaf node \"{name}\"");
#endif
                    return null!;
                }

#if DEBUG_ASSIMP
                Log.Debug($"  → intermediate node (non-bone, has children) \"{name}\"");
#endif
                return new ModelBoneHierarchyNode(name, nodeTransform, nodeChildren);
            }
        }

        private unsafe static bool TryCollectChainThatEndsInBone(Node* start, Dictionary<string, BoneInfo> boneInfoMap, out Matrix4x4 accumulated, out Node* boneNodeOut, out string boneNameOut)
        {
            accumulated = Matrix4x4.Identity;
            Node* cur = start;
            boneNodeOut = null;
            boneNameOut = null!;

#if DEBUG_ASSIMP
            Log.Debug($"  TryCollectChain START from=\"{start->MName}\"");
            int chainStep = 0;
#endif

            // Walk while there's exactly one child (linear chain) and stop if we find a bone node
            while (cur != null)
            {
                var t = cur->MTransformation;

#if DEBUG_ASSIMP
                ref readonly var rawT = ref cur->MTransformation;
                var sysT = (Matrix4x4)rawT;
                var accBefore = accumulated;
                Log.Debug($"    ChainStep {chainStep}: node=\"{cur->MName}\", children={cur->MNumChildren}");
                Log.Debug($"      MTransformation (raw): M11={rawT.M11:F6} M12={rawT.M12:F6} M13={rawT.M13:F6} M14={rawT.M14:F6}");
                Log.Debug($"                           M21={rawT.M21:F6} M22={rawT.M22:F6} M23={rawT.M23:F6} M24={rawT.M24:F6}");
                Log.Debug($"                           M31={rawT.M31:F6} M32={rawT.M32:F6} M33={rawT.M33:F6} M34={rawT.M34:F6}");
                Log.Debug($"                           M41={rawT.M41:F6} M42={rawT.M42:F6} M43={rawT.M43:F6} M44={rawT.M44:F6}");
                Log.Debug($"      MTransformation (compact):\n{new string(' ', 8)}{sysT.ToStrF2()}");
                Log.Debug($"      Accumulated BEFORE step:\n{new string(' ', 8)}{accBefore.ToStrF2()}");
#endif

                accumulated = accumulated * t;

#if DEBUG_ASSIMP
                Log.Debug($"      Accumulated AFTER step:\n{new string(' ', 8)}{accumulated.ToStrF2()}");
#endif

                string curName = cur->MName;

                // If this node maps directly to a bone, we finished the chain
                if (boneInfoMap.ContainsKey(curName))
                {
                    boneNodeOut = cur;
                    boneNameOut = curName;
#if DEBUG_ASSIMP
                    Log.Debug($"    → CHAIN HIT bone=\"{curName}\" at step {chainStep}");
                    if (boneInfoMap.TryGetValue(curName, out var hitInfo))
                    {
                        var hitOff = (Matrix4x4)hitInfo.Offset;
                        Log.Debug($"      BoneInfo Offset (sys):\n{new string(' ', 8)}{hitOff.ToStrF2()}");
                    }
#endif
                    return true;
                }

                // if there's not exactly one child, we can't continue folding safely
                if (cur->MNumChildren != 1)
                {
                    boneNodeOut = cur; // caller may still recurse from here
#if DEBUG_ASSIMP
                    Log.Debug($"    → CHAIN BROKEN at step {chainStep}: node has {cur->MNumChildren} children (need exactly 1)");
#endif
                    return false;
                }

                // Step into the single child
                cur = cur->MChildren[0];
#if DEBUG_ASSIMP
                chainStep++;
#endif
            }
            return false;
        }


        private static unsafe void PrintHierarchy(Node* node, int level)
        {
            string? n = node->MName;
            var name = string.IsNullOrEmpty(n) ? "(null)" : n;
            var indent = new string('-', level);
            Log.Debug($"{indent}{name} (children={node->MNumChildren})");
            for (var i = 0; i < node->MNumChildren; i++)
            {
                PrintHierarchy(node->MChildren[i], level + 1);
            }
        }

        private static int TabCount(List<AnimatorNode> nodes, AnimatorNode node)
        {
            if (node.ParentID == -1 || node.ParentID == 0)
                return 0;

            return TabCount(nodes, nodes[node.ParentID]) + 1;

        }
        private static void PrintFlattened(List<AnimatorNode> animatorNodes)
        {
            Log.Debug($"[AnimatorNodes] count={animatorNodes.Count}");
            for (var i = 0; i < animatorNodes.Count; i++)
            {
                var node = animatorNodes[i];
                var spc = new string('-', TabCount(animatorNodes, node));
                var type = node.IsBone ? "B" : "N";
                Log.Debug($"{type}{i}{spc} PID={node.ParentID} MID={node.ModelBoneID} \"{node.Name}\"");
                if (node.IsBone)
                {
                    Log.Debug($"  BindTx:\n{node.BindTransform.ToStrF2()}");
                    Log.Debug($"  Offset:\n{node.Offset.ToStrF2()}");
                }
                else
                {
                    Log.Debug($"  BindTx:\n{node.BindTransform.ToStrF2()}");
                }
            }
            Log.Debug("---");
        }

        private static void PrintFoldedHierarchy(ModelBoneHierarchyNode node, int level)
        {
            var indent = new string('-', level);
            var type = node.IsBone ? "B" : "N";
            var name = TrimBoneName(node.Name);
            Log.Debug($"{type}{indent} \"{name}\" children={node.Children.Count}");
            if (node.IsBone)
            {
                Log.Debug($"  Offset:\n{node.Offset.ToStrF2()}");
            }
            Log.Debug($"  Transform:\n{node.Transform.ToStrF2()}");
            foreach (var child in node.Children)
            {
                PrintFoldedHierarchy(child, level + 1);
            }
        }
        public static string TrimBoneName(string name)
        {
            return name.StartsWith("mixamorig:") ? name.Substring(10) : name;
        }
    }
}
