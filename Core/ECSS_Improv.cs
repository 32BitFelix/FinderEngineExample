

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.MemoryManagement;

namespace Core.ECSS;

// TODO: All operations must be made without
// branching behaviour, to ensure that the
// engine can be modified to work with SIMD in the backend

// The heart of the
// Finder Engine. It's
// a combination of
// Scenes and ECS,
// while heavily relying
// on the DOD design principle.
// It's not thread safe,
// because the multithreaded
// implementation is still
// pending  
public static unsafe class ECSSHandler
{
    // Type initializer
    static ECSSHandler()
    {
        // Set the amount of fixed updates
        // that should be called per second
        FixedUpdatesPerSecond = 45;

        // Set the subticks
        // per fixed update
        SubTicks = 4;


        // Initialize the scene array
        scenes = SmartPointer.CreateSmartPointer<Scene>();

        // Initialize the SMT array
        sceneManagementQueue = SmartPointer.CreateSmartPointer<SceneManagementToken>(0);


        // Initialize the entity array
        entities = SmartPointer.CreateSmartPointer<Entity>(1);

        Entity zeroEntity = new Entity()
        {
            Name = null,

            Enabled = false,

            Parent = 0,

            Children = null,

            componentRecord = null
        };

        entities[0] = zeroEntity;


        // Initialize the templates array
        componentTemplates = SmartPointer.CreateSmartPointer<int>();


        // Initialize the behaviours array
        componentBehaviours = SmartPointer.CreateSmartPointer<ComponentBehaviour>();


        // Improve this when adding archetypes

        // Iterate through each type defined
        // in the currently executing assembly
        foreach(Type t in Assembly.GetExecutingAssembly().GetTypes())
        {   
            // If the current iteration does
            // have the component attribute...
            if(t.GetCustomAttribute<ComponentAttribute>() != null)
                // Call the method that
                // helps for loading a
                // component's behaviour
                // and clues for component arrays
                loadComponent(t);
        }

        // Iterate through each type defined
        // in the currently executing assembly
        foreach(Type t in Assembly.GetExecutingAssembly().GetTypes())
        {   
            // If the current iteration
            // has the starter attribute...
            if(t.GetCustomAttribute<StarterAttribute>() != null)
            {
                // Add the type as
                // a scene
                AddScene(t);

                // Skip to the
                // next iteration
                continue;
            }
        }
    }

        // A hidden helper method
        // for loading a component
        private static void loadComponent(Type type)
        {
            // If the type's type initializer
            // is defined...
            if(type.TypeInitializer != null)
                // Call the type initializer
                _ = type.TypeInitializer.Invoke(null, null);


            // Initialize the template
            int template = type.GetHashCode();

            // Save the template
            fixed(int** tPtr = &componentTemplates)

                SmartPointer.Set(tPtr, SmartPointer.GetSmartPointerLength(componentTemplates), template);


            // Initialize the behaviour
            ComponentBehaviour behaviour = new ComponentBehaviour();

            // Set the type ID of the behaviour
            behaviour.TypeID = template;

            // Iterate through each
            // method of the given type
            foreach(MethodInfo m in type.GetMethods())
            {
                // Get the initializer
                if(m.GetCustomAttribute<ComponentInitialiseAttribute>() != null)
                {
                    behaviour.initialize = (delegate*<int, void>)m.MethodHandle.GetFunctionPointer();

                    continue;
                }

                // Get the finalizer
                if(m.GetCustomAttribute<ComponentFinaliseAttribute>() != null)
                {
                    behaviour.finalize = (delegate*<int, void>)m.MethodHandle.GetFunctionPointer();

                    continue;
                }

                // Get the update
                if(m.GetCustomAttribute<ComponentUpdateAttribute>() != null)
                {
                    behaviour.update = (delegate*<void>)m.MethodHandle.GetFunctionPointer();

                    continue;
                }

                // Get the fixed update
                if(m.GetCustomAttribute<ComponentFixedUpdateAttribute>() != null)
                {
                    behaviour.fixedUpdate = (delegate*<void>)m.MethodHandle.GetFunctionPointer();

                    continue;
                }

                // Get the render
                if(m.GetCustomAttribute<ComponentRenderAttribute>() != null)
                {
                    behaviour.render = (delegate*<void>)m.MethodHandle.GetFunctionPointer();

                    continue;
                }

                // Get the resize
                if(m.GetCustomAttribute<ComponentResizeAttribute>() != null)
                {
                    behaviour.resize = (delegate*<int, int, void>)m.MethodHandle.GetFunctionPointer();

                    continue;
                }
            }

            // Save the new behaviour
            fixed(ComponentBehaviour** bPtr = &componentBehaviours)

                SmartPointer.Set(bPtr, SmartPointer.GetSmartPointerLength(componentBehaviours), behaviour);
        }

    // The ID of the currently
    // run scene
    public static int CurrentScene {get; private set;}

    // The array to hold
    // all scenes that
    // have been made active
    public static Scene* scenes;

