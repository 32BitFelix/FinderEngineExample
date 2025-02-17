
using System.Reflection;
using System.Runtime.InteropServices;
using Core.MemoryManagement;

namespace Core.ECSS;

// TODO:
// Change the basic ECS
// to archetypal ECS

// TODO 2:
// Add a system for
// ouputting and
// tracking errors

// This system aims to achieve
// the best of both scenes and
// the entity component system.
// Thanks to reflection, it is
// capable of dynamically accounting
// for user made components,
// without having to rewrite the
// system itself, or manually
// adding the components
// through methods
public static unsafe class ECSSHandler
{
    // static constructor
    static ECSSHandler()
    {
        // Set the amount of
        // fixed updates per
        // second
        FixedUpdatesPerSecond = 30;

        // Get the behaviours
        // of the components
        behaviours = new ColumnBehaviours(ECSSTemplate.Clues);

        // Get all types in
        // the currently executing
        // assembly
        foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            // If the current iteration
            // does not have the scene
            // attribute...
            if(type.GetCustomAttribute<SceneAttribute>() == null)
                // Skip to the next
                // iteration
                continue;

            // If the current iteration
            // does not have the startup
            // attribute...
            if(type.GetCustomAttribute<StarterAttribute>() == null)
                // Skip to the next
                // iteration
                continue;


            // Use the built in
            // scene creator,
            // because it's convenient
            AddScene(type);
        }
    }


    // Adds a scene
    // to the collection
    public static void AddScene(Type type)
    {
        // If the cgiven type
        // does not have the scene
        // attribute...
        if(type.GetCustomAttribute<SceneAttribute>() == null)
            // Exit the method
            return;

        // Create a cache for the
        // index of the new scene.
        // Add a fallback that is
        // the length of the
        // unique id array
        int nIndex = UniqueSceneIDs.Length == 0 ? 1 :
            UniqueSceneIDs.Length;

        // Fix the unique id array,
        // because the compiler
        // asked nicely
        fixed(NA<int>* usidPtr = &UniqueSceneIDs)
        {
            // Iterate through
            // the unique scene ids
            for(int i = 1; i < UniqueSceneIDs.Length; i++)
            {
                // If current iteration
                // is zero...
                if(NAHandler.Get(i, usidPtr) != 0)
                    // Skip to next
                    // iteration
                    continue;

                // Save the current
                // iteration as the new
                // index
                nIndex = i;

                // Break out of
                // the loop
                break;
            }

            // Set the unique ID of
            // the scene
            NAHandler.Set(nIndex, type.GetHashCode(), usidPtr);
        }

        // Fix the array . . .
        fixed(NA<nint>* ssPtr = &SceneStarts)
            // Reserve space
            // at the start array
            NAHandler.Set(nIndex, 0, ssPtr);

        // Fix the array . . .
        fixed(NA<nint>* sfuPtr = &SceneFixedUpdates)
            // Reserve space
            // at the fixed update array
            NAHandler.Set(nIndex, 0, sfuPtr);

        // Fix the array . . .
        fixed(NA<nint>* suPtr = &SceneUpdates)
            // Reserve space
            // at the update array
            NAHandler.Set(nIndex, 0, suPtr);

        // Fix the array . . .
        fixed(NA<nint>* srPtr = &SceneRenders)
            // Reserve space
            // at the render array    
            NAHandler.Set(nIndex, 0, srPtr);

        // Fix the array . . .
        fixed(NA<nint>* srsPtr = &SceneResizes)
            // Reserve space
            // at the resize array
            NAHandler.Set(nIndex, 0, srsPtr);

        // Fix the array . . .
        fixed(NA<nint>* sePtr = &SceneEnds)
            // Reserve space
            // at the end array
            NAHandler.Set(nIndex, 0, sePtr);

        // Fix the array . . .
        fixed(NA<float>* sdtPtr = &SceneDeltaTimes)
            // Reserve space
            // at the delta time array
            NAHandler.Set(nIndex, 0, sdtPtr);

        // Fix the array . . .
        fixed(NA<float>* stsPtr = &SceneTimeScales)
            // Reserve space
            // at the time scale array
            NAHandler.Set(nIndex, 1, stsPtr);

        // Fix the array . . .
        fixed(NA<bool>* asaPtr = &AreScenesActive)
            // Reserve space
            // at the are active array
            NAHandler.Set(nIndex, true, asaPtr);

        // Iterate through the
        // clues of the ECSS template
        for(int i = 0; i < ECSSTemplate.Clues.Length; i++)
        {
            // Create a cache
            // to store the index
            // to put the current
            // component column at.
            // A fallback has been
            // set that is the length
            // of the column array
            int columnIndex = Components.Length;

            // Iterate through
            // the column array
            for(int j = 0; j < Components.Length; j++)
            {
                // If the current iteration's
                // type id is not zero...
                if(Components.Values[j].TypeID != 0)
                    // Skip to the
                    // next iteration
                    continue;

                // Svae the current
                // index of the iteration
                columnIndex = j;

                // Break out
                // of the loop
                break;
            }

            // Fix the column array . . .
            fixed(NA<ComponentColumn>* cc = &Components)
                // Add the new column
                NAHandler.Set(columnIndex, new ComponentColumn(ECSSTemplate.Clues.Values[i], nIndex), cc);
        }

        // Fix the array . . .
        fixed(NA<EntityCollection>* entities = &Entities)
            // Set the entity collection
            NAHandler.Set(nIndex, new EntityCollection(), entities);

        // Iterate through all methods in
        // the given type
        foreach(MethodInfo m in type.GetMethods())
        {
            // If the method
            // is not static...
            if(!m.IsStatic)
                // Skip to the
                // next iteration
                continue;

            // If the method
            // has the starter
            // attribute...
            if(m.GetCustomAttribute<StartAttribute>() != null)
            {
                // Set the function's pointer into
                // the starter method collection
                // with the newly found index
                SceneStarts.Values[nIndex] = m.MethodHandle.GetFunctionPointer();

                // Skip to the
                // next iteration
                continue;
            }

            // If the method
            // has the fixed update
            // attribute...
            if(m.GetCustomAttribute<FixedUpdateAttribute>() != null)
            {
                // Set the function's pointer into
                // the fixed update method collection
                // with the newly found index
                SceneFixedUpdates.Values[nIndex] = m.MethodHandle.GetFunctionPointer();

                // Skip to the
                // next iteration
                continue;
            }

            // If the method
            // has the update
            // attribute...
            if(m.GetCustomAttribute<UpdateAttribute>() != null)
            {
                // Set the function's pointer into
                // the update method collection
                // with the newly found index
                SceneUpdates.Values[nIndex] = m.MethodHandle.GetFunctionPointer();

                // Skip to the
                // next iteration
                continue;
            }

            // If the method
            // has the render
            // attribute...
            if(m.GetCustomAttribute<RenderAttribute>() != null)
            {
                // Set the function's pointer into
                // the render method collection
                // with the newly found index
                SceneRenders.Values[nIndex] = m.MethodHandle.GetFunctionPointer();

                // Skip to the
                // next iteration
                continue;
            }

            // If the method
            // has the resize
            // attribute...
            if(m.GetCustomAttribute<ResizeAttribute>() != null)
            {
                // Set the function's pointer into
                // the resize method collection
                // with the newly found index
                SceneResizes.Values[nIndex] = m.MethodHandle.GetFunctionPointer();

                // Skip to the
                // next iteration
                continue;
            }

            // If the method
            // has the end
            // attribute...
            if(m.GetCustomAttribute<EndAttribute>() != null)
            {
                // Set the function's pointer into
                // the end method collection
                // with the newly found index
                SceneEnds.Values[nIndex] = m.MethodHandle.GetFunctionPointer();

                // Skip to the
                // next iteration
                continue;
            }
        }
    }

    // Removes a scene
    // from the collection
    // that matches the
    // given type
    public static void RemoveScene(Type type)
    {
        // If the given type
        // doesn't have the
        // scene attribute...
        if(type.GetCustomAttribute<SceneAttribute>() == null)
            // Exit the method
            return;

        // Make out the unqiue
        // ID of the given scene
        int sceneID = type.GetHashCode();

        // Create a cache of
        // the index
        int Index = 0;

        // Iterate through
        // all unqiue IDs
        for(int i = 1; i < UniqueSceneIDs.Length; i++)
        {
            // If current iteration
            // isn't the same as the
            // ID of t given scene...
            if(UniqueSceneIDs.Values[i] != sceneID)
                // Skip to the
                // next iteration
                continue;

            // Save the current
            // index in the iteration
            Index = i;
        }

        // Set the index of
        // the made out scene
        UniqueSceneIDs.Values[Index] = 0;

        // Cache the end method
        // of the made out scene
        nint end = SceneEnds.Values[Index];

        // If the end method
        // isn't a nllpointer...
        if(end != 0)
            // Call the end
            // method
            ((delegate*<void>)end)();

        // Free the entity
        // collection of
        // th emade out scene
        Entities.Values[Index].Free();

        // Iterate through
        // the column array
        for(int i = 0; i < Components.Length; i++)
        {
            // If the current iteration's
            // scene belonging is not the
            // same as the removed scene...
            if(Components.Values[i].SceneBelonging != Index)
                // Skip to
                // the next iteration
                continue;

            // Free the unmanaged
            // items in the current
            // iteration
            Components.Values[i].Free();

            // Default the current
            // iteration
            Components.Values[i] = default;

            // Break out
            // of the loop
            break;
        }
    }

    // And array to store
    // the unique IDs of
    // the currently existing
    // scenes
    private static NA<int> UniqueSceneIDs;

    // An array to store
    // the start methods
    // of the currently
    // existing scenes
    private static NA<nint> SceneStarts;

    // An array to store
    // the fixed update methods
    // of the currently
    // existing scenes
    private static NA<nint> SceneFixedUpdates;

    // An array to store
    // the update methods
    // of the currently
    // existing scenes
    private static NA<nint> SceneUpdates;

    // An array to store
    // the render methods
    // of the currently
    // existing scenes
    private static NA<nint> SceneRenders;

    // An array to store
    // the resize methods
    // of the currently
    // existing scenes
    private static NA<nint> SceneResizes;

    // An array to store
    // the end methods
    // of the currently
    // existing scenes
    private static NA<nint> SceneEnds;

    // An array to store
    // the current delta
    // time of the currently
    // exitsing scenes
    private static NA<float> SceneDeltaTimes;

    // An array to store
    // the time scale
    // time of the currently
    // exitsing scenes
    private static NA<float> SceneTimeScales;


    public static float GetTimeScale()
        => SceneTimeScales.Values[CurrentScene];

    public static void SetTimeScale(float value)
        => SceneTimeScales.Values[CurrentScene] = value; 


    // An array to store
    // the active states
    // of the currently
    // existing scenes
    private static NA<bool> AreScenesActive;

    // An array to store
    // the entities of
    // each scene
    private static NA<EntityCollection> Entities;

    // An array to store
    // the components of
    // each scene
    private static NA<ComponentColumn> Components;

    // A structure to
    // hold the behaviours
    // of each component type
    private static ColumnBehaviours behaviours;


    // The index of
    // the currently
    // updated scene
    public static int CurrentScene {get; private set;}


    // The delta time
    // of the engine
    public static float GlobalDeltaTime {get; set;}

    // Returns the
    // delta time
    // of the currently
    // run scene
    public static float GetDeltaTime()
    {
        fixed(NA<float>* sdtPtr = &SceneDeltaTimes)
            return NAHandler.Get(CurrentScene, sdtPtr);
    }


    // Does the usual
    // update stuff with
    // scene's and components
    // as soon as it's called
    public static void ECSSUpdate()
    {

        // Iterate through the
        // scenes
        for(int i = 1; i < UniqueSceneIDs.Length; i++)
        {
            // If current scene's id is zero...
            if(UniqueSceneIDs.Values[i] == 0)
                // Skip to the
                // next iteration
                continue;

            // If the current scene
            // is not active...
            if(!AreScenesActive.Values[i])
                // Skip the iteration
                continue;

            // Set the current scene
            CurrentScene = i;

            // Cache the current
            // scene's delta time
            float ts = SceneTimeScales.Values[i];

            // Set the delta time of the
            // current scene
            SceneDeltaTimes.Values[i] = GlobalDeltaTime * ts;

            // Cache adress of
            // the current scene's
            // start method
            nint start = SceneStarts.Values[i];

            // If the start
            // method pointer
            // is not zero...
            if(start != 0)
            {
                // Set the delta time
                // of the scene to zero
                // to give the scene
                // a clean start
                SceneDeltaTimes.Values[i] = 0;

                // Call start
                ((delegate*<void>)start)();

                // Remove the
                // start method's
                // reference
                SceneStarts.Values[i] = 0;

                // Skip to the
                // next iteration
                continue;
            }

            // Cache adress of
            // the current scene's
            // update method
            nint update = SceneUpdates.Values[i];

            // If the update
            // method pointer
            // is not zero...
            if(update != 0)
                // Call the method
                ((delegate*<void>)update)();
        }

        // Iterate through the component
        // collections
        for(int i = 0; i < Components.Length; i++)
        {
            // If the current iteration's
            // type id is not valid...
            if(Components.Values[i].TypeID == 0)
                // Skip to the
                // next iteration
                continue;

            // If the length of the
            // current iteration is
            // zero...
            if(Components.Values[i].Length == 1)
                // Skip to the next iteration
                continue;

            // Set the current scene
            CurrentScene = Components.Values[i].SceneBelonging;
            
            // Create a cache for the
            // behaviour index. Make a
            // fallback of zero
            int behaviourIndex = 0;

            // Iterate through each
            // component behaviour...
            for(int j = 0; j < behaviours.TypeID.Length; j++)
            {
                // If the current iteration
                // is not the same as the
                // current column...
                if(Components.Values[i].TypeID != behaviours.TypeID.Values[j])
                    // Skip to the
                    // next iteration
                    continue;

                // Save the current
                // iteration index as
                // the behaviour index
                behaviourIndex = j;
                
                // Break out of
                // the loop
                break;
            }

            // If the current iteration's
            // update isn't undefined...
            if(behaviours.Update.Values[behaviourIndex] != 0)
                // call the update method
                ((delegate*<void>)behaviours.Update.Values[behaviourIndex])();
        }
    }

    // The delta time
    // of the fixed
    // time step (estimate)
    public static float FixedDeltaTime
        => 1 / (float)FixedUpdatesPerSecond;

    // Stores the amounts
    // of times fixed update
    // should be called
    // between each frame
    public static int FixedUpdatesPerSecond;

    // A hidden counter
    // to see if it's time
    // for a fixed update
    private static float fixedUpdateCounter;

    // Does the fixed
    // update related
    // things if the
    // time has come
    // upon call
    public static void ECSSFixedUpdate()
    {
        // If the fixedupdate
        // counter is less than
        // the interval of each
        // physics update...
        if(fixedUpdateCounter < FixedDeltaTime)
        {
            // Increment the
            // fixed update
            // counter by the
            // engine's delta
            // time
            fixedUpdateCounter += GlobalDeltaTime;

            // Exit the method
            return;
        }

        // Reset the fixed
        // update counter
        fixedUpdateCounter = 0;

        // Iterate through the
        // scenes
        for(int i = 1; i < UniqueSceneIDs.Length; i++)
        {
            // If current scene's id is zero...
            if(UniqueSceneIDs.Values[i] == 0)
                // Skip to the
                // next iteration
                continue;

            // If the current scene
            // is not active...
            if(!AreScenesActive.Values[i])
                // Skip the iteration
                continue;

            // Set the current scene
            CurrentScene = i;

            // Cache adress of
            // the current scene's
            // fixed update method
            nint fixedUpdate = SceneFixedUpdates.Values[i];

            // If the fixed update
            // method pointer
            // is not zero...
            if(fixedUpdate != 0)
                // Call the method
                ((delegate*<void>)fixedUpdate)();
        }

        // Iterate through the component
        // collections
        for(int i = 0; i < Components.Length; i++)
        {
            // If the current iteration's
            // type id is not valid...
            if(Components.Values[i].TypeID == 0)
                // Skip to the
                // next iteration
                continue;

            // If the length of the
            // current iteration is
            // zero...
            if(Components.Values[i].Length == 1)
                // Skip to the next iteration
                continue;

            // Set the current scene
            CurrentScene = Components.Values[i].SceneBelonging;
            
            // Create a cache for the
            // behaviour index. Make a
            // fallback of zero
            int behaviourIndex = 0;

            // Iterate through each
            // component behaviour...
            for(int j = 0; j < behaviours.TypeID.Length; j++)
            {
                // If the current iteration
                // is not the same as the
                // current column...
                if(Components.Values[i].TypeID != behaviours.TypeID.Values[j])
                    // Skip to the
                    // next iteration
                    continue;

                // Save the current
                // iteration index as
                // the behaviour index
                behaviourIndex = j;
                
                // Break out of
                // the loop
                break;
            }

            // If the current iteration's
            // fixed update isn't undefined...
            if(behaviours.FixedUpdate.Values[behaviourIndex] != 0)
                // call the fixed update method
                ((delegate*<void>)behaviours.FixedUpdate.Values[behaviourIndex])();
        }
    }

    // Does the rendering
    // related things
    public static void ECSSRender()
    {
        // Iterate through the
        // scenes
        for(int i = 1; i < UniqueSceneIDs.Length; i++)
        {
            // If current scene's id is zero...
            if(UniqueSceneIDs.Values[i] == 0)
                // Skip to the
                // next iteration
                continue;

            // Set the current scene
            CurrentScene = i;

            // Cache adress of
            // the current scene's
            // render method
            nint render = SceneRenders.Values[i];

            // If the render
            // method pointer
            // is not zero...
            if(render != 0)
                // Call the method
                ((delegate*<void>)render)();
        }

        // Iterate through the component
        // collections
        for(int i = 0; i < Components.Length; i++)
        {
            // If the current iteration's
            // type id is not valid...
            if(Components.Values[i].TypeID == 0)
                // Skip to the
                // next iteration
                continue;

            // If the length of the
            // current iteration is
            // zero...
            if(Components.Values[i].Length == 1)
                // Skip to the next iteration
                continue;

            // Set the current scene
            CurrentScene = Components.Values[i].SceneBelonging;
            
            // Create a cache for the
            // behaviour index. Make a
            // fallback of zero
            int behaviourIndex = 0;

            // Iterate through each
            // component behaviour...
            for(int j = 0; j < behaviours.TypeID.Length; j++)
            {
                // If the current iteration
                // is not the same as the
                // current column...
                if(Components.Values[i].TypeID != behaviours.TypeID.Values[j])
                    // Skip to the
                    // next iteration
                    continue;

                // Save the current
                // iteration index as
                // the behaviour index
                behaviourIndex = j;
                
                // Break out of
                // the loop
                break;
            }

            // If the current iteration's
            // render isn't undefined...
            if(behaviours.Render.Values[behaviourIndex] != 0)
                // call the render method
                ((delegate*<void>)behaviours.Render.Values[behaviourIndex])();
        }
    }

    // Does resizing
    // related things
    public static void ECSSResize(int width, int height)
    {
        //if(UniqueSceneIDs.Length == 0)
        //    return;

        // Iterate through the component
        // collections
        for(int i = 0; i < Components.Length; i++)
        {
            // If the current iteration's
            // type id is not valid...
            if(Components.Values[i].TypeID == 0)
                // Skip to the
                // next iteration
                continue;

            // If the length of the
            // current iteration is
            // zero...
            if(Components.Values[i].Length == 1)
                // Skip to the next iteration
                continue;

            // Set the current scene
            CurrentScene = Components.Values[i].SceneBelonging;
            
            // Create a cache for the
            // behaviour index. Make a
            // fallback of zero
            int behaviourIndex = 0;

            // Iterate through each
            // component behaviour...
            for(int j = 0; j < behaviours.TypeID.Length; j++)
            {
                // If the current iteration
                // is not the same as the
                // current column...
                if(Components.Values[i].TypeID != behaviours.TypeID.Values[j])
                    // Skip to the
                    // next iteration
                    continue;

                // Save the current
                // iteration index as
                // the behaviour index
                behaviourIndex = j;
                
                // Break out of
                // the loop
                break;
            }


            // If the current iteration's
            // render isn't undefined...
            if(behaviours.Resize.Values[behaviourIndex] != 0)
                // call the render method
                ((delegate*<int, int, void>)behaviours.Resize.Values[behaviourIndex])(width, height);
        }

        // Iterate through the
        // scenes
        for(int i = 1; i < UniqueSceneIDs.Length; i++)
        {
            // If current scene's id is zero...
            if(UniqueSceneIDs.Values[i] == 0)
                // Skip to the
                // next iteration
                continue;

            // Set the current scene
            CurrentScene = i;

            // Cache adress of
            // the current scene's
            // resize method
            nint resize = SceneResizes.Values[i];

            // If the resize
            // method pointer
            // is not zero...
            if(resize != 0)
                // Call the method
                ((delegate*<int, int, void>)resize)(width, height);
        }
    }

    // Does the finalisation stuff
    // of the engine when called
    public static void ECSSEnd()
    {

        // Iterate through the
        // scenes
        for(int i = 1; i < UniqueSceneIDs.Length; i++)
        {
            // If current scene's id is zero...
            if(UniqueSceneIDs.Values[i] == 0)
                // Skip to the
                // next iteration
                continue;

            // Set the current scene
            CurrentScene = i;

            // Cache the current
            // scene's delta time
            float ts = SceneTimeScales.Values[i];

            // Set the delta time of the
            // current scene
            SceneDeltaTimes.Values[i] = GlobalDeltaTime * ts;

            // Cache adress of
            // the current scene's
            // end method
            nint end = SceneEnds.Values[i];

            // If the end
            // method pointer
            // is not zero...
            if(end != 0)
                // Call the method
                ((delegate*<void>)end)();
        }
    }

    // Creates an entity
    // within the current
    // scene
    public static int CreateEntity()
    {
        // Cache for the index of the
        // new array. Set the length
        // of the entity array of the
        // current scene as a fallback.
        // If the length is 0, the value
        // 1 will be used instead
        int nIndex = Entities.Values[CurrentScene].IDs.Length == 0 ?
            1 : Entities.Values[CurrentScene].IDs.Length;

        // Iterate through each entity ID
        // in the current scene
        for(int i = 1; i < Entities.Values[CurrentScene].IDs.Length; i++)
        {
            // If the current iteration is
            // not zero...
            if(Entities.Values[CurrentScene].IDs.Values[i] != 0)
                // Skip to the next iteration
                continue;

            // A free space has been found....

            // Save the
            // current index
            // as the new
            // index for the array
            nIndex = i;

            // Break out
            // of the loop
            break;
        }

        // Set the entity's id to the
        // currnetly found index
        NAHandler.Set(nIndex, nIndex, &Entities.Values[CurrentScene].IDs);

        // Set the entity's parent to 0
        NAHandler.Set(nIndex, 0, &Entities.Values[CurrentScene].Parents);

        // Set the entity's children to 0
        NAHandler.Set(nIndex, new NA<int>(), &Entities.Values[CurrentScene].Children);

        // Set the entity's enable state to enbabled
        NAHandler.Set(nIndex, true, &Entities.Values[CurrentScene].EnableState);

        // Set the entity's component record to 0
        NAHandler.Set(nIndex, new ComponentRecord(), &Entities.Values[CurrentScene].ComponentRecords);

        // Return the made out index
        return nIndex;
    }

    // Removes an entity
    // with the fitting
    // id in the current scene
    public static void RemoveEntity(int entityID)
    {
        // Set the entity's id to the
        // currnetly found index
        NAHandler.Set(entityID, 0, &Entities.Values[CurrentScene].IDs);


        // Set the entity's parent to 0
        NAHandler.Set(entityID, 0, &Entities.Values[CurrentScene].Parents);


        // Free the children
        // and the array that
        // tracks them

        // Free the children
        for(int i = 0; i < Entities.Values[CurrentScene].Children.Values[entityID].Length; i++)
            // Get the id of the current
            // child and remove it
            RemoveEntity(Entities.Values[CurrentScene].Children.Values[entityID].Values[i]);

        // Free the array
        NAHandler.Free(&Entities.Values[CurrentScene].Children.Values[entityID]);


        // Set the entity's enable state to enbabled
        NAHandler.Set(entityID, false, &Entities.Values[CurrentScene].EnableState);


        // Iterate through the
        // component records
        // of the given entity
        for(int i = 0; i < Entities.Values[CurrentScene].ComponentRecords.Values[entityID].Length; i++)
        {
            // If the current iteration's
            // saved component type id is
            // zero...
            if(Entities.Values[CurrentScene].ComponentRecords.Values[entityID].IDs[i] == 0)
                // Skip to the
                // next iteration
                continue;

            // Cache the current type id
            // of the iteration
            int currentTypeID = Entities.Values[CurrentScene].ComponentRecords.Values[entityID].IDs[i];

            // Iterate through the
            // behaviours of the
            // component types
            for(int j = 0; j < behaviours.TypeID.Length; j++)
            {
                // If the current type iteration
                // of the behaviour list is not the
                // same as the type id of the current
                // iteration of the component records...
                if(behaviours.TypeID.Values[j] != currentTypeID)
                    // Skip to the
                    // next iteration
                    continue;

                // Call the finaliser
                ((delegate*<int, void>)behaviours.Finalise.Values[j])(entityID);
            }

            // Cache the index of
            // the column that holds
            // the current component
            int columnIndex = 0;

            // Iterate through
            // the columns of
            // the scenes
            for(int j = 0; j < Components.Length; j++)
            {
                // If the current column's
                // scene belonging isn't the
                // same as the current scene
                // or if the current column's
                // type id isn't the same as the
                // given component type id...
                if(Components.Values[j].SceneBelonging != CurrentScene ||
                    Components.Values[j].TypeID != currentTypeID)
                        // Skip to the
                        // next iteration
                        continue;

                // Store the current
                // iteration's index
                columnIndex = j;

                // Break out of
                // the loop
                break;
            }

            // Iterate through the
            // entity ids of the
            // made out column
            for(int j = 0; j < Components.Values[columnIndex].Length; j++)
            {
                // If the current iteration's
                // id is not the same as the
                // given entity id...
                if(Components.Values[columnIndex].EntityIDs[j] != entityID)
                    // Skip to the
                    // next iteration
                    continue;

                // Set the current
                // ietartion to zero
                Components.Values[columnIndex].EntityIDs[j] = 0;

                // Break out of the loop
                break;
            }
        }


        // Free the entity's component record
        Entities.Values[CurrentScene].ComponentRecords.Values[entityID].Free();
    }

    // Binds an entity
    // as a child to 
    // the given parent
    // entity
    public static void BindChild(int parent, int child)
    {
        // If the parent is the same as
        // the child...
        if(parent == child)
            // Exit the
            // method
            return;

        // If the child is already
        // bound to the parent...
        if(Entities.Values[CurrentScene].Parents.Values[child] == parent)
            // Exit the method
            return;

        // Get a cache for
        // the child's index
        // within it's parent's array.
        // Make a fallback already that
        // is the length of the children
        // array of the parent
        int nIndex = Entities.Values[CurrentScene].Children.Values[parent].Length;

        // Iterate through the
        // children array of the
        // parent
        for(int i = 1; i < Entities.Values[CurrentScene].Children.Values[parent].Length; i++)
        {
            // If the current iteratioon
            // is not 0...
            if(Entities.Values[CurrentScene].Children.Values[parent].Values[i] != 0)
                // Skip to the next iteration
                continue;

            // Set the new index
            // of the child to
            // the current index
            nIndex = i;

            // Break out
            // of the loop
            break;
        }

        // Add the parent
        // to the child
        Entities.Values[CurrentScene].Parents.Values[child] = parent;

        // Finally, set the child to
        // the made out index of the
        // parent's children array
        NAHandler.Set(nIndex, child, &Entities.Values[CurrentScene].Children.Values[parent]);
    }

    // Unbinds an entity
    // as a child from the
    // given parent entity
    public static void UnbindChild(int parent, int child)
    {
        // If the parent is the same as
        // the child...
        if(parent == child)
            // Exit the
            // method
            return;

        // If the child isn't
        // already bound to the
        // parent...
        if(Entities.Values[CurrentScene].Parents.Values[child] != parent)
            // Exit the
            // method
            return;

        // Create a cache
        // that stores
        // the whereabouts
        // of the parent's
        // child. Create a
        // fallback at index 0
        int nIndex = 0;

        // Iterate through the children
        // array of the parent
        for(int i = 1; i < Entities.Values[CurrentScene].Children.Values[parent].Length; i++)
        {
            // If the current iteration
            // doesn't match the given
            // id of the child...
            if(Entities.Values[CurrentScene].Children.Values[parent].Values[i] != child)
                // Skip to the
                // next iteration
                continue;

            // Store the current
            // index of the iteration
            nIndex = i;

            // Break out
            // of the loop
            break;
        }

        // Remove the parent
        // from the child
        Entities.Values[CurrentScene].Parents.Values[child] = 0;

        // Set the child
        // at the found index
        // to zero
        NAHandler.Set(nIndex, 0, &Entities.Values[CurrentScene].Children.Values[parent]);
    }

    // Shows the
    // children
    // of the given
    // parent entity
    public static NA<int> ShowChildren(int parent)
        => Entities.Values[CurrentScene].Children.Values[parent];

    // Shows the
    // parent
    // of the given
    // child entity
    public static int ShowParent(int child)
        => Entities.Values[CurrentScene].Parents.Values[child];

    // Adds a component of
    // the specified type
    // to the given entity
    public static void AddComponent<T>(int entityID, T value)
        where T : unmanaged
    {
        // Calculate the unqiue
        // type id of the given component
        int typeID = typeof(T).GetHashCode();

        // If a component of the given type
        // already exists in the given entity...
        if(Entities.Values[CurrentScene].ComponentRecords.Values[entityID].GetIndex(typeID) != 0)
            // Exit the method
            return;

        // Make a cache to
        // store the index
        // of the column to
        // store the component
        // at. Set it to zero
        // as a fallback
        int columnIndex = 0;

        // Iterate through the
        // component columns
        for(int i = 1; i < Components.Length; i++)
        {
            // If the current iteration's
            // scene belonging is not
            // matching the current scene...
            if(Components.Values[i].SceneBelonging != CurrentScene)
                // Skip to the
                // next ietartion
                continue;

            // If the current iteration's
            // type id is not the same as
            // the given component type...
            if(Components.Values[i].TypeID != typeID)
                // Skip to the
                // next iteration
                continue;

            // Set the column index
            // to the current index
            // in the iteration
            columnIndex = i;

            // Break out
            // of the loop
            break;
        }

        // make a cache to
        // store the index
        // of the component
        // for the entity to
        // occupy at
        int componentIndex = Components.Values[columnIndex].Length;

        // Iterate through the entity ids
        for(int i = 1; i < Components.Values[columnIndex].Length; i++)
        {
            // If the current entity id
            // is not zero...
            if(Components.Values[columnIndex].EntityIDs[i] != 0)
                // Skip to the
                // next iteration
                continue;

            // Set the potential index
            // of the component for the
            // entity to occupy at to
            // the current index of the iteration
            componentIndex = i;

            // Break out of
            // the loop
            break;
        }

        // If the potential index
        // of the component goes
        // becond the array
        if(componentIndex >= Components.Values[columnIndex].Length)
        {
            // Increment on the length
            Components.Values[columnIndex].Length++;

            // Resize the array that holds
            // the entity ids
            Components.Values[columnIndex].EntityIDs =
                (int*)NativeMemory.Realloc(Components.Values[columnIndex].EntityIDs, (nuint)(Components.Values[columnIndex].Length * sizeof(int)));

            // Resize the array that holds
            // the components
            Components.Values[columnIndex].Values =
                NativeMemory.Realloc(Components.Values[columnIndex].Values,
                    (nuint)Components.Values[columnIndex].Length * Components.Values[columnIndex].UnmanagedSize);
        }

        // Set the entity ID of the
        // bound component
        Components.Values[columnIndex].EntityIDs[componentIndex] = entityID;

        // Set the value of
        // the found component
        // slot
        ((T*)Components.Values[columnIndex].Values)[componentIndex] = value;

        // Add a record of the component
        // to the given entity
        Entities.Values[CurrentScene].ComponentRecords.Values[entityID].AddRecord(typeID, componentIndex);

        // Create a cache that
        // stores the index of
        // the needed initialiser
        int initIndex = 0;

        // Iterate through the
        // behaviours stored in
        // the behaviour collection
        for(int i = 0; i < behaviours.TypeID.Length; i++)
        {
            // If the current iteration's
            // type id is not the same as
            // the given component type id...
            if(behaviours.TypeID.Values[i] != typeID)
                // Skip to the
                // next iteration
                continue;

            // Store the current
            // index 
            initIndex = i;

            // Break out of the loop
            break;
        } 

        // If the initialiser
        // is not null...
        if(behaviours.Initialise.Values[initIndex] != 0)
            // Call the initialiser
            ((delegate*<int, void>)behaviours.Initialise.Values[initIndex])(entityID);
    }

    // Removes a component of
    // the specified type from
    // the given entity
    public static void RemoveComponent<T>(int entityID)
        where T : unmanaged
    {
        // Calculate the type id
        // of the given component
        int typeID = typeof(T).GetHashCode();

        // If there is no record of
        // a component with a fitting
        // id of the given component type...
        if(Entities.Values[CurrentScene].ComponentRecords.Values[entityID].GetIndex(typeID) == 0)
            // Exit the method
            return;

        // Create a cache that'll hold the
        // index of the component within it's
        // column. Add a fallback of 0
        int componentIndex = 0;

        // Iterate through the component
        // records of the given entity
        for(int i = 0; i < Entities.Values[CurrentScene].ComponentRecords.Values[entityID].Length; i++)
        {
            // If the current iteration's
            // type id matches the type id
            // of the given component type...
            if(Entities.Values[CurrentScene].ComponentRecords.Values[entityID].IDs[i] != typeID)
                // Skip to the
                // next iteration
                continue;

            // Set the index cache
            // to the index of the
            // fitting component record
            componentIndex = Entities.Values[CurrentScene].ComponentRecords.Values[entityID].Indexes[i];
            
            // Break out of
            // the loop
            break;
        }

        // If the potential
        // index is zero...
        if(componentIndex == 0)
            // Exit the method
            return;

        // Remove the record
        Entities.Values[CurrentScene].ComponentRecords.Values[entityID].RemoveRecord(typeID);

        // Create a cache that
        // stores the index of
        // the fitting behaviour.
        // Set it to zero as a fallback
        int behaviourIndex = 0;

        // Iterate through each
        // behaviour type id
        for(int i = 0; i < behaviours.TypeID.Length; i++)
        {
            // If the current iteration
            // is the same as the given
            // component type id...
            if(behaviours.TypeID.Values[i] != typeID)
                // Skip to the
                // next iteration
                continue;

            // Save the current index
            behaviourIndex = i;

            // Break out of
            // the loop
            break;
        }

        // If the finaliser
        // is not null...
        if(behaviours.Finalise.Values[behaviourIndex] != 0)
            // Call the finaliser to the component
            ((delegate*<int, void>)behaviours.Finalise.Values[behaviourIndex])(entityID);

        // Create a cache for 
        // the index of the column.
        // Create a fallback of zero
        int columnIndex = 0;

        // Iterate through the
        // columns
        for(int i = 0; i < Components.Length; i++)
        {
            // If the current iteration's
            // scene belonging is not the same
            // as the current scene or if the
            // current iteration's type id is
            // not the same as the given
            // component type id...
            if(Components.Values[i].SceneBelonging != CurrentScene ||
                Components.Values[i].TypeID != typeID)
                    // Skip to the next iteration
                    continue;

            // Save the index of
            // the current iteration
            columnIndex = i;
            
            // Break out of
            // the loop
            break;
        }

        // Iterate through the
        // entity ids of the
        // foudn out column
        for(int i = 0; i < Components.Values[columnIndex].Length; i++)
        {
            // If the current iteration is not
            // the same as the given entity id...
            if(Components.Values[columnIndex].EntityIDs[i] != entityID)
                // Skip to the
                // next iteration
                continue;

            // Set the current iteration
            // to zero
            Components.Values[columnIndex].EntityIDs[i] = 0;

            // Break out of
            // the loop
            break;
        }
    }

    // Returns the adress
    // of a component of
    // the given type
    // from the given entity
    public static T* GetComponent<T>(int entityID)
        where T : unmanaged
    {
        // Calculate the type id
        // of the given component type
        int typeID = typeof(T).GetHashCode();

        // Get the index
        // at which the
        // component is stored at
        int componentIndex =
            Entities.Values[CurrentScene].ComponentRecords.Values[entityID].GetIndex(typeID);

        // If the index of
        // the component is
        // zero...
        if(componentIndex == 0)
            // Exit with
            // null ptr
            return null;

        // Create a cache of the
        // index of the column that the wanted
        // component is stored at.
        // Make a fallback of zero
        int columnIndex = 0;

        // Iterate through
        // each component column
        for(int i = 0; i < Components.Length; i++)
        {
            // If the current iteration
            // does not have the same type
            // id or if the current iteration
            // does not have the same scene
            // belonging as the current scene...
            if(Components.Values[i].TypeID != typeID ||
                Components.Values[i].SceneBelonging != CurrentScene)
                    // Skip to the
                    // next iteration
                    continue;
            
            // Save the current
            // iteration's index
            // as the index of the
            // column
            columnIndex = i;

            // Break out of
            // the loop
            break;
        }

        // Return the pointer
        // to the value
        return &((T*)Components.Values[columnIndex].Values)[componentIndex];
    }

    // Overloads the
    // given pointer
    // with attributes
    // of the column
    // with the given type
    public static void GetCompactColumn<T>(T** values, int** entities, int* length)
        where T : unmanaged
    {
        // Calculate the type id
        // of the given component type
        int typeID = typeof(T).GetHashCode();

        // Create a cache of the
        // index of the column that the wanted
        // component is stored at.
        // Make a fallback of zero
        int columnIndex = 0;

        // Iterate through
        // each component column
        for(int i = 0; i < Components.Length; i++)
        {
            // If the current iteration
            // does not have the same type
            // id or if the current iteration
            // does not have the same scene
            // belonging as the current scene...
            if(Components.Values[i].TypeID != typeID ||
                Components.Values[i].SceneBelonging != CurrentScene)
                    // Skip to the
                    // next iteration
                    continue;
            
            // Save the current
            // iteration's index
            // as the index of the
            // column
            columnIndex = i;

            // Break out of
            // the loop
            break;
        }


        *length = Components.Values[columnIndex].Length;

        *entities = Components.Values[columnIndex].EntityIDs;

        *values = (T*)Components.Values[columnIndex].Values;
    }

    // Tells if the component
    // of the given type is
    // within the given entity
    public static bool ContainsComponent<T>(int entityID)
        where T : unmanaged
    {
        // Calculate the type id
        // of the given component type
        int typeID = typeof(T).GetHashCode();

        // If the given entity
        // has a record of the
        // given component type...
        if(Entities.Values[CurrentScene].ComponentRecords.Values[entityID].GetIndex(typeID) != 0)
            // Return true
            return true;

        // Return false
        return false;
    }

    // Set the state of the given
    // entity with the given bool
    public static void SetEntityState(int entityID, bool state)
        => Entities.Values[CurrentScene].EnableState.Values[entityID] = state;

    // Get the state of the given
    // entity
    public static bool GetEnableState(int entityID)
        => Entities.Values[CurrentScene].EnableState.Values[entityID];
}

