using OpenTK.Graphics.OpenGL;

namespace mode13hx.Presentation;

public class Shader
{
    private readonly int handle;

    public Shader(string vertPath, string fragPath)
    {
        string shaderSource = File.ReadAllText(vertPath);
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        
        GL.ShaderSource(vertexShader, shaderSource);
        CompileShader(vertexShader);
        
        shaderSource = File.ReadAllText(fragPath);
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, shaderSource);
        CompileShader(fragmentShader);
        
        handle = GL.CreateProgram();
        GL.AttachShader(handle, vertexShader);
        GL.AttachShader(handle, fragmentShader);
        LinkProgram(handle);
        
        GL.DetachShader(handle, vertexShader);
        GL.DetachShader(handle, fragmentShader);
        GL.DeleteShader(fragmentShader);
        GL.DeleteShader(vertexShader);
    }

    private static void CompileShader(int shader)
    {
        GL.CompileShader(shader);
        GL.GetShaderi(shader, ShaderParameterName.CompileStatus, out int code);
        if (code == (int)All.True) return;
        
        GL.GetShaderInfoLog(shader, out string infoLog);
        throw new Exception($"Error: {shader} {infoLog}");
    }

    private static void LinkProgram(int program)
    {
        GL.LinkProgram(program);
        GL.GetProgrami(program, ProgramProperty.LinkStatus, out int code);
        if (code != (int)All.True) { throw new Exception($"Error: {program}"); }
    }
    
    public void Use() { GL.UseProgram(handle); }
    public int GetAttribLocation(string attribName) { return GL.GetAttribLocation(handle, attribName); }
}
