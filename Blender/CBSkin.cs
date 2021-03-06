using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


public class CBSkin : CBMesh {	// Blender-centered class that extends CBMesh to provide skinned mesh support.

	[HideInInspector]	public 	SkinnedMeshRenderer	_oSkinMeshRendNow;

	[HideInInspector] 	public 	ArrayList 			_aSkinTargets = new ArrayList();   // Our collection of skin targets that follow their skin position and can act as 'magnets' to pull actors such as hands

	//---------------------------------------------------------------------------	INIT

	public override void OnDeserializeFromBlender(params object[] aExtraArgs) {
		base.OnDeserializeFromBlender(aExtraArgs);			// First call the CBMesh base class to serialize the mesh itself...

		_oSkinMeshRendNow = (SkinnedMeshRenderer)CUtility.FindOrCreateComponent(transform, typeof(SkinnedMeshRenderer));

		////=== Send blender command to obtain the skinned mesh info ===
		CByteArray oBA = new CByteArray("'CBody'", _oBodyBase._sBlenderInstancePath_CBodyBase + _sNameCBodyInstanceMember + ".Unity_GetMesh_SkinnedInfo()");
		

		//===== RECEIVE SKINNING INFORMATION =====
		Matrix4x4[] aSkinBindPoses 		= null;			//###WEAK: A bit of unfortunate structure imposed on the code because of how late we read the stream versus how we must init vars for skinned mesh...  Can be improved.
		Transform[]	aSkinBones 			= null;
		List<BoneWeight> aBoneWeights 	= null;

		//=== Read in the flat array of names of vertex groups.  This will enable us to map the Blender vertex group blends to our fixed Unity bone rig ===
		ushort nVertGroupsMesh = oBA.ReadUShort();
		aSkinBones = new Transform[nVertGroupsMesh];
		aSkinBindPoses = new Matrix4x4[nVertGroupsMesh];

		//=== Find the root of our skinning bones from our body's main skinned mesh ===
		Transform oNodeBoneParentRoot = CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Bones");

		if (oNodeBoneParentRoot == null)
			CUtility.ThrowException("CBMesh is attempting to reconstruct a skinned mesh but was not able to find root node of bones!");
		for (ushort nVertGroup = 0; nVertGroup < nVertGroupsMesh; nVertGroup++) {
			string sVertGroup = oBA.ReadString();
			//if (sVertGroup[0] == '+') {				//###CHECK: Bone names that start with '+' are dynamic and are fully-serialized at game time (now).  As we don't have statically-created bone info that Unity pulled from Blender at ship-time we must fully create this dyanic bone right here from full info Blender is now providing for this.
			Transform oNodeBone = CUtility.FindNodeByName(oNodeBoneParentRoot, sVertGroup);     //###OPT: Recursive on hundreds of bones for hundreds of bones = O squared!!  ###IMPROVE: Find by direct path!
			if (oNodeBone != null) {
				//Debug.Log("Bone found for '" + sVertGroup + "'");	
				aSkinBones[nVertGroup] = oNodeBone;
				aSkinBindPoses[nVertGroup] = oNodeBone.worldToLocalMatrix;	//###CHECK
			} else {
				Debug.LogWarningFormat("**ERROR: CBMesh.ctor() could not find bone '{0}'", sVertGroup);		//###DESIGN?: Throw?
			}
		}
			
		//=== Stream in the bone weight info ===		###NOTE: Note that this code assumes a compatible bone tree between Blender and Unity!!!  To update Unity bones from Blender bones use the CBodyEd.UpdateBonesFromBlender() function
		aBoneWeights = new List<BoneWeight>();
		int[] aBoneIndex = new int[32];			// Temp arrays 
		float[] aBoneWeight = new float[32];
		int nErr_VertsWithOutOfRangeSums = 0;
		int nErr_VertsWithZeroBones = 0;
		int nErr_VertsWithTooManyBones = 0;

		int nVerts = GetNumVerts();
		for (int nVert = 0; nVert < nVerts; nVert++) {
			byte nVertGroups = oBA.ReadByte();
			float nBoneWeightSum = 0;
            if (nVertGroups == 0) {
				nErr_VertsWithZeroBones++;
                //Debug.LogWarningFormat("Warning: Skinned mesh '{0}' at vert {1} has 0 vert groups!", _sNameBlenderMesh, nVert);
                //CUtility.ThrowExceptionF("CBMesh.ctor() encountered a vertex with {} vertex groups!", nVertGroups);
            }
            if (nVertGroups >= 5) {
				nErr_VertsWithTooManyBones++;
                Debug.LogWarningFormat("Warning: Skinned mesh '{0}' at vert {1} has {2} vert groups!", _sNameBlenderMesh, nVert, nVertGroups);
                CUtility.ThrowExceptionF("CBMesh.ctor() encountered a vertex with {0} vertex groups!", nVertGroups);
            }
    
            for (byte nVertGroup = 0; nVertGroup < nVertGroups; nVertGroup++) {		//###IMPROVE: It might be more elegant to shift this code to Blender Python?
				aBoneIndex[nVertGroup]  = oBA.ReadUShort();
				float nBoneWeight = oBA.ReadFloat();
				if (nBoneWeight < 0)
					CUtility.ThrowExceptionF("CBSkin.ctor() encountered a bone weight below 0 at vert {0} and vert group.", nVert, nVertGroup);
				if (nBoneWeight > 1)
					Debug.LogWarningFormat("CBSkin.ctor() encountered a bone weight over 1 = {0} at vert {1} and vert group {2}.", nBoneWeight, nVert, nVertGroup);	//###IMPROVE: Common!  What to do? cap??
				aBoneWeight[nVertGroup] = nBoneWeight;
				nBoneWeightSum += nBoneWeight;
			}
	
			if (nBoneWeightSum < 0.999 || nBoneWeightSum > 1.001) {
				//###TODO: Too many right now but put back in ASAP: Debug.LogWarning("###W: CBMesh.ctor() vertex " + nVert + " had out of range weight of " + nBoneWeightSum);
				nErr_VertsWithOutOfRangeSums++;
			}
			BoneWeight oBoneWeight = new BoneWeight();
			if (nVertGroups > 0) { oBoneWeight.boneIndex0 = aBoneIndex[0]; oBoneWeight.weight0 = aBoneWeight[0]; }
			if (nVertGroups > 1) { oBoneWeight.boneIndex1 = aBoneIndex[1]; oBoneWeight.weight1 = aBoneWeight[1]; }
			if (nVertGroups > 2) { oBoneWeight.boneIndex2 = aBoneIndex[2]; oBoneWeight.weight2 = aBoneWeight[2]; }
			if (nVertGroups > 3) { oBoneWeight.boneIndex3 = aBoneIndex[3]; oBoneWeight.weight3 = aBoneWeight[3]; }
				
			aBoneWeights.Add(oBoneWeight);
		}
			
		if (nErr_VertsWithOutOfRangeSums > 0)		//###CHECK: What to do???
			Debug.LogErrorFormat("###ERROR: CBSkin.ctor() on mesh '{0}' found {1} verts with out-of-range sums!", gameObject.name, nErr_VertsWithOutOfRangeSums);
			
		if (nErr_VertsWithZeroBones > 0)
			Debug.LogErrorFormat("###ERROR: CBSkin.ctor() on mesh '{0}' found {1} verts with zero bones!", gameObject.name, nErr_VertsWithZeroBones);
			
		if (nErr_VertsWithTooManyBones > 0)
			Debug.LogErrorFormat("###ERROR: CBSkin.ctor() on mesh '{0}' found {1} verts with too many bones!", gameObject.name, nErr_VertsWithTooManyBones);
			
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
		_oSkinMeshRendNow.materials = _aMatsCurrent;		//###DESIGN:!! Using materials instead of shared materials so that different bodies on the same texture base can go invisible or not... (but more costly in video memory!)  ###OPT:!!
		_oSkinMeshRendNow.bones = aSkinBones;


		//=== Conveniently reset skinned mesh renderer flags we always keep constant... makes it easier to override the defaults which go the other way ===
		_oSkinMeshRendNow.updateWhenOffscreen = true;                               //###OPT: Should some mesh have this off?
        _oSkinMeshRendNow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;       //###PERF: Runtime performance of shadows?  (All done in different quality settings?)
        _oSkinMeshRendNow.receiveShadows = true;
		if (_oSkinMeshRendNow.GetComponent<Collider>() != null)						//###CHECK: ReleaseGlobalHandles mesh collider here if it exists at gameplay
			Destroy(_oSkinMeshRendNow.GetComponent<Collider>());
	}


	
	
	//---------------------------------------------------------------------------	UPDATE

	public virtual void OnSimulate() {}

	//---------------------------------------------------------------------------	UTILITY
}
