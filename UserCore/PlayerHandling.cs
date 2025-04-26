



using System.Runtime.CompilerServices;
using Core.AnimationSystem;
using Core.ECSS;
using Core.FinderIO;
using Core.InputManager;
using Core.MemoryManagement;
using Core.SAS2D;
using Core.Shimshek;
using Core.TonKlang;
using Core.TonKlangIO;
using Core.Transformations;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using UserCode.WorldHandling;

namespace UserCode.PlayerHandling;


[Component]
public unsafe struct Cuppy
{
    // A charged multiplier
    // of the player character's
    // jump strength
    public float jumpStrength;


    // The additional strength of the
    // character's jump, that is
    // charged up
    const float additionalJumpPower = 15f;


    // The minimum strength of
    // the character's jump
    const float minJumpStrength = 10f;


    // The speed multiplier
    // of the character's
    // jump strength
    const float buildupMultiplier = 2f;


    // The threshold for a shallow angle.
    // It poses as a limit for where you
    // can jump towards
    const float shallowAngleThreshold = 90f;


    // The time frame, where the jump
    // of the cuppy is being buffered
    const float jumpBufferWindow = 0.2f;


    // The time frame, where the super
    // jump of the cuppy counts
    const float superJumpWindow = 0.1f;


    // The camera that the
    // player sees through
    public int cam;

    // The cursor that the
    // player controls with
    public int cursor;

    // The visual part
    // of the cuppy
    public int visualPart;


    // A smart array holding
    // the shards of the cuppy
    public int* shards;


    // The normal of the
    // surface that the
    // cuppy is on
    public Vector2 jumpNormal;

    // A timer for jump buffering
    public float jumpBufferTimer;


    // A boolean to check,
    // if cuppy has shattered
    public bool isDead;


    static Cuppy()
    {
        playerCMat = Collider.CreateColliderMaterial(1, -1, &PlayerReactions.playerEnter, &PlayerReactions.playerStay, &PlayerReactions.playerExit);

        playerRBMat = RigidBody.MakeRigidBodyMaterial(0, 1);


        shardCMat = Collider.CreateColliderMaterial(1, -1, &ShardReactions.ShardEnter, &ShardReactions.ShardStay, &ShardReactions.ShardExit);

        shardRBMat = RigidBody.MakeRigidBodyMaterial(0.25f, 0.75f);


        cursorTex = Sprite.LoadTexture("Tex/PlayPointer.png");


        hitSound = SourceObject.CreateAudioObject("Sounds/HitSound.ogg");


        cuppyAnim = Sprite.LoadAtlas("Animations/CuppyAnim.png", 48);
    }


    private static int playerCMat;

    private static int playerRBMat;


    private static int shardCMat;

    private static int shardRBMat;


    private static int cursorTex;


    private static int hitSound;


    private static NA<int> cuppyAnim;



