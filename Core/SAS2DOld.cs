

using OpenTK.Mathematics;
using Core.ECSS;
using Core.MemoryManagement;

using static Core.MemoryManagement.NAHandler;

using Core.Transformations;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.Shimshek;


// README: OUTDATED, because it aint
// working. Keep it still, as there
// are a lot of code that needs to
// migrate to the new version

namespace Core.SAS2DOld;

// The Aspect of an entity
// that is affected by the
// physical forces of the
// physics system
[Component]
public struct RigidBody
{

}

// Flags that define special
// behaviour for a rigidbody
[Flags]
public enum RigidBodyAttribs
{
    // No attributes have
    // been applied
    None = 0,

    // This attribute tells
    // that the rigidbody is
    // not affected by any force
    // for the time being
    IsNotSimulated = 1,

    // This attribute tells
    // that the rigidbody's
    // z rotation is not
    // manipulated by the
    // physics forces
    FreezeZRotation = 2,

    // This attribute tells
    // that the rigidbody's
    // x position is not
    // manipulated by the
    // physics forces
    FreezeXPosition = 4,

    // This attribute tells
    // that the rigidbody's
    // y position is not
    // manipulated by the
    // physics forces
    FreezeYPosition = 8,


}

// The solid body of
// an entity that may
// also funtion as a
// trigger or effector
[Component]
public unsafe struct Collider
{
    // The amount of additional steps
    // done in a physics frame
    public static byte subSteps = 1;


    // Called every physics frame
    [ComponentFixedUpdate]
    public static void FUpdate()
    {
        // The collider components
        Collider* colliders;

        // The IDs of the entities
        // bound to the components
        int* entityIDs;

        // The length of the
        // collider array
        int colliderLength;


        // Get the necessary informations
        // of the column
        ECSSHandler.GetCompactColumn(&colliders, &entityIDs, &colliderLength);


        // Iterate through each
        // substep
        for(byte b = 0; b < subSteps; b++)
        {
            // Calculates if the order
            // of the Y calculation has come
            //byte isYPass = (byte)(b & 1);


            // Get all possible pairs
            // of collision
            /*__sweepAndPrune(colliders, entityIDs, colliderLength);


            // Iterate through each
            // found pair
            for(int i = 0; i < pairs.Length; i += 2)
            {
                // If either of the
                // entities are null...
                if(pairs.Values[i] == 0 || pairs.Values[i + 1] == 0)
                    // Skip to the
                    // next iteration
                    continue;


                // DO COLLISION CHECK OF A TO B
                __checkCorrectCollision(entityIDs[pairs.Values[i]], entityIDs[pairs.Values[i + 1]],
                    &colliders[pairs.Values[i]], &colliders[pairs.Values[i + 1]]);


                // DO COLLISION CHECK OF B TO A
                //__checkCorrectCollision(entityIDs[pairs.Values[i + 1]], entityIDs[pairs.Values[i]],
                //    &colliders[pairs.Values[i + 1]], &colliders[pairs.Values[i]]);


                Console.WriteLine("------");
            }*/


            for(int a = 1; a < colliderLength - 1; a++)
                for(int c = a + 1; c < colliderLength; c++)
                    __checkCorrectCollision(entityIDs[a], entityIDs[c],
                        &colliders[a], &colliders[c]);
        }

        
        // Reset pairs array
        for(int i = 0; i < pairs.Length / 2; i++)
            ((long*)pairs.Values)[i] = 0;
    }

        // The pairs of colliders
        // to check for collision
        // with
        private static NA<int> pairs;

        // The sweep objects to
        // check for collision with
        private static long* sweeps;

        // The amount of sweep objects
        // within the sweeps array
        private static int sweepLength;

