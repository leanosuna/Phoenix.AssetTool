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
