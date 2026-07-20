//#define DEBUG_ASSIMP
using Silk.NET.Assimp;
using Phoenix.Rendering.Geometry;
using System.Numerics;

namespace Phoenix.AssetTool.Core.Model.Animation
{
    public unsafe class Animation
    {
        public string Name { get; private set; }
        public float Duration { get; private set; }
        public float TicksPerSecond { get; private set; }
        
        public Keyframe[][] Keyframes { get; private set; }
        public int BoneCount { get; private set; }

        public unsafe Animation(string name, Scene* scene, Dictionary<string, BoneInfo> boneInfoMap)
        {
            Name = name;
            var assAnimation = scene->MAnimations[0];
            Duration = (float)assAnimation->MDuration;
            TicksPerSecond = (float)assAnimation->MTicksPerSecond;
            if (TicksPerSecond <= 0)
                TicksPerSecond = 25.0f;
            
            Keyframes = ReadKeyFrames(assAnimation, boneInfoMap);

            BoneCount = Keyframes.GetLength(0);
                        
        }

        public void Precompute(IReadOnlyList<AnimatorNode> nodes, Matrix4x4 inverseGlobalTransform)
        {
            var boneCount = Keyframes.Length;

            var allTimestamps = new SortedSet<float>();
            for (int b = 0; b < boneCount; b++)
            {
                foreach (var kf in Keyframes[b])
                    allTimestamps.Add(kf.TimeStamp);
            }

#if DEBUG_ASSIMP
            Log.Debug($"Precompute: boneCount={boneCount} uniqueTimestamps={allTimestamps.Count}");
            int tsIdx = 0;
            foreach (var ts in allTimestamps)
            {
                if (tsIdx < 5 || tsIdx >= allTimestamps.Count - 5)
                    Log.Debug($"  ts[{tsIdx}]={ts:F4}");
                tsIdx++;
            }
            if (allTimestamps.Count > 10)
                Log.Debug($"  ... ({allTimestamps.Count - 10} more) ...");
            Log.Debug($"----");
#endif
            
            var newKeyframes = new List<Keyframe>[boneCount];
            for (int b = 0; b < boneCount; b++)
                newKeyframes[b] = new List<Keyframe>();

#if DEBUG_ASSIMP
            int frameIdx = 0;
            int totalFrames = allTimestamps.Count;
#endif
            foreach (var timestamp in allTimestamps)
            {
                List<Keyframe> kfs = new();
                for (int b = 0; b < boneCount; b++)
                {
                    var localSRT = InterpolateLocalSRT(Keyframes[b], timestamp);
                    kfs.Add(new Keyframe(timestamp, localSRT.Scale, localSRT.Rotation, localSRT.Translation));
                }

#if DEBUG_ASSIMP
                bool firstFrame = (frameIdx == 0);
                bool lastFrame = (frameIdx == totalFrames - 1);
                if (firstFrame || lastFrame)
                {
                    Log.Debug($"----- Frame {frameIdx}/{totalFrames} t={timestamp:F4} -----");
                }
#endif

                ProcessFrame(nodes, inverseGlobalTransform, kfs);

#if DEBUG_ASSIMP
                if (firstFrame || lastFrame)
                {
                    Log.Debug($"  Processed SRTs for frame {frameIdx}:");
                    for (int b = 0; b < Math.Min(boneCount, 5); b++)
                    {
                        Log.Debug($"  Bone[{b}] SRT: S={kfs[b].SRT.Scale.ToStr()} R={kfs[b].SRT.Rotation.ToStr()} T={kfs[b].SRT.Translation.ToStr()}");
                    }
                    if (boneCount > 5) Log.Debug($"  ... +{boneCount - 5} more bones");
                    Log.Debug($"-----");
                }
                frameIdx++;
#endif

                for (int b = 0; b < boneCount; b++)
                {
                    var fb = kfs[b];
                    newKeyframes[b].Add(fb);

                }
            }

            var result = new Keyframe[boneCount][];
            for (int b = 0; b < boneCount; b++)
                result[b] = newKeyframes[b].ToArray();
            Keyframes = result;
        }

