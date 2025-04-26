

using System.Runtime.InteropServices;

using Core.ECSS;
using Core.Engine;
using Core.FinderIO;
using Core.MemoryManagement;
using Core.Shimshek;
using Core.Transformations;

using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using Core.UISystem;

using Cursor = Core.UISystem.Cursor;
namespace UserCore.EditorImprov;

[Scene]
public static unsafe class LevelMaker
{
    // The size of the grid
    // that the selection
    // snaps to
    const float gridSize = 0.5f;

    // The speed of the camera
    const float camSpeed = 5;

    // The multiplier for
    // accelerating the
    // camera's speed
    const float camFastSpeedMul = 2f;


    // The amount of textures
    // stored in a row of the
    // texture tab
    public const int textureTabRow = 4;

    // A multiplier for the
    // scrolling speed of the
    // texture tab
    public const float tabScrollSpeedMultiplier = 2;


    // The current mode
    // of the editor
    private static MakerMode mode;

    private static int modeText;

    private static int helpText;

    private static int modeChangeText;


    private static int cam;

    private static int cursor;

    public static int exampleBox;


    private static int cursorTex;

    public static NA<int> spriteTexes;

    private static int frontTex, backTex;


    private static int UIFont;




    [Start]
    public static void Start()
    {
        WindowState* wState =
            (WindowState*)NativeMemory.Alloc(sizeof(WindowState));

        *wState = WindowState.Fullscreen;

        //FinderEngine.ChangeWindowAttrib(WindowChangeClue.WindowMode, wState);


        CursorModeValue* cState =
            (CursorModeValue*)NativeMemory.Alloc(sizeof(CursorModeValue));

        *cState = CursorModeValue.CursorDisabled;

        FinderEngine.ChangeWindowAttrib(WindowChangeClue.CursorMode, cState);


        cursorTex = Sprite.LoadTexture("Tex/Cursor.png");

        spriteTexes = Sprite.LoadAtlas("Tex/worldSprites2.png", 16);

        frontTex = Sprite.LoadTexture("Icons/Front.png");

        backTex = Sprite.LoadTexture("Icons/Back_1.png");


        UIFont = Label.LoadFont("Fonts/Arial.ttf");


        cam = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(cam, (1, 1, 1), (0, 0, 0), (0, 0, 10));

        ECSSHandler.AddComponent(cam , new Camera()
        {
            FOV = 90,

            ProjectionSize = 30,

            NearClip = 0.1f,

            FarClip = 100f,

            IsOrtho = true
        });


        cursor = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cam, cursor);

        Gymbal.CreateTransform(cursor, (0.5f, 0.5f, 1), (0, 0, 0), (0, 0, -2));

        ECSSHandler.AddComponent(cursor, new Sprite()
        {
            TextureObjectIndex = cursorTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 255,
        });


