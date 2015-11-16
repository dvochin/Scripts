using UnityEngine;


public class CBSkinBaked : CBSkin {		// An extension of CBSkin skinned mesh that operates on a heavily-reduced 'rim mesh' with a few hundred polygons only at the 'rim' of the skinned mesh to enable fast runtime baking of the needed polygons to 'attach' softbody/clothing parts to a skinned mesh.
	//###DESIGN!!!!!: Confusion between CBSkinBaked and CBSkinColSrc!!!!!!!		####SOON!!

	//---------------------------------------------------------------------------	MEMBERS
	[HideInInspector]	public 	Mesh				_oMeshBaked;
	[HideInInspector]	public 	CPin				_oPinGroup_Rim;		//###DESIGN!?!? No longer just a rim!!  The pin group we own and control.  All children under this node move at every frame based on the updated vert positions of our body mesh subset

	//---------------------------------------------------------------------------	INIT

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();
		GetComponent<Renderer>().enabled = false;						// Our skinned mesh exists only for providing fast position of our skinned verts and normals... we never display (unless for debugging)
		_oPinGroup_Rim = (CPin)CUtility.FindOrCreateNode(gameObject, "[Pins-" + gameObject.name + "]", typeof(CPin));
		_oMeshBaked = new Mesh();						//###LEARN: We can allocate mesh for BakeMesh() at beginning!  Yes!!
		//###DESIGN: Disabled because of CBBodyColCloth superclass!  ###DESIGN: Improve by re-classing these?? OnSimulatePre();								// Simulate manually first time so very first update PhysX gets valid positions
	}


	//---------------------------------------------------------------------------	FAST SKINNED VERTEX POSITIONING

	public virtual Vector3 GetSkinnedVertex(int nVert) {			// Much faster function than the manual function above that takes advantage of batch skinning made possible by 'BakeMesh()'
		return _memVerts.L[nVert];									//###OPT: Make inline or remove function?
	}

	public virtual Vector3 GetSkinnedNormal(int nVert) {			// Much faster function than the manual function above that takes advantage of batch skinning made possible by 'BakeMesh()'
		return _memNormals.L[nVert];
	}

	public void Baking_UpdateBakedMesh() {
		_oSkinMeshRendNow.BakeMesh(_oMeshBaked);
		//_oMeshBaked.RecalculateNormals();			//###LEARN: Weirdly enough this messes up the normals -> reset of pin vert normals if we recalc here will show seams as body moves away from startup T-pose.  I have no idea why!!
		//_oMeshBaked.RecalculateBounds();			//###CHECK: Not needed as we're invisible?
		Vector3[] aVerts = _oMeshBaked.vertices;	//###LEARN!!!!!: Absolutely IMPERATIVE to obtain whole array before loop like the one below... with individual access profiler reveals 7ms per frame if not!!!!!!!		###TODO!!!!!: Insure this is done throughout the game
		Vector3[] aNormals = _oMeshBaked.normals;
		for (int nVert = 0; nVert < _oMeshBaked.vertexCount; nVert++) {			//###HACK!!!! ###OPT!!!! Bodycol won't get updated verts if we don't do deep copy!  Find better way!!!
			_memVerts  .L[nVert] = aVerts[nVert];
			_memNormals.L[nVert] = aNormals[nVert];
		}
		//###DESIGN!!!  memVerts.L	= _oMeshBaked.vertices;
		//_memNormals.L = _oMeshBaked.normals;
	}

	//---------------------------------------------------------------------------	UPDATE

	public override void OnSimulatePre() {
		Baking_UpdateBakedMesh();
	}
}

//	public virtual Vector3 GetSkinnedVertex_Manual(int nVertOrig) {		//=== Manual (slow) implementation that is used during init before fast baking becomes fully operational ===
//		Vector3 vecUnskinned 	= _memVertsBaked[nVertOrig];		//###LEARN: Relatively expensive when not cached!... from 8fps to 5fps!
//		Vector3 vecSkinned 		= Vector3.zero;
//		BoneWeight oBW 			= _aBoneWeights[nVertOrig];
//		if (oBW.weight0 != 0.0f) vecSkinned  = _aSkinBones[oBW.boneIndex0].localToWorldMatrix.MultiplyPoint3x4(_aBindPoses[oBW.boneIndex0].MultiplyPoint3x4(vecUnskinned)) * oBW.weight0;	// Jeez... complex enough for you?
//		if (oBW.weight1 != 0.0f) vecSkinned += _aSkinBones[oBW.boneIndex1].localToWorldMatrix.MultiplyPoint3x4(_aBindPoses[oBW.boneIndex1].MultiplyPoint3x4(vecUnskinned)) * oBW.weight1;
//		if (oBW.weight2 != 0.0f) vecSkinned += _aSkinBones[oBW.boneIndex2].localToWorldMatrix.MultiplyPoint3x4(_aBindPoses[oBW.boneIndex2].MultiplyPoint3x4(vecUnskinned)) * oBW.weight2;
//		if (oBW.weight3 != 0.0f) vecSkinned += _aSkinBones[oBW.boneIndex3].localToWorldMatrix.MultiplyPoint3x4(_aBindPoses[oBW.boneIndex3].MultiplyPoint3x4(vecUnskinned)) * oBW.weight3;
//		return vecSkinned;
//	}
