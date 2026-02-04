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
        public float CurrentTime { get; private set; }
        public Transform[] CurrentFrame { get; private set; }
        public Matrix4x4[] Transforms { get; private set; }

        private Keyframe[][] _keyframes;


        private int _boneCount;
        public float _randomStartOffset;
        public unsafe Animation(string name, Scene* scene, Dictionary<string, BoneInfo> boneInfoMap)
        {
            Name = name;
            var assAnimation = scene->MAnimations[0];
            Duration = (float)assAnimation->MDuration;
            TicksPerSecond = (float)assAnimation->MTicksPerSecond;
            if (TicksPerSecond <= 0)
                TicksPerSecond = 25.0f;
            _randomStartOffset = (float)new Random().NextDouble() * Duration;
            CurrentFrame = new Transform[boneInfoMap.Count];

            for (int i = 0; i < CurrentFrame.Length; i++)
            {
                CurrentFrame[i] = new Transform(Vector3.One, Quaternion.Identity, Vector3.Zero);
            }
            _keyframes = ReadKeyFrames(assAnimation, boneInfoMap);

            _boneCount = _keyframes.GetLength(0);

            Transforms = new Matrix4x4[Vertex.MAX_BONE_COUNT];

            for (int b = 0; b < Vertex.MAX_BONE_COUNT; b++)
            {
                Transforms[b] = Matrix4x4.Identity;
            }
            
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

                if (!boneInfoMap.TryGetValue(nodeName, out var info))
                    continue;

                int maxKeys = (int)Math.Max(Math.Max(channel->MNumPositionKeys, channel->MNumRotationKeys), channel->MNumScalingKeys);
                for (int k = 0; k < maxKeys; k++)
                {
                    var t = (float)(
                        (k < channel->MNumPositionKeys) ? channel->MPositionKeys[k].MTime :
                        (k < channel->MNumRotationKeys) ? channel->MRotationKeys[k].MTime :
                        channel->MScalingKeys[k].MTime
                    );

                    var pos = (k < channel->MNumPositionKeys) ? channel->MPositionKeys[k].MValue : channel->MPositionKeys[channel->MNumPositionKeys - 1].MValue;
                    var rot = (k < channel->MNumRotationKeys) ? channel->MRotationKeys[k].MValue : channel->MRotationKeys[channel->MNumRotationKeys - 1].MValue;
                    var scl = (k < channel->MNumScalingKeys) ? channel->MScalingKeys[k].MValue : channel->MScalingKeys[channel->MNumScalingKeys - 1].MValue;

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