    // Adds a scene
    // to the collection
    public static void AddScene(Type type)
    {
        // If the given type doesn't have
        // the scene attribute, prematurely
        // end the loop
        if(type.GetCustomAttribute<SceneAttribute>() == null)
            return;


        // Check if the scene
        // already exists...
        // Prematurely end
        // the method, if
        // that is the case
        for(int i = 0; i < SmartPointer.GetSmartPointerLength(scenes); i++)
        {
            if(scenes[i].TypeID == type.GetHashCode())
                return;
        }


        // The cache for saving the index
        // the new scene should be saved at.
        // Set a fallback at the length
        // of the scenes array
        int nIndex = SmartPointer.GetSmartPointerLength(scenes);

        // See if there is any free slot
        for(int i = 0; i < SmartPointer.GetSmartPointerLength(scenes); i++)
        {
            if(scenes[i].TimeScale >= 0)
                continue;

            nIndex = i;

            break;
        }


        // Initialize the new scene
        Scene scene = new Scene();

        // Set the type id of the
        // scene to make it stick
        // out from the rest
        scene.TypeID = type.GetHashCode();

        // Set the timescale of the scene
        scene.TimeScale = 1;

        // Iterate through each
        // method in the type
        foreach(MethodInfo m in type.GetMethods())
        {
            // Get the start method
            if(m.GetCustomAttribute<StartAttribute>() != null)
            {
                scene.startCall = (delegate*<void>)m.MethodHandle.GetFunctionPointer();

                continue;
            }

            // Get the update method
            if(m.GetCustomAttribute<UpdateAttribute>() != null)
            {
                scene.updateCall = (delegate*<void>)m.MethodHandle.GetFunctionPointer();

                continue;
            }

            // Get the fixed update method
            if(m.GetCustomAttribute<FixedUpdateAttribute>() != null)
            {
                scene.fixedUpdateCall = (delegate*<void>)m.MethodHandle.GetFunctionPointer();

                continue;
            }

            // Get the render method
            if(m.GetCustomAttribute<RenderAttribute>() != null)
            {
                scene.renderCall = (delegate*<void>)m.MethodHandle.GetFunctionPointer();

                continue;
            }

            // Get the resize method
            if(m.GetCustomAttribute<ResizeAttribute>() != null)
            {
                scene.resizeCall = (delegate*<void>)m.MethodHandle.GetFunctionPointer();

                continue;
            }

            // Get the end method
            if(m.GetCustomAttribute<EndAttribute>() != null)
            {
                scene.endCall = (delegate*<void>)m.MethodHandle.GetFunctionPointer();

                continue;
            }
        }


        // Create an array of component arrays
        // for the scene
        ComponentArray* componentArrays =
            SmartPointer.CreateSmartPointer<ComponentArray>(SmartPointer.GetSmartPointerLength(componentTemplates));

        // Initialize each component array
        // in the just created array
        for(int i = 0; i < SmartPointer.GetSmartPointerLength(componentArrays); i++)
        {
            componentArrays[i] = new ComponentArray();

            componentArrays[i].TypeID = componentTemplates[i];

            componentArrays[i].EntityIDs = SmartPointer.CreateSmartPointer<int>(0);


            componentArrays[i].Components = NativeMemory.Alloc(sizeof(int));

            *(int*)componentArrays[i].Components = 0;

            componentArrays[i].Components = (void*)((nint)componentArrays[i].Components + 4);
        }

        // Save the array of component
        // array to the given scene
        scene.components = componentArrays;


        // Add the new sceen to
        // the SMT queue

        int SMTIndex = SmartPointer.GetSmartPointerLength(sceneManagementQueue);


        for(int i = SmartPointer.GetSmartPointerLength(sceneManagementQueue) - 1; i > -1; i--)
        {
            if(sceneManagementQueue[i].ManagementType != 0)
                continue;

            SMTIndex = i;

            break;
        }


        fixed(SceneManagementToken** sPtr = &sceneManagementQueue)
            SmartPointer.Set(sPtr, SMTIndex, new SceneManagementToken(){ ManagementType = 1, scene = scene });


        isSMTDirty = true;
    }

    // Removes a scene
    // from the collection
    // that matches the
    // given type
    public static void RemoveScene(Type type)
    {
        // If the given type doesn't have
        // the scene attribute, prematurely
        // end the loop
        if(type.GetCustomAttribute<SceneAttribute>() == null)
            return;


        int nIndex = SmartPointer.GetSmartPointerLength(sceneManagementQueue);


        for(int i = SmartPointer.GetSmartPointerLength(sceneManagementQueue) - 1; i > -1; i--)
        {
            if(sceneManagementQueue[i].ManagementType != 0)
                continue;

            nIndex = i;

            break;
        }


        SceneManagementToken nToken = new SceneManagementToken()
        {
            ManagementType = 2,

            scene = new Scene(){ TypeID = type.GetHashCode() }
        };


        fixed(SceneManagementToken** sPtr = &sceneManagementQueue)
            SmartPointer.Set(sPtr, nIndex, nToken);
    }


    // An array to store scene management
    // tokens for safe asynchronous scene
    // management
    private static SceneManagementToken* sceneManagementQueue;

    // A boolean to indicate,
    // if the SMT queue is dirty
    private static bool isSMTDirty;

    // The backend function
    // for processing
    // scene management tokens
    private static void __processSMT()
    {
        // If the SMT queue
        // is not dirty...
        if(!isSMTDirty)
            // Prematurely end
            // the method
            return;


        // Iterate through each
        // scene management token
        // in the SMT queue
        for(int i = SmartPointer.GetSmartPointerLength(sceneManagementQueue) - 1; i > -1; i--)
        {
            // If the current iteration has
            // nothing to manage...
            if(sceneManagementQueue[i].ManagementType == 0)
                // Skip to the
                // next iteration
                continue;


            // Check if the current
            // iteration should be
            // created
            bool check = sceneManagementQueue[i].ManagementType == 1;

            // Calculate if the
            // scene creation method
            // should be called
            nint mCall = (nint)(delegate*<Scene, void>)&__createSceneBackend * *(byte*)&check;


            // Check if the current
            // iteration should be
            // removed
            check = sceneManagementQueue[i].ManagementType == 2;

            // Calculate if the
            // scene removal method
            // should be called
            mCall += (nint)(delegate*<Scene, void>)&__removeSceneBackend * *(byte*)&check;


            // Call the made
            // out method
            ((delegate*<Scene, void>)mCall)(sceneManagementQueue[i].scene);


            sceneManagementQueue[i] = new SceneManagementToken()
            {
                ManagementType = 0,

                scene = default
            };
        }


        // Make it clear,
        // that the SMT
        // array is not
        // dirty anymore
        isSMTDirty = false;
    }

