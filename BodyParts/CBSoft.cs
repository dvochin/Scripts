/*###DISCUSSION: Soft Body / PhysX
=== NEXT ===
- For character edit mode have a mode without softbody entity?  (Just for morph?)
 
=== TODO ===

=== LATER ===

=== IMPROVE ===

=== DESIGN ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===
 * GPU on softbody not working!! WTF???  Has to be offline?  Used to work!  ####SOON

=== PROBLEMS??? ===

=== WISHLIST ===

*/

using UnityEngine;
using System;
using System.Collections.Generic;


public class CBSoft : CBMesh, IObject {					//####DEV ####DESIGN: Based on CBMesh or CBSkin??
	// Manages a single soft body object send to our PhysX implementation for soft body simulation.  These 'body parts' (such as breasts, penis, vagina) 
	//... are conneted to the main body skinned mesh via _oMeshRimBaked which pins this softbody's tetraverts to those skinned from the main body

	//---------------------------------------------------------------------------	MEMBERS
	[HideInInspector]	public 	CObject				_oObj;							// The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.
	[HideInInspector]	public	CBSkinBaked			_oMeshRimBaked;					// The skinned 'rim mesh' that is baked everyframe.  Contains rim and tetraverts.  Rim is to adjust normals at softbody mesh boundary and the tetraverts in this mesh are to 'pin' our softbody tetraverts to the skinned body (so softbody doesn't go 'flying off')
	
	[HideInInspector]	public	List<ushort>		_aMapRimVerts2Verts = new List<ushort>();		// Collection of mapping between our verts and the verts of our BodyRim.  Used to set softbody mesh rim verts and normals to their skinned-equivalent
	[HideInInspector]	public	List<ushort>		_aMapRimTetravert2Tetravert;	// Map of rim tetraverts to tetraverts.  Used to translate between our 'rim + close tetraverts' to PhysX2 softbody tetraverts. (To pin some tetraverts to skinned body)
	[HideInInspector]	public	List<ushort>		_aMapRimVerts2SourceVerts;		// Map of flattened rim vert IDs to source vert IDs.  Allows Unity to reset rim vert normals messed-up by capping to default normal for seamless rendering
	
	//---------------------------------------------------------------------------	PhysX-related properties sent during BSoft_Init()
	[HideInInspector]	public	string				_sNameSoftBody;					// The name of our 'detached softbody' in Blender.  ('BreastL', 'BreastR', 'Penis', 'VaginaL', 'VaginaR') from a substring of our class name.  Must match Blender!!
	[HideInInspector]	public  int					_SoftBodyDetailLevel;			// Detail level of the associated PhysX tetramesh... a range between 20 (low) and 50 (very high) is reasonable
	[HideInInspector]	public	EColGroups			_eColGroup;						// The PhysX collider group for this softbody.  Used to properly determine what this softbody collides with...
	[HideInInspector]	public	float				_nRangeTetraPinHunt = 0.025f;	// The maximum distance between the rim mesh and the tetraverts generated by PhysX2.  Determins which softbody tetraverts are 'pinned' to the skinned body

	//---------------------------------------------------------------------------	MISC
	[HideInInspector]	public	string				_sBlenderInstancePath_CSoftBody;				// The Blender instance path where our corresponding CSoftBody is access from CBody's Blender instance
	[HideInInspector]	public	string				_sBlenderInstancePath_CSoftBody_FullyQualfied;	// The Blender instance path where our corresponding CSoftBody is access from CBody's Blender instance (Fully qualified (includes CBody access string)
	[HideInInspector]	public	CBMesh				_oMesh_Unity2Blender;							// The Unity2Blender mesh we use to pass meshes from Unity to Blender for processing there (e.g. Softbody tetramesh skinning & pinning)



	//---------------------------------------------------------------------------	INIT

	public CBSoft() {                           // Setup the default arguments... usually overriden by our derived class   //###BUG??? Why are these settings not overriding those in instanced node???
		_nRangeTetraPinHunt = 0.025f;
	}

