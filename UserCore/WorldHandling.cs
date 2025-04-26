

using Core.ECSS;
using Core.MemoryManagement;
using Core.SAS2D;
using Core.Shimshek;
using Core.Transformations;
using OpenTK.Mathematics;

namespace UserCode.WorldHandling;


public unsafe class WorldHandler
{
    static WorldHandler()
    {
        spriteEntities = SmartPointer.CreateSmartPointer<int>();

        colliderEntities = SmartPointer.CreateSmartPointer<int>();

        checkPointEntities = SmartPointer.CreateSmartPointer<int>();


        sprites = Sprite.LoadAtlas("Tex/WorldSprites.png", 16);


        createGroundMaterials();

        loadGameWorld();
    }


    private static NA<int> sprites;


    private static int* spriteEntities;

    private static int* colliderEntities;

    private static int* checkPointEntities;

    

        private static FileStream? stream;

        private static string[] levels = Directory.GetFiles("./levels");

        private static void loadGameWorld()
        {
            foreach(string s in levels)
            {
                stream = File.OpenRead(s);


                loadLinks();

                loadSprites();

                loadColliders();

                loadObjects();


                stream.Dispose();
            }
        }

            // The links are for
            // seamlessly snapping levels
            // together
            private static Vector2 frontLink,
                                    backLink;

            private static void loadLinks()
            {
                byte[] linkData = new byte[sizeof(Vector2) << 1];

                stream?.ReadExactly(linkData, 0, sizeof(Vector2) << 1);

                fixed(byte* lPtr = linkData)
                {
                    frontLink = backLink;

                    backLink = ((Vector2*)lPtr)[1] + frontLink;
                }
            }

            private static void loadSprites()
            {
                byte[] spriteAmountRaw = new byte[sizeof(int)];

                stream?.ReadExactly(spriteAmountRaw, 0, sizeof(int));


                int spriteAmount = 0;

                fixed(byte* sPtr = spriteAmountRaw)
                    spriteAmount = *(int*)sPtr;


                byte[] spriteData = new byte[(sizeof(Vector3) + sizeof(int)) * spriteAmount];

                stream?.ReadExactly(spriteData, 0, spriteData.Length);

                fixed(byte* sPtr = spriteData)
                    for(int i = 0; i < spriteAmount; i++)
                    {
                        int index = i * (sizeof(Vector3) + sizeof(int));


                        int nEntity = ECSSHandler.CreateEntity();

                        Gymbal.CreateTransform(nEntity, (1, 1, 1), (0, 0, 0), ((Vector3*)&sPtr[index])[0] + new Vector3(frontLink.X, frontLink.Y, 0f));

                        ECSSHandler.AddComponent(nEntity, new Sprite()
                        {
                            TextureObjectIndex = sprites.Values[((int*)&sPtr[index + sizeof(Vector3)])[0]],

                            Red = 255,

                            Green = 255,

                            Blue = 255,

                            Alpha = 255,
                        });


                        fixed(int** ePtr = &spriteEntities)
                            SmartPointer.Set(ePtr, SmartPointer.GetSmartPointerLength(*ePtr), nEntity);
                    }
            }