        // Create the scene for real
        private static void __createSceneBackend(Scene scene)
        {
            int nIndex = SmartPointer.GetSmartPointerLength(scenes);


            for(int i = SmartPointer.GetSmartPointerLength(scenes) - 1; i > -1; i--)
            {
                if(scenes[i].TypeID != 0)
                    continue;

                nIndex = i;

                break;
            }


            fixed(Scene** sPtr = &scenes)
                SmartPointer.Set(sPtr, nIndex, scene);
        }

        // Remove the scene for real
        private static void __removeSceneBackend(Scene scene)
        {



            for(int i = SmartPointer.GetSmartPointerLength(scenes) - 1; i > -1; i--)
            {
                if(scenes[i].TypeID != scene.TypeID)
                    continue;


                SmartPointer.Free(scenes[i].components);

                scenes[i] = default;


                break;
            }
        }


    // The delta time of the whole
    // engine. It's set by the
    // window manager that runs it
    public static float GlobalDeltaTime;


    // Returns the time scale
    // of the current scene
    public static float GetTimeScale()
        => scenes[CurrentScene].TimeScale;

    // Sets the time scale of
    // the current scene
    public static void SetTimeScale(float value)
        => scenes[CurrentScene].TimeScale = value;

    // Returns the
    // delta time
    // of the currently
    // run scene
    public static float GetDeltaTime()
        => scenes[CurrentScene].DeltaTime;


    // Does the usual
    // update stuff with
    // scene's and components
    // as soon as it's called
    public static void ECSSUpdate()
    {
        // Process the
        // scene management
        // token queue
        __processSMT();


        // Update the deltatimes
        // of the scenes
        for(int i = 0; i < SmartPointer.GetSmartPointerLength(scenes); i++)

            scenes[i].DeltaTime = GlobalDeltaTime * scenes[i].TimeScale;


        // Update the components
        for(int i = SmartPointer.GetSmartPointerLength(componentBehaviours) * SmartPointer.GetSmartPointerLength(scenes) - 1; i > -1; i--)
        {

            // Get the scene ID of the current iteration
            CurrentScene = i / SmartPointer.GetSmartPointerLength(componentBehaviours);

            // Get the component behaviour id of the current iteration
            int currentBehaviour = i % SmartPointer.GetSmartPointerLength(componentBehaviours);


            // Calculate if the current iteration
            // has a valid scene...
            bool isScene = scenes[CurrentScene].TypeID != 0;

            // Calculate, if the current iteration
            // has an update method...
            bool isUpdate = componentBehaviours[currentBehaviour].update != null;

            // Calculate which method to call.
            //
            // isUpdate && isScene: update method
            // isUpdate && !isScene: dummy method
            // !isUpdate && isScene: dummy method
            // !isupdate && !isScene: dummy method
            nint updateAddress = (nint)componentBehaviours[currentBehaviour].update * *(byte*)&isScene * *(byte*)&isUpdate
                + ((nint)(delegate*<void>)&__dummy) * (*(byte*)&isUpdate * *(byte*)&isScene ^ 0x01);


            // Call the calculated method signature
            ((delegate*<void>)updateAddress)();
        }


        // Update the scenes
        for(int i = SmartPointer.GetSmartPointerLength(scenes) - 1; i > -1; i--)
        {
            // Set the ID of the
            // current scene
            CurrentScene = i;


            // See if the update method for the current
            // scene exists. If that isn't the case,
            // take the method signature of the
            // dummy method

            bool isUpdate = scenes[i].updateCall != null;

            nint updateAddress =
                (nint)scenes[i].updateCall * *(byte*)&isUpdate + ((nint)(delegate*<void>)&__dummy) * (*(byte*)&isUpdate ^ 0x01);


            //((delegate*<void>)updateAddress)();

            // See if the start method is defined.
            // If it is, take it's method signature
            // instead of update's. Otherwise,
            // if the update method is present,
            // take it's method signature.
            // If none of the above are true,
            // Take the method signature of the dummy method

            bool isStart = scenes[i].startCall != null;


            nint address =
                (nint)scenes[i].startCall * *(byte*)&isStart + updateAddress * (*(byte*)&isStart ^ 0x01);


            *(nint*)&scenes[i].startCall -= *(nint*)&scenes[i].startCall;


            // Call whatever method that
            // has been calculated

            ((delegate*<void>)address)();
        }
    }

        // A dummy method used to
        // supplement the absence
        // of an update method.
        // Is a safe guard for the
        // function pointer
        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void __dummy()
            {}

    // The amount of fixed updates
    // to call per second
    public static byte FixedUpdatesPerSecond;

    // Returns the time it takes
    // between each fixed update
    public static float FixedDeltaTime
        => 1f / FixedUpdatesPerSecond;

    // The amount of sub ticks
    // for each fixedc update
    public static byte SubTicks;

