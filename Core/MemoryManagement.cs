
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Core.MemoryManagement;

// TODO: Add an indexed
// accessor to the NA
// struct for the next
// iteration of the engine.
// Fixing static fields
// gets really repetitive...

// A custom structure
// that aims to simplify
// the use of native arrays
public unsafe struct NA<T>
    where T : unmanaged
{
    // constructor.
    // Call is needed
    // in order to make
    // the native array
    // work
    public NA(int size = 0)
    {
        Values =
            (T*)NativeMemory.Alloc((nuint)(sizeof(T) * size));

        Length = size;
    }

    // The array to store
    // the elements
    public T* Values;

    // A cache for
    // storing the
    // length of
    // the array
    public int Length;
}

// A handler for Native arrays,
// simply for the cause of making
// use of static methods, rather
// than instance methods
public static unsafe class NAHandler
{

    // A setter for the
    // array, that dynamically
    // resizes it, if the
    // given index is beyond
    // the actual length
    public static void Set<T>(int index, T value, NA<T>* array)
        where T : unmanaged
    {
        if((index + 1) > array->Length)
        {
            array->Length = index + 1;

            array->Values =
                (T*)NativeMemory.Realloc(array->Values, (nuint)(sizeof(T) * array->Length));
        }

        array->Values[index] = value;
    }

    // A getter that returns
    // the value at the given
    // index of the array.
    // Returns default, if the
    // index goes beyond
    // the length of the array
    public static T Get<T>(int index, NA<T>* array)
        where T : unmanaged
    {
        if((index + 1) > array->Length)
            return default;

        return array->Values[index];
    }


    // The same as the
    // "Get" function,
    // but returns a
    // pointer instead
    // of the value
    public static T* GetPtr<T>(int index, NA<T>* array)
        where T : unmanaged
    {
        if((index + 1) > array->Length)
            return default;

        return &array->Values[index];
    }

    
    // Frees the given array
    public static void Free<T>(NA<T>* array)
        where T : unmanaged
        => NativeMemory.Free(array->Values);


    // Copies a manged array to a native array
    public static void ManagedToNative<T>(NA<T>* array, T[] mArray)
        where T : unmanaged
    {
        // Allocate space for
        // the new unmanaged
        // array
        array->Values =
            (T*)NativeMemory.Alloc((nuint)(sizeof(T) * mArray.Length));

        // Set the length
        // of the array
        array->Length = mArray.Length;

        // Iterate through each
        // element of the unamanged
        // array...
        for(int i = 0; i < array->Length; i++)
            // Set it to the
            // same as the value
            // in the same index
            // of the managed array
            array->Values[i] = mArray[i];
    }


    // Checks if two
    // native arrays
    // are the same
    public static bool IsSame<T>(NA<T>* array, NA<T>* compareTo)
        where T : unmanaged
    {
        // If the lengths
        // don't match...
        if(array->Length != compareTo->Length)
            // Return false
            return false;

        // Iterate through the
        // elements of both arrays
        for(int i = 0; i < array->Length; i++)
        {
            // If the arrays don't have
            // the same value at the current
            // index...
            if(!EqualityComparer<T>.Default.Equals(Get(i, array), Get(i, compareTo)))
                // Return false
                return false;
        }

        // No inequailities
        // have been detected.
        // Return true
        return true;
    }


    // Keeping this for the
    // SIMDfication of the
    // engine's backend.

    // Compares both
    // arrays by the byte
    public static bool IsSameByteComp<T>(NA<T>* array, NA<T>* compareTo)
        where T : unmanaged
    {
        // If the lengths
        // od the arrays
        // don't match...
        if(array->Length != compareTo->Length)
            // Return false
            return false;

        // Setup byte array "a"
        // by taking the adress
        // of the first array
        byte* a = (byte*)array->Values;

        // Setup byte array "c"
        // by taking the adress
        // of the array to compare to
        byte* c = (byte*)compareTo->Values;

        // Calculate
        // the lengths
        // of both
        // byte arrays
        int length = array->Length * sizeof(T);

        // Iterate through both
        // arrays
        for(int i = 0; i < length; i++)
        {
            // If the values
            // of both arrays
            // from the current
            // index don't match...
            if(a[i] != c[i])
                // Return false
                return false;
        }

        // No inequalities
        // have been found.
        // Return true
        return false;
    }


    // Checks if the
    // given value is
    // within the
    // given array
    public static bool Contains<T>(T value, NA<T>* array)
        where T : unmanaged
    {
        if(array->Length == 0)
            return false;

        for(int i = 0; i < array->Length; i++)
        {
            if(!EqualityComparer<T>.Default.Equals(value, array->Values[i]))
                continue;

            return true;
        }


        return false;
    }
}