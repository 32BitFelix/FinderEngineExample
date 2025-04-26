

using System.Runtime.InteropServices;
using Core.ECSS;
using Core.MemoryManagement;
using Core.Shimshek;
using Core.TonKlangIO;
using Core.Transformations;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace Core.TonKlang;

// An audio object is
// an ECS style wrapper
// for an openal source
[Component]
public unsafe struct SourceObject
{
    // The initializer
    // of the audio object
    // component type
    [ComponentInitialise]
    public static void Initialize(int entityID)
    {

    }

    // The finalizer
    // of the audio object
    // component type
    [ComponentFinalise]
    public static void Finalize(int entityID)
    {

    }


    // instance constructor
    public SourceObject(int i_audioObject = 0, int i_groupBelonging = 0,
        float i_pitch = 1, float i_volume = 1, float i_referenceDistance = 1,
            float i_maximumDistance = 5, float i_rolloffFactor = 1, bool i_looping = false)
    {
        Source = getSourceID();

        AudioObject = i_audioObject;

        GroupBelonging = i_groupBelonging;

        Pitch = i_pitch;

        Volume = i_volume;

        ReferenceDistance = i_referenceDistance;

        MaximumDistance = i_maximumDistance;

        RolloffFactor = i_rolloffFactor;

        if(i_looping)
            State |= SourceStateFlags.Looping;
    }


    // OPENAL BACKEND


    // type initializer
    static SourceObject()
    {
        initAL();


        // Initialize the
        // pitches array
        pitches = new NA<float>(1);

        // Set the pitch
        // of the default
        // group
        pitches.Values[0] = 1;


        // Initialize the
        // volumes array
        volumes = new NA<float>(1);

        // Set the volum
        // of the default
        // group
        volumes.Values[0] = 1;



        // Intialize the array
        // that holds the source changes
        sourceChanges = new NA<SourceChangeHolder>(0);

        // Initialize the array
        // that holds the sources
        sources = new NA<int>(0);

        // Create a default audio object

        // Create the buffer of the
        // default audio object
        int defBuf = AL.GenBuffer();

        // Define the 
        // sound of the
        // buffer
        byte snd = 1;

        // Set the data of the
        // default audio buffer
        AL.BufferData(defBuf, ALFormat.Mono8, &snd, 1, 64640);

        // Initialize the
        // audio object array
        audioObjects = new NA<AudioObject>(1);

        // Set the default audio object
        audioObjects.Values[0] = new AudioObject()
        {
            // Set the default buffer
            buffer = defBuf,

            // No path needed
            // for this one
            path = null
        };
    }

        // Initializes the
        // openal library
        private static void initAL()
        {
            // The array of
            // possible devices
            // to output from
            string[] deviceNames =
            [
                "OpenAL Soft",
                "Generic Software",
                "Generic Hardware"
            ];
            
            // Iterate through each device
            for (int j = 0; j < deviceNames.Length; j++)
            {
                // Try to open the
                // current device
                device = ALC.OpenDevice(deviceNames[j]);

                // If connecting was
                // successful...
                if (device != ALDevice.Null)
                    // End the loop
                    break;
            }


            // Allocate an int array
            // that'll hold attributes
            // for the new context 
            int* newAttribs = stackalloc int[3];

            // Set the ID of the attribute,
            // so that the context knows
            // what should be set.
            // In this case it's the amount
            // of mono sources
            newAttribs[0] = 4112;

            // Set the value of the
            // attribute
            newAttribs[1] = int.MaxValue;

            // Add a null terminator,
            // to tell the context that
            // this is everything of the
            // new context
            newAttribs[2] = 0;


            // Create the new context
            context = ALC.CreateContext(device, newAttribs);

            // Make the new context current,
            // but if that fails...
            if (!ALC.MakeContextCurrent(context))
                // Exit the method
                return;


            // Set the Position of the
            // listener to zero
            AL.Listener(ALListener3f.Position, 0f, 0f, 0f);

            // Set the velocity of the
            // listener to zero
            AL.Listener(ALListener3f.Velocity, 0f, 0f, 0f);

            // Create the orientation of
            // the listener
            float[] orientation = [0, 0, 1, 0, 1, 0];

            // Set the orientation of
            // the listener
            AL.Listener(ALListenerfv.Orientation, orientation);

            // Set the distance model
            // of the listener
            AL.DistanceModel(ALDistanceModel.LinearDistanceClamped);


            // Get the amount of attributes
            // the current context has
            /*int size = ALC.GetInteger(device, AlcGetInteger.AttributesSize);

            // Allocate an array that'll
            // store the attributes
            int* attribs = stackalloc int[size * 3];

            // Get the attributes of the context
            ALC.GetInteger(device, AlcGetInteger.AllAttributes, size, attribs);

            // Iterate through each
            // attribute
            for(int i = 0; i < size; i++)
            {

                // If the attribute
                // 
                if(attribs[i] == 4112)
                {
                    int num = attribs[i + 1];


                }
            }*/
        }