    // Counts the time until
    // the next fixed update
    private static float FixedUpdateCounter;

    // Does the fixed
    // update related
    // things if the
    // time has come
    // upon call
    public static void ECSSFixedUpdate()
    {
        FixedUpdateCounter += GlobalDeltaTime;

        if(FixedUpdateCounter < FixedDeltaTime)
            return;

        FixedUpdateCounter = 0;


        // Update components
        for(int i = SmartPointer.GetSmartPointerLength(componentBehaviours) * SmartPointer.GetSmartPointerLength(scenes) - 1; i > -1; i--)
        {
            // Get the scene ID of the current iteration
            CurrentScene = i / SmartPointer.GetSmartPointerLength(componentBehaviours);

            // Get the component behaviour id of the current iteration
            int currentBehaviour = i % SmartPointer.GetSmartPointerLength(componentBehaviours);


            // Calculate if the current iteration
            // has a valid scene...
            bool isScene = scenes[CurrentScene].TypeID != 0;

            // Calculate if the current iteration
            // has a valid fixed update method
            bool isFixed = componentBehaviours[currentBehaviour].fixedUpdate != null;


            // Calculate which method to call.
            //
            // isFixed && isFixed: fixed update method
            // isFixed && !isFixed: dummy method
            // !isFixed && isFixed: dummy method
            // !isFixed && !isFixed: dummy method
            nint fixedAddress = (nint)componentBehaviours[currentBehaviour].fixedUpdate * *(byte*)&isScene * *(byte*)&isFixed
                + ((nint)(delegate*<void>)&__dummy) * (*(byte*)&isFixed * *(byte*)&isScene ^ 0x01);


            // Call the calculated method signature
            ((delegate*<void>)fixedAddress)();
        }


        // Update Scenes
        for(int i = SmartPointer.GetSmartPointerLength(scenes) - 1; i > -1; i--)
        {
            // Set the current scene
            CurrentScene = i;


            // Check if the current scene
            // is a valid scene
            bool isFixed = scenes[i].fixedUpdateCall != null;


            // Calculate the method to call
            nint fixedAddress = (nint)scenes[i].fixedUpdateCall * *(byte*)&isFixed + (nint)(delegate*<void>)&__dummy * (*(byte*)&isFixed ^ 0x01);

            // Call the calculated method
            ((delegate*<void>)fixedAddress)();
        }
    }

    // Does the rendering
    // related things
    public static void ECSSRender()
    {
        // Render components
        for(int i = SmartPointer.GetSmartPointerLength(componentBehaviours) * SmartPointer.GetSmartPointerLength(scenes) - 1; i > -1; i--)
        {
            // Get the scene ID of the current iteration
            CurrentScene = i / SmartPointer.GetSmartPointerLength(componentBehaviours);

            // Get the component behaviour id of the current iteration
            int currentBehaviour = i % SmartPointer.GetSmartPointerLength(componentBehaviours);


            // Calculate if the current iteration
            // has a valid scene...
            bool isScene = scenes[CurrentScene].TypeID != 0;

            // Calculate, if the current iteration
            // has a render method...
            bool isRender = componentBehaviours[currentBehaviour].render != null;

            // Calculate which method to call.
            //
            // isRender && isScene: render method
            // isRender && !isScene: dummy method
            // !isRender && isScene: dummy method
            // !isRender && !isScene: dummy method
            nint renderAddress = (nint)componentBehaviours[currentBehaviour].render * *(byte*)&isScene * *(byte*)&isRender
                + ((nint)(delegate*<void>)&__dummy) * (*(byte*)&isRender * *(byte*)&isScene ^ 0x01);


            // Call the calculated method signature
            ((delegate*<void>)renderAddress)();
        }

        // Render scenes
        for(int i = SmartPointer.GetSmartPointerLength(scenes) - 1; i > -1; i--)
        {
            // Set the current scene
            CurrentScene = i;


            // Check if the current scene
            // is a valid scene
            bool isRender = scenes[i].renderCall != null;


            // Calculate the method to call
            nint renderAddress = (nint)scenes[i].renderCall * *(byte*)&isRender + (nint)(delegate*<void>)&__dummy * (*(byte*)&isRender ^ 0x01);

            // Call the calculated method
            ((delegate*<void>)renderAddress)();
        }
    }

    // Does resizing
    // related things
    public static void ECSSResize()
    {
        // Resize Components
        for(int i = SmartPointer.GetSmartPointerLength(componentBehaviours) * SmartPointer.GetSmartPointerLength(scenes) - 1; i > -1; i--)
        {
            // Get the scene ID of the current iteration
            CurrentScene = i / SmartPointer.GetSmartPointerLength(componentBehaviours);

            // Get the component behaviour id of the current iteration
            int currentBehaviour = i % SmartPointer.GetSmartPointerLength(componentBehaviours);


            bool isScene = scenes[CurrentScene].TypeID != 0;


            bool isResize = componentBehaviours[currentBehaviour].resize != null;


            nint resizeAddress =
                (nint)componentBehaviours[currentBehaviour].resize * *(byte*)&isResize * *(byte*)&isScene + (nint)(delegate*<void>)&__dummy * (*(byte*)&isResize * *(byte*)&isScene ^ 0x01);

            ((delegate*<void>)resizeAddress)();
        }

        // Resize Scenes
        for(int i = SmartPointer.GetSmartPointerLength(scenes) - 1; i > -1; i--)
        {
            // Set the current scene
            CurrentScene = i;


            // Check if the current scene
            // is a valid scene
            bool isResize = scenes[i].resizeCall != null;


            // Calculate the method to call
            nint resizeAddress = (nint)scenes[i].resizeCall * *(byte*)&isResize + (nint)(delegate*<void>)&__dummy * (*(byte*)&isResize ^ 0x01);

            // Call the calculated method
            ((delegate*<void>)resizeAddress)();
        }
    }