    [ComponentInitialise]
    public static void Initialize(int self)
    {
        Cuppy* cup = ECSSHandler.GetComponent<Cuppy>(self);


        Gymbal.CreateTransform(self, (1, 1, 1), (0, 0, 0), (WorldHandler.Spawn.X, WorldHandler.Spawn.Y, 0));

        Collider.CreateCollider(self, [(-1, 1), (1, 1), (1, -1), (-1, -1)], playerCMat);

        RigidBody.CreateRigidBody(self, playerRBMat, 7.5f);

        ECSSHandler.AddComponent(self, new SourceObject(hitSound, 0, 1, 1, 10, 10, 1, false));


        cup->jumpBufferTimer = -1;



        cup->visualPart = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(self, cup->visualPart);

        Gymbal.CreateTransform(cup->visualPart, (3, 3, 1), (0, 0, 0), (0, 0, -1));

        ECSSHandler.AddComponent(cup->visualPart, new Sprite()
        {
            TextureObjectIndex = 0,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 255,
        });

        ECSSHandler.AddComponent(cup->visualPart, new SpriteAnimator(cuppyAnim, 0.1f, AnimationAttributes.Loop, AnimationState.Stopped));


        cup->cam = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(self, cup->cam);

        Gymbal.CreateTransform(cup->cam, (1, 1, 1), (0, 0, 0), (0, 0, 10));

        ECSSHandler.AddComponent(cup->cam, new Camera()
        {
            FOV = 90,

            ProjectionSize = 30,

            NearClip = 0.1f,

            FarClip = 100f,

            IsOrtho = true
        });


        cup->cursor = ECSSHandler.CreateEntity();

        ECSSHandler.BindChild(self, cup->cursor);

        Gymbal.CreateTransform(cup->cursor, (0.5f, 0.5f, 1), (0, 0, 0), (0, 0, 2));

        ECSSHandler.AddComponent(cup->cursor, new Sprite()
        {
            TextureObjectIndex = cursorTex,

            Red = 255,

            Green = 255,

            Blue = 255,

            Alpha = 255,
        });


        cup->shards = SmartPointer.CreateSmartPointer<int>(25);

        for(int i = SmartPointer.GetSmartPointerLength(cup->shards) - 1; i > -1; i--)
        {
            cup->shards[i] = ECSSHandler.CreateEntity();

            Gymbal.CreateTransform(cup->shards[i], (.25f, .25f, 1), (0, 0, 0), (0, 0, -1));

            Collider.CreateCollider(cup->shards[i], [(-1, 1), (1, 1), (1, -1), (-1, -1)], shardCMat);

            RigidBody.CreateRigidBody(cup->shards[i], shardRBMat, 2f);

            ECSSHandler.AddComponent(cup->shards[i], new SourceObject(hitSound, 0, 1, 0.25f, 10, 10, 1, false));

            ECSSHandler.AddComponent(cup->shards[i], new Sprite()
            {
                TextureObjectIndex = 0,

                Red = 0,

                Green = 0,

                Blue = 255,

                Alpha = 255,
            });

            ECSSHandler.SetEntityState(cup->shards[i], false);
        }


        Resize();
    }


    [ComponentFinalise]
    public static void Finalize(int self)
    {

    }


    private static float delta;

    [ComponentUpdate]
    public static void Update()
    {
        Cuppy* cuppies;

        int* cuppyEntities;

        int cuppyLength;


        ECSSHandler.GetCompactColumn(&cuppies, &cuppyEntities, &cuppyLength);


    	delta = ECSSHandler.GetDeltaTime() * ECSSHandler.GetTimeScale();


        for(int i = 1; i < cuppyLength; i++)
        {
            // If the current cuppy
            // is invalid... 
            if(cuppyEntities[i] == 0)
                // Skip to the
                // next iteration
                continue;

            // If the current cuppy
            // is disabled or invalid...
            if(!ECSSHandler.GetEnableState(cuppyEntities[i]))
                // Skip to the
                // next iteration
                continue;


            // Get a direct pointer
            // of the current cuppy.
            // Might not be very thread safe,
            // but that's a problem for the next
            // engine iteration
            Cuppy* cup = &cuppies[i];
        

            // Get the method signature of the dead method,
            // if the cuppy is shattered
            nint handleAddress = (nint)(delegate*<int, Cuppy*, void>)&deadHandle * *(byte*)&cup->isDead;

            // Get the method signature of the alive method,
            // if the cuppy is not shattered
            handleAddress += (nint)(delegate*<int, Cuppy*, void>)&aliveHandle * (*(byte*)&cup->isDead ^ 0x01);


            // Finally, call the method
            // that has been made out
            ((delegate*<int, Cuppy*, void>)handleAddress)(cuppyEntities[i], cup);
        }
    }

