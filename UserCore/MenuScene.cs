
using Core.ECSS;
using Core.Engine;
using Core.FinderIO;
using Core.InputManager;
using Core.SAS2D;
using Core.Shimshek;
using Core.TonKlang;
using Core.Transformations;
using Core.UISystem;

using Cursor = Core.UISystem.Cursor;

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using UserCore;

namespace UserCode.Menu;

[Scene]
public static unsafe class MenuScene
{   
    public static int cam;

    private static int cursor, cursorReactBox;

    private static int titleCard;

    private static int playButton, exitButton, mouseButton;


    private static int cursorTex;

    private static int titleCardTex;


    public static int playNormTex, playHoverTex;

    public static int exitNormTex, exitHoverTex;

    public static int mouseNormTex, mouseHoverTex;


    private static int menuMusic;


    [Start]
    public static void Start()
    {
        cursorTex = Sprite.LoadTexture("Tex/PlayPointer.png");

        titleCardTex = Sprite.LoadTexture("Icons/MenuUI/TerminusSummit.png");


        playNormTex = Sprite.LoadTexture("Icons/MenuUI/PlayIconNorm.png");

        playHoverTex = Sprite.LoadTexture("Icons/MenuUI/PlaySelect.png");


        exitNormTex = Sprite.LoadTexture("Icons/MenuUI/ExitIconNorm.png");

        exitHoverTex = Sprite.LoadTexture("Icons/MenuUI/ExitSelect.png");


        mouseNormTex = Sprite.LoadTexture("Icons/MenuUI/MouseIconNorm.png");

        mouseHoverTex = Sprite.LoadTexture("Icons/MenuUI/MouseSelect.png");


        menuMusic = SourceObject.CreateAudioObject("Music/Menu/TS2.ogg");


        cam = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(cam, (1, 1, 1), (0, 0, 0), (0, 0, 10));

        ECSSHandler.AddComponent(cam, new Camera()
        {
            FOV = 90,

            ProjectionSize = 30,

            NearClip = 0.1f,

            FarClip = 100f,

            IsOrtho = true
        });

        ECSSHandler.AddComponent(cam, new SourceObject(menuMusic, 0, 1, 1, 1, 1, 1, true));

        SourceObject* sObj = ECSSHandler.GetComponent<SourceObject>(cam);

        sObj->State |= SourceStateFlags.Playing;


        cursor = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(cursor, (.7f, .7f, 1), (0, 0, 0), (0, 0, 1));

        ECSSHandler.AddComponent(cursor, new Sprite()
        {
            TextureObjectIndex = cursorTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 255
        });


        cursorReactBox = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cursor, cursorReactBox);

        Gymbal.CreateTransform(cursorReactBox, (.25f, .25f, 1), (0, 0, 0), (0, 1, 0));

        ECSSHandler.AddComponent(cursorReactBox, new Cursor());


        titleCard = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(titleCard, (6, 6, 1), (0, 0, 0), (0, 6, 0));

        ECSSHandler.AddComponent(titleCard, new Sprite()
        {
            TextureObjectIndex = titleCardTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 255
        });


        playButton = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(playButton, (2.5f, 2.5f, 1), (0, 0, 0), (0, 0, 0));

        ECSSHandler.AddComponent(playButton, new Sprite()
        {
            TextureObjectIndex = playNormTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 255
        });

        ECSSHandler.AddComponent(playButton, new Button([(-1, 0.5f), (1, 0.5f), (1, -0.5f), (-1, -0.5f)],
            &PlayButtonReactions.OnClick, &PlayButtonReactions.OnEnter, &PlayButtonReactions.OnExit));


        exitButton = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(exitButton, (2.5f, 2.5f, 1), (0, 0, 0), (0, -4, 0));

        ECSSHandler.AddComponent(exitButton, new Sprite()
        {
            TextureObjectIndex = exitNormTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 255
        });

        ECSSHandler.AddComponent(exitButton, new Button([(-1, 0.5f), (1, 0.5f), (1, -0.5f), (-1, -0.5f)],
            &ExitButtonReactions.OnClick, &ExitButtonReactions.OnEnter, &ExitButtonReactions.OnExit));


        Resize();
    }

    [Update]
    public static void Update()
    {
        if(KBMInput.IsHeld((int)Keys.Escape))
            FinderEngine.ChangeWindowAttrib(WindowChangeClue.WindowClose, null);


        cursorMove();
    }


        private static Vector2 bound;

        private static void cursorMove()
        {
            // Move the cursor by the
            // delta of the mouse

            Translation* curTran = ECSSHandler.GetComponent<Translation>(cursor);

            //Console.WriteLine(curTran->Translations.Xy + " BEF");

            curTran->Translations.Xy += KBMInput.CursorPositionDelta * KBMInput.CursorSensitivity;


            //Console.WriteLine(curTran->Translations.Xy + " AF");

            //Console.WriteLine(bound);


            // Clamp the position of the
            // cursor to the frustum of
            // the camera

            curTran->Translations.Xy = Vector2.Clamp(curTran->Translations.Xy, -bound, bound);
        }


    [Resize]
    public static void Resize()
    {
        Camera* c = ECSSHandler.GetComponent<Camera>(cam);

        bound = new Vector2(c->ProjectionSize * c->AspectRatio, c->ProjectionSize) * 0.5f;  
    }
}


public static unsafe class PlayButtonReactions
{
    public static void OnClick(int self, int other)
    {
        ECSSHandler.AddScene(typeof(GameWorld));

        ECSSHandler.GetComponent<SourceObject>(MenuScene.cam)->State = SourceStateFlags.Stopped;

        ECSSHandler.RemoveScene(typeof(MenuScene));
    }

    public static void OnEnter(int self, int other)
    {
        Sprite* sp = ECSSHandler.GetComponent<Sprite>(self);

        sp->TextureObjectIndex = MenuScene.playHoverTex;
    }

    public static void OnExit(int self, int other)
    {
        Sprite* sp = ECSSHandler.GetComponent<Sprite>(self);

        sp->TextureObjectIndex = MenuScene.playNormTex;
    }
}


public static unsafe class ExitButtonReactions
{
    public static void OnClick(int self, int other)
        => FinderEngine.ChangeWindowAttrib(WindowChangeClue.WindowClose, null);

    public static void OnEnter(int self, int other)
    {
        Sprite* sp = ECSSHandler.GetComponent<Sprite>(self);

        sp->TextureObjectIndex = MenuScene.exitHoverTex;
    }

    public static void OnExit(int self, int other)
    {
        Sprite* sp = ECSSHandler.GetComponent<Sprite>(self);

        sp->TextureObjectIndex = MenuScene.exitNormTex;
    }
}