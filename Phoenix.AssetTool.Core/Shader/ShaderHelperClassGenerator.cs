using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Phoenix.AssetTool.Core.Shader
{
    public static class ShaderHelperClassGenerator
    {
        public static void Generate(string dir, string name, List<ShaderUniformInfo> uniforms)
        {
            

            var className = ToClassName(name);
            var fileName = className + ".cs";
            var outputPath = Path.Combine(
                dir,
                fileName);

            Log.Debug($"Generating {className} at {outputPath}");

            var source = GenerateString(className, uniforms);

            File.WriteAllText(outputPath, source);
        }
        public static string ToClassName(string name)
        {
            var firstChar = name.Substring(0, 1);
            firstChar = firstChar.ToUpperInvariant();

            var restOfName = name.Substring(1);
            var nameFirstUpper = firstChar+restOfName;
            return $"Shader{nameFirstUpper}";
        }
        public static string GenerateString(string className,List<ShaderUniformInfo> uniforms)
        {
            
            var sb = new StringBuilder();

            sb.AppendLine($"public partial class {className} : ShaderInterface");
            sb.AppendLine("{");
            sb.AppendLine("    private GLShader _shader;");

            foreach (var u in uniforms)
            {
                if(u.Type == UniformType.Sampler2D)
                {
                    sb.AppendLine($"    public ShaderTextureUniform {u.Name} {{get; private set;}}");
                    continue;
                }

                var csType = MapType(u);
                
                sb.AppendLine($"    public ShaderUniform<{csType}> {u.Name} {{get; private set;}}");
            }

            sb.AppendLine();
            sb.AppendLine($"    public {className}(GLShader shader)");
            sb.AppendLine("    {");
            sb.AppendLine("        _shader = shader;");

            int slot = 0;
            foreach (var u in uniforms)
            {
                if(u.Type == UniformType.Sampler2D)
                {
                    sb.AppendLine(
                        $"        {u.Name} = new ShaderTextureUniform(_shader, \"{u.Name}\", {slot});");
                    slot++;
                }
                else
                {
                    sb.AppendLine(
                        $"        {u.Name} = new ShaderUniform<{MapType(u)}>(_shader, \"{u.Name}\");");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine();
            
            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string MapType(ShaderUniformInfo u)
        { 
            var str = u.Type switch
            {
                UniformType.Float => "float",
                UniformType.FloatVec2 => "Vector2",
                UniformType.FloatVec3 => "Vector3",
                UniformType.FloatVec4 => "Vector4",

                //UniformType.FloatMat2 => "Matrix2x2",
                //UniformType.FloatMat3 => "Matrix3x3",
                UniformType.FloatMat4 => "Matrix4x4",

                //UniformType.FloatMat2x3 => "Matrix2x3",
                //UniformType.FloatMat2x4 => "Matrix2x4",
                //UniformType.FloatMat3x2 => "Matrix3x2",
                //UniformType.FloatMat3x4 => "Matrix3x4",
                //UniformType.FloatMat4x2 => "Matrix4x2",
                //UniformType.FloatMat4x3 => "Matrix4x3",

                UniformType.Int => "int",
                UniformType.IntVec2 => "Vector2",
                UniformType.IntVec3 => "Vector3",
                UniformType.IntVec4 => "Vector4",

                UniformType.UnsignedInt => "uint",
                //UniformType.UnsignedIntVec2 => "Vector2",
                //UniformType.UnsignedIntVec3 => "Vector3",
                //UniformType.UnsignedIntVec4 => "Vector4",

                UniformType.Bool => "bool",
                //UniformType.BoolVec2 => "Vector2",
                //UniformType.BoolVec3 => "Vector3",
                //UniformType.BoolVec4 => "Vector4",

                UniformType.Double => "double",
                //UniformType.DoubleVec2 => "Vector2",
                //UniformType.DoubleVec3 => "Vector3",
                //UniformType.DoubleVec4 => "Vector4",

                //UniformType.DoubleMat2 => "Matrix2x2Double",
                //UniformType.DoubleMat3 => "Matrix3x3Double",
                //UniformType.DoubleMat4 => "Matrix4x4Double",

                //UniformType.DoubleMat2x3 => "Matrix2x3Double",
                //UniformType.DoubleMat2x4 => "Matrix2x4Double",
                //UniformType.DoubleMat3x2 => "Matrix3x2Double",
                //UniformType.DoubleMat3x4 => "Matrix3x4Double",
                //UniformType.DoubleMat4x2 => "Matrix4x2Double",
                //UniformType.DoubleMat4x3 => "Matrix4x3Double",

                _ => "object"
            };

            if (u.Size > 1)
                str += "[]";

            return str;
        }
    }
}
