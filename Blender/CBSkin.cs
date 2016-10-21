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
		CByteArray oBA = new CByteArray("'CBody'", _oBodyBase._sBlenderInstancePath_CBodyBase + _sNameCBodyInstanceMember + ".Unity_GetMesh_SkinnedInfo()");
		

		//===== RECEIVE SKINNING INFORMATION =====
		Matrix4x4[] aSkinBindPoses 		= null;			//###WEAK: A bit of unfortunate structure imposed on the code because of how late we read the stream versus how we must init vars for skinned mesh...  Can be improved.
		Transform[]	aSkinBones 			= null;
		List<BoneWeight> aBoneWeights 	= null;

		//=== Read in the flat array of names of vertex groups.  This will enable us to map the Blender vertex group blends to our fixed Unity bone rig ===
		byte nVertGroupsMesh = oBA.ReadByte();
		aSkinBones = new Transform[nVertGroupsMesh];
		aSkinBindPoses = new Matrix4x4[nVertGroupsMesh];

		//=== Find the root of our skinning bones from our body's main skinned mesh ===
		Transform oNodeBoneParentRoot = CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Bones");

		if (oNodeBoneParentRoot == null)
			CUtility.ThrowException("CBMesh is attempting to reconstruct a skinned mesh but was not able to find root node of bones!");
		for (byte nVertGroup = 0; nVertGroup < nVertGroupsMesh; nVertGroup++) {
			string sVertGroup = oBA.ReadString();
			Transform oNodeBone = CUtility.FindNodeByName(oNodeBoneParentRoot, sVertGroup);     //###OPT: Recursive on hundreds of bones for hundreds of bones = O squared!!  ###IMPROVE: Find by direct path!
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
			byte nVertGroups = oBA.ReadByte();
			float nBoneWeightSum = 0;
            if (nVertGroups >= 5) { 
                Debug.LogWarningFormat("Warning: Skinned mesh '{0}' at vert {1} has {2} vert groups!", _sNameBlenderMesh, nVert, nVertGroups);
                CUtility.ThrowException("CBMesh.ctor() encountered a vertex with " + nVertGroups + " vertex groups!");
            }
    
            for (byte nVertGroup = 0; nVertGroup < nVertGroups; nVertGroup++) {		//###IMPROVE: It might be more elegant to shift this code to Blender Python?
				aBoneIndex[nVertGroup]  = oBA.ReadByte();
				float nBoneWeight = oBA.ReadFloat();
				if (nBoneWeight < 0)
					//Debug.LogError("CBMesh.ctor() encountered a bone weight below 0 at vert " + nVert + " and vert group " + nVertGroup);
					CUtility.ThrowException("CBSkin.ctor() encountered a bone weight below 0 at vert " + nVert + " and vert group " + nVertGroup);
				if (nBoneWeight > 1)
					//CUtility.ThrowException("CBMesh.ctor() encountered a bone weight over 1 at vert " + nVert + " and vert group " + nVertGroup);	//###IMPROVE: Common!  What to do? cap??
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
		int nErrorsBoneGroups = oBA.ReadInt();
		if (nErrorsBoneGroups > 0)			//###IMPROVE ###CHECK: What to do???
			Debug.LogError("###ERROR: CBSkin.ctor() detected " + nErrorsBoneGroups + "	blender-side errors while reading in blender mesh!");

		oBA.CheckMagicNumber_End();


		//=== Finalize the mesh creation by stuffing _oMeshRender into mesh filter or skinned mesh renderer as appropriate ===
		UpdateNormals();									// Fix the normals with the just-serialized map of shared normals
		_oMeshNow.bindposes 	= aSkinBindPoses;
		_oMeshNow.boneWeights 	= aBoneWeights.ToArray();
		_oSkinMeshRendNow.sharedMesh = _oMeshNow;					//###TODO: skinned mesh complex bounds!
		_oSkinMeshRendNow.materials = _aMats;
		_oSkinMeshRendNow.bones = aSkinBones;


		//=== Conveniently reset skinned mesh renderer flags we always keep constant... makes it easier to override the defaults which go the other way ===
		_oSkinMeshRendNow.updateWhenOffscreen = true;                               //###OPT: Should some mesh have this off?
        _oSkinMeshRendNow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;       //###PERF: Runtime performance of shadows?  (All done in different quality settings?)
        _oSkinMeshRendNow.receiveShadows = true;
		if (_oSkinMeshRendNow.GetComponent<Collider>() != null)						//###CHECK: ReleaseGlobalHandles mesh collider here if it exists at gameplay
			Destroy(_oSkinMeshRendNow.GetComponent<Collider>());
	}


	
	
	//---------------------------------------------------------------------------	UPDATE

	public virtual void OnSimulatePre() {}

	//---------------------------------------------------------------------------	UTILITY
}
