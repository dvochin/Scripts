/*###DISCUSSION: Flex

=== NEXT ===
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


public interface IFlexProcessor {
    void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters);
}


public class CFlex : MonoBehaviour {

	//void Start () {}
	//void Update () {}

    public static void CreateFlexObject(GameObject go, Mesh oMeshAppearance, Mesh oMeshFlexCollider, uFlex.FlexBodyType bodyType, uFlex.FlexInteractionType interactionType, float nMass, Color color)
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
        bool softMesh = (bodyType == uFlex.FlexBodyType.Soft) || (bodyType == uFlex.FlexBodyType.Cloth);


        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        Vector3[] vertices = oMeshFlexCollider.vertices;
        int vertexCount = oMeshFlexCollider.vertexCount;

        int[] triangles = oMeshFlexCollider.triangles;
        int triIndicesCount = triangles.Length;
        int trianglesCount = triIndicesCount / 3;

        IntPtr flexAssetPtr = IntPtr.Zero;

        int[] uniqueVerticesIds = new int[vertexCount];
        int[] origToUniqueVertMapping = new int[vertexCount];
        int uniqueVerticesCount = uFlex.FlexExt.flexExtCreateWeldedMeshIndices(vertices, vertexCount, uniqueVerticesIds, origToUniqueVertMapping, 0.00001f);

        Debug.Log("Welding Mesh: " + uniqueVerticesCount + "/" + vertexCount);

        Vector3[] uniqueVertices = new Vector3[uniqueVerticesCount];
        Vector4[] verticesWithInvMass = new Vector4[uniqueVerticesCount];               //###MOD
        Vector4[] uniqueVerticesWithInvMass = new Vector4[uniqueVerticesCount];
        for (int i = 0; i < uniqueVerticesCount; i++) {
            uniqueVertices[i] = vertices[uniqueVerticesIds[i]];

            verticesWithInvMass[i] = vertices[i];           //###MOD
            verticesWithInvMass[i].w = 1.0f / nMass;

            uniqueVerticesWithInvMass[i] = vertices[uniqueVerticesIds[i]];
            uniqueVerticesWithInvMass[i].w = 1.0f / nMass;

            min = uFlex.FlexUtils.Min(min, uniqueVertices[i]);
            max = uFlex.FlexUtils.Max(max, uniqueVertices[i]);
        }

        int[] uniqueTriangles = new int[trianglesCount * 3];
        for (int i = 0; i < trianglesCount * 3; i++) {
            uniqueTriangles[i] = origToUniqueVertMapping[triangles[i]];
        }


        if (bodyType == uFlex.FlexBodyType.Rigid)       //###CLEANUP
        {
            //flexAssetPtr = FlexExt.flexExtCreateRigidFromMesh(vertices, vertexCount, triangles, triIndicesCount, rigidRadius, rigidExpand);
            ///flexAssetPtr = uFlex.FlexExt.flexExtCreateRigidFromMesh(uniqueVertices, uniqueVerticesCount, uniqueTriangles, triIndicesCount, rigidRadius, rigidExpand);
        }

        if (bodyType == uFlex.FlexBodyType.Soft)
        {   //###F: Do away with vert glueing?
            flexAssetPtr = uFlex.FlexExt.flexExtCreateSoftFromMesh(vertices, vertexCount, triangles, triIndicesCount, particleSpacing, volumeSampling, surfaceSampling, clusterSpacing, clusterRadius, clusterStiffness, linkRadius, linkStiffness);
            //flexAssetPtr = uFlex.FlexExt.flexExtCreateSoftFromMesh(uniqueVertices, uniqueVerticesCount, uniqueTriangles, triIndicesCount, particleSpacing, volumeSampling, surfaceSampling, clusterSpacing, clusterRadius, clusterStiffness, linkRadius, linkStiffness);
        }

        if (bodyType == uFlex.FlexBodyType.Cloth)
        {
            flexAssetPtr = uFlex.FlexExt.flexExtCreateClothFromMesh(verticesWithInvMass, vertexCount, triangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, 0);
            //flexAssetPtr = uFlex.FlexExt.flexExtCreateClothFromMesh(uniqueVerticesWithInvMass, uniqueVerticesCount, uniqueTriangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, 0);
        }

        if (bodyType == uFlex.FlexBodyType.Inflatable)
        {
            ///flexAssetPtr = uFlex.FlexExt.flexExtCreateClothFromMesh(verticesWithInvMass, vertexCount, triangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, pressure);
            ///flexAssetPtr = uFlex.FlexExt.flexExtCreateClothFromMesh(uniqueVerticesWithInvMass, uniqueVerticesCount, uniqueTriangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, pressure);
        }

        //if (bodyType == uFlex.FlexBodyType.Cloth)
        //{
        //    // flexAssetPtr = FlexExt.flexExtCreateClothFromMesh(verticesWithInvMass, vertexCount, triangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, 0);
        //    ///flexAssetPtr = uFlex.FlexExt.flexExtCreateClothFromMesh(uniqueVerticesWithInvMass, uniqueVerticesCount, uniqueTriangles, trianglesCount, stretchStiffness, bendStiffness, tetherStiffness, tetherGive, 0);
        //}

        if (flexAssetPtr != IntPtr.Zero)
        {
            //Flex Asset Marshalling
            uFlex.FlexExt.FlexExtAsset flexAsset = (uFlex.FlexExt.FlexExtAsset)Marshal.PtrToStructure(flexAssetPtr, typeof(uFlex.FlexExt.FlexExtAsset));
            Vector4[] particles = uFlex.FlexUtils.MarshallArrayOfStructures<Vector4>(flexAsset.mParticles, flexAsset.mNumParticles);

            int[] springIndices = uFlex.FlexUtils.MarshallArrayOfStructures<int>(flexAsset.mSpringIndices, flexAsset.mNumSprings * 2);
            float[] springRestLengths = uFlex.FlexUtils.MarshallArrayOfStructures<float>(flexAsset.mSpringRestLengths, flexAsset.mNumSprings);
            float[] springCoefficients = uFlex.FlexUtils.MarshallArrayOfStructures<float>(flexAsset.mSpringCoefficients, flexAsset.mNumSprings);

            int[] shapeIndices = uFlex.FlexUtils.MarshallArrayOfStructures<int>(flexAsset.mShapeIndices, flexAsset.mNumShapeIndices);
            int[] shapeOffsets = uFlex.FlexUtils.MarshallArrayOfStructures<int>(flexAsset.mShapeOffsets, flexAsset.mNumShapes);
            Vector3[] shapeCenters = uFlex.FlexUtils.MarshallArrayOfStructures<Vector3>(flexAsset.mShapeCenters, flexAsset.mNumShapes);
            float[] shapeCoefficients = uFlex.FlexUtils.MarshallArrayOfStructures<float>(flexAsset.mShapeCoefficients, flexAsset.mNumShapes);

            Debug.LogFormat("[FlexSB] '{0}' has {1} Particules  {2} Shapes  {3} Springs  {4} Tris.", go.name, flexAsset.mNumParticles, flexAsset.mNumShapes, flexAsset.mNumSprings, flexAsset.mNumTriangles);


            if (flexAsset.mNumParticles > 0) {
                uFlex.FlexParticles part = go.AddComponent<uFlex.FlexParticles>();
                part.m_particlesCount = flexAsset.mNumParticles;
                part.m_particles = new uFlex.Particle[flexAsset.mNumParticles];
                part.m_colours = new Color[flexAsset.mNumParticles];
                part.m_velocities = new Vector3[flexAsset.mNumParticles];
                part.m_densities = new float[flexAsset.mNumParticles];
                part.m_particlesActivity = new bool[flexAsset.mNumParticles];
                part.m_colour = color;
                part.m_interactionType = interactionType;
                part.m_collisionGroup = group;
                part.m_bounds.SetMinMax(min, max);

                for (int i = 0; i < flexAsset.mNumParticles; i++) {
                    part.m_particles[i].pos = particles[i];
                    part.m_particles[i].invMass = particles[i].w;
                    part.m_colours[i] = color;
                    part.m_particlesActivity[i] = true;
                }

            }


            if (flexAsset.mNumSprings > 0) {
                uFlex.FlexSprings springs = go.AddComponent<uFlex.FlexSprings>();
                springs.m_springsCount = flexAsset.mNumSprings;
                springs.m_springIndices = springIndices;
                springs.m_springRestLengths = springRestLengths;
                springs.m_springCoefficients = springCoefficients;
            }

            if (flexAsset.mNumTriangles > 0) {
                uFlex.FlexTriangles tris = go.AddComponent<uFlex.FlexTriangles>();
                tris.m_trianglesCount = trianglesCount;
                tris.m_triangleIndices = uniqueTriangles;
            }


            uFlex.FlexShapeMatching shapes = null;
            if (flexAsset.mNumShapes > 0) {
                shapes = go.AddComponent<uFlex.FlexShapeMatching>();
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
                for (int s = 0; s < shapes.m_shapesCount; s++) {
                    shapes.m_shapeTranslations[s] = new Vector3();
                    shapes.m_shapeRotations[s] = Quaternion.identity;

                    shapeIndex++;

                    int shapeEnd = shapes.m_shapeOffsets[s];

                    for (int i = shapeStart; i < shapeEnd; ++i) {
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

            if (flexAsset.mInflatable) {
                uFlex.FlexInflatable infla = go.AddComponent<uFlex.FlexInflatable>();
                infla.m_pressure = flexAsset.mInflatablePressure;
                infla.m_stiffness = flexAsset.mInflatableStiffness;
                infla.m_restVolume = flexAsset.mInflatableVolume;
            }

            if (softMesh) {
                Renderer rend = null;
                if (bodyType == uFlex.FlexBodyType.Soft)
                {
                    float[] skinWeights = new float[oMeshAppearance.vertexCount * 4];
                    int[] skinIndices = new int[oMeshAppearance.vertexCount * 4];
                    uFlex.FlexExt.flexExtCreateSoftMeshSkinning(oMeshAppearance.vertices, oMeshAppearance.vertexCount, shapes.m_shapeCenters, shapes.m_shapesCount, skinFalloff, skinMaxDist, skinWeights, skinIndices);

                    Mesh mesh = new Mesh();
                    mesh.name = oMeshAppearance.name + "FlexMesh";
                    mesh.vertices = oMeshAppearance.vertices;
                    mesh.triangles = oMeshAppearance.triangles;
                    mesh.normals = oMeshAppearance.normals;
                    mesh.uv = oMeshAppearance.uv;

                    Transform[] bones = new Transform[shapes.m_shapesCount];
                    BoneWeight[] boneWeights = new BoneWeight[oMeshAppearance.vertexCount];
                    Matrix4x4[] bindPoses = new Matrix4x4[shapes.m_shapesCount];

                    Vector3[] rigidRestPoses = new Vector3[shapes.m_shapesCount];

                    //GameObject oTemplateGO = Resources.Load("Prefabs/CVisualizeShape", typeof(GameObject)) as GameObject;

                    for (int i = 0; i < shapes.m_shapesCount; i++) {
                        rigidRestPoses[i] = shapes.m_shapeCenters[i];

                        bones[i] = new GameObject("FlexShape_" + i).transform;
                        //bones[i] = Instantiate(oTemplateGO).transform;
                        bones[i].name = "Shape" + i.ToString();
                        bones[i].parent = go.transform;
                        bones[i].localPosition = shapes.m_shapeCenters[i];
                        bones[i].localRotation = Quaternion.identity;

                        bindPoses[i] = bones[i].worldToLocalMatrix * go.transform.localToWorldMatrix;
                    }

                    for (int i = 0; i < oMeshAppearance.vertexCount; i++) {
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

                    ///AssetDatabase.CreateAsset(mesh, "Assets/uFlex/Meshes/" + inputMesh.name + "FlexMesh.asset");     //###CHECK!
                    ///AssetDatabase.SaveAssets();

                    //uFlex.FlexSkinnedMesh skin = go.AddComponent<uFlex.FlexSkinnedMesh>();
                    uFlex.FlexSkinnedMesh skin = CUtility.FindOrCreateComponent(go, typeof(uFlex.FlexSkinnedMesh)) as uFlex.FlexSkinnedMesh;
                    skin.m_bones = bones;
                    skin.m_boneWeights = boneWeights;
                    skin.m_bindPoses = bindPoses;

                    //rend = go.AddComponent<SkinnedMeshRenderer>();
                    rend = CUtility.FindOrCreateComponent(go, typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
                    ((SkinnedMeshRenderer)rend).sharedMesh = mesh;
                    ((SkinnedMeshRenderer)rend).updateWhenOffscreen = true;
                    ((SkinnedMeshRenderer)rend).quality = SkinQuality.Bone4;
                    ((SkinnedMeshRenderer)rend).bones = bones;

                    rend.enabled = false;       //###MOD: We don't actually draw the skinned mesh... we bake its verts into a regular mesh, adjust rim pos and normals and draw that one!
                }

                if (bodyType == uFlex.FlexBodyType.Rigid) {
                    go.AddComponent<uFlex.FlexRigidTransform>();
                    //MeshFilter meshFilter = go.AddComponent<MeshFilter>();
                    MeshFilter meshFilter = CUtility.FindOrCreateComponent(go, typeof(MeshFilter)) as MeshFilter;
                    meshFilter.sharedMesh = oMeshFlexCollider;
                    rend = CUtility.FindOrCreateComponent(go, typeof(MeshRenderer)) as MeshRenderer;
                }

                if (bodyType == uFlex.FlexBodyType.Cloth || bodyType == uFlex.FlexBodyType.Inflatable) {
                    go.AddComponent<uFlex.FlexClothMesh>();
                    //MeshFilter meshFilter = go.AddComponent<MeshFilter>();
                    MeshFilter meshFilter = CUtility.FindOrCreateComponent(go, typeof(MeshFilter)) as MeshFilter;
                    meshFilter.sharedMesh = oMeshFlexCollider;
                    //rend = go.AddComponent<MeshRenderer>();
                    rend = CUtility.FindOrCreateComponent(go, typeof(MeshRenderer)) as MeshRenderer;
                }

                //Material mat = new Material(Shader.Find("Diffuse"));
                //mat.name = newName + "Mat";
                //mat.color = color;
            }
        }

        //=== Add particle renderer ===
        uFlex.FlexParticlesRenderer partRend = CUtility.FindOrCreateComponent(go, typeof(uFlex.FlexParticlesRenderer)) as uFlex.FlexParticlesRenderer;
        partRend.m_size = particleSpacing;
        partRend.m_radius = partRend.m_size / 2.0f;
        partRend.enabled = false;           // Hidden by default
    }
}
