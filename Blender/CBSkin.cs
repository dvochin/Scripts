using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


public class CBSkin : CBMesh {	// Blender-centered class that extends CBMesh to provide skinned mesh support.

	[HideInInspector]	public 	SkinnedMeshRenderer	_oSkinMeshRendNow;

	[HideInInspector] 	public 	ArrayList 			_aSkinTargets = new ArrayList();	// Our collection of skin targets that follow their skin position and can act as 'magnets' to pull actors such as hands

	//---------------------------------------------------------------------------	INIT

	public override void OnStart(CBody oBody) {
		base.OnStart(oBody);
		_oSkinMeshRendNow = GetComponent<SkinnedMeshRenderer>();
		
		//=== Conveniently reset skinned mesh renderer flags we always keep constant... makes it easier to override the defaults which go the other way ===
		_oSkinMeshRendNow.updateWhenOffscreen = false;
		//_oSkinMeshRendNow.castShadows = false;
        _oSkinMeshRendNow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _oSkinMeshRendNow.receiveShadows = false;
		if (Application.isPlaying && _oSkinMeshRendNow.GetComponent<Collider>() != null)						//###CHECK: ReleaseGlobalHandles mesh collider here if it exists at gameplay
			Destroy(_oSkinMeshRendNow.GetComponent<Collider>());
	} 
	
	
	//---------------------------------------------------------------------------	UPDATE

	public virtual void OnSimulatePre() {}

	//---------------------------------------------------------------------------	UTILITY
}
