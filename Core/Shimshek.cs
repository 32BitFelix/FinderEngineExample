


using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Xml;
using Core.ECSS;
using Core.Engine;
using Core.GLHelper;
using Core.MemoryManagement;
using Core.Transformations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

using static Core.MemoryManagement.NAHandler;

namespace Core.Shimshek;

// The component that
// represents a 2D image
[Component]
public unsafe struct Sprite
{
    // static constructor
    static Sprite()
    {
        // Set the deserialized image coordinate
        // norms to the same as opengl's
        StbImage.stbi_set_flip_vertically_on_load(1);


        // Initialise the shader
        gShader =
            ShaderHelper.CreateShader("Sprite.vert", "Sprite.frag");

        // Use the shader
        GL.UseProgram(gShader);


        // Generate geometry
        // vertex buffer
        gGVBO = GL.GenBuffer();

        // Bind to the vertex buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, gGVBO);

        // Allocate the data of the buffer
        GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float), Vertices, BufferUsageHint.DynamicDraw);


        // Generate vertex buffer
        gVBO = GL.GenBuffer();

        // Bind to the vertex buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, gVBO);

        // Allocate the data of the buffer
        GL.BufferData(BufferTarget.ArrayBuffer, 0, 0, BufferUsageHint.DynamicDraw);

        // Set the buffer length
        bufferLength = 0;


        // Generate vertex array buffer
        gVAO = GL.GenVertexArray();

        // Bind to the vertex array buffer
        GL.BindVertexArray(gVAO);


            // The offset between each
            // vertex attribute
            int offset = 0;


            // vertex position

            // Bind to geametry buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, gGVBO);

            // Enable the vertex attribute
            GL.EnableVertexAttribArray(0);

            // Set the pointer for the attribute 
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            // Set the divisor
            GL.VertexAttribDivisor(0, 0);


            // Texture coordinate

            // Enable the vertex attribute
            GL.EnableVertexAttribArray(1);

            // Set the pointer for the attribute 
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            // Set the divisor
            GL.VertexAttribDivisor(1, 0);
        

            // Color

            // Bind to vertex buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, gVBO);

            // Enable the vertex attribute
            GL.EnableVertexAttribArray(2);

            // Set the pointer for the attribute 
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, blockSize, offset);

            // Set the divisor
            GL.VertexAttribDivisor(2, 1);

            // Increment the offset
            offset += 4 * sizeof(byte);


            // Texture

            // Enable the vertex attribute
            GL.EnableVertexAttribArray(3);

            // Set the pointer for the attribute 
            GL.VertexAttribIPointer(3, 2, VertexAttribIntegerType.Int, blockSize, offset);

            // Set the divisor
            GL.VertexAttribDivisor(3, 1);
            
            // Increment the offset
            offset += 2 * sizeof(int);


            // Model matrix

            // Enable the vertex attribute
            GL.EnableVertexAttribArray(4);

            // Set the pointer for the attribute 
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, blockSize, offset);

            // Set the divisor
            GL.VertexAttribDivisor(4, 1);

            // Increment the offset
            offset += 4 * sizeof(float);


            // Enable the vertex attribute
            GL.EnableVertexAttribArray(5);

            // Set the pointer for the attribute 
            GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, blockSize, offset);

            // Set the divisor
            GL.VertexAttribDivisor(5, 1);

            // Increment the offset
            offset += 4 * sizeof(float);


            // Enable the vertex attribute
            GL.EnableVertexAttribArray(6);

            // Set the pointer for the attribute 
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, blockSize, offset);

            // Set the divisor
            GL.VertexAttribDivisor(6, 1);

            // Increment the offset
            offset += 4 * sizeof(float);


            // Enable the vertex attribute
            GL.EnableVertexAttribArray(7);

            // Set the pointer for the attribute 
            GL.VertexAttribPointer(7, 4, VertexAttribPointerType.Float, false, blockSize, offset);

            // Set the divisor
            GL.VertexAttribDivisor(7, 1);



        // Generate element buffer
        gEBO = GL.GenBuffer();

        // Bind to the element buffer
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, gEBO);

        // Populate the element buffer's data
        GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(byte), Indices, BufferUsageHint.DynamicDraw);


        // Initialise the array
        // that'll hold the identifiers
        // of the not occluded sprites
        // and their distances
        nonOccludedSprites =
            (int*)NativeMemory.Alloc(0);


        // Initialise the texture
        // info array
        textureObjects = new NA<TextureInfo>();


        // Stackallovate array for the
        // dfeault texture's pixels
        uint* pixels = stackalloc uint[4];


        pixels[0] = 0xFFCCCCCC;

        pixels[1] = 0xFFAAAAAA;

        pixels[2] = 0xFFAAAAAA;

        pixels[3] = 0xFFCCCCCC;


        // Create standard texture
        TextureInfo defInfo = new TextureInfo
        {
            TextureID = TextureHelper.CreateTextureFromData((byte*)pixels, 2, 2),

            TextureData = null,

            X = 2,

            Y = 2,
        };


        // Add default texture
        // to the texture object
        // collection
        fixed(NA<TextureInfo>* tPtr = &textureObjects)
            Set(0, defInfo, tPtr);


        // Create the bindless texture
        // handle of the default texture
        long bindlessHandle = GL.Arb.GetTextureHandle(defInfo.TextureID);

        // Make the texture handle
        // resident. (It tells opengl
        // that's it's, in fact, needed)
        GL.Arb.MakeTextureHandleResident(bindlessHandle);


        // Add default bindless
        // texture handle to the
        // bindless handle collection
        fixed(NA<long>* lPtr = &bindlessTextures)
            Set(0, bindlessHandle, lPtr);
    }

    // The indices of each
    // triangle
    private static byte[] Indices = 
    {
        0, 1, 2, // First triangle
        0, 2, 3 // Second trianle
    };

    // The vertices of
    // each triangle
    private static float[] Vertices =
    {
         1f,  1f, .0f,  1.0f, 1.0f,  // top right
         1f, -1f, .0f,  1.0f, 0.0f,  // bottom right
        -1f, -1f, .0f,  0.0f, 0.0f,  // bottom left
        -1f,  1f, .0f,  0.0f, 1.0f   // top left
    };

    // The shader globally used
    // by all sprites. This points
    // to an instruction set for
    // the graphics card
    // to render sprites
    private static int gShader;

    // The gemoetry vertex buffer object
    // used by all sprites. It stores the
    // position of each vertex
    private static int gGVBO;

    // The vertex buffer object
    // used by all sprites.
    // This holds the data of
    // all sprites
    private static int gVBO;

    // The vertex array object
    // used by all sprites.
    // It specifies where each
    // field of data
    // are partitioned at
    private static int gVAO;

    // The element buffer object
    // used by all sprites.
    // This points to a set of
    // values that explain what
    // triangle each vertex of a
    // sprite belongs to
    private static int gEBO;

    // The amount of space that
    // has been allocated, measured
    // per sprite
    private static int bufferLength;


    // The array to hold the
    // arrays waiting for
    // initialisation in opengl
    private static NA<TextureInfo> textureObjects;


    // The array to hold the
    // bindless texture ids
    // of the texture objects
    private static NA<long> bindlessTextures;


    // Indicates if the texture object
    // array has been modified without
    // notice from the texture manager
    private static bool isTextureArrayDirty;


    // Loads a texture from the
    // given texture info
    public static int LoadTextureInfo(TextureInfo info)
    {
        // Cache of the index the
        // texture will be saved at.
        // Defaults to the length of
        // the texture object array
        int nIndex = textureObjects.Length;

        // Find if there is an
        // availlable slot for
        // the texture object array
        for(int i = 1; i < textureObjects.Length; i++)
        {   
            // If the current index
            // has no assigned texture
            // object id...
            if(textureObjects.Values[i].TextureID == 0)
                // If the current index
                // has no data to read...
                if(textureObjects.Values[i].TextureData != null)
                    // Skip to the
                    // next iteration
                    continue;

            // Set the index for
            // the new texture object
            // to the current index
            nIndex = i;
        }   


        // Tell that the texture object
        // array has been tampered with
        isTextureArrayDirty = true;


        // Fix the texture object
        // array . . .
        fixed(NA<TextureInfo>* tPtr = &textureObjects)
            // Add the new texture object
            // to the array
            Set(nIndex, info, tPtr);

        // Return the index
        // of the newly
        // created texture object
        return nIndex;
    }


    // Loads a texture from the
    // given path
    public static int LoadTexture(string path)
    {
        // Cache of the index the
        // texture will be saved at.
        // Defaults to the length of
        // the texture object array
        int nIndex = textureObjects.Length == 0 ?
            1 : textureObjects.Length;

        // Find if there is an
        // availlable slot for
        // the texture object array
        for(int i = 1; i < textureObjects.Length; i++)
        {   
            // If the current index
            // has no assigned texture
            // object id...
            if(textureObjects.Values[i].TextureID == 0)
                // If the current index
                // has no data to read...
                if(textureObjects.Values[i].TextureData != null)
                    // Skip to the
                    // next iteration
                    continue;

            // Set the index for
            // the new texture object
            // to the current index
            nIndex = i;
        }   

        // Load the image
        ImageResult image =
            ImageResult.FromStream(File.OpenRead("./rsc/" + path), ColorComponents.RedGreenBlueAlpha);

        // Allocate unsafe
        // memory
        byte* nMemory =
            (byte*)NativeMemory.Alloc((nuint)(image.Width * image.Height * 4));

        // Copy data from the managed array
        // to the unmanged array
        Marshal.Copy(image.Data, 0, (nint)nMemory, image.Width * image.Height * 4);

        // Tell that the texture object
        // array has been tampered with
        isTextureArrayDirty = true;

        // Create the textureinfo
        // of the new texture object
        TextureInfo texInfo =
            new TextureInfo()
            {
                // Null the texture id 
                TextureID = 0,

                // Set the data to read
                TextureData = nMemory,

                // Set the width of
                // the texture
                X = (short)image.Width,

                // Set the height of
                // the texture
                Y = (short)image.Height
            };  

        // Fix the texture object
        // array . . .
        fixed(NA<TextureInfo>* tPtr = &textureObjects)
            // Add the new texture object
            // to the array
            Set(nIndex, texInfo, tPtr);

        // Return the index
        // of the newly
        // created texture object
        return nIndex;
    }

    // Loads an atlas from the
    // given path
    public static NA<int> LoadAtlas(string path, short atletResolution)
    {
        // Set the deserialized image coordinate norms to the same as opengl's
        StbImage.stbi_set_flip_vertically_on_load(1);

        // Load the image
        ImageResult image =
            ImageResult.FromStream(File.OpenRead("./rsc/" + path), ColorComponents.RedGreenBlueAlpha);

        // The array to store
        // the pixels in an
        // unmanaged array
        byte* pixelsRaw = (byte*)NativeMemory.Alloc((nuint)image.Data.Length);

        // Copy the values from the
        // managed array to the unmanaged
        // array in a very basic, but safe fashion
        for(int i = 0; i < image.Data.Length; i++)
            // Set the element in the current
            // index of the managed array into
            // the current index of the unmanaged array
            pixelsRaw[i] = image.Data[i];

        // Initialise array to hold the
        // texture infos
        NA<TextureInfo> texInfos =
            new NA<TextureInfo>(image.Width / atletResolution * (image.Height / atletResolution));

        // Initialise the elements in the
        // array that'll each hold an atlet
        for(int i = 0; i < texInfos.Length; i++)
        {   
            // Initialise current
            // element
            TextureInfo texInfo =
                new TextureInfo(){TextureData = (byte*)NativeMemory.Alloc((nuint)(atletResolution * atletResolution * sizeof(int))),
                                    Y = atletResolution,
                                    X = atletResolution};

            // Set the current element
            // into the array
            Set(i, texInfo, &texInfos);
        }

        // Iterate through each pixel in
        // the raw image
        for(int i = 0; i < image.Height * image.Width; i++)
        {
            // The index of the current atlet
            // in the x dimension
            int currentHeight = i / image.Width / atletResolution;

            // The index of the current atlet
            // in the y dimension
            int currentWidth = i % image.Width / atletResolution;

            // The index of the current
            // atlet in the atlet collection
            int atletIndex = currentHeight * (image.Width / atletResolution) + currentWidth;

            // The index of the element
            // that is going to be populated
            // relative to the buffer element index
            int lineIndex = i % atletResolution;

            // The index of the element in the
            // buffer to populate
            int bufferElementIndex = i / image.Width % atletResolution;


            ((int*)texInfos.Values[atletIndex].TextureData)[bufferElementIndex * atletResolution + lineIndex] = ((int*)pixelsRaw)[i];
        }

        // Free the raw image data
        // container
        NativeMemory.Free(pixelsRaw);

        // Initialise the array
        // that'll hold the 
        NA<int> texes = new NA<int>(texInfos.Length);


        // The index at which 
        // the following loop
        // will be looking for
        // free indices
        int liftOff = 1;

        // Iterate through each texture info...
        for(int i = 0; i < texInfos.Length; i++)
        {
            // Cache of the index the
            // texture will be saved at.
            // Defaults to the length of
            // the texture object array
            int nIndex = textureObjects.Length == 0 ?
                1 : textureObjects.Length;

            // Find if there is an
            // availlable slot for
            // the texture object array
            for(int j = liftOff; j < textureObjects.Length; j++)
            {   
                // If the current index
                // has no assigned texture
                // object id...
                if(textureObjects.Values[j].TextureID == 0)
                    // If the current index
                    // has no data to read...
                    if(textureObjects.Values[j].TextureData != null)
                        // Skip to the
                        // next iteration
                        continue;

                // Set the index for
                // the new texture object
                // to the current index
                nIndex = j;

                // Set the liftoff
                // to an index
                liftOff = j + 1;

                // End the loop
                break;
            }   

            // Fix the texture object
            // array . . .
            fixed(NA<TextureInfo>* tPtr = &textureObjects)
                // Add the new texture object
                // to the array
                Set(nIndex, texInfos.Values[i], tPtr);

            // Save the index into
            // the texes array
            texes.Values[i] = nIndex;
        }


        isTextureArrayDirty = true;


        // Return the array
        // that holds the
        // texture arrays
        return texes;
    }


        // Does some management for the
        // texture as soon as it's called
        private static void manageTextures()
        {
            // Iterate through each texture 
            for(int i = 1; i < textureObjects.Length; i++)
            {
                // If current iteration is initialised...
                if(textureObjects.Values[i].TextureID != 0)
                    // Skip to the
                    // next iteration
                    continue;

                // If the current iteration has no
                // data to read from...
                if(textureObjects.Values[i].TextureData == null)
                    // Skip to the
                    // next iteration
                    continue;

                // Get a copy of the
                // current texture info
                TextureInfo currentInfo =
                    textureObjects.Values[i];

                // Create the plain old
                // texture object
                int texID =
                    TextureHelper.CreateTextureFromData(currentInfo.TextureData, currentInfo.X, currentInfo.Y);

                // Create a bindless texture
                long bindlessID = GL.Arb.GetTextureHandle(texID);

                // Make the texture handle
                // resident. (It tells opengl
                // that's it's, in fact, needed)
                GL.Arb.MakeTextureHandleResident(bindlessID);

                // Fix the array...
                fixed(NA<long>* bPtr = &bindlessTextures)
                    // Add the new
                    // bindless texture
                    Set(i, bindlessID, bPtr);
            }

            // Make sure to tell the
            // renderer that the
            // texture array has been
            // managed like a boss
            isTextureArrayDirty = false;
        }


    // Called every render pass
    [ComponentRender]
    public static void Render()
    {

        // If the texture object array
        // has been tampered with...
        if(isTextureArrayDirty)
            // Call the texture manager
            manageTextures();

        // Reference to the
        // Camera array
        Camera* cameras;

        // Reference to the
        // entities bound to
        // the camera
        int* camEntities;

        // The length of
        // both arrays
        int camLength;

        // Get the informations necessary
        // for the camera array
        ECSSHandler.GetCompactColumn(&cameras, &camEntities, &camLength);


        // Reference to the
        // sprite array
        Sprite* sprites;

        // Reference to the
        // entities bound to
        // the sprites
        int* spriteEntities;

        // The length of
        // both arrays
        int spriteLength;

        // Get the informations necessary
        // for the sprite array
        ECSSHandler.GetCompactColumn(&sprites, &spriteEntities, &spriteLength);


        // The matrix to hold the view
        // matrix of each camera
        Matrix4 view;

        // The matrix to hold the projection
        // matrix of each camera
        Matrix4 projection;


        // Use the global shader
        // of all sprites
        GL.UseProgram(gShader);

        // Bind to global vertex array
        // of all sprites
        GL.BindVertexArray(gVAO);


        // Iterate through each camera component
        for(int i = 1; i < camLength; i++)
        {
            // If the current camera is either
            // nonexistent or diabled...
            if(!ECSSHandler.GetEnableState(camEntities[i]))
                // Skip to the
                // next camera
                continue;


            // If the current camera doesn't
            // contain a translation component...
            if(!ECSSHandler.ContainsComponent<Scale>(camEntities[i]))
                // Skip to the
                // next camera
                continue;


            // Set the view matrix of
            // the current camera
            view = Gymbal.GetViewMatrix(camEntities[i]);

            // Set the projection matrix
            // of the current camera
            Camera.GetProjectionMatrix(camEntities[i], &projection);


            ShaderHelper.SetMat4("uProjection", &projection, gShader);

            ShaderHelper.SetMat4("uView", &view, gShader);


            occlusionAndOrderPass(sprites, spriteEntities, spriteLength, camEntities[i]);


            opaquePass();


            transparentPass();
        }

    }

        // The radius of the
        // circle surrounding
        // every sprite (precalculated, rounded... and afraid)
        private const float spriteRadius = 1.4f;

        // The list of sprite IDs and their
        // distances that are not occluded
        private static int* nonOccludedSprites;

        // The amount of sprites
        // within the view
        private static int spritesInCircle;

        // The pass where the sprites visible
        // to the camera get picked out and
        // ordered to closeness to the camera
        private static void occlusionAndOrderPass(Sprite* sprites, int* spriteEntities,
            int spriteLength, int camID)
        {
            // Get the translation
            // of the camera
            Vector3 camPos = Gymbal.GetRelativeTranslation(camID);


            // The radius of the
            // circle around the
            // sprite
            float camRadius = 0;
            
            // Get the the radius of
            // the circle surrounding
            // the camera's aperture
            Camera.GetRadius(camID, &camRadius);


            // Stores the last index
            // in the nonOccludeSprites
            // array that was overwritten
            int lastWrittenIndex = 0;


            // See how many sprites
            // are within the possible
            // view of the camera

            // Iterate through each sprite
            for(int i = 1; i < spriteLength; i++)
            {
                if(spriteEntities[i] == 0)
                    continue;


                if(!ECSSHandler.GetEnableState(spriteEntities[i]))
                    continue;


                if(!ECSSHandler.ContainsComponent<Scale>(spriteEntities[i]))
                    continue;


                // Get the translation of
                // the current sprite
                Vector3 spritePos =
                    Gymbal.GetRelativeTranslation(spriteEntities[i]);

                // Get the scale of
                // the current sprite
                Vector3 spriteScale =
                    Gymbal.GetRelativeScale(spriteEntities[i]);

                // Calculate the distance between
                // the camera and the sprite
                float distance =
                    Vector3.Distance(camPos, spritePos);


                // If the distance is 
                // greater than the
                // sum of the radii
                // of the sprite and
                // the camera
                if(distance > camRadius + (spriteRadius * getLargestScale(spriteScale)))
                    // Skip to the
                    // next sprite
                    continue;


                // If the sprite to add
                // to the non-occluded
                // list will exceed the
                // limits of the list....
                if(lastWrittenIndex + 2 > spritesInCircle)
                    // Resize the list
                    nonOccludedSprites = (int*)NativeMemory.Realloc(nonOccludedSprites, (nuint)(sizeof(int) * 2 * (lastWrittenIndex + 2)));


                // Store the ID of the
                // current sprite
                nonOccludedSprites[lastWrittenIndex++] = spriteEntities[i];


                // Store the distance
                // of the current sprite
                // to the camera
                *(float*)&nonOccludedSprites[lastWrittenIndex++] = distance;
            }


            spritesInCircle = lastWrittenIndex >> 1;


            // If the length of the buffer
            // is less than the sprite array...
            if(bufferLength < spritesInCircle)
            {
                // Change the buffer length
                bufferLength = spritesInCircle;

                // Bind to the global vertex buffer
                GL.BindBuffer(BufferTarget.ArrayBuffer, gVBO);

                // Resize the buffer to the
                // new length
                GL.BufferData(BufferTarget.ArrayBuffer, bufferLength * blockSize, 0, BufferUsageHint.DynamicDraw);
            }


            // Reorder the informations based
            // on the distance from closest
            // to farthest

            sortIDs();
        }

            // Returns the largest dimension
            // from a scale vector
            private static float getLargestScale(Vector3 scale)
            {
                // Initialise initial
                // value by setting it
                // to the first scale
                float val = scale.X;

                // If the y dimension
                // is greater than the
                // previous greatest value...
                if(scale.Y > val)
                    // Set the greatets value
                    // to the y dimension
                    val = scale.Y;

                // If the z dimension
                // is greater than the
                // previous greatest value...
                if(scale.Z > val)
                    // Return the z-
                    // dimension
                    return scale.Z;

                // Return the greatest
                // found value
                return val;
            }

            // Sorts the visible
            // sprite IDs depending
            // on their distances
            // with radix sort
            private static void sortIDs()
            {
                // Create a buffer that'll
                // swap itself by each pass
                long* swapBuffer =
                    (long*)NativeMemory.Alloc((nuint)(sizeof(long) * spritesInCircle * 2));


                // The counter counts the
                // duplicates of numbers
                // from 0 to 255
                int* counters = stackalloc int[256];

                // The offset table holds
                // the offsets of each number
                int* offsetTable = stackalloc int[256];


                // Iterate through each element
                // and count up it's fitting counter
                for(int i = 0; i < spritesInCircle; i++)
                {
                    // This extracts the highest 4 bytes
                    // of the long
                    int currentValue = (int)((((long*)nonOccludedSprites)[i]) >> 32);

                    byte b = (byte)currentValue;

                    counters[b]++;
                }


                // Set up the offsets for
                // each element
                for(int i = 1; i < 256; i++)
                    offsetTable[i] = offsetTable[i - 1] + counters[i - 1];
                    
                // Copy the values from the
                // current buffer to the
                // next buffer according to
                // the offset table
                for(int i = 0; i < spritesInCircle; i++)
                {
                    // This extracts the highest 4 bytes
                    // of the long
                    int currentValue = (int)((((long*)nonOccludedSprites)[i]) >> 32);

                    byte b = (byte)currentValue;

                    swapBuffer[offsetTable[b]++] = ((long*)nonOccludedSprites)[i];
                }


                // Clear the counters
                for(int i = 0; i < 256; i++)
                    counters[i] = 0;

                // Clear the offset table
                for(int i = 0; i < 256; i++)
                    offsetTable[i] = 0;


                // The first three passes
                for(int p = 1; p < 3; p++)
                {
                    // Calculates which partition
                    // of the swapbuffer comes next
                    byte order = (byte)(p % 2);

                    // The offset of the
                    // current buffer
                    int currentBufferOffset = (order ^ 1) * spritesInCircle;

                    // The offset of the
                    // other buffer
                    int otherBufferOffset = order * spritesInCircle;


                    // Iterate through each element
                    // and count up it's fitting counter
                    for(int i = 0; i < spritesInCircle; i++)
                    {
                        // This extracts the highest 4 bytes
                        // of the long
                        int currentValue = (int)((swapBuffer[currentBufferOffset + i]) >> 32);

                        byte b = (byte)(currentValue >> (p * 8));

                        counters[b]++;
                    }


                    // Set up the offsets for
                    // each element
                    for(int i = 1; i < 256; i++)
                        offsetTable[i] = offsetTable[i - 1] + counters[i - 1];
                    
                    // Copy the values from the
                    // current buffer to the
                    // next buffer according to
                    // the offset table
                    for(int i = 0; i < spritesInCircle; i++)
                    {
                        // This extracts the highest 4 bytes
                        // of the long
                        int currentValue = (int)((swapBuffer[currentBufferOffset + i]) >> 32);

                        byte b = (byte)(currentValue >> (p * 8));

                        swapBuffer[otherBufferOffset + offsetTable[b]++] = swapBuffer[currentBufferOffset + i];
                    }

                    // Clear the counters
                    for(int i = 0; i < 256; i++)
                        counters[i] = 0;

                    // Clear the offset table
                    for(int i = 0; i < 256; i++)
                        offsetTable[i] = 0;
                }


                // The final pass

                // Iterate through each element
                // and count up it's fitting counter
                for(int i = 0; i < spritesInCircle; i++)
                {
                    // This extracts the highest 4 bytes
                    // of the long
                    int currentValue = (int)((swapBuffer[i]) >> 32);

                    byte b = (byte)(currentValue >> 24);

                    counters[b]++;
                }


                // Set the first offset to
                // the amount of negatve values
                offsetTable[0] = 0;

                // Set up the offsets for
                // each element
                for(int i = 1; i < 256; i++)
                    offsetTable[i] = offsetTable[i - 1] + counters[i - 1];


                // Now finish the sorting
                // by applying the result
                // to the given array
                for(int i = 0; i < spritesInCircle; i++)
                {
                    // This extracts the highest 4 bytes
                    // of the long
                    int currentValue = (int)((swapBuffer[i]) >> 32);

                    byte b = (byte)(currentValue >> 24);

                    ((long*)nonOccludedSprites)[offsetTable[b]++] = swapBuffer[i];
                }


                // Free the swapbuffer
                NativeMemory.Free(swapBuffer);
            }

        // The size of a block of sprite data
        private const int blockSize = 18 * sizeof(float) + 4;

        // First, only the opaque objects
        // will bew drawn in a
        // front to back order
        private static void opaquePass()
        {
            // Map the vertex buffer to a
            // cpu side write only array
            byte* buffer =
                (byte*)GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);

            // Iterate through each sprite
            for(int i = 0; i < spritesInCircle * 2; i += 2)
            {
                if(nonOccludedSprites[i] == 0)
                    continue;

                // Get the sprite
                Sprite* s =
                    ECSSHandler.GetComponent<Sprite>(nonOccludedSprites[i]);

                // The offset for each
                // block of data
                int offset = blockSize * (i / 2);


                // Set the color modifier
                // of the current sprite
                *(int*)&buffer[offset] = *(int*)&s->Red;

                offset += 4;


                // Set the bindless texture
                *(long*)&buffer[offset] = bindlessTextures.Values[s->TextureObjectIndex];

                offset += 8;


                // Set the model matrix of
                // the sprite
                *(Matrix4*)&buffer[offset] = Gymbal.GetModelMatrix(nonOccludedSprites[i]);

                // Transpose the model matrix
                ((Matrix4*)&buffer[offset])->Transpose();
            }

            // Unmap the vertex buffer
            GL.UnmapBuffer(BufferTarget.ArrayBuffer);

            // Set transparent pass 
            // to false
            ShaderHelper.SetInt("uIsTransparentPass", 0, gShader);

            // Draw the instances
            GL.DrawElementsInstanced(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedByte, 0, spritesInCircle);
        }

        // Second, only the transparent objects
        // will bew drawn in a back to front order
        private static void transparentPass()
        {
            // Map the vertex buffer to a
            // cpu side write only array
            byte* buffer =
                (byte*)GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);

            // Iterate through each sprite
            for(int i = spritesInCircle * 2 - 1; i >= 0 ; i -= 2)
            {
                if(nonOccludedSprites[i - 1] == 0)
                    break;

                // Get the sprite
                Sprite* s =
                    ECSSHandler.GetComponent<Sprite>(nonOccludedSprites[i - 1]);

                // The offset for each
                // block of data
                int offset = blockSize * (i / 2);


                // Set the color modifier
                // of the current sprite
                *(int*)&buffer[offset] = *(int*)&s->Red;

                offset += 4;


                // Set the bindless texture
                *(long*)&buffer[offset] = bindlessTextures.Values[s->TextureObjectIndex];

                offset += 8;


                // Set the model matrix of
                // the sprite
                *(Matrix4*)&buffer[offset] = Gymbal.GetModelMatrix(nonOccludedSprites[i - 1]);

                // Transpose the model matrix
                ((Matrix4*)&buffer[offset])->Transpose();
            }

            // Unmap the vertex buffer
            GL.UnmapBuffer(BufferTarget.ArrayBuffer);

            // Set transparent pass 
            // to false
            ShaderHelper.SetInt("uIsTransparentPass", 1, gShader);

            // Draw the instances
            GL.DrawElementsInstanced(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedByte, 0, spritesInCircle);
        }


    // The red component
    // of the sprite's
    // color multiplier
    public byte Red,
        // The green component
        // of the sprite's
        // color multiplier
        Green,

        // The blue component
        // of the sprite's
        // color multiplier
        Blue,

        // The alpha component
        // of the sprite's
        // color multiplier
        Alpha;


    // The index of the applied
    // texture object from the
    // texture object array
    public int TextureObjectIndex;
}

