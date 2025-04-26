

using System.Runtime.InteropServices;

using Core.Engine;
using Core.MemoryManagement;

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Core.FinderIO;

public unsafe class KBMInput
{
    // type initializer
    static KBMInput()
    {
        // Set the keyboard key callback
        delegate*<Window*, Keys, int, InputAction, KeyModifiers, void> k = &KeyCallback;

        kCall = Marshal.GetDelegateForFunctionPointer<GLFWCallbacks.KeyCallback>((nint)k);

        GLFW.SetKeyCallback(FinderEngine.window, kCall);


        // Set the mouse button callback
        delegate*<Window*, MouseButton, InputAction, KeyModifiers, void> mb = &MouseButtonCallback;

        mbCall = Marshal.GetDelegateForFunctionPointer<GLFWCallbacks.MouseButtonCallback>((nint)mb);

        GLFW.SetMouseButtonCallback(FinderEngine.window, mbCall);


        // Set the mousewheel callback
        delegate*<Window*, double, double, void> s = &MouseWheelCallback;

        sCall = Marshal.GetDelegateForFunctionPointer<GLFWCallbacks.ScrollCallback>((nint)s);

        GLFW.SetScrollCallback(FinderEngine.window, sCall);


        // Set the last position of the cursor
        GLFW.GetCursorPos(FinderEngine.window, out double x, out double y);

        // Set the last position
        // of the cursor
        lastCursorPos = new Vector2((float)x, (float)y);


        // Check, if raw mouse input
        // is supported on this device
        if(GLFW.RawMouseMotionSupported())
            // Activate it, in case
            // it is deactivated
            GLFW.SetInputMode(FinderEngine.window, RawMouseMotionAttribute.RawMouseMotion, true);


        // Set the default
        // cursor sensitivity
        CursorSensitivity = 0.2f;


        // Allocate an array to store
        // the input of all currently
        // known keys
        allInput = SmartPointer.CreateSmartPointer<InputTracker>(349);

        // Iterate through each element
        // of the allInput array
        for(int i = SmartPointer.GetSmartPointerLength(allInput) - 1; i > -1; i--)
            // Default the current
            // element
            allInput[i] = default;
    }


    // Managed callback to
    // keyboard callback
    private static GLFWCallbacks.KeyCallback kCall;

    // Managed callback to
    // mouse button callback
    private static GLFWCallbacks.MouseButtonCallback mbCall;

    // Manged callback to
    // mouse wheel callback
    private static GLFWCallbacks.ScrollCallback sCall;


    // A smart pointer, that keeps
    // track, if a key or mousebutton
    // has been pressed
    private static InputTracker* allInput;

    // Get the input's state
    // of the current frame
    public static bool IsPressed(int inputID)
        => allInput[inputID].IsInputDirect;

    // Get the input's
    // contionous state
    public static bool IsHeld(int inputID)
        => allInput[inputID].IsInputContinous;

    
    // Stores the ID of the
    // last pressed key
    private static int lastInput;

    // A simple accessor
    // for the last input
    public static int GetLastInput()
        => lastInput;


    // The delta of the mousewheel in the y dimension
    public static float MouseWheelDelta { get; private set; }

    // The Cursor position delta
    // of the cursor in the current
    // frame
    public static Vector2 CursorPositionDelta {get; private set;}

    // The position of the cursor
    // in the last frame
    private static Vector2 lastCursorPos;

    // The sensitivity of the cursor
    public static float CursorSensitivity;



    private static void KeyCallback(Window* window, Keys key, int scanCode, InputAction action, KeyModifiers mods)
    {
        if(action == InputAction.Repeat)
            return;


        lastInput = (int)key;


        bool isPressed = action == InputAction.Press;

        allInput[(int)key].IsInputContinous = isPressed;

        allInput[(int)key].IsInputDirect = isPressed;
    }


    private static void MouseButtonCallback(Window* window, MouseButton button, InputAction action, KeyModifiers mods)
    {
        if(action == InputAction.Repeat)
            return;


        lastInput = (int)button;


        bool isPressed = action == InputAction.Press;

        allInput[(int)button].IsInputContinous = isPressed;

        allInput[(int)button].IsInputDirect = isPressed;
    }


    private static void MouseWheelCallback(Window* window, double offsetX, double offsetY)
        => MouseWheelDelta = (float)offsetY;


    public static void Update()
    {
        // Iterate through each element
        // in the allInput array
        for(int i = SmartPointer.GetSmartPointerLength(allInput) - 1; i > -1; i--)
            // Set the direct
            // input to zero
            allInput[i].IsInputDirect = false;


        // Cursor position delta handle
        GLFW.GetCursorPos(FinderEngine.window, out double x, out double y);

        Vector2 cursorPos = new Vector2((float)x, -(float)y);


        Vector2 delta = cursorPos - lastCursorPos;


        lastCursorPos = cursorPos;

        CursorPositionDelta = delta * CursorSensitivity;


        // Mouse wheel delta handle
        MouseWheelDelta = 0;
    }
}

// A struct that keeps
// track of the input
// of a key
public unsafe struct InputTracker
{
    // The input stroke
    // that spans multiple
    // frames
    public bool IsInputContinous;

    // The input stroke
    // that spans a single
    // frame
    public bool IsInputDirect;
}