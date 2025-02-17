

using OpenTK.Mathematics;
using Core.ECSS;
using System.Runtime.CompilerServices;

namespace Core.Transformations;

// A class that contains
// helper functions in
// all things transformations
public static unsafe class Gymbal
{
    // Helper function for
    // creating a transform
    public static void CreateTransform(int entityID, Vector3 scale,
        Vector3 rotation, Vector3 translation)
    {
        // Set and create the scale portion
        // of the transformation
        ECSSHandler.AddComponent(entityID, new Scale(scale));

        // Set and create the rotation portion
        // of the transformation
        ECSSHandler.AddComponent(entityID, new Rotation(rotation));

        // Set and create the translation portion
        // of the transformation
        ECSSHandler.AddComponent(entityID, new Translation(translation));
    }

    // Returns the up vector
    // of the given transform
    public static Vector3 GetUp(int entityID)
        => Vector3.Normalize(Vector3.UnitY * CreateRotationXYZ_M3(GetRelativeRotation(entityID)));

    // Returns the right vector
    // of the given transform
    public static Vector3 GetRight(int entityID)
        => Vector3.Normalize(Vector3.UnitX * CreateRotationXYZ_M3(GetRelativeRotation(entityID)));

    // Returns the front vector
    // of the given transform
    public static Vector3 GetFront(int entityID)
        => Vector3.Normalize(-Vector3.UnitZ * CreateRotationXYZ_M3(GetRelativeRotation(entityID)));

    // Returns the scale of
    // a transform relative
    // to it's parent
    public static Vector3 GetRelativeScale(int entityID)
    {
        // Get the parent id
        // of the given entity
        int p = ECSSHandler.ShowParent(entityID);

        // If the parent
        // is nonexistent...
        if(p == 0)
            // Return the
            // normal scale
            return ECSSHandler.GetComponent<Scale>(entityID)->Scales;
        // If the parent
        // does exist...
        else
            // Return the
            // scale relative
            // to the parent
            return ECSSHandler.GetComponent<Scale>(entityID)->Scales * GetRelativeScale(p);
    }

    // Sets the scale of
    // a transform relative
    // to it's parent,
    // if it has one
    public static void SetRelativeScale(int entityID, Vector3 value)
    {
        // Get the id of the
        // given entity's parent
        int p = ECSSHandler.ShowParent(entityID);

        // Set a multiplier depending
        // on if the entity has a parent
        Vector3 pMulti = p != 0 ? GetRelativeScale(p) : Vector3.Zero;

        // Set the new scale
        // with the value and
        // the given multiplier
        ECSSHandler.GetComponent<Scale>(entityID)->Scales = value - pMulti;
    }

    // Returns the rotation
    // relative to the transform's
    // parent, if it has one
    public static Vector3 GetRelativeRotation(int entityID)
    {
        // Get the parent of
        // the given entity
        int p = ECSSHandler.ShowParent(entityID);

        // If the parent
        // is nonexistent...
        if(p == 0)
            // Return the
            // normal rotation
            return ECSSHandler.GetComponent<Rotation>(entityID)->Rotations;
        // If the parent
        // does exist...
        else
            // Return the
            // rotation relative
            // to the parent
            return ECSSHandler.GetComponent<Rotation>(entityID)->Rotations + GetRelativeRotation(p);
    }

    // Sets the rotation of
    // the transform relative
    // to it's parent
    public static void SetRelativeRotation(int entityID, Vector3 value)
    {
        // Get the id of the
        // given entity's parent
        int p = ECSSHandler.ShowParent(entityID);

        // Set a multiplier depending
        // on if the entity has a parent
        Vector3 pMulti = p != 0 ? GetRelativeRotation(p) : Vector3.Zero;

        // Set the new rotation
        // with the value and
        // the given multiplier
        ECSSHandler.GetComponent<Rotation>(entityID)->Rotations = value - pMulti;
    }

