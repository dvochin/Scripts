using UnityEngine;
using System;
using System.Collections.Generic;


public class CBMesh : MonoBehaviour {		// The base class to any Unity object that requires Blender meshes.  Derived classes extend functionality to skinned meshes, softbody-simulated meshes, clothing-simulated meshes

	//---------------------------------------------------------------------------	IMPLEMENTATION MEMBERS
						public	string			_sNameCBodyInstanceMember;		// The base part of the name (without suffix)
						public	string			_sNameBlenderMesh;				// The name of the Blender mesh object.  Used to obtain mesh verts, tris, etc.
	[HideInInspector]	public 	CBodyBase		_oBodyBase;						// Body base this mesh / skinned mesh is associated too.
	[HideInInspector]	public  Mesh			_oMeshNow;	
	//[HideInInspector]	public	Vector3[]		_aNormals;						// Blender's original normals for this mesh.  We adopt Blender's normals because verts that have been split accross materials each get a 'wrong' normal
	[HideInInspector]	public	Material[]		_aMatsCurrent;					// Array of materials currently applied to mesh (any slot can be from opaque or transparent lists below)		###OBS:? Keep this array of material (needed by old crappy implementation of materials)

	[HideInInspector]	public	Boolean		    _bConnectedToBlenderMesh;		// Blender has a valid C++ connection to this Blender mesh.  (We can update its verts and get vert updates)
	[HideInInspector]	public	Boolean		    _bKeepBlenderShare;             // Link to Blender mesh won't be closed once mesh is loaded (e.g. _bConnectedToBlenderMesh always remains true)

	//---------------------------------------------------------------------------	MESH ARRAYS
	[HideInInspector]	public 	CMemAlloc<Vector3> 	_memVerts			= new CMemAlloc<Vector3>();	// The vertices we send and receive to/from PhysX
	[HideInInspector]	public 	CMemAlloc<Vector3> 	_memVertsStart		= new CMemAlloc<Vector3>();	// The vertices at creation time.  Can be used to 'restore' mesh at start position (e.g. softbodies)
	[HideInInspector]	public 	CMemAlloc<Vector3>	_memNormals 		= new CMemAlloc<Vector3>();	// The normals we send and receive to/from PhysX
	[HideInInspector]	public 	CMemAlloc<int>		_memTris  			= new CMemAlloc<int>();		// The triangles we send to Phys
    [HideInInspector]   public  List<ushort>		_aMapSharedNormals;		// Blender-created map of which verts share a given normal.  Enables seamless rendering of meshes accross material boundaries
	
	
	//---------------------------------------------------------------------------	INIT
	public static CBMesh Create(GameObject oBMeshGO, CBodyBase oBodyBase, string sNameCBodyInstanceMember, Type oTypeBMesh, Boolean bKeepBlenderShare = false, params object[] aExtraArgs) {
		//===== Important static function that reads a Blender mesh definition stream and create the requested CBMesh-derived entity on the provided gameObject node =====
	    //###DESIGN: Redo the crappy static create to just create component with a separate Initialize() with the proper arguments

		//=== Create the game object that will host our component ===
		if (oBMeshGO == null) {                     // When CBody or test meshes are creating itself this will be set, null for rim and softbody parts.  ###WEAK!!!
			oBMeshGO = new GameObject(sNameCBodyInstanceMember, oTypeBMesh);        // If we're here it means we're rim or a body part.  Create a new game object...		####DEV ####NOW: Name problem here!
			if (oBodyBase._oBody != null)											// Parent to body main node if available, to body base static collider if not.
				oBMeshGO.transform.SetParent(oBodyBase._oBody._oBodySkinnedMeshGO_HACK.transform);   //... Parent it to the body if it's there  ###HACK!
			else
				oBMeshGO.transform.SetParent(oBodyBase._oMeshStaticCollider.transform);		//... else to the body base
		}

		//=== Create our component (of the requested type) from the above-created game object ===
		CBMesh oBMesh = (CBMesh)CUtility.FindOrCreateComponent(oBMeshGO.transform, oTypeBMesh);
		oBMesh._oBodyBase = oBodyBase;									// Push-in some important args manually (so we don't have to push them in OnDeserializeFromBlender()
		oBMesh._sNameCBodyInstanceMember = sNameCBodyInstanceMember;
		oBMesh.gameObject.name = oBMesh._sNameCBodyInstanceMember;			//###DESIGN11:!!!  IMPROVE THIS CRAPPY Node name!!  Give Unity node the 'Blender node name (i.e. not path to instance variable string!)
        oBMesh._bKeepBlenderShare = bKeepBlenderShare;

        //=== Obtain the name of the Blender mesh object from the data-member of Blender's CBody class we're paired to ===
        string sCBodyBaseDataMember = oBodyBase._sBlenderInstancePath_CBodyBase + sNameCBodyInstanceMember;			// Fully-qualified path to the CBody data member currently pointing to the mesh we need.
		oBMesh._sNameBlenderMesh = CGame.gBL_SendCmd("CBody", sCBodyBaseDataMember + ".GetName()");                              // Store the name of the Blender object holding the mesh so we can directly access.
		oBMesh.OnDeserializeFromBlender(aExtraArgs);	// Fully deserialize the mesh from Blender passing extra arguments.  (Virtual call that cascades accross several classes in order)
        oBMesh.FinishIntialization();					//###DESIGN ###CHECK?

        return oBMesh;
	}

