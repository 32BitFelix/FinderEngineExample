using Core.ECSS;
using OpenTK.Mathematics;
using FreeTypeSharp.Native;
using Core.MemoryManagement;
using System.Runtime.InteropServices;
using Core.Shimshek;
using Core.Transformations;

// TODO:
// Add a standard font
// built into the engine's
// source code and always
// index it to zero

// TODO 2:
// Implement vertical alignment

// TODO 3:
// Implement breaks on
// the sentence

// A label represents
// plain old text that
// exists in the game world
[Component]
public unsafe struct Label
{
    // type initializer
    static Label()
    {
        // Initialise a freetype
        // library instance.
        // But if that fails...
		if(FT.FT_Init_FreeType(out lib) != FT_Error.FT_Err_Ok)
            // Exit. We got nothing
            // else to do
			return;

        // Initialise
        // the array
        // to hold the
        // fonts
        fonts = new NA<NA<Character>>(0);
    }


    // instance initializer
    public Label(string i_text, int i_fontID, Alignment i_textAlignment,
        byte i_red, byte i_green, byte i_blue, byte i_alpha)
    {
        // Initialise the array
        // to store the charcters
        // of the sentence
        Text = new NA<char>(i_text.Length);

        // Initialise the array
        // to store the entities
        // that'll display the
        // characters of the sentence
        Symbols = new NA<int>(i_text.Length);


        // Save the given
        // font id
        FontID = i_fontID;

        // Get a reference
        // to the current font
        NA<Character>* font = &fonts.Values[FontID];


        // Save the alignment of
        // the text
        TextAlignment = i_textAlignment;


        // A cache for the
        // advance of the
        // sentence
        float advance = 0f;

        // Iterate through each character
        // from the given string
        for(int i = 0; i < i_text.Length; i++)
        {
            // Copy the current character
            // to the text array of the label
            Text.Values[i] = i_text[i];


            // Create the entity that'll
            // represent the current character
            Symbols.Values[i] = ECSSHandler.CreateEntity();

            // Get the info of the
            // current character
            Character c = font->Values[Text.Values[i]];


            // Add a transformation component
            // to the symbol and set it's
            // scale, rotation and translation
            Gymbal.CreateTransform(Symbols.Values[i],
                (c.Size.X, c.Size.Y, 1f), (0f, 0f, 0f), (advance + c.Bearing.X + c.Size.X, c.Bearing.Y * 2f - c.Size.Y, 0f));

            // Increment the advance
            // by the advance of the
            // current character
            advance += c.Advance;


            // Add a sprite component to the
            // current symbol
            ECSSHandler.AddComponent(Symbols.Values[i], new Sprite()
                {
                    TextureObjectIndex = c.TextureID,

                    Red = i_red,

                    Green = i_green,

                    Blue = i_blue,

                    Alpha = i_alpha
                });
        }
    }


    // The amount of characters
    // to load for a font
    public const int characterAmount = 128;

    // The Y resolution of
    // a character's texture
    public const uint pixelSizeY = 46;

    // A pointer reference to the
    // freetype library instance
    private static nint lib;

    // Holds the fonts
    // loaded by the user
    private static NA<NA<Character>> fonts;


	// The array to store
    // the characters
    // of the label's
    // sentence
	public NA<char> Text;

    // An array that stores
    // the entitys that
    // display each character
    // of the label
    public NA<int> Symbols;

	// The idnetifier of the
    // used font
	public int FontID;

    // The alignment of
    // the text relative
    // to it's parent
    Alignment TextAlignment;


	// The red color
    // modifier of the
    // label
	public byte Red;

	// The green color
    // modifier of the
    // label
	public byte Green;

	// The blue color
    // modifier of the
    // label
	public byte Blue;

	// The alpha
    // modifier of
    // the label
	public byte Alpha;


