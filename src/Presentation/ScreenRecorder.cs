using System.Diagnostics;
using mode13hx.Model;
using mode13hx.Util;

namespace mode13hx.Presentation;

public class ScreenRecorder(int xOffset, int yOffset, int capWidth, int capHeight, int fps, int totalFrames)
{
    public readonly int XOffset = xOffset;
    public readonly int YOffset = yOffset;
    public readonly int CapWidth = capWidth;
    public readonly int CapHeight = capHeight;
    private int capFrameCount = 0;
    public readonly Texture CapTexture = Texture.CreateBlank(totalFrames, capWidth * capHeight, 0x0);
    private readonly double timePerFrame = 1.0 / fps;
    
    private readonly int[] accBuffer = new int[capWidth * capHeight * 3]; // RGB
    private int acc = 0; // count of accumulated frames
    private readonly Stopwatch accTime = Stopwatch.StartNew(); // accumulated time

    public bool IsFinished() => capFrameCount >= totalFrames;
    
    public void Capture(FrameDescriptor fd)
    {
        if (IsFinished()) { return; }
        if (acc > 0 && accTime.Elapsed.TotalSeconds > (1.0+capFrameCount)*timePerFrame) // flush current frame to output texture (if we got at least one frame)
        {
            int frameOffset = capFrameCount * CapWidth * CapHeight; // thanks to texture being stored as columns, we can just add the offset to the current frame
            for (int cx = 0; cx < CapWidth; ++cx)
            {
                for (int cy = 0; cy < CapHeight; ++cy)
                {   // write acc[cx,cy] to output texture at t[capFrameCount*capWidth * capHeight + cx*CapHeight + cy]
                    int capIndex = 3*(cx*CapHeight + cy);
                    int r = accBuffer[capIndex++]/acc;
                    int g = accBuffer[capIndex++]/acc;
                    int b = accBuffer[capIndex]/acc;
                    uint color = Func.EncodePixelColor(r, g, b);
                    CapTexture.Data[frameOffset + cx * CapHeight + cy] = color;
                }
            }
            
            ++capFrameCount; 
            acc = 0; Array.Clear(accBuffer, 0, accBuffer.Length); // reset accumulator for next frame
        }
        
        // accumulate frame
        acc++;
        for (int x = 0; x < CapWidth; ++x) 
        {   
            int index = fd.Offset + (XOffset + x) * fd.Buffer.Height + YOffset;
            for (int y = 0; y < CapHeight; ++y, ++index)
            {
                Func.DecodePixelColor(fd.Buffer.Data[index], out int r, out int g, out int b);
                int capIndex = 3*(x*CapHeight + y);
                accBuffer[capIndex++] += r;
                accBuffer[capIndex++] += g;
                accBuffer[capIndex] += b;
            }
        }
    }
    
}