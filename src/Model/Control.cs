namespace mode13hx.Model;

// Representation of a control (action which can be bound to a key or any other input)
public class Control
{
    public readonly ControlEnum Id;
    public bool Active;
    public int Delta;

    private Control(ControlEnum id)
    {
        Id = id;
        Active = false;
        Interlocked.Exchange(ref Delta, 0);
    }
    
    private static readonly Dictionary<ControlEnum, Control> Controls = new();
    
    // factory method for controls
    public static Control Create(ControlEnum id)
    {
        if (Controls.TryGetValue(id, out Control control)) return control;
        control = new Control(id);
        Controls.Add(id, control);
        return control;
    }
    
    public static void Reset() { foreach (Control control in Controls.Values) { control.Active = false; } }
}

// ReSharper disable InconsistentNaming
public enum ControlEnum
{
    VK_FORWARD = 100,
    VK_BACKWARD,
    VK_TURN_LEFT,
    VK_TURN_RIGHT,
    VK_USE,
    MOUSE_DELTA_X = 500,
    MOUSE_DELTA_Y,
    MOUSE_DELTA_WHEEL,
    MOUSE_BUTTON_LEFT,
    MOUSE_BUTTON_RIGHT,
}