	public virtual void OnDeserializeFromBlender(params object[] aExtraArgs) {        //===== Very important top-level call to begin the process of deserializing a mesh from Blender.  Central to all our meshes! =====
		//=== Send blender command to obtain the mesh header.  This will return a large stream containing material names, bones, bone weight, etc ===
		CByteArray oBA = new CByteArray("'CBody'", _oBodyBase._sBlenderInstancePath_CBodyBase + _sNameCBodyInstanceMember + ".Unity_GetMesh()");

		//=== Create various casts to subclasses to facilitate deserializing info into the proper subclasses ===
		bool bCreatingSkinnedMesh = ((this as CBSkin) != null);     // We create a skinned mesh if we're a CBSkin or related rim... otherwise we're (probably) a body part requiring softbody/cloth simulation so we're just a simple mesh
		_bConnectedToBlenderMesh = true;							// Mark this mesh as having a valid C++ connection to Blender

		//=== Read the number of verts and tris.  Must be < 64K for Unity! ===
		int nVerts = oBA.ReadInt();
		int nTris  = oBA.ReadInt();
		byte nMats = oBA.ReadByte();
		Debug.Log("CBMesh obtains mesh '" + _sNameCBodyInstanceMember + "' for body '" + _oBodyBase._nBodyID + "' with " + nVerts + " verts " + nTris + " triangles and " + nMats + " materials.");
		
		if (nVerts > 65530)
			CUtility.ThrowExceptionF("ERROR in CBMesh.ctor().  Mesh has {0} verts  (over 64K)", nVerts);
	
		//=== Create the new mesh / skinned mesh now that we have the beginning of a valid stream ===
		MeshFilter   		oMeshFilter 	= null;
		MeshRenderer 		oMeshRenderer 	= null;
		if (bCreatingSkinnedMesh == false) {
			oMeshFilter 	= (MeshFilter)  CUtility.FindOrCreateComponent(transform, typeof(MeshFilter));
			oMeshRenderer	= (MeshRenderer)CUtility.FindOrCreateComponent(transform, typeof(MeshRenderer));
		}
		_oMeshNow = new Mesh();				// Create the actual mesh the object will use.  Will persist throughout...
		//_oMeshNow.MarkDynamic();			//###CHECK: Use by default or case-by-case basis in subclasses??    ###OPT!

        if (GetComponent<Collider>() != null)
            Destroy(GetComponent<Collider>());                      //###INFO: Hugely expensive mesh collider created by the above lines... turn it off!

		//===== READ THE MATERIALS =====
		//=== Read the material list from Blender and create the necessary materials linking to the provided texture images ===
		string sTextureSet = _oBodyBase._eBodySex == EBodySex.Man ? "Man" : "Woman";
		_aMatsCurrent		= new Material[nMats];
		string sPathTextures = "Textures/" + sTextureSet + "/A/";   //###DESIGN: Keep this way of getting the texture path?	//###TODO: Give the user the option of selecting the texture selection
		string sTextureSetSuffix = "_A";							//###TODO: Give the user the option of selecting the texture selection
		for (byte nMat = 0; nMat < nMats; nMat++) {
			string sNameMaterialAndTexture = oBA.ReadString();
			Material oMat = new Material(Shader.Find("Standard"));       // If material was not found (usual case) we just create a standard diffuse on
			Debug.Log("-Texture: " + sNameMaterialAndTexture);

			//=== Assign the mandatory diffuse texture ===
			string sPathTextureResource = sPathTextures + sNameMaterialAndTexture + sTextureSetSuffix;		//###HACK25:!!! Path info should NOT be in CBMesh!  ###MOVE:!!!!  ###DESIGN: How do we specify which texture set?  ###IMPROVE: Sex selection here?
			Texture oTex = Resources.Load(sPathTextureResource, typeof(Texture)) as Texture;	//###DEV19: Add normal if exists!		###HACK: Always adding .jpg... safe?  (e.g. always convert to jpg?)
			if (oTex != null) {
				oMat.name = sNameMaterialAndTexture;
				oMat.mainTexture = oTex;
			} else {
				Debug.LogError("**ERROR: Texture '" + sPathTextureResource + "' not found!");
			}

			//=== Attempt to find and set the optional normal texture ===
			sPathTextureResource = sPathTextures + sNameMaterialAndTexture + "N" + sTextureSetSuffix;      //###INFO: Normal map images have 'N' suffixed to their texture name
			oTex = Resources.Load(sPathTextureResource, typeof(Texture)) as Texture;
			if (oTex != null) {
				oMat.name = sNameMaterialAndTexture;		//###IMPROVE: Set the strenght of the normal map?  But at what value and where would that value come from or be edited?
				oMat.SetTexture("_BumpMap", oTex);			//###INFO: How to set a normal map into a material ('_BumpMap')
			} else {
				Debug.LogWarningFormat("Textures: Could not find a normal map for material '{0}' of body '{1}'", sNameMaterialAndTexture, _oBodyBase._sBodyPrefix);
			}
			_aMatsCurrent[nMat] = oMat;
		}

		if (nMats == 0) {					// If Blender didn't have a material for this mesh we now create a dummy one as we need at least one 'submesh' for DLL to pass in triangles to us ===
			nMats = 1;
			_aMatsCurrent       = new Material[nMats];
			_aMatsCurrent[0] = new Material(Shader.Find("Standard"));	// Create a default material so we can see the material-less Blender mesh in Unity
		}
		_oMeshNow.subMeshCount = nMats;
		if (bCreatingSkinnedMesh == false)
			oMeshRenderer.materials = _aMatsCurrent;

		oBA.CheckMagicNumber_End();                 // Check the 'end magic number' that always follows a stream.
		

		
		//===== CREATE THE MESH =====
		//=== Create the memory buffers that can extract the data from our C++ dll ===
		CMemAlloc<Vector3> memVerts 	= new CMemAlloc<Vector3>();
		CMemAlloc<Vector3> memNormals 	= new CMemAlloc<Vector3>();		//###DESIGN! ###OPT!! We don't use Blender's normals anymore... keep serializing??
		CMemAlloc<Vector2> memUVs 		= new CMemAlloc<Vector2>();
		memVerts  .Allocate(nVerts);
		memNormals.Allocate(nVerts);
		memUVs    .Allocate(nVerts);

		//=== With the header data process, the GetMeshHeader command also copied the large verts & tri arrays in shared memory for us to access quickly ===
		int nError = ErosEngine.gBL_GetMeshArrays(_sNameBlenderMesh, nMats, memVerts.P, memNormals.P, memUVs.P);
		if (nError != 0)
			CUtility.ThrowExceptionF("Exception in CBMesh.ctor().  gBL_GetMeshArrays('{0}') returns error {1} on mesh '{2}'.", _sNameBlenderMesh, nError, gameObject.name);

		_oMeshNow.vertices 	= memVerts.L;
		_oMeshNow.normals 	= memNormals.L;
		_oMeshNow.uv 		= memUVs.L;

		//=== Create separation of mesh by material ===
		for (int nMat = 0; nMat < nMats; nMat++) {
			int nTrisThisMat = ErosEngine.gBL_GetNumTrianglesAtMaterial(nMat);			//###HACK: These are 'last access' only!
			if (nTrisThisMat > 0) {								//###IMPROVE: Issue warning when no materials?  (So Blender mesh can clean up its unused materials)?
				CMemAlloc<int> memTris = new CMemAlloc<int>();
				memTris.Allocate(3 * nTrisThisMat);				// Number of triangles = 3 * number of triangles
				ErosEngine.gBL_GetTrianglesAtMaterial(nMat, memTris.P);
				_oMeshNow.SetTriangles(memTris.L, nMat);
			}
		}
		
		//=== Read the 'shared normals' flattened map ===
		_aMapSharedNormals = CByteArray.GetArray_USHORT("'CBody'", _oBodyBase._sBlenderInstancePath_CBodyBase + _sNameCBodyInstanceMember + ".aMapSharedNormals.Unity_GetBytes()");

		////=== Read the mesh normals.  We need them to traverse vert-splitting accross materials ===
		//if (nMats > 1) {			//###HACK25:!!!!!!! Only requesting normals on meshes with more than one material!  BAD!
		//	List<Vector3> aNormalsAsList	= CByteArray.GetArray_VECTOR3("'CBody'", _oBodyBase._sBlenderInstancePath_CBodyBase + _sNameCBodyInstanceMember + ".Unity_GetMeshNormals()");
		//	_aNormals = aNormalsAsList.ToArray();
		//}

		//=== Finalize the mesh creation by stuffing _oMeshRender into mesh filter or skinned mesh renderer as appropriate ===
		if (bCreatingSkinnedMesh == false) {		
			UpdateNormals();									// Fix the normals with the just-serialized map of shared normals
			_oMeshNow.RecalculateBounds();
			oMeshFilter.mesh = _oMeshNow;
		}

		_memVerts		.AssignAndPin(_oMeshNow.vertices);
		_memVertsStart	.AssignAndPin(_oMeshNow.vertices);
		_memNormals		.AssignAndPin(_oMeshNow.normals);
		_memTris		.AssignAndPin(_oMeshNow.triangles);

        //=== Mesh is loaded in Unity.  Close link to Blender to save memory unless maintaining the link is requested by base class ===
        if (_bKeepBlenderShare == false)
            ReleaseBlenderMesh();
    }