    // Does the finalisation stuff
    // of the engine when called
    public static void ECSSEnd()
    {
        // End Scenes
        for(int i = SmartPointer.GetSmartPointerLength(scenes) - 1; i > -1; i--)
        {
            // Set the current scene
            CurrentScene = i;


            // Check if the current scene
            // is a valid scene
            bool isEnd = scenes[i].endCall != null;


            // Calculate the method to call
            nint endAddress = (nint)scenes[i].endCall * *(byte*)&isEnd + (nint)(delegate*<void>)&__dummy * (*(byte*)&isEnd ^ 0x01);

            // Call the calculated method
            ((delegate*<void>)endAddress)();


            for(int j = 0; j < SmartPointer.GetSmartPointerLength(scenes[i].components); j++)
            {
                SmartPointer.Free(scenes[i].components[j].EntityIDs);

                SmartPointer.Free((int*)scenes[i].components[j].Components);
            }

            SmartPointer.Free(scenes[i].components);
        }


        SmartPointer.Free(entities);
    }

    // The array that stores all
    // entities within the engine
    private static Entity* entities;

    // Creates an entity
    // within the current
    // scene
    public static int CreateEntity(string name = "NONAME")
    {   
        // Create the new entity
        Entity nEntity = new Entity();


        // Allocate memory for
        // it's name's container
        nEntity.Name = SmartPointer.CreateSmartPointer<char>(name.Length);

        // Copy the given string to
        // the name container of the entity
        for(int i = 0; i < SmartPointer.GetSmartPointerLength(nEntity.Name); i++)
            nEntity.Name[i] = name[i];

        // Set the enable state
        // of the new entity
        // to true
        nEntity.Enabled = true;

        // Set the parent of
        // the entity to zero
        nEntity.Parent = 0;

        // Allocate an array
        // to store the entity's
        // children
        nEntity.Children = SmartPointer.CreateSmartPointer<int>();

        // Allocate an array to store
        // the component records of
        // the bound components to
        // the entity
        nEntity.componentRecord = SmartPointer.CreateSmartPointer<ComponentRecord>();


        // The index the new entity will be
        // saved at. Fallback is set at the
        // length of the entities array
        int nIndex = SmartPointer.GetSmartPointerLength(entities) == 0 ?
            1: SmartPointer.GetSmartPointerLength(entities);

        // Look for a free entity
        // in the entity collection
        for(int i = 1; i < SmartPointer.GetSmartPointerLength(entities); i++)
        {
            if(entities[i].Name != null)
                continue;

            nIndex = i;

            break;
        }


        // Save the new entity
        fixed(Entity** ePtr = &entities)
            SmartPointer.Set(ePtr, nIndex, nEntity);

        // Return the index
        // the entity has
        // been saved at
        return nIndex;
    }

    // Removes an entity
    // with the fitting
    // id in the current scene
    public static void RemoveEntity(int entityID)
    {
        // Free the entity's name container

        SmartPointer.Free(entities[entityID].Name);

        entities[entityID].Name = null;


        // Set the enable state of the
        // entity to disabled

        entities[entityID].Enabled = false;


        // Unbind the entity
        // from it's parent,
        // if it has one

        bool hasParent = entities[entityID].Parent != 0;

        nint unbindAddress =
            (nint)(delegate*<int, int, void>)&UnbindChild * *(byte*)&hasParent + (nint)(delegate*<void>)&__dummy * (*(byte*)&hasParent ^ 0x01);

        ((delegate*<int, int, void>)unbindAddress)(entities[entityID].Parent, entityID);


        // Dispose of the entity's children

        for(int i = 0; i < SmartPointer.GetSmartPointerLength(entities[entityID].Children); i++)

            RemoveEntity(entities[entityID].Children[i]);

        SmartPointer.Free(entities[entityID].Children);

        entities[entityID].Children = null;


        // Dispose of the bound components
        // of the given entity

        for(int i = 0; i < SmartPointer.GetSmartPointerLength(entities[entityID].componentRecord); i++)
        {
            // NOTE: Make the amount of scenes constant.
            // There's no way someone would need more than 255

            // Behold, a taste of the german vocabulary
            scenes[CurrentScene].components[entities[entityID].componentRecord[i].ArrayIndex].EntityIDs[entities[entityID].componentRecord[i].ComponentIndex] = 0;

            // Calculate, if the finalizer for the
            // current component is given
            bool isFinDefined = componentBehaviours[entities[entityID].componentRecord[i].ArrayIndex].finalize != null;

            // Calculate the address of the
            // method to call
            nint address =
                (nint)componentBehaviours[entities[entityID].componentRecord[i].ArrayIndex].finalize * *(byte*)&isFinDefined +
                    (nint)(delegate*<void>)&__dummy * (*(byte*)&isFinDefined ^ 0x01);

            // Call the calculated method
            ((delegate*<int, void>)address)(entityID);
        }

        SmartPointer.Free(entities[entityID].componentRecord);

        entities[entityID].componentRecord = null;


        entities[entityID] = default;
    }

