using System.Numerics;
using mode13hx.Util;

namespace mode13hx.Controls;

public class SlideBar<T> : UiControlBase where T : INumber<T>
{
    public T Value { get; private set; }
    private double position;
    private readonly int defaultMarkerX;
    private const int Margin = 5;
    private readonly uint barColor;
    private readonly T min;
    private readonly T max;
    
    public Action<T> OnActivate { get; set; }
    
    public SlideBar(int x, int y, int width, int height, uint color, T min, T max, T value) : base(x, y)
    {
        this.Width = width;
        this.Height = height;
        this.barColor = color;
        this.min = min;
        this.max = max;
        this.Value = value;
        position = Convert.ToDouble((value - min)) / Convert.ToDouble((max - min));
        defaultMarkerX = X + Margin + (int)(Convert.ToDouble(value - min) * (Width - 2 * Margin) / Convert.ToDouble(max - min));
    }

    public override void Draw(Canvas canvas, bool active = false)
    {
        canvas.SetPenColor(Func.EncodePixelColor(0xFF, 0xFF, 0xFF)).Rectangle(X, Y, Width, Height);
        canvas.SetPenColor(barColor).Rectangle(X + Margin, Y + Margin, (int)(position * (Width - 2 * Margin)), Height - 2 * Margin);
        canvas.SetPenColor(Func.EncodePixelColor(0xFF, 0x00, 0x00)).MoveTo(defaultMarkerX, Y + Margin).Line(0, Height - 2 * Margin);
        if (active) { canvas.SetPenColor(0xFFFFFF).DrawString(ToolTip, X + 2*Margin, Y - 2*Margin - 16, Canvas.Font9X16); }
    }
    
    public override bool ActiveArea(int x, int y) { return x >= X && x < X + Width && y >= Y && y < Y + Height; }

    public override bool Click(int mx, int my)
    {
        if (!ActiveArea(mx, my)) return false;
        position = (mx-X)*1.0/Width;
        Value = T.CreateChecked(min) + T.CreateChecked(T.CreateChecked(max - min) * T.CreateChecked(position));
        OnActivate?.Invoke(Value); return true;
    }
}
