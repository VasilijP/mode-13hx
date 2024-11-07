# mode-13hx

~~~sh
// run test (bouncing box) with triple buffer (-l 2), fullscreen (-f), framechart (-d) in 4k
// it will downscale(or upscale) to any display resolution
dotnet mode13hx.dll test -f -d -l 2 -w 3840 -h 2160

// run blank rasterizer in a fullscreen (-f), V-Sync (-v) in a default (1920x1080) resolution with double buffer
dotnet mode13hx.dll blank -f -v
~~~

Feeling nostalgic for Mode 13h? Try out Mode 13HX - a modern alternative inspired by the classic graphics mode, built with [OpenTK](https://opentk.net/index.html) and targeting **.NET 8**.

This lightweight framework provides direct pixel access and simple APIs for 2D/3D graphics, supporting both **Windows** and **Linux**. Whether you're creating retro-style games or experimenting with low-level graphics, Mode 13HX offers the ease of use of the past with the power of today.

- **Resolution**: 4k, 1920x1080 (or 1280x720) vs. 320x200.
- **Color Depth**: 24-bit RGB (True Color) vs. 8-bit (256-color palette).
- **Hardware Access**: Direct pixel manipulation with optional hardware acceleration, no reliance on manual palette-based effects.
- **Memory Layout**: Larger linear frame buffer with direct access.
- **Ease of Development**: Simpler input/output APIs and cross-platform support.

This is a good base for:

- Retro-style indie games, educational graphics programming: It could be a great tool for teaching students how to program basic graphics and game development, much like how Mode 13h was a starting point for many programmers in the 90s.
- Prototyping: fast, low-overhead prototyping for graphical applications, quick experimentation with pixel-level graphics, transitions, or 2D/3D mechanics.
- Creative Coding and Demos: For the demo scene or creative coding enthusiasts, mix of simplicity and direct control for pushing boundaries with visual effects, procedural generation, and interactive art.

## History and Motivation

Mode 13h is a standard video graphics mode from the VGA (Video Graphics Array) specification. It was widely used in the late 1980s and early 1990s for video games, demos, and other graphical software on IBM-compatible PCs. It is known for its simplicity and ease of use for graphics programming, especially in the DOS era. Here are its main characteristics and benefits:

- **Resolution**: Mode 13h operates at a resolution of **320x200 pixels**.
- **Color Depth**: It supports **256 colors** from a palette of 262,144 (18-bit RGB), making it one of the first standard modes to allow for a large number of colors on the screen at once.
- **Aspect Ratio**: The 320x200 resolution has a 16:10 aspect ratio, which approximates a 4:3 aspect ratio when displayed on typical CRT monitors of the time.
- **Memory Layout**: The video memory is mapped linearly, making pixel access straightforward. Each pixel corresponds directly to one byte in video memory, and the entire 64KB of video memory is accessible at once (which was a big deal with a [16bit memory model](https://devblogs.microsoft.com/oldnewthing/20200728-00/?p=104012) in MS-DOS ).
- **Direct Memory Access**: The video memory begins at address `0xA0000`, allowing programmers to directly access and manipulate pixel data in RAM.
- **No Hardware Acceleration**: Mode 13h does not include any hardware acceleration features like modern graphics modes, so all graphics operations (drawing lines, circles, bitmaps, etc.) must be done manually in software.

Main benefits of this mode (some of which are desirable to this day) were:

1. **Ease of Programming**: The linear memory model and the direct mapping of pixels to memory made it very easy to program. A single byte in video memory represented one pixel, which simplified graphics programming for developers.
2. **Good Performance for the Time**: Given that Mode 13h used 256 colors and had a low resolution, it was not memory-intensive. Its simplicity allowed developers to focus on efficient software rendering techniques, resulting in relatively good performance for games and graphical applications on the hardware available at the time.
3. **Versatility**: The 256-color palette was large enough to support detailed and colorful graphics, which was especially useful for video games. It allowed developers to switch between colors dynamically and implement special effects like palette animations.
4. **Widespread Support**: Since it was part of the VGA standard, Mode 13h was supported by nearly every IBM-compatible PC with a VGA card, making it a universal mode that could be relied upon across systems.
5. **Popular in Games**: Many popular DOS games, including *DOOM*, *Commander Keen*, and *Wolfenstein 3D*, were developed using Mode 13h, which solidified its legacy as a key graphics mode for game development.

Mode 13h, though limited by today's standards, was a critical mode during the early PC gaming and graphical software era due to its simplicity, ease of access, and reasonable performance.

## Mode "13hx"

How would the 13h video mode look today (if we were [still / again] manipulating pixels by the CPU)?

It would probably allow high resolution like **1920x1080 (Full HD)** or **1280x720 (HD)**. This would be a huge leap from 320x200 but allows for immersive detail while still pixelated on todays 4k+ screens.
**Color Depth** would be at least **True Color (24-bit RGB)** instead of an 8-bit palette, which gives access to **16.7 million colors**. This eliminates the need for a palette, making the mode more intuitive for modern programmers while allowing for smooth gradients, richer textures, and photorealism.
**Memory Layout** would still offer a **linear frame buffer**, but now much larger. With a resolution of 1920x1080 at 24-bit color depth, each pixel would take 3 bytes (RGB), resulting in around 6 MB of frame buffer space. Direct access to this memory, similar to the original Mode 13h, would make pixel manipulation straightforward for programmers. **Performance Efficiency** is high enough to allow for **direct buffer updates**, where the entire buffer is flushed to the screen without complex shaders or layers of abstraction. Support for a **60/120/144/+ FPS cap** or **V-Sync** by default to prevent excessive CPU/GPU usage, but allow higher frame rates for more advanced or time-critical applications.

If You are searching for such mode as described above :arrow_up_small:, which would work with C# and .Net 8, you may want to clone this repo!

Additional features which would be nice to have (not necessarily provided or planned by this project) are:

1. **Hardware-Accelerated Option** (like accelerated Canvas): blitting, line drawing, or image scaling—without getting too complex like modern GPU APIs.

2. **Input/Output Simplicity**: simplified APIs for input handling. For example, direct keyboard and mouse input support without requiring complex event handling like in modern frameworks. This is already provided by OpenTK, good question is whether this could be even more streamlined somehow.

3. **Cross-platform support** out of the box (Windows, Linux, macOS) with minimal dependencies. Currently I tested this using Win10 and Kubuntu.

4. **Sprite Handling**: Easy-to-use APIs for handling **sprites** (2D images) with transparency, blending, and scaling.

5. **Basic 3D**: Basic support for **3D rendering** with simple polygons, retaining the spirit of Mode 13h's use in early 3D games but making it easier to implement basic 3D visuals without requiring complex GPU knowledge.

6. **Easy Integration with Modern Sound**: Built-in support for sound and music (e.g., modern sound APIs like OpenAL or SDL audio) that allows for easy playback of audio without requiring complex setup.

7. **VRAM Size and Support for Larger Textures**: While the original Mode 13h had limited memory (64KB), "mode 13hx" could easily support **multiple textures or sprite sheets**, enabling developers to load and manipulate large textures for richer visual content.

8. **Post-Processing Effects**: While Mode 13h had cool palette tricks, modern "Mode 13h 2024" could support simple **post-processing effects** like bloom, color grading, or blur with minimal overhead.

9. **Network Capabilities**: Simplified support for **networking** (e.g., basic TCP/UDP) to allow for multiplayer or online experiences without requiring developers to dive into complex networking code.

   

   