    public virtual void FinishIntialization() {
    }

    public virtual void OnDestroy() {
		Debug.Log("OnDestroy CBMesh " + gameObject.name);
        ReleaseBlenderMesh();
	}			
    public virtual void ReleaseBlenderMesh() {
        if (_bConnectedToBlenderMesh) {
            Debug.Log("CBMesh.ReleaseBlenderMesh() on " + gameObject.name);
            CGame.gBL_SendCmd("Client", "gBL_ReleaseMesh('" + _sNameBlenderMesh + "')");        // Tell Blender to stop sharing this mesh to free shared memory.
            _bConnectedToBlenderMesh = false;
        }
    }
 //   public virtual void UpdateVertsFromBlenderMesh(bool bUpdateNormals) {       // Ask Blender to update our copy of the verts.  Assumes the topology of the mesh hasn't changed!
 //       if (_bConnectedToBlenderMesh == false)       //###NOW### ###BROKEN??
 //           CUtility.ThrowException("Exception in CBMesh.UpdateVertsFromBlenderMesh().  Mesh is not exported / shared from Blender!");
 //       CGame.gBL_SendCmd("Client", "gBL_UpdateClientVerts('" + _sNameBlenderMesh + "')");

 //       //###BUG!!! (Fixed with hack)  For some reason we can't get an update on our own buffer!  WTF?  Hack is to create a temp pinned array, get updated results and manually copy array to its source = WTF crap!!
 //       //###IDEA: Is it possible it is because each vert is an object and we replaced these object's reference from their original pinned array?? (Verify this with pointer addresses!)
 //       //int nError = ErosEngine.gBL_UpdateClientVerts(_sNameBlenderMesh, _memVerts.P);
 //       //if (nError != 0)
	//	//	CUtility.ThrowExceptionF("Exception in CBMesh.gBL_UpdateClientVerts().  DLL returns error {0} on mesh '{0}'.", nError, gameObject.name);