    // There is a slight suspicion, that
    // the branchless approach on methods
    // like bind and unbind child will have
    // a hit on performnance. Or not, as
    // the cpu will probably keep the data
    // closer, due to being iterated 'till
    // the very end of the array.
    // SIMD will be a great help here, indeed

    // Binds an entity
    // as a child to 
    // the given parent
    // entity
    public static void BindChild(int parent, int child)
    {
        if(parent == child)
            return;


        // Check if the child is already
        // bound to a different parent
        bool hasParent = entities[child].Parent != 0;

        nint address =
            (nint)(delegate*<int, int, void>)&UnbindChild * *(byte*)&hasParent + (nint)(delegate*<void>)&__dummy * (*(byte*)&hasParent ^ 0x01);

        ((delegate*<int, int, void>)address)(entities[child].Parent, child);


        // Indicator to see if the
        // given child is already
        // referenced within it's
        // parent
        bool isContained = false;

        // Check through each child...
        for(int i = 0; i < SmartPointer.GetSmartPointerLength(entities[parent].Children); i++)
        {
            // Calculate if the current
            // iteration is zero
            bool isCurrentNull = entities[parent].Children[i] == 0;

            // Set the given child to the
            // current index, if it is free
            entities[parent].Children[i] += child * *(byte*)&isCurrentNull;

            // Calculate, if the child is already defined
            isContained |= entities[parent].Children[i] == child;


            i = SmartPointer.GetSmartPointerLength(entities[parent].Children) * *(byte*)&isCurrentNull + i * (*(byte*)&isCurrentNull ^ 0x01);
        }

        // Set the parent of the child,
        // if it hasn't been set
        entities[child].Parent = parent;


        // If there wasn't a free index
        // to use, create a new index
        // to save the child at
        if(!isContained)
            SmartPointer.Set(&entities[parent].Children, SmartPointer.GetSmartPointerLength(entities[parent].Children), child);
    }

    // Unbinds an entity
    // as a child from the
    // given parent entity
    public static void UnbindChild(int parent, int child)
    {
        // Remove the parent from
        // the child
        entities[child].Parent = 0;

        // Iterate through each child...
        for(int i = 0; i < SmartPointer.GetSmartPointerLength(entities[parent].Children); i++)
        {
            // Calculate if the current
            // iteration is the same as
            // the given child
            bool isCurrentSame = entities[parent].Children[i] == child;

            // Set the given child to the
            // current index, if it is free
            entities[parent].Children[i] -= child * *(byte*)&isCurrentSame;
        }
    }

    // Shows the
    // children
    // of the given
    // parent entity
    public static int* ShowChildren(int parent)
        => entities[parent].Children;

    // Shows the
    // parent
    // of the given
    // child entity
    public static int ShowParent(int child)
        => entities[child].Parent;


    // An array of all components'
    // behaviour
    private static ComponentBehaviour* componentBehaviours;

    // An array that holds templates for all components'
    // relevant informations, like type ID and size in bytes
    private static int* componentTemplates;


    // Adds a component of
    // the specified type
    // to the given entity
    public static void AddComponent<T>(int entityID, T value)
        where T : unmanaged
    {
        // If the given value
        // is not a component,
        // prematurely end
        // the method
        if(typeof(T).GetCustomAttribute<ComponentAttribute>() == null)
            return;

        // Initialize the new component
        // record for the given entity
        ComponentRecord componentRecord = new ComponentRecord();

        // Set the type ID
        // of the record
        componentRecord.TypeID = typeof(T).GetHashCode();


        // A cache that stores the index
        // of the component array that
        // fits the given component type.
        // Fallback is set to zero
        int arrayIndex = 0;

        // Iterate through each
        // component array of the
        // current scene
        for(int i = 0; i < SmartPointer.GetSmartPointerLength(scenes[CurrentScene].components); i++)
        {
            // Calculate, if the component record
            // and the component array of the
            // current iteration share the same type id
            bool isSame =
                componentRecord.TypeID == scenes[CurrentScene].components[i].TypeID; 

            // Save the current index,
            // if the condition holds true
            arrayIndex += i * *(byte*)&isSame;
        }

        // Save the index of the
        // component's array
        componentRecord.ArrayIndex = arrayIndex;


        // Save the index, that the new
        // component will be saved at.
        // Set the fallback to the length
        // of the given component array
        componentRecord.ComponentIndex =
            SmartPointer.GetSmartPointerLength(scenes[CurrentScene].components[arrayIndex].EntityIDs) == 0 ?
                1 : SmartPointer.GetSmartPointerLength(scenes[CurrentScene].components[arrayIndex].EntityIDs);

        // Iterate through each entity ID
        for(int i = 1; i < SmartPointer.GetSmartPointerLength(scenes[CurrentScene].components[arrayIndex].EntityIDs); i++)
        {
            // Calculate if the current iteration is zero
            bool isCurrentNull = scenes[CurrentScene].components[arrayIndex].EntityIDs[i] == 0;

            // Change the index to save the
            // new component at, if the current
            // iteration turns out to be zero
            componentRecord.ComponentIndex =
                componentRecord.ComponentIndex * (*(byte*)&isCurrentNull ^ 0x01) + i * *(byte*)&isCurrentNull;

            
            i = SmartPointer.GetSmartPointerLength(scenes[CurrentScene].components[arrayIndex].EntityIDs) * *(byte*)&isCurrentNull + i * (*(byte*)&isCurrentNull ^ 0x01);
        }

        // Save the entity id of the new component
        // to it's component array
        SmartPointer.Set(&scenes[CurrentScene].components[arrayIndex].EntityIDs, componentRecord.ComponentIndex, entityID);

        // Save the new component within
        // it's respective component array
        SmartPointer.Set((T**)&scenes[CurrentScene].components[arrayIndex].Components, componentRecord.ComponentIndex, value);


        // Save the record to the entity

        // A boolean to indicate,
        // if the record found
        // a free space in the
        // given entity's record array
        bool isDefined = false;

        // Iterate through each element of
        // the entity's component record
        // array
        for(int i = 0; i < SmartPointer.GetSmartPointerLength(entities[entityID].componentRecord); i++)
        {
            // Calculate, if the current iteration
            // is zero
            bool isCurrentNull = entities[entityID].componentRecord[i].TypeID == 0;


            // Set the value of the
            // empty record, if it
            // is empty
            entities[entityID].componentRecord[i].TypeID += componentRecord.TypeID * *(byte*)&isCurrentNull;

            entities[entityID].componentRecord[i].ArrayIndex += arrayIndex * *(byte*)&isCurrentNull;

            entities[entityID].componentRecord[i].ComponentIndex += componentRecord.ComponentIndex * *(byte*)&isCurrentNull;


            // Check if the record is already
            // defined
            isDefined |= entities[entityID].componentRecord[i].TypeID == componentRecord.TypeID;


            i = SmartPointer.GetSmartPointerLength(entities[entityID].componentRecord) * *(byte*)&isCurrentNull + i * (*(byte*)&isCurrentNull ^ 0x01);
        }

        // If there was no space
        // in the entity's record array,
        // add the new record to a new index
        if(!isDefined)
            SmartPointer.Set(&entities[entityID].componentRecord, SmartPointer.GetSmartPointerLength(entities[entityID].componentRecord), componentRecord);


        // Calculate if the initializer
        // of the given component type
        // is defined
        bool isInitDefined = componentBehaviours[arrayIndex].initialize != null;

        // Calculate of the address
        // of the method to call
        nint address =
            (nint)componentBehaviours[arrayIndex].initialize * *(byte*)&isInitDefined + (nint)(delegate*<void>)&__dummy * (*(byte*)&isInitDefined ^ 0x01);

        // Call the method that has
        // been calculated
        ((delegate*<int, void>)address)(entityID);
    }