        int cursorTip = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cursor, cursorTip);

        Gymbal.CreateTransform(cursorTip, (1, 1, 1), (0, 0, 0), (-0.5f * 1.5f, 0.5f * 1.5f, 0));

        ECSSHandler.AddComponent(cursorTip, new Cursor());


        exampleBox = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(exampleBox, (1, 1, 1), (0, 0, 0), (0, 0, 6));

        ECSSHandler.AddComponent(exampleBox, new Sprite()
        {
            TextureObjectIndex = spriteTexes.Values[0],

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 100,
        });


        modeText = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cam, modeText);

        Gymbal.CreateTransform(modeText, (1, 1, 1), (0, 0, 0), (0, 0, -1));

        ECSSHandler.AddComponent(modeText, new Label("DEPTH: -1", UIFont, Alignment.Left | Alignment.Middle, 0, 0, 0, 255));


        helpText = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cam, helpText);

        Gymbal.CreateTransform(helpText, (0.5f, 0.5f, 1), (0, 0, 0), (0, 0, -1));

        ECSSHandler.AddComponent(helpText, new Label("[N]: Decrement Depth, [M]: Increment Depth", UIFont, Alignment.Left | Alignment.Middle, 0, 0, 0, 255));


        modeChangeText = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cam, modeChangeText);

        Gymbal.CreateTransform(modeChangeText, (0.3f, 0.3f, 1), (0, 0, 0), (0, 0, -1));

        ECSSHandler.AddComponent(modeChangeText, new Label("[I]: Decrement Mode, [O]: Increment Mode", UIFont, Alignment.Left | Alignment.Middle, 0, 0, 0, 255));


        textureTab = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(cam, textureTab);

        Gymbal.CreateTransform(textureTab, (textureTabRow, spriteTexes.Length / textureTabRow, 1), (0, 0, 0), (0, 0, -6));

        ECSSHandler.AddComponent(textureTab, new Sprite()
        {
            TextureObjectIndex = 0,

            Red = 0,

            Green = 255,

            Blue = 100,

            Alpha = 255,
        });


        float inverseWidthDivisor = 1f / textureTabRow;

        float inverseHeightDivisor = 1f / (spriteTexes.Length / textureTabRow);

        for(int i = 0; i < spriteTexes.Length; i++)
        {
            int nEntity = ECSSHandler.CreateEntity();

            ECSSHandler.BindChild(textureTab, nEntity);

            Gymbal.CreateTransform(nEntity, (0.75f * inverseWidthDivisor, 0.75f * inverseHeightDivisor, 1),
                                            (0, 0, 0),
                                            ((float)i % textureTabRow * inverseWidthDivisor * 2 - textureTabRow * 0.75f * inverseWidthDivisor, i / textureTabRow * 2 * inverseHeightDivisor - spriteTexes.Length / textureTabRow * inverseHeightDivisor + 1 * inverseHeightDivisor, 0.5f));

            ECSSHandler.AddComponent(nEntity, new Sprite()
            {
                TextureObjectIndex = spriteTexes.Values[i],

                Red = 255,

                Green = 255,

                Blue = 255,

                Alpha = 255
            });

            ECSSHandler.AddComponent(nEntity, new TextureTabImage()
            {
                SpriteIndex = i
            });

            ECSSHandler.AddComponent(nEntity, new Button([(-1, 1), (1, 1), (1, -1), (-1, -1)],
                &TabImageButtonReaction.OnClick, &TabImageButtonReaction.OnEnter, &TabImageButtonReaction.OnExit));
        }


        CurrentSpriteIndex = 0;

        spriteDepth = 1;

        spriteEntities = SmartPointer.CreateSmartPointer<int>(0);


        colliderEntities = SmartPointer.CreateSmartPointer<int>(0);


        objectEntities = SmartPointer.CreateSmartPointer<int>();

        objects = SmartPointer.CreateSmartPointer<int>(2);

        objects[0] = Sprite.LoadTexture("Icons/SpawnIcon.png");

        objects[1] = Sprite.LoadTexture("Icons/CheckpointIcon.png");


        frontLink = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(frontLink, (1, 1, 1), (0, 0, 0), (0, 0, 0));

        ECSSHandler.AddComponent(frontLink, new Sprite()
        {
            TextureObjectIndex = frontTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 100
        });


        backLink = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(backLink, (1, 1, 1), (0, 0, 0), (0, 0, 0));

        ECSSHandler.AddComponent(backLink, new Sprite()
        {
            TextureObjectIndex = backTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 100
        });



        Resize();


        LoadLevel();


        if(SmartPointer.GetSmartPointerLength(colliderEntities) == 0)
        {
            fixed(int** cPtr = &colliderEntities)
                SmartPointer.Set(cPtr, 0, ECSSHandler.CreateEntity());

            ECSSHandler.AddComponent(colliderEntities[0], new ColliderObject()
            {
                ColType = ColliderType.Sand,

                Vertices = SmartPointer.CreateSmartPointer<int>(0),

                Connectors = SmartPointer.CreateSmartPointer<int>(0),

                FocusedVertex = 0
            });
        }
    }


        private static FileStream? stream;

        private static Vector2 tempFrontPos;

        private static void LoadLevel()
        {
            string[] files = Directory.GetFiles("./EditorInput/");

            if(files.Length == 0)
                return;

            stream = File.OpenRead(files[0]);


            readLinks();

            readTextures();

            readColliders();

            readObjects();


            stream.Dispose();
        }

            private static void readLinks()
            {
                byte[] LinkData = new byte[sizeof(Vector2)];

                stream?.ReadExactly(LinkData, 0, LinkData.Length);

                fixed(byte* lPtr = LinkData)
                    tempFrontPos = ECSSHandler.GetComponent<Translation>(frontLink)->Translations.Xy = *(Vector2*)lPtr;


                stream?.ReadExactly(LinkData, 0, LinkData.Length);

                fixed(byte* lPtr = LinkData)
                    ECSSHandler.GetComponent<Translation>(backLink)->Translations.Xy = *(Vector2*)lPtr + tempFrontPos;
            }

            private static void readTextures()
            {
                byte[] spriteAmountRaw = new byte[sizeof(int)];

                stream?.ReadExactly(spriteAmountRaw, 0, spriteAmountRaw.Length);

                int spriteAmount = 0;

                fixed(byte* saPtr = spriteAmountRaw)
                    spriteAmount = *(int*)saPtr;

                
                byte[] spritesRaw = new byte[(sizeof(Vector3) + sizeof(int)) * spriteAmount];
                
                stream?.ReadExactly(spritesRaw, 0, spritesRaw.Length);


                fixed(byte* sprites = spritesRaw)
                    for(int i = 0; i < spriteAmount; i++)
                    {
                        int index = i * (sizeof(Vector3) + sizeof(int));


                        int collectionIndex = SmartPointer.GetSmartPointerLength(spriteEntities);

                        fixed(int** sPtr = &spriteEntities)
                            SmartPointer.Set(sPtr, collectionIndex, ECSSHandler.CreateEntity());

                        Gymbal.CreateTransform(spriteEntities[collectionIndex], (1, 1, 1), (0, 0, 0), *(Vector3*)&sprites[index] + (tempFrontPos.X, tempFrontPos.Y, 0));

                        ECSSHandler.AddComponent(spriteEntities[collectionIndex], new Sprite()
                        {
                            TextureObjectIndex = spriteTexes.Values[*(int*)&sprites[index + sizeof(Vector3)]],

                            Red = 255,

                            Green = 255,

                            Blue = 255,

                            Alpha = 255,
                        });

                        ECSSHandler.AddComponent(spriteEntities[collectionIndex], new SpriteObject()
                        {
                            SpriteIndex = *(int*)&sprites[index + sizeof(Vector3)]
                        });
                    }
            }

            private static void readColliders()
            {
                byte[] colliderAmountRaw = new byte[sizeof(int)];

                stream?.ReadExactly(colliderAmountRaw, 0, colliderAmountRaw.Length);


                fixed(byte* colliderAmount = colliderAmountRaw)
                    for(int i = 0; i < *(int*)colliderAmount; i++)
                    {
                        byte[] colliderTypeRaw = new byte[sizeof(byte)];

                        stream?.ReadExactly(colliderTypeRaw, 0, colliderTypeRaw.Length);


                        byte[] vertexAmountRaw = new byte[sizeof(int)];

                        stream?.ReadExactly(vertexAmountRaw, 0, vertexAmountRaw.Length);

                        int vertexAmount = 0;

                        fixed(byte* vPtr = vertexAmountRaw)
                            vertexAmount = *(int*)vPtr;


                        byte[] colliderCenterRaw = new byte[sizeof(Vector2)];

                        stream?.ReadExactly(colliderCenterRaw, 0, colliderCenterRaw.Length);


                        byte[] verticesRaw = new byte[sizeof(Vector2) * vertexAmount];

                        stream?.ReadExactly(verticesRaw, 0, verticesRaw.Length);


                        int colliderIndex = SmartPointer.GetSmartPointerLength(colliderEntities);

                        fixed(int** cPtr = &colliderEntities)
                            SmartPointer.Set(cPtr, colliderIndex, ECSSHandler.CreateEntity());

                        ECSSHandler.AddComponent(colliderEntities[colliderIndex], new ColliderObject()
                        {
                            ColType = (ColliderType)colliderTypeRaw[0],

                            Vertices = SmartPointer.CreateSmartPointer<int>(0),

                            Connectors = SmartPointer.CreateSmartPointer<int>(0),

                            FocusedVertex = 0
                        });


                        ColliderObject* colObj = ECSSHandler.GetComponent<ColliderObject>(colliderEntities[colliderIndex]);


                        fixed(byte* colliderCenter = colliderCenterRaw)
                        fixed(byte* vertices = verticesRaw)
                            for(int v = 0; v < vertexAmount; v++)
                            {
                                int vertexIndex = SmartPointer.GetSmartPointerLength(colObj->Vertices);


                                SmartPointer.Set(&colObj->Vertices, vertexIndex, ECSSHandler.CreateEntity());

                                Vector3 nPos =
                                    new Vector3(((Vector2*)vertices)[v].X + ((Vector2*)colliderCenter)->X + tempFrontPos.X, ((Vector2*)vertices)[v].Y + ((Vector2*)colliderCenter)->Y + tempFrontPos.Y, 2);

                                Gymbal.CreateTransform(colObj->Vertices[vertexIndex], (0.5f, 0.5f, 1), (0, 0, 0), nPos);

                                ECSSHandler.AddComponent(colObj->Vertices[vertexIndex], new Sprite()
                                {
                                    TextureObjectIndex = 0,

                                    Red = 0xFF,

                                    Green = 0x99,

                                    Blue = 0x00,

                                    Alpha = 100
                                });

                                colObj->FocusedVertex++;


                                if((colObj->FocusedVertex - 1) < 1)
                                    continue;

                                
                                // Create the entity to represent
                                // the new connector
                                SmartPointer.Set(&colObj->Connectors, colObj->FocusedVertex - 1, ECSSHandler.CreateEntity());

                                // Calculate the scale of
                                // the new connector
                                float connectorXScale =
                                    Vector2.Distance(ECSSHandler.GetComponent<Translation>(colObj->Vertices[colObj->FocusedVertex - 1])->Translations.Xy, ECSSHandler.GetComponent<Translation>(colObj->Vertices[colObj->FocusedVertex - 2])->Translations.Xy) * 0.5f;

                                // Calculate the rotation of
                                // the new connector
                                Vector2 normal =
                                    ECSSHandler.GetComponent<Translation>(colObj->Vertices[colObj->FocusedVertex - 2])->Translations.Xy - ECSSHandler.GetComponent<Translation>(colObj->Vertices[colObj->FocusedVertex - 1])->Translations.Xy;

                                normal.Normalize();

                                float connectorAngle = signedAngle(Vector2.UnitY, normal) + 90;

                                // Calculate the position of
                                // the new connector
                                Vector2 connectorPos =
                                    ECSSHandler.GetComponent<Translation>(colObj->Vertices[colObj->FocusedVertex - 2])->Translations.Xy + ECSSHandler.GetComponent<Translation>(colObj->Vertices[colObj->FocusedVertex - 1])->Translations.Xy;

                                connectorPos *= 0.5f;


                                // Add a transform component
                                // to the connector
                                Gymbal.CreateTransform(colObj->Connectors[colObj->FocusedVertex - 1], (connectorXScale, 0.25f, 1), (0, 0, connectorAngle), (connectorPos.X, connectorPos.Y, 2));

                                // Add a sprite component
                                // to the connector
                                ECSSHandler.AddComponent(colObj->Connectors[colObj->FocusedVertex - 1], new Sprite()
                                {
                                    TextureObjectIndex = 0,

                                    Red = 0xFF,

                                    Green = 0x99,

                                    Blue = 0x00,

                                    Alpha = 100
                                });
                            }
                    }
            }

            private static void readObjects()
            {
                byte[] objectAmountRaw = new byte[sizeof(int)];

                stream?.ReadExactly(objectAmountRaw, 0, objectAmountRaw.Length);

                int objectAmount = 0;

                fixed(byte* oPtr = objectAmountRaw)
                    objectAmount = *(int*)oPtr;


                byte[] objectsRaw = new byte[(sizeof(Vector2) + sizeof(int)) * objectAmount];

                stream?.ReadExactly(objectsRaw, 0, objectsRaw.Length);


                fixed(byte* objectsNonRaw = objectsRaw)
                    for(int i = 0; i < objectAmount; i++)
                    {
                        int index = i * (sizeof(Vector2) + sizeof(int));


                        fixed(int** oPtr = &objectEntities)
                            SmartPointer.Set(oPtr, i, ECSSHandler.CreateEntity());

                        Vector3 nPos = new Vector3(((Vector2*)(&objectsNonRaw[index]))->X + tempFrontPos.X, ((Vector2*)(&objectsNonRaw[index]))->Y + tempFrontPos.Y, 0);

                        Gymbal.CreateTransform(objectEntities[i], (1, 1, 1), (0, 0, 0), nPos);

                        ECSSHandler.AddComponent(objectEntities[i], new Sprite()
                        {
                            TextureObjectIndex = objects[*(int*)(&objectsNonRaw[index + sizeof(Vector2)])],

                            Red = 255,

                            Green = 255,

                            Blue = 255,

                            Alpha = 255,
                        });


                        byte objectValue = (byte)*(int*)(&objectsNonRaw[index + sizeof(Vector2)]);

                        ECSSHandler.AddComponent(objectEntities[i], new LevelObject()
                        {
                            ObjectType = (LevelObjectType)objectValue,
                        });
                    }
            }


    [Update]
    public static void Update()
    {
        if(KBMInput.IsPressed((int)Keys.Escape))
            FinderEngine.ChangeWindowAttrib(WindowChangeClue.WindowClose, null);


        modeCheck();


        switch(mode)
        {
            case MakerMode.TexturePainting:
                texturePaintWorker();
            break;

            case MakerMode.ColliderPainting:
                colliderPaintWorker();
            break;

            case MakerMode.ObjectPainting:
                objectPaintWorker();
            break;

            case MakerMode.LinkPainting:
                linkPaintWorker();
            break;
        }

        cameraMove();

        cursorMove();


        if(KBMInput.IsPressed((int)Keys.Enter))
            saveLevel();
    }


        private static void cameraMove()
        {
            Translation* camTran = ECSSHandler.GetComponent<Translation>(cam);


            float delta = ECSSHandler.GetDeltaTime() * camSpeed;


            if(KBMInput.IsHeld((int)Keys.LeftShift))

                delta *= camFastSpeedMul;


            if(KBMInput.IsHeld((int)Keys.W))

                camTran->Translations.Y += delta;

            if(KBMInput.IsHeld((int)Keys.S))

                camTran->Translations.Y -= delta;


            if(KBMInput.IsHeld((int)Keys.A))

                camTran->Translations.X -= delta;

            if(KBMInput.IsHeld((int)Keys.D))

                camTran->Translations.X += delta;

        }


        private static Vector2 bound;

        private static Vector2? gridOrigin = null;


        private static void cursorMove()
        {
            Translation* curTran = ECSSHandler.GetComponent<Translation>(cursor);

            curTran->Translations.Xy += KBMInput.CursorPositionDelta * KBMInput.CursorSensitivity;


            curTran->Translations.Xy = Vector2.Clamp(curTran->Translations.Xy, -bound, bound);


            Vector2 trueCursorPos = Gymbal.GetRelativeTranslation(cursor).Xy;

            // ex... box
            Translation* exBox = ECSSHandler.GetComponent<Translation>(exampleBox);


            if(gridOrigin == null && KBMInput.IsHeld((int)MouseButton.Button1))
                gridOrigin = exBox->Translations.Xy;


            if(gridOrigin != null && !KBMInput.IsHeld((int)MouseButton.Button1))
                gridOrigin = null;


            if(gridOrigin != null)
                goto uniformGrid;


            exBox->Translations.X = trueCursorPos.X - (trueCursorPos.X % gridSize) - 0.5f;

            exBox->Translations.Y = trueCursorPos.Y - (trueCursorPos.Y % gridSize) + 0.5f;

            return;


            uniformGrid:


            Vector2 diff = trueCursorPos - (Vector2)gridOrigin;


            diff.X = diff.X - (diff.X % 2);

            diff.Y = diff.Y - (diff.Y % 2);


            diff += (Vector2)gridOrigin;


            exBox->Translations.Xy = diff;
        }


        private static void modeCheck()
        {
            // Check if the mode can and should be decremented
            bool DoDecrement = KBMInput.IsPressed((int)Keys.I) && mode > (MakerMode)0;

            // Check if the mode can and should be incremented
            bool DoIncrement = KBMInput.IsPressed((int)Keys.O) && mode < (MakerMode)3;


            // Calculate if there
            // are changes in the mode
            byte nMode = (byte)((byte)mode - *(byte*)&DoDecrement + *(byte*)&DoIncrement);


            // Prematurely end the
            // method, if there is
            // no change in the mode
            if(nMode == (byte)mode)
                return;


            // Do some initialization
            // for the new mode
            switch(nMode)
            {
                case 0:
                    ECSSHandler.SetEntityState(textureTab, true);

                    Label.ChangeText(modeText, "Depth: " + (-spriteDepth).ToString());

                    Label.ChangeText(helpText, "[N]: Decrement Depth, [M]: Increment Depth");


                    ECSSHandler.GetComponent<Sprite>(exampleBox)->TextureObjectIndex = spriteTexes.Values[CurrentSpriteIndex];
                break;

                case 1:
                    {
                        Sprite* exSprite = ECSSHandler.GetComponent<Sprite>(exampleBox);

                        exSprite->TextureObjectIndex = 0;

                        exSprite->Red = 0xFF;

                        exSprite->Green = 0x99;

                        exSprite->Blue = 0x00;


                        Scale* exScale = ECSSHandler.GetComponent<Scale>(exampleBox);

                        exScale->Scales = (0.5f, 0.5f, 1f);
                    }

                    Label.ChangeText(modeText, "COL: " + colliderIndex + ", VERT: " + 0 + ", COLTYPE: " + ColliderType.Sand);

                    Label.ChangeText(helpText, "[N]: Decrement Collider, [M]: Increment Collider, [J]: Decrement Col Type, [K]: Increment Col Type");


                    ColliderObject* colObj = ECSSHandler.GetComponent<ColliderObject>(colliderEntities[colliderIndex]);

                    // Make the current collider opaque
                    for(int i = colObj->FocusedVertex - 1; i > -1; i--)
                    {
                        // Make vertex opaque
                        ECSSHandler.GetComponent<Sprite>(colObj->Vertices[i])->Alpha = 255;

                        // Skip the connector
                        // portion, if the index
                        // is almost below zero
                        if(i == 0)
                            continue;

                        // Make connector opaque
                        ECSSHandler.GetComponent<Sprite>(colObj->Connectors[i])->Alpha = 255;
                    }
                break;

                case 2:
                    {
                        Sprite* exSprite = ECSSHandler.GetComponent<Sprite>(exampleBox);

                        exSprite->TextureObjectIndex = objects[objectIndex];

                        exSprite->Red = 0xFF;

                        exSprite->Green = 0x99;

                        exSprite->Blue = 0x00;
                    }

                    Label.ChangeText(modeText, "OBJ: " + 0);

                    Label.ChangeText(helpText, "[N]: Decrement Object, [M]: Increment Object");
                break;

                case 3:
                    Translation* frontTran = ECSSHandler.GetComponent<Translation>(frontLink);

                    Translation* backTran = ECSSHandler.GetComponent<Translation>(backLink);

                    Label.ChangeText(modeText, "FL: " + frontTran->Translations.Xy + ", BL: " + backTran->Translations.Xy);

                    Label.ChangeText(helpText, "[LMB]: Set Front-Link, [RMB]: Set Back-Link");


                    ECSSHandler.GetComponent<Sprite>(exampleBox)->TextureObjectIndex = 0;


                    ECSSHandler.GetComponent<Sprite>(frontLink)->Alpha = 255;

                    ECSSHandler.GetComponent<Sprite>(backLink)->Alpha = 255;
                break;
            }


            // Do some finalization
            // for the old mode
            switch(mode)
            {
                case MakerMode.TexturePainting:
                    ECSSHandler.SetEntityState(textureTab, false);
                break;
                
                case MakerMode.ColliderPainting:
                    {
                        Sprite* exSprite = ECSSHandler.GetComponent<Sprite>(exampleBox);

                        exSprite->Red = 0xFF;

                        exSprite->Green = 0xFF;

                        exSprite->Blue = 0xFF;

                    
                        Scale* exScale = ECSSHandler.GetComponent<Scale>(exampleBox);

                        exScale->Scales = (1f, 1f, 1f);
                    }

                    ColliderObject* colObj = ECSSHandler.GetComponent<ColliderObject>(colliderEntities[colliderIndex]);

                    // Make the current collider opaque
                    for(int i = colObj->FocusedVertex - 1; i > -1; i--)
                    {
                        // Make vertex opaque
                        ECSSHandler.GetComponent<Sprite>(colObj->Vertices[i])->Alpha = 100;

                        // Skip the connector
                        // portion, if the index
                        // is almost below zero
                        if(i == 0)
                            continue;

                        // Make connector opaque
                        ECSSHandler.GetComponent<Sprite>(colObj->Connectors[i])->Alpha = 100;
                    }
                break;

                case MakerMode.ObjectPainting:

                break;

                case MakerMode.LinkPainting:
                    ECSSHandler.GetComponent<Sprite>(frontLink)->Alpha = 100;

                    ECSSHandler.GetComponent<Sprite>(backLink)->Alpha = 100;
                break;
            }


            // Save the new mode
            mode = (MakerMode)nMode;
        }



        // The index of the
        // currently used sprite
        public static int CurrentSpriteIndex;

        // The current depth to set
        // to the new sprite
        private static byte spriteDepth;

        // A list holding the entites,
        // that are supposed to represent
        // the sprites of the scene
        private static int* spriteEntities;

        // The tab to hold the
        // textures of the world
        // stuffs
        private static int textureTab;

        // The system to handle all the
        // things related texture painting
        private static void texturePaintWorker()
        {
            // Check if the painting should be
            // skipped, by calling the method
            // responsible for the behaviour
            // of the texture tab
            bool shouldSkipPainting = textureTabBehaviour();

            // Make the example box fully transparent,
            // if the cursor is on top of the texture tab,
            // otherwise make it somewhat half transparent
            ECSSHandler.GetComponent<Sprite>(exampleBox)->Alpha = (byte)(100 * (*(byte*)&shouldSkipPainting ^ 0x01));


            // Check if the depth can and should be deeper
            bool DoDecrement = KBMInput.IsPressed((int)Keys.M) && spriteDepth > 1;

            // Check if the depth can and should be more shallow
            bool DoIncrement = KBMInput.IsPressed((int)Keys.N) && spriteDepth < 255;


            // Calculate the new depth
            spriteDepth += (byte)(*(byte*)&DoIncrement + -*(byte*)&DoDecrement);


            // Change the text displaying the
            // depth, if the depth has changed
            if(*(byte*)&DoIncrement - *(byte*)&DoDecrement != 0)
                Label.ChangeText(modeText, "DEPTH: " + (-spriteDepth).ToString());


            // If the painting should
            // be skipped...
            if(shouldSkipPainting)
                // Prematurely
                // end the method
                return;


            // Get the 2D position of the
            // example box
            Vector3 exBoxPos = Gymbal.GetRelativeTranslation(exampleBox);

            // If the button for adding
            // a sprite is not pressed...
            if(!KBMInput.IsHeld((int)MouseButton.Left))
                // Skip to the
                // part of removing
                // a sprite
                goto removal;


            // Check if the exBox is overlapping with
            // a sprite on the same depth. If so, skip
            // to the removal behaviour
            for(int i = SmartPointer.GetSmartPointerLength(spriteEntities) - 1; i > -1; i--)
            {
                if(spriteEntities[i] == 0)
                    continue;

                if(ECSSHandler.GetComponent<Translation>(spriteEntities[i])->Translations == (exBoxPos.X, exBoxPos.Y, -spriteDepth))
                    goto removal;
            }


            // A cache for saving the index
            // of the new sprite. Fallback
            // is set to the length of the array
            int nIndex = SmartPointer.GetSmartPointerLength(spriteEntities);

            // Iterate through each element
            // in the sprite entities array,
            // to find a free spot to save
            // the new sprite at
            for(int i = SmartPointer.GetSmartPointerLength(spriteEntities) - 1; i > -1; i--)
            {
                if(spriteEntities[i] != 0)
                    continue;

                nIndex = i;

                break;
            }


            // Create the new sprite object
            fixed(int** sPtr = &spriteEntities)
                SmartPointer.Set(sPtr, nIndex, ECSSHandler.CreateEntity());

            Gymbal.CreateTransform(spriteEntities[nIndex], (1, 1, 1), (0, 0, 0), (exBoxPos.X, exBoxPos.Y, -spriteDepth));

            ECSSHandler.AddComponent(spriteEntities[nIndex], new Sprite()
            {
                TextureObjectIndex = spriteTexes.Values[CurrentSpriteIndex],

                Red = 255,

                Green = 255,

                Blue = 255,

                Alpha = 255
            });

            ECSSHandler.AddComponent(spriteEntities[nIndex], new SpriteObject()
            {
                SpriteIndex = CurrentSpriteIndex,
            });


            // Jumping point
            // to the removal part
            removal:

            // If the button for removing
            // a sprite is not pressed...
            if(!KBMInput.IsHeld((int)MouseButton.Right))
                // Prematurely
                // end the method
                return;


            // A cache for saving the index
            // of the new sprite. Fallback
            // is set to the length of the array
            nIndex = -1;

            // Iterate through each element
            // in the sprite entities array,
            // to find a free spot to save
            // the new sprite at
            for(int i = SmartPointer.GetSmartPointerLength(spriteEntities) - 1; i > -1; i--)
            {
                if(spriteEntities[i] == 0)
                    continue;

                Translation* curTran = ECSSHandler.GetComponent<Translation>(spriteEntities[i]);

                if(curTran->Translations.Xy != exBoxPos.Xy)
                    continue;

                nIndex = i;

                break;
            }


            if(nIndex == -1)
                return;


            ECSSHandler.RemoveEntity(spriteEntities[nIndex]);

            spriteEntities[nIndex] = 0;
        }

            // A method specifically handling
            // the behaviour of the texture tab.
            // Returns true, if the cursor is
            // on top of it
            private static bool textureTabBehaviour()
            {
                Translation* curTran = ECSSHandler.GetComponent<Translation>(cursor);


                bool ifCursorIsOnTab = (curTran->Translations.X - 0.5f) < (-bound.X + textureTabRow * 2);
                        

                Translation* texTab = ECSSHandler.GetComponent<Translation>(textureTab);
                    

                if(texTab->Translations.Y > (spriteTexes.Length / textureTabRow - bound.Y) && KBMInput.MouseWheelDelta < 0)
                    return ifCursorIsOnTab;

                if(texTab->Translations.Y < (-(spriteTexes.Length / textureTabRow) + bound.Y) && KBMInput.MouseWheelDelta > 0)
                    return ifCursorIsOnTab;


                if(ifCursorIsOnTab && KBMInput.MouseWheelDelta != 0)
                    texTab->Translations.Y -= KBMInput.MouseWheelDelta * tabScrollSpeedMultiplier;


                return ifCursorIsOnTab;
            }



        private static int* colliderEntities;

        private static byte colliderIndex;

        // The behaviour for all the
        // collider painting stuff
        private static void colliderPaintWorker()
        {
            bool DoDecrement = KBMInput.IsPressed((int)Keys.N) && colliderIndex > 0;

            bool DoIncrement = KBMInput.IsPressed((int)Keys.M) && colliderIndex < 255;


            colliderIndex += (byte)(*(byte*)&DoIncrement - *(byte*)&DoDecrement);


            if(colliderIndex > SmartPointer.GetSmartPointerLength(colliderEntities) - 1)
            {
                fixed(int** cPtr = &colliderEntities)
                    SmartPointer.Set(cPtr, colliderIndex, ECSSHandler.CreateEntity());

                ECSSHandler.AddComponent(colliderEntities[colliderIndex], new ColliderObject()
                {
                    ColType = ColliderType.Sand,

                    Vertices = SmartPointer.CreateSmartPointer<int>(0),

                    Connectors = SmartPointer.CreateSmartPointer<int>(0),

                    FocusedVertex = 0
                });
            }


            Vector2 exBoxPos = ECSSHandler.GetComponent<Translation>(exampleBox)->Translations.Xy;

            ColliderObject* colObj = ECSSHandler.GetComponent<ColliderObject>(colliderEntities[colliderIndex]);


            if(*(byte*)&DoIncrement - *(byte*)&DoDecrement != 0)
            {
                Label.ChangeText(modeText, "COL: " + colliderIndex.ToString() + ", VERT: " + colObj->FocusedVertex + ", COLTYPE: " + colObj->ColType);

                // Make the current collider opaque
                for(int i = colObj->FocusedVertex - 1; i > -1; i--)
                {
                    // Make vertex opaque
                    ECSSHandler.GetComponent<Sprite>(colObj->Vertices[i])->Alpha = 255;

                    // Skip the connector
                    // portion, if the index
                    // is almost below zero
                    if(i == 0)
                        continue;

                    // Make connector opaque
                    ECSSHandler.GetComponent<Sprite>(colObj->Connectors[i])->Alpha = 255;
                }

                if(colliderIndex - *(byte*)&DoIncrement + *(byte*)&DoDecrement < 0 && colliderIndex == 0)
                    goto skipInv;

                ColliderObject* otherCol =
                    ECSSHandler.GetComponent<ColliderObject>(colliderEntities[colliderIndex - *(byte*)&DoIncrement + *(byte*)&DoDecrement]);

                for(int i = otherCol->FocusedVertex - 1; i > -1; i--)
                {
                    // Make vertex opaque
                    ECSSHandler.GetComponent<Sprite>(otherCol->Vertices[i])->Alpha = 100;

                    // Skip the connector
                    // portion, if the index
                    // is almost below zero
                    if(i == 0)
                        continue;

                    // Make connector opaque
                    ECSSHandler.GetComponent<Sprite>(otherCol->Connectors[i])->Alpha = 100;
                }
            }


            skipInv:


            DoDecrement = KBMInput.IsPressed((int)Keys.J) && colObj->ColType > 0;

            DoIncrement = KBMInput.IsPressed((int)Keys.K) && colObj->ColType < (ColliderType)4;


            *(byte*)&colObj->ColType += (byte)(*(byte*)&DoIncrement - *(byte*)&DoDecrement);


            if(*(byte*)&DoIncrement - *(byte*)&DoDecrement != 0)
                Label.ChangeText(modeText, "COL: " + colliderIndex.ToString() + ", VERT: " + colObj->FocusedVertex + ", COLTYPE: " + colObj->ColType);


            if(!KBMInput.IsPressed((int)MouseButton.Button1))
                goto removal;


            // Check, if the vertex is not overlapping
            // with a previous one
            for(int i = SmartPointer.GetSmartPointerLength(colObj->Vertices) - 1; i > -1; i--)
            {
                if(colObj->Vertices[i] == 0)
                    continue;

                if(ECSSHandler.GetComponent<Translation>(colObj->Vertices[i])->Translations.Xy == exBoxPos)
                    goto removal;
            }


            SmartPointer.Set(&colObj->Vertices, colObj->FocusedVertex, ECSSHandler.CreateEntity());

            Gymbal.CreateTransform(colObj->Vertices[colObj->FocusedVertex], (0.5f, 0.5f, 1), (0, 0, 0), (exBoxPos.X, exBoxPos.Y, 2));

            ECSSHandler.AddComponent(colObj->Vertices[colObj->FocusedVertex], new Sprite()
            {
                TextureObjectIndex = 0,

                Red = 0xFF,

                Green = 0x99,

                Blue = 0x00,

                Alpha = 0xFF
            });

            colObj->FocusedVertex++;


            Label.ChangeText(modeText, "COL: " + colliderIndex.ToString() + ", VERT: " + colObj->FocusedVertex + ", COLTYPE: " + colObj->ColType);


            if((colObj->FocusedVertex - 1) < 1)
                goto removal;


            // Create the entity to represent
            // the new connector
            SmartPointer.Set(&colObj->Connectors, colObj->FocusedVertex - 1, ECSSHandler.CreateEntity());

            // Calculate the scale of
            // the new connector
            float connectorXScale = Vector2.Distance(exBoxPos, ECSSHandler.GetComponent<Translation>(colObj->Vertices[colObj->FocusedVertex - 2])->Translations.Xy) * 0.5f;

            // Calculate the rotation of
            // the new connector
            Vector2 normal = ECSSHandler.GetComponent<Translation>(colObj->Vertices[colObj->FocusedVertex - 2])->Translations.Xy - exBoxPos;

            normal.Normalize();

            float connectorAngle = signedAngle(Vector2.UnitY, normal) + 90;

            // Calculate the position of
            // the new connector
            Vector2 connectorPos = ECSSHandler.GetComponent<Translation>(colObj->Vertices[colObj->FocusedVertex - 2])->Translations.Xy + exBoxPos;

            connectorPos *= 0.5f;


            // Add a transform component
            // to the connector
            Gymbal.CreateTransform(colObj->Connectors[colObj->FocusedVertex - 1], (connectorXScale, 0.25f, 1), (0, 0, connectorAngle), (connectorPos.X, connectorPos.Y, 2));

            // Add a sprite component
            // to the connector
            ECSSHandler.AddComponent(colObj->Connectors[colObj->FocusedVertex - 1], new Sprite()
            {
                TextureObjectIndex = 0,

                Red = 0xFF,

                Green = 0x99,

                Blue = 0x00,

                Alpha = 0xFF
            });


            removal:


            if(!KBMInput.IsHeld((int)MouseButton.Button2))
                return;


            if(colObj->FocusedVertex < 1)
                return;


            Vector2 lastPos = ECSSHandler.GetComponent<Translation>(colObj->Vertices[colObj->FocusedVertex - 1])->Translations.Xy;

            if(lastPos != exBoxPos)
                return;


            ECSSHandler.RemoveEntity(colObj->Vertices[--colObj->FocusedVertex]);

            colObj->Vertices[colObj->FocusedVertex] = 0;   


            if(colObj->FocusedVertex > 0)
            {
                ECSSHandler.RemoveEntity(colObj->Connectors[colObj->FocusedVertex]);

                colObj->Connectors[colObj->FocusedVertex] = 0;  
            }



            Label.ChangeText(modeText, "COL: " + colliderIndex.ToString() + ", VERT: " + colObj->FocusedVertex + ", COLTYPE: " + colObj->ColType);         
        }

            private static float signedAngle(Vector2 a, Vector2 b)
            {
                float unsigned_angle = angleDot(a, b);

                float sign = MathF.Sign(a.X * b.Y - a.Y * b.X);

                return unsigned_angle * sign;
            }

            private static float angleDot(Vector2 a, Vector2 b)
            {
                float value = MathF.Acos(Vector2.Dot(a, b) / (a.Length * b.Length));

                return value * (180 / MathHelper.Pi);
            }


        private static int* objectEntities;

        private static int* objects;

        private static byte objectIndex;

        // The behaviour for all the
        // object painting stuff
        private static void objectPaintWorker()
        {
            const byte objectAmount = 2;


            bool DoDecrement = KBMInput.IsPressed((int)Keys.N) && objectIndex > 0;

            bool DoIncrement = KBMInput.IsPressed((int)Keys.M) && objectIndex < (objectAmount - 1);


            objectIndex += (byte)(*(byte*)&DoIncrement - *(byte*)&DoDecrement);


            if(*(byte*)&DoIncrement - *(byte*)&DoDecrement != 0)
            {
                Label.ChangeText(modeText, "OBJ: " + objectIndex.ToString());

                ECSSHandler.GetComponent<Sprite>(exampleBox)->TextureObjectIndex = objects[objectIndex];
            }


            Vector2 exBoxPos = Gymbal.GetRelativeTranslation(exampleBox).Xy;


            if(!KBMInput.IsPressed((int)MouseButton.Button1))
                goto remove;


            for(int i = SmartPointer.GetSmartPointerLength(objectEntities) - 1; i > -1; i--)
            {
                if(objectEntities[i] == 0)
                    continue;

                if(!ECSSHandler.GetEnableState(objectEntities[i]))
                    continue;
                
                if(ECSSHandler.GetComponent<Translation>(objectEntities[i])->Translations.Xy == exBoxPos)
                    goto remove;
            }


            int nIndex = SmartPointer.GetSmartPointerLength(objectEntities);

            for(int i = SmartPointer.GetSmartPointerLength(objectEntities) - 1; i > -1; i--)
            {
                if(objectEntities[i] != 0)
                    continue;

                nIndex = i;
                
                break;
            }


            fixed(int** oPtr = &objectEntities)
                SmartPointer.Set(oPtr, nIndex, ECSSHandler.CreateEntity());

            Gymbal.CreateTransform(objectEntities[nIndex], (1, 1, 1), (0, 0, 0), (exBoxPos.X, exBoxPos.Y, 0));

            ECSSHandler.AddComponent(objectEntities[nIndex], new Sprite()
            {
                TextureObjectIndex = objects[objectIndex],

                Red = 255,

                Green = 255,

                Blue = 255,

                Alpha = 255
            });

            ECSSHandler.AddComponent(objectEntities[nIndex], new LevelObject()
            {
                ObjectType = (LevelObjectType)objectIndex,
            });


            remove:


            if(!KBMInput.IsHeld((int)MouseButton.Button2))
                return;


            nIndex = -1;

            for(int i = SmartPointer.GetSmartPointerLength(objectEntities) - 1; i > -1; i--)
            {
                if(objectEntities[i] == 0)
                    continue;


                if(ECSSHandler.GetComponent<Translation>(objectEntities[i])->Translations.Xy != exBoxPos)
                    continue;


                nIndex = i;

                break;
            }  


            if(nIndex == -1)
                return;

            
            ECSSHandler.RemoveEntity(objectEntities[nIndex]);

            objectEntities[nIndex] = 0;
        }


        // The entities representing the
        // front link's and back link's positions
        private static int frontLink, backLink;

        // The behaviour for all things
        // painting links
        private static void linkPaintWorker()
        {
            bool setFront = KBMInput.IsPressed((int)MouseButton.Button1);

            bool setBack = KBMInput.IsPressed((int)MouseButton.Button2);


            Translation* exTran = ECSSHandler.GetComponent<Translation>(exampleBox);

            Translation* frontTran = ECSSHandler.GetComponent<Translation>(frontLink);

            Translation* backTran = ECSSHandler.GetComponent<Translation>(backLink);


            if(!setFront)
                goto back;

            frontTran->Translations.Xy = exTran->Translations.Xy;


            back:

            if(!setBack)
                goto end;

            backTran->Translations.Xy = exTran->Translations.Xy;


            end:

            if(!(setFront || setBack))
                return;


            Label.ChangeText(modeText, "FL: " + frontTran->Translations.Xy + ", BL: " + backTran->Translations.Xy);
        }


    private static void saveLevel()
    {
        stream = File.OpenWrite("./EditorOutput/NEWLEVEL.lvl");


        saveLinks();

        saveSprites();

        saveColliders();

        saveObjects();


        stream.Dispose();


        Console.WriteLine("Success");
    }

        private static Vector2 frontLinkPos;

        private static void saveLinks()
        {
            byte[] linkData = new byte[sizeof(Vector2) * 2];

            fixed(byte* lPtr = linkData)
            {
                ((Vector2*)lPtr)[0] = frontLinkPos = ECSSHandler.GetComponent<Translation>(frontLink)->Translations.Xy;

                ((Vector2*)lPtr)[1] = ECSSHandler.GetComponent<Translation>(backLink)->Translations.Xy - ECSSHandler.GetComponent<Translation>(frontLink)->Translations.Xy;
            }

            stream?.Write(linkData, 0, linkData.Length);
        }

        private static void saveSprites()
        {
            int* validSprites = SmartPointer.CreateSmartPointer<int>(0);

            for(int i = SmartPointer.GetSmartPointerLength(spriteEntities) - 1; i > -1 ; i--)
            {
                if(spriteEntities[i] == 0)
                    continue;

                SmartPointer.Set(&validSprites, SmartPointer.GetSmartPointerLength(validSprites), spriteEntities[i]);
            }


            byte[] spriteAmountRaw = new byte[sizeof(int)];

            fixed(byte* spriteAmount = spriteAmountRaw)
                *(int*)spriteAmount = SmartPointer.GetSmartPointerLength(validSprites);

            stream?.Write(spriteAmountRaw, 0, spriteAmountRaw.Length);


            byte[] spriteDataRaw = new byte[(sizeof(Vector3) + sizeof(int)) * SmartPointer.GetSmartPointerLength(validSprites)];

            fixed(byte* spriteData = spriteDataRaw)
                for(int i = SmartPointer.GetSmartPointerLength(validSprites) - 1; i > -1; i--)
                {
                    int index = i * (sizeof(Vector3) + sizeof(int));


                    *(Vector3*)&spriteData[index] = ECSSHandler.GetComponent<Translation>(validSprites[i])->Translations - (frontLinkPos.X, frontLinkPos.Y, 0);

                    *(int*)&spriteData[index + sizeof(Vector3)] = ECSSHandler.GetComponent<SpriteObject>(validSprites[i])->SpriteIndex;
                }

            stream?.Write(spriteDataRaw, 0, spriteDataRaw.Length);


            SmartPointer.Free(validSprites);
        }

        private static void saveColliders()
        {
            int* validColliders = SmartPointer.CreateSmartPointer<int>(0);

            for(int i = SmartPointer.GetSmartPointerLength(colliderEntities) - 1; i > -1; i--)
            {
                if(colliderEntities[i] == 0)
                    continue;

                SmartPointer.Set(&validColliders, SmartPointer.GetSmartPointerLength(validColliders), colliderEntities[i]); 
            }


            byte[] colliderAmountRaw = new byte[sizeof(int)];

            fixed(byte* colliderAmount = colliderAmountRaw)
                *(int*)colliderAmount = SmartPointer.GetSmartPointerLength(validColliders);

            stream?.Write(colliderAmountRaw, 0, colliderAmountRaw.Length);


            for(int i = SmartPointer.GetSmartPointerLength(validColliders) - 1; i > -1; i--)
            {
                // Get a reference for the current collider

                ColliderObject* colObj = ECSSHandler.GetComponent<ColliderObject>(validColliders[i]);


                // Save collider type

                byte[] colTypeRaw = [(byte)colObj->ColType];

                stream?.Write(colTypeRaw, 0, colTypeRaw.Length);


                // Save the amount of vertices of the collider

                byte[] vertexAmountRaw = new byte[sizeof(int)];

                fixed(byte* vertexAmount = vertexAmountRaw)
                    *(int*)vertexAmount = colObj->FocusedVertex;
                
                stream?.Write(vertexAmountRaw, 0, vertexAmountRaw.Length);


                // Calculate the center of the collider

                Vector2 colliderCenter = Vector2.Zero;

                for(int v = 0; v < colObj->FocusedVertex; v++)
                    colliderCenter += ECSSHandler.GetComponent<Translation>(colObj->Vertices[v])->Translations.Xy;

                colliderCenter /= colObj->FocusedVertex;


                // Save the center of the collider

                byte[] colliderCenterRaw = new byte[sizeof(Vector2)];

                fixed(byte* cPtr = colliderCenterRaw)
                    *(Vector2*)cPtr = colliderCenter - frontLinkPos;

                stream?.Write(colliderCenterRaw, 0, colliderCenterRaw.Length);


                // Save the vertices of the collider

                byte[] verticesRaw = new byte[sizeof(Vector2) * colObj->FocusedVertex];

                fixed(byte* vertices = verticesRaw)
                    for(int v = 0; v < colObj->FocusedVertex; v++)
                        ((Vector2*)vertices)[v] = ECSSHandler.GetComponent<Translation>(colObj->Vertices[v])->Translations.Xy - colliderCenter;

                stream?.Write(verticesRaw, 0, verticesRaw.Length);
            }


            SmartPointer.Free(validColliders);
        }

        private static void saveObjects()
        {
            // Find the valid objects

            int* validObjects = SmartPointer.CreateSmartPointer<int>(0);

            for(int i = SmartPointer.GetSmartPointerLength(objectEntities) - 1; i > -1; i--)
            {
                if(objectEntities[i] == 0)
                    continue;

                SmartPointer.Set(&validObjects, SmartPointer.GetSmartPointerLength(validObjects), objectEntities[i]);
            }


            // Save the amount of valid objects

            byte[] objectAmountRaw = new byte[sizeof(int)];

            fixed(byte* objectAmount = objectAmountRaw)
                *(int*)objectAmount = SmartPointer.GetSmartPointerLength(validObjects);

            stream?.Write(objectAmountRaw, 0, objectAmountRaw.Length);


            // Save the objects

            byte[] objectsRaw = new byte[(sizeof(Vector2) + sizeof(int)) * SmartPointer.GetSmartPointerLength(validObjects)];

            fixed(byte* oPtr = objectsRaw)
                for(int i = SmartPointer.GetSmartPointerLength(validObjects) - 1; i > -1; i--)
                {
                    int index = i * (sizeof(Vector2) + sizeof(int));


                    *(Vector2*)&oPtr[index] = ECSSHandler.GetComponent<Translation>(validObjects[i])->Translations.Xy - frontLinkPos;

                    *(int*)&oPtr[index + sizeof(Vector2)] = (byte)ECSSHandler.GetComponent<LevelObject>(validObjects[i])->ObjectType;
                }

            stream?.Write(objectsRaw, 0, objectsRaw.Length);


            SmartPointer.Free(validObjects);
        }


    [End]
    public static void End()
    {

    }


    [Resize]
    public static void Resize()
    {
        // Calculate the positive bounds
        // of the camera's frustum

        Camera* c = ECSSHandler.GetComponent<Camera>(cam);

        bound = new Vector2(c->ProjectionSize * c->AspectRatio, c->ProjectionSize) * 0.5f; 


        // Set the mode Text's
        // position relative
        // to the camera's bounds

        Translation* mText = ECSSHandler.GetComponent<Translation>(modeText);

        mText->Translations.X = bound.X - 2;

        mText->Translations.Y = bound.Y - 2;


        // Set the help Text's
        // position relative
        // to the camera's bounds

        Translation* hText = ECSSHandler.GetComponent<Translation>(helpText);

        hText->Translations.X = bound.X - 2;

        hText->Translations.Y = -bound.Y + 1;


        // Set the mode change Text's
        // position relative
        // to the camera's bounds

        Translation* mcText = ECSSHandler.GetComponent<Translation>(modeChangeText);

        mcText->Translations.X = bound.X - 2;

        mcText->Translations.Y = bound.Y - 3.5f;


        // Set the texture tab's
        // position relative to
        // the camera's bounds

        Translation* txTab = ECSSHandler.GetComponent<Translation>(textureTab);

        txTab->Translations.X = -bound.X + textureTabRow;
    }
}


