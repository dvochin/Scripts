using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


public class CBSkin : CBMesh {	// Blender-centered class that extends CBMesh to provide skinned mesh support.

	[HideInInspector]	public 	SkinnedMeshRenderer	_oSkinMeshRendNow;

	[HideInInspector] 	public 	ArrayList 			_aSkinTargets = new ArrayList();   // Our collection of skin targets that follow their skin position and can act as 'magnets' to pull actors such as hands

	//---------------------------------------------------------------------------	INIT

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();			// First call the CBMesh base class to serialize the mesh itself...

		_oSkinMeshRendNow = (SkinnedMeshRenderer)CUtility.FindOrCreateComponent(transform, typeof(SkinnedMeshRenderer));

		////=== Send blender command to obtain the skinned mesh info ===
		CMemAlloc<byte> memBA = new CMemAlloc<byte>();
		CGame.gBL_SendCmd_GetMemBuffer("'Client'", "gBL_GetMesh_SkinnedInfo('" + _sNameBlenderMesh + "')", ref memBA);	
		byte[] oBA = (byte[])memBA.L;			// Setup the deserialization arrays and deserialize the body from Blender
		int nPosBA = 0;
		CUtility.BlenderSerialize_CheckMagicNumber(ref oBA, ref nPosBA, false);				// Read the 'beginning magic number' that always precedes a stream.
		

		//===== RECEIVE SKINNING INFORMATION =====
		Matrix4x4[] aSkinBindPoses 		= null;			//###WEAK: A bit of unfortunate structure imposed on the code because of how late we read the stream versus how we must init vars for skinned mesh...  Can be improved.
		Transform[]	aSkinBones 			= null;
		List<BoneWeight> aBoneWeights 	= null;
		
		//=== Read in the flat array of names of vertex groups.  This will enable us to map the Blender vertex group blends to our fixed Unity bone rig ===
		byte nVertGroupsMesh = oBA[nPosBA]; nPosBA++;
		aSkinBones = new Transform[nVertGroupsMesh];
		aSkinBindPoses = new Matrix4x4[nVertGroupsMesh];

		//=== Find the root of our skinning bones from our body's main skinned mesh ===
		Transform oNodeBoneParentRoot = _oBody._oBodyRootGO.transform.FindChild("Bones/chest");

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

		int nVerts = GetNumVerts();
		for (int nVert = 0; nVert < nVerts; nVert++) {
			byte nVertGroups = oBA[nPosBA]; nPosBA++;
			float nBoneWeightSum = 0;
            if (nVertGroups >= 5) { 
                Debug.LogWarningFormat("Warning: Skinned mesh '{0}' at vert {1} has {2} vert groups!", _sNameBlenderMesh, nVert, nVertGroups);
                throw new CException("CBMesh.ctor() encountered a vertex with " + nVertGroups + " vertex groups!");
            }
    
            for (byte nVertGroup = 0; nVertGroup < nVertGroups; nVertGroup++) {		//###IMPROVE: It might be more elegant to shift this code to Blender Python?
				aBoneIndex[nVertGroup]  = oBA[nPosBA]; nPosBA++;
				float nBoneWeight = BitConverter.ToSingle(oBA, nPosBA); nPosBA+=4;
				if (nBoneWeight < 0)
					//Debug.LogError("CBMesh.ctor() encountered a bone weight below 0 at vert " + nVert + " and vert group " + nVertGroup);
					throw new CException("CBSkin.ctor() encountered a bone weight below 0 at vert " + nVert + " and vert group " + nVertGroup);
				if (nBoneWeight > 1)
					//throw new CException("CBMesh.ctor() encountered a bone weight over 1 at vert " + nVert + " and vert group " + nVertGroup);	//###IMPROVE: Common!  What to do? cap??
					Debug.LogWarning("CBSkin.ctor() encountered a bone weight over 1 = " + nBoneWeight + " at vert " + nVert + " and vert group " + nVertGroup);	//###IMPROVE: Common!  What to do? cap??
				aBoneWeight[nVertGroup] = nBoneWeight;
				nBoneWeightSum += nBoneWeight;
			}
	
			if (nBoneWeightSum < 0.999 || nBoneWeightSum > 1.001) {
				Debug.LogWarning("###W: CBMesh.ctor() vertex " + nVert + " had out of range weight of " + nBoneWeightSum);
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
			Debug.LogWarning("###ERROR: CBSkin.ctor() found " + nErrSumOutOfRange + " bones with out-of-range sums!");
			
		//=== Read the number of errors detected when sending over the blender bone weights... what to do?? ===		
		int nErrorsBoneGroups = BitConverter.ToInt32(oBA, nPosBA); nPosBA+=4;
		if (nErrorsBoneGroups > 0)			//###IMPROVE ###CHECK: What to do???
			Debug.LogError("###ERROR: CBSkin.ctor() detected " + nErrorsBoneGroups + "	blender-side errors while reading in blender mesh!");
		
		CUtility.BlenderSerialize_CheckMagicNumber(ref oBA, ref nPosBA, true);				// Read the 'end magic number' that always follows a stream.


		//=== Finalize the mesh creation by stuffing _oMeshRender into mesh filter or skinned mesh renderer as appropriate ===
		UpdateNormals();									// Fix the normals with the just-serialized map of shared normals
		_oMeshNow.bindposes 	= aSkinBindPoses;
		_oMeshNow.boneWeights 	= aBoneWeights.ToArray();
		_oSkinMeshRendNow.sharedMesh = _oMeshNow;					//###TODO: skinned mesh complex bounds!
		_oSkinMeshRendNow.materials = _aMats;
		_oSkinMeshRendNow.bones = aSkinBones;


		//=== Conveniently reset skinned mesh renderer flags we always keep constant... makes it easier to override the defaults which go the other way ===
		_oSkinMeshRendNow.updateWhenOffscreen = true;                               //###OPT: Should some mesh have this off?
        _oSkinMeshRendNow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _oSkinMeshRendNow.receiveShadows = false;
		if (_oSkinMeshRendNow.GetComponent<Collider>() != null)						//###CHECK: ReleaseGlobalHandles mesh collider here if it exists at gameplay
			Destroy(_oSkinMeshRendNow.GetComponent<Collider>());
	}


	
	
	//---------------------------------------------------------------------------	UPDATE

	public virtual void OnSimulatePre() {}

	//---------------------------------------------------------------------------	UTILITY
}
