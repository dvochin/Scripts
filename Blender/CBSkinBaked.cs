using UnityEngine;
using System.Collections.Generic;


public class CBSkinBaked : CBSkin {		// An extension of CBSkin skinned mesh that operates on a heavily-reduced 'rim mesh' with a few hundred polygons only at the 'rim' of the skinned mesh to enable fast runtime baking of the needed polygons to 'attach' softbody/clothing parts to a skinned mesh.

	//---------------------------------------------------------------------------	MEMBERS
	                            Mesh				_oMeshBaked;
	[HideInInspector]	public 	GameObject			_oDebugBakedMeshOutputGO;		// Debug dump of baked skinned mesh.  Used to visualize the shape of what has been baked
                                uint                _nFrameCount_LastBake;          // Ensure we only bake once per frame
    //---------------------------------------------------------------------------	INIT

    public virtual void Initialize() {
		_oMeshBaked = new Mesh();
        if (GetComponent<Renderer>())
		    GetComponent<Renderer>().enabled = false;						// Our skinned mesh exists only for providing fast position of our skinned verts and normals... we never display (unless for debugging)
    }

    public override void OnDeserializeFromBlender(params object[] aExtraArgs) {
		base.OnDeserializeFromBlender(aExtraArgs);
        Initialize();
	}

	public Mesh Baking_GetBakedSkinnedMesh() {
        if (_oSkinMeshRend == null) {           //#DEV26: ###WEAK: Have to hack this super-important class for static colliders to collide properly with fluids... can find another way?
            MeshCollider oMeshCol = GetComponent<MeshCollider>();
            return oMeshCol.sharedMesh;
        } else { 
            if (_nFrameCount_LastBake != CGame._nFrameCount) {
                _nFrameCount_LastBake  = CGame._nFrameCount;
                _oSkinMeshRend.BakeMesh(_oMeshBaked);
            }
            return _oMeshBaked;
        }
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
        oMF.mesh = Baking_GetBakedSkinnedMesh();
	}
}
