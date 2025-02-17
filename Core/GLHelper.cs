



using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

namespace Core.GLHelper;

// A class to hold helper
// methods for the texture objects 
public static unsafe class TextureHelper
{

    // Creates a texture from the
    // given path
    public static int CreateTextureFromPath(string path)
    {
        // Generate a texture object
        int Handle = GL.GenTexture();


        // Bind the texture object
        GL.BindTexture(TextureTarget.Texture2D, Handle);


        // Set the deserialized image coordinate norms to the same as opengl's
        StbImage.stbi_set_flip_vertically_on_load(1);

        // Load the image
        ImageResult image = ImageResult.FromStream(File.OpenRead("./rsc/" + path), ColorComponents.RedGreenBlueAlpha);


        // Upload the loaded image to the gl context
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);


        // Setup texture parameters
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);


        // Generate mipmaps for the targeted texture
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);


        // Finally, return the
        // finished texture object
        return Handle;
    }

    // Creates a texture from the
    // given data
    public static int CreateTextureFromData(byte* pixels, short X, short Y)
    {
        // Generate a texture object
        int Handle = GL.GenTexture();


        // Bind the texture object
        GL.BindTexture(TextureTarget.Texture2D, Handle);


        // Upload the loaded image to the gl context
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, X, Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (nint)pixels);


        // Setup texture parameters
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);


        // Generate mipmaps for the targeted texture
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);


        // Finally, return the
        // finished texture object
        return Handle;
    }

    // Removes a texture
    // from the memory
    public static void DeleteTexture(int textureID)
        => GL.DeleteTexture(textureID);
}

// A class to contain helper methods
// for the shaders
public static unsafe class ShaderHelper
{
    // Creates a shader program with
    // the given paths
    public static int CreateShader(string vertexPath, string fragmentPath)
    {
        // Find source code of vertex shader
        string vertexShaderSource = File.ReadAllText("./shdr/" + vertexPath);

        // Find source code of fragment shader
        string fragementShaderSource = File.ReadAllText("./shdr/" + fragmentPath);

        // Create a shader object and get it's id
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        // Appends the source code to the vertex shader
        GL.ShaderSource(vertexShader, vertexShaderSource);

        // Create a shader object and get it's id
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        // Appends the source code to the fragment shader
        GL.ShaderSource(fragmentShader, fragementShaderSource);   

        // Compile the vertex shader
        GL.CompileShader(vertexShader);     

        // Get parameter from vertex shader. In this case its it's compile status
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vertSuccess);
        // If compilation failed log a message
        if(vertSuccess == 0)
            // Log the info log of the fragment shader
            Console.WriteLine(GL.GetShaderInfoLog(vertexShader));

        // Compile the vertex shader
        GL.CompileShader(fragmentShader);     

        // Get parameter from vertex shader. In this case its it's compile status
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fragSuccess);
        // If compilation failed...
        if(fragSuccess == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            // Log the info log of the fragment shader
            Console.WriteLine(GL.GetShaderInfoLog(fragmentShader));

            Console.ForegroundColor = ConsoleColor.White;
        }        

        // Create a shader object and get it's id
        int Handle = GL.CreateProgram();

        // Attach vertex shader to the shader object
        GL.AttachShader(Handle, vertexShader);
        // Attach fragment shader to the shader object
        GL.AttachShader(Handle, fragmentShader);

        // Link shader object to gpu's program query
        GL.LinkProgram(Handle);        

        // Get parameter from shader object. In this case its it's link status
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int shaderSuccess);
        // If shader object linking failed...
        if(shaderSuccess == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            // Log the info log of the shader object
            Console.WriteLine(GL.GetProgramInfoLog(Handle));

            Console.ForegroundColor = ConsoleColor.White;
        }

        // Detach the shaders from the program,
        // as their compiled data have been
        // transferred to the shader object
        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        
        // Finally, delete the shaders
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        // Return handle of the shader
        return Handle;
    }


    // Set one of the shader's uniforms
    public static void SetInt(string uniformName, int value, int shaderID)
    {
        int location = GL.GetUniformLocation(shaderID, uniformName);

        GL.Uniform1(location, value);
    }

    // Set uniform Vector3 variables
    // with the given name and value 
    public static void SetVec3(string uniformName, Vector3 value, int shaderID)
    {
        int location = GL.GetUniformLocation(shaderID, uniformName);

        GL.Uniform3(location, value.X, value.Y, value.Z);   
    }

    // Set uniform Vector4 variables
    // with the given name and value 
    public static void SetVec4(string uniformName, Vector4 value, int shaderID)
    {
        int location = GL.GetUniformLocation(shaderID, uniformName);

        GL.Uniform4(location, value.X, value.Y, value.Z, value.W);
    }

    // Set uniform Matrix4 variables
    // with the given name and value 
    public static void SetMat4(string uniformName, Matrix4* value, int shaderID)
    {
        int location = GL.GetUniformLocation(shaderID, uniformName);

        GL.UniformMatrix4(location, true, ref Unsafe.AsRef<Matrix4>(value));
    }
}