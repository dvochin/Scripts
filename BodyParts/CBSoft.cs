/*###DISCUSSION: Soft Body / PhysX
=== NEXT ===
 * Cleanup old non-CProp properties

=== TODO ===
 * Test shutdown on all objects

=== LATER ===

=== IMPROVE ===

=== DESIGN ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===
 * GPU on softbody not working!! WTF???  Has to be offline?  Used to work!

=== PROBLEMS??? ===

=== WISHLIST ===
 * Give PhysX global entity its own controllable CObject??

*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class CBSoft : CBSkin, IObject {	// Manages a single soft body object send to our PhysX implementation for soft body simulation.  These 'body parts' (such as breasts with associated clothing, penises, vaginas) 
								//... are conneted to our 'CBSkinBaked' which can be quickly queried for the position of each vertex this softbody part connects to.
	
	//---------------------------------------------------------------------------	MEMBERS
	[HideInInspector]	public 	CObject				_oObj;						// The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.
	[HideInInspector] 	public 	CPin				_oPinGroup_Part;			// Our pin group for the part.   Our parent is CBody._oPinGroup_Rim and our children are CPinSkinned
	
	[HideInInspector]	public	List<CMapTwinVert>	_aMapTwinVerts = new List<CMapTwinVert>();		// Collection of mapping between our verts and the verts of our BodyRim.  Used to create CPinSkinned
	[HideInInspector]	public 	List<CPinSkinned>	_aPinSkinned;				// Our collection of CPinSkinned.  Stored for fast per-frame processing
	
	//---------------------------------------------------------------------------	PhysX-related properties sent during BSoft_Init()
						public  int					_SoftBodyDetailLevel;			// Detail level of the associated PhysX tetramesh... a range between 20 (low) and 50 (very high) is reasonable
	[HideInInspector]	public	float				_nRangeTetraPinHunt;
	[HideInInspector]	public	EColGroups			_eColGroup;				// The PhysX collider group for this softbody.  Used to properly determine what this softbody collides with...

	//---------------------------------------------------------------------------	MISC
						public const float 			C_DistBSoftMesh2SkinnedMesh = 0.001f;               // Maximum allowed distance between a vert on a softbody and the nearby vert on skinned-side for them to be considered 'joined'  ###WEAK: Made larger because of right breast!

	public bool _bDisableSoftBodySimulation_HACK = false;


	//---------------------------------------------------------------------------	INIT

	public CBSoft() {							// Setup the default arguments... usually overriden by our derived class   //###BUG??? Why are these settings not overriding those in instanced node???
		_nRangeTetraPinHunt		= 0.03f;
	}

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();

		////=== Receive the important 'aMapTwinVerts' array Blender has prepared for softbody-connection to skinned mesh.  (to map the softbody edge vertices to the skinned-body vertices they should attach to.)  Only present on softbody Blender meshes!
		//int nTwinVerts = BitConverter.ToInt32(oBA, nPosBA) / 6; nPosBA += 4;    // Number of twin vert definitions is divided by serialized lenght per defintion (three members of 2-byte = 6 bytes)
		//if (nTwinVerts > 0) {
		//	List<CMapTwinVert> aMapTwinVerts;
		//	if (oBSoft != null)
		//		aMapTwinVerts = oBSoft._aMapTwinVerts;
		//	else
		//		throw new CException("Error in CBMesh.ctor(): Receiving a aMapTwinVerts array but INSTANCE was not of type CBSoft or CBCloth!");

		//	for (int nTwinVert = 0; nTwinVert < nTwinVerts; nTwinVert++) {
		//		CMapTwinVert oMapTwinVert = new CMapTwinVert();
		//		oMapTwinVert.nVertPart = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;
		//		oMapTwinVert.nVertHost = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;
		//		oMapTwinVert.nVertHostAdj = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;
		//		aMapTwinVerts.Add(oMapTwinVert);
		//	}
		//}
		//ReadEndMagicNumber(ref oBA, ref nPosBA);

		_oObj = new CObject(this, 0, typeof(ESoftBody), GetType().Name);		//###IMPROVE: Name of soft body to GUI

		_oMeshNow.MarkDynamic();		// Docs say "Call this before assigning vertices to get better performance when continually updating mesh"

		if (GetComponent<Collider>() != null)
			Destroy(GetComponent<Collider>());                      //###LEARN: Hugely expensive mesh collider created by the above lines... turn it off!

		_oPinGroup_Part = CPin.CreatePin(_oBody._oBSkinRim._oPinGroup_Rim, "Part:" + transform.name, "Pin");    // Create our pin group and shut off debug rendering

		_aPinSkinned = new List<CPinSkinned>();

		foreach (CMapTwinVert oMapTwinVert in _aMapTwinVerts) {
			CPinSkinned oPinSkinned = CPinSkinned.CreatePinSkinned(_oPinGroup_Part, _oBody._oBSkinRim, oMapTwinVert.nVertPart, oMapTwinVert.nVertHost, oMapTwinVert.nVertHostAdj);
			_aPinSkinned.Add(oPinSkinned);
		}

		//=== Call our C++ side to construct the solid tetra mesh.  We need that to assign tetrapins ===		//###DESIGN!: Major design problem between cutter sent here... can cut cloth too??  (Will have to redesign cutter on C++ side for this problem!)
		_oObj._hObject = ErosEngine.SoftBody_Create(_oObj._sNameObject, _oObj.GetNumProps(), _memVerts.P, _memVerts.L.Length, _memTris.P, _memTris.L.Length / 3, _memNormals.P, _SoftBodyDetailLevel, .01f, false, false, (int)_eColGroup); //###DESIGN: Density??  ###OBS??

		_oObj.PropGroupBegin("", "", true);
		_oObj.PropAdd(ESoftBody.VolumeStiffness,		"Volume Stiffness",			0.9f,	0.01f,	1,		"");	//###CHECK: Can go to zero??
		_oObj.PropAdd(ESoftBody.StretchingStiffness,	"Stretching Stiffness",		0.5f,	0.01f,	1,		"");
		_oObj.PropAdd(ESoftBody.SoftBody_Damping,		"Damping Coefficient",		0,		0,		1,		"");
		_oObj.PropAdd(ESoftBody.Friction,				"Friction",					0,		0,		1,		"");
		_oObj.PropAdd(ESoftBody.SoftBody_Gravity,		"Local Gravity",			-0.5f,	-2,		2,		"");
		_oObj.PropAdd(ESoftBody.ParticleRadius,			"Particle Radius",			0.015f,	0.001f,	0.030f,	"");	//###TUNE!!!! ###TODO! ###DESIGN!! Override by each softbody??
		_oObj.PropAdd(ESoftBody.SolverIterations,		"Solver Iterations",		1,		1,		6,		"Number of times PhysX iterates over this soft body's tetrahedra solid elements per frame.");		//###DESIGN!!! ###TUNE!!!!
		_oObj.PropAdd(ESoftBody.SoftBody_GPU,			"GPU",						1,		"", CProp.AsCheckbox);	//###TODO!!!: Connect to global settings
		_oObj.FinishInitialization();

		ErosEngine.Object_GoOnline(_oObj._hObject, IntPtr.Zero);

		//=== Now that PhysX has constructed the tetramesh, we can complete the initialization by connecting our skinned pins to tetrahedron pins so the softbody solid is moved by our skinned mesh host ===
		ConnectPinsToTetraMesh();

		//=== Set bounds to infinite so our dynamically-created mesh never has to recalculate bounds ===
		_oMeshNow.bounds = CGame._oBoundsInfinite;
	}

	public override void OnDestroy() {
		Debug.Log("Destroy CBSoft " + gameObject.name);
		ErosEngine.SoftBody_Destroy(_oObj._hObject);		//###CHECK: Everything destroyed?  Pins, colliders, etc?
		base.OnDestroy();
	}
	
	//---------------------------------------------------------------------------	UPDATE
	
	public override void OnSimulatePre() {
		if (_bDisableSoftBodySimulation_HACK)
			return;
		foreach (CPinSkinned oPinSkinned in _aPinSkinned)       // Update the position of all our CPinSkinned  so that PhysX receives the location of our pins for the upcoming simulation step
			oPinSkinned.OnSimulatePre();
	}

	public virtual void OnSimulateBetweenPhysX23() {}

	public virtual void OnSimulatePost() {
		if (_bDisableSoftBodySimulation_HACK) {
			if (Input.GetKeyDown(KeyCode.F12))			//####TEMP
				UpdateVertsFromBlenderMesh(false);
			if (Input.GetKeyDown(KeyCode.F11))			//####TEMP
				_oMeshNow.vertices  = _memVertsStart.L;			//####DEV: Just show the start vertices when not simulated
		} else {
			foreach (CPinSkinned oPinSkinned in _aPinSkinned) {			// Overwrite the position and normals of all pin verts
				_memVerts  .L[oPinSkinned._nVertPart] = oPinSkinned.transform.position;
				_memNormals.L[oPinSkinned._nVertPart] = oPinSkinned._vecNormal;
			}
			_oMeshNow.vertices  = _memVerts.L;			//###OPT: Necessary to copy to array too?
			_oMeshNow.normals   = _memNormals.L;
		}
	}

		
	
	//---------------------------------------------------------------------------	IMPLEMENTATION
	
	public void ConnectPinsToTetraMesh() {		//=== With the C++ side having prepared a valid tetramesh from the solid mesh we sent in, Iterate through the skinned mesh pins to create the child tetra pins we control as a group (though a Phys shape linked to CPinMesh that moves all child CPinTetra) ===
		Hashtable mapVertTetraToClosestPinSkinned = new Hashtable();		// Maps tetra pin index to index of closest mesh pin
		Hashtable mapVertTetraMinDistance = new Hashtable();			// Stores the distance during next loop to find closest mesh pin to any tetra pin.
		
		//=== Iterate through the tetra verts of the PhysX-generated tetravertex solid to find those that are within a close distance to our CPinSkinned.
		int nVertTetras = ErosEngine.SoftBody_GetTetraVertCount(_oObj._hObject);
		foreach (CPinSkinned oPinSkinned in _aPinSkinned) {										//###OPT!!!: Very slow exhaustive-search algorithm!! REDO!
			Vector3 vecPinSkinnedVert =  oPinSkinned.transform.position;				// Everything related to PhysX is in global space!
			for (int nVertTetra = 0; nVertTetra < nVertTetras; nVertTetra++) {
				Vector3 oVertTetraG = ErosEngine.SoftBody_GetTetraVert(_oObj._hObject, nVertTetra);	// Everything related to PhysX is in global space!
				Vector3 vecDiff = oVertTetraG - vecPinSkinnedVert;
				float nDist = vecDiff.magnitude;
				if (nDist < _nRangeTetraPinHunt) {						//###WEAK //###CHECK: Design flaw in that large tris like breasts require bigger while small ones like vagina much smaller!!!
					if (mapVertTetraToClosestPinSkinned.Contains(nVertTetra) == false || ((float)mapVertTetraMinDistance[nVertTetra]) > nDist) {
						mapVertTetraToClosestPinSkinned[nVertTetra] = oPinSkinned;
						mapVertTetraMinDistance[nVertTetra] = nDist;
					}
				}
			}
		}

		//=== Iterate through the maps that identified the closest links and link tetra vertex with its master skinned pin ===
		foreach (DictionaryEntry oDict in mapVertTetraToClosestPinSkinned) {
			int nVertTetra = (int)oDict.Key;
			CPinSkinned oPinSkinned = (CPinSkinned)oDict.Value;
			Vector3 oVertTetraG = ErosEngine.SoftBody_GetTetraVert(_oObj._hObject, nVertTetra);		// Everything related to PhysX is in global space!
			CPinTetra.CreatePinTetra(oPinSkinned, this, nVertTetra, oVertTetraG, 0);
		}

		//###NOTE: We can't remove the skinned pins without tetra pins as the normals won't be updated!  (Can be improved for performance??)
		////=== Remove the skinned pins that have no tetra pins to drive ===
		//for (int nPin = _aPinSkinned.Count - 1; nPin >= 0; nPin--) {
		//	CPinSkinned oPinSkinned = _aPinSkinned[nPin];
		//	if (oPinSkinned.transform.childCount == 0) {
		//		_aPinSkinned.RemoveAt(nPin);
		//		GameObject.Destroy(oPinSkinned.gameObject);
		//	}
		//}
	}
		
	public Transform GetPackageRoot() { return transform.parent.parent; }		// CBSoft node exists under <PackageRoot>/Meshes/<NameOfBSoftType>.  Get 'package root' for example to acces bones under <PackageRoot>/Root
	public virtual void OnStart_Extension() {}


	public void OnPropSet_NeedReset(CProp oProp, float nValueOld, float nValueNew) { }

}

public struct CMapTwinVert {		// Map of 'twin verts' that begin (and are to be forced to remain at) the same (boundary) location on both a soft body mesh and its parent skinned BodyRim mesh (Information calculated by Blender that is needed for CPinSkinned creation)
	public ushort nVertPart;		// The vertex ID on the soft body 'body part' mesh of the 'twin vert'.
	public ushort nVertHost;		// The vertex ID on the skinned body 'host' mesh of the 'twin vert'
	public ushort nVertHostAdj;		// An 'adjacent vertex' to nVertSkinned on the skinned body 'host' mesh (used for normal Z-orientation with 'LookAt')
}
