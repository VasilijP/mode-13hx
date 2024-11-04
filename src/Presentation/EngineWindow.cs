using System.Diagnostics;
using mode13hx.Configuration;
using mode13hx.Model;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using Timer = System.Timers.Timer;

namespace mode13hx.Presentation;

public class EngineWindow : GameWindow
{
    // In NDC, (0, 0) is the center of the screen.
    private readonly float[] vertices =
    [
        // Position         Texture coordinates
         1.0f,  1.0f, 0.0f, 0.0f, 1.0f, // top right
         1.0f, -1.0f, 0.0f, 1.0f, 1.0f, // top left
        -1.0f, -1.0f, 0.0f, 1.0f, 0.0f, // bottom left
        -1.0f,  1.0f, 0.0f, 0.0f, 0.0f, // bottom right
    ];
    
    private readonly uint[] indices =
    [
        0, 1, 3,
        1, 2, 3
    ];
    
    private int elementBufferObject;
    private int vertexBufferObject; // handle
    private int vertexArrayObject; // handle
    
    private Shader shader;
    private FrameBuffer frameBuffer;
    private readonly Thread renderThread;
    private readonly IRasterizer rasterizer;

    private readonly Stopwatch elapsedTime;
    private long frameCount = 0;
    private readonly Timer timer = new(1000);
    private readonly CommonOptions config;
    private ScreenRecorder screenRecorder;

    public EngineWindow(CommonOptions config, IRasterizer rasterizer, GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    {
        this.config = config;
        this.rasterizer = rasterizer;
        renderThread = new Thread(RenderThreadMain);
        elapsedTime = Stopwatch.StartNew();
        // FPS: 2736.1887571167426, elapsed time: 92.9098913000005, frame count: 254219
        // FPS: 2737.1297002713604, elapsed time: 93.90968940000053, frame count: 257043
        //TODO: replace with proper frametime/fps counting component
        timer.Elapsed += (_, _) => { Console.WriteLine($"FPS copied: {frameCount / elapsedTime.Elapsed.TotalSeconds}, FPS rendered: {(frameCount+frameBuffer.DroppedFrames) / elapsedTime.Elapsed.TotalSeconds}, elapsed time: {elapsedTime}, frame count: {frameCount} +dropped: {frameBuffer.DroppedFrames} PCIe Bandwidth: {config.Width * config.Height * frameCount * 4 / elapsedTime.Elapsed.TotalSeconds / 1024 / 1024} MB/s"); };
        timer.Start();
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        
        CursorState = CursorState.Grabbed;
        
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f); // This will be the color of the background after we clear it, in normalized colors.

        vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayObject);    

        vertexBufferObject = GL.GenBuffer(); // First, we need to create a buffer. This function returns a handle to it, but as of right now, it's empty.
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject); // Now, bind the buffer. OpenGL uses one global state, so after calling this, all future calls that modify the VBO will be applied to this buffer until another buffer is bound instead.
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsage.StaticDraw); // Finally, upload the vertices to the buffer.
        
        elementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsage.StaticDraw);
                
        shader = new Shader("resources/shader.vert", "resources/shader.frag");
        shader.Use(); // Now, enable the shader. Just like the VBO, this is global, so every function that uses a shader will modify this one until a new one is bound instead.        
                
        uint vertexLocation = (uint)shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);         // Enable variable 0 in the shader.
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        
        uint texCoordLocation = (uint)shader.GetAttribLocation("aTexCoord");
        GL.EnableVertexAttribArray(texCoordLocation);
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        frameBuffer = new FrameBuffer(config);
        elapsedTime.Restart();
        renderThread.Start();
    }
    
    private void RenderThreadMain()
    {
        Stopwatch sw = Stopwatch.StartNew();
        
        while (!IsExiting)
        {
            double secondsSinceLastFrame = sw.Elapsed.TotalSeconds; sw.Restart();
            rasterizer.Render(frameBuffer, secondsSinceLastFrame);
        }
    }
    
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        frameCount++;
        
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.BindVertexArray(vertexArrayObject); // Bind the VAO
        frameBuffer.Use(screenRecorder); // TODO: maintain minimum update rate even if render thread is lagging (do not flip/draw, but update input, world, etc.)
        shader.Use(); // Bind the shader
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0); //GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        SwapBuffers(); // swap the buffers to display the rendered image
    }
    
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        if (KeyboardState.IsKeyDown(Keys.Escape)) { Close(); }
        Control.Reset();
        foreach (KeyValuePair<Keys, Control> c in config.KbControls) { c.Value.Active |= KeyboardState.IsKeyDown(c.Key); }
        Interlocked.Add(ref config.MouseControls[ControlEnum.MOUSE_DELTA_X].Delta, (int)MouseState.Delta.X);
        Interlocked.Add(ref config.MouseControls[ControlEnum.MOUSE_DELTA_Y].Delta, (int)MouseState.Delta.Y);
        Interlocked.Add(ref config.MouseControls[ControlEnum.MOUSE_DELTA_WHEEL].Delta, (int)MouseState.ScrollDelta.Y);
        config.MouseControls[ControlEnum.MOUSE_BUTTON_LEFT].Active |= MouseState.IsButtonDown(MouseButton.Left);
        config.MouseControls[ControlEnum.MOUSE_BUTTON_RIGHT].Active |= MouseState.IsButtonDown(MouseButton.Right);
        
        // Screen Recording Hotkey
        if (screenRecorder != null && screenRecorder.IsFinished()) { screenRecorder.CapTexture.SaveVariant(); screenRecorder = null; }
        if (KeyboardState.IsKeyDown(Keys.R) && screenRecorder == null) { screenRecorder = new ScreenRecorder(config.Rx, config.Ry, config.Rw, config.Rh, config.Rfps, config.Rframes); }
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
    }
}