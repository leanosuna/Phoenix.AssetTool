using Phoenix.AssetTool.Core.Model;

namespace Phoenix.Rendering.Geometry
{
    public class ModelPart
    {
        public string Name { get; private set; }
        public List<Mesh> Meshes { get; private set; }

        public ModelPart(string name, List<Mesh> meshes)
        {
            Name = name;
            Meshes = meshes;
        }
    }
}