// An attribute to indicate what
// scene should begin at startup
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class StarterAttribute : Attribute;

// An attribute to indicate what
// type is a scene
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SceneAttribute : Attribute;

// An attribute to indicate what
// method in the scene is called
// upon the scene's initialisation
[AttributeUsage(AttributeTargets.Method)]
public class StartAttribute : Attribute;

// An attribute to indicate what
// method in the scene is called
// every fixed time step
[AttributeUsage(AttributeTargets.Method)]
public class FixedUpdateAttribute : Attribute;

// An attribute to indicate what
// method in the scene is called
// every update
[AttributeUsage(AttributeTargets.Method)]
public class UpdateAttribute : Attribute;

// An attribute to indicate what
// method in the scene is called
// every render frame
[AttributeUsage(AttributeTargets.Method)]
public class RenderAttribute : Attribute;

// An attribute to indicate what
// method in the scene is called
// every time the window is resized
[AttributeUsage(AttributeTargets.Method)]
public class ResizeAttribute : Attribute;

// An attribute to indicate what
// method in the scene is called
// upon the scene's finalisation
[AttributeUsage(AttributeTargets.Method)]
public class EndAttribute : Attribute;

// A class to hold
// a precreated template
// of the ecs side of a scene
public static unsafe class ECSSTemplate
{
    // static constructor
    static ECSSTemplate()
    {
        // Initialise the
        // clues array
        Clues = new NA<ComponentClue>();

        // Prepare the currrent component clue
        ComponentClue currentClue = new ComponentClue();

        // Fix the clues array . . .
        fixed(NA<ComponentClue>* cPtr = &Clues)
            // Set the default
            NAHandler.Set(0, default, cPtr);

        // Fix the clues array . . .
        fixed(NA<ComponentClue>* cPtr = &Clues)
            // Iterate through all
            // types in the executing
            // assembly
            foreach(Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                // If the given type
                // is not a value type...
                if(!t.IsValueType)
                    // Skip to the
                    // next iteration
                    continue;

                // If the given type
                // does not have the
                // component attribute...
                if(t.GetCustomAttribute<ComponentAttribute>() == null)
                    // Skip to the
                    // next iteration
                    continue;


                // If as type initializer is defined
                // for this type...
                if(t.TypeInitializer != null)
                    // Call the type initializer
                    _ = t.TypeInitializer.Invoke(null, null);


                // Default the clue
                currentClue = default;

                // Get the size of the
                // type in bytes
                currentClue.unmanagedSize = Marshal.SizeOf(t);

                // calculate a unique ID
                // for the current type
                currentClue.uniqueID = t.GetHashCode();

                // Iterate through all
                // methods in 
                foreach(MethodInfo m in t.GetMethods())
                {
                    // If the current
                    // iteration is not
                    // a static member...
                    if(!m.IsStatic)
                        // Skip to the
                        // next iteration
                        continue;

                    // If the current iteration
                    // is a component initialiser...
                    if(m.GetCustomAttribute<ComponentInitialiseAttribute>() != null)
                    {
                        // Cache the initialiser
                        // of the component type
                        currentClue.initialise = m.MethodHandle.GetFunctionPointer();

                        // Skip to the
                        // next iteration
                        continue;
                    }

                    // If the current iteration
                    // is a component finaliser...
                    if(m.GetCustomAttribute<ComponentFinaliseAttribute>() != null)
                    {
                        // Cache the finaliser
                        // of the component type
                        currentClue.finalise = m.MethodHandle.GetFunctionPointer();

                        // Skip to the
                        // next iteration
                        continue;
                    }


                    // If the current iteration
                    // is a component updater...
                    if(m.GetCustomAttribute<ComponentUpdateAttribute>() != null)
                    {
                        // Cache the update
                        // of the component type
                        currentClue.update = m.MethodHandle.GetFunctionPointer();

                        // Skip to the
                        // next iteration
                        continue;
                    }

                    // If the current iteration
                    // is a component fixed updater...
                    if(m.GetCustomAttribute<ComponentFixedUpdateAttribute>() != null)
                    {
                        // Cache the fixed update
                        // of the component type
                        currentClue.fixedUpdate = m.MethodHandle.GetFunctionPointer();

                        // Skip to the
                        // next iteration
                        continue;
                    }

                    // If the current iteration
                    // is a component renderer...
                    if(m.GetCustomAttribute<ComponentRenderAttribute>() != null)
                    {
                        // Cache the render
                        // of the component type
                        currentClue.render = m.MethodHandle.GetFunctionPointer();

                        // Skip to the
                        // next iteration
                        continue;
                    }

                    // If the current iteration
                    // is a component resizer...
                    if(m.GetCustomAttribute<ComponentResizeAttribute>() != null)
                    {
                        // Cache the resize
                        // of the component type
                        currentClue.resize = m.MethodHandle.GetFunctionPointer();

                        // Skip to the
                        // next iteration
                        continue;
                    }
                }

                // Set the new clue at the end
                // of the array
                NAHandler.Set(cPtr->Length, currentClue, cPtr);
            }


    }

