using UnityEngine;
using System.Collections.Generic;


public class CBSkinBaked : CBSkin {		// An extension of CBSkin skinned mesh that operates on a heavily-reduced 'rim mesh' with a few hundred polygons only at the 'rim' of the skinned mesh to enable fast runtime baking of the needed polygons to 'attach' softbody/clothing parts to a skinned mesh.

	//---------------------------------------------------------------------------	MEMBERS
	[HideInInspector]	public 	Mesh				_oMeshBaked;
	[HideInInspector]	public 	GameObject			_oDebugBakedMeshOutputGO;		// Debug dump of baked skinned mesh.  Used to visualize the shape of what has been baked

	//---------------------------------------------------------------------------	INIT

	public override void OnDeserializeFromBlender(params object[] aExtraArgs) {
		base.OnDeserializeFromBlender(aExtraArgs);
		GetComponent<Renderer>().enabled = false;						// Our skinned mesh exists only for providing fast position of our skinned verts and normals... we never display (unless for debugging)
		_oMeshBaked = new Mesh();						//###INFO: We can allocate mesh for BakeMesh() at beginning!  Yes!!
	}


	//---------------------------------------------------------------------------	FAST SKINNED VERTEX POSITIONING

	//public virtual Vector3 GetSkinnedVertex(int nVert) {			// Much faster function than the manual function above that takes advantage of batch skinning made possible by 'BakeMesh()'
	//	return _memVerts.L[nVert];									//###OPT: Make inline or remove function?
	//}

	//public virtual Vector3 GetSkinnedNormal(int nVert) {			// Much faster function than the manual function above that takes advantage of batch skinning made possible by 'BakeMesh()'
	//	return _memNormals.L[nVert];
	//}

	public void Baking_UpdateBakedMesh() {
		_oSkinMeshRendNow.BakeMesh(_oMeshBaked);
	}

	public void Baking_DumpBakedMeshToDebugGameObject() {
		MeshFilter oMF;
		if (_oDebugBakedMeshOutputGO == null) {
			_oDebugBakedMeshOutputGO = new GameObject(gameObject.name + "-DebugBakedMeshOutput");
			oMF = _oDebugBakedMeshOutputGO.AddComponent<MeshFilter>();
			MeshRenderer oMR = _oDebugBakedMeshOutputGO.AddComponent<MeshRenderer>();
			oMR.sharedMaterial = new Material(Shader.Find("Standard"));
		} else {
			oMF = _oDebugBakedMeshOutputGO.GetComponent<MeshFilter>();
		}
		_oSkinMeshRendNow.BakeMesh(oMF.mesh);
	}

	//---------------------------------------------------------------------------	UPDATE

	public override void OnSimulate() {		//####CHECK: Call base class?       //###OBS!
		Baking_UpdateBakedMesh();
	}
}