	public static CBSoft Create(CBody oBody, Type oTypeBMesh) { 
		string sNameSoftBody = oTypeBMesh.Name.Substring(1);                            // Obtain the name of our detached body part ('Breasts', 'Penis', 'Vagina') from a substring of our class name.  Must match Blender!!  ###WEAK?
		String sIsBreast = oTypeBMesh.Name.StartsWith("CBreast") ?  "True" : "False";	// Blender needs to know what type of soft body we're creating ###IMPROVE: Pass 'a type of' instead?
		CGame.gBL_SendCmd("CBody", "CBody_GetBody(" + oBody._nBodyID.ToString() + ").CreateSoftBody('" + sNameSoftBody + "', " + sIsBreast + ")");		// Separate the softbody from the source body.
		CBSoft oBSoft = (CBSoft)CBMesh.Create(null, oBody, "aSoftBodies['" + sNameSoftBody + "'].oMeshSoftBody", oTypeBMesh);		// Create the softbody mesh from the just-created Blender mesh.
		return oBSoft;
	}

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();

		_sNameSoftBody = GetType().Name.Substring(1);                            // Obtain the name of our detached body part ('Breasts', 'Penis', 'Vagina') from a substring of our class name.  Must match Blender!!  ###WEAK?
		_sBlenderInstancePath_CSoftBody = "aSoftBodies['" + _sNameSoftBody + "']";							// Simplify access to Blender CSoftBody instance
		_sBlenderInstancePath_CSoftBody_FullyQualfied = _oBody._sBlenderInstancePath_CBody + "." + _sBlenderInstancePath_CSoftBody;	// Simplify access to fully-qualified Blender CSoftBody instance (from CBody instance)

		if (GetComponent<Collider>() != null)
			Destroy(GetComponent<Collider>());                      //###LEARN: Hugely expensive mesh collider created by the above lines... turn it off!

		//=== Set bounds to infinite so our dynamically-created mesh never has to recalculate bounds ===
		_oMeshNow.bounds = CGame._oBoundsInfinite;          //####IMPROVE: This can hurt performance ####OPT!!
		_oMeshNow.MarkDynamic();        // Docs say "Call this before assigning vertices to get better performance when continually updating mesh"

