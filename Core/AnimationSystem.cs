


using System.Runtime.CompilerServices;
using Core.ECSS;
using Core.MemoryManagement;
using Core.Shimshek;

namespace Core.AnimationSystem;

// A component specifically
// made for animating sprites
[Component]
public unsafe struct SpriteAnimator
{

    // FRAMES

    // The frames to go through
    // for the animation
    private NA<int> frames;


    // TIMING

    // The time it takes
    // for a frame
    public float FrameTime;

    // The total amount of time
    // that progressed through
    // the animation
    private float currentFrameTime;


    // MISC.

    // Unique attributes for
    // the animation component
    public AnimationAttributes Attributes;

    // The current state of the animation
    public AnimationState State;


    // Instance constructor
    public SpriteAnimator(NA<int> animationFrames, float frameTime,
        AnimationAttributes attributes = AnimationAttributes.None, AnimationState state = AnimationState.Stopped)
    {
        frames = animationFrames;

        FrameTime = frameTime;

        Attributes = attributes;

        State = state;
    }


    // A helper method for
    // creating and binding
    // a sprite animator
    public static void CreateSpriteAnimator(int entityID, SpriteAnimator animator)
    {   
        // Add the animator component
        ECSSHandler.AddComponent(entityID, animator);

        // Set the standard texture
        // of the sprite component
        ECSSHandler.GetComponent<Sprite>(entityID)->TextureObjectIndex = animator.frames.Values[0];
    }   


    // Initialization method for
    // the component
    [ComponentInitialise]
    public static void Initialize(int self)
    {



    }


    // Finalization method for
    // the component
    [ComponentFinalise]
    public static void Finalize(int self)
    {



    }


    // The time that passed between
    // the last and current frame
    private static float delta;


    // An updating method
    // that is called every frame
    [ComponentRender]
    public static void Render()
    {
        SpriteAnimator* animators;

        int* animatorEntities;

        int animatorAmount;


        ECSSHandler.GetCompactColumn(&animators, &animatorEntities, &animatorAmount);


        // Precalculate the delta
        // for all upcoming sprite animators
        delta = ECSSHandler.GetDeltaTime() * ECSSHandler.GetTimeScale();


        // Iterate through each
        // sprite animator in a
        // backwards fashion
        for(int i = animatorAmount - 1; i > 0; i--)
        {
            // If the current animator is
            // invalid or disabled...
            if(animatorEntities[i] == 0)
                // Skip to the
                // next iteration
                continue;


            if(!ECSSHandler.GetEnableState(animatorEntities[i]))
                continue;


            // Get the reference of
            // the current animator
            SpriteAnimator* animator = &animators[i];


            // Check, if the animator is playing
            bool check = animator->State == AnimationState.Playing;

            // Get the method for playing behaviour,
            // if the current animator is playing
            nint animMethod = (nint)(delegate*<int, SpriteAnimator*, void>)&__play * *(byte*)&check;


            // Check, if the animator is paused
            check = animator->State == AnimationState.Paused;

            // Get the method for paused behaviour,
            // if the current animator is paused
            animMethod += (nint)(delegate*<int, SpriteAnimator*, void>)&__pause * *(byte*)&check;


            // Check, if the animator is stopped
            check = animator->State == AnimationState.Stopped;

            // Get the method for stopped behaviour,
            // if the current animator is stopped
            animMethod += (nint)(delegate*<int, SpriteAnimator*, void>)&__stop * *(byte*)&check;


            // Finally, call the method
            // that has been determined
            ((delegate*<int, SpriteAnimator*, void>)animMethod)(animatorEntities[i], &animators[i]);
        }
    }

        // Calculates the percentage of the
        // given fraction relative to the total value
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float calcPercentage(float totalValue, float fraction)
            => fraction / totalValue;