            private static void loadColliders()
            {
                byte[] colliderAmountRaw = new byte[sizeof(int)];

                stream?.ReadExactly(colliderAmountRaw, 0, colliderAmountRaw.Length);


                int colliderAmount = 0;

                fixed(byte* cPtr = colliderAmountRaw)
                    colliderAmount = *(int*)cPtr;


                for(int i = 0; i < colliderAmount; i++)
                {
                    byte[] colliderType = new byte[1];

                    stream?.ReadExactly(colliderType, 0, 1);


                    int material = sandMat;

                    switch(colliderType[0])
                    {                        
                        // Dirt
                        case 1:
                            material = dirtMat;
                        break;
    
                        // Rock
                        case 2:
                            material = rockMat;
                        break;

                        // Water
                        case 3:
                            material = waterMat;
                        break;
                    }


                    byte[] vertexAmountRaw = new byte[sizeof(int)];

                    stream?.ReadExactly(vertexAmountRaw, 0, vertexAmountRaw.Length);


                    int vertexAmount = 0;

                    fixed(byte* vPtr = vertexAmountRaw)
                        vertexAmount = *(int*)vPtr;


                    byte[] colliderCenterRaw = new byte[sizeof(Vector2)];

                    stream?.ReadExactly(colliderCenterRaw, 0, colliderCenterRaw.Length);


                    Vector2 colliderCenter = Vector2.Zero;

                    fixed(byte* cPtr = colliderCenterRaw)
                        colliderCenter = *(Vector2*)cPtr;

                    
                    byte[] vertexPositionsRaw = new byte[vertexAmount * sizeof(Vector2)];

                    stream?.ReadExactly(vertexPositionsRaw, 0, vertexPositionsRaw.Length);

                    fixed(byte* vPtr = vertexPositionsRaw)
                    {
                        Vector2[] vertices = new Vector2[vertexAmount];

                        for(int j = 0; j < vertexAmount; j++)

                            vertices[j] = ((Vector2*)vPtr)[j];


                        int nEntity = ECSSHandler.CreateEntity();

                        fixed(int** ePtr = &colliderEntities)
                            SmartPointer.Set(ePtr, SmartPointer.GetSmartPointerLength(*ePtr), nEntity);


                        Gymbal.CreateTransform(nEntity, (1, 1, 1), (0, 0, 0), new Vector3(colliderCenter.X, colliderCenter.Y, 0f) + new Vector3(frontLink.X, frontLink.Y, 0f));           


                        Collider.CreateCollider(nEntity, vertices, material);

                        Collider* cPtr = ECSSHandler.GetComponent<Collider>(nEntity);

                        cPtr->colliderAttribs |= ColliderAttrib.Static;

                        switch(colliderType[0])
                        {
                            // Sand
                            case 0:
                                RigidBody.CreateRigidBody(nEntity, dirtRBMat, 1000);

                                RigidBody* wrPtr = ECSSHandler.GetComponent<RigidBody>(nEntity);

                                wrPtr->rigidBodyAttribs |= RigidBodyAttrib.NotSimulated; 
                            break;

                            // Dirt
                            case 1:
                                RigidBody.CreateRigidBody(nEntity, dirtRBMat, 1000);

                                RigidBody* drPtr = ECSSHandler.GetComponent<RigidBody>(nEntity);

                                drPtr->rigidBodyAttribs |= RigidBodyAttrib.NotSimulated; 
                            break;
        
                            // Rock
                            case 2:
                                RigidBody.CreateRigidBody(nEntity, rockRBMat, 1000);

                                RigidBody* rrPtr = ECSSHandler.GetComponent<RigidBody>(nEntity);

                                rrPtr->rigidBodyAttribs |= RigidBodyAttrib.NotSimulated; 
                            break;

                            // Water
                            case 3:
                                cPtr->colliderAttribs |= ColliderAttrib.Effector;

                                cPtr->effectorForce = (0, 20);
                            break;
                        }
                    }

                }
            }


            public static Vector2 Spawn;

            private static void loadObjects()
            {
                byte[] objectAmountRaw = new byte[sizeof(int)];

                stream?.ReadExactly(objectAmountRaw, 0, objectAmountRaw.Length);


                int objectAmount = 0;

                fixed(byte* oPtr = objectAmountRaw)
                    objectAmount = *(int*)oPtr;


                byte[] objects = new byte[(sizeof(int) + sizeof(Vector2)) * objectAmount];

                stream?.ReadExactly(objects, 0, objects.Length);


                fixed(byte* oPtr = objects)
                    for(int i = 0; i < objectAmount; i++)
                    {
                        int index = i * (sizeof(int) + sizeof(Vector2));

                        switch(((int*)&oPtr[index + sizeof(Vector2)])[0])
                        {
                            case 0:
                                Spawn = ((Vector2*)&oPtr[index])[0] + frontLink;
                            break;

                            case 1:
                                int nEntity = ECSSHandler.CreateEntity();

                                fixed(int** ePtr = &checkPointEntities)
                                    SmartPointer.Set(ePtr, SmartPointer.GetSmartPointerLength(*ePtr), nEntity);

                                Gymbal.CreateTransform(nEntity, (1, 1, 1), (0, 0, 0), (((Vector2*)&oPtr[index])[0].X, ((Vector2*)&oPtr[index])[0].Y, 0));

                            break;
                        }

                    }
            }


        // The collider materials
        // for each type of ground
        private static int sandMat,
                        dirtMat,
                        rockMat,
                        waterMat;

        // The rigidbody materials
        // for each type of ground
        // (except water)
        private static int sandRBMat,
                            dirtRBMat,
                            rockRBMat;

        // Initializes the collider materials
        // of each ground type
        private static void createGroundMaterials()
        {
            sandMat =
                Collider.CreateColliderMaterial(1, -1, &WorldReactions.SandEnter, null, null);

            sandRBMat =
                RigidBody.MakeRigidBodyMaterial(0, 1f);


            dirtMat =
                Collider.CreateColliderMaterial(1, -1, null, null, null);

            dirtRBMat =
                RigidBody.MakeRigidBodyMaterial(0, 1f);


            rockMat =
                Collider.CreateColliderMaterial(1, -1, null, null, null);

            rockRBMat =
                RigidBody.MakeRigidBodyMaterial(0, 1f);


            waterMat =
                Collider.CreateColliderMaterial(1, -1, null, null, null);
        }

}


public static class WorldReactions
{
    public static void SandEnter(int self, int other, Vector2 normal)
    {

    }


    public static void SandStay(int self, int other, Vector2 normal)
    {

    }


    public static void SandExit(int self, int other, Vector2 normal)
    {

    }

    
}