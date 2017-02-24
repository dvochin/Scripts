/*###DISCUSSION: Flex

=== NEXT ===
- CBCloth dynamic part now collides with skinned one...  Can 
- Rethink Flex collision groups!
- Can't re-enter cut-cloth mode cuz of Blender object destruction?  Make sure Blender objects are destroyed when done cut!

- Body collider no longer there in cloth cutting mode!
- Revise hacks into Flex55.  Add to repo
- Game mode crash on second tit... too many particles?  Reduce max
- Revist traps...
- Remove duplication


- Automate creation of collider mesh
- Add cloth

- Would be great to create / recreate SB to test params!
- Fixed normals and position of rims... but SMR always renders... can shutoff??
- UVs
- Not enough pin range.
- Remove extra skinned mesh... or pass this one to Flex C#?
- Delay between the codebases
- Object name... choose where functionality goes...
    - CSoftBody is based on CBMesh which draws... pass this to Flex?
- Creating Flex object at proper tree address.

=== TODO ===
- Remove extra UV in Blender before sending.
- Start reviewing the code structure... do we keep the complete framework or do we flatten it?
- Cleanup of after-skinnedmesh-skin can be improved??

=== LATER ===
- Will have to add springs for shirt top to shoulder
- Add a fun texture for shirt.
- Add runtime cloth tightness
- Need to sync Flex frame with ours!
- Breast SB broken in Blender (no geometry??)
- Hide collision SMR and show body!
    - Add better flags for what to show... (and runtime keys?)

=== IMPROVE ===
- Performance of skinned mesh normal adjustment ok?
- Add more verts to breasts (under and over and side)... what about cleavage?

=== DESIGN ===
- Consider a single breast area instead of separate? (pins further away)  Might need pins under cleavage skin

=== IDEAS ===

=== LEARNED ===
- Flex repetition makes collisions stiffer
- Reducing clusterStiffness enables more flexibility of SB and henceforth accomodates collisions better (slower conversion tho)
- Steps to calibrate:
    - Settle on a particle size
    - Tweak clusterSpacingMult until we have a reasonable number of bones
    - Raise clusterRadiusMult from 1 until all mesh particles stick together.
    - Adjust clusterStiffness for desired stiffness
- Solid Rest Distance must be a little less (~90%) than Radius to avoid 'collision shimmer' (particles alternating colliding / not colliding)
    - Can also set 'particle collision margin
- Cloth spring coeficient shimmers over 2 for 59 iterations and 1.5 for 3 iterations.
- Adhesion super helpful to keep cloth on body!!
- Extra springs tests
    - Setting spring distance closer than particle collision distance will have two forces working against another.
        - Can't set desired distance to zero because of repel force... need to turn off collsion for those groups! (if possible)
    - Setting force too strong and distance too low will probably force simulation to blow up.
    - IDEA: Set cloth to NOT collide against itself = Can now add springs where parts of cloth should be with 'virtual particles' that are set to startup bone position!
- Good idea to set max speed to a reasonable setting (so blow ups can possibly recover?)

=== PROBLEMS ===

=== PROBLEMS??? ===

=== WISHLIST ===

*/

using UnityEngine;
using System;
using System.Runtime.InteropServices;


namespace uFlex
{
    public interface IFlexProcessor {
        void PreContainerUpdate(FlexSolver solver, FlexContainer cntr, FlexParameters parameters);
    }

    public class CFlex {
		//=== Bogus variables so we can have closest-possible code between uFlex code and our needs for game-time running of this function ===  Based on FlexWindow.cs / GenerateFromMesh()

		Mesh inputMesh;         //###CHECK: Gets assigned everytime we run!  Good?
		float mass;

		public CFlex() { }

