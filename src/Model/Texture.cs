using mode13hx.Util;
using tgalib_core;

namespace mode13hx.Model;

public class Texture
{
    // file name
    public string Name { get; set; }
    
    // is loaded flag
    public bool Loaded { get; set; }
    
    // data [x, y] @ 32 bit RGBA
    public uint[] Data;
    public readonly List<uint[]> Variant = [];
    public readonly Dictionary<string, uint[]> MipMap = new(); // width_height_variant -> data
    public int Width;
    public int Height;
    
    public void LoadData()
    {
        if (Data == null)
        {
            using FileStream stream = File.OpenRead(Name);
            using BinaryReader reader = new BinaryReader(stream);
            TgaImage tgaImage = new(reader);
            Image bitmap = tgaImage.GetImage();
            Width = bitmap.Width;
            Height = bitmap.Height;

            // Convert the bitmap to a uint array
            Data = new uint[bitmap.Width * bitmap.Height];
            int index = 0;

            for (int x = 0; x < bitmap.Width; x++) {
                for (int y = 0; y < bitmap.Height; y++) // Store by y to form contiguous data (columns)
                {
                    bitmap.GetPixelRgba(x, Height - y - 1, out int r, out int g, out int b, out int a); // Flipped y-axis from TGA to texture coordinates
                    Data[index++] = Func.EncodePixelColorRgba(r, g, b, a);
                }
            }
        }

        // Load all variants of this texture
        foreach (int variantNumber in GetVariants())
        {
            if (variantNumber < Variant.Count) continue; // Only load if not already loaded
            string variantFileName = $"{Path.GetFileNameWithoutExtension(Name)}_{variantNumber:0000}{Path.GetExtension(Name)}";
            using FileStream streamV = File.OpenRead(variantFileName);
            using BinaryReader reader = new BinaryReader(streamV);
            TgaImage tgaImage = new TgaImage(reader);
            Image bitmapV = tgaImage.GetImage();

            if (bitmapV.Width != Width || bitmapV.Height != Height) { throw new Exception($"Variant {variantNumber} has different size than original texture!"); }

            uint[] variantData = new uint[bitmapV.Width * bitmapV.Height];
            int variantIndex = 0;

            for (int x = 0; x < bitmapV.Width; x++) {
                for (int y = 0; y < bitmapV.Height; y++) // Store by y to form contiguous data for columns
                {
                    bitmapV.GetPixelRgba(x, Height - y - 1, out int r, out int g, out int b, out int a); // Flipped y-axis from TGA to texture coordinates
                    variantData[variantIndex++] = Func.EncodePixelColorRgba(r, g, b, a);
                }
            }

            Variant.Add(variantData);
        }
        
        Loaded = true;
    }
    
    public void PrepareMipMap(int width, int height, int variant = -1)
    {
        if (!Loaded) { LoadData(); }
        
        if (Width % width != 0 || Height % height != 0) { throw new Exception($"MipMap width and height must be a divisor of texture width and height!"); }
        if (Width / width != Height / height) { throw new Exception($"MipMap width and height must be a (single) divisor of texture width and height!"); }
        int d = Width / width;
        
        string key = $"{width}_{height}_{variant}";
        if (MipMap.ContainsKey(key)) { return; }
    
        uint[] data = Data;
        if (variant >= 0) { data = Variant[variant]; }
        
        // resample data into mipmap
        uint[] mipmap = new uint[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y =0; y < height; y++)
            {
                int r = 0; int g = 0; int b = 0;
                for (int dx = 0; dx < d; dx++)
                {
                    for (int dy = 0; dy < d; dy++)
                    {
                        uint pixel = data[(x*d + dx) * Height + y*d + dy];
                        Func.DecodePixelColor(pixel, out int rf, out int gf, out int bf);
                        r += rf; g += gf; b += bf;
                    }
                }
                uint filtered = Func.EncodePixelColor(r / (d * d), g / (d * d), b / (d * d));
                mipmap[x * height + y] = filtered;
            }
        }
        MipMap[key] = mipmap;
    }
    
    // Scale the texture to the specified dimensions
    public void Scale(int newWidth, int newHeight)
    {
        if (!Loaded) { LoadData(); } // Ensure the texture data is loaded
        if (Variant.Count > 0) { throw new Exception("Cannot scale texture with variants!"); }

        // Compute scaling ratios using fixed-point arithmetic (16.16 format)
        int xRatio = (Width << 16) / newWidth + 1;
        int yRatio = (Height << 16) / newHeight + 1;
        
        int[] sourceXs = new int[newWidth], sourceYs = new int[newHeight];
        for (int i = 0; i < newWidth; i++) { sourceXs[i] = Math.Clamp((i * xRatio) >> 16, 0, Width - 1); } // Precompute source X indices
        for (int j = 0; j < newHeight; j++) { sourceYs[j] = Math.Clamp((j * yRatio) >> 16, 0, Height - 1); } // Precompute source Y indices

        uint[] newData = new uint[newWidth * newHeight];
        for (int ix = 0; ix < newWidth; ++ix) // Loop over columns (x-axis)
        {
            int frameColumnIndex = ix * newHeight;
            int textureColumnIndex = sourceXs[ix] * Height;
            for (int j = 0; j < newHeight; ++j) // Loop over rows (y-axis) within the column
            {
                newData[frameColumnIndex + j] = Data[textureColumnIndex + sourceYs[j]];
            }
        }
        
        Data = newData;
        Width = newWidth;
        Height = newHeight;
    }
    
    // this will list all available variants of this texture by suffix number, e.g. texture.png -> texture_0001.png, texture_0002.png, etc.
    public IEnumerable<int> GetVariants()
    {
        string fileName = Path.GetFileNameWithoutExtension(Name);
        string extension = Path.GetExtension(Name);
        int variantNumber = 0;
        while (true)
        {
            string variantSuffixFormatted = variantNumber.ToString("0000");
            string variantFileName = $"{fileName}_{variantSuffixFormatted}{extension}";
            if (!File.Exists(variantFileName)) { yield break; }
            yield return variantNumber;
            variantNumber++;
        }
    }

    // get pixel color by coordinates, this is slow and used only for single pixel access (e.g. color picker)
    public uint GetPixel(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) { return 0; } 
        return Data[x * Height + y];
    }
}