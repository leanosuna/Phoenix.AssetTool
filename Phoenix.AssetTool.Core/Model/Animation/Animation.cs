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

            
            var newKeyframes = new List<Keyframe>[boneCount];
            for (int b = 0; b < boneCount; b++)
                newKeyframes[b] = new List<Keyframe>();

            foreach (var timestamp in allTimestamps)
            {
                List<Keyframe> kfs = new();
                for (int b = 0; b < boneCount; b++)
                {
                    var localSRT = InterpolateLocalSRT(Keyframes[b], timestamp);
                    kfs.Add(new Keyframe(timestamp, localSRT.Scale, localSRT.Rotation, localSRT.Translation));
                }

                ProcessFrame(nodes, inverseGlobalTransform, kfs);

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
                    animTransform = Matrix4x4.Transpose(animTransform);
                    localTransform = animTransform;
                }
                else
                    localTransform = node.BindTransform;

                node.Transform = parentTransform * localTransform;

                if (node.IsBone)
                {
                    var final = inverseGlobalTransform * node.Transform * node.Offset;
                    final = Matrix4x4.Transpose(final);
                    if (Matrix4x4.Decompose(final, out var scale, out var rotation, out var translation))
                        keyFrame[node.ModelBoneID].SRT = new Transform(scale, rotation, translation);
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
            if (boneInfoMap.TryGetValue(nodeName, out var exactBoneInfo))
            {
                info = exactBoneInfo;
                return true;
            }

            var bi = boneInfoMap.Select(e => (nodeName.Contains(e.Key)));

            foreach(var e in boneInfoMap)
            {
                if(nodeName.Contains(e.Key))
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

            for (int c = 0; c < anim->MNumChannels; c++)
            {
                var channel = anim->MChannels[c];
                var nodeName = channel->MNodeName;
                
                var behaviourPre = channel->MPreState;
                var behaviourPost = channel->MPostState;

                //if (!boneInfoMap.TryGetValue(nodeName, out var info))
                //    continue;

                if (!GetBoneInfo(nodeName, boneInfoMap, out var info))
                    continue;

                var posKeyCount = channel->MNumPositionKeys;
                var rotKeyCount = channel->MNumPositionKeys;
                var sclKeyCount = channel->MNumPositionKeys;


                int maxKeys = (int)Math.Max(Math.Max(posKeyCount, rotKeyCount), sclKeyCount);
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
            }

            var result = new Keyframe[boneCount][];
            for (int i = 0; i < boneCount; i++)
                result[i] = keyframes[i].ToArray();
            return result;
        }
        
    }



}