        public void CreateFlexObject(GameObject go, Mesh oMeshAppearance, Mesh oMeshFlexColider, FlexBodyType bodyType, FlexInteractionType interactionType, float nMass, Color color)
        {
            CGame oGame = CGame.INSTANCE;
            float particleSpacing = oGame.particleSpacing;              //###WEAK ###MOVE?
            float volumeSampling  = oGame.volumeSampling;
            float surfaceSampling = oGame.surfaceSampling;
            float clusterSpacing = particleSpacing * oGame.clusterSpacingMult;
            float clusterRadius = particleSpacing * oGame.clusterRadiusMult;
            float clusterStiffness = oGame.clusterStiffness;
            float linkRadius = particleSpacing * oGame.linkRadiusMult;
            float linkStiffness = oGame.linkStiffness;
            float skinFalloff = oGame.skinFalloff;          
            float skinMaxDist = particleSpacing * oGame.skinMaxDistMult;

            float stretchStiffness = oGame.stretchStiffness;
            float bendStiffness = oGame.bendStiffness;
            float tetherStiffness = oGame.tetherStiffness;
            float tetherGive = oGame.tetherGive;

            int group = -1;
            bool softMesh = (bodyType == FlexBodyType.Soft) || (bodyType == FlexBodyType.Cloth);


            //=== CONVERSION VARS ===
            this.inputMesh = oMeshFlexColider;			//###MODFLEX: Note that oMeshAppearance relates only to softbody section at the end of this function
			this.mass = nMass;
			bool tearable = false;
			bool inflatable = false;
			float rigidRadius = 0;
			float rigidExpand = 0;
			int phase = 0;
			int maxSplits = 0;
            int maxStrain = 0;
			float pressure = 0;
            
            //===== BEGIN LOW MODIFICATION OF GenerateFromMesh() =====
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            Vector3[] vertices = inputMesh.vertices;
            int vertexCount = inputMesh.vertexCount;

            int[] triangles = inputMesh.triangles;
            int triIndicesCount = triangles.Length;
            int trianglesCount = triIndicesCount / 3;

            IntPtr flexAssetPtr = IntPtr.Zero;

            int[] uniqueVerticesIds = new int[vertexCount];
            int[] origToUniqueVertMapping = new int[vertexCount];
            int uniqueVerticesCount = FlexExt.flexExtCreateWeldedMeshIndices(vertices, vertexCount,  uniqueVerticesIds,  origToUniqueVertMapping, 0.00001f);

            Debug.Log("Welding Mesh: " + uniqueVerticesCount + "/" + vertexCount);

            Vector3[] uniqueVertices = new Vector3[uniqueVerticesCount];
            Vector4[] verticesWithInvMass = new Vector4[uniqueVerticesCount];               //###MOD
            Vector4[] uniqueVerticesWithInvMass = new Vector4[uniqueVerticesCount];
            for (int i = 0; i < uniqueVerticesCount; i++)
            {
                uniqueVertices[i] = vertices[uniqueVerticesIds[i]];

                verticesWithInvMass[i] = vertices[i];           //###MOD
                verticesWithInvMass[i].w = 1.0f / this.mass;

                uniqueVerticesWithInvMass[i] = vertices[uniqueVerticesIds[i]];
                uniqueVerticesWithInvMass[i].w = 1.0f / this.mass;

                min = FlexUtils.Min(min, uniqueVertices[i]);
                max = FlexUtils.Max(max, uniqueVertices[i]);
            }

            int[] uniqueTriangles = new int[trianglesCount*3];
            for (int i = 0; i < trianglesCount * 3; i++)
            {
                uniqueTriangles[i] = origToUniqueVertMapping[triangles[i]];
            }


            if (bodyType == FlexBodyType.Rigid)
            {
                //flexAssetPtr = FlexExt.flexExtCreateRigidFromMesh(vertices, vertexCount, triangles, triIndicesCount, rigidRadius, rigidExpand);
                flexAssetPtr = FlexExt.flexExtCreateRigidFromMesh(uniqueVertices, uniqueVerticesCount, uniqueTriangles, triIndicesCount, rigidRadius, rigidExpand);
            }

            if (bodyType == FlexBodyType.Soft)
            {	//###MOD
                flexAssetPtr = FlexExt.flexExtCreateSoftFromMesh(vertices, vertexCount, triangles, triIndicesCount, particleSpacing, volumeSampling, surfaceSampling, clusterSpacing, clusterRadius, clusterStiffness, linkRadius, linkStiffness);
                //flexAssetPtr = FlexExt.flexExtCreateSoftFromMesh(uniqueVertices, uniqueVerticesCount, uniqueTriangles, triIndicesCount, particleSpacing, volumeSampling, surfaceSampling, clusterSpacing, clusterRadius, clusterStiffness, linkRadius, linkStiffness);
            }

            if (bodyType == FlexBodyType.Cloth)
            {
                // flexAssetPtr = FlexExt.flexExtCreateClothFromMesh(verticesWithInvMass, vertexCount, triangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, 0);
                if (!tearable)
                {
                    if (inflatable)
                        flexAssetPtr = FlexExt.flexExtCreateClothFromMesh(uniqueVerticesWithInvMass, uniqueVerticesCount, uniqueTriangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, pressure);
                    else
		    			flexAssetPtr = FlexExt.flexExtCreateClothFromMesh(verticesWithInvMass, vertexCount, triangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, 0);
                        //###MOD flexAssetPtr = FlexExt.flexExtCreateClothFromMesh(uniqueVerticesWithInvMass, uniqueVerticesCount, uniqueTriangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, 0);
                }
                else
                {
                    if (inflatable)
                        flexAssetPtr = FlexExt.flexExtCreateTearingClothFromMesh(uniqueVerticesWithInvMass, uniqueVerticesCount, uniqueVerticesCount * 2, uniqueTriangles, trianglesCount, stretchStiffness, bendStiffness, pressure);
                    else
                        flexAssetPtr = FlexExt.flexExtCreateTearingClothFromMesh(uniqueVerticesWithInvMass, uniqueVerticesCount, uniqueVerticesCount * 2, uniqueTriangles, trianglesCount, stretchStiffness, bendStiffness, 0);
                }
            }

            //if (bodyType == FlexBodyType.Inflatable)
            //{
            ////    flexAssetPtr = FlexExt.flexExtCreateClothFromMesh(verticesWithInvMass, vertexCount, triangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, pressure);
            //    flexAssetPtr = FlexExt.flexExtCreateClothFromMesh(uniqueVerticesWithInvMass, uniqueVerticesCount, uniqueTriangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, pressure);
            //}


            //if (bodyType == FlexBodyType.Tearable)
            //{
            //    // flexAssetPtr = FlexExt.flexExtCreateClothFromMesh(verticesWithInvMass, vertexCount, triangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, 0);
            //    flexAssetPtr = FlexExt.flexExtCreateTearingClothFromMesh(uniqueVerticesWithInvMass, uniqueVerticesCount, uniqueVerticesCount * 2, uniqueTriangles, trianglesCount, stretchStiffness, bendStiffness, 0);
            //}


            if (flexAssetPtr != IntPtr.Zero)
            {      
                //Flex Asset Marshalling
                FlexExt.FlexExtAsset flexAsset = (FlexExt.FlexExtAsset)Marshal.PtrToStructure(flexAssetPtr, typeof(FlexExt.FlexExtAsset));
                Vector4[] particles = FlexUtils.MarshallArrayOfStructures<Vector4>(flexAsset.mParticles, flexAsset.mNumParticles);

                int[] springIndices = FlexUtils.MarshallArrayOfStructures<int>(flexAsset.mSpringIndices, flexAsset.mNumSprings * 2);
                float[] springRestLengths = FlexUtils.MarshallArrayOfStructures<float>(flexAsset.mSpringRestLengths, flexAsset.mNumSprings);
                float[] springCoefficients = FlexUtils.MarshallArrayOfStructures<float>(flexAsset.mSpringCoefficients, flexAsset.mNumSprings);

                int[] shapeIndices = FlexUtils.MarshallArrayOfStructures<int>(flexAsset.mShapeIndices, flexAsset.mNumShapeIndices);
                int[] shapeOffsets = FlexUtils.MarshallArrayOfStructures<int>(flexAsset.mShapeOffsets, flexAsset.mNumShapes);
                Vector3[] shapeCenters = FlexUtils.MarshallArrayOfStructures<Vector3>(flexAsset.mShapeCenters, flexAsset.mNumShapes);
                float[] shapeCoefficients = FlexUtils.MarshallArrayOfStructures<float>(flexAsset.mShapeCoefficients, flexAsset.mNumShapes);

                Debug.LogFormat("[FlexSB] '{0}' has {1} Particules  {2} Shapes  {3} Springs  {4} Tris.", go.name, flexAsset.mNumParticles, flexAsset.mNumShapes, flexAsset.mNumSprings, flexAsset.mNumTriangles);


                if (flexAsset.mNumParticles > 0)
                {
					int nExtraAdded_HACK = 0;			//###HACK<18>!!!! Add extra space at end of buffers.  Used as a hack to overcome some situation where Flex codebase tries to access further into array... (Because of our code to add particles for spring?)  ###BUG<18>!!!!!!!!

                    FlexParticles part = go.AddComponent<FlexParticles>();
                    part.m_particlesCount = flexAsset.mNumParticles;
                    part.m_maxParticlesCount = flexAsset.mMaxParticles;
                    part.m_particles = new Particle[flexAsset.mMaxParticles+nExtraAdded_HACK];
                    part.m_restParticles = new Particle[flexAsset.mMaxParticles+nExtraAdded_HACK];
                    part.m_smoothedParticles = new Particle[flexAsset.mMaxParticles+nExtraAdded_HACK];
                    part.m_colours = new Color[flexAsset.mMaxParticles+nExtraAdded_HACK];
                    part.m_velocities = new Vector3[flexAsset.mMaxParticles+nExtraAdded_HACK];
                    part.m_densities = new float[flexAsset.mMaxParticles+nExtraAdded_HACK];
                    part.m_phases = new int[flexAsset.mMaxParticles+nExtraAdded_HACK];
                    part.m_particlesActivity = new bool[flexAsset.mMaxParticles+nExtraAdded_HACK];


                    part.m_colour = color;
                    part.m_interactionType = interactionType;
                    part.m_collisionGroup = group;
                    part.m_bounds.SetMinMax(min, max);

                    for (int i = 0; i < flexAsset.mNumParticles; i++)
                    {
                        part.m_particles[i].pos = particles[i];
                        part.m_particles[i].invMass = particles[i].w;
                        part.m_restParticles[i] = part.m_particles[i];
                        part.m_smoothedParticles[i] = part.m_particles[i];
                        part.m_colours[i] = color;
                        part.m_particlesActivity[i] = true;

                        
                        part.m_phases[i] = (int)phase;



                        //if (spacingRandomness != 0)
                        //{
                        //    part.m_particles[i].pos  += UnityEngine.Random.insideUnitSphere * spacingRandomness;
                        //}
                    }

                }


                if (flexAsset.mNumSprings > 0)
                {
                    FlexSprings springs = go.AddComponent<FlexSprings>();
                    springs.m_springsCount = flexAsset.mNumSprings;
                    springs.m_springIndices = springIndices;
                    springs.m_springRestLengths = springRestLengths;
                    springs.m_springCoefficients = springCoefficients;


                }

                if(flexAsset.mNumTriangles > 0)
                {
                    FlexTriangles tris = go.AddComponent<FlexTriangles>();
                    tris.m_trianglesCount = trianglesCount;
                    tris.m_triangleIndices = uniqueTriangles; 

                }


                FlexShapeMatching shapes = null;
                if (flexAsset.mNumShapes > 0)
                {
                    shapes = go.AddComponent<FlexShapeMatching>();
                    shapes.m_shapesCount = flexAsset.mNumShapes;
                    shapes.m_shapeIndicesCount = flexAsset.mNumShapeIndices;
                    shapes.m_shapeIndices = shapeIndices;
                    shapes.m_shapeOffsets = shapeOffsets;
                    shapes.m_shapeCenters = shapeCenters;
                    shapes.m_shapeCoefficients = shapeCoefficients;
                    shapes.m_shapeTranslations = new Vector3[flexAsset.mNumShapes];
                    shapes.m_shapeRotations = new Quaternion[flexAsset.mNumShapes];
                    shapes.m_shapeRestPositions = new Vector3[flexAsset.mNumShapeIndices];

                    int shapeStart = 0;
                    int shapeIndex = 0;
                    int shapeIndexOffset = 0;
                    for (int s = 0; s < shapes.m_shapesCount; s++)
                    {
                        shapes.m_shapeTranslations[s] = new Vector3();
                        shapes.m_shapeRotations[s] = Quaternion.identity;

                    //    int indexOffset = shapeIndexOffset;

                        shapeIndex++;

                        int shapeEnd = shapes.m_shapeOffsets[s];

                        for (int i = shapeStart; i < shapeEnd; ++i)
                        {
                            int p = shapes.m_shapeIndices[i];

                            // remap indices and create local space positions for each shape
                            Vector3 pos = particles[p];
                            shapes.m_shapeRestPositions[shapeIndexOffset] = pos - shapes.m_shapeCenters[s];

                            //   m_shapeIndices[shapeIndexOffset] = shapes.m_shapeIndices[i] + particles.m_particlesIndex;

                            shapeIndexOffset++;
                        }

                        shapeStart = shapeEnd;



                    }
                }

                if (flexAsset.mInflatable)
                {
                    FlexInflatable infla = go.AddComponent<FlexInflatable>();
                    infla.m_pressure = flexAsset.mInflatablePressure;
                    infla.m_stiffness = flexAsset.mInflatableStiffness;
                    infla.m_restVolume = flexAsset.mInflatableVolume;
     
                }

                if (softMesh)
                {

                    Renderer rend = null;
                    if (bodyType == FlexBodyType.Soft)
                    {
                        float[] skinWeights = new float[oMeshAppearance.vertexCount * 4];
                        int[] skinIndices = new int[oMeshAppearance.vertexCount * 4];
                        FlexExt.flexExtCreateSoftMeshSkinning(oMeshAppearance.vertices, oMeshAppearance.vertexCount, shapes.m_shapeCenters, shapes.m_shapesCount, skinFalloff, skinMaxDist, skinWeights, skinIndices);

                        Mesh mesh = new Mesh();
                        mesh.name = oMeshAppearance.name+"FlexMesh";		//###MODFLEX: Everything under softMesh deals with oMeshAppearance instead of this.inputMesh (oMeshFlexCollider)
                        mesh.vertices = oMeshAppearance.vertices;
                        mesh.triangles = oMeshAppearance.triangles;
                        mesh.normals = oMeshAppearance.normals;
                        mesh.uv = oMeshAppearance.uv;

                        Transform[] bones = new Transform[shapes.m_shapesCount];
                        BoneWeight[] boneWeights = new BoneWeight[oMeshAppearance.vertexCount];
                        Matrix4x4[] bindPoses = new Matrix4x4[shapes.m_shapesCount];

                        Vector3[] rigidRestPoses = new Vector3[shapes.m_shapesCount];

                        for (int i = 0; i < shapes.m_shapesCount; i++)
                        {
                            rigidRestPoses[i] = shapes.m_shapeCenters[i];

                            bones[i] = new GameObject("FlexShape_" + i).transform;
                            bones[i].parent = go.transform;
                            bones[i].localPosition = shapes.m_shapeCenters[i];
                            bones[i].localRotation = Quaternion.identity;

                            bindPoses[i] = bones[i].worldToLocalMatrix * go.transform.localToWorldMatrix;
                        }

                        for (int i = 0; i < oMeshAppearance.vertexCount; i++)
                        {
                            boneWeights[i].boneIndex0 = skinIndices[i * 4 + 0];
                            boneWeights[i].boneIndex1 = skinIndices[i * 4 + 1];
                            boneWeights[i].boneIndex2 = skinIndices[i * 4 + 2];
                            boneWeights[i].boneIndex3 = skinIndices[i * 4 + 3];

                            boneWeights[i].weight0 = skinWeights[i * 4 + 0];
                            boneWeights[i].weight1 = skinWeights[i * 4 + 1];
                            boneWeights[i].weight2 = skinWeights[i * 4 + 2];
                            boneWeights[i].weight3 = skinWeights[i * 4 + 3];

                        }

                        mesh.bindposes = bindPoses;
                        mesh.boneWeights = boneWeights;

                        mesh.RecalculateNormals();
                        mesh.RecalculateBounds();

                        //AssetDatabase.CreateAsset(mesh, "Assets/uFlex/Meshes/" + this.inputMesh.name + "FlexMesh.asset");		//###MOD: Runs at gametime now!
                        //AssetDatabase.SaveAssets();

                        FlexSkinnedMesh skin = go.AddComponent<FlexSkinnedMesh>();
                        skin.m_bones = bones;
                        skin.m_boneWeights = boneWeights;
                        skin.m_bindPoses = bindPoses;

                        rend = go.AddComponent<SkinnedMeshRenderer>();
                        ((SkinnedMeshRenderer)rend).sharedMesh = mesh;
                        ((SkinnedMeshRenderer)rend).updateWhenOffscreen = true;
                        ((SkinnedMeshRenderer)rend).quality = SkinQuality.Bone4;
                        ((SkinnedMeshRenderer)rend).bones = bones;

                        rend.enabled = false;       //###MOD: We don't actually draw the skinned mesh... we bake its verts into a regular mesh, adjust rim pos and normals and draw that one!
                    }

                    if (bodyType == FlexBodyType.Rigid)
                    {

						//go.AddComponent<FlexRigidTransform>();			
						//MeshFilter meshFilter = go.AddComponent<MeshFilter>();
						//meshFilter.sharedMesh = this.inputMesh;
						//rend = go.AddComponent<MeshRenderer>();

						go.AddComponent<uFlex.FlexRigidTransform>();		//###MODFLEX
						MeshFilter meshFilter = CUtility.FindOrCreateComponent(go, typeof(MeshFilter)) as MeshFilter;
						meshFilter.sharedMesh = this.inputMesh;
						rend = CUtility.FindOrCreateComponent(go, typeof(MeshRenderer)) as MeshRenderer;

                    }

                    if (bodyType == FlexBodyType.Cloth)
                    {
                        if (tearable)
                        {
                            FlexTearableMesh tearableMesh = go.AddComponent<FlexTearableMesh>();
                            tearableMesh.m_stretchKs = stretchStiffness;
                            tearableMesh.m_bendKs = bendStiffness;
                            tearableMesh.m_maxSplits = maxSplits;
                            tearableMesh.m_maxStrain = maxStrain;
                        }
                        else
                        {
                            go.AddComponent<FlexClothMesh>();
                        }

						//MeshFilter meshFilter = go.AddComponent<MeshFilter>();		
						//meshFilter.sharedMesh = this.inputMesh;
						//rend = go.AddComponent<MeshRenderer>();

						MeshFilter meshFilter = CUtility.FindOrCreateComponent(go, typeof(MeshFilter)) as MeshFilter;		//###MODFLEX
						meshFilter.sharedMesh = this.inputMesh;
						rend = CUtility.FindOrCreateComponent(go, typeof(MeshRenderer)) as MeshRenderer;

                    }

					/*###MOD
                    Material mat = new Material(Shader.Find("Diffuse"));
                    mat.name = this.newName + "Mat";
                    mat.color = color;
					rend.material = mat;
					*/
                }
            }


            //===== BEGIN EXTRA STUFF =====
            //=== Add particle renderer ===
            FlexParticlesRenderer partRend = CUtility.FindOrCreateComponent(go, typeof(FlexParticlesRenderer)) as FlexParticlesRenderer;
            partRend.m_size = particleSpacing;
            partRend.m_radius = partRend.m_size / 2.0f;
            partRend.enabled = false;           // Hidden by default
        }
    }
}