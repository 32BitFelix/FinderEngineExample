


using OpenTK.Mathematics;
using Core.ECSS;
using Core.Shimshek;
using Core.Transformations;
using Core.InputManager;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;
using Core.Engine;
using Core.MemoryManagement;
using OpenTK.Windowing.Common;
using System.Runtime.CompilerServices;

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


        // Add inputs

        // Escape for leaving the application
        Input.AddInput((int)Keys.Escape, InputPressType.continous, 0);


        // I for decrementing the
        // primary mode
        Input.AddInput((int)Keys.I, InputPressType.direct, 1);

        // O for incrementing the
        // primary mode
        Input.AddInput((int)Keys.O, InputPressType.direct, 2);


        // Leftclick of the mouse for
        // using the mode's primary function
        Input.AddInput((int)MouseButton.Left, InputPressType.direct, 5);

        // Leftclick of the mouse for
        // using the mode's secondary function
        Input.AddInput((int)MouseButton.Right, InputPressType.direct, 6);


        // Enter for saving the
        // current level
        Input.AddInput((int)Keys.Enter, InputPressType.direct, 7);

        // Delete for deleting the
        // current level
        Input.AddInput((int)Keys.Delete, InputPressType.direct, 8);


        // N for decrementing the
        // secondary mode
        Input.AddInput((int)Keys.N, InputPressType.direct, 9);

        // M for incrementing the
        // secondary mode
        Input.AddInput((int)Keys.M, InputPressType.direct, 10);


        // W for moving the
        // camera up
        Input.AddInput((int)Keys.W, InputPressType.continous, 11);

        // A for moving the
        // camera left
        Input.AddInput((int)Keys.A, InputPressType.continous, 12);

        // S for moving the
        // camera down
        Input.AddInput((int)Keys.S, InputPressType.continous, 13);

        // D for moving the
        // camera right
        Input.AddInput((int)Keys.D, InputPressType.continous, 14);


        // Create the arial font

        ariFont = Label.LoadFont("Fonts/Arial.ttf");


        // Create the cursor's texture

        cursorTex = Sprite.LoadTexture("Tex/Cursor.png");


        // Create the sprite's texture

        spriteTexes = Sprite.LoadAtlas("Tex/WorldSprites.png", 16);


        // Create the camera

        cam = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(cam, (1, 1, 1), (0, 0, 0), (0, 0, 10));

        ECSSHandler.AddComponent(cam, new Camera()
        {
            FOV = 90,

            ProjectionSize = 25,

            NearClip = 0.1f,

            FarClip = 100f,

            IsOrtho = true
        });


        // Create the cursor

        cursor = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cam, cursor);

        Gymbal.CreateTransform(cursor, (0.5f, 0.5f, 1), (0, 0, 0), (0, 0, -2));


        cursorSprite = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cursor, cursorSprite);

        Gymbal.CreateTransform(cursorSprite, (1, 1, 1), (0, 0, 0), (1, -1, 0));

        ECSSHandler.AddComponent(cursorSprite, new Sprite()
        {
            TextureObjectIndex = cursorTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 255
        });


        clueText = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cam, clueText);

        Gymbal.CreateTransform(clueText, (1, 1, 1), (0, 0, 0), (0, 0, 0));

        ECSSHandler.AddComponent(clueText, new Label("TEX: " + textureIndex + ", LYR: " + textureLayer, ariFont, Alignment.Center, 0, 0, 0, 255));


        box = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(box, (1, 1, 1), (0, 0, 0), (0, 0, 7));

        ECSSHandler.AddComponent(box, new Sprite()
        {
            TextureObjectIndex = 0,

            Red = 255,

            Green = 100,

            Blue = 255,

            Alpha = 100,
        });


        spriteIndex = SmartPointer.CreateSmartPointer<int>();

        textureEntities = SmartPointer.CreateSmartPointer<int>();


        colliders = SmartPointer.CreateSmartPointer<ColliderClue>(1);

        colliders[0] = new ColliderClue()
        {
            vertices = SmartPointer.CreateSmartPointer<int>(0),

            connectors = SmartPointer.CreateSmartPointer<int>(0),

            type = ColliderType.Sand,
        };


        objEntities = SmartPointer.CreateSmartPointer<int>(0);

        objTypes = SmartPointer.CreateSmartPointer<int>(0);

        objectIcons = SmartPointer.CreateSmartPointer<int>(2);

        objectIcons[0] = Sprite.LoadTexture("Icons/SpawnIcon.png");

        objectIcons[1] = Sprite.LoadTexture("Icons/CheckpointIcon.png");


        frontTex = Sprite.LoadTexture("Icons/Front.png");

        backTex = Sprite.LoadTexture("Icons/Back_1.png");


        frontLink = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(frontLink, (1, 1, 1), (0, 0, 0), (0, 0, 5));

        ECSSHandler.AddComponent(frontLink, new Sprite()
        {
            TextureObjectIndex = frontTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 255,
        });


        backLink = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(backLink, (1, 1, 1), (0, 0, 0), (0, 0, 5));

        ECSSHandler.AddComponent(backLink, new Sprite()
        {
            TextureObjectIndex = backTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 255,
        });



        Resize();
    }


    // An ID reference to the
    // arial font
    private static int ariFont;


    // The texture the cursor
    // will be displayed with
    private static int cursorTex;

    // The textures of each
    // sprite set with the
    // texture painter
    private static NA<int> spriteTexes;


    // The entity to act
    // as the camera
    private static int cam;

    // The entity to act
    // as the cursor for
    // editing the level
    private static int cursor;

    // The visual of the
    // cursor
    private static int cursorSprite;

    // A text that helps
    // show the user
    // what is going on
    private static int clueText;


    // The Positive bounds of
    // the camera's frustum,
    // relative to the camera's
    // position
    private static Vector2 bound;


    // Stores the current
    // mode of the editor
    private static sbyte mode;


    // The example box
    // for setting things
    private static int box;
    

    // Update marked methods
    // are called every frame
    [Update]
    public static void Update()
    {
        if(Input.IsInput(0))
            FinderEngine.ChangeWindowAttrib(WindowChangeClue.WindowClose, null);


        cursorMove();

        boxClamp();

        moveCam();


        modeCheck();
        

        switch(mode)
        {

            case (sbyte)EditorMode.TexturePaint:
                texHandle();
            break;

            case (sbyte)EditorMode.ColliderPaint:
                collHandle();
            break;

            case (sbyte)EditorMode.ObjectPaint:
                objHandle();
            break;

            case (sbyte)EditorMode.LinkPaint:
                linkHandle();
            break;
        }


        if(Input.IsInput(7))
            saveLevel();
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

        // Clamp the example box
        // to a grid
        private static void boxClamp()
        {
            Vector2 curPos = Gymbal.GetRelativeTranslation(cursor).Xy;

            Translation* boxTran = ECSSHandler.GetComponent<Translation>(box);


            boxTran->Translations.X = curPos.X - (curPos.X % .5f); 

            boxTran->Translations.Y = curPos.Y - (curPos.Y % .5f);
        }


        // Checks if a new mode has
        // been selected and does
        // some management afterwards,
        // if that is the case
        private static void modeCheck()
        {


            if(Input.MouseWheelDelta == 0)
                return;


            // Check the old mode
            switch(mode)
            {
                // Dispose some things
                // from the texture painting
                // mode to have a clear slate
                // for the new mode
                case (sbyte)EditorMode.TexturePaint:

                break;

                // Dispose some things
                // from the collider painting
                // mode to have a clear slate
                // for the new mode
                case (sbyte)EditorMode.ColliderPaint:

                    // Make all vertices of the
                    // last focused collider transparent

                    for(int i = SmartPointer.GetSmartPointerLength(colliders[colliderIndex].vertices) - 1; i > -1; i--)
                    {
                        if(colliders[colliderIndex].vertices[i] == 0)
                            break;

                        Sprite* sp = ECSSHandler.GetComponent<Sprite>(colliders[colliderIndex].vertices[i]);

                        sp->Alpha = 100;
                    }

                    // Make all vertices of the
                    // last focused collider transparent

                    for(int i = SmartPointer.GetSmartPointerLength(colliders[colliderIndex].connectors) - 1; i > -1; i--)
                    {
                        if(colliders[colliderIndex].connectors[i] == 0)
                            break;

                        Sprite* sp = ECSSHandler.GetComponent<Sprite>(colliders[colliderIndex].connectors[i]);

                        sp->Alpha = 100;
                    }

                break;

                // Dispose some things
                // from the object painting
                // mode to have a clear slate
                // for the new mode
                case (sbyte)EditorMode.ObjectPaint:

                break;

                // Dispose some things
                // from the link painting
                // mode to have a clear slate
                // for the new mode
                case (sbyte)EditorMode.LinkPaint:

                break;
            }


            // Change the mode
            // to the new one
            // and clamp it,
            // if it went
            // beyond it's limit

            mode += (sbyte)Input.MouseWheelDelta;

            mode = sbyte.Clamp(mode, (sbyte)EditorMode.TexturePaint, (sbyte)EditorMode.LinkPaint);


            // Check the new mode
            switch(mode)
            {
                // Prepare the editor for
                // the texture paint mode
                case (sbyte)EditorMode.TexturePaint:
    	            Label.ChangeText(clueText, "TEX: " + textureIndex + ", LYR: " + textureLayer);


                    ECSSHandler.GetComponent<Sprite>(box)->TextureObjectIndex = spriteTexes.Values[textureIndex];


                    ECSSHandler.GetComponent<Scale>(box)->Scales.Xy = (1, 1);
                break;

                // Prepare the editor for
                // the collider paint mode
                case (sbyte)EditorMode.ColliderPaint:
    	            Label.ChangeText(clueText, "COL: " + colliderIndex + ", TYPE: " + colliders[colliderIndex].type.ToString());


                    Sprite* cSprite = ECSSHandler.GetComponent<Sprite>(box);

                    cSprite->TextureObjectIndex = 0;


                    ECSSHandler.GetComponent<Scale>(box)->Scales.Xy = (0.5f, 0.5f);

                    // Make all vertices of the
                    // last focused collider transparent

                    for(int i = SmartPointer.GetSmartPointerLength(colliders[colliderIndex].vertices) - 1; i > -1; i--)
                    {
                        if(colliders[colliderIndex].vertices[i] == 0)
                            break;

                        Sprite* sp = ECSSHandler.GetComponent<Sprite>(colliders[colliderIndex].vertices[i]);

                        sp->Alpha = 255;
                    }

                    // Make all vertices of the
                    // last focused collider transparent

                    for(int i = SmartPointer.GetSmartPointerLength(colliders[colliderIndex].connectors) - 1; i > -1; i--)
                    {
                        if(colliders[colliderIndex].connectors[i] == 0)
                            break;

                        Sprite* sp = ECSSHandler.GetComponent<Sprite>(colliders[colliderIndex].connectors[i]);

                        sp->Alpha = 255;
                    }
                break;

                // Prepare the editor for
                // the object paint mode
                case (sbyte)EditorMode.ObjectPaint:
    	            Label.ChangeText(clueText, "OBJ: " + currentObjType.ToString());


                    Sprite* oSprite = ECSSHandler.GetComponent<Sprite>(box);

                    oSprite->TextureObjectIndex = objectIcons[(int)currentObjType];


                    ECSSHandler.GetComponent<Scale>(box)->Scales.Xy = (1, 1);
                break;

                // Prepare the editor for
                // the link paint mode
                case (sbyte)EditorMode.LinkPaint:


                    Sprite* fbSprite = ECSSHandler.GetComponent<Sprite>(box);

                    fbSprite->TextureObjectIndex = 0;


                    ECSSHandler.GetComponent<Scale>(box)->Scales.Xy = (1, 1);


                    Translation* frontTran = ECSSHandler.GetComponent<Translation>(frontLink);

                    Translation* backTran = ECSSHandler.GetComponent<Translation>(backLink);


                    Label.ChangeText(clueText, "FL: " + frontTran->Translations.Xy + ", BL: " + backTran->Translations.Xy);

                break;
            }
        }


        private static void moveCam()
        {
            Translation* cT = ECSSHandler.GetComponent<Translation>(cam);


            float delta = ECSSHandler.GetDeltaTime() * 5;       


            if(Input.IsInput(11))
                cT->Translations.Y += delta;

            if(Input.IsInput(12))
                cT->Translations.X -= delta;

            if(Input.IsInput(13))
                cT->Translations.Y -= delta;

            if(Input.IsInput(14))
                cT->Translations.X += delta;
        }


        // Saves the sprite index
        // of t he sprite entity at
        // the same index
        private static int* spriteIndex;

        // The array of entities
        // that display the
        // created entities
        private static int* textureEntities;

        // The index of the
        // currently used texture
        private static int textureIndex;

        // The layer the texture
        // will be set to
        private static int textureLayer = -1;

        // The system that handles
        // all the texture stuff
        private static void texHandle()
        {
            // Primary function

            // Retrieve the inputs of
            // the I and O key and save
            // them to an array

            bool* inputs = stackalloc bool[2];

            inputs[0] = Input.IsInput(1);

            inputs[1] = Input.IsInput(2);


            // Check mathematically,
            // if the primary function
            // should be incremented
            // or decremented

            sbyte sub = (sbyte)-((sbyte*)inputs)[0];

            sbyte add = ((sbyte*)inputs)[1];


            // Calculate if
            // a new mode
            // has been requested

            sbyte addition = (sbyte)(sub + add);


            if((textureIndex == 0 && addition < 0) ||
                (textureIndex == spriteTexes.Length - 1 && addition > 0))

                    goto secondary;


            textureIndex += addition;


            Label.ChangeText(clueText, "TEX: " + textureIndex + ", LYR: " + textureLayer);


            Sprite* bSprite = ECSSHandler.GetComponent<Sprite>(box);

            bSprite->TextureObjectIndex = spriteTexes.Values[textureIndex];


            // Secondary function

            secondary:


            inputs[0] = Input.IsInput(9);

            inputs[1] = Input.IsInput(10);


            sub = (sbyte)-((sbyte*)inputs)[0];

            add = ((sbyte*)inputs)[1];


            addition = (sbyte)(sub + add);


            if((textureLayer == -1 && addition > 0) ||
                (textureLayer == -100 && addition < 0))
                    
                    goto pNR;


            textureLayer += addition;


            Label.ChangeText(clueText, "TEX: " + textureIndex + ", LYR: " + textureLayer);


            // Placement and removal

            pNR:

            // Left click
            if(Input.IsInput(5))
            {
                int nIndex = SmartPointer.GetSmartPointerLength(textureEntities);


                for(int i = nIndex - 1; i > -1; i--)
                {
                    if(textureEntities[i] != 0)
                        continue;

                    nIndex = i;

                    break;
                }


                Vector2 boxPos = Gymbal.GetRelativeTranslation(box).Xy;


                int nEntity = ECSSHandler.CreateEntity();


                Gymbal.CreateTransform(nEntity, (1, 1, 1), (0, 0, 0), (boxPos.X, boxPos.Y, textureLayer));


                ECSSHandler.AddComponent(nEntity, new Sprite()
                    {
                        TextureObjectIndex = spriteTexes.Values[textureIndex],

                        Red = 255,

                        Green = 255,

                        Blue = 255,

                        Alpha = 255,
                    });


                fixed(int** tPtr = &textureEntities)
                    SmartPointer.Set(tPtr, nIndex, nEntity);

                fixed(int** sPtr = &spriteIndex)
                    SmartPointer.Set(sPtr, nIndex, textureIndex);
            }

            // Right click
            if(Input.IsInput(6))
            {
                int rIndex = -1;

                for(int i = SmartPointer.GetSmartPointerLength(textureEntities) - 1; i > -1; i--)
                {
                    if(textureEntities[i] == 0)
                        continue;


                    Translation* entTran = ECSSHandler.GetComponent<Translation>(textureEntities[i]);

                    Vector2 boxPos = Gymbal.GetRelativeTranslation(box).Xy;


                    if(boxPos != entTran->Translations.Xy)
                        continue;


                    rIndex = i;

                    break;
                }


                if(rIndex == -1)
                    return;


                ECSSHandler.RemoveEntity(textureEntities[rIndex]);


                textureEntities[rIndex] = 0;

                spriteIndex[rIndex] = 0;
            }



        }


        // The array that holds the colliders
        // and their vertices
        private static ColliderClue* colliders;

        // The index of the currently
        // used collider
        private static int colliderIndex = 0;

        // The system that handles
        // all the collider stuff
        private static void collHandle()
        {
            // Primary function

            // Retrieve the inputs of
            // the I and O key and save
            // them to an array

            bool* inputs = stackalloc bool[2];

            inputs[0] = Input.IsInput(1);

            inputs[1] = Input.IsInput(2);


            // Check mathematically,
            // if the primary function
            // should be incremented
            // or decremented

            sbyte sub = (sbyte)-((sbyte*)inputs)[0];

            sbyte add = ((sbyte*)inputs)[1];


            // Calculate if
            // a new mode
            // has been requested

            sbyte addition = (sbyte)(sub + add);


            createDeleteBehaviour();


            if(addition == 0)
                goto secondary;


            if((colliderIndex == 0 && addition < 0) || (colliderIndex == 99 && addition > 0))
                goto secondary;


            // Make all vertices of the
            // last focused collider transparent

            for(int i = SmartPointer.GetSmartPointerLength(colliders[colliderIndex].vertices) - 1; i > -1; i--)
            {
                if(colliders[colliderIndex].vertices[i] == 0)
                    break;

                Sprite* sp = ECSSHandler.GetComponent<Sprite>(colliders[colliderIndex].vertices[i]);

                sp->Alpha = 100;
            }

            // Make all vertices of the
            // last focused collider transparent

            for(int i = SmartPointer.GetSmartPointerLength(colliders[colliderIndex].connectors) - 1; i > -1; i--)
            {
                if(colliders[colliderIndex].connectors[i] == 0)
                    break;

                Sprite* sp = ECSSHandler.GetComponent<Sprite>(colliders[colliderIndex].connectors[i]);

                sp->Alpha = 100;
            }


            colliderIndex += addition;


            // Create a new collider,
            // if one in the new index
            // doesn't exist
            if(colliderIndex > SmartPointer.GetSmartPointerLength(colliders) - 1)
                fixed(ColliderClue** cPtr = &colliders)
                    SmartPointer.Set(cPtr, colliderIndex, new ColliderClue()
                    {
                        vertices = SmartPointer.CreateSmartPointer<int>(),

                        connectors = SmartPointer.CreateSmartPointer<int>(),

                        type = ColliderType.Sand
                    });

            
            // Make all vertices of the
            // newly focused collider opaque

            for(int i = SmartPointer.GetSmartPointerLength(colliders[colliderIndex].vertices) - 1; i > -1; i--)
            {
                if(colliders[colliderIndex].vertices[i] == 0)
                    break;

                Sprite* sp = ECSSHandler.GetComponent<Sprite>(colliders[colliderIndex].vertices[i]);

                sp->Alpha = 255;
            }

            // Make all vertices of the
            // newly focused collider opaque

            for(int i = SmartPointer.GetSmartPointerLength(colliders[colliderIndex].connectors) - 1; i > -1; i--)
            {
                if(colliders[colliderIndex].connectors[i] == 0)
                    break;

                Sprite* sp = ECSSHandler.GetComponent<Sprite>(colliders[colliderIndex].connectors[i]);

                sp->Alpha = 255;
            }



            secondary:

            inputs[0] = Input.IsInput(9);

            inputs[1] = Input.IsInput(10);


            sub = (sbyte)-((sbyte*)inputs)[0];

            add = ((sbyte*)inputs)[1];


            addition = (sbyte)(sub + add);


            if(addition == 0)
                goto display;

            if((colliders[colliderIndex].type == ColliderType.Sand && addition < 0) || (colliders[colliderIndex].type == ColliderType.Water && addition > 0))
                goto display;


            colliders[colliderIndex].type += addition;


            display: 

    	    Label.ChangeText(clueText, "COL: " + colliderIndex + ", TYPE: " + colliders[colliderIndex].type.ToString());
        }

            // The method for creating
            // or deleting vertices
            private static void createDeleteBehaviour()
            {
                // Left click
                if(Input.IsInput(5))
                {
                    int nIndex = SmartPointer.GetSmartPointerLength(colliders[colliderIndex].vertices);

                    // Iterate through each vertex
                    // of the current collider
                    for(int i = 0; i < SmartPointer.GetSmartPointerLength(colliders[colliderIndex].vertices); i++)
                    {
                        if(colliders[colliderIndex].vertices[i] != 0)
                            continue;

                        nIndex = i;
                        
                        break;
                    }


                    int nEntity = ECSSHandler.CreateEntity();


                    Vector2 boxPos = Gymbal.GetRelativeTranslation(box).Xy;

                    Gymbal.CreateTransform(nEntity, (.5f, .5f, 1), (0, 0, 0), (boxPos.X, boxPos.Y, .5f));


                    ECSSHandler.AddComponent(nEntity, new Sprite()
                        {
                            TextureObjectIndex = 0,

                            Red = 0xff,

                            Green = 0x99,

                            Blue = 0x33,

                            Alpha = 255,
                        });


                    SmartPointer.Set(&colliders[colliderIndex].vertices, nIndex, nEntity);


                    if(nIndex == 0)
                        return;

                    
                    Translation* firstPos = ECSSHandler.GetComponent<Translation>(colliders[colliderIndex].vertices[nIndex - 1]);

                    Translation* secondPos = ECSSHandler.GetComponent<Translation>(colliders[colliderIndex].vertices[nIndex]);


                    Vector2 between = (firstPos->Translations.Xy + secondPos->Translations.Xy) * .5f;


                    float distance = Vector2.Distance(firstPos->Translations.Xy, secondPos->Translations.Xy);

                    distance *= 0.5f;


                    float a = angle(Vector2.UnitY, secondPos->Translations.Xy - firstPos->Translations.Xy);

                    if(firstPos->Translations.Y > secondPos->Translations.Y)
                        a = -a;



                    int lineEntity = ECSSHandler.CreateEntity();

                    Gymbal.CreateTransform(lineEntity, (.1f, distance, 1), (0, 0, a), (between.X, between.Y, .5f));

                    ECSSHandler.AddComponent(lineEntity, new Sprite()
                        {
                            TextureObjectIndex = 0,

                            Red = 0xff,

                            Green = 0x99,

                            Blue = 0x33,

                            Alpha = 255,
                        });


                    SmartPointer.Set(&colliders[colliderIndex].connectors, nIndex - 1, lineEntity);


                    return;
                }

                // Right click
                if(Input.IsInput(6))
                {
                    int rIndex = -1;

                    for(int i = SmartPointer.GetSmartPointerLength(colliders[colliderIndex].vertices) - 1; i > -1; i--)
                    {
                        if(colliders[colliderIndex].vertices[i] == 0)
                            continue;


                        Translation* entTran = ECSSHandler.GetComponent<Translation>(colliders[colliderIndex].vertices[i]);

                        Vector2 boxPos = Gymbal.GetRelativeTranslation(box).Xy;


                        if(boxPos != entTran->Translations.Xy)
                            continue;


                        rIndex = i;

                        break;
                    }


                    if(rIndex == -1)
                        return;


                    removeColliderLine(rIndex - 1);


                    if(rIndex + 1 > SmartPointer.GetSmartPointerLength(colliders[colliderIndex].vertices) - 1)
                    {
                        ECSSHandler.RemoveEntity(colliders[colliderIndex].vertices[rIndex]);


                        colliders[colliderIndex].vertices[rIndex] = 0;


                        return;
                    }
                    else
                    {
                        if(colliders[colliderIndex].vertices[rIndex + 1] != 0)
                            return;
                    }


                    ECSSHandler.RemoveEntity(colliders[colliderIndex].vertices[rIndex]);


                    colliders[colliderIndex].vertices[rIndex] = 0;
                }
            }

                private static void removeColliderLine(int index)
                {
                    if(index < 0)
                        return;

                    ECSSHandler.RemoveEntity(colliders[colliderIndex].connectors[index]);

                    colliders[colliderIndex].connectors[index] = 0;
                }


        // The list of entities,
        // that count as objects
        private static int* objEntities;

        private static int* objTypes;

        // The array that holds
        // the texture IDs of
        // each object type
        private static int* objectIcons;

        // The currently selected
        // object type
        private static ObjectType currentObjType;

        // The system that handles
        // all the collider stuff
        private static void objHandle()
        {
            // Primary function

            // Retrieve the inputs of
            // the I and O key and save
            // them to an array

            bool* inputs = stackalloc bool[2];

            inputs[0] = Input.IsInput(1);

            inputs[1] = Input.IsInput(2);


            // Check mathematically,
            // if the primary function
            // should be incremented
            // or decremented

            sbyte sub = (sbyte)-((sbyte*)inputs)[0];

            sbyte add = ((sbyte*)inputs)[1];


            // Calculate if
            // a new mode
            // has been requested

            sbyte addition = (sbyte)(sub + add);


            if(addition == 0)
                goto primary;


            if((addition < 0 && currentObjType == 0) ||
                (addition > 0 && currentObjType == (ObjectType)1))
                goto primary;


            currentObjType += addition;


            Sprite* bSprite = ECSSHandler.GetComponent<Sprite>(box);

            bSprite->TextureObjectIndex = objectIcons[(int)currentObjType];


            // The primary function of
            // object creation

            primary:

                if(Input.IsInput(5))
                {
                    int nIndex = SmartPointer.GetSmartPointerLength(objEntities);


                    for(int i = nIndex - 1; i > -1; i--)
                    {
                        if(objEntities[i] != 0)
                            continue;

                        nIndex = i;
                        
                        break;
                    }


                    int nEntity = ECSSHandler.CreateEntity();


                    Vector2 boxPos = Gymbal.GetRelativeTranslation(box).Xy;

                    Gymbal.CreateTransform(nEntity, (1, 1, 1), (0, 0, 0), (boxPos.X, boxPos.Y, 0.5f));

                    ECSSHandler.AddComponent(nEntity, new Sprite()
                    {
                        TextureObjectIndex = objectIcons[(int)currentObjType],

                        Red = 255,

                        Green = 255,

                        Blue = 255,

                        Alpha = 255
                    });


                    fixed(int** oPtr = &objEntities)
                        SmartPointer.Set(oPtr, nIndex, nEntity);

                    fixed(int** otPtr = &objTypes)
                        SmartPointer.Set(otPtr, nIndex, (int)currentObjType);


                    goto display;
                }


                if(Input.IsInput(6))
                {
                    int rIndex = -1;


                    Vector2 boxPos = Gymbal.GetRelativeTranslation(box).Xy;

                    for(int i = SmartPointer.GetSmartPointerLength(objEntities) - 1; i > -1; i--)
                    {
                        if(objEntities[i] == 0)
                            continue;

                        if(ECSSHandler.GetComponent<Translation>(objEntities[i])->Translations.Xy != boxPos)
                            continue;

                        rIndex = i;
                        
                        break;
                    }


                    if(rIndex == -1)
                        return;

                    
                    ECSSHandler.RemoveEntity(objEntities[rIndex]);


                    objEntities[rIndex] = 0;

                    objTypes[rIndex] = 0;
                }


            // The display of the values

            display:

    	        Label.ChangeText(clueText, "OBJ: " + currentObjType.ToString());
        }



        private static int frontTex;

        private static int backTex;


        private static int frontLink;

        private static int backLink;

        private static void linkHandle()
        {   
            if(Input.IsInput(5))
            {
                Vector2 boxPos = Gymbal.GetRelativeTranslation(box).Xy;

                // Hehe... fronttran
                Translation* frontTran = ECSSHandler.GetComponent<Translation>(frontLink);

                frontTran->Translations.Xy = boxPos;
            }


            if(Input.IsInput(6))
            {
                Vector2 boxPos = Gymbal.GetRelativeTranslation(box).Xy;

                Translation* backTran = ECSSHandler.GetComponent<Translation>(backLink);

                backTran->Translations.Xy = boxPos;
            }


            if(Input.IsInput(5) || Input.IsInput(6))
            {
                Translation* frontTran = ECSSHandler.GetComponent<Translation>(frontLink);

                Translation* backTran = ECSSHandler.GetComponent<Translation>(backLink);


    	        Label.ChangeText(clueText, "FL: " + frontTran->Translations.Xy + ", BL: " + backTran->Translations.Xy);
            }
        }


        // I had the perfect opportunity to use components
        // for saving the data at runtime and make
        // the serialization process easier...   Lesson learned!


        private static FileStream? stream;

        private static Vector3 frontPos, backPos;

        private static void saveLevel()
        {

            stream = File.OpenWrite("./LEVEL.lvl");


            saveLinks();

            saveSprites();

            saveColliders();

            saveObjects();


            stream.Dispose();


            //FinderEngine.ChangeWindowAttrib(WindowChangeClue.WindowClose, null);
        }

            private static void saveLinks()
            {
                // Allocate the byte array
                // to write the data to
                byte[] linkData = new byte[sizeof(Vector2) << 1];


                // Get a pointer to the
                // byte array, to easily
                // write the data to
                fixed(byte* lPtr = linkData)
                {
                    ((Vector2*)lPtr)[0] = ECSSHandler.GetComponent<Translation>(frontLink)->Translations.Xy;

                    ((Vector2*)lPtr)[1] = ECSSHandler.GetComponent<Translation>(backLink)->Translations.Xy - ((Vector2*)lPtr)[0];


                    frontPos = ECSSHandler.GetComponent<Translation>(frontLink)->Translations;

                    frontPos.Z = 0;

                    backPos = ECSSHandler.GetComponent<Translation>(backLink)->Translations;

                    backPos.Z = 0;
                }


                // Finally, write the data
                // in the byte array to the
                // target file
                stream?.Write(linkData, 0, sizeof(Vector2) << 1);
            }

            private static void saveSprites()
            {
                // Create an array, to hold
                // the IDs of the sprites,
                // that are valid for saving
                long* validSprites = SmartPointer.CreateSmartPointer<long>(0);

                for(int i = SmartPointer.GetSmartPointerLength(textureEntities) - 1; i > -1; i--)
                {
                    if(textureEntities[i] == 0)
                        continue;

                    int currentIndex = SmartPointer.GetSmartPointerLength(validSprites);


                    SmartPointer.Resize(&validSprites, currentIndex + 1);


                    ((int*)&validSprites[currentIndex])[0] = textureEntities[i];

                    ((int*)&validSprites[currentIndex])[1] = spriteIndex[i];
                }


                // Save the amount of sprites

                byte[] spriteAmount = new byte[sizeof(int)];

                fixed(byte* cPtr = spriteAmount)
                    *(int*)cPtr = SmartPointer.GetSmartPointerLength(validSprites);

                stream?.Write(spriteAmount, 0, sizeof(int));



                byte[] spriteData =
                    new byte[(sizeof(Vector3) + sizeof(int)) * SmartPointer.GetSmartPointerLength(validSprites)];


                fixed(byte* sPtr = spriteData)
                {

                    for(int i = 0; i < SmartPointer.GetSmartPointerLength(validSprites); i++)
                    {
                        // Calculate the index of the
                        // Block of memory to write to
                        int index = i * (sizeof(Vector3) + sizeof(int));

                        // Save the position
                        // of the current sprite
                        ((Vector3*)&sPtr[index])[0] = ECSSHandler.GetComponent<Translation>(((int*)&validSprites[i])[0])->Translations - frontPos;

                        // Save the sprite index
                        // of the current sprite
                        ((int*)&sPtr[index + sizeof(Vector3)])[0] = ((int*)&validSprites[i])[1];
                    }

                }


                stream?.Write(spriteData, 0, (sizeof(Vector3) + sizeof(int)) * SmartPointer.GetSmartPointerLength(validSprites));


                SmartPointer.Free(validSprites);
            }

            private static void saveColliders()
            {
                // Get all valid colliders
                int* validColliders = SmartPointer.CreateSmartPointer<int>(0);

                for(int i = SmartPointer.GetSmartPointerLength(colliders) - 1; i > -1; i--)
                {
                    if(SmartPointer.GetSmartPointerLength(colliders[i].vertices) == 0)
                        continue;

                    if(colliders[i].vertices[0] == 0)
                        continue;

                    SmartPointer.Set(&validColliders, SmartPointer.GetSmartPointerLength(validColliders), i);
                }


                // Save the amount of valid colliders

                byte[] colliderAmount = new byte[sizeof(int)];

                fixed(byte* cPtr = colliderAmount)
                    *(int*)cPtr = SmartPointer.GetSmartPointerLength(validColliders);

                stream?.Write(colliderAmount, 0, sizeof(int));


                // Iterate through each valid collider
                // and do the necessary stuff for serialization
                for(int i = SmartPointer.GetSmartPointerLength(validColliders) - 1; i > -1; i--)
                {
                    int vertexAmount = 0;

                    Vector2 colliderCenter = Vector2.Zero;


                    // Iterate through each vertex in
                    // the current collider
                    for(int j = 0; j < SmartPointer.GetSmartPointerLength(colliders[validColliders[i]].vertices); j++)
                    {
                        if(colliders[validColliders[i]].vertices[j] == 0)
                            break;

                        vertexAmount++;

                        colliderCenter += ECSSHandler.GetComponent<Translation>(colliders[validColliders[i]].vertices[j])->Translations.Xy;
                    }


                    colliderCenter /= vertexAmount;


                    // Save the type of the
                    // current collider
                    stream?.WriteByte((byte)colliders[validColliders[i]].type);


                    byte[] colliderData = new byte[sizeof(int) + sizeof(Vector2) * (vertexAmount + 1)];

                    fixed(byte* cPtr = colliderData)
                    {
                        // Save the amount of vertices
                        *(int*)&cPtr[0] = vertexAmount;

                        // Save the center of the
                        // current collider
                        *(Vector2*)&cPtr[sizeof(int)] = colliderCenter - frontPos.Xy;

                        // Save each vertex
                        for(int j = 0; j < vertexAmount; j++)
                        {
                            // Save the current vertex
                            ((Vector2*)&cPtr[sizeof(int) + sizeof(Vector2)])[j] =
                                ECSSHandler.GetComponent<Translation>(colliders[validColliders[i]].vertices[j])->Translations.Xy - colliderCenter;
                        }
                    }


                    stream?.Write(colliderData, 0, sizeof(int) + sizeof(Vector2) * (vertexAmount + 1));
                }


                SmartPointer.Free(validColliders);
            }

            private static void saveObjects()
            {
                long* validObjects = SmartPointer.CreateSmartPointer<long>();
            
                for(int i = SmartPointer.GetSmartPointerLength(objEntities) - 1; i > -1; i--)
                {
                    if(objEntities[i] == 0)
                        continue;

                    int currentIndex = SmartPointer.GetSmartPointerLength(validObjects);


                    SmartPointer.Resize(&validObjects, currentIndex + 1);


                    ((int*)&validObjects[currentIndex])[0] = objEntities[i];

                    ((int*)&validObjects[currentIndex])[1] = objTypes[i];
                }


                // Save the amount of valid objects

                byte[] objAmount = new byte[sizeof(int)];

                fixed(byte* cPtr = objAmount)
                    *(int*)cPtr = SmartPointer.GetSmartPointerLength(validObjects);

                stream?.Write(objAmount, 0, sizeof(int));
                


                byte[] objData = new byte[(sizeof(int) + sizeof(Vector2)) * SmartPointer.GetSmartPointerLength(validObjects)];

                fixed(byte* oPtr = objData)
                    for(int i = SmartPointer.GetSmartPointerLength(validObjects) - 1; i > -1; i--)
                    {
                        int currentIndex = (sizeof(int) + sizeof(Vector2)) * i;

                        *(Vector2*)&oPtr[currentIndex] = ECSSHandler.GetComponent<Translation>(((int*)&validObjects[i])[0])->Translations.Xy - frontPos.Xy;

                        *(int*)&oPtr[currentIndex + sizeof(Vector2)] = ((int*)&validObjects[i])[1];


                        Console.WriteLine(((int*)&validObjects[i])[1] + " " + i);
                    }

                
                stream?.Write(objData, 0, (sizeof(int) + sizeof(Vector2)) * SmartPointer.GetSmartPointerLength(validObjects));
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


        Translation* cLT = ECSSHandler.GetComponent<Translation>(clueText);

        cLT->Translations.Xy = (0, bound.Y - 1.5f);
    }


        // Calculates the angle
        // between two vectors
        // 
        // Keeping it for future plans
        private static float angle(Vector2 a, Vector2 b)
        {
            float val = cross(a, b);

            val /= a.Length * b.Length;

            val = (float)MathHelper.Asin(val);

            return val * 180 / MathHelper.Pi;
        }

        // Calculate crossproduct
        // of two vectors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float cross(Vector2 value1, Vector2 value2)
            => value1.X * value2.Y - value1.Y * value2.X;
}