    // A container for
    // the component clues
    public static NA<ComponentClue> Clues;
}

// A structure to hold
// clues for a components
// creation
public unsafe struct ComponentClue
{

    // The unique id
    // of the component type
    public int uniqueID; 

    // The unmanaged size
    // of the component type
    public int unmanagedSize;

    // The method that handles
    // the initialisation of the
    // component type
    public nint initialise;

    // The method that handles
    // the finalisation of the
    // component type
    public nint finalise;

    // The method that handles
    // the updates of the
    // component type
    public nint update;

    // The method that handles
    // the fixed updates of the
    // component type
    public nint fixedUpdate;

    // The method that handles
    // the rendering of the
    // component type
    public nint render;

    // The method that handles
    // the resizing of the
    // component type
    public nint resize;
}

// A structure of arrays
// that holds the entities
// of a scene
public unsafe struct EntityCollection
{
    // constructor
    public EntityCollection()
    {
        // Initialise ID
        // array
        IDs = new NA<int>();

        // Set the default id
        fixed(NA<int>* ids = &IDs)
            NAHandler.Set(0, 0, ids);

        // Initialise parents
        // array
        Parents = new NA<int>();

        // Set the default parent
        fixed(NA<int>* parents = &Parents)
            NAHandler.Set(0, 0, parents);

        // Initialise children
        // array
        Children = new NA<NA<int>>();

        // Set the default children
        fixed(NA<NA<int>>* children = &Children)
            NAHandler.Set(0, new NA<int>(), children);

        // Initialise enable
        // state array
        EnableState = new NA<bool>();

        // Set the default children
        fixed(NA<bool>* enableState = &EnableState)
            NAHandler.Set(0, false, enableState);

        // Iniialise component
        // record array
        ComponentRecords = new NA<ComponentRecord>();

        // Set the default children
        fixed(NA<ComponentRecord>* componentRecords = &ComponentRecords)
            NAHandler.Set(0, new ComponentRecord(), componentRecords);
    }