    // Removes a component of
    // the specified type from
    // the given entity
    public static void RemoveComponent<T>(int entityID)
        where T : unmanaged
    {
        int hashCode = typeof(T).GetHashCode();


        for(int i = 0; i < SmartPointer.GetSmartPointerLength(entities[entityID].componentRecord); i++)
        {
            // Check if the current record
            // and the given component type
            // share the same hashcode 
            bool isCurrentSame = entities[entityID].componentRecord[i].TypeID == hashCode;


            // Calculate, if the finalizer
            // of the component is defined
            bool isFinDefined = componentBehaviours[entities[entityID].componentRecord[i].ArrayIndex].finalize != null;

            // Claulcate the address of
            // the finalizer
            nint finAddress =
                (nint)componentBehaviours[entities[entityID].componentRecord[i].ArrayIndex].finalize * *(byte*)&isFinDefined +
                    (nint)(delegate*<void>)&__dummy * (*(byte*)&isFinDefined ^ 0x01);


            // Calculate the address
            // of the method to call
            nint address =
                finAddress * *(byte*)&isCurrentSame + (nint)(delegate*<void>)&__dummy * (*(byte*)&isCurrentSame ^ 0x01);

            // Call the calculated method
            ((delegate*<int, void>)address)(entityID);


            // Set the entity id to zero
            // of the respective component
            scenes[CurrentScene].components[entities[entityID].componentRecord[i].ArrayIndex].EntityIDs[entities[entityID].componentRecord[i].ComponentIndex] 
                -= scenes[CurrentScene].components[entities[entityID].componentRecord[i].ArrayIndex].EntityIDs[entities[entityID].componentRecord[i].ComponentIndex] * *(byte*)&isCurrentSame;


            // Dispose of the given record,
            // if it should be  

            entities[entityID].componentRecord[i].TypeID -= entities[entityID].componentRecord[i].TypeID * *(byte*)&isCurrentSame;

            entities[entityID].componentRecord[i].ArrayIndex -= entities[entityID].componentRecord[i].ArrayIndex * *(byte*)&isCurrentSame;

            entities[entityID].componentRecord[i].ComponentIndex -= entities[entityID].componentRecord[i].ComponentIndex * *(byte*)&isCurrentSame;
        }
    }

    // Returns the adress
    // of a component of
    // the given type
    // from the given entity
    public static T* GetComponent<T>(int entityID)
        where T : unmanaged
    {
        int hashCode = typeof(T).GetHashCode();


        int nComInd = 0;

        int nArrayInd = 0;


        for(int i = 0; i < SmartPointer.GetSmartPointerLength(entities[entityID].componentRecord); i++)
        {
            // Calculate if the current
            // component record
            bool isCurrentSame = entities[entityID].componentRecord[i].TypeID == hashCode;


            nComInd += entities[entityID].componentRecord[i].ComponentIndex * *(byte*)&isCurrentSame;

            nArrayInd += entities[entityID].componentRecord[i].ArrayIndex * *(byte*)&isCurrentSame;


            // Prematurely ends the method by setting
            // the i to a value beyond the allowed threshhold
            i = i * (*(byte*)&isCurrentSame ^ 0x01) + SmartPointer.GetSmartPointerLength(entities[entityID].componentRecord) * *(byte*)&isCurrentSame;         
        }


        return &((T*)scenes[CurrentScene].components[nArrayInd].Components)[nComInd];
    }