 //       //###HACK: Create temporary pinned array of the same size, get updated results, manually copy to where the results should go!
 //       CMemAlloc<Vector3> memVertsCopy = new CMemAlloc<Vector3>(_memVerts.L.Length);
 //       int nError = ErosEngine.gBL_UpdateClientVerts(_sNameBlenderMesh, memVertsCopy.P);
	//	if (nError != 0)
	//		CUtility.ThrowExceptionF("Exception in CBMesh.gBL_UpdateClientVerts().  DLL returns error {0} on mesh '{0}'.", nError, gameObject.name);
 //       _memVerts.L = (Vector3[])memVertsCopy.L.Clone();      //###CHECK: Will screw up pin??  Copy each vert by value??
 //       memVertsCopy = null;

 //       _oMeshNow.MarkDynamic();		// Docs say "Call this before assigning vertices to get better performance when continually updating mesh"
	//	_oMeshNow.vertices = _memVerts.L;       //###CHECK: This ok/needed with next call??
 //       CopyOriginalVertsToVerts(true);
 //       if (bUpdateNormals)
	//		UpdateNormals();								// Verts have moved, update * fix normals...
	//}	

    //public void CopyOriginalVertsToVerts(bool bInvert) {
    //    if (bInvert) {      //###IMPROVE: Can make more efficient?  Study what is going on with references and deep copy failures!
    //        _memVertsStart.L = (Vector3[])_memVerts.L.Clone();
    //    } else {
    //        _memVerts.L = (Vector3[])_memVertsStart.L.Clone();
    //        _oMeshNow.vertices = (Vector3[])_memVerts.L;	//_memVertsStart.L.Clone();
    //    }
    //    //Array.Copy(_memVertsStart.L, _oMeshNow.vertices, GetNumVerts());        //###INFO: This DOES NOT WORK!  Why???  Array.Copy obviously does not do a deep copy  (Clone() always works!... a reference problem?)
    //    //for (int nVert = 0; nVert < GetNumVerts(); nVert++)                       // Old slow way to deep copy ALWAYS WORKS
    //    //    _oMeshNow.vertices[nVert] = _memVertsStart.L[nVert];					// Set the 'startup verts' to what Blender just provided.  (Blender is always authoritative)
    //}
    public virtual void UpdateVertsToBlenderMesh() {		// Ask Blender to update its copy of the verts
        if (_bConnectedToBlenderMesh == false)
            CUtility.ThrowException("Exception in CBMesh.UpdateVertsToBlenderMesh().  Mesh is not exported / shared from Blender!");

		//int nError = ErosEngine.gBL_UpdateBlenderVerts(_sNameBlenderMesh, _memVerts.P);
		//if (nError != 0)
		//	CUtility.ThrowException("Exception in CBMesh.gBL_UpdateBlenderVerts().  DLL returns error " + nError + " on mesh " + gameObject.name);

		//###HACK: Create temporary pinned array of the same size, send copy of updated results, manually copy to where the results should go!
		CMemAlloc<Vector3> memVertsCopy = new CMemAlloc<Vector3>(_memVerts.L.Length);
		memVertsCopy.L = (Vector3[])_memVerts.L.Clone();      //###CHECK: Will screw up pin??  Copy each vert by value??
		memVertsCopy.PinInMemory();			//###PROBLEM17: Dll would get only zeros without this line... Previous call to Pin too soon? ###BUG17: Memory leak??
		int nError = ErosEngine.gBL_UpdateBlenderVerts(_sNameBlenderMesh, memVertsCopy.P);
		if (nError != 0)
			CUtility.ThrowExceptionF("Exception in CBMesh.gBL_UpdateClientVerts().  DLL returns error {0} on mesh '{0}'.", nError, gameObject.name);
		memVertsCopy = null;
	}	

