#if false
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Phoenix.AssetTool.Core.Model.Animation;

namespace Phoenix.AssetTool.Core.Model
{
    public static unsafe class ModelDiagnostics
    {
        private static StringBuilder? _sb;
        private static string? _dumpFilePath;

        public static void Initialize()
        {
            _dumpFilePath = Environment.GetEnvironmentVariable("ASSIMP_DUMP_FILE");
            if (!string.IsNullOrEmpty(_dumpFilePath))
            {
                _sb = new StringBuilder();
                _sb.AppendLine($"=== ASSIMP DUMP START ===");
                _sb.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
            }
        }

        public static void Flush()
        {
            if (_sb != null && !string.IsNullOrEmpty(_dumpFilePath))
            {
                _sb.AppendLine($"=== ASSIMP DUMP END ===");
                System.IO.File.WriteAllText(_dumpFilePath, _sb.ToString());
                Console.WriteLine($"[Diagnostics] Wrote diagnostic dump to {_dumpFilePath}");
            }
        }

        public static void LogLine(string line)
        {
            _sb?.AppendLine(line);
        }

        public static void DumpModelScene(Scene* scene)
        {
            if (_sb == null || scene == null) return;
            _sb.AppendLine("\n--- MODEL SCENE ---");
            _sb.AppendLine($"Flags: {scene->MFlags}, Meshes: {scene->MNumMeshes}, Animations: {scene->MNumAnimations}");
            _sb.AppendLine("Node Hierarchy:");
            DumpNode(scene->MRootNode, 0);

            _sb.AppendLine("\nMeshes:");
            for (uint i = 0; i < scene->MNumMeshes; i++)
            {
                var mesh = scene->MMeshes[i];
                _sb.AppendLine($"  Mesh {i}: Name='{mesh->MName}', Vertices={mesh->MNumVertices}, Bones={mesh->MNumBones}");
                for (uint b = 0; b < mesh->MNumBones; b++)
                {
                    var bone = mesh->MBones[b];
                    _sb.AppendLine($"    Bone {b}: Name='{bone->MName}', Weights={bone->MNumWeights}");
                    _sb.AppendLine($"      Offset: {FormatMatrix(bone->MOffsetMatrix)}");
                }
            }
        }

        public static void DumpBoneInfoMap(Dictionary<string, BoneInfo> map)
        {
            if (_sb == null) return;
            _sb.AppendLine("\n--- BONE INFO MAP ---");
            _sb.AppendLine($"Count: {map.Count}");
            foreach (var kvp in map)
            {
                _sb.AppendLine($"  Bone '{kvp.Key}': ID={kvp.Value.ID}");
                _sb.AppendLine($"    Offset: {FormatMatrix(kvp.Value.Offset)}");
            }
        }

        public static void DumpAnimationScene(string animFile, Scene* scene)
        {
            if (_sb == null || scene == null) return;
            _sb.AppendLine($"\n--- ANIMATION SCENE: {animFile} ---");
            _sb.AppendLine($"Flags: {scene->MFlags}, Animations: {scene->MNumAnimations}");
            _sb.AppendLine("Node Hierarchy:");
            DumpNode(scene->MRootNode, 0);

            for (uint a = 0; a < scene->MNumAnimations; a++)
            {
                var anim = scene->MAnimations[a];
                _sb.AppendLine($"\nAnimation {a}: Name='{anim->MName}', Duration={anim->MDuration}, TPS={anim->MTicksPerSecond}, Channels={anim->MNumChannels}");
                for (uint c = 0; c < anim->MNumChannels; c++)
                {
                    var ch = anim->MChannels[c];
                    _sb.AppendLine($"  Channel {c}: NodeName='{ch->MNodeName}' (PosKeys={ch->MNumPositionKeys}, RotKeys={ch->MNumRotationKeys}, SclKeys={ch->MNumScalingKeys})");
                    if (ch->MNumPositionKeys > 0)
                    {
                        var pk0 = ch->MPositionKeys[0];
                        var pkLast = ch->MPositionKeys[ch->MNumPositionKeys - 1];
                        _sb.AppendLine($"    Pos[0] t={pk0.MTime:F2}: ({pk0.MValue.X:F4}, {pk0.MValue.Y:F4}, {pk0.MValue.Z:F4})");
                        _sb.AppendLine($"    Pos[last] t={pkLast.MTime:F2}: ({pkLast.MValue.X:F4}, {pkLast.MValue.Y:F4}, {pkLast.MValue.Z:F4})");
                    }
                    if (ch->MNumRotationKeys > 0)
                    {
                        var rk0 = ch->MRotationKeys[0];
                        var rkLast = ch->MRotationKeys[ch->MNumRotationKeys - 1];
                        _sb.AppendLine($"    Rot[0] t={rk0.MTime:F2}: ({rk0.MValue.X:F4}, {rk0.MValue.Y:F4}, {rk0.MValue.Z:F4}, {rk0.MValue.W:F4})");
                        _sb.AppendLine($"    Rot[last] t={rkLast.MTime:F2}: ({rkLast.MValue.X:F4}, {rkLast.MValue.Y:F4}, {rkLast.MValue.Z:F4}, {rkLast.MValue.W:F4})");
                    }
                }
            }
        }