    // A reference to the
    // audio device thta'll
    // output the audio
    private static ALDevice device;

    // The global openal context
    private static ALContext context;


    // SOURCE CHANGES


    // An array that holds clues
    // to changes of source attributes
    private static NA<SourceChangeHolder> sourceChanges;

    // Hidden helper method
    // for adding changes to
    // a source object to the
    // sourcechanges array
    private static void addSourceChange(SourceChangeHolder change)
    {
        int nIndex = sourceChanges.Length;

        // Iterae through each
        // sourcechange element
        for(int i = 0; i < sourceChanges.Length; i++)
        {
            // If the current iteration isn't empty...
            if(sourceChanges.Values[i].ChangeClue != SourceChangeClue.None)
                // Skip to the
                // next iteration
                continue;

            nIndex = i;

            // Prematurely end
            // the loop
            break;
        }

        // Save the new change
        // to the free or new index
        fixed(NA<SourceChangeHolder>* sPtr = &sourceChanges)
            NAHandler.Set(nIndex, change, sPtr);
    }

    // Updates the sources' attributes
    // by taking the sourceChanges array
    // as a reference
    private static void updateSourceChanges()
    {
        // Iterate through each
        // source change...
        for(int i = 0; i < sourceChanges.Length; i++)
        {

            // Get the actual source
            int src = sources.Values[sourceChanges.Values[i].Source];

            // Check what change
            // has been commited
            switch(sourceChanges.Values[i].ChangeClue)
            {
                // If there is no change...
                case SourceChangeClue.None:
                    // Prematurely
                    // end the loop,
                    // as it's assumed
                    // that nothing new
                    // will come after
                    break;

                // If there is a change
                // in reference distance...
                case SourceChangeClue.RefDistance:

                    // Cast the integer data to
                    // a floating point without
                    // any conversion
                    float* refD = (float*)&sourceChanges.Values[i].ClueData;

                    // Set the new refernece distance
                    AL.Source(src, ALSourcef.ReferenceDistance, *refD);

                    // Reset the current source change
                    sourceChanges.Values[i] = default;

                // Skip to the
                // next iteration
                continue;

                // If there is a change
                // in maximum distance...
                case SourceChangeClue.MaxDistance:

                    // Cast the integer data to
                    // a floating point without
                    // any conversion
                    float* maxD = (float*)&sourceChanges.Values[i].ClueData;

                    // Set the new maximum distance
                    AL.Source(src, ALSourcef.MaxDistance, *maxD);

                    // Reset the current source change
                    sourceChanges.Values[i] = default;

                // Skip to the
                // next iteration
                continue;

                // If there is a change
                // in the rolloff factor...
                case SourceChangeClue.RolloffFactor:

                    // Cast the integer data to
                    // a floating point without
                    // any conversion
                    float* rollO = (float*)&sourceChanges.Values[i].ClueData;

                    // Set the new rolloff factor
                    AL.Source(src, ALSourcef.RolloffFactor, *rollO);

                    // Reset the current source change
                    sourceChanges.Values[i] = default;

                // Skip to the
                // next iteration
                continue;

                // If there is a change
                // in state...
                case SourceChangeClue.State:

                    if(((SourceStateFlags)sourceChanges.Values[i].ClueData & SourceStateFlags.Stopped) == SourceStateFlags.Stopped)
                        AL.SourceStop(src);

                    if(((SourceStateFlags)sourceChanges.Values[i].ClueData & SourceStateFlags.Playing) == SourceStateFlags.Playing)
                        AL.SourcePlay(src);

                    if(((SourceStateFlags)sourceChanges.Values[i].ClueData & SourceStateFlags.Paused) == SourceStateFlags.Paused)
                        AL.SourcePause(src);


                    // This is unnecessarily complicated.
                    // Why didn't openal use buffers for
                    // the attributes like opengl too?

                    // If the engine side looping state doesn't align with
                    // the openal side looping state...
                    if(AL.GetSource(src, ALSourceb.Looping) !=
                        (((SourceStateFlags)sourceChanges.Values[i].ClueData & SourceStateFlags.Looping) == SourceStateFlags.Looping))
                        // Update the looping state
						AL.Source(src, ALSourceb.Looping, ((SourceStateFlags)sourceChanges.Values[i].ClueData & SourceStateFlags.Looping) == SourceStateFlags.Looping);

                    // Reset the current source change
                    sourceChanges.Values[i] = default;


                // Skip to the
                // next iteration
                continue;

                // If there is a change
                // in audio object...
                case SourceChangeClue.AudioObject:

                    // If the source already has the
                    // given buffer bound...
                    if(AL.GetSource(src, ALGetSourcei.Buffer) ==
                        sourceChanges.Values[i].ClueData)
                        // Skip to the
                        // next iteration
                        continue;

                    // Get the state of the
                    // source before the
                    // changing of the buffer
                    int initialState =
                        AL.GetSource(src, ALGetSourcei.SourceState);

                    // Stop the source,
                    // if it is playing,
                    // to make sure nothing
                    // wrong happens with the
                    // buffer changing
                    AL.SourceStop(src);

                    // Get the desired buffer
                    int buffer = audioObjects.Values[sourceChanges.Values[i].ClueData].buffer;

                    // Change the buffer
                    AL.Source(src, ALSourcei.Buffer, buffer);


                    // If the state of the
                    // source before the
                    // change of the buffer
                    // was playing...
                    if(initialState == 4114)
                        // Play the source again
                        AL.SourcePlay(src);


                    // Reset the current source change
                    sourceChanges.Values[i] = default;


                // Skip to the
                // next iteration
                continue;
            }

        }
    }