        void ProcessFrame(IReadOnlyList<AnimatorNode> nodes, Matrix4x4 inverseGlobalTransform, List<Keyframe> keyFrame)
        {
#if DEBUG_ASSIMP
            Log.Debug($"ProcessFrame: nodeCount={nodes.Count} boneCount={BoneCount}");
            Log.Debug($"  inverseGlobalTransform:\n{inverseGlobalTransform.ToStrF2()}");
#endif
            var nodeCount = nodes.Count;
            for (var i = 0; i < nodeCount; i++)
            {
                var node = nodes[i];

                var pid = node.ParentID;
                var parentTransform = pid != -1 ? nodes[pid].Transform : Matrix4x4.Identity;

                Matrix4x4 localTransform;
                if (node.IsBone)
                {
                    var animTransform = keyFrame[node.ModelBoneID].SRT.AsMatrix();
#if DEBUG_ASSIMP
                    var animBeforeTranspose = animTransform;
                    Log.Debug($"  Node[{i}] BONE \"{node.Name}\" MID={node.ModelBoneID} PID={pid}");
                    Log.Debug($"    keyFrame SRT={{\n      S={keyFrame[node.ModelBoneID].SRT.Scale.ToStr()}\n      R={keyFrame[node.ModelBoneID].SRT.Rotation.ToStr()}\n      T={keyFrame[node.ModelBoneID].SRT.Translation.ToStr()}}}");
                    Log.Debug($"    animTransform BEFORE Transpose:\n{animBeforeTranspose.ToStrF2()}");
#endif
                    animTransform = Matrix4x4.Transpose(animTransform);
#if DEBUG_ASSIMP
                    Log.Debug($"    animTransform AFTER Transpose:\n{animTransform.ToStrF2()}");
#endif
                    localTransform = animTransform;
                }
                else
                {
                    localTransform = node.BindTransform;
#if DEBUG_ASSIMP
                    Log.Debug($"  Node[{i}] NODE \"{node.Name}\" PID={pid}");
                    Log.Debug($"    localTransform = BindTransform:\n{localTransform.ToStrF2()}");
#endif
                }

#if DEBUG_ASSIMP
                Log.Debug($"    parentTransform:\n{parentTransform.ToStrF2()}");
#endif
                node.Transform = parentTransform * localTransform;
#if DEBUG_ASSIMP
                Log.Debug($"    node.Transform (parent * local):\n{node.Transform.ToStrF2()}");
#endif

                if (node.IsBone)
                {
                    var final = inverseGlobalTransform * node.Transform * node.Offset;
#if DEBUG_ASSIMP
                    Log.Debug($"    node.Offset:\n{node.Offset.ToStrF2()}");
                    Log.Debug($"    final BEFORE Transpose (invGlobal * nodeTx * offset):\n{final.ToStrF2()}");
#endif
                    final = Matrix4x4.Transpose(final);
#if DEBUG_ASSIMP
                    Log.Debug($"    final AFTER Transpose:\n{final.ToStrF2()}");
#endif
                    if (Matrix4x4.Decompose(final, out var scale, out var rotation, out var translation))
                        keyFrame[node.ModelBoneID].SRT = new Transform(scale, rotation, translation);
#if DEBUG_ASSIMP
                    Log.Debug($"    Decomposed SRT: S={scale.ToStr()} R={rotation.ToStr()} T={translation.ToStr()}");
#endif
                }
            }
        }

        
        private static Transform InterpolateLocalSRT(Keyframe[] keyframes, float time)
        {
            if (keyframes.Length == 0)
                return new Transform(Vector3.One, Quaternion.Identity, Vector3.Zero);

            if (keyframes.Length == 1)
                return keyframes[0].SRT;

            int i0 = keyframes.Length - 2;
            for (int i = 0; i < keyframes.Length - 1; i++)
            {
                if (time < keyframes[i + 1].TimeStamp)
                {
                    i0 = i;
                    break;
                }
            }

            int i1 = Math.Min(i0 + 1, keyframes.Length - 1);

            var k0 = keyframes[i0];
            var k1 = keyframes[i1];

            var diff = k1.TimeStamp - k0.TimeStamp;
            if (diff < 0.0001f)
                return k0.SRT;

            float factor = (time - k0.TimeStamp) / diff;
            return k0.SRT.Interpolate(k1.SRT, factor);
        }
        bool GetBoneInfo(string nodeName, Dictionary<string, BoneInfo> boneInfoMap, out BoneInfo info)
        {
            var trimmedNodeName = AnimationLoader.TrimBoneName(nodeName);
            foreach(var e in boneInfoMap)
            {
                var trimmedKey = AnimationLoader.TrimBoneName(e.Key);
                if(string.Equals(trimmedNodeName, trimmedKey, StringComparison.OrdinalIgnoreCase))
                {
                    info = e.Value;
                    return true;
                }
            }
            info = default!;
            return false;
            
        }
        unsafe Keyframe[][] ReadKeyFrames(Silk.NET.Assimp.Animation* anim, Dictionary<string, BoneInfo> boneInfoMap)
        {
            var boneCount = boneInfoMap.Count;
            var keyframes = new List<Keyframe>[boneCount];
            for (int i = 0; i < boneCount; i++)
                keyframes[i] = new List<Keyframe>();

#if DEBUG_ASSIMP
            Log.Debug($"===== DEBUG_ASSIMP: Animation.ReadKeyFrames =====");
            Log.Debug($"Animation: \"{Name}\" channels={anim->MNumChannels} boneCount={boneCount}");
            Log.Debug($"BoneInfoMap contents:");
            foreach (var b in boneInfoMap)
            {
                var offSys = (Matrix4x4)b.Value.Offset;
                Log.Debug($"  ID={b.Value.ID} \"{b.Key}\" Offset:\n{offSys.ToStrF2()}");
            }
#endif

            for (int c = 0; c < anim->MNumChannels; c++)
            {
                var channel = anim->MChannels[c];
                var nodeName = channel->MNodeName;
                
                var behaviourPre = channel->MPreState;
                var behaviourPost = channel->MPostState;

                //if (!boneInfoMap.TryGetValue(nodeName, out var info))
                //    continue;

                if (!GetBoneInfo(nodeName, boneInfoMap, out var info))
                {
#if DEBUG_ASSIMP
                    Log.Debug($"Channel[{c}]: \"{nodeName}\" → SKIPPED (not in BoneInfoMap)");
#endif
                    continue;
                }

                // bug: all three vars use MNumPositionKeys instead of their respective fields
                var posKeyCount = channel->MNumPositionKeys;
                var rotKeyCount = channel->MNumPositionKeys;
                var sclKeyCount = channel->MNumPositionKeys;

#if DEBUG_ASSIMP
                Log.Debug($"Channel[{c}]: \"{nodeName}\" → boneID={info.ID}");
                Log.Debug($"  PreState={behaviourPre} PostState={behaviourPost}");
                Log.Debug($"  posKeyCount={posKeyCount} (MNumPositionKeys={channel->MNumPositionKeys})");
                Log.Debug($"  rotKeyCount={rotKeyCount} (MNumRotationKeys={channel->MNumRotationKeys}) << USES MNumPositionKeys (BUG)");
                Log.Debug($"  sclKeyCount={sclKeyCount} (MNumScalingKeys={channel->MNumScalingKeys}) << USES MNumPositionKeys (BUG)");
                Log.Debug($"  ACTUAL: pos={channel->MNumPositionKeys} rot={channel->MNumRotationKeys} scl={channel->MNumScalingKeys}");
#endif


                int maxKeys = (int)Math.Max(Math.Max(posKeyCount, rotKeyCount), sclKeyCount);

#if DEBUG_ASSIMP
                Log.Debug($"  maxKeys (computed)={maxKeys}");
                if (channel->MNumPositionKeys > 0)
                {
                    var firstPos = channel->MPositionKeys[0];
                    var lastPos = channel->MPositionKeys[channel->MNumPositionKeys - 1];
                    Log.Debug($"  PosKeys: ft={firstPos.MTime:F2} last={lastPos.MTime:F2} firstVal={firstPos.MValue.ToStr()} lastVal={lastPos.MValue.ToStr()}");
                }
                if (channel->MNumRotationKeys > 0)
                {
                    var firstRot = channel->MRotationKeys[0];
                    var lastRot = channel->MRotationKeys[channel->MNumRotationKeys - 1];
                    Log.Debug($"  RotKeys: ft={firstRot.MTime:F2} last={lastRot.MTime:F2} firstVal={firstRot.MValue.ToStr()} lastVal={lastRot.MValue.ToStr()}");
                }
                if (channel->MNumScalingKeys > 0)
                {
                    var firstScl = channel->MScalingKeys[0];
                    var lastScl = channel->MScalingKeys[channel->MNumScalingKeys - 1];
                    Log.Debug($"  SclKeys: ft={firstScl.MTime:F2} last={lastScl.MTime:F2} firstVal={firstScl.MValue.ToStr()} lastVal={lastScl.MValue.ToStr()}");
                }
#endif

                for (int k = 0; k < maxKeys; k++)
                {
                    var t = (float)(
                        (k < posKeyCount) ? channel->MPositionKeys[k].MTime :
                        (k < rotKeyCount) ? channel->MRotationKeys[k].MTime :
                        channel->MScalingKeys[k].MTime
                    );

                    //Vector3 pos, scl;
                    //Quaternion rot;

                    //if (k == 0)
                    //{
                    //    if(k)
                    //    pos = Vector3.Zero;
                    //    scl = Vector3.One;
                    //    rot = Quaternion.Identity;
                    //}
                    //else
                    //{
                    //}
                    var pos = (k < posKeyCount) ? channel->MPositionKeys[k].MValue : channel->MPositionKeys[channel->MNumPositionKeys - 1].MValue;
                    var rot = (k < rotKeyCount) ? channel->MRotationKeys[k].MValue : channel->MRotationKeys[channel->MNumRotationKeys - 1].MValue;
                    var scl = (k < sclKeyCount) ? channel->MScalingKeys[k].MValue : channel->MScalingKeys[channel->MNumScalingKeys - 1].MValue;
                    
                    keyframes[info.ID].Add(new Keyframe((float)t, scl, rot, pos));
                }

#if DEBUG_ASSIMP
                Log.Debug($"  Stored {keyframes[info.ID].Count} keyframes for bone[{info.ID}]");
#endif
            }

            var result = new Keyframe[boneCount][];
            for (int i = 0; i < boneCount; i++)
            {
                result[i] = keyframes[i].ToArray();
#if DEBUG_ASSIMP
                if (result[i].Length > 0)
                {
                    Log.Debug($"Bone[{i}]: {result[i].Length} keyframes, f0 t={result[i][0].TimeStamp:F2} l t={result[i][^1].TimeStamp:F2}");
                }
#endif
            }
            return result;
        }
        
    }



}
