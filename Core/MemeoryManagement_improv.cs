

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Core.MemoryManagement;


// A helper class for handling the
// smart pointer. In this case, a
// smart pointer is a plain old
// pointer with seamless changes
// to it's intended behaviour.
//
// A smart pointer looks
// as follows in memory:
//
// -----------------------------------------------------------------
// int (length cache) | elements (element 1, element 2, element3...)
// -----------------------------------------------------------------
//                    ^
//                    The adress that's returned to the user
//
public static unsafe class SmartPointer
{
    // Creates a smart pointer
    // with the desired size
    public static T* CreateSmartPointer<T>(int length = 0)
        where T : unmanaged
    {
        // Allocate memory
        // for the pointer
        // and it's length cache
        T* ptr =
            (T*)NativeMemory.Alloc((nuint)(sizeof(int) + sizeof(T) * length));

        // Save the length
        // of the fresh
        // smart pointer to
        // it's length cache
        ((int*)ptr)[0] = length;

        // Return the adress of
        // the pointer, where
        // the elements begin
        return (T*)((nint)ptr + sizeof(int));
    }

    // Returns the length of the
    // smart pointer, that was
    // saved in it's length cache
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetSmartPointerLength(void* pointer)
        // Negative indices in a plain
        // old pointer indexer seem to be
        // turned to their absolutes. Noted!
        => *(int*)((nint)pointer - 4);

    // A helper method for setting
    // a value into the list of
    // elements of the smart pointer.
    // Also resizes it to take some
    // of the user's work off
    // of their hands
    public static void Set<T>(T** pointer, int index, T value)
        where T : unmanaged
    {
        // If the given index is greater
        // than what the pointer already stores...
        if(index + 1 > *(int*)(*(nint*)pointer - 4))
        {
            // Allocate new memory
            // to the pointer
            *pointer =
                (T*)NativeMemory.Realloc((void*)(*(nint*)pointer - 4), (nuint)(sizeof(int) + sizeof(T) * (index + 1)));

            // Set the new length
            // of the pointer
            ((int*)*pointer)[0] = index + 1;

            // Push the pointer back
            // to the beginning of
            // the elements
            *pointer = (T*)(*(nint*)pointer + sizeof(int));
        }

        // Add the new value
        // to the elements of
        // the pointer
        (*pointer)[index] = value;
    }

    // A helper method for resizing
    // the smart pointer, as things
    // can get out of hand, if not
    // resized correctly
    public static void Resize<T>(T** pointer, int length)
        where T : unmanaged
    {
        // If the new length is the
        // same as the pointer's length...
        if(length == *(int*)(*(nint*)pointer - 4))
            // Prematurely
            // end the method
            return;

        // Allocate new memory
        // to the pointer
        *pointer =
            (T*)NativeMemory.Realloc((void*)(*(nint*)pointer - 4), (nuint)(sizeof(int) + sizeof(T) * length));

        // Set the new length
        // of the pointer
        ((int*)*pointer)[0] = length;

        // Push the pointer back
        // to the beginning of
        // the elements
        *pointer = (T*)(*(nint*)pointer + sizeof(int));
    }

    // Helper method fro freeing
    // a smart pointer
    public static void Free<T>(T* pointer)
        where T : unmanaged

        => NativeMemory.Free((void*)((nint)pointer - 4));


}