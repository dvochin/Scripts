using UnityEngine;
using System;
using System.Collections;

public class CMorphChannel {

	CFace				_oFace;
	CMemAlloc<byte>		_memMorphDeltaVerts = new CMemAlloc<byte>();
	public float		_nMorphStrength;
	float				_nMorphStrengthPrevious;
	public float		_nMorphStrengthRandom;			// Small amount of random morphing added separately ###DESIGN: Right??

	public CMorphChannel(CFace oFace, string sNameMorphChannel) {
		_oFace = oFace;
		//=== Get the morphing records from Blender.  Code finds the proper shape key, calculates delta and sends us records that our C++ code processes containing vert and delta vector ===
		CGame.gBL_SendCmd_GetMemBuffer("'Client'", "CMorphChannel_GetMorphVerts('" + _oFace._sNameBlenderMesh  + "', '" + sNameMorphChannel + "')", ref _memMorphDeltaVerts);
	}

	public void ApplyMorph(float nMorphStrength) {
		//###DESIGN!!! We are using a 'delta approach' to add and remove morphs to the mesh, which is *far* more efficient but can degrade over time due to numerical inacuracies adding up.
		//###IMPROVE: Rebuild the mesh occasionally??
		_nMorphStrength = nMorphStrength;
		float nMorphTotal = _nMorphStrength + _nMorphStrengthRandom;
		if (nMorphTotal == _nMorphStrengthPrevious)
			return;
		float nMorphStrenghDelta = nMorphTotal - _nMorphStrengthPrevious;		//###CHECK: Add random to previous?
		_nMorphStrengthPrevious = nMorphTotal;
		ErosEngine.Morph_ApplyMorphOpToMesh(/*_oFace._memVertsBase.P*/ IntPtr.Zero, _oFace._memVerts.P, _oFace._memVerts.L.Length, _memMorphDeltaVerts.P, _memMorphDeltaVerts.L.Length / 16, nMorphStrenghDelta);	// Each record is one int (vert#) and one 3D vector = 16 bytes
		_oFace._bMorphApplied = true;			// Force owner mesh to update.
	}

	public void ApplyMorphRandom(float nMorphStrengthRandom) {		// Separate channel for randomization so main value is unchanged
		_nMorphStrengthRandom = nMorphStrengthRandom;
		ApplyMorph(_nMorphStrength);
	}
}