    // The ids of the entities
    public NA<int> IDs;

    // The parents bound
    // to the entities
    public NA<int> Parents;

    // The children bound
    // to the entities
    public NA<NA<int>> Children;

    // The states of
    // the entitities
    public NA<bool> EnableState;

    // Records of components
    // that are bound to the
    // entities
    public NA<ComponentRecord> ComponentRecords;

    // Frees unmanaged
    // resources in this
    // collection
    public void Free()
    {
        // Fix the array . . .
        fixed(NA<int>* idPtr = &IDs)
            // Free the array
            NAHandler.Free(idPtr);

        // Fix the array . . .
        fixed(NA<int>* pPtr = &Parents)
            // Free the array
            NAHandler.Free(pPtr);

        // Iterate through each
        // element in children
        for(int i = 0; i < Children.Length; i++)
            // Free the current iteration
            NAHandler.Free(&Children.Values[i]);

        // Fix the array . . .
        fixed(NA<NA<int>>* cPtr = &Children)
            // Free the array
            NAHandler.Free(cPtr);

        // Fix the array . . .
        fixed(NA<bool>* ePtr = &EnableState)
            // Free the array
            NAHandler.Free(ePtr);

        // Iterate through each
        // element in component records
        for(int i = 0; i < ComponentRecords.Length; i++)
            // Free the current iteration
            ComponentRecords.Values[i].Free();
    }
}

