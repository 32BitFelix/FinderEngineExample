


using OpenTK.Mathematics;
using Core.ECSS;
using Core.Shimshek;
using Core.Transformations;
using Core.InputManager;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;
using Core.Engine;
using OpenTK.Windowing.Common;
using System.Net.WebSockets;


namespace UserCore;

[Scene]
public static unsafe class Editor
{


    // Start marked methods
    // are called after the
    // creation of a scene
    [Start]
    public static void Start()
    {
        // Tell the window to disable
        // the cursor

        CursorModeValue* cM =
            (CursorModeValue*)NativeMemory.Alloc(sizeof(CursorModeValue));

        *cM = CursorModeValue.CursorDisabled;

        FinderEngine.ChangeWindowAttrib(WindowChangeClue.CursorMode, cM);


        // Tell the window to
        // make itself fullscreen

        WindowState* wS =
            (WindowState*)NativeMemory.Alloc(sizeof(WindowState));

        *wS = WindowState.Fullscreen;

        FinderEngine.ChangeWindowAttrib(WindowChangeClue.WindowMode, wS);


        // Change the mouse sensitivity

        Input.CursorSensitivity = 0.2f;


        // Add input setting

        Input.AddInput((int)Keys.Escape, InputPressType.continous, 0);


        // Create the cursor's texture

        cursorTex = Sprite.LoadTexture("Tex/Cursor.png");


        // Create the camera

        cam = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(cam, (1, 1, 1), (0, 0, 0), (0, 0, 0));

        ECSSHandler.AddComponent(cam, new Camera()
        {
            FOV = 90,

            ProjectionSize = 20,

            NearClip = 0.1f,

            FarClip = 100f,

            IsOrtho = true
        });


        // Create the cursor

        cursor = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cam, cursor);

        Gymbal.CreateTransform(cursor, (.5f, .5f, 1), (0, 0, 0), (0, 0, -1));

        ECSSHandler.AddComponent(cursor, new Sprite()
        {
            TextureObjectIndex = cursorTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 255
        });



        Resize();
    }


    // The texture the cursor
    // will be displayed with
    private static int cursorTex;


    // The entity to act
    // as the camera
    private static int cam;

    // The entity to act
    // as the cursor for
    // editing the level
    private static int cursor;


    // The Positive bounds of
    // the camera's frustum,
    // relative to the camera's
    // position
    private static Vector2 bound;

    
    // Update marked methods
    // are called every frame
    [Update]
    public static void Update()
    {
        if(Input.IsInput(0))
            FinderEngine.ChangeWindowAttrib(WindowChangeClue.WindowClose, null);


        cursorMove();


        
    }

        // The method responsible
        // for the movement
        // of the cursor
        private static void cursorMove()
        {
            // Move the cursor by the
            // delta of the mouse

            Translation* curTran = ECSSHandler.GetComponent<Translation>(cursor);

            curTran->Translations.Xy += Input.CursorPositionDelta * Input.CursorSensitivity;


            // Clamp the position of the
            // cursor to the frustum of
            // the camera

            curTran->Translations.Xy = Vector2.Clamp(curTran->Translations.Xy, -bound, bound);
        }


    // Resize marked methods
    // are called at every
    // resizing of the window
    [Resize]
    public static void Resize()
    {

        // Calculate the positive bounds
        // of the camera's frustum

        Camera* c = ECSSHandler.GetComponent<Camera>(cam);

        bound = new Vector2(c->ProjectionSize * c->AspectRatio, c->ProjectionSize) * 0.5f;


    }
}