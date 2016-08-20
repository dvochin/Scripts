using UnityEngine;


public class CBSkinBaked : CBSkin {		// An extension of CBSkin skinned mesh that operates on a heavily-reduced 'rim mesh' with a few hundred polygons only at the 'rim' of the skinned mesh to enable fast runtime baking of the needed polygons to 'attach' softbody/clothing parts to a skinned mesh.
	//###DESIGN!!!!!: Confusion between CBSkinBaked and CBSkinColSrc!!!!!!!		####SOON!!

	//---------------------------------------------------------------------------	MEMBERS
	[HideInInspector]	public 	Mesh				_oMeshBaked;

	//---------------------------------------------------------------------------	INIT

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();
		GetComponent<Renderer>().enabled = false;						// Our skinned mesh exists only for providing fast position of our skinned verts and normals... we never display (unless for debugging)
		_oMeshBaked = new Mesh();						//###LEARN: We can allocate mesh for BakeMesh() at beginning!  Yes!!
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

	//---------------------------------------------------------------------------	UPDATE

	public override void OnSimulatePre() {		//####CHECK: Call base class?       //###OBS!
		Baking_UpdateBakedMesh();
	}
}
