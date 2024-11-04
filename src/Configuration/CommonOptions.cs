using CommandLine;
using mode13hx.Model;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace mode13hx.Configuration;

public class CommonOptions
{
    [Option('w', "width", Default = 1920, HelpText = "Set the window width.")]
    public int Width { get; set; }

    [Option('h', "height", Default = 1080, HelpText = "Set the window height.")]
    public int Height { get; set; }

    [Option('f', "fullscreen", Default = false, HelpText = "Set fullscreen mode.")]
    public bool Fullscreen { get; set; }

    [Option('v', "vsync", Default = false, HelpText = "Enable or disable VSync.")]
    public bool VSync { get; set; }

    [Option('d', "dofps", Default = false, HelpText = "Toggle display of frames per second.")]
    public bool Dofps { get; set; } = true;
    
    [Option('x', "rx", Default = 583, HelpText = "Record the screen to a video file. X offset of the recording.")]
    public int Rx { get; set; }
    
    [Option('y', "ry", Default = 55, HelpText = "Record the screen to a video file. Y offset of the recording.")]
    public int Ry { get; set; }
    
    [Option("rw", Default = 100, HelpText = "Record the screen to a video file. Width of the recording.")]
    public int Rw { get; set; }
    
    [Option("rh", Default = 100, HelpText = "Record the screen to a video file. Height of the recording.")]
    public int Rh { get; set; }
    
    [Option("rfps", Default = 30, HelpText = "Record the screen to a video file. FPS of the recording.")]
    public int Rfps { get; set; }
    
    [Option("rframes", Default = 150, HelpText = "Record the screen to a video file. How many frames to record.")]
    public int Rframes { get; set; }

    // higher value could increase performance and decrease input lag but will increase system load and number of dropped frames 
    [Option('l', "frames", Default = 1, HelpText = "Set the frame prerender limit. Minimum reasonable value is 1, to have at least 1 frame ready and another one being rendered.")]
    public int FramesPrerenderLimit { get; set; }
    
    public Dictionary<Keys, Control> KbControls { get; } = new()
    {
        {Keys.W, Control.Create(ControlEnum.VK_FORWARD)},
        {Keys.S, Control.Create(ControlEnum.VK_BACKWARD)},
        {Keys.A, Control.Create(ControlEnum.VK_TURN_LEFT)},
        {Keys.D, Control.Create(ControlEnum.VK_TURN_RIGHT)},
        {Keys.Space, Control.Create(ControlEnum.VK_USE)},
    };
     
    public Dictionary<ControlEnum, Control> MouseControls { get; } = new()
    {
        {ControlEnum.MOUSE_DELTA_X, Control.Create(ControlEnum.MOUSE_DELTA_X)},
        {ControlEnum.MOUSE_DELTA_Y, Control.Create(ControlEnum.MOUSE_DELTA_Y)},
        {ControlEnum.MOUSE_DELTA_WHEEL, Control.Create(ControlEnum.MOUSE_DELTA_WHEEL)},
        {ControlEnum.MOUSE_BUTTON_LEFT, Control.Create(ControlEnum.VK_USE)},
        {ControlEnum.MOUSE_BUTTON_RIGHT, Control.Create(ControlEnum.MOUSE_BUTTON_RIGHT)}
    };
}
