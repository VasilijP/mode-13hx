﻿using mode13hx.Model;
using mode13hx.Presentation;

namespace mode13hx.Util;

// Canvas for drawing into buffer
public sealed class Canvas(FrameDescriptor frame)
{
    // 8 bit per RGB channel
    private uint penColor;
    private int penX = 0;
    private int penY = 0;
    
    private int clipX = 0;
    private int clipXw = frame.Buffer.Width;
    private int clipY = 0;
    private int clipYh = frame.Buffer.Height;
    
    // font texture
    public static readonly Font Font16X16 = new("resources/texture/oldschool_16x16.png", 16, 16);
    public static readonly Font Font9X16 = new("resources/texture/oldschool_9x16.png", 9, 16);
    
    public Canvas SetPenColor(int r, int g, int b) { penColor = Func.EncodePixelColor(r, g, b); return this; }
    public Canvas SetPenColor(uint color) { penColor = color; return this; }
    
    public void SetClip(int cX, int cY, int cWidth, int cHeight) { clipX = cX; clipY = cY; clipXw = cX + cWidth; clipYh = cY + cHeight; }
    public void ResetClip() { clipX = 0; clipY = 0; clipXw = frame.Buffer.Width; clipYh = frame.Buffer.Height; }
    
    public Canvas MoveTo(int x, int y) { penX = x; penY = y; return this;}
    public Canvas Move(int dx, int dy) { penX += dx; penY += dy; return this;}
    
    // Draws a line from current position to (x,y) but pen position is optionally not changed
    public Canvas DrawTo(int x, int y, bool movePen = false) { int a = penX, b = penY; LineTo(x, y); if (!movePen) { MoveTo(a, b); } return this; }
    
    // Draws a line from current position relative to +(dx, dy) but pen position is not changed
    public Canvas Draw(int dx, int dy, bool movePen = false) { return DrawTo(penX + dx, penY + dy, movePen); }
    