	public void UpdateNormals() {                       // Unity must split Blender's verts at the seam.  Blender provides the '' map for us to average out these normals in order to display seamlessly accross seams.  
		//_oMeshNow.normals = _aNormals;
		_oMeshNow.RecalculateNormals();					// First recalc the normals from Unity's implementations.  Duplicated verts at the seams will have slighly different normals because their polygons don't meet over seams.
		if (_aMapSharedNormals == null)					// If the map of shared normal info does not exist then there are no normals to fix so just RecalculateNormals() above is enough.
			return;

		//=== Manually iterate through the Blender-supplied flat list of 'shared normal groups' to set all verts that were split by seams to the same (average) normal so they appear seamless ===
		Vector3[] aNormals = _oMeshNow.normals;			// Obtain array for much faster iteration
		List<ushort> aSharedNormals_CurrentGroup = new List<ushort>();
		foreach (ushort nSharedNormal in _aMapSharedNormals) {
			if (nSharedNormal != G.C_MagicNo_EndOfFlatGroup) {		// Normal vertex in the current 'shared normal group'.  Just append to current group until we meet the 'end of group' magic number
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
		_oMeshNow.normals = aNormals;					// Stuff back the fixed normals into the mesh.  Unity will remember the normals for skinned meshes as it bends so (fortunately!) no need to call this every frame!!

	}

	
	//---------------------------------------------------------------------------	UTILITY

	public int GetNumVerts() 		{ return _oMeshNow.vertices.Length; }
	public int GetNumTris()  		{ return GetNumTriIndices() / 3; }
	public int GetNumTriIndices()  	{ return _oMeshNow.triangles.Length; }
}



////				CUtility.ThrowException("Exception in CBMesh.Create: Blender is sending skinning info on mesh '" + gameObject.name + "' but object INSTANCE is not derived from CBSkin!");




//sNameTexture = sNameTexture.Replace('\\', '/');						// Replace backslashes to the slash Unity requires.
//if (sNameTexture != "NoTexture") {            //###IMPROVE: What to do??  ###IMPROVE: Standarize constants between blender and Unity!

//	int nPosOfDot = sNameTexture.IndexOf(".");
//	if (nPosOfDot != -1)
//		sNameTexture = sNameTexture.Substring(0, nPosOfDot);        //###HACK19: To catch Blender's stupid automatic Material rename during imports!
//	//###TODO19:!!! Clean up all legacy textures and materials and make material and texture paths coherent!!
//	//string sPathMaterial = String.Format("Models/{0}/Materials/{1}", _oBodyBase._sMeshSource, sCodedMaterial);

//	//=== Obtain reference to previously-defined opaque material ===
//	string sPathMaterialO = String.Format("Models/(Tests)/Ana4-Bones/MaterialsO/{0}", sNameTexture);		//###HACK20:!!!!!! Redo this material shit when we go from Blender to Unity better
//	Material oMatO = Resources.Load(sPathMaterialO, typeof(Material)) as Material;	//###TODO19:!!!: Need to have Blender tell us which character it is so we look at right place??
//	if (oMatO == null) { 
//		Debug.LogWarningFormat("CBMesh.Create() could not find opaque material '{0}' on mesh '{1}'", sNameTexture, gameObject.name);
//		oMatO = new Material(Shader.Find("Standard"));				// Give a standard material with no texture (anything to avoid shadeless hot pink!)
//	}
//	_aMatsCurrent[nMat] = _aMatsOpaque[nMat] = oMatO;
//	//###OBS:!!!  Transparency / opacity on demand very ineficient.  Can find a better way then to have duplicate material sets?
//	//=== Obtain reference to previously-defined transparent material ===			###INFO: Could never find a way to programmatically turn a given material to / from transparency.  Doing things properly at design time is safest / easiest and we get to tune the params in editor anyway.  //###INFO: Procedure at https://docs.unity3d.com/Manual/MaterialsAccessingViaScript.html might help.  Tried but no luck
//	string sPathMaterialT = String.Format("Models/(Tests)/Ana4-Bones/MaterialsT/{0}", sNameTexture);
//	Material oMatT = Resources.Load(sPathMaterialT, typeof(Material)) as Material;		//###TODO: Check if there!!!
//	oMatT.EnableKeyword("_ALPHAPREMULTIPLY_ON");             //###INFO: Standard shader actually has tens of thousands (!) of variants!  Unity needs to know which variant to pick in regards to transparency with this call  (See https://docs.unity3d.com/Manual/MaterialsAccessingViaScript.html)
//	oMatT.SetFloat("_Mode", 3);                              //###INFO: How to set standard shader transparency.  (0 = Opaque, 1 = Cutout, 3 = Fade, 4 = Transparent)
//	oMatT.color = new Color(1, 1, 1, 0.25f);
//	if (oMatT == null) { 
//		Debug.LogWarningFormat("CBMesh.Create() could not find transparent material '{0}' on mesh '{1}'", sNameTexture, gameObject.name);
//		oMatT = new Material(Shader.Find("Standard"));
//	}
//	_aMatsTransparent[nMat] = oMatT;

//if (sCodedMaterial.StartsWith("Material_")) {				 // Blender material that start with this string are processed differently: We attempt to find the same name material here and attach to this submesh.

//	Material oMat = Resources.Load("Materials/" + sCodedMaterial, typeof(Material)) as Material;
//	if (oMat != null)
//		_aMats[nMat] = oMat;
//	else
//		Debug.LogWarning("ERROR: Unknown special material '" + sCodedMaterial + "' in CBMesh.ctor()");

//} else {

//	int nLocTexPath = sCodedMaterial.IndexOf(CGame.C_RelPath_Textures);		// Blender gives a fully-qualified path that MUST point to our Resources folder
//	if (nLocTexPath != -1) {
//		string sTextureResourcePath = sCodedMaterial.Substring(nLocTexPath + CGame.C_RelPath_Textures.Length);
//		int nLocExtDot = sTextureResourcePath.IndexOf('.');			// Strip the extension... ###CHECK: Assume jpg throughout??
//		if (nLocExtDot != -1)
//			sTextureResourcePath = sTextureResourcePath.Substring(0, nLocExtDot);

//		Material oMat = Resources.Load(sTextureResourcePath + "_Mat", typeof(Material)) as Material;		// We attempt to load the pre-defined material with _Mat suffix if its there...
//		if (oMat == null) {
//			if (sTextureResourcePath.EndsWith("_Transp"))			// If texture ends with this string we create a standard transparent material
//				oMat = new Material(Shader.Find("Transparent/Diffuse"));
//			else
//				oMat = new Material(Shader.Find("Standard"));		// If material was not found (usual case) we just create a standard diffuse on
//			//Debug.Log("-Texture: " + sTextureResourcePath);
//			object o = Resources.Load(sTextureResourcePath, typeof(Texture));		//###DEV19: Add normal if exists!
//			Texture oTex = o as Texture;
//			if (oTex != null) {
//				int nLocLastSlash = sTextureResourcePath.LastIndexOf('/');
//				oMat.name = sTextureResourcePath.Substring(nLocLastSlash + 1);
//				oMat.mainTexture = oTex;
//			} else {
//				Debug.LogError("**ERROR: Texture '" + sTextureResourcePath + "' not found!");
//			}
//		} else {
//			//Debug.Log("-Material: " + sTextureResourcePath);			// If material was defined we leave it to material designer to connect whatever texture...
//		}
//		_aMats[nMat] = oMat;
//	} else {
//		Debug.LogError("ERROR: in CBMesh.ctor().  Could not find Unity resource path in texture path " + sCodedMaterial);
//	}
//}
//} else {		// Blender gave us a 'NoTexture' -> Assign a default material
//	_aMatsCurrent[nMat] = new Material(Shader.Find("Standard"));		//###WEAK:!! Revise this default material crap
//	_aMatsOpaque[nMat] = new Material(Shader.Find("Standard"));	// Create a default material so we can see the material-less Blender mesh in Unity
//	_aMatsTransparent[nMat] = new Material(Shader.Find("Standard"));	//###DESIGN: What to do??
//}










//public void GetBlenderSerializableCollection(string sNameArray, ref List<ushort> aBlenderArray) {		// Deserialize a Blender mesh's previously-created array		//####DEV: As array too?
//	CMemAlloc<byte> memBA = new CMemAlloc<byte>();
//	CGame.gBL_SendCmd_GetMemBuffer("'Client'", "Unity_GetMesh_Array(sNameMesh='" + _sNameBlenderMesh + "', sNameArray='" + sNameArray + "')", ref memBA);

//	byte[] oBA = (byte[])memBA.L;					// Obtain byte array and set read position to zero
//	int nPosBA = 0;

//	CheckMagicNumber(ref oBA, ref nPosBA, false);				// Read the 'beginning magic number' that always precedes a stream.
//	int nArrayElements = BitConverter.ToInt32(oBA, nPosBA) / 2; nPosBA+=4;				// Unity_GetMeshArray always returns the byte-length of the serialized stream as the first 4 bytes
//	if (nArrayElements > 0) {
//		aBlenderArray = new List<ushort>();
//		for (int nArrayElement = 0; nArrayElement  < nArrayElements; nArrayElement++) {
//			aBlenderArray.Add(BitConverter.ToUInt16(oBA, nPosBA)); nPosBA+=2;
//		}
//	}
//	CheckMagicNumber(ref oBA, ref nPosBA, true);				// Read the 'end magic number' that always follows a stream.
//}





//[HideInInspector]	public	Material[]		_aMatsOpaque;					// Array of opaque materials created from Blender info
//[HideInInspector]	public	Material[]		_aMatsTransparent;				// Array of transparent materials created from Blender info.  Needed when we want to go transparent