        // Seeks for collision pairs
        // with the sweep and prune
        // algorithm
        private static void __sweepAndPrune(Collider* cols, int* ID, int colLength)
        {
            // If the size of the
            // sweeps array isn't
            // sufficient enough
            // for all colliders...
            if(sweepLength < (colLength - 1) * 2)
            {
                // Set the new length of the
                // sweeps array
                sweepLength = (colLength - 1) * 2;

                // Resize the sweeps array
                sweeps =
                    (long*)NativeMemory.Realloc(sweeps, (nuint)(sizeof(long) * sweepLength));
            }


            // DO THE X DIMENSION CHECK FIRST

            // Create the sweeper bounds of
            // the colliders on the x dimension
            for(int i = 0; i < sweepLength; i += 2)
            {
                // Gets the index of the
                // current entity
                int currentID = i / 2 + 1;

                // Get the xPosition of
                // the current entity
                float xPos =
                    Gymbal.GetRelativeTranslation(ID[currentID]).X;

                // Get the largest scale from
                // the current entity's transform
                float scale =
                    getLargestScale(Gymbal.GetRelativeScale(ID[currentID]));


                // Set first ID
                ((int*)&sweeps[i])[0] = currentID;

                // Set first Distance
                ((float*)&sweeps[i])[1] = xPos - cols[currentID].Radius * scale;


                // Set second ID
                ((int*)&sweeps[i + 1])[0] = currentID;

                // Set second Distance
                ((float*)&sweeps[i + 1])[1] = xPos + cols[currentID].Radius * scale;
            }


            // Sort the sweeper bounds from
            // smallest to greatest
            sortSweep();


            // Finally, find the pairs


            // Ladies and gentlemen,
            // i present to you...
            // the stupid sweeper algorithm


            // TODO: Convert boolean
            // behaviour to arithmetic
            // solution


            // Stores the index of
            // where the pairs array
            // was written at last
            int writtenIndex = -1;


            // Stores the ID of
            // the current parent
            int currentParent = 0;

            // Iterate through each
            // sweeper object
            for(int i = 0; i < sweepLength; i++)
            {
                // Get the entity ID of the
                // current sweeper object
                int currentID = ((int*)&sweeps[i])[0];


                // If the current entity
                // is disabled or nonexistent...
                if(!ECSSHandler.GetEnableState(currentID))
                    // Skip to the
                    // next sweeper
                    continue;

                // If the current entity
                // has no translation component...
                if(!ECSSHandler.ContainsComponent<Translation>(currentID))
                    // Skip to the
                    // next sweeper
                    continue;


                // If the current parent
                // is the same as the
                // current sweeper...
                if(currentParent == currentID)
                {
                    // Undefine the
                    // current parent
                    currentParent = 0;

                    // Skip to the
                    // next sweeper
                    continue;
                }


                // If the current parent
                // is undefined...
                if(currentParent == 0)
                {
                    // Set the current
                    // parent to the
                    // current sweeper
                    currentParent = currentID;

                    // Skip to the
                    // next sweeper
                    continue;
                }


                // If the current entity
                // is supposed to ignore
                // the parent entity
                if(Contains(cols[currentID].ownLayer, &cols[currentParent].ignoreLayers))
                    continue;


                // If the parent entity
                // is supposed to ignore
                // the current entity
                if(Contains(cols[currentParent].ownLayer, &cols[currentID].ignoreLayers))
                    continue;


                // Add the new pair to the
                // pair array
                fixed(NA<int>* pPtr = &pairs)
                {
                    Set(++writtenIndex, currentParent, pPtr);

                    Set(++writtenIndex, currentID, pPtr);
                }
            }
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

            // Sort the sweep objects
            // with the radix sorting algorithm
            private static void sortSweep()
            {
                // Allocate space for
                // the auxiliary buffer
                long* auxiliary =
                    (long*)NativeMemory.Alloc((nuint)(sizeof(long) * sweepLength));


                // The counters for
                // each occuring instance
                // of a number
                int* counters = stackalloc int[256];

                // The offsets of each
                // group of value
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
                    long* currentArray = (long*)((nint)sweeps * otherOrder + (nint)auxiliary * order);

                    // Get the adress of the array
                    // write to
                    long* otherArray = (long*)((nint)sweeps * order + (nint)auxiliary * otherOrder);


                    // See how many instances
                    // of a certain value exist
                    for(int i = 0; i < sweepLength; i++)
                    {
                        byte b = (byte)(((int*)&currentArray[i])[1] >> (p * 8));

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
                    for(int i = 0; i < sweepLength; i++)
                    {
                        byte b = (byte)(((int*)&currentArray[i])[1] >> (p * 8));

                        otherArray[offsetTable[b]++] = currentArray[i];
                    }


                    // Clear the counter array
                    for(int i = 0; i < 128; i++)
                        ((long*)counters)[i] = 0;
                }


                // The final pass

                // Count every instance of a
                // certain value
                for(int i = 0; i < sweepLength; i++)
                {
                    byte b = (byte)(((int*)&auxiliary[i])[1] >> 24);

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
                for(int i = 0; i < sweepLength; i++)
                {
                    byte b = (byte)(((int*)&auxiliary[i])[1] >> 24);

                    sweeps[offsetTable[b]++] = auxiliary[i];
                }


                // Free the auxiliary buffer,
                // we don't need it no more
                NativeMemory.Free(auxiliary);
            }


        // Checks if a collision has occured
        // between two different colliders
        private static void __checkCorrectCollision(int entityA, int entityB,
            Collider* colA, Collider* colB)
        {
            // Get the model matrix
            // of entity a
            Matrix4 aModel =
                Gymbal.GetModelMatrix(entityA);

            // Get the model matrix
            // of entity b
            Matrix4 bModel =
                Gymbal.GetModelMatrix(entityB);


            // Allocate an array that'll
            // store the transformed
            // vertices of a triangle
            // known as a
            Vector2* vertsA = stackalloc Vector2[3];

            // Allocate an array that'll
            // store the transformed
            // vertices of a triangle
            // known as b
            Vector2* vertsB = stackalloc Vector2[3];


            // TODO: Replace triangle
            // broad phase
            // collision detection with
            // something linear like
            // the sweep and prune algorithm


            Vector2 closestNormal = (0, 0);


            float closestDepth = float.MaxValue;


            bool intersects = false;


            for(int a = 0; a < colA->Triangles.Length; a++)
            {
                // Get the pointer
                // to the raw vertices
                // of the triangle a
                Vector2* rawVertsA = colA->Vertices.Values;


                // Get the a vertex of the
                // current a triangle
                vertsA[0] = rawVertsA[colA->Triangles.Values[a].A];

                // Get the b vertex of the
                // current a triangle
                vertsA[1] = rawVertsA[colA->Triangles.Values[a].B];

                // Get the c vertex of the
                // current a triangle
                vertsA[2] = rawVertsA[colA->Triangles.Values[a].C];


                // Transforms the vertices
                // of the triangles with the
                // given transformation matrix
                SAS2DUtility.TransformTriangle(&aModel, vertsA);


                for(int b = 0; b < colB->Triangles.Length; b++)
                {
                    // Get the pointer
                    // to the raw vertices
                    // of the triangle b
                    Vector2* rawVertsB = colB->Vertices.Values;


                    // Get the a vertex of the
                    // current b triangle
                    vertsB[0] = rawVertsB[colB->Triangles.Values[b].A];

                    // Get the b vertex of the
                    // current b triangle
                    vertsB[1] = rawVertsB[colB->Triangles.Values[b].B];

                    // Get the c vertex of the
                    // current b triangle
                    vertsB[2] = rawVertsB[colB->Triangles.Values[b].C];


                    // Transforms the vertices
                    // of the triangles with the
                    // given transformation matrix
                    SAS2DUtility.TransformTriangle(&bModel, vertsB);



                    intersects |= SAS2DUtility.TriangleToTriangle(vertsA, vertsB, out Vector2 cNormal, out float cDepth);


                    if(cDepth < closestDepth)
                    {
                        closestNormal = cNormal;

                	    closestDepth = cDepth;
                    }

                    //intersects |=
                    //    SAS2DUtility.TriangleIntersectsTriangle(vertsA, vertsB, &closestDepth, &closestNormal);
                }
            }


            if(!intersects)
                return;


            Translation* aT = ECSSHandler.GetComponent<Translation>(entityA);

            Translation* bT = ECSSHandler.GetComponent<Translation>(entityB);


            aT->Translations.Xy += closestNormal * closestDepth * 0.5f;

            bT->Translations.Xy -= closestNormal * closestDepth * 0.5f;
        }

        // Does some additional collision
        // resolution between two colliders
        // that also have a rigidbody component
        private static void __collisionResolution(int entityA, int entityB,
            Collider* colA, Collider* colB)
        {

        }

        // Calls some collision related
        // events of a off of the collision
        // of two colliders
        private static void __collisionEvent(int entityA, int entityB,
            Collider* colA, Collider* colB)
        {

        }


    // Helper method for creating a
    // collider component for the
    // given entity
    public static void CreateCollider(int entityID, Vector2[] vertices, int[] ignoreLayers,
        int ownLayer = 0, int materialID = 0, ColliderAttribs attribs = ColliderAttribs.None)
    {
        // Initialise a new
        // collider component
        Collider c =
            new Collider();

        // Initialise the triangle
        // array of the collider
        c.Triangles = new NA<Triangle>();

        // Set the material id of
        // the collider
        c.materialIndex = materialID;


        // Set the attributes of
        // the collider
        c.collAttribs = attribs;


        // Initialise the vertex array
        c.Vertices = new NA<Vector2>();

        // If the given vertices
        // array is overloaded...
        if(vertices.Length != 0)
            // Copy the values of
            // the managed array
            // to the unmanaged
            // vertices array
            ManagedToNative(&c.Vertices, vertices);


        // Initialise the ignore
        // layer array 
        c.ignoreLayers = new NA<int>();

        // If the ignore layers
        // array is overloaded...
        if(ignoreLayers.Length != 0)
            // Copy the values of
            // the managed array
            // to the unmanaged
            // ignore layers array
            ManagedToNative(&c.ignoreLayers, ignoreLayers);


        // Triangulate the list of vertices
        // with the triangles list
        SAS2DUtility.TriangulatePolygon(&c.Triangles, &c.Vertices);


        // Null the 
        // radius of
        // the collider
        c.Radius = 0;

        // Find the vertex that is farthest
        // from the origin of the polygon
        for(int i = 0; i < c.Vertices.Length; i++)
        {
            // Calculate the distance
            // of the current vertex to the
            // origin of the polygon
            float dist =
                Vector2.Distance((0, 0), c.Vertices.Values[i]);

            // If the current
            // distance is smaller
            // than the saved radius...
            if(dist < c.Radius)
                // Skip to the
                // next iteration
                continue;

            // Save the current
            // distance as the
            // new radius
            c.Radius = dist;
        }


        // Add the collider component
        // to the given entity
        ECSSHandler.AddComponent(entityID, c);
    }


    // The individual triangles
    // that are based on the
    // vertices of the polygon
    public NA<Triangle> Triangles;

    // The vertices of the polygon
    public NA<Vector2> Vertices;


    // The radius of the circle
    // surrounding the polygon
    public float Radius;


    // Unique attributes
    // of the collider
    public ColliderAttribs collAttribs;


    // The index of the material
    // that the collider uses
    public int materialIndex;


    // The layer of the collider
    public int ownLayer;

    // The set of layers
    // that the collider
    // ignores
    public NA<int> ignoreLayers;


    // The set of colliders
    // the collider recently
    // collided with
    public NA<int> collidingWith;
}

// Flags that define
// special behaviour
// for a collider
[Flags]
public enum ColliderAttribs : byte
{
    // No attribute has
    // been applied
    None = 0,

    // This attribute signals
    // that the collider
    // ignores all collisions
    IgnoreCollision = 1,

    // This attribute signals
    // that the collider is a
    // field of force that 
    // continously applies a
    // force to the rigidbodies
    // that come within it's area.
    // No collision resolution
    IsEffector = 2,

    // This attribute signals
    // that the collider is a
    // field that triggers the
    // collision events like
    // onCollisionEnter if
    // another collider gets
    // into it's area.
    // No collision resolution
    IsTrigger = 4,

    
}

// A triangle represents
// a partition of a polygon
public struct Triangle
{
    // The vertices of the triangle
    public int A, B, C;
}

// Contains utility methods
// for the physis engine
public static unsafe class SAS2DUtility
{
    static int redBox = 0;

    static int[] boxes = new int[3];

    static SAS2DUtility()
    {
        redBox = ECSSHandler.CreateEntity();

        Gymbal.CreateTransform(redBox, (0.25f, 0.25f, 1f), (0, 0, 0), (0, 0, -10));

        ECSSHandler.AddComponent(redBox, new Sprite()
        {
            TextureObjectIndex = 0,

            Red = 255,

            Green = 0,

            Blue = 0,

            Alpha = 255,
        });


        for(int i = 0; i < 3; i++)
        {
            boxes[i] = ECSSHandler.CreateEntity();

            Gymbal.CreateTransform(boxes[i], (0.25f, 0.25f, 1f), (0, 0, 0), (0, 0, -10));

            ECSSHandler.AddComponent(boxes[i], new Sprite()
            {
                TextureObjectIndex = 0,

                Red = 0,

                Green = 255,

                Blue = 0,

                Alpha = 255,
            });
        }
    }


    // Transforms the given
    // vectors of a triangle
    // with the given
    // transformation matrix
    public static void TransformTriangle(Matrix4* model, Vector2* verts)
    {
        // Iterate through
        // each vertex of the
        // triangle
        for(int i = 0; i < 3; i++)
        {
            // Transforms the
            // current vertex
            // with the given
            // transformation
            // matrix
            Vector4 result =
                new Vector4(verts[i].X, verts[i].Y, 0f, 1f) * (*model);

            // Saves the transformed
            // result to the fetched
            // index
            verts[i] = result.Xy;
        }
    }


    // Checks if the two sets
    // of triangles intersect
    // eachother
    public static bool TriangleIntersectsTriangle(Vector2* a, Vector2* b,
        float* depth, Vector2* normal)
    {


        // Keeping the code below as scraps for
        // the future retry of a truly optimized
        // triangle to triangle collision
        // detection. They should work, but
        // somewhere must be a missconduction

        // The container of the
        // intersection
        /*Vector2* intersection = stackalloc Vector2[1];


        // Allocate space for
        // the a iteration line
        Vector2* aLine = stackalloc Vector2[2];

        // Allocate space for
        // the b iteration line
        Vector2* bLine = stackalloc Vector2[2];


    	// Iterate through triangle
        // a's lines
        for(int ia = 0; ia < 3; ia++)
        {
            aLine[0] = a[ia];

            aLine[1] = a[(ia + 1) % 3];

            // Iterate through triangle
            // b's lines
            for(int ib = 0; ib < 3; ib++)
            {
                bLine[0] = b[ib];

                bLine[1] = b[(ib + 1) % 3];


                if(LineIntersectsLine(aLine, bLine, intersection))
                    goto colliding;
            }
        }

        return false;


        // A sort of collision
        // has been detetcted
        colliding:


        Translation* t = ECSSHandler.GetComponent<Translation>(redBox);

        t->Translations.Xy = *intersection;


        *normal = a[0] - a[1];

        normal->Normalize();


        *depth = 1;


        return true;*/

        /*float aZeroDist = Vector2.Distance(a[0], *intersection);

        float aOneDist = Vector2.Distance(a[1], *intersection);


        if(aZeroDist < aOneDist)
        {
            *normal = a[1] - a[0];

            normal->Normalize();


            *depth = Vector2.Distance(a[0], *intersection);


            return true;
        }


        *normal = a[0] - a[1];

        normal->Normalize();


        *depth = Vector2.Distance(a[1], *intersection);


        return true;*/


        // Holds the foreign triangle
        // to check the points in
        Vector2* targetTri = null;

        // Holds the triangle that
        // has the points that will
        // be checked for being
        // within a foreign triangle
        Vector2* fromTri = null;


        // The iterator
        // of both loops
        byte i = 0;


        // Set the foreign
        // triangle to b
        targetTri = b;

        // Set the point
        // triangle to a
        fromTri = a;

        // Check if any
        // of a's points
        // are within b
        for(; i < 3; i++)
            // If any of a's points
            // are within b, jump to
            // "colliding"
            if(pointInTriangle(a[i], b)) goto colliding;


        // Set the foreign
        // triangle to a
        targetTri = a;

        // Set the point
        // triangle to b
        fromTri = b;

        // Check if any
        // of b's points
        // are within a
        for(; i < 6; i++)
            // If any of b's points
            // are within a, jump to
            // "colliding"
            if(pointInTriangle(b[i - 3], a)) goto colliding;

        // No collision
        // has been detected
        return false;


        // A jumping point
        // for cases where
        // a collision has
        // been detected
        colliding:


        // Indicates if
        // it is either
        // a's or b's turn
        // of being the
        // point triangle
        byte bTurn = (byte)(i / 3);


        //Translation* t = ECSSHandler.GetComponent<Translation>(redBox);

        //t->Translations.Xy = fromTri[i - (3 * bTurn)];


        // Calculate the center
        // of the point triangle
        Vector2 fromCenter =
            GetTriangleCenter(fromTri);


        // Stores the index
        // of the frathest vertex
        // from the target triangle
        byte farthestIndex = 0;


        // Iterate through
        // every other vertex
        // from the target
        // triangle
        for(byte j = 1; j < 3; j++)
        {
            // If the farthest distance is
            // farther than the distance of
            // the current vertex...
            if(Vector2.DistanceSquared(targetTri[farthestIndex], fromCenter) >
                Vector2.DistanceSquared(targetTri[j], fromCenter))
                    // Skip to the
                    // next iteration
                    continue;

            // Save the new 
            // farthest index
            farthestIndex = j;
        }


        //Console.WriteLine(((farthestIndex + 2) % 3) + " a  " + ((farthestIndex + 1) % 3) + " b  " + (i - (3 * bTurn)) + " c  ");


        Vector2 ab = targetTri[(farthestIndex + 2) % 3] - targetTri[(farthestIndex + 1) % 3];

        Vector2 ap = fromTri[i - (3 * bTurn)] - targetTri[(farthestIndex + 1) % 3];


        float proj = Vector2.Dot(ap, ab);

        float abLSq = ab.LengthSquared;

        float d = proj / abLSq;


        Vector2 cp = targetTri[(farthestIndex + 1) % 3] + ab * d;


        float dep =
            Vector2.Distance(fromTri[i - (3 * bTurn)], cp);  



        Translation* t = ECSSHandler.GetComponent<Translation>(redBox);

        t->Translations.Xy = fromTri[i - (3 * bTurn)];


        for(int k = 0; k < 3; k++)
        {
            Translation* tr = ECSSHandler.GetComponent<Translation>(boxes[k]); 

            tr->Translations.Xy = targetTri[k];



        }


        if(dep > *depth)
            return false;


        if(dep == 0)
            return false;


        *normal = (cp - fromTri[i - (3 * bTurn)]).Normalized();

        *depth = 1;


        Console.WriteLine(*normal);


        //Translation* t = ECSSHandler.GetComponent<Translation>(redBox);

        //t->Translations.Xy = fromTri[i - (3 * bTurn)];


        // A collision has
        // been detected!
        return true;
    }


public unsafe static bool TriangleToTriangle(Vector2* triA, Vector2* triB, out Vector2 normal, out float depth)
{
	normal = Vector2.Zero;
	depth = float.MaxValue;
	Vector2 edge = *triA - triA[1];
	Vector2 axis = new Vector2(-edge.Y, edge.X);
	axis.Normalize();
	float minA;
	float maxA;
	ProjectVertices(*triA, triA[1], triA[2], axis, out minA, out maxA);
	float minB;
	float maxB;
	ProjectVertices(*triB, triB[1], triB[2], axis, out minB, out maxB);
	if (minA >= maxB || minB >= maxA)
	{
		return false;
	}
	float axisDepth = MathF.Min(maxB - minA, maxA - minB);
	if (axisDepth < depth)
	{
		depth = axisDepth;
		normal = axis;
	}
	edge = triA[1] - triA[2];
	axis = new Vector2(-edge.Y, edge.X);
	axis.Normalize();
	ProjectVertices(*triA, triA[1], triA[2], axis, out minA, out maxA);
	ProjectVertices(*triB, triB[1], triB[2], axis, out minB, out maxB);
	if (minA >= maxB || minB >= maxA)
	{
		return false;
	}
	axisDepth = MathF.Min(maxB - minA, maxA - minB);
	if (axisDepth < depth)
	{
		depth = axisDepth;
		normal = axis;
	}
	edge = triA[2] - *triA;
	axis = new Vector2(-edge.Y, edge.X);
	axis.Normalize();
	ProjectVertices(*triA, triA[1], triA[2], axis, out minA, out maxA);
	ProjectVertices(*triB, triB[1], triB[2], axis, out minB, out maxB);
	if (minA >= maxB || minB >= maxA)
	{
		return false;
	}
	axisDepth = MathF.Min(maxB - minA, maxA - minB);
	if (axisDepth < depth)
	{
		depth = axisDepth;
		normal = axis;
	}
	edge = *triB - triB[1];
	axis = new Vector2(-edge.Y, edge.X);
	axis.Normalize();
	ProjectVertices(*triA, triA[1], triA[2], axis, out minA, out maxA);
	ProjectVertices(*triB, triB[1], triB[2], axis, out minB, out maxB);
	if (minA >= maxB || minB >= maxA)
	{
		return false;
	}
	axisDepth = MathF.Min(maxB - minA, maxA - minB);
	if (axisDepth < depth)
	{
		depth = axisDepth;
		normal = axis;
	}
	edge = triB[1] - triB[2];
	axis = new Vector2(-edge.Y, edge.X);
	axis.Normalize();
	ProjectVertices(*triA, triA[1], triA[2], axis, out minA, out maxA);
	ProjectVertices(*triB, triB[1], triB[2], axis, out minB, out maxB);
	if (minA >= maxB || minB >= maxA)
	{
		return false;
	}
	axisDepth = MathF.Min(maxB - minA, maxA - minB);
	if (axisDepth < depth)
	{
		depth = axisDepth;
		normal = axis;
	}
	edge = triB[2] - *triB;
	axis = new Vector2(-edge.Y, edge.X);
	axis.Normalize();
	ProjectVertices(*triA, triA[1], triA[2], axis, out minA, out maxA);
	ProjectVertices(*triB, triB[1], triB[2], axis, out minB, out maxB);
	if (minA >= maxB || minB >= maxA)
	{
		return false;
	}
	axisDepth = MathF.Min(maxB - minA, maxA - minB);
	if (axisDepth < depth)
	{
		depth = axisDepth;
		normal = axis;
	}
	if (Vector2.Dot(triB[3] - triA[3], normal) < 0f)
	{
		normal = -normal;
	}
	return true;
}


private static void ProjectVertices(Vector2 vertexA, Vector2 vertexB, Vector2 vertexC, Vector2 axis, out float min, out float max)
{
	min = float.MaxValue;
	max = float.MinValue;
	float proj = Vector2.Dot(vertexA, axis);
	if (proj < min)
	{
		min = proj;
	}
	if (proj > max)
	{
		max = proj;
	}
	proj = Vector2.Dot(vertexB, axis);
	if (proj < min)
	{
		min = proj;
	}
	if (proj > max)
	{
		max = proj;
	}
	proj = Vector2.Dot(vertexC, axis);
	if (proj < min)
	{
		min = proj;
	}
	if (proj > max)
	{
		max = proj;
	}
}


    // Get the center of a triangle
    // off of it's vertices
    public static Vector2 GetTriangleCenter(Vector2* verts)
        => ((verts[0].X + verts[1].X + verts[2].X) / 0.33f, (verts[0].Y + verts[1].Y + verts[2].Y) / 0.33f);


    // Triangulates a polygon
    // into inidividual triangles
    public static void TriangulatePolygon(NA<Triangle>* triangles, NA<Vector2>* vertices)
    {
        // If no vertices have
        // been given...
        if(vertices->Length == 0)
            return;

        // If only one vertex
        // was given...
        if(vertices->Length == 1)
        {
            Set(0, new Triangle(){A = 0, B = 0, C = 0}, triangles);

            return;
        }

        // If only two vertices
        // were given...
        if(vertices->Length == 2)
        {
            Set(0, new Triangle(){A = 0, B = 1, C = 0}, triangles);

            return;
        }

        // Compute the total
        // amount of triangles
        // the polygon contains
        int triangleAmount = vertices->Length - 2;

        // The array to hold the
        // indices to ignore
        int* ignoreVertices =
            (int*)NativeMemory.Alloc((nuint)(sizeof(int) * vertices->Length));

        // The last index in the
        // ignore vertices array
        // that was populated
        int loadIndex = -1;


        // Iterate through each possible triangle
        // within the polygon
        for(int t = 0; t < triangleAmount; t++)
        {
            // The first vertex
            // of the triangle
            int a = 0;

            // Go back to restart with
            // a different vertex
            restart:

            // Iterate through each vertex
            for(int v = a; v < vertices->Length; v++)
            {


                // Iterate through each 
                // ignore vertex
                for(int i = 0; i < loadIndex + 1; i++)
                {
                    
                    // if the current vertex index
                    // is not the same as the current
                    // ignore vertex...
                    if(ignoreVertices[i] != v)
                        // Skip to the next
                        // iteration
                        continue;

                    goto skipNext;
                }


                // At this stage, a valid
                // vertex has been found

                a = v; // save the index

                break; // End the loop


                // Skip to the
                // next vertex
                skipNext:

                    ; // don't mind this semicollon
            }   


            // The second vertex
            // of the triangle
            int b = 0;

            // Iterate through each vertex
            for(int v = (a + 1) % vertices->Length; v < vertices->Length; v++)
            {


                // Iterate through each 
                // ignore vertex
                for(int i = 0; i < loadIndex + 1; i++)
                {
                    
                    // if the current vertex index
                    // is not the same as the current
                    // ignore vertex...
                    if(ignoreVertices[i] != v)
                        // Skip to the next
                        // iteration
                        continue;

                    goto skipNext;
                }


                // At this stage, a valid
                // vertex has been found

                b = v; // save the index

                break; // End the loop


                // Skip to the
                // next vertex
                skipNext:

                    ; // don't mind this semicollon
            }   


            // The third vertex
            // of the triangle
            int c = 0;

            // Iterate through each vertex
            for(int v = (b + 1) % vertices->Length; v < vertices->Length; v++)
            {


                // Iterate through each 
                // ignore vertex
                for(int i = 0; i < loadIndex + 1; i++)
                {
                    
                    // if the current vertex index
                    // is not the same as the current
                    // ignore vertex...
                    if(ignoreVertices[i] != v)
                        // Skip to the next
                        // iteration
                        continue;

                    goto skipNext;
                }


                // At this stage, a valid
                // vertex has been found

                c = v; // save the index

                break; // End the loop


                // Skip to the
                // next vertex
                skipNext:

                    ; // don't mind this semicollon
            }  


            // If a point is within the triangle...
            /*for(int v = 0; v < vertices->Length; v++)
            {
                if(v == a || v == b || v == c)  
                    continue;

                if(!pointInTriangle(vertices->Values[v], vertices->Values[a], vertices->Values[b], vertices->Values[c]))
                    continue;

                a = (a + 1) % vertices->Length;

                //ignoreVertices[++loadIndex] = b;

                goto restart;
            }*/


            // Calculate the vector
            // between vertex b and a
            Vector2 bToA = vertices->Values[b] - vertices->Values[a];

            // Calculate the vector
            // between vertex c and a
            Vector2 cToA = vertices->Values[c] - vertices->Values[a];


            // If the crossproduct
            // of the two vectors
            // is negative...
            if(cross(cToA, bToA) < 0f)
            {
                a = (a + 1) % vertices->Length;

                goto restart;
            }
            

            // Add the b vertex to the
            // vertices to ignore
            ignoreVertices[++loadIndex] = b;


            // Create the new triangle
            Triangle nTri = new Triangle()
                {
                    A = a,

                    B = b,   

                    C = c,   
                };


            // Add the new triangle to the array
            Set(triangles->Length, nTri, triangles);
        }


        // Free the array that held the
        // indices of ingored vertices
        NativeMemory.Free(ignoreVertices);
    }





    // Calculates the angle
    // between two vectors
    // 
    // Keeping it for future plans
    private static float angle(Vector2 a, Vector2 b)
    {
        float val = cross(a, b);

        val = (float)MathHelper.Asin(val);

        return val * 180 / MathHelper.Pi;
    }

    // Calculate crossproduct
    // of two vectors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float cross(Vector2 value1, Vector2 value2)
        => value1.X * value2.Y - value1.Y * value2.X;

    // Checks if the given point
    // is within a triangle
    private static bool pointInTriangle(Vector2 pt, Vector2* verts)
    {
        Vector2 ab = verts[1] - verts[0];
        Vector2 bc = verts[2] - verts[1];
        Vector2 ca = verts[0] - verts[2];


        Vector2 ap = pt - verts[0];
        Vector2 bp = pt - verts[1];
        Vector2 cp = pt - verts[2];


        float cross1 = cross(ab, ap);
        float cross2 = cross(bc, bp);
        float cross3 = cross(ca, cp);


        if(cross1 > 0f || cross2 > 0f || cross3 > 0f)

            return false;


        return true;
    }


    // Checks if the
    // two given lines
    // intersect eachother
    public static bool LineIntersectsLine(Vector2* a, Vector2* b, Vector2* intersection)
    {
        float aDen = (b[1].X - b[0].X) * (b[0].Y - a[0].Y) - (b[1].Y - b[0].Y) * (b[0].X - a[0].X);

        float bDen = (b[1].X - b[0].X) * (a[1].Y - a[0].Y) - (b[1].Y - b[0].Y) * (a[1].X - a[0].X);

        float cDen = (a[1].X - a[0].X) * (b[0].Y - a[0].Y) - (a[1].Y - a[0].Y) * (b[0].X - a[0].X);


        // Both Lines are
        // overlaying eachother
        // (collinear)
        if(aDen == 0 && bDen == 0)
            return false;



        // Both shapes
        // are parallel
        if(bDen == 0)
            return false;


        float alpha = aDen / bDen;

        float beta = cDen / bDen;


        // Alpha or beta
        // are beyond zero
        if(alpha < 0f || beta < 0f)
            return false;

        // Alpha or beta
        // are beyond one
        if(alpha > 1f || beta > 1f)
            return false;


        // Calculate the x dimension
        // of the intersection
        intersection->X = a[0].X + alpha * (a[1].X - a[0].X);

        // Calculate the y dimension
        // of the intersection
        intersection->Y = a[0].Y + alpha * (a[1].Y - a[0].Y);


        // The shapes are
        // intersecting
        return true;
    }
}