    // PITCH AND VOLUME GROUPS


    // Groups in this case are
    // seen as sources that share
    // a pitch modifier.
    // They still have their own
    // pitch, but the group pitch
    // is added to the equation, Like:
    //
    //  pitch = sourcepitch * grouppitch
    //
    // This ensures consistency and ease
    // for grouping sources together, like
    // background music. Kinda like a mixer

    // The array to hold the
    // pitch of groups
    private static NA<float> pitches;

    // Set the pitch of a group
    public static void SetGroupPitch(int groupID, float value)
        => pitches.Values[groupID] = value;

    // Get the pitch of a group
    public static float GetGroupPitch(int groupID)
        => pitches.Values[groupID];


    // The array to hold the
    // volume of groups
    private static NA<float> volumes;

    // Set the volume of a group
    public static void SetGroupVolume(int groupID, float value)
        => volumes.Values[groupID] = value;

    // Get the volume of a group
    public static float GetGroupVolume(int groupID)
        => volumes.Values[groupID];


    // AUDIO OBJECTS


    // The array of audio objects.
    // Audio objects are used for
    // initializing and wrapping
    // openal buffers
    private static NA<AudioObject> audioObjects;


    // Helper Method for creating
    // an audio object with the
    // given path
    public static int CreateAudioObject(string path)
    {
        // The chache of the index to load
        // the audio object to. Fallback is
        // set to the length of audioObjects
        int nIndex = audioObjects.Length;


        // Iterate through each
        // element
        for(int i = 1; i < audioObjects.Length; i++)
        {   
            // If the current element
            // is already occupied...
            if(audioObjects.Values[i].buffer != 0)
                // Skip to the
                // next iteration
                continue;

            // If the current element
            // isn't initialized, but
            // clearly occupied...
            if(audioObjects.Values[i].buffer == 0 &&
                audioObjects.Values[i].path != null)
                // Skip to the
                // next iteration
                continue;

            // Save the current index
            // as the place to load
            // the new audio object at
            nIndex = i;

            // Prematurely end
            // the loop
            break;
        }


        // Allocate the char array
        // to hold the path
        char* uPath =
            (char*)NativeMemory.Alloc((nuint)((path.Length + 1) * sizeof(char)));

        // Copy the string
        // into the char array
        for(int i = 0; i < path.Length; i++)
            uPath[i] = path[i];

        // Add a nullterminator
        // to the end
        uPath[path.Length] = '\0';

        // Add the new audio object
        // to the list of audioobjects
        fixed(NA<AudioObject>* aPtr = &audioObjects)
            NAHandler.Set(nIndex, new AudioObject(){buffer = 0, path = uPath}, aPtr);

        // Return the index
        // at which the audio
        // object has been
        // stored at
        return nIndex;
    } 

