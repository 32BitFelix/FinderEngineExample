

using OpenTK.Mathematics;
using Core.ECSS;
using Core.MemoryManagement;
using System.Runtime.CompilerServices;
using Core.Transformations;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Core.FinderIO;

namespace Core.UISystem;


[Component]
public unsafe struct Cursor
{
    static Cursor()
        => ClickBind = (int)MouseButton.Left;

    public static int ClickBind;


    [ComponentInitialise]
    public static void Initialize(int self)
    {

    }


    [ComponentFinalise]
    public static void Finalize(int self)
    {

    }
}


[Component]
public unsafe struct Button
{
    // The vertices of the
    // button that represent
    // it's interactible area
    public Vector2* verts;


    // On Click is called,
    // when the button
    // is clicked on
    public delegate*<int, int, void> onClick;

    // On Enter is called,
    // when a cursor
    // enters the button
    public delegate*<int, int, void> onEnter;

    // On Exit is called,
    // when a cursor
    // exits the button
    public delegate*<int, int, void> onExit;


    // A clue that memorizes,
    // if a cursor is within
    // the button
    public bool isEntered;


    // Constructor for
    // the button
    public Button(Vector2[] Vertices, delegate*<int, int, void> OnClick = null,
        delegate*<int, int, void> OnEnter = null, delegate*<int, int, void> OnExit = null)
    {
        // Allocate an unmanaged
        // array to store the vertices
        verts = SmartPointer.CreateSmartPointer<Vector2>(Vertices.Length);

        
        // The sum of all edges
        // of the given polygon
        float allSum = 0;

        // Iterate through each possible edge
        // of the polygon and add their sums
        // to the 
        for(int i = 0; i < Vertices.Length; i++)
            allSum += Sum(Vertices[i], Vertices[(i + 1) % Vertices.Length]);

        // Check if the vertices
        // of the polygon are
        // laid out clockwise
        bool isClockwise = allSum >= 0;


        // Set the method to call
        // to the vertex reader
        // for clockwise order,
        // if the polygon's vertices
        // are laid out clockwise
        nint mCall = (nint)(delegate*<Vector2*, Vector2[], void>)&GetVerticesOnClockwise * *(byte*)&isClockwise;

        // Set the method to call
        // to the vertex reader
        // for counter clockwise order,
        // if the polygon's vertices
        // are laid out counter clockwise
        mCall += (nint)(delegate*<Vector2*, Vector2[], void>)&GetVerticesOnCounterClockwise * (*(byte*)&isClockwise ^ 0x01);

        
        // Call the made out method
        ((delegate*<Vector2*, Vector2[], void>)mCall)(verts, Vertices);


        // Save the method
        // to call, when the
        // button is clicked
        onClick = OnClick;

        // Save the method
        // to call, when the
        // cursor enters
        // the area of the
        // button
        onEnter = OnEnter;

        // Save the method
        // to call, when the
        // cursor exits
        // the area of the
        // button
        onExit = OnExit;


        // Make it clear,
        // that the button
        // hasn't been entered
        // by a cursor
        isEntered = false;
    }

        // Read the vertices of the
        // managed array, if the polygon
        // is clockwise
        private static void GetVerticesOnClockwise(Vector2* vert, Vector2[] mVert)
        {
            // Iterate through each
            // vertex of the managed
            // array in a reverse fashion...
            for(int i = mVert.Length - 1; i > -1; i--)
                // Copy the current element
                // of the managed array
                // to the element at the
                // same index of the
                // unmanaged array
                vert[i] = mVert[i];
        }

        // Read the vertices of the
        // managed array, if the polygon
        // is counter clockwise
        private static void GetVerticesOnCounterClockwise(Vector2* vert, Vector2[] mVert)
        {
            // Iterate through each
            // vertex of the managed
            // array in a reverse fashion...
            for(int i = mVert.Length - 1; i > -1; i--)
                // Copy the element at
                // the "opposite" index
                // of the managed array
                // to the current index
                // of the unmanaged array
                vert[i] = mVert[mVert.Length - 1 - i];
        }