// An enumerator that
// defines the different
// modes, that the
// editor can be in
public enum EditorMode : sbyte
{
    // The mode to set
    // textures with
    TexturePaint = 0,

    // The mode to draw
    // different colliders
    // with and specify
    // their material
    ColliderPaint = 1,

    // The mode to place
    // different objects
    // with unqiue behaviour
    ObjectPaint = 2,

    // The mode to paint
    // the links of the
    // current level
    LinkPaint = 3,
}

// A structure that holds
// the vertices of a collider
// and it's type
public unsafe struct ColliderClue
{
    // A list of entities,
    // that represent the
    // vertices of the collider
    public int* vertices;

    // A list of entities,
    // that visualizes the
    // connection between
    // each vertex, to
    // show the order of them
    // and avoid
    // self-intersecting polygons
    public int* connectors;

    // The type of the collider.
    // Is relevant for assigning
    // special collision events
    // to it
    public ColliderType type;
}

// An enumerator for
// defining the type
// of surface, that
// a collider has
public enum ColliderType
{
    Sand = 0,

    Dirt = 1,

    Rock = 2,

    Water = 3,

    
}

// An enumerator for
// defining the type
// of unique object,
// that should be created
public enum ObjectType
{
    Spawn = 0,

    Checkpoint = 1,

    Spade = 2,

    PA = 3,

    Panzer = 4,

    Robo = 5,

    RedFlower = 6,

    ShortGrass = 7,


}