        // The behaviour of the
        // cuppy for when it's
        // alive
        private static void aliveHandle(int self, Cuppy* cup)
        {
            // Check if the jump buffer counter
            // should be counted
            bool shouldCount = cup->jumpBufferTimer >= 0 && cup->jumpBufferTimer < jumpBufferWindow;
            
            cup->jumpBufferTimer += delta * *(byte*)&shouldCount;


            bool shouldPrepare = !KBMInput.IsHeld((int)MouseButton.Left) && (cup->jumpStrength != 0) && cup->jumpBufferTimer < 0 && cup->jumpNormal == (0, 0);

            cup->jumpBufferTimer += *(byte*)&shouldPrepare;


            bool shouldReset = KBMInput.IsHeld((int)MouseButton.Left) || (cup->jumpBufferTimer >= jumpBufferWindow);

            cup->jumpBufferTimer = cup->jumpBufferTimer * (*(byte*)&shouldReset ^ 0x01) + -1 * *(byte*)&shouldReset;


            // Charge the strength of
            // the cuppy, if necessary

            bool shouldCharge = (KBMInput.IsHeld((int)MouseButton.Left) || shouldCount) && (cup->jumpStrength < 1);

            cup->jumpStrength += delta * (buildupMultiplier * *(byte*)&shouldCharge);


            // Clamp the chargeup

            bool greaterThanOne = cup->jumpStrength > 1;

            cup->jumpStrength -= cup->jumpStrength % 1 * *(byte*)&greaterThanOne;


            // Call the movement code,
            // if it is necessary

            if(!KBMInput.IsHeld((int)MouseButton.Left) && cup->jumpStrength > 0)
                MoveCup(self, cup);


            // Update the state
            // of the cursor

            cursorMove(self, cup);

            cursorLooks(self, cup);
        }


        // The behaviour of the
        // cuppy for when it's
        // shattered
        private static void deadHandle(int self, Cuppy* cup)
        {
            // Check if the key for
            // revival has been pressed
            bool isInput = KBMInput.IsHeld((int)Keys.Enter);


            if(!isInput)
                return;


            cup->isDead = false;


            // Reenable the visual cuppy
            ECSSHandler.SetEntityState(cup->visualPart, true);

            // Reenable the cursor
            ECSSHandler.SetEntityState(cup->cursor, true);

            // Set it's position to it's spawn
            ECSSHandler.GetComponent<Translation>(self)->Translations.Xy = WorldHandler.Spawn;

            // Reset it's velocity
            ECSSHandler.GetComponent<RigidBody>(self)->_linearVelocity = (0, 0);


            RigidBody* rbC = ECSSHandler.GetComponent<RigidBody>(self);

            rbC->rigidBodyAttribs = RigidBodyAttrib.None;


            Collider* col = ECSSHandler.GetComponent<Collider>(self);

            col->colliderAttribs = ColliderAttrib.None;  


            for(int i = 0; i < SmartPointer.GetSmartPointerLength(cup->shards); i++)
            {   
                RigidBody* rb = ECSSHandler.GetComponent<RigidBody>(cup->shards[i]);

                rb->_linearVelocity = (0, 0);


                ECSSHandler.SetEntityState(cup->shards[i], false);
            }
        }


        private Vector2 bound;

        private static void cursorMove(int self, Cuppy* cup)
        {
            // Move the cursor by the
            // delta of the mouse

            Translation* curTran = ECSSHandler.GetComponent<Translation>(cup->cursor);

            curTran->Translations.Xy += KBMInput.CursorPositionDelta * KBMInput.CursorSensitivity;


            // Clamp the position of the
            // cursor to the frustum of
            // the camera

            curTran->Translations.Xy = Vector2.Clamp(curTran->Translations.Xy, -cup->bound, cup->bound);


            // Rotate the cursor in a way, that
            // it points away from the player

            Translation* pTran = ECSSHandler.GetComponent<Translation>(self);

            Vector2 relCurTran = Gymbal.GetRelativeTranslation(cup->cursor).Xy;

            Rotation* rot = ECSSHandler.GetComponent<Rotation>(cup->cursor);

            float a = angle(Vector2.UnitY, relCurTran - pTran->Translations.Xy);

            if(pTran->Translations.X < relCurTran.X)
                a = -a;

            rot->Rotations.Z = a;
        }