		//=== Create the Unity2Blender mesh so we can pass tetraverts to Blender for processing there ===
		_oMesh_Unity2Blender = CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshUnity2Blender", typeof(CBMesh));       // Also obtain the Unity2Blender mesh call above created.
	}



	public override void OnDestroy() {
		Debug.Log("Destroy CBSoft " + gameObject.name);
		ErosEngine.SoftBody_Destroy(_oObj._hObject);		//###CHECK: Everything destroyed?  Actors, colliders, etc??
		base.OnDestroy();
	}
	public virtual void OnChangeGameMode(EGameModes eGameModeNew, EGameModes eGameModeOld) {		//###DEV

		switch (eGameModeNew) { 
			case EGameModes.Play:
				//=== Call our C++ side to construct the solid tetra mesh.  We need that to assign tetrapins ===		//###DESIGN!: Major design problem between cutter sent here... can cut cloth too??  (Will have to redesign cutter on C++ side for this problem!)
				//###DEV ###DESIGN: Recreate public properties each time???
				_oObj = new CObject(this, 0, typeof(ESoftBody), _sNameSoftBody);        //###IMPROVE: Name of soft body to GUI
				_oObj._hObject = ErosEngine.SoftBody_Create(_oObj._sNameObject, _oObj.GetNumProps(), _memVerts.P, _memVerts.L.Length, _memTris.P, _memTris.L.Length / 3, _memNormals.P, _SoftBodyDetailLevel, .01f, false, false, (int)_eColGroup); //###DESIGN: Density??  ###OBS??
				_oObj.PropGroupBegin("", "", true);
				_oObj.PropAdd(ESoftBody.VolumeStiffness,		"Volume Stiffness",			0.9f,	0.01f,		1, "");   //###CHECK: Can go to zero??
				_oObj.PropAdd(ESoftBody.StretchingStiffness,	"Stretching Stiffness",		0.5f,	0.01f,		1, "");
				_oObj.PropAdd(ESoftBody.SoftBody_Damping,		"Damping Coefficient",		0,		0,			1, "");
				_oObj.PropAdd(ESoftBody.Friction,				"Friction",					0,		0,			1, "");
				_oObj.PropAdd(ESoftBody.SoftBody_Gravity,		"Local Gravity",			-0.5f,	-2,			2, "");
				_oObj.PropAdd(ESoftBody.ParticleRadius,			"Particle Radius",			0.015f, 0.001f,		0.030f, ""); //###TUNE!!!! ###TODO! ###DESIGN!! Override by each softbody??
				_oObj.PropAdd(ESoftBody.SolverIterations,		"Solver Iterations",		1,		1,			6, "Number of times PhysX iterates over this soft body's tetrahedra solid elements per frame.");       //###DESIGN!!! ###TUNE!!!!
				_oObj.PropAdd(ESoftBody.SoftBody_GPU,			"GPU",						1,		"",			CProp.AsCheckbox);  //###TODO!!!: Connect to global settings
				_oObj.FinishInitialization();
				ErosEngine.Object_GoOnline(_oObj._hObject, IntPtr.Zero);

				//=== Fill in the PhysX2 tetraverts into our 'Unity2Blender' mesh so it can quickly skin and pin the appropriate verts ===
				int nVertTetras = ErosEngine.SoftBody_GetTetraVertCount(_oObj._hObject);
				if (nVertTetras > CBody.C_Unity2Blender_MaxVerts)			// Unity to Blender mesh created at a fixed size with the max number of verts we're expecting.  Check if we're within our set limit
					throw new Exception("ERROR in CBSoft.Init()  More tetraverts than # of verts in Unity2Blender mesh!");					

				//=== Upload our tetraverts to Blender so it can select those that are pinned and skin them ===
				for (int nVertTetra = 0; nVertTetra < nVertTetras; nVertTetra++)
					_oMesh_Unity2Blender._memVerts.L[nVertTetra] = ErosEngine.SoftBody_GetTetraVert(_oObj._hObject, nVertTetra);
				_oMesh_Unity2Blender.UpdateVertsToBlenderMesh();				// Blender now has our tetraverts.  It can now find the tetraverts near the rim and skin them

				//=== Create and retrieve the softbody rim mesh responsible to pin softbody to skinned body ===
				CGame.gBL_SendCmd("CBody", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".ProcessTetraVerts(" + nVertTetras.ToString() + ", " + _nRangeTetraPinHunt.ToString() + ")");		// Ask Blender select the tetraverts near the rim and skin them
				_oMeshRimBaked = (CBSkinBaked)CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshSoftBodyRim", typeof(CBSkinBaked));           // Retrieve the skinned softbody rim mesh Blender just created so we can pin softbody at runtime

				//=== Receive the important 'CSoftBody.aMapRimTetravert2Tetravert' and 'CSoftBody.aMapTwinVerts' array Blender has prepared for softbody-connection to skinned mesh.  (to map the softbody edge vertices to the skinned-body vertices they should attach to)
				CUtility.BlenderSerialize_GetSerializableCollection("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aMapRimTetravert2Tetravert()",	out _aMapRimTetravert2Tetravert);		// Read the tetravert traversal map from our CSoftBody instance
				CUtility.BlenderSerialize_GetSerializableCollection("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aMapRimVerts2Verts()",			out _aMapRimVerts2Verts);				// Read the twin-vert traversal map from our CSoftBody instance
				
				//=== Bake the rim tetramesh a first time so its rim and tetraverts are updated to its skinned body ===
				_oMeshRimBaked.Baking_UpdateBakedMesh();                                        
				break;

			case EGameModes.Configure:
				if (_oObj != null) {
					ErosEngine.Object_GoOffline(_oObj._hObject);
					ErosEngine.SoftBody_Destroy(_oObj._hObject);
					_oObj = null;
				}
				if (_oMeshRimBaked != null)
					Destroy(_oMeshRimBaked.gameObject);
				_oMeshRimBaked = null;
				_aMapRimVerts2Verts = null;

				//=== Capped softbodies have messed up normals due to capping.  Blender constructed for us a map of which rim verts map to which source verts.  Reset the rim normals to the corresponding source vert normal for seamless rendering ===
				CUtility.BlenderSerialize_GetSerializableCollection("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aMapRimVerts2SourceVerts()",	out _aMapRimVerts2SourceVerts);
				for (int nIndex = 0; nIndex < _aMapRimVerts2SourceVerts.Count;) {         // Iterate through the flattened map...
					int nVertID			= _aMapRimVerts2SourceVerts[nIndex++];            // The simple list has been flattened into <nVertID0, nVertSourceID0>, etc...
					int nVertSourceID   = _aMapRimVerts2SourceVerts[nIndex++];
					_memNormals.L[nVertID] = _oBody._oBodySource._memNormals.L[nVertSourceID];
				}
				_oMeshNow.normals   = _memNormals.L;

				//=== Restore Blender's mesh and our visible mesh to the way it was during first creation (e.g. no softbody movement) ===
				//_oMeshNow.vertices  = _memVerts.L = _memVertsStart.L;				//###LEARN: This call doesn't do it! (copy whole array)  Must copy each vert for _memVerts to really be set to _memVertsStart...
				for (int nVert = 0; nVert < GetNumVerts(); nVert++)					//###IMPROVE: A better way to 'deep copy'??
					_memVerts.L[nVert] = _memVertsStart.L[nVert];
				_oMeshNow.vertices  = _memVerts.L;
//				UpdateVertsToBlenderMesh();			//###CHECK: Needed??
				break;
		}
	}
	
	//---------------------------------------------------------------------------	UPDATE
	
	public virtual void OnSimulatePre() {
		if (Input.GetKeyDown(KeyCode.F10))			//####TEMP			####OBS ####CLEAN
			UpdateVertsFromBlenderMesh(false);

		switch (CGame.INSTANCE._GameMode) {
			case EGameModes.Play:
				_oMeshRimBaked.Baking_UpdateBakedMesh();                                        // Bake the rim tetramesh so its rim and tetraverts are updated to its skinned body.
				Vector3[] aVertsRimBaked = _oMeshRimBaked._oMeshBaked.vertices;		//###LEARN!!!!!: Absolutely IMPERATIVE to obtain whole array before loop like the one below... with individual access profiler reveals 7ms per frame if not!!
				for (int nIndex = 0; nIndex < _aMapRimTetravert2Tetravert.Count; ) {			// Iterate through the rim tetraverts to update the position of their corresponding tetraverts
					ushort nRimTetravert	= _aMapRimTetravert2Tetravert[nIndex++];			// The simple list has been flattened into <nRimTetravert0, nTetravert0>, <nRimTetravert1, nTetravert1>, etc...
					ushort nTetravert		= _aMapRimTetravert2Tetravert[nIndex++];
					ErosEngine.PinTetra_AttachTetraVertToPos(_oObj._hObject, nTetravert, aVertsRimBaked[nRimTetravert]);				// If we're a simple fix we just update to the latest position
				}
				break;
			case EGameModes.Configure:
				break;
		}
	}

	public virtual void OnSimulateBetweenPhysX23() {}

	public virtual void OnSimulatePost() {
		switch (CGame.INSTANCE._GameMode) {
			case EGameModes.Play:       //=== Update the position and normals of the softbody mesh rim vertices to their equivalent on baked skinned rim mesh.  (This prevents gaps in the two meshes and aligns normals so shading is ok accross the two meshes) ===
				Vector3[] aVertsRimBaked   = _oMeshRimBaked._oMeshBaked.vertices;       //###LEARN!!!!!: Absolutely IMPERATIVE to obtain whole array before loop like the one below... with individual access profiler reveals 7ms per frame if not!!!
				Vector3[] aNormalsRimBaked = _oMeshRimBaked._oMeshBaked.normals;
				for (int nIndex = 0; nIndex < _aMapRimVerts2Verts.Count;) {         // Iterate through the twin vert flattened map...
					ushort nVertMesh    = _aMapRimVerts2Verts[nIndex++];            // The simple list has been flattened into <nVertMesh0, nVertRim0>, <nVertMesh1, nVertRim1>, etc
					ushort nVertRim     = _aMapRimVerts2Verts[nIndex++];
					_memVerts.L[nVertMesh] = aVertsRimBaked[nVertRim];
					_memNormals.L[nVertMesh] = aNormalsRimBaked[nVertRim];
				}
				_oMeshNow.vertices  = _memVerts.L;
				_oMeshNow.normals   = _memNormals.L;
				break;
			case EGameModes.Configure:
				break;
		}
	}
			
	public void OnPropSet_NeedReset(CProp oProp, float nValueOld, float nValueNew) { }
}