        // Processes audio objects
        private static void processAudioObjects()
        {
            // Iterat through each
            // audio object
            for(int i = 1; i < audioObjects.Length; i++)
            {
                // If the current audioobject
                // has been created...
                if(audioObjects.Values[i].buffer != 0 &&
                    audioObjects.Values[i].path == null)
                    // Skip to the
                    // next iteration
                    continue;

                // Create a buffer
                audioObjects.Values[i].buffer =
                    AudioReader.ReadData(audioObjects.Values[i].path);

                // Free the pointer that
                // held the path
                NativeMemory.Free(audioObjects.Values[i].path);
                
                // Null the reference to the
                // now invalid adress
                audioObjects.Values[i].path = null;
            }
        }


    // The identifier
    // of the openal
    // audio source
    // assigned to
    // the audio object
    public int Source;

    // The pitch of
    // the source
    public float Pitch;

    // The volume of
    // the source
    public float Volume;

    // The ID of the
    // currently used
    // audio object
    private int _audioObjectID;

    public int AudioObject
    {
        set
        {
            addSourceChange(new SourceChangeHolder()
            {
                Source = this.Source,

                ChangeClue = SourceChangeClue.AudioObject,

                ClueData = value,
            });

            _audioObjectID = value;
        }

        get => _audioObjectID;
    }

    // The id of the group
    // the source object belongs to
    public int GroupBelonging;

    // The distance at which the
    // sound can be heard at
    // full volume
    private float _referenceDistance;

    public float ReferenceDistance
    {
        set
        {
            addSourceChange(new SourceChangeHolder()
            {
                Source = this.Source,

                ChangeClue = SourceChangeClue.RefDistance,

                ClueData = *(int*)&value,
            });

            _referenceDistance = value;
        }

        get => _referenceDistance;
    }

    // The distance at which the
    // source can be heard at
    private float _maximumDistance;

    public float MaximumDistance
    {
        set
        {
            addSourceChange(new SourceChangeHolder()
            {
                Source = this.Source,

                ChangeClue = SourceChangeClue.MaxDistance,

                ClueData = *(int*)&value,
            });

            _maximumDistance = value;
        }

        get => _maximumDistance;
    }

    // The coefficient at how
    // much the sound's volume
    // falls off from the distance
    private float _rolloffFactor;

    public float RolloffFactor
    {
        set
        {
            addSourceChange(new SourceChangeHolder()
            {
                Source = this.Source,

                ChangeClue = SourceChangeClue.RolloffFactor,

                ClueData = *(int*)&value,
            });

            _rolloffFactor = value;
        }

        get => _rolloffFactor;
    }

    // The state of the
    // source object
    private SourceStateFlags _state;

    public SourceStateFlags State
    {
        set
        {
            addSourceChange(new SourceChangeHolder()
            {
                Source = this.Source,

                ChangeClue = SourceChangeClue.State,

                ClueData = (int)value,
            });

            _state = value;
        }

        get => _state;
    }


    // SOURCE COLLECTION


    // The array to hold the
    // sources that were created
    private static NA<int> sources;

    // Hidden helper method
    // for creating a source
    // and it's index
    private static int getSourceID()
    {
        // A cache for storing the
        // index to save the new
        // source at. Fallback is
        // set to the length of
        // the sources array
        int nIndex = sources.Length;

        // Iterate through each
        // element...
        for(int i = 0; i < sources.Length; i++)
        {
            // Skip to the next iteration,
            // if the current index is not zero
            if(sources.Values[i] != 0)
                continue;

            // Save the current index
            // as the index to save
            // the new source at.
            // After that, prematurely
            // end the loop

            nIndex = i;

            break;
        }

        // Add the new source
        // to the list.
        // (Make it clear that
        // it's a valid source,
        // by giving it the
        // value of -1)
        fixed(NA<int>* sPtr = &sources)
            NAHandler.Set(nIndex, -1, sPtr);

        // Return the index,
        // at which the new
        // source is being
        // stored at
        return nIndex;
    }

    // Processes the sources from
    // the sources array
    private static void processSources()
    {

        for(int i = 0; i < sources.Length; i++)
        {
            // If current source is
            // already initialized...
            if(sources.Values[i] > 0)
                // Skip to the next value
                continue;

            // If current source is
            // not valid or occupied
            if(sources.Values[i] == 0)
                // Skip to the next value
                continue;

            // Generate a new source for
            // the current iteration
            sources.Values[i] = AL.GenSource();
        }

    }


    // RENDER LOOP