        public static void DumpAnimatorNodes(List<AnimatorNode> nodes, Matrix4x4 invGlobalTransform)
        {
            if (_sb == null) return;
            _sb.AppendLine("\n--- ANIMATOR NODES (FLATTENED) ---");
            _sb.AppendLine($"Count: {nodes.Count}");
            _sb.AppendLine($"InverseGlobalTransform: {FormatMatrix(invGlobalTransform)}");
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                _sb.AppendLine($"  Node {i}: Name='{n.Name}', IsBone={n.IsBone}, ParentID={n.ParentID}, ModelBoneID={n.ModelBoneID}, Level={n.Level}");
                _sb.AppendLine($"    BindTransform: {FormatMatrix(n.BindTransform)}");
                _sb.AppendLine($"    Transform:     {FormatMatrix(n.Transform)}");
                if (n.IsBone)
                    _sb.AppendLine($"    Offset:        {FormatMatrix(n.Offset)}");
            }
        }

        public static void DumpPrecomputedAnimation(Animation.Animation anim)
        {
            if (_sb == null) return;
            _sb.AppendLine($"\n--- PRECOMPUTED ANIMATION: {anim.Name} ---");
            _sb.AppendLine($"Duration: {anim.Duration}, TPS: {anim.TicksPerSecond}, BoneCount: {anim.BoneCount}");
            for (int b = 0; b < anim.BoneCount; b++)
            {
                var kfs = anim.Keyframes[b];
                _sb.AppendLine($"  Bone {b}: KeyframeCount={kfs.Length}");
                if (kfs.Length > 0)
                {
                    var k0 = kfs[0];
                    var kLast = kfs[kfs.Length - 1];
                    _sb.AppendLine($"    KF[0] t={k0.TimeStamp:F2}: S=({k0.SRT.Scale.X:F3},{k0.SRT.Scale.Y:F3},{k0.SRT.Scale.Z:F3}) R=({k0.SRT.Rotation.X:F3},{k0.SRT.Rotation.Y:F3},{k0.SRT.Rotation.Z:F3},{k0.SRT.Rotation.W:F3}) T=({k0.SRT.Translation.X:F3},{k0.SRT.Translation.Y:F3},{k0.SRT.Translation.Z:F3})");
                    _sb.AppendLine($"    KF[last] t={kLast.TimeStamp:F2}: S=({kLast.SRT.Scale.X:F3},{kLast.SRT.Scale.Y:F3},{kLast.SRT.Scale.Z:F3}) R=({kLast.SRT.Rotation.X:F3},{kLast.SRT.Rotation.Y:F3},{kLast.SRT.Rotation.Z:F3},{kLast.SRT.Rotation.W:F3}) T=({kLast.SRT.Translation.X:F3},{kLast.SRT.Translation.Y:F3},{kLast.SRT.Translation.Z:F3})");
                }
            }
        }

        private static void DumpNode(Node* node, int indent)
        {
            if (node == null || _sb == null) return;
            var prefix = new string(' ', indent * 2);
            _sb.AppendLine($"{prefix}- Node: '{node->MName}', Children={node->MNumChildren}, Meshes={node->MNumMeshes}");
            _sb.AppendLine($"{prefix}  Transform: {FormatMatrix(node->MTransformation)}");
            for (uint i = 0; i < node->MNumChildren; i++)
            {
                DumpNode(node->MChildren[i], indent + 1);
            }
        }

        public static string FormatMatrix(Matrix4x4 m)
        {
            return $"[{m.M11:F4} {m.M12:F4} {m.M13:F4} {m.M14:F4} | {m.M21:F4} {m.M22:F4} {m.M23:F4} {m.M24:F4} | {m.M31:F4} {m.M32:F4} {m.M33:F4} {m.M34:F4} | {m.M41:F4} {m.M42:F4} {m.M43:F4} {m.M44:F4}]";
        }
    }
}
#endif
