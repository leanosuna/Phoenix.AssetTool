using Phoenix.AssetTool.Core.Model;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;
namespace Phoenix.AssetTool.Core.Shader
{
    public static class GLCompiler
    {
        private static GL GL = default!;
        
        public static void Init(GL gl)
        {
            GL = gl;
        }

        
        public static CompileResult Compile(string vertSource, string fragSource)
        {
            //return new CompileResult { Success = true };

            var result = LoadShader(ShaderType.VertexShader, vertSource, out uint vertex);
            if (!result.Success)
                return result;

            result = LoadShader(ShaderType.FragmentShader, fragSource, out uint fragment);
            if (!result.Success)
                return result;

            var handle = GL.CreateProgram();
            
            GL.AttachShader(handle, vertex);
            GL.AttachShader(handle, fragment);
            GL.LinkProgram(handle);
            GL.GetProgram(handle, GLEnum.LinkStatus, out var status);
            
            if (status == 0)
                return new CompileResult { Success = false, ErrorMessage = $"[PROGRAM FAIL] {GL.GetProgramInfoLog(handle)}" };
            
            GL.GetProgram(handle, GLEnum.ActiveUniforms, out int uniformsCount);
            List<ShaderUniformInfo> uniformsInfo = new();
            for (int i = 0; i < uniformsCount; i++)
            {
                var name = GL.GetActiveUniform(handle, (uint)i, out int size, out UniformType type);

                name = name.EndsWith("[0]") ? name.Substring(0, name.Length - 3) : name;

                var location = GL.GetUniformLocation(handle, name);

                if (location != -1)
                {
                    //GL.GetActiveUniformBlock(handle, 0, GLEnum.UniformBlockIndex, out int index);
                    //if (index != -1)
                    //    Log.Debug($"{type} {name} sz {size} UBO {index}");
                    uniformsInfo.Add(new ShaderUniformInfo{Name = name, Type = type, Size = size});
                }

                //Log.Debug($"{type} {name} sz {size}");
            }

            GL.DetachShader(handle, vertex);
            GL.DetachShader(handle, fragment);
            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);


            return new CompileResult { Success = true, UniformsInfo = uniformsInfo };
        }

        private static CompileResult LoadShader(ShaderType type, string src, out uint handle)
        {
            handle = GL.CreateShader(type);
            GL.ShaderSource(handle, src);
            GL.CompileShader(handle);
            string infoLog = GL.GetShaderInfoLog(handle);
            
            if (!string.IsNullOrWhiteSpace(infoLog))
                return new CompileResult { Success = false, ErrorMessage = $"[{type} FAIL] : {infoLog}"};
            
            return new CompileResult { Success = true };
        }
    }

    public class CompileResult
    {
        public bool Success = false;
        public string ErrorMessage = "";
        public List<ShaderUniformInfo> UniformsInfo = default!;
        public CompileResult()
        {
            
        }

    }

    public class ShaderUniformInfo
    {
        public string Name = "";
        public UniformType Type = default;
        public int Size = 0;
        public ShaderUniformInfo()
        {
        }

        
    }
}