    // This is called every
    // render frame
    [ComponentRender]
    public static void Render()
    {
        processSources();

        processAudioObjects();

        updateSourceChanges();


        Matrix4 ln = Matrix4.Zero;

        getListenerMatrix(&ln);


        SourceObject* sourceObjs;

        int* sourceObjEnts;

        int sourceObjLen;


        ECSSHandler.GetCompactColumn(&sourceObjs, &sourceObjEnts, & sourceObjLen);


        for(int i = 1; i < sourceObjLen; i++)
        {   
            if(!ECSSHandler.GetEnableState(sourceObjEnts[i]))
                continue;
        
            if(!ECSSHandler.ContainsComponent<Scale>(sourceObjEnts[i]))
                continue;
                

            // Get the actual source for the
            // current source object
            int src = sources.Values[sourceObjs[i].Source];


            // Get the group id that the
            // source belongs to
            int grp = sourceObjs[i].GroupBelonging;


            // Position
            Vector4 differencePos =
                -new Vector4(Gymbal.GetRelativeTranslation(sourceObjEnts[i]), 1) * ln;

            AL.Source(src, ALSource3f.Position, differencePos.X, differencePos.Y, differencePos.Z);    


            // Pitch
            float desiredPitch = sourceObjs[i].Pitch * pitches.Values[grp] * ECSSHandler.GetTimeScale();

            if(AL.GetSource(src, ALSourcef.Pitch) != desiredPitch)
                AL.Source(src, ALSourcef.Pitch, desiredPitch); 


            // Volume
            float desiredVolume = sourceObjs[i].Volume * volumes.Values[grp];

            if(AL.GetSource(src, ALSourcef.Gain) != desiredVolume)
                AL.Source(src, ALSourcef.Gain, desiredVolume);
        }
    }

        private static void getListenerMatrix(Matrix4* listener)
        {
            Camera* cams;

            int* camEnts;

            int camLen;


            ECSSHandler.GetCompactColumn(&cams, &camEnts, &camLen);


            // The amount of listeners
            // ignored due to their
            // entities being disabled
            int ignoredListeners = 0;


            // Iterate through each
            // listener...
            for(int i = 1; i < camLen; i++)
            {
                // If the entity of
                // the current listener
                // is disabled...
                if(!ECSSHandler.GetEnableState(camEnts[i]))
                {
                    // Increment the
                    // amount of ignored
                    // listeners
                    ignoredListeners++;

                    // Skip to the
                    // next iteration
                    continue;
                }

                // Add the current
                // instances's model
                // matrix to the
                // average view matrix cache
                *listener += Gymbal.GetViewMatrix(camEnts[i]);
            }

            // Set the inverse divisor
            // for the average
            Vector4 invDivisor = new Vector4(1f / (camLen - ignoredListeners - 1));


            // Multiply the sums of
            // all listener's
            // view matrices by the
            // inverse divisor
            listener->Row0 *= invDivisor;

            listener->Row1 *= invDivisor;

            listener->Row2 *= invDivisor;

            listener->Row3 *= invDivisor;
        }
}


// Holds the changes on
// a audio source's
// attributes
public unsafe struct SourceChangeHolder
{
    // The source that is
    // affected
    public int Source;

    // The attribute of the
    // source that is affected
    public SourceChangeClue ChangeClue;

    // The new data for the
    // attribute
    public int ClueData;
}

// An enum that holds flags
// for the possible changes
// on the attributes of an
// openal source
public enum SourceChangeClue : byte
{
    // Nothing has
    // been changed
    None = 0,

    // The reference distance
    // has been changed
    RefDistance = 3,

    // The max distance
    // has been changed
    MaxDistance = 4,

    // The rolloff factor
    // has been changed
    RolloffFactor = 5,

    // The state of the
    // audio has been
    // changed
    State = 6,

    // The audioobject
    // of the source
    // has been changed
    AudioObject = 7,
}

// Special flags
// that specify
// the current
// state of the
// audiosource
[Flags]
public enum SourceStateFlags : byte
{
    // The source
    // has finished
    // playing the
    // audio or is
    // in initial state
    Stopped = 1,

    // The source is
    // currently playing
    // the current audio
    Playing = 2,

    // The source has
    // paused playing
    // the current audio
    Paused = 4,

    // The source will
    // play the audio
    // indefinitely,
    // if it is playing
    Looping = 8,
}


// A wrapper for the
// openal buffer
public unsafe struct AudioObject
{
	// The ID of an
    // openal buffer
	public int buffer;

	// The path of the
    // audio file
	public char* path;
}