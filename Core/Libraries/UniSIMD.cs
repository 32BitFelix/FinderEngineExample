


using System.Runtime.InteropServices;

namespace UniSIMD;


public static unsafe class Intrinsics
{
    static Intrinsics()
    {
        _INIT_SIMD();

        nint libHandle = LoadLibrary("./Core/Libraries/mSIMD.dll");


        GetRegisterSize = (delegate* unmanaged[Cdecl] <byte>)*(nint*)NativeLibrary.GetExport(libHandle, "_GET_REG_SIZE");
    }


    public static readonly delegate* unmanaged[Cdecl] <byte> GetRegisterSize;


    public static readonly delegate* unmanaged[Cdecl] <float*, float*, float*, void> FloatAdd;


    public static readonly delegate* unmanaged[Cdecl] <float*, float*, float*, void> FloatSub;


    public static readonly delegate* unmanaged[Cdecl] <float*, float*, float*, void> FloatMul;


    public static readonly delegate* unmanaged[Cdecl] <float*, float*, float*, void> FloatDiv;


    [DllImport("./Core/Libraries/mSIMD.dll")]
    public static extern void _INIT_SIMD(); 


    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

}

/*

// Holds the reference
// to the method that
// returns the size of
// the registers of the
// current extension in bytes
char (*_GET_REG_SIZE)();

// Holds the reference to
// a method that adds the
// register of floats
void (*_FADD)(float*, float*, float*);

// Holds the reference to
// a method that subtracts the
// register of floats
void (*_FSUB)(float*, float*, float*);

// Holds the reference to
// a method that multiplies the
// register of floats
void (*_FMUL)(float*, float*, float*);

// Holds the reference to
// a method that divides the
// register of floats
void (*_FDIV)(float*, float*, float*);


// Methods beyond this
// point have not been
// implemented yet


// Holds the reference to
// a method that calculates the
// remainder from the registers
// of floats
void (*_FMOD)(float*, float*, float*);

// Holds the reference to
// a method that caluclates the
// squareroot from the registers
// of floats
void (*_FSQRT)(float*, float*, float*);

// Holds the reference to
// a method that caluclates the
// reverse squareroot from the registers
// of floats
void (*_FRSQRT)(float*, float*, float*);


// Holds the reference to
// a method that adds the
// register of floats
void (*_IADD)(float*, float*, float*);

// Holds the reference to
// a method that subtracts the
// register of floats
void (*_ISUB)(float*, float*, float*);

// Holds the reference to
// a method that multiplies the
// register of floats
void (*_IMUL)(float*, float*, float*);

// Holds the reference to
// a method that divides the
// register of floats
void (*_IDIV)(float*, float*, float*);

// Holds the reference to
// a method that calculates the
// remainder from the registers
// of floats
void (*_IMOD)(float*, float*, float*);

// Holds the reference to
// a method that caluclates the
// squareroot from the registers
// of floats
void (*_ISQRT)(float*, float*, float*);

// Holds the reference to
// a method that caluclates the
// reverse squareroot from the registers
// of floats
void (*_IRSQRT)(float*, float*, float*);


// Specifies the currently
// used SIMD instructions.
//   0 = SCALAR
//   1 = MMX
//   2 = SSE
//   3 = SSE2
//   4 = SSE3
//   5 = SSE41
//   6 = SSE42
//   7 = AVX
char _CURRENT_SUPPORT;

*/