// A structure to hold the
// data of a structure for
// the initialisation of a
// texture object in opengl
public unsafe struct TextureInfo
{
    // The id of the
    // texture object
    public int TextureID;

    // The array to hold
    // the pixel data
    public byte* TextureData;

    // The X and Y Dimensions
    // of the texture
    public short X, Y;
}

// The component that
// represents a camera.
// All things are seen by this
[Component]
public unsafe struct Camera
{

    [ComponentInitialise]
    public static void Init(int entityID)
    {
        Camera* cam = ECSSHandler.GetComponent<Camera>(entityID);

        cam->AspectRatio = (float)FinderEngine.GetWindowSize.X / FinderEngine.GetWindowSize.Y;
    }


    [ComponentResize]
    public static void Resize()
    {   
        Camera* cameras;

        int* entities;

        int cameraLength;


        ECSSHandler.GetCompactColumn(&cameras, &entities, &cameraLength);


        float nAspect = (float)FinderEngine.GetWindowSize.X / FinderEngine.GetWindowSize.Y;

        for(int i = 1; i < cameraLength; i++)
            cameras[i].AspectRatio = nAspect;
    }


    // The field of view of the camera
    private float _fov;

    // Mutator of the fov
    public float FOV
    {
        // Returns the
        // FOV as is
        get => _fov * 180 / MathHelper.Pi;

        // Sets the given value
        // to a radian
        set
        {
            float angle = MathHelper.Clamp(value, 1, 120);
            _fov = angle * (MathHelper.Pi / 180);
        }
    }