    // Draws a line from current position to (x,y)
    public Canvas LineTo(int x, int y)
    {
        int dx = Math.Abs(x - penX);
        int dy = Math.Abs(y - penY);
        int sx = penX < x ? 1 : -1;
        int sy = penY < y ? 1 : -1;
        int err = dx - dy;

        SetPixel(penX, penY);
        while (penX != x || penY != y)
        {
            SetPixel(penX, penY);
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; penX += sx; }
            if (e2 < dx) { err += dx; penY += sy; }
        }
        return this;
    }
    
    public Canvas Line(int dx, int dy) { return LineTo(penX + dx, penY + dy); }

    // clipped and slow, use only for small objects
    public void SetPixel(int x, int y)
    {
        if (x < clipX || x >= clipXw || y < clipY || y >= clipYh) return;
        int index = frame.Offset + x * frame.Buffer.Height + y;
        frame.Buffer.Data[index] = penColor;
    }
    
    public void Rectangle(float positionX, float positionY, int boxWidth, int boxHeight)
    {
        int xStart = (int)Math.Max(clipX, positionX);
        int yStart = (int)Math.Max(clipY, positionY);
        int xEnd = (int)Math.Min(clipXw, positionX + boxWidth);
        int yEnd = (int)Math.Min(clipYh, positionY + boxHeight);
        int index1 = frame.Offset + xStart*frame.Buffer.Height + yStart;
        for (int x1 = xStart; x1 < xEnd; ++x1, index1 += frame.Buffer.Height)
        {
            Array.Fill(frame.Buffer.Data, penColor, index1, Math.Max(0, yEnd - yStart));
        }
    }
    
    // Method to draw a texture onto the canvas at position (posX, posY)
    public void Draw(Texture texture, int posX, int posY, int variant = -1)
    {
        if (!texture.Loaded) { texture.LoadData(); }
        
        uint[] textureData = (variant >= 0)?texture.Variant[variant]:texture.Data;
        int startX = Math.Max(clipX, posX); int endX = Math.Min(clipXw, posX + texture.Width);
        int startY = Math.Max(clipY, posY); int endY = Math.Min(clipYh, posY + texture.Height);
        
        // Adjust starting indices for texture data
        int textureStartX = startX - posX;
        int textureStartY = startY - posY;

        // Loop over columns (x)
        for (int x = startX; x < endX; x++)
        {
            int frameColumnIndex = frame.Offset + x * frame.Buffer.Height + startY;
            int textureColumnIndex = (textureStartX + x - startX) * texture.Height + textureStartY;
            Buffer.BlockCopy(textureData, textureColumnIndex * sizeof(uint), frame.Buffer.Data, frameColumnIndex * sizeof(uint), Math.Max(0, (endY - startY)) * sizeof(uint));
        }
    }
    
    public void DrawScaled(Texture texture, int posX, int posY, int newWidth, int newHeight, int variant = -1)
    {
        // Ensure the texture data is loaded
        if (!texture.Loaded) { texture.LoadData(); }
        uint[] textureData = (variant >= 0) ? texture.Variant[variant] : texture.Data;

        int sourceWidth = texture.Width;
        int sourceHeight = texture.Height;

        // Calculate drawing boundaries with clipping
        int destStartX = Math.Max(clipX, posX);
        int destEndX = Math.Min(clipXw, posX + newWidth);
        int destStartY = Math.Max(clipY, posY);
        int destEndY = Math.Min(clipYh, posY + newHeight);

        int destWidthClipped = destEndX - destStartX;
        int destHeightClipped = destEndY - destStartY;

        // Compute scaling ratios using fixed-point arithmetic (16.16 format)
        int xRatio = ((sourceWidth << 16) / newWidth) + 1;
        int yRatio = ((sourceHeight << 16) / newHeight) + 1;

        // Precompute source X indices
        int[] sourceXs = new int[destWidthClipped];
        for (int i = 0; i < destWidthClipped; i++)
        {
            int x = destStartX - posX + i;
            int sourceX = Math.Clamp((x * xRatio) >> 16, 0, sourceWidth - 1);
            sourceXs[i] = sourceX;
        }

        // Precompute source Y indices
        int[] sourceYs = new int[destHeightClipped];
        for (int j = 0; j < destHeightClipped; j++)
        {
            int y = destStartY - posY + j;
            int sourceY = Math.Clamp((y * yRatio) >> 16, 0, sourceHeight - 1);
            sourceYs[j] = sourceY;
        }

        for (int ix = 0; ix < destWidthClipped; ++ix) // Loop over columns (x-axis)
        {
            int frameColumnIndex = frame.Offset + (destStartX + ix) * frame.Buffer.Height + destStartY;
            int textureColumnIndex = sourceXs[ix] * sourceHeight;
            for (int j = 0; j < destHeightClipped; ++j) // Loop over rows (y-axis) within the column
            {
                frame.Buffer.Data[frameColumnIndex + j] = textureData[textureColumnIndex + sourceYs[j]];
            }
        }
    }
    
    public void DrawMip(Texture texture, int posX, int posY, int mW, int mH, int variant = -1)
    {
        string key = $"{mW}_{mH}_{variant}";
        if (!texture.MipMap.TryGetValue(key, out uint[] textureData)) { return; }

        int startX = Math.Max(clipX, posX); int endX = Math.Min(clipXw, posX + mW);
        int startY = Math.Max(clipY, posY); int endY = Math.Min(clipYh, posY + mH);

        // Adjust starting indices for texture data
        int textureStartX = startX - posX;
        int textureStartY = startY - posY;

        // Loop over columns (x)
        for (int x = startX; x < endX; x++)
        {
            int frameColumnIndex = frame.Offset + x * frame.Buffer.Height + startY;
            int textureColumnIndex = (textureStartX + x - startX) * mH + textureStartY;
            Buffer.BlockCopy(textureData, textureColumnIndex * sizeof(uint), frame.Buffer.Data, frameColumnIndex * sizeof(uint), Math.Max(0, (endY - startY)) * sizeof(uint));
        }
    }
    
    public Canvas FillTriangle(int x1, int y1, int x2, int y2, int x3, int y3)
    {
        // y1 ≤ y2 ≤ y3
        if (y2 < y1) { Func.Swap(ref x1, ref x2); Func.Swap(ref y1, ref y2); }
        if (y3 < y1) { Func.Swap(ref x1, ref x3); Func.Swap(ref y1, ref y3); }
        if (y3 < y2) { Func.Swap(ref x2, ref x3); Func.Swap(ref y2, ref y3); }
        
        float dx1 = (y2 - y1) > 0 ? (float)(x2 - x1) / (y2 - y1) : 0;
        float dx2 = (y3 - y1) > 0 ? (float)(x3 - x1) / (y3 - y1) : 0;
        float dx3 = (y3 - y2) > 0 ? (float)(x3 - x2) / (y3 - y2) : 0;

        float sx = x1; float ex = x1;
        
        for (int y = y1; y <= y2; y++) // upper part of the triangle (from y1 to y2)
        {
            MoveTo((int)sx, y).LineTo((int)ex, y);
            sx += dx1;
            ex += dx2;
        }

        sx = x2;
        
        for (int y = y2; y <= y3; y++) // lower part of the triangle (from y2 to y3)
        {
            MoveTo((int)sx, y).LineTo((int)ex, y);
            sx += dx3;
            ex += dx2;
        }

        return this;
    }
    
    // draws a string onto the canvas at position (startX, startY)
    public void DrawString(string text, int startX, int startY, Font font = null)
    {
        font ??= Font9X16;
        int charactersPerRow = font.Texture.Width / font.CharacterWidth;

        // Loop through each character in the input string
        for (int i = 0; i < text.Length; i++)
        {
            byte charIndex = (byte)text[i]; // Get the ASCII value of the character (0-255)

            // Calculate the row and column in the font texture
            int charRow = charIndex / charactersPerRow;
            int charColumn = charIndex % charactersPerRow;

            // Calculate the starting coordinates in the font texture
            int sourceX = charColumn * font.CharacterWidth;
            int sourceY = charRow * font.CharacterHeight;

            // Calculate the starting position to draw this character on the canvas
            int drawX = startX + i * font.CharacterWidth;
            int drawY = startY;

            // Loop through the character's pixels
            for (int x = 0; x < font.CharacterWidth; x++)
                for (int y = 0; y < font.CharacterHeight; y++)
                {
                    uint pixel = font.Texture.Data[(sourceX + x) * font.Texture.Height + sourceY + y];
                    if (pixel == 0xFFFFFF) // white pixels (255, 255, 255) are used for the character and black for the background
                    {
                        SetPixel(drawX + x, drawY + y); // Draw the pixel using the current pen color, respecting the frame bounds
                    }
                }
        }
    }
}