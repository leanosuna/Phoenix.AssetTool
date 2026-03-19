using Phoenix.AssetTool.Core.Model;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Text;
namespace Phoenix.AssetTool.Core.Shader
{
    public static class GLCompiler
    {
        private static GL GL = default!;
        private static ContextState _contextState = ContextState.Invalid;
        private static IWindow _window = default!;
        public static void Set(GL gl)
        {
            GL = gl;
        }

        public static CompileResult Compile(string vertSource, string fragSource)
        {
            Init();

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
            for (int i = 0; i < uniformsCount; i++)
            {
                var name = GL.GetActiveUniform(handle, (uint)i, out int size, out UniformType type);

                Log.Debug($"{type} {name} sz {size}");
            }

            GL.DetachShader(handle, vertex);
            GL.DetachShader(handle, fragment);
            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);


            return new CompileResult { Success = true };
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

        public static void Init()
        {   
            _contextState = ContextState.Creating;

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(1, 1);
            options.IsVisible = false;
            var glApi = new APIVersion(4, 1);
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, glApi);

            _window = Window.Create(options);
            _window.Initialize();
            GL = GL.GetApi(_window);
            _contextState = ContextState.Set;
        }
        public static void Dispose()
        {
            _window.Dispose();
        }
    }

    internal enum ContextState
    {
        Invalid,
        Creating,
        Set
        //SetFromGui
    }


    public class CompileResult
    {
        public bool Success;
        public string ErrorMessage = "";

        public CompileResult()
        {
            
        }

    }
}