// A Record to keep
// track of the components
// that are bound to the
// entity
public unsafe struct ComponentRecord
{
    // The type IDs
    // of the bound
    // components
    public int* IDs;

    // The indices at which
    // the components are
    // bound at
    public int* Indexes;

    // The length of
    // both arrays
    public int Length;

    // Adds a component record
    // to the array with the given
    // values
    public void AddRecord(int typeID, int componentIndex)
    {
        // Get the index
        int arrayIndex = addTypeID(typeID);

        // Set the new index
        // with the given index
        Indexes[arrayIndex] = componentIndex;
    }

        // Hidden helper
        private int addTypeID(int typeID)
        {
            // Find free space

            // Iterate through the type IDs
            for(int i = 0; i < Length; i++)
            {
                // If current iteration
                // is not zero...
                if(IDs[i] != 0)
                    // Skip to next
                    // iteration
                    continue;

                // Set current iteration to
                // the given type id
                IDs[i] = typeID;
                // Return the index
                // for the index array
                return i;
            }

            // Make free space

            // Increase the
            // theoretical
            // length of array
            Length++;

            // Reallocate type ID array
            IDs =
                (int*)NativeMemory.Realloc(IDs, (nuint)(sizeof(int) * Length));

            // Set the new type ID
            IDs[Length - 1] = typeID;

            // Reallocate index array
            Indexes =
                (int*)NativeMemory.Realloc(Indexes, (nuint)(sizeof(int) * Length));

            // Return the index
            // for the index array
            return Length - 1;
        }

