

using System.Runtime.InteropServices;
using Core.Engine;
using Core.MemoryManagement;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Core.InputManager;


// TODO: Pack input type, input bind and inputness into a struct

// All in one abtraction
// for GLFW's input
public static unsafe class Input
{
    // static constructor
    static Input()
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

        lastCursorPos = new Vector2((float)x, (float)y);


        // Initialise the arrays
        areInput = new NA<bool>();

        inputBinds = new NA<int>();

        inputTypes = new NA<byte>();


        // Set the default of the
        // cursor sensitivity
        CursorSensitivity = 0.05f;
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


    // The array to hold the indicators
    // of if the keys have been pressed
    private static NA<bool> areInput;

    // The array to hold the kind of bind
    // the input in the current index has
    private static NA<int> inputBinds;

    // The array of the type of inputs
    // the keys have
    private static NA<byte> inputTypes;

    // Creates an input on the given index,
    // bind and input type
    public static void AddInput(int bindID, InputPressType inputType, int index)
    {
        // Get
        fixed(NA<int>* iBPtr = &inputBinds)
        //
        fixed(NA<byte>* iTPtr = &inputTypes)
        //
        fixed(NA<bool>* aIPtr = &areInput)
        {
            NAHandler.Set(index, bindID, iBPtr);
            //
            NAHandler.Set(index, (byte)inputType, iTPtr);
            //
            NAHandler.Set(index, false, aIPtr);
        }
    }

    public static bool IsInput(int index)
        => areInput.Values[index];

    public static int GetBindID(int index)
        => inputBinds.Values[index];

    public static int SetBindID(int index, int bind)
        => inputBinds.Values[index] = bind;

    public static InputPressType GetInputType(int index)
        => (InputPressType)inputTypes.Values[index];

    public static void SetInputType(int index, InputPressType type)
        => inputTypes.Values[index] = (byte)type;


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


    private static void MouseWheelCallback(Window* window, double offsetX, double offsetY)
        => MouseWheelDelta = (float)offsetY;


    private static void MouseButtonCallback(Window* window, MouseButton button, InputAction action, KeyModifiers mods)
    {
        // Get
        fixed(NA<int>* iBPtr = &inputBinds)
        //
        fixed(NA<byte>* iTPtr = &inputTypes)
        //
        fixed(NA<bool>* aIPtr = &areInput)
        {

            switch(action)
            {
                case InputAction.Release:
                    for(int i = 0; i < areInput.Length; i++)
                    {
                        if((int)button != NAHandler.Get(i, iBPtr))
                            continue;

                        switch(NAHandler.Get(i, iTPtr))
                        {

                            case (int)InputPressType.continous:
                                NAHandler.Set(i, false, aIPtr);
                            return;

                            case (int)InputPressType.direct:

                            return;

                            case (int)InputPressType.toggle:

                            return;

                            case (int)InputPressType.directRelease:
                                NAHandler.Set(i, true, aIPtr);
                            return;
                        }
                    }
                return;

                case InputAction.Press:
                    for(int i = 0; i < areInput.Length; i++)
                    {   
                        if((int)button != NAHandler.Get(i, iBPtr))
                            continue;

                        switch(NAHandler.Get(i, iTPtr))
                        {

                            case (int)InputPressType.continous:
                                if(!NAHandler.Get(i, aIPtr))
                                    NAHandler.Set(i, true, aIPtr);
                            return;

                            case (int)InputPressType.direct:
                                    NAHandler.Set(i, true, aIPtr); 
                            return;

                            case (int)InputPressType.toggle:
                                NAHandler.Set(i, !NAHandler.Get(i, aIPtr), aIPtr);
                            return;

                            case (int)InputPressType.directRelease:

                            return;
                        }
                    }
                return;

                /*case InputAction.Repeat:
                    // Don't know what to do with this. Keeping it
                    // if demand needs it.
                return;*/
            }
        }


    }

    private static void KeyCallback(Window* window, Keys key, int scanCode, InputAction action, KeyModifiers mods)
    {
        // Get
        fixed(NA<int>* iBPtr = &inputBinds)
        //
        fixed(NA<byte>* iTPtr = &inputTypes)
        //
        fixed(NA<bool>* aIPtr = &areInput)
        {

            switch(action)
            {
                case InputAction.Release:
                    for(int i = 0; i < areInput.Length; i++)
                    {
                        if((int)key != NAHandler.Get(i, iBPtr))
                            continue;

                        switch(NAHandler.Get(i, iTPtr))
                        {

                            case (int)InputPressType.continous:
                                NAHandler.Set(i, false, aIPtr);
                            return;

                            case (int)InputPressType.direct:

                            return;

                            case (int)InputPressType.toggle:

                            return;

                            case (int)InputPressType.directRelease:
                                NAHandler.Set(i, true, aIPtr);
                            return;
                        }
                    }
                return;

                case InputAction.Press:
                    for(int i = 0; i < areInput.Length; i++)
                    {   
                        if((int)key != NAHandler.Get(i, iBPtr))
                            continue;

                        switch(NAHandler.Get(i, iTPtr))
                        {

                            case (int)InputPressType.continous:
                                if(!NAHandler.Get(i, aIPtr))
                                    NAHandler.Set(i, true, aIPtr);
                            return;

                            case (int)InputPressType.direct:
                                NAHandler.Set(i, true, aIPtr); 
                            return;

                            case (int)InputPressType.toggle:
                                NAHandler.Set(i, !NAHandler.Get(i, aIPtr), aIPtr);
                            return;

                            case (int)InputPressType.directRelease:

                            return;
                        }
                    }
                return;

                /*case InputAction.Repeat:
                    // Don't know what to do with this. Keeping it
                    // if demand needs it.
                return;*/
            }
        }


    }



    public static void ResetDirect()
    {
        // Direct key handle
        fixed(NA<byte>* pTPtr = &inputTypes)
        fixed(NA<bool>* aIPtr = &areInput)
        {
            for(int i = 0; i < areInput.Length; i++)
            {
                if(NAHandler.Get(i, pTPtr) == (int)InputPressType.direct ||
                    NAHandler.Get(i, pTPtr) == (int)InputPressType.directRelease)   
                NAHandler.Set(i, false, aIPtr);
            }
        }


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

// An enumerator for indicating
// how the manage is supposed to
// interpret the input
public enum InputPressType : byte
{

    // Positive as long as
    // the bind is held
    continous = 0,

    // Positive as long as
    // the bind is pressed
    direct = 1,

    // Switches between positive
    // and negative with each press
    toggle = 2,


    directRelease = 3,
}