        // Calculates the angle
        // between two vectors
        private static float angle(Vector2 a, Vector2 b)
        {
            float value = MathF.Acos(Vector2.Dot(a, b) / (a.Length * b.Length));

            return value * (180 / MathHelper.Pi);
        }

        // SOme movement code
        // related to cuppy
        private static void MoveCup(int self, Cuppy* cup)
        {
            // If normal is not
            // existent...
            if(cup->jumpNormal == new Vector2(0, 0))
                // Skip to the
                // end of the method
                goto negative;


            Translation* curTran = ECSSHandler.GetComponent<Translation>(cup->cursor);

            // Calculate the Vector that
            // points from the character
            // to the cursor
            Vector2 norm = -curTran->Translations.Xy.Normalized();


            if(norm != norm)
                goto negative;

            if(norm == (0, 0))
                goto negative;


            // Check if the angle for the cuppy
            // to jump off is too shallow relative
            // to the surface. If that is the
            // case, invert the y dimension of
            // the given player's requested vector

            bool isShallow = angle(norm, cup->jumpNormal) < shallowAngleThreshold;

            norm = norm * (*(byte*)&isShallow ^ 0x01) + -Rotate(signedAngle(norm, cup->jumpNormal), cup->jumpNormal) * *(byte*)&isShallow;


            //if((cup->jumpBufferTimer < superJumpWindow) && (cup->jumpBufferTimer > 0) && (cup->jumpStrength >= 1))
            //    cup->jumpStrength = 1.25f;


            // Hurl the cuppy based
            // on the given vector

            RigidBody.AddForce(self, (-norm * additionalJumpPower * cup->jumpStrength) + new Vector2(0, minJumpStrength));


            // Set the volume of the cuppy's
            // output volume

            ECSSHandler.GetComponent<SourceObject>(self)->Volume = 0.5f + cup->jumpStrength * 0.5f;


            cup->jumpBufferTimer = -1;


            // Point for cases
            // where the method
            // has to end wihtout
            // giving a force
            negative:

            if(!(cup->jumpBufferTimer >= 0 && cup->jumpBufferTimer < jumpBufferWindow))
                cup->jumpStrength = 0;
        }


        // Rotates a 2D vector
        // around a certain angle
        private static Vector2 Rotate(float rad, Vector2 vec)
        {
            rad = MathHelper.DegreesToRadians(rad);

            float c = MathF.Cos(rad);

            float s = MathF.Sin(rad);

            return (vec.X * c + vec.Y * -s, vec.X * s + vec.Y * c);
        }


        // Calculates the signed angle
        // between two vectors
        private static float signedAngle(Vector2 a, Vector2 b)
        {
            float val = cross(a, b);

            val /= a.Length * b.Length;

            val = (float)MathHelper.Asin(val);

            return val * (180 / MathHelper.Pi);
        }

        // Calculate crossproduct
        // of two vectors
        private static float cross(Vector2 value1, Vector2 value2)
            => value1.X * value2.Y - value1.Y * value2.X;


        private static void cursorLooks(int self, Cuppy* cup)
        {
            Sprite* sp = ECSSHandler.GetComponent<Sprite>(cup->cursor);

            sp->Blue = (byte)(255 - byteLerp(0, 255, cup->jumpStrength - (cup->jumpStrength % 0.33f)));

            sp->Green = (byte)(255 - byteLerp(0, 100, cup->jumpStrength - (cup->jumpStrength % 0.33f)));
        }


        // Custom linear interpolation for bytes
        private static byte byteLerp(byte value1, byte value2, float amount)
            => (byte)((byte)(value1 * (1.0f - amount)) + (value2 * amount));