    // Removes a record
    // that mathches the given ID
    public void RemoveRecord(int typeID)
    {
        // Iterate through the
        // component type IDs
        for(int i = 0; i < Length; i++)
        {
            // If current iteration
            // doesn't match the given ID...
            if(IDs[i] != typeID)
                // Skip to next iteration
                continue;

            // Set current iteration
            // to zero
            IDs[i] = 0;

            // Set current ietartion
            // to zero
            Indexes[i] = 0;
        }
    }

    // Returns the index of
    // a component type
    // with the matching ID to 
    // the given one
    public int GetIndex(int typeID)
    {
        // Iterate through all IDs
        for(int i = 0; i < Length; i++)
        {
            // If current iteration doesn't
            // match the given type ID...
            if(IDs[i] != typeID)
                // Skip to next
                // iteration
                continue;

            // Return the index
            // of the component
            return Indexes[i];
        }

        // Return 0
        return 0;
    }

    // Frees unamanged resources
    // in this record
    public void Free()
    {
        NativeMemory.Free(IDs);

        NativeMemory.Free(Indexes);
    }
}

// A structure
// to contain the
// behaviour of a
// component type
public unsafe struct ColumnBehaviours
{
    // constructor
    public ColumnBehaviours(NA<ComponentClue> clues)
    {
        // Initialise array
        TypeID = new NA<int>();

        // Initialise array
        Initialise = new NA<nint>();

        // Initialise array
        Finalise = new NA<nint>();

        // Initialise array
        Update = new NA<nint>();

        // Initialise array
        FixedUpdate = new NA<nint>();

        // Initialise array
        Render = new NA<nint>();

        // Initialise array
        Resize = new NA<nint>();

        // Fix the array . . .
        fixed(NA<int>* typeIDs = &TypeID)
        // Fix the array . . .
        fixed(NA<nint>* initialisers = &Initialise)
        // Fix the array . . .
        fixed(NA<nint>* finalisers = &Finalise)
        // Fix the array . . .
        fixed(NA<nint>* updaters = &Update)
        // Fix the array . . .
        fixed(NA<nint>* fixedUpdaters = &FixedUpdate)
        // Fix the array . . .
        fixed(NA<nint>* renders = &Render)
        // Fix the array . . .
        fixed(NA<nint>* resizes = &Resize)
            // Iterate through
            // each clue
            for(int i = 0; i < clues.Length; i++)
            {
                // Set the type id
                NAHandler.Set(i, clues.Values[i].uniqueID, typeIDs);

                // Set the initialiser
                NAHandler.Set(i, clues.Values[i].initialise, initialisers);

                // Set the finaliser
                NAHandler.Set(i, clues.Values[i].finalise, finalisers);

                // Set the updater
                NAHandler.Set(i, clues.Values[i].update, updaters);

                // Set the fixed updater
                NAHandler.Set(i, clues.Values[i].fixedUpdate, fixedUpdaters);

                // Set the render
                NAHandler.Set(i, clues.Values[i].render, renders);

                // Set the resizer
                NAHandler.Set(i, clues.Values[i].resize, resizes);
            }
    }

