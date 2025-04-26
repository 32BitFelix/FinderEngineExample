

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.ECSS;
using Core.MemoryManagement;
using Core.Transformations;
using OpenTK.Mathematics;

using static Core.MemoryManagement.NAHandler;


// TODO: Document and refactor this mess

namespace Core.SAS2D;

	// The broadphaser deals with
    // broadphase collision detection
	[Component]
	public struct BroadPhaser
	{
		// The array to hold all the
		// pairs that have been made out
		public static NA<long> Pairs;

		// The radius of the
		// broad phasing circle
		public float Radius;
	}


	// Token: 0x0200001C RID: 28
	[Component]
	public unsafe struct RigidBody
	{
		// Token: 0x0600006C RID: 108 RVA: 0x000057FC File Offset: 0x000039FC
		unsafe static RigidBody()
		{
			fixed (NA<bool>* ptr = &occupied)
			{
				NA<bool>* oPtr = ptr;
				Set(0, true, oPtr);
			}
			fixed (NA<float>* ptr2 = &frictions)
			{
				NA<float>* fPtr = ptr2;
				Set(0, 0f, fPtr);
			}
			fixed (NA<float>* ptr2 = &bouncinesses)
			{
				NA<float>* bPtr = ptr2;
				Set(0, 0f, bPtr);
			}
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00005868 File Offset: 0x00003A68
		//[ComponentFixedUpdate]
		public unsafe static void FixedUpdate()
		{
			RigidBody* rigidBodies;

			int* rigidBodyEntities;

			int rigidBodyLength;


			ECSSHandler.GetCompactColumn(&rigidBodies, &rigidBodyEntities, &rigidBodyLength);


			float deltaTime = ECSSHandler.FixedDeltaTime * ECSSHandler.GetTimeScale();

			for (int i = 1; i < rigidBodyLength; i++)
			{
				if(rigidBodyEntities[i] == 0)
					continue;

				if(!ECSSHandler.GetEnableState(rigidBodyEntities[i]))
					continue;

				if((rigidBodies[i].rigidBodyAttribs & RigidBodyAttrib.NotSimulated) == RigidBodyAttrib.NotSimulated)
					continue;


				Translation* translation = ECSSHandler.GetComponent<Translation>(rigidBodyEntities[i]);

				rigidBodies[i]._translationalVelocity = translation->Translations.Xy - translation->Translations.Xy;

				rigidBodies[i]._linearVelocity = (rigidBodies[i]._linearVelocity + (rigidBodies[i]._linearVelocity + WorldGravity * rigidBodies[i].Mass * deltaTime)) * 0.5f;
				
				translation->Translations.Xy = translation->Translations.Xy + rigidBodies[i]._linearVelocity * deltaTime;
			}
		}

		// Token: 0x0600006E RID: 110 RVA: 0x000059AC File Offset: 0x00003BAC
		public static void CreateRigidBody(int entityID, int materialIndex = 0, float mass = 1f)
		{
			RigidBody r = new RigidBody
			{
				MaterialIndex = materialIndex,
				Mass = mass
			};

			ECSSHandler.AddComponent(entityID, r);
		}

		// Token: 0x0600006F RID: 111 RVA: 0x000059DA File Offset: 0x00003BDA
		public unsafe static void AddForce(int entityID, Vector2 force)
		{
			RigidBody* r = ECSSHandler.GetComponent<RigidBody>(entityID);

			if((r->rigidBodyAttribs & RigidBodyAttrib.NotSimulated) == RigidBodyAttrib.NotSimulated)
				return;

			r->_linearVelocity += force;
		}

		// Token: 0x06000070 RID: 112 RVA: 0x000059F8 File Offset: 0x00003BF8
		public unsafe static int MakeRigidBodyMaterial(float bounciness, float friction)
		{
			// Holds the index at which
			// the new rigidbody material
			// should reside at.
			// Fallback is set to the
			// length of the array
			int nIndex = occupied.Length;

			// Iterate through each occupation
			for(int i = 0; i < occupied.Length; i++)
			{
				// If the current material
				// is occupied...
				if(occupied.Values[i])
					// Skip to the
					// next iteration
					continue;

				// Save the new index
				nIndex = i;

				// End the loop
				break;
			}


			fixed (NA<bool>* ptr = &occupied)
			{
				NA<bool>* oPtr = ptr;
				Set(nIndex, true, oPtr);
			}

			fixed (NA<float>* ptr2 = &frictions)
			{
				NA<float>* fPtr = ptr2;
				Set(nIndex, friction, fPtr);
			}

			fixed (NA<float>* ptr2 = &bouncinesses)
			{
				NA<float>* bPtr = ptr2;
				Set(nIndex, bounciness, bPtr);
			}

			return nIndex;
		}

		// Token: 0x04000058 RID: 88
		public static Vector2 WorldGravity = (0f, -9.81f);

		// Token: 0x04000059 RID: 89
		public static NA<float> bouncinesses;

		// Token: 0x0400005A RID: 90
		public static NA<float> frictions;

		// Token: 0x0400005B RID: 91
		private static NA<bool> occupied;

		// Token: 0x0400005C RID: 92
		public Vector2 _translationalVelocity;

		// Token: 0x0400005D RID: 93
		public Vector2 _linearVelocity;

		// Token: 0x0400005E RID: 94
		public int MaterialIndex;

		// Token: 0x0400005F RID: 95
		public float Mass;

		// Token: 0x04000060 RID: 96
		public RigidBodyAttrib rigidBodyAttribs;


		[ComponentInitialise]
		public static void Initialize(int entityID)
		{

		}


		[ComponentFinalise]
		public static void Finalize(int entityID)
		{
			RigidBody* rb = ECSSHandler.GetComponent<RigidBody>(entityID);

			occupied.Values[rb->MaterialIndex] = false;
		}
	}


	// Defines unique behaviour
	// for a rigidbody
	public enum RigidBodyAttrib : byte
	{
		// No attribute has
		// been applied
		None = 0,

		// Constraints the
		// z rotation from
		// all physics interactions
		ConstrainZRot = 1,

		// Constraints the
		// x position from
		// all physics interactions
		ConstrainXPos = 2,

		// Constraints the
		// y position from
		// all physics interactions
		ConstrainYPos = 4,

		// Does not simulate the
		// rigidbody at any shape
		NotSimulated = 8
	}


	// The collider component
	// makes any bound entity
	// affected by collision
	[Component]
	public unsafe struct Collider
	{
		// static constructor
		unsafe static Collider()
		{
			
			fixed (NA<int>* ptr = &layers)
			{
				NA<int>* lPtr = ptr;
				NAHandler.Set(0, 0, lPtr);
			}


			fixed (NA<int>* ptr = &ignoreLayers)
			{
				NA<int>* ilPtr = ptr;
				NAHandler.Set(0, -1, ilPtr);
			}


			fixed (NA<IntPtr>* ptr2 = &onEnters)
			{
				NA<IntPtr>* oEPtr = ptr2;
				NAHandler.Set(0, 0, oEPtr);
			}


			fixed (NA<IntPtr>* ptr2 = &onStays)
			{
				NA<IntPtr>* oSPtr = ptr2;
				NAHandler.Set(0, 0, oSPtr);
			}


			fixed (NA<IntPtr>* ptr2 = &onExits)
			{
				NA<IntPtr>* oEPtr2 = ptr2;
				NAHandler.Set(0, 0, oEPtr2);
			}
		}

		// Fixed update is called every physics update
		[ComponentFixedUpdate]
		public static void FixedUpdate()
		{
			RigidBody.FixedUpdate();

			__broadPhase();

			__collisionCheck();
		}

		// Does all sorts of collision
		// detection with the pairs
		// found in the broad phaser
		private unsafe static void __collisionCheck()
		{
			// Iterate through each pair
			for(int i = 0; i < BroadPhaser.Pairs.Length; i++)
			{
				// Get the first ID
				int valA = (int)(BroadPhaser.Pairs.Values[i] >> 32);

				// Get the second ID
				int valB = (int)BroadPhaser.Pairs.Values[i];


				// If either of the IDs are disbaled
				// or invalid...
				if(!ECSSHandler.GetEnableState(valA) || !ECSSHandler.GetEnableState(valB))
					// Skip to the
					// next iteration
					continue;


				// Get the collider component
				// of entity A
				Collider* colliderA = ECSSHandler.GetComponent<Collider>(valA);

				// Get the collider component
				// of entity B
				Collider* colliderB = ECSSHandler.GetComponent<Collider>(valB);


				if((colliderA->colliderAttribs & ColliderAttrib.NotResolved) == ColliderAttrib.NotResolved)
					continue;

				if((colliderB->colliderAttribs & ColliderAttrib.NotResolved) == ColliderAttrib.NotResolved)
					continue;

				
				// If collider a ignores collider b...
				if(ignoreLayers.Values[colliderA->colliderMaterialIndex] ==
					layers.Values[colliderB->colliderMaterialIndex])
					// Skip to the
					// next iteration
					continue;


				// If collider b ignores collider a...
				if(ignoreLayers.Values[colliderB->colliderMaterialIndex] ==
					layers.Values[colliderA->colliderMaterialIndex])
					// Skip to the
					// next iteration
					continue;


				// A float to hold
				// the closest depth
				// from the collision
				float sDepth = 0f;

				// A vector2 to hold
				// the normal of the
				// closest collision
				Vector2 sNormal = (0, 0);


				// Check if a
				// collision occured
				__areColliding(valA, valB, colliderA, colliderB, &sDepth, &sNormal);


				// Handling of the
				// collision events
				// depending on the
				// situation
				__collisionEvent(valA, valB, colliderA, colliderB, sNormal != (0, 0), sNormal);


				if((colliderA->colliderAttribs & ColliderAttrib.Effector) == ColliderAttrib.Effector)

					continue;


				if((colliderB->colliderAttribs & ColliderAttrib.Effector) == ColliderAttrib.Effector)
				
					continue;


				if (sNormal != (0, 0) && ECSSHandler.ContainsComponent<RigidBody>(valA) && ECSSHandler.ContainsComponent<RigidBody>(valB))

					__rbResolve(valA, valB, sNormal);
			}
		}


		private unsafe static void __broadPhase()
		{
            // Reset each pair
            for(int i = 0; i < BroadPhaser.Pairs.Length; i++)
                BroadPhaser.Pairs.Values[i] = 0;

            // The counter that keeps
            // track of the last index
            // that was populated with
            // pair data
            int pairIndex = 0;


            // Reference to the array
            // that holds all broadphasers
			BroadPhaser* broadPhasers;

            // Reference that holds all
            // IDs of entities bound to
            // the broadphasers
			int* broadPhaserEntities;

            // The length of both the
            // broadphaser and ID array
			int broadPhaserLength;


            // Get the necessary info
			ECSSHandler.GetCompactColumn(&broadPhasers, &broadPhaserEntities, &broadPhaserLength);


            // The a iteration of each broadphaser
            for(int a = 1; a < broadPhaserLength - 1; a++)
            {

				// Save the ID of
				// entity a
				int entityA = broadPhaserEntities[a];


				// If entity a is
				// disabled or invalid...
				if(!ECSSHandler.GetEnableState(entityA))
					// Skip to the
					// next ietration
					continue;


                // Get the translation
                // of a
				Vector2 originA =
                    Gymbal.GetRelativeTranslation(broadPhaserEntities[a]).Xy;

                // Get the radius
                // of a's circle
				float radA = broadPhasers[a].Radius;

                // Get the scale 
                // of a
				Vector2 scaleA = Gymbal.GetRelativeScale(broadPhaserEntities[a]).Xy;


                // Scale the 
                // circle accordingly
				if (scaleA.X >= scaleA.Y)
					radA *= scaleA.X;
				else
					radA *= scaleA.Y;


                // The b iteration of each broadphaser
                for(int b = a + 1; b < broadPhaserLength; b++)
                {
					// Save the ID of
					// entity b
					int entityB = broadPhaserEntities[b];


					// If entity b is
					// disabled or invalid...
					if(!ECSSHandler.GetEnableState(entityB))
						// Skip to the
						// next ietration
						continue;

                    // Get the translation
                    // of b
                    Vector2 originB =
                        Gymbal.GetRelativeTranslation(broadPhaserEntities[b]).Xy;

                    // Get the radius
                    // of b's circle
                    float radB = broadPhasers[b].Radius;

                    // Get the scale 
                    // of b
                    Vector2 scaleB = Gymbal.GetRelativeScale(broadPhaserEntities[b]).Xy;


                    // Scale the 
                    // circle accordingly
                    if (scaleB.X >= scaleB.Y)
                        radB *= scaleB.X;
                    else
                        radB *= scaleB.Y;


					// Calculate the difference
					// between a anb b
					float differenceLength = (originA - originB).Length;

					
					// If the difference is
					// greater than the sum
					// of both circle's radii...
					if(radA + radB < differenceLength)
						// Skip to the
						// next itration
						continue;


					// Set the first
					// entity ID
					long pair = entityA;


					// Shift the first
					// entity ID to the
					// right to make space
					// for the second ID
					pair = pair << 32;

#pragma warning disable CS0675

					// Add the second
					// entity ID
					pair |= entityB;

#pragma warning restore CS0675

					
					// Fix the array . . .
					fixed(NA<long>* pPtr = &BroadPhaser.Pairs)
						// Add the new pair
						// to the list
						Set(pairIndex++, pair, pPtr);
                }
            }
		}

		// Checks if the given pair
		// are colliding with eachother
		private unsafe static bool __areColliding(int entityA, int entityB,
			Collider* colliderA, Collider* colliderB,
				float* dep, Vector2* norm)
		{

			// Get the model matrix of a
			Matrix4 modelA = Gymbal.GetModelMatrix(entityA);

			// Get the model matrix of b
			Matrix4 modelB = Gymbal.GetModelMatrix(entityB);


			// Allocate an array to hold
			// the vertices of a's trinagles
			Vector2* aVals =
				(Vector2*)NativeMemory.Alloc((nuint)(sizeof(Vector2) * 4));

			// Allocate an array to hold
			// the vertices of b's trinagles
			Vector2* bVals =
				(Vector2*)NativeMemory.Alloc((nuint)(sizeof(Vector2) * 4));


			// Reset the depth
			*dep = 0f;

			// Reset the normal
			*norm = (0, 0);


			// TODO:
			// Add linear broad phase
			// collision detection like Sweep and prune


			// Iterate through a's triangles
			for(int a = 0; a < colliderA->triangles.Length; a++)
			{

				aVals[0] = colliderA->vertices.Values[colliderA->triangles.Values[a].A];
				aVals[1] = colliderA->vertices.Values[colliderA->triangles.Values[a].B];
				aVals[2] = colliderA->vertices.Values[colliderA->triangles.Values[a].C];

				CollisionHelper.TransformTriangle(&modelA, aVals);

				aVals[3] = (aVals[0] + aVals[1] + aVals[2]) * 0.34f;


				// Iterate through b's triangles
				for(int b = 0; b < colliderB->triangles.Length; b++)
				{

					bVals[0] = colliderB->vertices.Values[colliderB->triangles.Values[b].A];
					bVals[1] = colliderB->vertices.Values[colliderB->triangles.Values[b].B];
					bVals[2] = colliderB->vertices.Values[colliderB->triangles.Values[b].C];

					CollisionHelper.TransformTriangle(&modelB, bVals);

					bVals[3] = (bVals[0] + bVals[1] + bVals[2]) * 0.34f;


					Vector2 nNormal;

					float nDepth;


					if(!CollisionHelper.TriangleToTriangle(aVals, bVals, out nNormal, out nDepth))
						continue;

					if(nDepth < *dep)
						continue;


					*dep = nDepth;
					*norm = nNormal;
				}
			}


			NativeMemory.Free(aVals);

			NativeMemory.Free(bVals);


			if(*norm == (0, 0))

				return false;


			// If collider a counts as
			// a trigger...
			if((colliderA->colliderAttribs & ColliderAttrib.Trigger) == ColliderAttrib.Trigger)
				// Exit positive
				// without doing
				// any collision
				// resolution
				return true;


			// If collider b counts as
			// a trigger...
			if((colliderB->colliderAttribs & ColliderAttrib.Trigger) == ColliderAttrib.Trigger)
				// Exit positive
				// without doing
				// any collision
				// resolution
				return true;


			// If collider a counts as
			// an effector...
			if((colliderA->colliderAttribs & ColliderAttrib.Effector) == ColliderAttrib.Effector)
			{
				// If collider b also
				// has a rigidbody...
				if(ECSSHandler.ContainsComponent<RigidBody>(entityB))
					// Add some force
					// to collider b
					RigidBody.AddForce(entityB, colliderA->effectorForce);

				// Exit positively
				// without doing
				// any collision
				// resolution
				return true;
			}


			// If collider b counts as
			// an effector...
			if((colliderB->colliderAttribs & ColliderAttrib.Effector) == ColliderAttrib.Effector)
			{
				// If collider a also
				// has a rigidbody...
				if(ECSSHandler.ContainsComponent<RigidBody>(entityA))
					// Add some force
					// to collider a
					RigidBody.AddForce(entityA, colliderB->effectorForce);

				// Exit positively
				// without doing
				// any collision
				// resolution
				return true;
			}


			// Get transform component
			// of collider a
			Translation* aPos = ECSSHandler.GetComponent<Translation>(entityA);

			// Get transform component
			// of collider b
			Translation* bPos = ECSSHandler.GetComponent<Translation>(entityB);


			// Default a's pushstrength
			// to half
			float aPush = 0.5f;

			// If collider b is static, make
			// a's push strength full
			if((colliderB->colliderAttribs & ColliderAttrib.Static) == ColliderAttrib.Static)

				aPush = 1f;

			// Default b's pushstrength
			// to half
			float bPush = 0.5f;

			// If collider a is static, make
			// b's push strength full
			if((colliderA->colliderAttribs & ColliderAttrib.Static) == ColliderAttrib.Static)

				bPush = 1f;



			if((colliderA->colliderAttribs & ColliderAttrib.Static) != ColliderAttrib.Static)

				aPos->Translations.Xy = aPos->Translations.Xy - *norm * *dep * aPush;


			if((colliderB->colliderAttribs & ColliderAttrib.Static) != ColliderAttrib.Static)

				bPos->Translations.Xy = bPos->Translations.Xy + *norm * *dep * bPush;



			return true;
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00005FF8 File Offset: 0x000041F8
		private unsafe static void __rbResolve(int entityA, int entityB, Vector2 normal)
		{

			RigidBody* rA = ECSSHandler.GetComponent<RigidBody>(entityA);

			RigidBody* rB = ECSSHandler.GetComponent<RigidBody>(entityB);


			Vector2 relativeVelocity = rB->_linearVelocity - rA->_linearVelocity + rB->_translationalVelocity - rA->_translationalVelocity;


			if (Vector2.Dot(relativeVelocity, normal) >= 0f)

				return;


			float bounciness =
				MathF.Max(RigidBody.bouncinesses.Values[rA->MaterialIndex] * rA->Mass, RigidBody.bouncinesses.Values[rB->MaterialIndex] * rB->Mass);
			

			float rawForce = -(1f + bounciness) * Vector2.Dot(relativeVelocity, normal);

			rawForce /= 1f / rA->Mass + 1f / rB->Mass;


			Vector2 impulse = rawForce * normal;

			float friction = MathF.Max(RigidBody.frictions.Values[rA->MaterialIndex], RigidBody.frictions.Values[rB->MaterialIndex]);

			Vector2 frictionImpulse = (relativeVelocity.Normalized() + normal) * rawForce * friction;


			float aPush = 0.5f;

			if ((rB->rigidBodyAttribs & RigidBodyAttrib.NotSimulated) == RigidBodyAttrib.NotSimulated)

				aPush = 1f;


			float bPush = 0.5f;

			if ((rA->rigidBodyAttribs & RigidBodyAttrib.NotSimulated) == RigidBodyAttrib.NotSimulated)

				bPush = 1f;


			if ((rA->rigidBodyAttribs & RigidBodyAttrib.NotSimulated) != RigidBodyAttrib.NotSimulated)

				rA->_linearVelocity -= (impulse - frictionImpulse) / rA->Mass * aPush;


			if ((rB->rigidBodyAttribs & RigidBodyAttrib.NotSimulated) != RigidBodyAttrib.NotSimulated)

				rB->_linearVelocity += (impulse - frictionImpulse) / rB->Mass * bPush;
		}

		// Token: 0x06000076 RID: 118 RVA: 0x000061BC File Offset: 0x000043BC
		private unsafe static void __collisionEvent(int entityA, int entityB,
			Collider* colA, Collider* colB,
				bool areColliding, Vector2 normal)
		{

			int bIndex = -1;

			for (int i = 0; i < colA->collidingWith.Length; i++)
			{
				if (colA->collidingWith.Values[i] == entityB)
				{
					bIndex = i;
					break;
				}
			}


			if (bIndex != -1 && areColliding)
			{
				if(onStays.Values[colA->colliderMaterialIndex] != 0)

					((delegate*<int, int, Vector2, void>)onStays.Values[colA->colliderMaterialIndex])(entityA, entityB, normal);
			}


			if(bIndex != -1 && !areColliding)
			{
				colA->collidingWith.Values[bIndex] = 0;

				if(onExits.Values[colA->colliderMaterialIndex] != 0)

					((delegate*<int, int, Vector2, void>)onExits.Values[colA->colliderMaterialIndex])(entityA, entityB, normal);
			}


			if(bIndex == -1 && areColliding)
			{

				
				int freeIndex = colA->collidingWith.Length;

				for (int j = 0; j < colA->collidingWith.Length; j++)
				{
					if (colA->collidingWith.Values[j] == 0)
					{
						freeIndex = j;
						break;
					}
				}

				Set(freeIndex, entityB, &colA->collidingWith);


				if(onEnters.Values[colA->colliderMaterialIndex] != 0)

					((delegate*<int, int, Vector2, void>)onEnters.Values[colA->colliderMaterialIndex])(entityA, entityB, normal);

			}


			int aIndex = -1;

			for(int k = 0; k < colB->collidingWith.Length; k++)
			{
				if (colB->collidingWith.Values[k] == entityA)
				{
					aIndex = k;
					break;
				}
			}


			if(aIndex != -1 && areColliding)
			{

				if(onStays.Values[colB->colliderMaterialIndex] != 0)

					((delegate*<int, int, Vector2, void>)onStays.Values[colB->colliderMaterialIndex])(entityB, entityA, normal);


				return;
			}

			if(aIndex != -1 && !areColliding)
			{

				colB->collidingWith.Values[aIndex] = 0;
				

				if(onExits.Values[colB->colliderMaterialIndex] != 0)

					((delegate*<int, int, Vector2, void>)onExits.Values[colB->colliderMaterialIndex])(entityB, entityA, normal);


				return;
			}

			if(aIndex == -1 && areColliding)
			{

				int freeIndex2 = colB->collidingWith.Length;

				for(int l = 0; l < colB->collidingWith.Length; l++)
				{
					if(colB->collidingWith.Values[l] == 0)
					{
						freeIndex2 = l;
						break;
					}
				}

				Set(freeIndex2, entityA, &colB->collidingWith);


				if(onEnters.Values[colB->colliderMaterialIndex] != 0)

					((delegate*<int, int, Vector2, void>)onEnters.Values[colB->colliderMaterialIndex])(entityB, entityA, normal);

			}
		}

		// Creates a collider material,
		// which is shared across multiple
		// colliders
		public unsafe static int CreateColliderMaterial(int layer = 1, int ignoreLayer = -1,
			delegate*<int, int, Vector2, void> onEnter = null,
			delegate*<int, int, Vector2, void> onStay = null,
			delegate*<int, int, Vector2, void> onExit = null)
		{

			// Holds the index of the
			// index to load the data to.
			// Fallback is set to a new
			// index of the array
			int nIndex = layers.Length;

			// Iterate through each
			// index
			for(int i = 1; i < layers.Length; i++)
			{
				// If the current index
				// is already assigned...
				if(layers.Values[i] != 0)
					// Skip to the
					// next iteration
					continue;

				// Save the free index
				nIndex = i;

				// End the loop
				break;
			}


			// Set the layer
			// of the material
			fixed(NA<int>* ptr = &layers)
			{
				NA<int>* lPtr = ptr;
				Set(nIndex, layer, lPtr);
			}

			// Set the ignore layer
			// of the material
			fixed(NA<int>* ptr = &ignoreLayers)
			{
				NA<int>* iLPtr = ptr;
				Set(nIndex, ignoreLayer, iLPtr);
			}

			// Set the on enter event
			// of the material
			fixed(NA<IntPtr>* ptr2 = &onEnters)
			{
				NA<IntPtr>* oEPtr = ptr2;
				Set(nIndex, (nint)onEnter, oEPtr);
			}

			// Set the on stay event
			// of the material
			fixed(NA<IntPtr>* ptr2 = &onStays)
			{
				NA<IntPtr>* oSPtr = ptr2;
				Set(nIndex, (nint)onStay, oSPtr);
			}

			// Set the on exit event
			// of the material
			fixed(NA<IntPtr>* ptr2 = &onExits)
			{
				NA<IntPtr>* oEPtr2 = ptr2;
				Set(nIndex, (nint)onExit, oEPtr2);
			}

			// Return the index
			// of the material
			return nIndex;
		}


		public unsafe static void CreateCollider(int entityID, Vector2[] vertices, int colliderMaterialIndex = 0)
		{
			Collider c = new Collider
			{
				vertices = new NA<Vector2>(vertices.Length),
				triangles = new NA<Triangle>(),
				collidingWith = new NA<int>(),
				colliderMaterialIndex = colliderMaterialIndex,
				colliderAttribs = ColliderAttrib.None,
				effectorForce = (0, 0),
			};


			// The sum of all edges
			// of the given polygon
			float allSum = 0;

			// Iterate through each possible edge
			// of the polygon and add their sums
			// to the 
			for(int i = 0; i < vertices.Length; i++)
				allSum += CollisionHelper.Sum(vertices[i], vertices[(i + 1) % vertices.Length]);

			// Check if the vertices
			// of the polygon are
			// laid out clockwise
			bool isClockwise = allSum >= 0;



			// Set the method to call
			// to the vertex reader
			// for clockwise order,
			// if the polygon's vertices
			// are laid out clockwise
			nint mCall = (nint)(delegate*<NA<Vector2>*, Vector2[], void>)&CollisionHelper.GetVerticesOnClockwise * *(byte*)&isClockwise;

			// Set the method to call
			// to the vertex reader
			// for counter clockwise order,
			// if the polygon's vertices
			// are laid out counter clockwise
			mCall += (nint)(delegate*<NA<Vector2>*, Vector2[], void>)&CollisionHelper.GetVerticesOnCounterClockwise * (*(byte*)&isClockwise ^ 0x01);

			
			// Call the made out method
			((delegate*<NA<Vector2>*, Vector2[], void>)mCall)(&c.vertices, vertices);


			CollisionHelper.TriangulatePolygon(&c.triangles, &c.vertices);

			ECSSHandler.AddComponent(entityID, c);


			float rad = 0f;

			for (int j = 0; j < c.vertices.Length; j++)
			{
				if (c.vertices.Values[j].Length > rad)
					rad = Vector2.Distance(c.vertices.Values[j], new ValueTuple<float, float>(0f, 0f));

			}


			ECSSHandler.AddComponent(entityID, new BroadPhaser
			{
				Radius = rad
			});
		}

		// Token: 0x04000066 RID: 102
		private static NA<int> layers;

		// Token: 0x04000067 RID: 103
		private static NA<int> ignoreLayers;

		// Token: 0x04000068 RID: 104
		private static NA<nint> onEnters;

		// Token: 0x04000069 RID: 105
		private static NA<nint> onStays;

		// Token: 0x0400006A RID: 106
		private static NA<nint> onExits;

		// Token: 0x0400006B RID: 107
		public NA<Vector2> vertices;

		// Token: 0x0400006C RID: 108
		public NA<Triangle> triangles;

		// Token: 0x0400006D RID: 109
		public NA<int> collidingWith;

		// Token: 0x0400006E RID: 110
		public ColliderAttrib colliderAttribs;

		// Token: 0x0400006F RID: 111
		public int colliderMaterialIndex;

		// The force the collider
		// outputs as an effector
		public Vector2 effectorForce;


		[ComponentInitialise]
		public static void Initialize(int entityID)
		{

		}


		[ComponentFinalise]
		public static void Finalise(int entityID)
		{
			Collider* c = ECSSHandler.GetComponent<Collider>(entityID);


			Free(&c->vertices);

			Free(&c->triangles);

			Free(&c->collidingWith);
		}
	}

	// Defines unique
	// behaviour for
	// a collider
	[Flags]
	public enum ColliderAttrib : byte
	{
		// No attribute added
		None = 0,

		// Not moved by collision
		Static = 1,

		// The collider
		// counts as
		// a field
		// of force
		Effector = 2,

		// The collider
		// only outputs
		// collision
		// events
		Trigger = 4,

		// The collider
		// is not being
		// resolved
		NotResolved = 8,
	}

	// Represents the triangle
	// froma group of vertices
	public struct Triangle
	{
		// Instance constructor
		public Triangle(int i_a, int i_b, int i_c)
		{
			A = i_a;
			B = i_b;
			C = i_c;
		}

		// The index of the
		// first vertex
		public int A;

		// The index of the
		// second vertex
		public int B;

		// The index of the
		// third vertex
		public int C;
	}

	// Holds helper methods
	// for collision detection
	// things
	public static unsafe class CollisionHelper
	{
		// Token: 0x0600007A RID: 122 RVA: 0x000066C3 File Offset: 0x000048C3
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 CalcTriangleCenter(Vector2 a, Vector2 b, Vector2 c)
			=> ((a.X + b.X + c.X) * 0.34f, (a.Y + b.Y + c.Y) * 0.34f);

		// Token: 0x0600007B RID: 123 RVA: 0x00006703 File Offset: 0x00004903
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Perpendicular(Vector2 vector)
			=> (-vector.Y, vector.X);

		// Token: 0x0600007C RID: 124 RVA: 0x0000671C File Offset: 0x0000491C
		public static float HeronsTheorem(float lengthA, float lengthB, float lengthC)
		{
			float halfDiameter = (lengthA + lengthB + lengthC) / 2f;
			return MathF.Sqrt(halfDiameter * (halfDiameter - lengthA) * (halfDiameter - lengthB) * (halfDiameter - lengthC));
		}


        // Read the vertices of the
        // managed array, if the polygon
        // is clockwise
        public static void GetVerticesOnClockwise(NA<Vector2>* vert, Vector2[] mVert)
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
                vert->Values[i] = mVert[i];
        }

        // Read the vertices of the
        // managed array, if the polygon
        // is counter clockwise
        public static void GetVerticesOnCounterClockwise(NA<Vector2>* vert, Vector2[] mVert)
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
                vert->Values[i] = mVert[mVert.Length - 1 - i];
        }

        // Calculates the sum of the edge ab
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum(Vector2 a, Vector2 b)
            => (b.X - a.X) * (b.Y + a.Y);


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
            for(int v = 0; v < vertices->Length; v++)
            {
                if(v == a || v == b || v == c)  
                    continue;

                if(!IsPointInTriangle(vertices->Values[v], vertices->Values[a], vertices->Values[b], vertices->Values[c]))
                    continue;

                a = (a + 1) % vertices->Length;

                goto restart;
            }


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

    // Calculate crossproduct
    // of two vectors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float cross(Vector2 value1, Vector2 value2)
        => value1.X * value2.Y - value1.Y * value2.X;


		// Token: 0x0600007E RID: 126 RVA: 0x00006914 File Offset: 0x00004B14
		public static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
		{
			Vector2 a2 = b - a;
			Vector2 bc = c - b;
			Vector2 ca = a - c;
			Vector2 ap = p - a;
			Vector2 bp = p - b;
			Vector2 cp = p - c;
			float num = cross(a2, ap);
			float cross2 = cross(bc, bp);
			float cross3 = cross(ca, cp);
			return num <= 0f && cross2 <= 0f && cross3 <= 0f;
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
                new Vector4(verts[i].X, verts[i].Y, 0, 1) * (*model);

            // Saves the transformed
            // result to the fetched
            // index
            verts[i] = result.Xy;
        }
    }


		// Token: 0x06000081 RID: 129 RVA: 0x000069DC File Offset: 0x00004BDC
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

		// Token: 0x06000082 RID: 130 RVA: 0x00006A50 File Offset: 0x00004C50
		public static bool RayToTriangle(Vector2 aa, Vector2 ab, Vector2 ba, Vector2 bb, Vector2 bc, Vector2 aPos, Vector2 bPos, out Vector2 normal, out float depth)
		{
			normal = Vector2.Zero;
			depth = float.MaxValue;
			Vector2 edge = ab - aa;
			Vector2 axis = new Vector2(-edge.Y, edge.X);
			axis.NormalizeFast();
			float minA;
			float maxA;
			CollisionHelper.ProjectVertices2(aa, ab, axis, out minA, out maxA);
			float minB;
			float maxB;
			CollisionHelper.ProjectVertices(ba, bb, bc, axis, out minB, out maxB);
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
			edge = bb - ba;
			axis = new Vector2(-edge.Y, edge.X);
			axis.NormalizeFast();
			CollisionHelper.ProjectVertices2(aa, ab, axis, out minA, out maxA);
			CollisionHelper.ProjectVertices(ba, bb, bc, axis, out minB, out maxB);
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
			edge = bc - bb;
			axis = new Vector2(-edge.Y, edge.X);
			axis.NormalizeFast();
			CollisionHelper.ProjectVertices2(aa, ab, axis, out minA, out maxA);
			CollisionHelper.ProjectVertices(ba, bb, bc, axis, out minB, out maxB);
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
			edge = ba - bc;
			axis = new Vector2(-edge.Y, edge.X);
			axis.NormalizeFast();
			CollisionHelper.ProjectVertices2(aa, ab, axis, out minA, out maxA);
			CollisionHelper.ProjectVertices(ba, bb, bc, axis, out minB, out maxB);
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
			if (Vector2.Dot(bPos - aPos, normal) < 0f)
			{
				normal = -normal;
			}
			return true;
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00006C54 File Offset: 0x00004E54
		public static Vector2 FindContactPoint(Vector2 aa, Vector2 ab, Vector2 ba, Vector2 bb, Vector2 bc)
		{
			Vector2 contact = Vector2.Zero;
			float minDistSq = float.MaxValue;
			float distSq;
			Vector2 cp;
			CollisionHelper.PointSegmentDistance(aa, ba, bb, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(aa, bb, bc, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(aa, bc, ba, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ab, ba, bb, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ab, bb, bc, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ab, bc, ba, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ba, aa, ab, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(bb, aa, ab, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(bc, aa, ab, out distSq, out cp);
			if (distSq < minDistSq)
			{
				contact = cp;
			}
			return contact;
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00006D28 File Offset: 0x00004F28
		public static Vector2 FindContactPoint(Vector2 aa, Vector2 ab, Vector2 ac, Vector2 ba, Vector2 bb, Vector2 bc)
		{
			Vector2 contact = Vector2.Zero;
			float minDistSq = float.MaxValue;
			float distSq;
			Vector2 cp;
			CollisionHelper.PointSegmentDistance(aa, ba, bb, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(aa, bb, bc, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(aa, bc, ba, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ab, ba, bb, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ab, bb, bc, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ab, bc, ba, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ac, ba, bb, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ac, bb, bc, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ac, bc, ba, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ba, aa, ab, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ba, ab, ac, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(ba, ac, aa, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(bb, aa, ab, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(bb, ab, ac, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(bb, ac, aa, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(bc, aa, ab, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(bc, ab, ac, out distSq, out cp);
			if (distSq < minDistSq)
			{
				minDistSq = distSq;
				contact = cp;
			}
			CollisionHelper.PointSegmentDistance(bc, ac, aa, out distSq, out cp);
			if (distSq < minDistSq)
			{
				contact = cp;
			}
			return contact;
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00006EBC File Offset: 0x000050BC
		public static void PointSegmentDistance(Vector2 p, Vector2 a, Vector2 b, out float distanceSquared, out Vector2 cp)
		{
			Vector2 ab = b - a;
			float num = Vector2.Dot(p - a, ab);
			float abLenSq = ab.LengthSquared;
			float d = num / abLenSq;
			cp = a + ab * d;
			distanceSquared = Vector2.Distance(p, cp);
		}

        private static void ProjectVertices3(Vector2* vertices, Vector2 axis, out float min, out float max)
        {
			float proj = Vector2.Dot(vertices[2], axis);

            min = proj;
            max = proj;

            for(int i = 1; i > -1; i--)
            {
                proj = Vector2.Dot(vertices[i], axis);


				// Check if the projection
				// is less than the given
				// minimum
				bool check = proj < min;

				min = proj * *(byte*)&check + min * (*(byte*)&check ^ 0x01);

				
				// Check if the projection
				// is greater than the given
				// maximum
				check = proj > max;

				max = proj * *(byte*)&check + max * (*(byte*)&check ^ 0x01);
            }
        }

		// Checks if two triangles
		// intersect with eachother
		public unsafe static bool TriangleToTriangle(Vector2* triA, Vector2* triB, out Vector2 normal, out float depth)
		{
			normal = (0, 0);

			depth = float.MaxValue;


			for(int i = 2; i > -1; i--)
			{
                Vector2 va = triA[i];
                Vector2 vb = triA[(i + 1) % 3];

                Vector2 edge = vb - va;
                Vector2 axis = new Vector2(-edge.Y, edge.X);
                axis = Vector2.Normalize(axis);

                ProjectVertices3(triA, axis, out float minA, out float maxA);
                ProjectVertices3(triB, axis, out float minB, out float maxB);

                if(minA >= maxB || minB >= maxA)
                {
                    return false;
                }

                float axisDepth = MathF.Min(maxB - minA, maxA - minB);

                if(axisDepth < depth)
                {
                    depth = axisDepth;
                    normal = axis;
                }
			}


			for(int i = 2; i > -1; i--)
			{
                Vector2 va = triB[i];
                Vector2 vb = triB[(i + 1) % 3];

                Vector2 edge = vb - va;
                Vector2 axis = new Vector2(-edge.Y, edge.X);
                axis = Vector2.Normalize(axis);

                ProjectVertices3(triA, axis, out float minA, out float maxA);
                ProjectVertices3(triB, axis, out float minB, out float maxB);

                if(minA >= maxB || minB >= maxA)
                {
                    return false;
                }

                float axisDepth = MathF.Min(maxB - minA, maxA - minB);

                if(axisDepth < depth)
                {
                    depth = axisDepth;
                    normal = axis;
                }
			}


            Vector2 direction = triB[3] - triA[3];

            if(Vector2.Dot(direction, normal) < 0f)
            {
                normal = -normal;
            }


            return true;
		}

		// Token: 0x06000087 RID: 135 RVA: 0x000073EC File Offset: 0x000055EC
		private static void ProjectVertices2(Vector2 vertexA, Vector2 vertexB, Vector2 axis, out float min, out float max)
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
		}
	}
