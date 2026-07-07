using System.Numerics;

namespace Phoenix.AssetTool.Core.Model.Animation
{
    public class Keyframe
    {
        public float TimeStamp { get; }
        public Transform SRT { get; internal set; }
        public Keyframe(float timeStamp, Vector3 scale, Quaternion rotation, Vector3 position)
        {
            TimeStamp = timeStamp;
            SRT = new Transform(scale, rotation, position);
        }
    }
}