[Component]
public struct TextureTabImage
{
    // The index of the texture object
    // related to the sprite textures
    public int SpriteIndex;
}


// A list of numerical
// values representing
// the different states
// of the level maker
public enum MakerMode : byte
{
    TexturePainting = 0,

    ColliderPainting = 1,

    ObjectPainting = 2,

    LinkPainting = 3
}


[Component]
public struct SpriteObject
{
    // The index of the sprite's
    // texture, within the
    // world texture array
    public int SpriteIndex;
}


[Component]
public unsafe struct ColliderObject
{
    // The type of the collider
    public ColliderType ColType;

    // A list holding the
    // entities representing
    // as the vertices
    // of the collider
    public int* Vertices;

    // A list holding the
    // entities representing
    // as the lines connecting
    // between each vertex of
    // the collider
    public int* Connectors;

    // The index of the
    // focused vertex
    public int FocusedVertex;
}


[Component]
public struct LevelObject
{
    // The object type of
    // the entitybound to
    // this component
    public LevelObjectType ObjectType;
}


// A list of numerical
// values representing
// each different type
// of object in a level
public enum LevelObjectType : byte
{
    // The starting point
    // of cuppy
    Spawn = 0,

    // A point to save
    // progress at when
    // the game turns off
    Checkpoint = 1,

