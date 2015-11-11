using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class CBMesh : MonoBehaviour {		// The base class to any Unity object that requires Blender meshes.  Derived classes extend functionality to skinned meshes, softbody-simulated meshes, clothing-simulated meshes

	//---------------------------------------------------------------------------	IMPLEMENTATION MEMBERS
						public	string			_sNameBase;				// The base part of the name (without suffix)
						public	string			_sNameSuffix;			// The suffix applied to the name (to find what variant in Blender)
						public	string			_sNameBlender;			// The name of our mesh in Blender = Base + Suffix.  (Note that Blender prefixes this with _S_ or _U_ for its own share flag)
	[HideInInspector]	public 	CBody			_oBody;					// Pushed in by CBody in OnStart()
	[HideInInspector]	public  Mesh			_oMeshNow;	
	[HideInInspector]	public	List<ushort>	_aSharedNormals;

	//---------------------------------------------------------------------------	MESH ARRAYS
	[HideInInspector]	public 	CMemAlloc<Vector3> 	_memVerts			= new CMemAlloc<Vector3>();	// The vertices we send and receive to/from PhysX
	[HideInInspector]	public 	CMemAlloc<Vector3> 	_memVertsStart		= new CMemAlloc<Vector3>();	// The vertices at creation time.  Can be used to 'restore' mesh at start position (e.g. softbodies)
	[HideInInspector]	public 	CMemAlloc<Vector3>	_memNormals 		= new CMemAlloc<Vector3>();	// The normals we send and receive to/from PhysX
	[HideInInspector]	public 	CMemAlloc<int>		_memTris  			= new CMemAlloc<int>();		// The triangles we send to Phys
	
	
	
	//---------------------------------------------------------------------------	INIT

	public static CBMesh Create(GameObject oBMeshGO, CBody oBody, string sNameBase, string sNameSuffix, string sCmdBlModule, string sCmdBlFunction, string sCmdExtraArgs, Type oTypeBMesh) {
		//===== Important static function that reads a Blender mesh definition stream and create the requested CBMesh-derived entity on the provided gameObject node =====
		string sNameBlender = sNameBase + sNameSuffix;
		if (oBMeshGO == null) {						// When CBody or test meshes are creating itself this will be set, null for rim and softbody parts.  ###WEAK!!!
			oBMeshGO = new GameObject(sNameBlender, oTypeBMesh);		// If we're here it means we're rim or a body part.  Create a new game object...
			if (oBody != null)
				oBMeshGO.transform.parent = oBody.transform;			//... Parent it to the body if specified (body is always specified for rim or body parts)
		}
		CBMesh oBMesh = (CBMesh)CUtility.FindOrCreateComponent(oBMeshGO.transform, oTypeBMesh);

		//=== Do the name concatenation here, remembering each aspect separately for future access by various part of the code ===
		oBMesh._sNameBase		= sNameBase;
		oBMesh._sNameSuffix		= sNameSuffix;
		oBMesh._sNameBlender	= sNameBlender;

		//=== Create various casts to subclasses to facilitate deserializing info into the proper subclasses ===
		CBSkin 	oBSkin 	= oBMesh as CBSkin;			// For each of these, cast to the xxx subclass so code that deserializes info only present on xxx can run
		CBSoft 	oBSoft 	= oBMesh as CBSoft;
		bool bCreatingSkinnedMesh = (oBSkin != null);		// We create a skinned mesh if we're a CBSkin or related rim... otherwise we're (probably) a body part requiring softbody/cloth simulation so we're just a simple mesh
		
		//=== Send blender command to obtain the mesh header.  This will return a large stream containing material names, bones, bone weight, etc ===
		CMemAlloc<byte> memBA = new CMemAlloc<byte>();
		string sCmd = sCmdBlFunction + "('" + sNameBlender + "'";
		if (sCmdExtraArgs != "") sCmd += "," + sCmdExtraArgs;
		sCmd += ")";
		CGame.gBL_SendCmd_GetMemBuffer(sCmdBlModule, sCmd, ref memBA);
		byte[] oBA = (byte[])memBA.L;
		int nPosBA = 0;

		//=== Now process the header stream for the remaining mesh info such as material names, bone tree and vert bone weights ===
		ushort nMagicBegin = BitConverter.ToUInt16(oBA, nPosBA); nPosBA+=2;		// Read basic sanity check magic number at start
		if (nMagicBegin != G.C_MagicNo_TranBegin)
			throw new CException("ERROR in CBMesh.ctor().  Invalid transaction begin magic number!");

		//=== Read the number of verts.  Must be < 64K for Unity! ===
		int nVerts = BitConverter.ToInt32(oBA, nPosBA); nPosBA+=4;
		int nTris  = BitConverter.ToInt32(oBA, nPosBA); nPosBA+=4;
		byte nMats = oBA[nPosBA]; nPosBA++;
		Debug.Log("CBMesh obtains mesh '" + sNameBlender + "' with " + nVerts + " verts " + nTris + " triangles and " + nMats + " materials.");
		
		if (nVerts > 65530)
			throw new CException("ERROR in CBMesh.ctor().  Mesh has over 64K verts: " + nVerts);
	
		//=== Create the new mesh / skinned mesh now that we have the beginning of a valid stream ===
		SkinnedMeshRenderer oSkinMeshRend 	= null;
		MeshFilter   		oMeshFilter 	= null;
		MeshRenderer 		oMeshRenderer 	= null;
		if (bCreatingSkinnedMesh) {
			oSkinMeshRend = (SkinnedMeshRenderer)CUtility.FindOrCreateComponent(oBMeshGO.transform, typeof(SkinnedMeshRenderer));
			oSkinMeshRend.updateWhenOffscreen = true;		//###CHECK: Not working?
		} else {
			oMeshFilter 	= (MeshFilter)  CUtility.FindOrCreateComponent(oBMeshGO.transform, typeof(MeshFilter));
			oMeshRenderer	= (MeshRenderer)CUtility.FindOrCreateComponent(oBMeshGO.transform, typeof(MeshRenderer));
			//oMeshRenderer.updateWhenOffscreen = true;		//###IMPROVE: How to avoid recalc bounds???
		}
		oBMesh._oMeshNow = new Mesh();				// Create the actual mesh the object will use.  Will persist throughout...
		//oBMesh._oMeshNow.MarkDynamic();			//###CHECK: Use by default or case-by-case basis in subclasses??

		//=== Read the material list from Blender and create the necessary materials linking to the provided texture images ===
		Material[] aMats = new Material[nMats];
		for (byte nMat = 0; nMat < nMats; nMat++) {
			string sCodedMaterial = CUtility.BlenderStream_ReadStringPascal(ref oBA, ref nPosBA);
			sCodedMaterial = sCodedMaterial.Replace('\\', '/');						// Replace backslashes to the slash Unity requires.
			if (sCodedMaterial != "NoTexture") {			//###IMPROVE: What to do??  ###IMPROVE: Standarize constants between blender and Unity!

				if (sCodedMaterial.StartsWith("Material_")) {			// Blender material that start with this string are processed differently: We attempt to find the same name material here and attach to this submesh.

					Material oMat = Resources.Load("Materials/" + sCodedMaterial, typeof(Material)) as Material;
					if (oMat != null)
						aMats[nMat] = oMat;
					else
						Debug.LogError("ERROR: Unknown special material '" + sCodedMaterial + "' in CBMesh.ctor()");

				} else {

					int nLocTexPath = sCodedMaterial.IndexOf(CGame.C_RelPath_Textures);		// Blender gives a fully-qualified path that MUST point to our Resources folder
					if (nLocTexPath != -1) {
						string sTextureResourcePath = sCodedMaterial.Substring(nLocTexPath + CGame.C_RelPath_Textures.Length);
						int nLocExtDot = sTextureResourcePath.IndexOf('.');			// Strip the extension... ###CHECK: Assume jpg throughout??
						if (nLocExtDot != -1)
							sTextureResourcePath = sTextureResourcePath.Substring(0, nLocExtDot);

						Material oMat = Resources.Load(sTextureResourcePath + "_Mat", typeof(Material)) as Material;		// We attempt to load the pre-defined material with _Mat suffix if its there...
						if (oMat == null) {
							if (sTextureResourcePath.EndsWith("_Transp"))			// If texture ends with this string we create a standard transparent material
								oMat = new Material(Shader.Find("Transparent/Diffuse"));
							else
								oMat = new Material(Shader.Find("Diffuse"));		// If material was not found (usual case) we just create a standard diffuse on
							//Debug.Log("-Texture: " + sTextureResourcePath);
							object o = Resources.Load(sTextureResourcePath, typeof(Texture));
							Texture oTex = o as Texture;
							if (oTex != null) {
								int nLocLastSlash = sTextureResourcePath.LastIndexOf('/');
								oMat.name = sTextureResourcePath.Substring(nLocLastSlash + 1);
								oMat.mainTexture = oTex;
							} else {
								Debug.LogError("**ERROR: Texture '" + sTextureResourcePath + "' not found!");
							}
						} else {
							//Debug.Log("-Material: " + sTextureResourcePath);			// If material was defined we leave it to material designer to connect whatever texture...
						}
						aMats[nMat] = oMat;
					} else {
						Debug.LogError("ERROR: in CBMesh.ctor().  Could not find Unity resource path in texture path " + sCodedMaterial);
					}
				}
			}
		}
		//=== If Blender didn't have a material for this mesh we now create a dummy one as we need at least one 'submesh' for DLL to pass in triangles to us ===
		if (nMats == 0) {			
			nMats = 1;	
			aMats = new Material[nMats];
			aMats[0] = new Material(Shader.Find("Diffuse"));	// Create a default material so we can see the material-less Blender mesh in Unity
		}
		
		
		
		//===== RECEIVE SKINNING INFORMATION =====
		oBMesh._oMeshNow.subMeshCount = nMats;
		Matrix4x4[] aSkinBindPoses 		= null;			//###WEAK: A bit of unfortunate structure imposed on the code because of how late we read the stream versus how we must init vars for skinned mesh...  Can be improved.
		Transform[]	aSkinBones 			= null;
		List<BoneWeight> aBoneWeights 	= null;
		
		//=== Branch reception of skinning info if Blender is sending it.  It does so only on _BodySkin, _BodyRim and _BodyCol meshes, and it is assumed Unity and Blender are synchronized on knowning when the info is sent or not)
		byte bReceivingSkinningInfo = oBA[nPosBA]; nPosBA+=1;
		if (bReceivingSkinningInfo == 1) {
			if (oBSkin == null)					// If Blender is sending skin info it assumes Unity is requesting a CBSkin-derived mesh
				throw new CException("Exception in CBMesh.Create: Blender is sending skinning info on mesh '" + sNameBlender + "' but object INSTANCE is not derived from CBSkin!");
			
			//=== Read in the flat array of names of vertex groups.  This will enable us to map the Blender vertex group blends to our fixed Unity bone rig ===
			byte nVertGroupsMesh = oBA[nPosBA]; nPosBA++;
			aSkinBones = new Transform[nVertGroupsMesh];
			aSkinBindPoses = new Matrix4x4[nVertGroupsMesh];

			//=== Find the root of our skinning bones (from our node if we're a body and from our body if we're a body collider ===		###IMPROVE: Clarify / improve tests below!!
			Transform oNodeBoneParentRoot = null;
			if (oBMesh as CBody != null)												// If we're a body we fetch our bones right off our own node.
				oNodeBoneParentRoot = oBMesh.transform.root.FindChild("Bones/chest");	//###NOTE: Bones are always found at the root node, so 'root' enables both rim and display skinned mesh can find it
			else if (oBody != null)
				oNodeBoneParentRoot = oBody.transform.FindChild("Bones/chest");			// If we're passed in a body (we're a body collider or a body-related attachment) we adopt same bones as our body.

			if (oNodeBoneParentRoot == null)
				throw new CException("CBMesh is attempting to reconstruct a skinned mesh but was not able to find root node of bones!");
			for (byte nVertGroup = 0; nVertGroup < nVertGroupsMesh; nVertGroup++) {
				string sVertGroup = CUtility.BlenderStream_ReadStringPascal(ref oBA, ref nPosBA);
				Transform oNodeBone = CUtility.FindNodeByName(oNodeBoneParentRoot, sVertGroup);
				if (oNodeBone != null) {
					//Debug.Log("Bone found for '" + sVertGroup + "'");	
					aSkinBones[nVertGroup] = oNodeBone;
					aSkinBindPoses[nVertGroup] = oNodeBone.worldToLocalMatrix;	//###CHECK
				} else {
					Debug.LogError("**ERROR: CBMesh.ctor() could not find bone '" + sVertGroup + "'");		//###DESIGN?: Throw?
				}
			}
			
			//=== Stream in the bone weight info ===		###NOTE: Note that this code assumes a compatible bone tree between Blender and Unity!!!  To update Unity bones from Blender bones use the CBodyEd.UpdateBonesFromBlender() function
			aBoneWeights = new List<BoneWeight>();
			int[] aBoneIndex = new int[32];			// Temp arrays 
			float[] aBoneWeight = new float[32];
			int nErrSumOutOfRange = 0;
			
			for (int nVert = 0; nVert < nVerts; nVert++) {
				byte nVertGroups = oBA[nPosBA]; nPosBA++;
				float nBoneWeightSum = 0;
				
				for (byte nVertGroup = 0; nVertGroup < nVertGroups; nVertGroup++) {		//###IMPROVE: It might be more elegant to shift this code to Blender Python?
					aBoneIndex[nVertGroup]  = oBA[nPosBA]; nPosBA++;
					float nBoneWeight = BitConverter.ToSingle(oBA, nPosBA); nPosBA+=4;
					if (nBoneWeight < 0)
						//Debug.LogError("CBMesh.ctor() encountered a bone weight below 0 at vert " + nVert + " and vert group " + nVertGroup);
						throw new CException("CBMesh.ctor() encountered a bone weight below 0 at vert " + nVert + " and vert group " + nVertGroup);
					if (nBoneWeight > 1)
						//throw new CException("CBMesh.ctor() encountered a bone weight over 1 at vert " + nVert + " and vert group " + nVertGroup);	//###IMPROVE: Common!  What to do? cap??
						Debug.LogWarning("CBMesh.ctor() encountered a bone weight over 1 = " + nBoneWeight + " at vert " + nVert + " and vert group " + nVertGroup);	//###IMPROVE: Common!  What to do? cap??
					aBoneWeight[nVertGroup] = nBoneWeight;
					nBoneWeightSum += nBoneWeight;
				}
	
				if (nVertGroups >= 5) 
					Debug.LogError("CBMesh.ctor() encountered a vertex with " + nVertGroups + " vertex groups!");
					//throw new CException("CBMesh.ctor() encountered a vertex with " + nVertGroups + " vertex groups!");
				if (nBoneWeightSum < 0.999 || nBoneWeightSum > 1.001) {
					///Debug.LogWarning("###W: CBMesh.ctor() vertex " + nVert + " had out of range weight of " + nBoneWeightSum);
					nErrSumOutOfRange++;
				}
				BoneWeight oBoneWeight = new BoneWeight();
				if (nVertGroups > 0) { oBoneWeight.boneIndex0 = aBoneIndex[0]; oBoneWeight.weight0 = aBoneWeight[0]; }
				if (nVertGroups > 1) { oBoneWeight.boneIndex1 = aBoneIndex[1]; oBoneWeight.weight1 = aBoneWeight[1]; }
				if (nVertGroups > 2) { oBoneWeight.boneIndex2 = aBoneIndex[2]; oBoneWeight.weight2 = aBoneWeight[2]; }
				if (nVertGroups > 3) { oBoneWeight.boneIndex3 = aBoneIndex[3]; oBoneWeight.weight3 = aBoneWeight[3]; }
				
				aBoneWeights.Add(oBoneWeight);
			}
			
			if (nErrSumOutOfRange > 0)		//###CHECK: What to do???
				Debug.LogWarning("###ERROR: CBMesh.ctor() found " + nErrSumOutOfRange + " bones with out-of-range sums!");
			
			//=== Read the number of errors detected when sending over the blender bone weights... what to do?? ===		
			int nErrorsBoneGroups = BitConverter.ToInt32(oBA, nPosBA); nPosBA+=4;
			if (nErrorsBoneGroups > 0)			//###IMPROVE ###CHECK: What to do???
				Debug.LogError("###ERROR: CBMesh.ctor() detected " + nErrorsBoneGroups + "	blender-side errors while reading in blender mesh!");

		} else {
			if (bCreatingSkinnedMesh)
				throw new CException("ERROR: CBMesh.ctor() was called to retrieve a skinned mesh but Blender not sending skinning data!");
		}
		


		//===== RECEPTION OF OPTIONAL BLENDER INFORMATION ARRAYS =====
		//=== Read the 'shared normals' flattened map if it exists.  These are important to render seamlessly accross duplicated verts at seams ===
		int nArrayElements = BitConverter.ToInt32(oBA, nPosBA) / 2; nPosBA+=4;		// Shared normals is a flat array of ushorts with Blender sending us the lenght of the array in bytes, so we divide by 2 for count of elements		###LEARN: Python struct sending ushort and sending zero made us read 32768!! WTF???
		if (nArrayElements > 0) {
			oBMesh._aSharedNormals = new List<ushort>();
			for (int nArrayElement = 0; nArrayElement  < nArrayElements; nArrayElement++) {
				oBMesh._aSharedNormals.Add(BitConverter.ToUInt16(oBA, nPosBA)); nPosBA+=2;
			}
		}
		
		//=== Receive the important 'aMapTwinVerts' array Blender has prepared for softbody-connection to skinned mesh.  (to map the softbody edge vertices to the skinned-body vertices they should attach to.)  Only present on softbody Blender meshes!
		int nTwinVerts = BitConverter.ToInt32(oBA, nPosBA) / 6; nPosBA+=4;	// Number of twin vert definitions is divided by serialized lenght per defintion (three members of 2-byte = 6 bytes)
		if (nTwinVerts > 0) {
			List<CMapTwinVert> aMapTwinVerts;
			if (oBSoft != null)
				aMapTwinVerts = oBSoft._aMapTwinVerts;
			else
				throw new CException("Error in CBMesh.ctor(): Receiving a aMapTwinVerts array but INSTANCE was not of type CBSoft or CBCloth!");

			for (int nTwinVert = 0; nTwinVert < nTwinVerts; nTwinVert++) {
				CMapTwinVert oMapTwinVert = new CMapTwinVert();
				oMapTwinVert.nVertPart	    = BitConverter.ToUInt16(oBA, nPosBA); nPosBA+=2;
				oMapTwinVert.nVertHost 		= BitConverter.ToUInt16(oBA, nPosBA); nPosBA+=2;
				oMapTwinVert.nVertHostAdj 	= BitConverter.ToUInt16(oBA, nPosBA); nPosBA+=2;
				aMapTwinVerts.Add(oMapTwinVert);
			}
		}

        //=== Read the 'end magic number' that always follows a stream.  Helps catch deserialization errors.  (Superclasses will also call this for the end of their stream additions) ===
		ReadEndMagicNumber(ref oBA, ref nPosBA);

        //=== Call this object's optional OnSerializeIn() (if it exists) so that it can extract additional information for its type to match what Blender sent it ===
        oBMesh.OnSerializeIn(ref oBA, ref nPosBA);
		
		//=== Create the memory buffers that can extract the data from our C++ dll ===
		CMemAlloc<Vector3> memVerts 	= new CMemAlloc<Vector3>();
		CMemAlloc<Vector3> memNormals 	= new CMemAlloc<Vector3>();		//###DESIGN! ###OPT!! We don't use Blender's normals anymore... keep serializing??
		CMemAlloc<Vector2> memUVs 		= new CMemAlloc<Vector2>();
		memVerts  .Allocate(nVerts);
		memNormals.Allocate(nVerts);
		memUVs    .Allocate(nVerts);

		//=== With the header data process, the GetMeshHeader command also copied the large verts & tri arrays in shared memory for us to access quickly ===
		int nError = ErosEngine.gBL_GetMeshArrays(sNameBlender, nMats, memVerts.P, memNormals.P, memUVs.P);
		if (nError != 0)
			throw new CException("Exception in CBMesh.ctor().  gBL_GetMeshArrays() returns error " + nError + " on mesh " + oBMeshGO.name);

		oBMesh._oMeshNow.vertices 	= memVerts.L;
		oBMesh._oMeshNow.normals 	= memNormals.L;
		oBMesh._oMeshNow.uv 		= memUVs.L;

		//=== Obtain access to the triangles of the mesh, one set for each material ===
		for (int nMat = 0; nMat < nMats; nMat++) {
			int nTrisThisMat = ErosEngine.gBL_GetNumTrianglesAtMaterial(nMat);			//###HACK: These are 'last access' only!
			if (nTrisThisMat > 0) {								//###IMPROVE: Issue warning when no materials?  (So Blender mesh can clean up its unused materials)?
				CMemAlloc<int> memTris = new CMemAlloc<int>();
				memTris.Allocate(3 * nTrisThisMat);				// Number of triangles = 3 * number of triangles
				ErosEngine.gBL_GetTrianglesAtMaterial(nMat, memTris.P);
				oBMesh._oMeshNow.SetTriangles(memTris.L, nMat);
			}
		}

		//=== Finalize the mesh creation by stuffing _oMeshRender into mesh filter or skinned mesh renderer as appropriate ===
		oBMesh.UpdateNormals();									// Fix the normals with the just-serialized map of shared normals
		if (bCreatingSkinnedMesh) {		
			oBMesh._oMeshNow.bindposes 	= aSkinBindPoses;
			oBMesh._oMeshNow.boneWeights 	= aBoneWeights.ToArray();
			oSkinMeshRend.sharedMesh = oBMesh._oMeshNow;					//###TODO: skinned mesh complex bounds!
			oSkinMeshRend.materials = aMats;
			oSkinMeshRend.bones = aSkinBones;
		} else {
			oBMesh._oMeshNow.RecalculateBounds();
			oMeshRenderer.materials = aMats;
			oMeshFilter.mesh = oBMesh._oMeshNow;
		}
		
		oBMesh.OnStart(oBody);					// Notify object that it has done loading so it can perform Unity initialization

		return oBMesh;
	}

    public static void ReadEndMagicNumber(ref byte[] oBA, ref int nPosBA) {
        //=== Read the end 'magic number' that must exists at the end of all Blender-sourced streams to help catch out-of-sync errors.  === (Can be called multiple times in a stream dependant on derived classes)
        ushort nMagicEnd = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;		// Read basic sanity check magic number at end
        if (nMagicEnd != G.C_MagicNo_TranEnd)
            throw new CException("ERROR in CBMesh.ctor().  Invalid transaction end magic number!  Stream is bad.");
    }
	
	public virtual void OnSerializeIn(ref byte[] oBA, ref int nPosBA) {}
	
	public virtual void OnDestroy() {
		Debug.Log("OnDestroy CBMesh " + _sNameBlender);
		CGame.gBL_SendCmd("Client", "gBL_ReleaseMesh('" + _sNameBlender + "')");		// Tell Blender to stop sharing this mesh to free shared memory.
	}			
	
	public virtual void UpdateVertsFromBlenderMesh(bool bUpdateNormals) {		// Ask Blender to update our copy of the verts.  Assumes the topology of the mesh hasn't changed!
		CGame.gBL_SendCmd("Client", "gBL_UpdateClientVerts('" + _sNameBlender + "')");
		int nError = ErosEngine.gBL_UpdateClientVerts(_sNameBlender, _memVerts.P);
		if (nError != 0)
			throw new CException("Exception in CBMesh.gBL_UpdateClientVerts().  DLL returns error " + nError + " on mesh " + gameObject.name);
		_oMeshNow.MarkDynamic();		// Docs say "Call this before assigning vertices to get better performance when continually updating mesh"
		_oMeshNow.vertices = _memVerts.L;
		if (bUpdateNormals)
			UpdateNormals();								// Verts have moved, update * fix normals...
	}	

	public void UpdateNormals() {		// Unity must split Blender's verts at the seam.  Blender provides the '' map for us to average out these normals in order to display seamlessly accross seams.  
		_oMeshNow.RecalculateNormals();					// First recalc the normals from Unity's implementations.  Duplicated verts at the seams will have slighly different normals because their polygons don't meet over seams.
		if (_aSharedNormals == null)					// If the map of shared normal info does not exist then there are no normals to fix so just RecalculateNormals() above is enough.
			return;

		//=== Manually iterate through the Blender-supplied flat list of 'shared normal groups' to set all verts that were split by seams to the same (average) normal so they appear seamless ===
		Vector3[] aNormals = _oMeshNow.normals;			// Obtain array for much faster iteration
		List<ushort> aSharedNormals_CurrentGroup = new List<ushort>();
		foreach (ushort nSharedNormal in _aSharedNormals) {
			if (nSharedNormal != G.C_MagicNo_EndOfFlatGroup) {							// Normal vertex in the current 'shared normal group'.  Just append to current group until we meet the 'end of group' magic number
				aSharedNormals_CurrentGroup.Add(nSharedNormal);
			} else {												// This 'invalid vertex ID' reprensents the 'end of shared normal group'. At this point we set the normals for this group to their average
				Vector3 vecNormalShared = new Vector3();
				foreach (ushort nSharedNormalNow in aSharedNormals_CurrentGroup)		// Iterate through our group a first time to add up the Unity-calculated normals.
					vecNormalShared += aNormals[nSharedNormalNow];
				vecNormalShared.Normalize();											// Normalize the sum
				foreach (ushort nSharedNormalNow in aSharedNormals_CurrentGroup)		// Iterate through our group a second time to stuff in the 'averaged normal' we just calculated
					aNormals[nSharedNormalNow] = vecNormalShared;
				aSharedNormals_CurrentGroup.Clear();				// Clear the current group to enable processing for the next one...
			}
		}
		_oMeshNow.normals = aNormals;					// Stuff back the fixed normals.
	}
		
	
	public virtual void OnStart(CBody oBody) {				// Function that is called right after we have finished serializing the object from Blender.  Used for initialization of the more complex derived classes
		_oBody = oBody;
		_memVerts		.AssignAndPin(_oMeshNow.vertices);
		_memVertsStart	.AssignAndPin(_oMeshNow.vertices);
		_memNormals		.AssignAndPin(_oMeshNow.normals);
		_memTris		.AssignAndPin(_oMeshNow.triangles);
	}

	
	//---------------------------------------------------------------------------	UTILITY

	public int GetNumVerts() 		{ return _oMeshNow.vertices.Length; }
	public int GetNumTris()  		{ return GetNumTriIndices() / 3; }
	public int GetNumTriIndices()  	{ return _oMeshNow.triangles.Length; }
}