    // The aspect ratio of the window
    public float AspectRatio;

    // The orthographic fov
    public float ProjectionSize;

    // The distance of the near clip
    // from the camera away
    public float NearClip;

    // The distance of the far clip
    // from the camera away 
    public float FarClip;

    // Bool that indicates if the
    // camera is in either perspective
    // or orthographic mode
    public bool IsOrtho;

    // Returns the radius of the circle
    // that surrounds the camera
    public static void GetRadius(int entityID, float* radius)
    {
        // Get the component
        // of the camera
        Camera* c = ECSSHandler.GetComponent<Camera>(entityID);


        // If the camera is
        // orthographics...
        if(c->IsOrtho)
            // skip to
            // the orthographic
            // method
            goto ortho;


        // Get the radius in
        // a perspective way
        getRadiusPerspective(c, radius);

        return;


        ortho:

            // Get the radius in a
            // orthographic way
            getRadiusOrtho(c, radius);

            return;
    }

        // Calculates the radius of the circle
        // surrounding the orthographic aperture
        // of the camera
        private static void getRadiusOrtho(Camera* cam, float* rad)
        {
            // The back bottom left
            // vertex of the camera's
            // aperture
            Vector3 backBottomLeft = (-cam->ProjectionSize * cam->AspectRatio, -cam->ProjectionSize, cam->NearClip);

            // The center point of
            // the camera's aperture
            Vector3 middle = (0, 0, (cam->FarClip * .5f) + cam->NearClip);

            // Return the radius of the
            // circle around the
            // camera's aperture
            *rad = Vector3.Distance(backBottomLeft, middle);
        }

        // Calculates the radius of the circle
        // surrounding the perspective aperture
        // of the camera
        private static void getRadiusPerspective(Camera* cam, float* rad)
        {

        }

    // Returns the projection matrix
    // of the camera
    public static void GetProjectionMatrix(int entityID, Matrix4* mat)
    {
        // Get the camera component
        Camera* cam = ECSSHandler.GetComponent<Camera>(entityID);

        // Returns either a perspective-
        // or projection matrix depending
        // on the projection mode
        *mat = cam->IsOrtho ? Matrix4.CreateOrthographic(cam->ProjectionSize * cam->AspectRatio, cam->ProjectionSize, cam->NearClip, cam->FarClip)
            : Matrix4.CreatePerspectiveFieldOfView(cam->_fov, cam->AspectRatio, cam->NearClip, cam->FarClip);
    }
}