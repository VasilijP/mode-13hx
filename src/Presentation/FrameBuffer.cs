using System.Collections.Concurrent;
using System.Diagnostics;
using mode13hx.Configuration;
using OpenTK.Graphics.OpenGL;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
namespace mode13hx.Presentation;

public class FrameBuffer
{
    public readonly int Width;
    public readonly int Height;
    public readonly int FrameSize;
    public int DroppedFrames { get; private set; }
    public static readonly FrametimeComponent Fps = new(400, 4.0);

    private readonly int frameBufferCount; // +1 for the actual buffer being rendered to
    
    private readonly int glHandle;
    public readonly uint[] Data;
    
    private int actualBufferIndex = 0;
    private readonly SemaphoreSlim renderSemaphore;
    private readonly FrameDescriptor[] frameDescriptors;
    private readonly Stopwatch frameTimer = Stopwatch.StartNew();
    private double lastFrameTime = 0;
    
    // Bounded frame queue of frames waiting to be presented. We allow only up to FramesPrerenderLimit frames to be queued up, next frame could be actually rendered and waiting to joing the queue.
    private readonly BlockingCollection<int> frameQueue; // The default collection type for BlockingCollection<T> is ConcurrentQueue<T>

    public FrameBuffer(CommonOptions config)
    {
        Width = config.Width;
        Height = config.Height;
        FrameSize = Width * Height;
        frameBufferCount = config.FramesPrerenderLimit + 1;
        renderSemaphore = new SemaphoreSlim(config.FramesPrerenderLimit-1, config.FramesPrerenderLimit);
        Data = new uint[frameBufferCount * Width * Height];
        frameDescriptors = new FrameDescriptor[frameBufferCount];
        frameQueue = new BlockingCollection<int>(boundedCapacity: config.FramesPrerenderLimit);
        frameQueue.Add(actualBufferIndex); // add first frame in order to finalize init in this constructor NOTE: this adds +1 to renderSemaphore once the frame is presented or discarded in Use()
        
        for (int i = 0; i < frameBufferCount; ++i) { frameDescriptors[i] = new FrameDescriptor(i, FrameOffset(i), this); }
        
        glHandle = GL.GenTexture(); // texture handle
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, glHandle);
        
        Use();
        
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear); // scale down (filter, e.g. 8k -> 4k)
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest); // scale up (pixelated look)
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat); // S is for the X axis
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat); // T is for the Y axis.
    }
    
    // Returns index of the next frame to be rendered.
    public FrameDescriptor StartNextFrame()
    {
        renderSemaphore.Wait();
        return frameDescriptors[actualBufferIndex = (actualBufferIndex + 1) % frameBufferCount];
    }
    
    public void FinishFrame(FrameDescriptor fd) 
    {
        frameQueue.Add(fd.FrameIndex);
        double time = frameTimer.Elapsed.TotalSeconds;
        Fps.RecordFrame(time - lastFrameTime); // we don't measure time from StartNextFrame to FinishFrame but intervals between rendered frames as it is closer to the actual framerate (though it may not be displayed if frames are dropped)
        lastFrameTime = time;
    }
    
    private int FrameOffset(int frameIndex) => frameIndex * FrameSize;

    // update texture with latest rendered frame
    public void Use(ScreenRecorder sr = null)
    {
        int release = 0;
        try
        {
            int readyFrameIndex = frameQueue.Take(); ++release;
            while (frameQueue.TryTake(out int nextFrame)){ readyFrameIndex = nextFrame; ++release; } // advance to the latest frame in the queue
            if (sr != null) // capture the frame and draw recorded area to the screen
            {
                FrameDescriptor fd = frameDescriptors[readyFrameIndex];
                sr.Capture(fd);
                fd.Canvas.SetPenColor(0xFF, 0, 0).MoveTo(sr.XOffset, sr.YOffset).Line(sr.CapWidth, 0).Line(0, sr.CapHeight).Line(-sr.CapWidth, 0).Line(0, -sr.CapHeight);
            }
            ReadOnlySpan<uint> frameData = new(Data, FrameOffset(readyFrameIndex), FrameSize);
            GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, Height, Width, 0, PixelFormat.Rgba, PixelType.UnsignedByte, frameData);
        }
        finally
        {
            renderSemaphore.Release(release);
            DroppedFrames += release-1;
        }
    }
}