        // A hidden method that does all
        // the animating
        private static void __play(int self, SpriteAnimator* animator)
        {
            // Calculate the percentage of
            // the current frame time,
            // relative to the total time
            // of the animation
            float percentage =
                calcPercentage(animator->FrameTime * animator->frames.Length, animator->currentFrameTime);

            // Calculate, which frame is
            // in order now
            int frame = (int)(animator->frames.Length * percentage);

            // Bound safety mechanism
            frame %= animator->frames.Length;


            // Check, if the counter of the
            // current animator should be
            // counted up
            bool shouldCount = animator->currentFrameTime < (animator->FrameTime * animator->frames.Length);


            // Calculate the first case of value
            // to add to the counter
            float valueToCount = -animator->currentFrameTime * (*(byte*)&shouldCount ^ 0x01);

            // Calculate the second case of value
            // to add to the counter
            valueToCount += delta * *(byte*)&shouldCount;


            // Count the animator's timer
            // up, if it is allowed
            animator->currentFrameTime += valueToCount;


            // Negate the shouldCount boolean
            shouldCount = !shouldCount;


            bool isNotLooping = (animator->Attributes & AnimationAttributes.Loop) != AnimationAttributes.Loop;


            // Set the state of the current
            // animator to stopped, if the
            // counting has come to an end
            animator->State -= (AnimationState)(*(byte*)&shouldCount * *(byte*)&isNotLooping);


            // Get a reference to the
            // animator's related sprite
            Sprite* sp = ECSSHandler.GetComponent<Sprite>(self);

            // Set the texture of the
            // sprite to the current frame
            sp->TextureObjectIndex = animator->frames.Values[frame];
        }


        // A hidden method that deals with
        // the animator's stopped behaviour
        private static void __stop(int self, SpriteAnimator* animator)
        {
            // Get a reference to the
            // animator's related sprite
            Sprite* sp = ECSSHandler.GetComponent<Sprite>(self);

            // Set the texture of the
            // sprite to the current frame
            sp->TextureObjectIndex = animator->frames.Values[0];
        }


        // A hidden method that deals with
        // the animator's paused behaviour
        private static void __pause(int self, SpriteAnimator* animator)
        {

        }

    // Stops the sprite animator
    // bound to the given entity
    public static void StopAnimation(int entityID)
    {
        // Get a reference to the
        // given entity's sprite component
        Sprite* sp = ECSSHandler.GetComponent<Sprite>(entityID);

        // Get a reference to the
        // entity's sprite animator
        SpriteAnimator* anim = ECSSHandler.GetComponent<SpriteAnimator>(entityID);

        // Set the texture of the
        // sprite to the first frame
        // of the animation
        sp->TextureObjectIndex = anim->frames.Values[0];

        // Reset the timer of the
        // animation component
        anim->currentFrameTime = 0;
    }


    // Changes the animation of
    // the given sprite animator
    // and stops it
    public static void ChangeAnimation(int entityID, NA<int> nAnimation)
    {
        // Get a reference to the
        // entity's sprite animator
        SpriteAnimator* anim = ECSSHandler.GetComponent<SpriteAnimator>(entityID);

        // Set the animation frames of
        // the sprite animator
        anim->frames = nAnimation;

        // Stop the animator
        anim->State = AnimationState.Stopped;

        // Reset the timer of the
        // animation component
        anim->currentFrameTime = 0;


        // Get a reference to the
        // given entity's sprite component
        Sprite* sp = ECSSHandler.GetComponent<Sprite>(entityID);

        // Set the texture of the
        // sprite to the first frame
        // of the animation
        sp->TextureObjectIndex = anim->frames.Values[0];
    }
}


// An enumerator for
// representing different
// states of an animation
// component
public enum AnimationState : byte
{
    // The animation
    // is not progressing
    // and idles on the
    // first frame.
    Stopped =   0,

    // The animation
    // is progressing
    Playing =   1,

    // The animation
    // is not progressing
    // and is idling
    // on the frame it
    // paused on
    Paused =    2,
}


// An enumerator for
// storing different
// attributes that can
// change the behaviour
// of an animation component
[Flags]
public enum AnimationAttributes : byte
{
    // No attribute is present
    None =          0,

    // Linearly interpolates
    // between each frames
    Interpolate =   0b0000_0001,

    // Loops through the
    // animation indefinitely
    Loop =          0b0000_0010,

    // The animation actively
    // moves the origin of the
    // animation, rather than
    // moving around the it
    Additive =  	0b0000_0100,
}