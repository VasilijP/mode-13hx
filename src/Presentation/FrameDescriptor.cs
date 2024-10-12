namespace mode13hx.Presentation;

public sealed class FrameDescriptor
{
    public readonly int FrameIndex;
    public readonly int Offset;
    public readonly FrameBuffer Buffer;

    internal FrameDescriptor(int frameIndex, int offset, FrameBuffer buffer)
    {
        FrameIndex = frameIndex;
        Offset = offset;
        Buffer = buffer;
    }
}
