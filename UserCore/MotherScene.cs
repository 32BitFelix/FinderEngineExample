

using OpenTK.Mathematics;
using Core.ECSS;
using Core.InputManager;
using Core.SAS2D;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Core.Shimshek;
using Core.Transformations;
using Core.MemoryManagement;
using Core.Engine;
using Core.TonKlang;
using OpenTK.Audio.OpenAL;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Common;
using UserCode.Menu;
using UserCore.EditorImprov;


namespace UserCore.MotherScene;

[Scene][Starter]
public static unsafe class Mother
{

    [Start]
    public static void Start()
    {
        

    #if DEBUG

        ECSSHandler.AddScene(typeof(LevelMaker));

        return;

    #endif


        WindowState* wState =
            (WindowState*)NativeMemory.Alloc(sizeof(WindowState));

        *wState = WindowState.Fullscreen;

        FinderEngine.ChangeWindowAttrib(WindowChangeClue.WindowMode, wState);


        CursorModeValue* cState =
            (CursorModeValue*)NativeMemory.Alloc(sizeof(CursorModeValue));

        *cState = CursorModeValue.CursorDisabled;

        FinderEngine.ChangeWindowAttrib(WindowChangeClue.CursorMode, cState);


        ECSSHandler.AddScene(typeof(MenuScene));

    }


