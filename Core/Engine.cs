
using System.Reflection;
using System.Runtime.InteropServices;
using Core.InputManager;
using Core.MemoryManagement;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Monitor = OpenTK.Windowing.GraphicsLibraryFramework.Monitor;

namespace Core.Engine;

public static unsafe class FinderEngine
{
    // Some important starting
    // stuffs for the window
    static FinderEngine()
    {
        // Initialise the
        // size of the window
        windowSize = (800, 600);


        // Allocate the array
        // to hold the title
        // of the window
        title =
            (byte*)NativeMemory.Alloc(sizeof(byte) * 7);

        title[0] = (byte)'S';

        title[1] = (byte)'A';

        title[2] = (byte)'M';

        title[3] = (byte)'P';

        title[4] = (byte)'L';

        title[5] = (byte)'E';

        title[6] = (byte)'\0';


        // Initialise GLFW
        if(!GLFW.Init())
        {


            return;
        }

        // Set the major version
        GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 3);

        // Set the minor version
        GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 3);

        // Hinting that the client's api is OpenGL
        GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);

        // Hinting that the GL to use is the core version
        GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);


        GLFW.WindowHint(WindowHintInt.Samples, 4);


        GLFW.WindowHint(WindowHintBool.Focused, true);


        currentCursorMode = CursorModeValue.CursorNormal;


        // Create a window
        window = GLFW.CreateWindowRaw(windowSize.X, windowSize.Y, title, null, null);


        GLFW.SetWindowSize(window, windowSize.X, windowSize.Y);


        // Check if the making of the window failed
        if(window == (Window*)null)
        {
            GLFW.Terminate();

            return;
        }


        monitor = GLFW.GetPrimaryMonitor();


        delegate*<Window*, int, int, void> r = &OnResize;

        rCall = Marshal.GetDelegateForFunctionPointer<GLFWCallbacks.WindowSizeCallback>((nint)r);

        GLFW.SetWindowSizeCallback(window, rCall);


        // Set the window
        // as current context
        GLFW.MakeContextCurrent(window);


        // Set the vertical synchronisation mode
        // zero is off, one is on
        GLFW.SwapInterval(0);


        // Load a glfw bindings context
        glfwContext = new GLFWBindingsContext();


        // Load opengl bindings
        GL.LoadBindings(glfwContext);


        // Set the clear color of
        // the backbuffer
        GL.ClearColor(1, 1, 1, 1);   


        GL.Enable(EnableCap.Blend);

        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);  


        OnResize((Window*)0, windowSize.X, windowSize.Y);

        // Start the resource loader
        //new FinderEngie.ResourceLoader.Loader();
    }


    // Bindingscontext to
    // glfw and opengl
    private static GLFWBindingsContext? glfwContext;


    // Pointer reference
    // to the current
    // window
    public static Window* window;

    // Pointer reference
    // to the current
    // monitor
    public static Monitor* monitor;


    // Cache of the size of the
    // window
    private static Vector2i windowSize;

    // Only returns the
    // size of the window
    public static Vector2 GetWindowSize
        => windowSize;


    // Cache of the refresh rate
    private static int refreshRate;


    // The currently set cursor mode
    private static CursorModeValue currentCursorMode;


    // Cache of the window's title
    private static byte* title;


    // The last set state of the window
    private static WindowState previousWindowState;



    // The starting point
    // of the engine
    public static void Start()
    {
        // Some starting preparations
        
        Loop();
    }

    // The main loop
    // of the engine
    private static void Loop()
    {
        while(!GLFW.WindowShouldClose(window))
        {
            GLFW.PollEvents();

            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit |
                ClearBufferMask.StencilBufferBit);


            // Loop stuff

            ECSS.ECSSHandler.GlobalDeltaTime = (float)GLFW.GetTime();

            GLFW.SetTime(0);

            ECSS.ECSSHandler.ECSSUpdate();

            ECSS.ECSSHandler.ECSSFixedUpdate();

            ECSS.ECSSHandler.ECSSRender();


            GLFW.SwapBuffers(window);

            Input.ResetDirect();


            processWindowChanges();
        }

        ECSS.ECSSHandler.ECSSEnd();
    }


    // Delegate to
    // the onresize method
    private static GLFWCallbacks.WindowSizeCallback? rCall;

    // Called whenever the window
    // resizes
    private static void OnResize(Window* wPtr, int width, int height)
    {
        // Set OpenGL's
        // viewport
        GL.Viewport(0, 0, width, height);

        // Set the new
        // windowsize
        windowSize = (width, height);

        // Call the resize
        // of the ECSSHandler
        ECSS.ECSSHandler.ECSSResize(width, height);
    }

    // The array to hold the requested
    // changes to the window
    private static NA<WindowChange> changes;

    // Processes window changes,
    // if there are any
    private static void processWindowChanges()
    {


        // Iterate through each
        // window change in the
        // changes array
        for(int i = 0; i < changes.Length; i++)
        {

            // If the current iteration
            // has no clue, it is assumed
            // that the following clues
            // have nothing meaningful,
            // which leads to a premature
            // end to the loop

            if(changes.Values[i].clue == WindowChangeClue.None)
                break;


            // The engine requested a change
            // in the size of the window

            if(changes.Values[i].clue == WindowChangeClue.WindowSize)
            {
                // Cache the window size
                windowSize = *(Vector2i*)changes.Values[i].value;

                // Set the windowsize for real
                GLFW.SetWindowSize(window, windowSize.X, windowSize.Y);

                // Set the window change clue
                // to default
                changes.Values[i].clue = WindowChangeClue.None;

                // Free the value
                NativeMemory.Free(changes.Values[i].value);


                continue;
            }


            // The engine requested a change
            // in the refreshrate of the
            // IO polling

            if(changes.Values[i].clue == WindowChangeClue.RefreshRate)
            {
                // Cache the refresh rate
                refreshRate = *(int*)changes.Values[i].value;

                // Get video mode info
                VideoMode* mode = GLFW.GetVideoMode(monitor);

                // Set the refresh rate
                // of the monitor
                GLFW.SetWindowMonitor(window, monitor, 0, 0, mode->Width, mode->Height, refreshRate);

                // Set the window change clue
                // to default
                changes.Values[i].clue = WindowChangeClue.None;

                // Free the value
                NativeMemory.Free(changes.Values[i].value);


                continue;
            }


            // The engine requested a change
            // in the mode of the cursor

            if(changes.Values[i].clue == WindowChangeClue.CursorMode)
            {
                // Cache the cursor mode
                currentCursorMode = *(CursorModeValue*)changes.Values[i].value;

                // Set the cursor mode for real
                GLFW.SetInputMode(window, CursorStateAttribute.Cursor, currentCursorMode);

                // Set the window change clue
                // to default
                changes.Values[i].clue = WindowChangeClue.None;

                // Free the value
                NativeMemory.Free(changes.Values[i].value);


                continue;
            }


            // The engine requested a change
            // in the window's mode

            if(changes.Values[i].clue == WindowChangeClue.WindowMode)
            {
                // If the new window state is
                // not fullscreen...
                if(previousWindowState == WindowState.Fullscreen && *(WindowState*)changes.Values[i].value != WindowState.Fullscreen)
                    // Set the window stuff
                    GLFW.SetWindowMonitor(window, null, 0, 0, windowSize.X, windowSize.Y, 0);

                // Check the window state...
                switch(*(WindowState*)changes.Values[i].value)
                {

                    // If the new state is normal...
                    case WindowState.Normal:
                         // Restore the window state
                        GLFW.RestoreWindow(window);

                        break;

                    // If the new state is minimize...
                    case WindowState.Minimized:
                        // Iconify the window
                        GLFW.IconifyWindow(window);

                        break;

                    // If the state is maximized...
                    case WindowState.Maximized:
                        // Maximize the window
                        GLFW.MaximizeWindow(window);

                        break;

                    // If the new state is fullscreen...
                    case WindowState.Fullscreen:
                        // Get the video mode of
                        // the current window
                        VideoMode* nMode = GLFW.GetVideoMode(monitor);

                        // Set the window to
                        // fullscreenm while
                        // preserving the
                        // previous information
                        GLFW.SetWindowMonitor(window, monitor, 0, 0, nMode->Width, nMode->Height, nMode->RefreshRate);

                        break;
                }

                // Cache the new window state
                previousWindowState = *(WindowState*)changes.Values[i].value;

                // Set the window change clue
                // to default
                changes.Values[i].clue = WindowChangeClue.None;

                // Free the value
                NativeMemory.Free(changes.Values[i].value);


                continue;
            }


            // The engine requested a change
            // in the window's title

            if(changes.Values[i].clue == WindowChangeClue.WindowTitle)
            {
                title = (byte*)changes.Values[i].value;

                GLFW.SetWindowTitleRaw(window, title);

                // Set the window change clue
                // to default
                changes.Values[i].clue = WindowChangeClue.None;

                // Free the value
                NativeMemory.Free(changes.Values[i].value);


                continue;
            }


            // The engine requested to
            // end the window

            if(changes.Values[i].clue == WindowChangeClue.WindowClose)
            {
                // Tell glfw to close the window
                GLFW.SetWindowShouldClose(window, true);

                // Set the window change clue
                // to default
                changes.Values[i].clue = WindowChangeClue.None;

                
                continue;
            }
        }
    }

    // Adds a request to the window change
    public static void ChangeWindowAttrib(WindowChangeClue clue, void* value)
    {
        // Create an index cache
        int nIndex = changes.Length;

        // Iterate through all
        // elements in the changes array
        for(int i = 0; i < changes.Length; i++)
        {
            // If current clue is not none...
            if(changes.Values[i].clue != WindowChangeClue.None)
                // continue to next iteration
                continue;

            // Overwrite
            // the fallback index
            // with the current
            // index
            nIndex = i;

            // Stop the loop
            break;
        }

        // Add the new clue
        // to the changes array

        fixed(NA<WindowChange>* cPtr = &changes)
            NAHandler.Set(nIndex, new WindowChange(){clue = clue, value = value}, cPtr);
    }


}

// A struct to hold
// one window change
// request
public unsafe struct WindowChange
{
    // The type of the request
    public WindowChangeClue clue;

    // The value associated
    // with the request
    public void* value;
}

// Clues for when the window changes
public enum WindowChangeClue : byte
{
    // No request
    None = 0,

    // Window size request
    WindowSize = 2,

    // Refreshrate request
    RefreshRate = 3,

    // Cursormode request
    CursorMode = 4,

    // WindowMode request
    WindowMode = 5,

    // Window title request
    WindowTitle = 6,

    // Window close request
    WindowClose = 7,
}