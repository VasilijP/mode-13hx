using System.Collections.Concurrent;
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

    private readonly int frameBufferCount; // +1 for the actual buffer being rendered to
    
    private readonly int glHandle;
    public readonly uint[] Data;
    
    private int actualBufferIndex = 0;
    private readonly SemaphoreSlim renderSemaphore;
    
    // Bounded frame queue of frames waiting to be presented. We allow only up to FramesPrerenderLimit frames to be queued up, next frame could be actually rendered and waiting to joing the queue.
    private readonly BlockingCollection<int> frameQueue; // The default collection type for BlockingCollection<T> is ConcurrentQueue<T>

    public FrameBuffer(CommonOptions config)
    {
        Width = config.Width;
        Height = config.Height;
        FrameSize = Width * Height;
        frameBufferCount = config.FramesPrerenderLimit + 1;
        renderSemaphore = new SemaphoreSlim(config.FramesPrerenderLimit, frameBufferCount);
        Data = new uint[frameBufferCount * Width * Height];
        frameQueue = new BlockingCollection<int>(boundedCapacity: config.FramesPrerenderLimit);
        frameQueue.Add(actualBufferIndex); // add first frame in order to finalize init in this constructor NOTE: this adds +1 to renderSemaphore once the frame is presented or discarded in Use()
        
        glHandle = GL.GenTexture(); // texture handle
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, glHandle);
        
        Use();
        
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest); // scale down
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest); // scale up
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat); // S is for the X axis
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat); // T is for the Y axis.
    }
    
    // Returns index of the next frame to be rendered.
    public FrameDescriptor StartNextFrame()
    {
        renderSemaphore.Wait();
        int index = actualBufferIndex = (actualBufferIndex + 1) % frameBufferCount;
        int offset = FrameOffset(index);
        return new FrameDescriptor(index, offset, this);
    }
    
    public void FinishFrame(FrameDescriptor fd) { frameQueue.Add(fd.FrameIndex); }
    private int FrameOffset(int frameIndex) => frameIndex * FrameSize;

    // update texture with latest rendered frame
    public void Use()
    {
        int release = 0;
        try
        {
            int readyFrameIndex = frameQueue.Take(); ++release;
            while (frameQueue.TryTake(out int nextFrame)){ readyFrameIndex = nextFrame; ++release; } // advance to the latest frame in the queue
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