    [ComponentResize]
    public static void Resize()
    {
        Cuppy* cuppies;

        int* cuppyEntities;

        int cuppyLength;


        ECSSHandler.GetCompactColumn(&cuppies, &cuppyEntities, &cuppyLength);


        for(int i = 1; i < cuppyLength; i++)
        {   
            Cuppy* cup = ECSSHandler.GetComponent<Cuppy>(cuppyEntities[i]);


            // Calculate the positive bounds
            // of the camera's frustum

            Camera* c = ECSSHandler.GetComponent<Camera>(int.Abs(cup->cam));

            cup->bound = new Vector2(c->ProjectionSize * c->AspectRatio, c->ProjectionSize) * 0.5f;   
        }
    }
}

public unsafe static class PlayerReactions
{
    private static Random rand = new Random();

    public static void playerEnter(int self, int other, Vector2 normal)
    {
        SourceObject* sObj = ECSSHandler.GetComponent<SourceObject>(self);

        sObj->Pitch = 0.75f + rand.NextSingle() * 0.25f;


        sObj->Volume = 1;


        sObj->State = SourceStateFlags.Playing;


        Cuppy* cup = ECSSHandler.GetComponent<Cuppy>(self);

        SpriteAnimator* anim = ECSSHandler.GetComponent<SpriteAnimator>(cup->visualPart);

        anim->State = AnimationState.Paused;



        RigidBody* rb = ECSSHandler.GetComponent<RigidBody>(self);


        bool isDeadly = rb->_linearVelocity.Length > 28;


        if(isDeadly)
        {
            ECSSHandler.SetEntityState(cup->visualPart, false);

            ECSSHandler.SetEntityState(cup->cursor, false);


            Translation* cTR = ECSSHandler.GetComponent<Translation>(self);


            cup->isDead = true;

            cup->jumpBufferTimer = -1;

            cup->jumpStrength = 0;


            SpriteAnimator.StopAnimation(cup->visualPart);     


            Collider* col = ECSSHandler.GetComponent<Collider>(self);

            col->colliderAttribs = ColliderAttrib.NotResolved;


            rb->rigidBodyAttribs = RigidBodyAttrib.NotSimulated;


            for(int i = SmartPointer.GetSmartPointerLength(cup->shards) - 1; i > -1; i--)
            {
                ECSSHandler.SetEntityState(cup->shards[i], true);

                Translation* tr = ECSSHandler.GetComponent<Translation>(cup->shards[i]); 

                tr->Translations.Xy = cTR->Translations.Xy;

            
                RigidBody.AddForce(cup->shards[i], normal * 10 + new Vector2((rand.NextSingle() - 0.5f) * 20, 0));
            } 
        }
    }

    public static void playerStay(int self, int other, Vector2 normal)
    {
        Cuppy* cup = ECSSHandler.GetComponent<Cuppy>(self);


        // Checks, if the given
        // surface can be jumped
        // off of
        cup->jumpNormal = normal;
    }


    public static void playerExit(int self, int other, Vector2 normal)
    {
        Cuppy* cup = ECSSHandler.GetComponent<Cuppy>(self);


        SpriteAnimator* anim = ECSSHandler.GetComponent<SpriteAnimator>(cup->visualPart);

        anim->Attributes |= AnimationAttributes.Loop;

        anim->State = AnimationState.Playing;


        // Disbales the player's
        // ability to jump
        cup->jumpNormal = (0, 0);


        SourceObject* sObj = ECSSHandler.GetComponent<SourceObject>(self);

        sObj->Pitch = 1 + rand.NextSingle() * 0.25f;


        sObj->State = SourceStateFlags.Playing;
    }
}

public unsafe static class ShardReactions
{
    private static Random rand = new Random();

    public static void ShardEnter(int self, int other, Vector2 normal)
    {
        SourceObject* sObj = ECSSHandler.GetComponent<SourceObject>(self);

        sObj->Pitch = 0.9f + rand.NextSingle() * 0.1f;


        sObj->State = SourceStateFlags.Playing;
    }

    public static void ShardStay(int self, int other, Vector2 normal)
    {

    }

    public static void ShardExit(int self, int other, Vector2 normal)
    {

    }
}