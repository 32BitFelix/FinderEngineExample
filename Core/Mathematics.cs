

using System.Runtime.InteropServices;

namespace Core.Mathematics;

// The class to store
// the algorithms
public unsafe static class Algorithms
{

    // Four pass Radix
    // sorting algorithm
    // for integer arrays.
    // Optimised to be as
    // efficient as possible
    // both arithmetically
    // and memory wise
    public static void RadixInt(int* array, int length)
    {
        // Create a buffer that'll
        // swap itself by each pass
        int* swapBuffer =
            (int*)NativeMemory.Alloc((nuint)(sizeof(int) * length));


        // The counter counts the
        // duplicates of numbers
        // from 0 to 255
        int* counters = stackalloc int[256];

        // The offset table holds
        // the offsets of each number
        int* offsetTable = stackalloc int[256];


        // The first three passes
        for(int p = 0; p < 3; p++)
        {
            // Calculates if the swapbuffer's
            // order has come
            byte order = (byte)(p % 2);

            // Calculates if the array's
            // order has come
            byte otherOrder = (byte)(order ^ 1);


            // Get the adress of the array
            // to read from
            int* currentArray = (int*)((nint)array * otherOrder + (nint)swapBuffer * order);

            // Get the adress of the array
            // write to
            int* otherArray = (int*)((nint)array * order + (nint)swapBuffer * otherOrder);


            // See how many instances
            // of a certain value exist
            for(int i = 0; i < length; i++)
            {
                byte b = (byte)(currentArray[i] >> (p * 8));

                counters[b]++;
            }


            // Set the offset of
            // the starting value
            offsetTable[0] = 0;

            // Set the offsets of each
            // value relative to the
            // previous value
            for(int i = 1; i < 256; i++)
                offsetTable[i] = offsetTable[i - 1] + counters[i - 1];

            // Finally, sort the given
            // values based on the
            // current byte
            for(int i = 0; i < length; i++)
            {
                byte b = (byte)(currentArray[i] >> (p * 8));

                otherArray[offsetTable[b]++] = currentArray[i];
            }


            // Clear the counter array
            for(int i = 0; i < 128; i++)
                ((long*)counters)[i] = 0;
        }


        // The final pass

        // Count every instance of a
        // certain value
        for(int i = 0; i < length; i++)
        {
            byte b = (byte)(swapBuffer[i] >> 24);

            counters[b]++;
        }


        // The number of negative
        // values in the final pass
        int numNeg = 0;

        // Counts the negative values
        // within the final pass
        for(int i = 128; i < 256; i++)
            numNeg += counters[i];


        // Set the offset
        // of the first positve value
        // to the amount of negative
        // values within the last pass
        offsetTable[0] = numNeg;

        // Set the offsets of
        // each positive value
        for(int i = 1; i < 128; i++)
            offsetTable[i] = offsetTable[i - 1] + counters[i - 1];


        // Set the offset of the
        // first negative value
        // to the very start of
        // the array
        offsetTable[128] = 0;

        // Set the offsets of
        // each negative value
        for(int i = 129; i < 256; i++)
            offsetTable[i] = offsetTable[i - 1] + counters[i - 1];


        // FInally, set the values
        // in their new order
        for(int i = 0; i < length; i++)
        {
            byte b = (byte)(swapBuffer[i] >> 24);

            array[offsetTable[b]++] = swapBuffer[i];
        }


        // Free the swapbuffer,
        // we don't need it anymore
        NativeMemory.Free(swapBuffer);    
    }


    // Four pass Radix
    // sorting algorithm
    // for floating point arrays.
    // Optimised to be as
    // efficient as possible
    // both arithmetically
    // and memory wise
    public static void RadixFloat(float* array, int length)
    {
        // Create a buffer that'll
        // swap itself by each pass
        float* swapBuffer =
            (float*)NativeMemory.Alloc((nuint)(sizeof(float) * length));


        // The counter counts the
        // duplicates of numbers
        // from 0 to 255
        int* counters = stackalloc int[256];

        // The offset table holds
        // the offsets of each number
        int* offsetTable = stackalloc int[256];


        // The first three passes
        for(int p = 0; p < 3; p++)
        {
            // Calculates if the swapbuffer's
            // order has come
            byte order = (byte)(p % 2);

            // Calculates if the array's
            // order has come
            byte otherOrder = (byte)(order ^ 1);


            // Get the adress of the array
            // to read from
            float* currentArray = (float*)((nint)array * otherOrder + (nint)swapBuffer * order);

            // Get the adress of the array
            // write to
            float* otherArray = (float*)((nint)array * order + (nint)swapBuffer * otherOrder);


            // See how many instances
            // of a certain value exist
            for(int i = 0; i < length; i++)
            {
                byte b = (byte)(((int*)currentArray)[i] >> (p * 8));

                counters[b]++;
            }


            // Set the offset of
            // the starting value
            offsetTable[0] = 0;

            // Set the offsets of each
            // value relative to the
            // previous value
            for(int i = 1; i < 256; i++)
                offsetTable[i] = offsetTable[i - 1] + counters[i - 1];

            // Finally, sort the given
            // values based on the
            // current byte
            for(int i = 0; i < length; i++)
            {
                byte b = (byte)(((int*)currentArray)[i] >> (p * 8));

                otherArray[offsetTable[b]++] = currentArray[i];
            }


            // Clear the counter array
            for(int i = 0; i < 128; i++)
                ((long*)counters)[i] = 0;
        }


        // The final pass

        // Count every instance of a
        // certain value
        for(int i = 0; i < length; i++)
        {
            byte b = (byte)(((int*)swapBuffer)[i] >> 24);

            counters[b]++;
        }


        // The number of negative
        // values in the final pass
        int numNeg = 0;

        // Counts the negative values
        // within the final pass
        for(int i = 128; i < 256; i++)
            numNeg += counters[i];


        // Set the offset
        // of the first positve value
        // to the amount of negative
        // values within the last pass
        offsetTable[0] = numNeg;

        // Set the offsets of
        // each positive value
        for(int i = 1; i < 128; i++)
            offsetTable[i] = offsetTable[i - 1] + counters[i - 1];


        // Set the offset of the
        // first negative value
        // to the very start of
        // the array
        offsetTable[255] = 0;

        // Set the offsets of
        // each negative value
        for(int i = 254; i > 127; i--)
            offsetTable[i] = offsetTable[i + 1] + counters[i + 1];


        // FInally, set the values
        // in their new order
        for(int i = 0; i < length; i++)
        {
            byte b = (byte)(((int*)swapBuffer)[i] >> 24);

            array[offsetTable[b]++] = swapBuffer[i];
        }


        // Free the swapbuffer,
        // we don't need it anymore
        NativeMemory.Free(swapBuffer);    
    }
}

// The class to store
// logical operations
public static class Logic
{

}

// The class to store
// arithmetic operations
public static class Arithmetics
{

}

// A vector of type float
public unsafe struct vecF
{
    // public constructor
    public vecF()
    {

    }

    // The dimension of the
    // vector type
    public readonly int Dimensions;

    // The amount of
    // stored vectors
    public int Count;

    // A pointer to
    // the vectors
    public void* Vectors;

    //
    public float this[int index, int dimension]
    {
        get
        {
            return 0;
        }

        set
        {
            
        }
    }
}


public struct vecI
{

}