    // Returns the translation
    // of a transform relative
    // to it's parent, if it has one
    public static Vector3 GetRelativeTranslation(int entityID)
    {
        // Get the parent of
        // the given entity
        int p = ECSSHandler.ShowParent(entityID);

        // If the parent
        // is nonexistent...
        if(p == 0)
            // Return the
            // normal translation
            return ECSSHandler.GetComponent<Translation>(entityID)->Translations;
        // If the parent
        // does exist...
        else
            // Return the
            // Translation relative
            // to the parent
            return ECSSHandler.GetComponent<Translation>(entityID)->Translations * GetRelativeScale(p) *
                CreateRotationXYZ_M3(GetRelativeRotation(p)) + GetRelativeTranslation(p);
    }

    // Sets the translation
    // of the transformation
    // relative to it's parent,
    // if it has one
    public static void SetRelativeTranslation(int entityID, Vector3 value)
    {
        // Get the id of the
        // given entity's parent
        int p = ECSSHandler.ShowParent(entityID);

        // Set a multiplier depending
        // on if the entity has a parent
        Vector3 pMulti = p != 0 ? GetRelativeTranslation(p) : Vector3.Zero;

        // Set the new translation
        // with the value and
        // the given multiplier
        ECSSHandler.GetComponent<Translation>(entityID)->Translations = value - pMulti;
    }

    // Returns the model matrix
    // of the given transformation
    public static Matrix4 GetModelMatrix(int entityID)
    {
        // Create the matrix as 
        // a scale first
        Matrix4 nMatrix = Matrix4.CreateScale(GetRelativeScale(entityID));

        // Rotate the matrix
        nMatrix *= CreateRotationXYZ_M4(GetRelativeRotation(entityID));

        // Finally, translate
        // the matrix and return it
        return nMatrix * Matrix4.CreateTranslation(GetRelativeTranslation(entityID));
    }

    // Returns the view matrix
    // of the given transformation
    public static Matrix4 GetViewMatrix(int entityID)
    {
        // Get the positon of the camera
        Vector3 position = GetRelativeTranslation(entityID);

        // Get the up vector of the camera
        Vector3 up = GetUp(entityID);

        // Get the front vector of the camera
        Vector3 front = GetFront(entityID);

        // Finally, use the gotten values for the lookAt function
        return Matrix4.LookAt(position, position + front, up);
    }

    // Turns a Vector3's
    // radians into degrees
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 RadToDegVector(Vector3 value)
    {
        return (MathHelper.RadiansToDegrees(value.X),
                MathHelper.RadiansToDegrees(value.Y),
                MathHelper.RadiansToDegrees(value.Z));
    }

    // Turns a Vector3's
    // degrees into radians        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 DegToRadVector(Vector3 value)
    {
        return (MathHelper.DegreesToRadians(value.X),
                MathHelper.DegreesToRadians(value.Y),
                MathHelper.DegreesToRadians(value.Z));
    }      

    // Creates a 4x4 dimensional rotation matrix
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 CreateRotationXYZ_M4(Vector3 value)
    {
        return Matrix4.CreateRotationX(MathHelper.DegreesToRadians(value.X)) *
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(value.Y)) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(value.Z));
    }

    // Creates a 3x3 dimensional rotation matrix
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3 CreateRotationXYZ_M3(Vector3 value)
    {
        return Matrix3.CreateRotationX(MathHelper.DegreesToRadians(value.X)) *
            Matrix3.CreateRotationY(MathHelper.DegreesToRadians(value.Y)) *
            Matrix3.CreateRotationZ(MathHelper.DegreesToRadians(value.Z));
    }
}

[Component]
public struct Scale
{
    public Scale(Vector3 value)
        => Scales = value;

    public Vector3 Scales;
}

[Component]
public struct Rotation
{
    public Rotation(Vector3 value)
        => Rotations = value;

    public Vector3 Rotations;
}

[Component]
public struct Translation
{
    public Translation(Vector3 value)
        => Translations = value;

    public Vector3 Translations;
}