    // An animated
    // sprite representing
    // grassblades
    Grass = 2,

    // An animated
    // sprite representing
    // a small, red flower
    Flower = 3,

    // An animated
    // sprite representing
    // leaves of a tree
    Leaf = 4,

    // An animated
    // sprite representing
    // the surface of water
    WaterSurface = 5,

    // An animated
    // sprite representing
    // the body of water
    WaterBody = 6,


}


// A list of numerical
// values representing
// the different types
// of surfaces
public enum ColliderType
{
    // Sandy surface,
    // particles may
    // appear on collision
    // and less fall damage
    Sand = 0,

    // Dirty/Soil surface,
    // has more friction
    // than sand
    Dirt = 1,

    // Rocky surface,
    // has the most friction
    Rock = 2,

    // Fluid collider,
    // progressively
    // moves anything up
    // that is within itself
    Water = 3,

    // Metal surface,
    // Very hard and least
    // friction
    Metal = 4,
}


public static unsafe class TabImageButtonReaction
{


    public static void OnClick(int self, int other)
    {
        TextureTabImage* STTI = ECSSHandler.GetComponent<TextureTabImage>(self);

        LevelMaker.CurrentSpriteIndex = STTI->SpriteIndex;


        Sprite* exSprite = ECSSHandler.GetComponent<Sprite>(LevelMaker.exampleBox);

        exSprite->TextureObjectIndex = LevelMaker.spriteTexes.Values[STTI->SpriteIndex];
    }       


    public static void OnEnter(int self, int other)
    {
        Scale* sScale = ECSSHandler.GetComponent<Scale>(self);


        float inverseWidthDivisor = 1f / LevelMaker.textureTabRow;

        float inverseHeightDivisor = 1f / (LevelMaker.spriteTexes.Length / LevelMaker.textureTabRow);


        sScale->Scales = (1 * inverseWidthDivisor, 1 * inverseHeightDivisor, 1);
    }


    public static void OnExit(int self, int other)
    {
        Scale* sScale = ECSSHandler.GetComponent<Scale>(self);


        float inverseWidthDivisor = 1f / LevelMaker.textureTabRow;

        float inverseHeightDivisor = 1f / (LevelMaker.spriteTexes.Length / LevelMaker.textureTabRow);


        sScale->Scales = (0.75f * inverseWidthDivisor, 0.75f * inverseHeightDivisor, 1);
    }
}