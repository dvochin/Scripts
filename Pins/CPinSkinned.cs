using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class CPinSkinned : CPin {								// Abstracts a skinned rigid-body pin: Calculating the skinned position at each frame, updating the position of all child CPinTetra according to their offset, etc (In 'Master' vert index)
	public CBSkinBaked 			_oBSkinBaked;					// Pointer to the skin rim we refer to for position and normals.  (Reduced version of main body mesh)
	public int					_nVertHost;						// The vertex on the original host we skin to
	public int					_nVertHostAdj;					// Another vertex on our same triangle (used for consistent rotation through 'LookAt()'
	public int 					_nVertPart;						// The vertex on the softbody-side that we pin to the location of _nVertHost
	public Vector3				_vecPos;						// Our current position.  Same as transform.position but cached for speed.
	public Vector3				_vecPosAdjacent;				// The position of our adjacent vert.  Only here to avoid alloc/dealloc at runtime
	public Vector3				_vecNormal;						// Our current normal (similar to our orientation).  Cached for speed
	public Material				_oMatPinSkinned;				// The material used to draw this CPinSkinned		//###OPT: Share common material for all pins!
	public Material				_oMatPinTetra;					// The material used to draw our child CPinTetra (color related to _oMatPinSkinned)
	
	public static CPinSkinned CreatePinSkinned(CPin oPinParent, CBSkinBaked oBSkinBaked, int nVertPart, int nVertHost, int nVertHostAdjacent) {
		GameObject oGoPinTemplate = Resources.Load("GUI-Pins/PinSkinned") as GameObject;
		GameObject oGO = GameObject.Instantiate(oGoPinTemplate) as GameObject;
		CPinSkinned oPinSkinned = oGO.GetComponent<CPinSkinned>();
		oPinSkinned.InitializePinSkinned(oPinParent, oBSkinBaked, nVertPart, nVertHost, nVertHostAdjacent);
		return oPinSkinned;
	}
	
	public void InitializePinSkinned(CPin oPinParent, CBSkinBaked oBSkinBaked, int nVertPart, int nVertHost, int nVertHostAdj) {
		_oBSkinBaked		= oBSkinBaked;
		_nVertPart			= nVertPart;
		_nVertHost			= nVertHost;
		_nVertHostAdj		= nVertHostAdj;
		
		transform.name = _nVertHost.ToString();							//###IMPROVE: Decorate the name for Unity GUI? or keep it just ID?
		transform.parent = oPinParent.transform;
		transform.localPosition = Vector3.zero;							// Reset our position to coincide with parent (doesn't really matter as we set below but just to be safe)
		GetComponent<Renderer>().enabled = false;									// Hide renderer for pin.  Power users can show with a hotkey

		_oMatPinSkinned			= new Material(Shader.Find("Diffuse"));			//###OPT: Reduce cost of this debug visualization stuff...
		_oMatPinSkinned.color	= new Color(UnityEngine.Random.Range(0.1f,0.6f), UnityEngine.Random.Range(0.1f,0.6f), UnityEngine.Random.Range(0.1f,0.6f));
		_oMatPinTetra			= new Material(Shader.Find("Diffuse"));		// Give our child tetra pins some darker color of our own.
		_oMatPinTetra.color		= new Color(_oMatPinSkinned.color.r*.8f, _oMatPinSkinned.color.g*.8f, _oMatPinSkinned.color.b*.8f);
		GetComponent<Renderer>().sharedMaterial = _oMatPinSkinned;
		
		OnSimulatePre();						// Set our initial position and orientation so rest of initialization procedure (Attachment to CPinTetra) occurs correctly
	}

	public override void OnSimulatePre() {
		//if (_bManualControl == false) {
		_vecPos = _oBSkinBaked.GetSkinnedVertex(_nVertHost);
		_vecNormal = _oBSkinBaked.GetSkinnedNormal(_nVertHost);
		_vecPosAdjacent = _oBSkinBaked.GetSkinnedVertex(_nVertHostAdj);

		transform.position = _vecPos;
		transform.LookAt(_vecPos + _vecNormal, _vecPosAdjacent - _vecPos);		//###LEARN: Important to not only provide forward (1st arg) but also where 'up' is.  To always keep things constant no matter how the body is oriented, we define up a our own vertex 1 - vertex 0!

		base.OnSimulatePre();				//###OPT!! Review need for hierarchy calling at every frame??
	}
};
