using mode13hx.Util;

namespace mode13hx.Presentation;

public sealed class FrameDescriptor
{
    public readonly int FrameIndex;
    public readonly int Offset;
    public readonly FrameBuffer Buffer;
    public Span<uint> FrameSpan => new Span<uint>(Buffer.Data, Offset, Buffer.FrameSize); 
    public readonly Canvas Canvas;

    internal FrameDescriptor(int frameIndex, int offset, FrameBuffer buffer)
    {
        FrameIndex = frameIndex;
        Offset = offset;
        Buffer = buffer;
        Canvas = new Canvas(this);
    }
}