    // Loads a font from
    // the given path
    public static int LoadFont(string fontPath)
    {
        // A pointer that'll store
        // the instance of the
        // requested font
        nint face;

        // Initialise an instance of the
        // given font. But if that fails... 
		if(FT.FT_New_Face(lib, "./rsc/" + fontPath, 0, out face) != FT_Error.FT_Err_Ok)
            // Exit with the index
            // of the standard font
            return 0;


        // Cast the face field to
        // a FaceRec pointer so that
        // the individual character's
        // positional attributes can
        // be read
		FT_FaceRec* faceRec = (FT_FaceRec*)face;

        // Set the Y dimensional
        // pixel size for every
        // character
		FT.FT_Set_Pixel_Sizes(face, 0U, pixelSizeY);


        // Stores the index of
        // the new font.
        // Fallback is set to
        // the length of the
        // array
		int length = fonts.Length;

        // Fix the array . . .
		fixed(NA<NA<Character>>* fPtr = &fonts)
            // Create the new character array
            NAHandler.Set(length, new NA<Character>(characterAmount), fPtr);


        // Get a pointer reference to
        // the array to load the
        // characters to
        NA<Character>* font = &fonts.Values[length];


        // Iterate for each
        // character that we
        // want to fetch
        for(uint c = 0; c < characterAmount; c++)
        {   
            // Load the current
            // character. But if
            // that fails...
            if (FT.FT_Load_Char(face, c, 4) != FT_Error.FT_Err_Ok)
                // Skip to the
                // next ietartion
                continue;


            // Calculate the world-space
            // width of the current
            // character's texture
			float width = 1f / pixelSizeY * faceRec->glyph->bitmap.width;

            // Calculate the world-space
            // height of the current
            // character's texture
			float height = 1f / pixelSizeY * faceRec->glyph->bitmap.rows;

            // Calculate the world-space
            // x bearing of the current
            // character's texture
			float bearingX = 1f / pixelSizeY * faceRec->glyph->bitmap_left;

            // Calculate the world-space
            // y bearing of the current
            // character's texture
			float bearingY = 1f / pixelSizeY * faceRec->glyph->bitmap_top;

            // Calculate the world-space
            // advance of the current
            // character's texture
			float advance = 1f / pixelSizeY * faceRec->glyph->advance.x / 32f;


            // Allocate an array, that'll store
            // the converted texture data of the
            // current character
			int* data =
                (int*)NativeMemory.AllocZeroed(4 * faceRec->glyph->bitmap.width * faceRec->glyph->bitmap.rows);

            // Iterate through each red component from
            // the array and convert it to the rgba format
			for (int i = 0; i < (int)(faceRec->glyph->bitmap.width * faceRec->glyph->bitmap.rows); i++)
			{
                // Get the current red component
				byte currVal = ((byte*)faceRec->glyph->bitmap.buffer)[i];

                // Calculate which column
                // in the new array will
                // be populated with the
                // new color data
				int column = i % (int)faceRec->glyph->bitmap.width;

                // Calculate which row
                // in the new array
                // will be accessed
				int row = (int)(faceRec->glyph->bitmap.width * faceRec->glyph->bitmap.rows - faceRec->glyph->bitmap.width - (uint)(i - column));


                // Finally, populate the data
                // in the new array
				data[column + row] = currVal | currVal << 8 | currVal << 16 | currVal << 24;
			}


            // Now, create the structure
            // that'll hold the data we
            // extracted, aswell as the
            // texture of the character
			Character character = new Character
				{
                    // Create the texture of
                    // the character and store
                    // it's identifier
					TextureID = Sprite.LoadTextureInfo(new TextureInfo
                        {
                            TextureData = (byte*)data,
                            X = (short)faceRec->glyph->bitmap.width,
                            Y = (short)faceRec->glyph->bitmap.rows
                        }),

                    // Save the world space
                    // size of the character
					Size = new Vector2(width, height),

                    // Save the world space
                    // bearing of the character
					Bearing = new Vector2(bearingX, bearingY),

                    // Save the world space
                    // advance of the character
					Advance = advance
				};


            // Save the newly made character
            // to the array of the font
            NAHandler.Set((int)c, character, font);
        }
        
        
        // Tell freetype that
        // we are finished
        // with the font
		FT.FT_Done_Face(face);

        // Return the index
        // of the new font
        return length;
    }


    // The initializer
    // for the component
    // type
	[ComponentInitialise]
	public unsafe static void Initialize(int entityID)
	{
        // Get a pointer refernce to
        // the label component
		Label* l = ECSSHandler.GetComponent<Label>(entityID);

        // Iterate through
        // each symbol
		for (int i = 0; i < l->Symbols.Length; i++)
            // Bind the current
            // symbol to the parent
			ECSSHandler.BindChild(entityID, l->Symbols.Values[i]);


        // Text is aligned to the
        // right by default


        // Align text to the left
        if((l->TextAlignment & Alignment.Left) == Alignment.Left)
        {

            float startToLastDist =
                Gymbal.GetRelativeTranslation(l->Symbols.Values[l->Symbols.Length - 1]).X - Gymbal.GetRelativeTranslation(entityID).X;


            for(int i = 0; i < l->Symbols.Length; i++)
            {
                Translation* sTran = ECSSHandler.GetComponent<Translation>(l->Symbols.Values[i]);

                sTran->Translations.X -= startToLastDist;

                Console.WriteLine(sTran->Translations);
            }

            goto vertical;
        }


        // Align text to the center
        if((l->TextAlignment & Alignment.Center) == Alignment.Center)
        {
            float startToLastDist =
                (Gymbal.GetRelativeTranslation(l->Symbols.Values[l->Symbols.Length - 1]).X - Gymbal.GetRelativeTranslation(entityID).X) * 0.5f;


            for(int i = 0; i < l->Symbols.Length; i++)
            {
                Translation* sTran = ECSSHandler.GetComponent<Translation>(l->Symbols.Values[i]);

                sTran->Translations.X -= startToLastDist;

                Console.WriteLine(sTran->Translations);
            }

            goto vertical;
        }


    	// Implement later


        vertical:


        if((l->TextAlignment & Alignment.Middle) == Alignment.Middle)
        {


            return;
        }


        if((l->TextAlignment & Alignment.Down) == Alignment.Down)
        {



        }
	}

    // The finalizer
    // for the component
    // type
	[ComponentFinalise]
	public unsafe static void Finalize(int entityID)
	{
        // Get a pointer refernce to
        // the label component
		Label* l = ECSSHandler.GetComponent<Label>(entityID);

        // Free the unmanaged
        // character array
        NAHandler.Free(&l->Text);
	}
}


// Stores the information
// of a character
// translated to world space
public struct Character
{
    // The ID of the
    // character's
    // texture
	public int TextureID;

	// The world-space size
    // of the character
	public Vector2 Size;

	// The world-space bearing
    // of the character
	public Vector2 Bearing;

	// The world-space advance
    // of the character
	public float Advance;
}


// Represents the alignment
// of the text both horizontally
// and vertically
[Flags]
public enum Alignment : byte
{
    // Horizontal
    // alignments

    //
    Left = 1,

    //
    Center = 2,

    //
    Right = 4,


    // Vertical
    // alignments

    //
    Up = 8,

    //
    Middle = 16,

    //
    Down = 32
}