        // Calculates the sum of the edge ab
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Sum(Vector2 a, Vector2 b)
            => (b.X - a.X) * (b.Y + a.Y);


    [ComponentInitialise]
    public static void Initialize(int self)
    {

    }


    [ComponentFinalise]
    public static void Finalize(int self)
    {
        Button* b = ECSSHandler.GetComponent<Button>(self);

        SmartPointer.Free(b->verts);
    }


    [ComponentUpdate]
    public static void Update()
    {
        Button* buttons;

        int* buttonEntities;

        int buttonLength;


        ECSSHandler.GetCompactColumn(&buttons, &buttonEntities, &buttonLength);


        Cursor* cursors;

        int* cursorEntities;

        int cursorLength;


        ECSSHandler.GetCompactColumn(&cursors, &cursorEntities, &cursorLength);


        // Iterate through each
        // button in the button collection
        for(int b = 1; b < buttonLength; b++)
        {
            if(buttonEntities[b] == 0)
                continue;

            
            if(!ECSSHandler.GetEnableState(buttonEntities[b]))
                continue;


            Button* button = &buttons[b];


            Vector2* nVertices =
                SmartPointer.CreateSmartPointer<Vector2>(SmartPointer.GetSmartPointerLength(button->verts));


            Matrix4 buttonModelMatrix = Gymbal.GetModelMatrix(buttonEntities[b]);

            
            TransformVertices(&buttonModelMatrix, nVertices, button->verts);


            // Iterate through each cursor
            // in the cursor collection
            for(int c = 1; c < cursorLength; c++) // Get it? c++?
            {
                if(cursorEntities[c] == 0)
                    continue;

                
                if(!ECSSHandler.GetEnableState(cursorEntities[c]))
                    continue;

                
                Vector2 cursorPos = Gymbal.GetRelativeTranslation(cursorEntities[c]).Xy;


                bool iCIB = isCursorInButton(cursorPos, nVertices);

                bool isEntering = iCIB && !button->isEntered;

                    button->isEntered |= isEntering;

                bool isExiting = !iCIB && button->isEntered;

                    button->isEntered ^= isExiting;


                nint mCall = (nint)button->onEnter * *(byte*)&isEntering;

                mCall += (nint)button->onExit * *(byte*)&isExiting;


                if(mCall != 0)
                    ((delegate*<int, int, void>)mCall)(buttonEntities[b], cursorEntities[c]);


                // Is the button bound
                // for UI input pressed
                // for this frame?
                bool iBP = KBMInput.IsPressed(Cursor.ClickBind) && button->isEntered;

                nint cCall = (nint)button->onClick * *(byte*)&iBP;

                if(cCall == 0)
                    continue;

                ((delegate*<int, int, void>)cCall)(buttonEntities[b], cursorEntities[c]);
            }


            SmartPointer.Free(nVertices);
        }
    }

        // Check if a cursor is within
        // the given button
        private static bool isCursorInButton(Vector2 cursorPos, Vector2* buttonVertices)
        {
            int buttonVertexLength = SmartPointer.GetSmartPointerLength(buttonVertices);


            for(int i = 0; i < buttonVertexLength; i++)
            {
                Vector2 a = buttonVertices[i];

                Vector2 b = buttonVertices[(i + 1) % buttonVertexLength];


                Vector2 bToA = b - a;

                Vector2 pToA = cursorPos - a;


                if(cross(bToA, pToA) > 0)
                    return false;
            }


            return true;
        }


        // Transforms the
        // vertices of the
        // given array of
        // vertices and stores
        // the result in a
        // secondary array
        private static void TransformVertices(Matrix4* transformation, Vector2* nVertices, Vector2* oVertices)
        {
            for(int i = 0; i < SmartPointer.GetSmartPointerLength(nVertices); i++)

                nVertices[i] = (new Vector4(oVertices[i].X, oVertices[i].Y, 0, 1) * *transformation).Xy;
        }


        // Calculate crossproduct
        // of two vectors
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float cross(Vector2 value1, Vector2 value2)
            => value1.X * value2.Y - value1.Y * value2.X;
}


public unsafe struct Slider
{



}