    // Overloads the
    // given pointer
    // with attributes
    // of the column
    // with the given type
    public static void GetCompactColumn<T>(T** values, int** entities, int* length)
        where T : unmanaged
    {
        int hashCode = typeof(T).GetHashCode();


        int nArrayInd = 0;


        for(int i = 0; i < SmartPointer.GetSmartPointerLength(componentBehaviours); i++)
        {
            bool isCurrentSame = componentBehaviours[i].TypeID == hashCode;


            nArrayInd += i * *(byte*)&isCurrentSame;

            
            i = i * (*(byte*)&isCurrentSame ^ 0x01) + SmartPointer.GetSmartPointerLength(componentBehaviours) * *(byte*)&isCurrentSame;
        }


        *values = (T*)scenes[CurrentScene].components[nArrayInd].Components;

        *entities = scenes[CurrentScene].components[nArrayInd].EntityIDs;

        *length = SmartPointer.GetSmartPointerLength(*entities);
    }

    // Tells if the component
    // of the given type is
    // within the given entity
    public static bool ContainsComponent<T>(int entityID)
        where T : unmanaged
    {
        // Precompute the hashcode.
        // This apprach is preferred,
        // as hashcodes aren't very
        // cheap to calculate
        int hashCode = typeof(T).GetHashCode();

        // A cache to see, if
        // the entity is bound
        // to a component of the
        // given type
        bool isContained = false;


        for(int i = 0; i < SmartPointer.GetSmartPointerLength(entities[entityID].componentRecord); i++)
        {
            isContained |= entities[entityID].componentRecord[i].TypeID == hashCode;

            i = i * (*(byte*)&isContained ^ 0x01) + SmartPointer.GetSmartPointerLength(entities[entityID].componentRecord) * *(byte*)&isContained;
        }


        return isContained;
    }

    // Set the state of the given
    // entity with the given bool
    public static void SetEntityState(int entityID, bool state)
        => entities[entityID].Enabled = state;

    // Get the state of the given
    // entity
    public static bool GetEnableState(int entityID)
    {
        if(entities[entityID].Parent != 0)
            return GetEnableState(entities[entityID].Parent) & entities[entityID].Enabled;

        return entities[entityID].Enabled;
    }
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

// A scene represents
// a group of self-contained
// and custom logic, aswell
// as components
public unsafe struct Scene
{
    // A type id to
    // differentiate
    // the scene from
    // the others
    public int TypeID;


    // A pointer reference
    // to the start method
    public delegate*<void> startCall;

    // A pointer reference
    // to the update method
    public delegate*<void> updateCall;

    // A pointer reference
    // to the fixed update method
    public delegate*<void> fixedUpdateCall;

    // A pointer reference
    // to the render method
    public delegate*<void> renderCall;

    // A pointer reference
    // to the resize method
    public delegate*<void> resizeCall;

    // A pointer reference
    // to the end method
    public delegate*<void> endCall;

    
    // A multiplier to indicate
    // how the time passes in
    // the scene
    public float TimeScale;

    // The time it took
    // between the last
    // and the current frame
    // (in seconds)
    public float DeltaTime;


    // A boolean to
    // indicate if
    // the scene
    // should be run
    public bool IsEnabled;


    // A collection of
    // component arrays
    public ComponentArray* components;
}

// An entity represents
// an object in the scene
public unsafe struct Entity
{
    // The name of
    // the entity
    public char* Name;

    // The boolean
    // to indicate,
    // if the entity
    // is enabled
    public bool Enabled;

    // The ID of the
    // entity's parent
    public int Parent;

    // An array to
    // store the
    // children of
    // the entity
    public int* Children;

    // An array of records
    // of the entity's
    // bound components
    public ComponentRecord* componentRecord;
}

// Component arrays
// keep track of the
// components of their
// respective type
public unsafe struct ComponentArray
{
    // The id of the
    // type of the
    // component
    public int TypeID;

    // The IDs of the
    // entities bound
    // to the component
    // of the same index
    public int* EntityIDs;

    // The array to store
    // the components
    public void* Components;
}

// Holds the methods defined
// for the behaviour of a component
public unsafe struct ComponentBehaviour
{
    // The Id of the component
    // behaviour's type
    public int TypeID;


    // A pointer reference to the
    // components' initializing method
    public delegate*<int, void> initialize;

    // A pointer reference to the
    // components' finalizing method
    public delegate*<int, void> finalize;


    // A pointer reference to the
    // method that updates the
    // components every update
    public delegate*<void> update;

    // A pointer reference to the
    // method that updates the
    // components every fixed update
    public delegate*<void> fixedUpdate;

    // A pointer reference to the
    // method that updates the
    // components every render
    public delegate*<void> render;

    // A pointer reference to the
    // method that updates the
    // components every resize
    public delegate*<int, int, void> resize;
}

// A record of a component
// for an entity to keep
// track of it's component
public struct ComponentRecord
{
    // The type id of
    // the respective
    // component
    public int TypeID;

    // The index of the component
    // array, the component has
    // been saved to
    public int ArrayIndex;

    // The index of the
    // respective component
    // within it's component array
    public int ComponentIndex;
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


// A helper structure to
// store informations for
// managing a scene
public struct SceneManagementToken
{
    // A number representing
    // what to do with the
    // scene in the queue.
    // 0: Empty
    // 1: Create
    // 2: Remove
    public byte ManagementType;

    // The type of the
    // scene to manage
    public Scene scene;
}