    // A list of component type
    // identifiers
    public NA<int> TypeID;

    // A list of component
    // type specific initialisers
    public NA<nint> Initialise;

    // A list of component
    // type specific finalisers
    public NA<nint> Finalise;

    // A list of component
    // type specific updaters
    public NA<nint> Update;

    // A list of component
    // type specific fixed updaters
    public NA<nint> FixedUpdate;

    // A list of component
    // type specific renderers
    public NA<nint> Render;

    // A list of component
    // type specific resizers
    public NA<nint> Resize;
}

// A structure
// to contain an
// array of a
// certain type
// of component
public unsafe struct ComponentColumn
{
    // constructor
    public ComponentColumn(ComponentClue clue, int scene)
    {
        // Set the scene belonging
        SceneBelonging = scene;

        // Set the component type id
        TypeID = clue.uniqueID;

        // Set the unmanaged size
        UnmanagedSize = (nuint)clue.unmanagedSize;

        // Set the length at which
        // both the value and entity id
        // array begin at
        Length = 1;

        // Allocate the array
        // to hold the components
        Values = NativeMemory.AllocZeroed(UnmanagedSize);

        // Allocate the array
        // to hold the entity ids
        EntityIDs = (int*)NativeMemory.AllocZeroed(sizeof(int));
    }

