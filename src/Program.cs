using CommandLine;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using mode13hx.Configuration;
using mode13hx.Presentation;

namespace mode13hx;

public static class Program
{
    public static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<TestOptions, BlankOptions>(args) // https://github.com/commandlineparser/commandline
            .MapResult(
                (TestOptions opts) => RunWithOptions(opts, new TestRasterizer(opts)),
                (BlankOptions opts) => RunWithOptions(opts, new BlankRasterizer(opts)),
                errs => 1);
    }

    private static int RunWithOptions<T>(T config, IRasterizer ras) where T : CommonOptions
    {
        NativeWindowSettings nativeWindowSettings = new()
        {
            Title = "mode13hx",
            WindowState = config.Fullscreen?WindowState.Fullscreen:WindowState.Normal,
            Vsync = config.VSync?VSyncMode.On:VSyncMode.Off,
            ClientSize = new Vector2i(config.Width, config.Height),
            Flags = ContextFlags.ForwardCompatible, // This is needed to run on macos
        };
        
        using EngineWindow window = new(config, ras, GameWindowSettings.Default, nativeWindowSettings);
        window.Run();
        return 0;
    }
}