    /*static int ent;

    static int cam;

    static int box;

    static int bigBox;


    static int leftText;

    static int rightText;

    static int centerText;


    [Start]
    public static void Start()
    {
        Console.WriteLine("Hello");

        Input.AddInput((int)Keys.Space, InputPressType.continous, 0);


        Input.AddInput((int)Keys.W, InputPressType.continous, 1);

        Input.AddInput((int)Keys.A, InputPressType.continous, 2);

        Input.AddInput((int)Keys.S, InputPressType.continous, 3);

        Input.AddInput((int)Keys.D, InputPressType.continous, 4);


        int ari = Label.LoadFont("Arial.ttf");

        int horr = Label.LoadFont("horrendo.ttf");

        int comi = Label.LoadFont("COMICATE.TTF");


        int tex = Sprite.LoadTexture("Egg.png");


        NA<int> texes = Sprite.LoadAtlas("sprites.png", 16);


        Console.WriteLine(texes.Length);


        cam = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(cam, (1, 1, 1), (0, 0, 0), (0, 0, 50));

        ECSSHandler.AddComponent(cam, new Camera()
            {
                FOV = 90,

                ProjectionSize = 20,

                IsOrtho = true,

                NearClip = .1f,

                FarClip = 100f,
            });
        

        int colMat = Collider.CreateColliderMaterial(1, -1, null, null, &react);


        int rbMat = RigidBody.MakeRigidBodyMaterial(0, 1);


        int sound = SourceObject.CreateAudioObject("MonoVore.ogg");

        int sound2 = SourceObject.CreateAudioObject("HorAtmos.ogg");


        Console.WriteLine(AL.GetError());


        box = ECSSHandler.CreateEntity();

        Collider.CreateCollider(box, [(-1, 1), (1, 1), (1, -1), (-1, -1)], colMat);


        Collider* bc = ECSSHandler.GetComponent<Collider>(box);

        bc->colliderAttribs |= ColliderAttrib.Trigger;

        bc->effectorForce = (0, 1);


        Gymbal.CreateTransform(box, (1, 1, 1), (0, 0, 0), (-2, 0, -10));

        ECSSHandler.AddComponent(box, new Sprite()
            {
                TextureObjectIndex = texes.Values[0],

                Red = 255,

                Green = 255,

                Blue = 255,

                Alpha = 200,
            });


        ent = ECSSHandler.CreateEntity();

        Collider.CreateCollider(ent, [(-1, 1), (1, 1), (1, -1), (-1, -1)], colMat);

        RigidBody.CreateRigidBody(ent, rbMat, 1);

        Gymbal.CreateTransform(ent, (1, 1, 1), (0, 0, 0), (2, 0, -15));

        ECSSHandler.BindChild(ent, cam);

        ECSSHandler.AddComponent(ent, new Sprite()
            {
                TextureObjectIndex = tex,

                Red = 255,

                Green = 255,

                Blue = 255,

                Alpha = 255,
            });

        ECSSHandler.AddComponent(ent, new SourceObject(sound2, 0, 1, .25f, 2, 10, 1, true));

        SourceObject* sA = ECSSHandler.GetComponent<SourceObject>(ent);

        sA->State = sA->State | SourceStateFlags.Playing;


        bigBox = ECSSHandler.CreateEntity();

        Collider.CreateCollider(bigBox, [(-1, 1), (1, 1), (1, -1), (-1, -1)], colMat);


        Collider* cbb = ECSSHandler.GetComponent<Collider>(bigBox);

        cbb->colliderAttribs |= ColliderAttrib.Static;


        RigidBody.CreateRigidBody(bigBox, rbMat, 10);

        RigidBody* bbrb = ECSSHandler.GetComponent<RigidBody>(bigBox);

        bbrb->rigidBodyAttribs = RigidBodyAttrib.NotSimulated;


        ECSSHandler.AddComponent(bigBox, new SourceObject(sound, 0, 1, 1, 2, 10, 1, true));

        SourceObject* sO = ECSSHandler.GetComponent<SourceObject>(bigBox);

        sO->State = sO->State | SourceStateFlags.Playing;


        Gymbal.CreateTransform(bigBox, (10, 1, 1), (0, 0, 0), (0, -2, -15));

        ECSSHandler.AddComponent(bigBox, new Sprite()
            {
                TextureObjectIndex = 0,

                Red = 255,

                Green = 255,

                Blue = 255,

                Alpha = 100,
            });



        leftText = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cam, leftText);

        Gymbal.CreateTransform(leftText, (1, 1, 1), (0, 0, 0), (0, 0, -1));

        ECSSHandler.AddComponent(leftText, new Label("HP: 10/100", comi, Alignment.Right, 255, 50, 0, 255));


        centerText = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cam, centerText);

        Gymbal.CreateTransform(centerText, (1, 1, 1), (0, 0, 0), (0, 0, -1));

        ECSSHandler.AddComponent(centerText, new Label("Objective: Survive", horr, Alignment.Center, 75, 0, 0, 255));


        rightText = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cam, rightText);

        Gymbal.CreateTransform(rightText, (1, 1, 1), (0, 0, 0), (0, 0, -1));

        ECSSHandler.AddComponent(rightText, new Label("'Good Luck.' - Dr. Bad", ari, Alignment.Left, 0, 0, 0, 255));


        Resize(0, 0);
    }


    public static void react(int self, int other, Vector2 normal)
    {
        Console.WriteLine(self + " self  " + other + " other  " + normal + " normal");

    }


    [FixedUpdate]
    public static void Update()
    {
        if(Input.IsInput(0))
            Console.WriteLine("success " + ECSSHandler.GetDeltaTime());


        Translation* t = ECSSHandler.GetComponent<Translation>(ent);


        float multi = ECSSHandler.FixedDeltaTime * 7;


        if(Input.IsInput(1))
            t->Translations.Y += multi;

        if(Input.IsInput(2))
            t->Translations.X -= multi;

        if(Input.IsInput(3))
            t->Translations.Y -= multi;

        if(Input.IsInput(4))
            t->Translations.X += multi;


        //Vector3 rot = Gymbal.GetRelativeRotation(ent);

        //rot.Z += 5 * ECSSHandler.FixedDeltaTime;

        //Gymbal.SetRelativeRotation(ent, rot);
    }


    [End]
    public static void End()
    {
        Console.WriteLine("Goodbye");



    }


    [Resize]
    public static void Resize(int x, int y)
    {
        if(cam == 0)
            return;


        Camera* c = ECSSHandler.GetComponent<Camera>(cam);


        Translation* lT = ECSSHandler.GetComponent<Translation>(leftText);

        lT->Translations.Xy = new Vector2(c->ProjectionSize * c->AspectRatio * -0.5f + .5f, c->ProjectionSize * -0.5f + .5f);


        Translation* cT = ECSSHandler.GetComponent<Translation>(centerText);

        cT->Translations.Xy = new Vector2(0, c->ProjectionSize * 0.5f - 1.75f);


        Translation* rT = ECSSHandler.GetComponent<Translation>(rightText);

        rT->Translations.Xy = new Vector2(c->ProjectionSize * c->AspectRatio * 0.5f - 1, c->ProjectionSize * 0.5f - 4.5f);
    }*/

}