    // The scene the component
    // column belongs to
    public int SceneBelonging;

    // A unique ID of the column
    public int TypeID;

    // The unmanaged size
    // of the stored component
    public nuint UnmanagedSize;

    // The array to hold
    // the components
    public void* Values;

    // The IDs of the entities
    // that are bound to the
    // stored components
    public int* EntityIDs;


    // The length of
    // both arrays
    public int Length;


    // Dispose unmanaged items
    public void Free()
    {

        // Free the current value array
        NativeMemory.Free(Values);

        // Free the current entity id array
        NativeMemory.Free(EntityIDs);
    }
}

// An attribute to indicate what
// type counts as a component
[AttributeUsage(AttributeTargets.Struct)]
public class ComponentAttribute : Attribute;

// An attribute to indicate what
// method in a component is for
// initialising the component
[AttributeUsage(AttributeTargets.Method)]
public class ComponentInitialiseAttribute : Attribute;

// An attribute to indicate what
// method in a component is for
// finalising the component
[AttributeUsage(AttributeTargets.Method)]
public class ComponentFinaliseAttribute : Attribute;

// An attribute to indicate what
// method in a component is for
// updating it in a fixed time step
[AttributeUsage(AttributeTargets.Method)]
public class ComponentFixedUpdateAttribute : Attribute;

// An attribute to indicate what
// method in a component is for
// updating it
[AttributeUsage(AttributeTargets.Method)]
public class ComponentUpdateAttribute : Attribute;

// An attribute to indicate what
// method in a component is for
// updating it every frame
[AttributeUsage(AttributeTargets.Method)]
public class ComponentRenderAttribute : Attribute;

// An attribute to indicate what
// method in a component is for
// updating it every time the
// window resizes
[AttributeUsage(AttributeTargets.Method)]
public class ComponentResizeAttribute : Attribute;

// A structure to contain
// a memeory management request.
//
public struct MMRequest
{

}

// An enum to hold
// the types of an
// mm request
public enum MMRequestType : byte
{
    // No request
    None = 0,

    // Request to
    // resize an
    // array
    Resize = 1,

    // Request to
    // remove an
    // array
    Remove = 2,


}