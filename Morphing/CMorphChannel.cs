using UnityEngine;
using System.Collections.Generic;

public class CMorphChannel {

	CBodyBase			_oBodyBase;
	public float		_nMorphStrength;
	float				_nMorphStrengthPrevious;
	List<float>         _aMorphDataRecords;				// Collection of morphing verts of four floats per morph record in order <VertID>, X, Y, Z

	public CMorphChannel(CBodyBase oBodyBase, string sNameMorphChannel) {
		_oBodyBase = oBodyBase;
		//=== Get the morphing records from Blender.  Code finds the proper shape key, calculates delta and sends us records that our C++ code processes containing vert and delta vector ===
		_aMorphDataRecords = CByteArray.GetArray_FLOAT("'CBody'", _oBodyBase._sBlenderInstancePath_CBodyBase + ".CMorphChannel_GetMorphVerts('" + sNameMorphChannel + "')");
	}

	public bool ApplyMorph(float nMorphStrength) {
		//###DESIGN We are using a 'delta approach' to add and remove morphs to the mesh, which is *far* more efficient but can degrade over time due to numerical inacuracies adding up.
		//###IMPROVE:? Rebuild the mesh from non-delta morphs occasionally (to remove floating point imprecisions over time)
		_nMorphStrength = nMorphStrength;
		if (_nMorphStrength == _nMorphStrengthPrevious)
			return false;			// Mesh has not changed.
		float nMorphStrenghDelta = _nMorphStrength - _nMorphStrengthPrevious;
		_nMorphStrengthPrevious = _nMorphStrength;

		Vector3[] aVerts = _oBodyBase._oSkinMeshMorphing._memVerts.L;            //###DESIGN14: Direct access this way??  ###MOVE??
		int nMorphRecords = _aMorphDataRecords.Count / 4;               // Data records consist of 4 floats in order (VertID, DeltaX, DeltaY, DeltaZ)
		Vector3 vecMorphFull;
		Vector3 vecMorphDelta;
		int nMorphRecordIndex = 0;
		for (int nMorphRecord = 0; nMorphRecord < nMorphRecords; nMorphRecord++) {
			int nVert = (int)	 _aMorphDataRecords[nMorphRecordIndex++];
			vecMorphFull.x =     _aMorphDataRecords[nMorphRecordIndex++];	// Obtain the position of the vert at morph value = 1.0
			vecMorphFull.y =     _aMorphDataRecords[nMorphRecordIndex++];	// nMorphRecordIndex avoids us having to multiply by 4 and add offsets in a tight loop
			vecMorphFull.z =     _aMorphDataRecords[nMorphRecordIndex++];
			vecMorphDelta = vecMorphFull * nMorphStrenghDelta;				// Obtain how much we need to move this vert to take it from the previous morph position to the current one.
			aVerts[nVert] += vecMorphDelta;									// Move this vert from the previous position to the new one.  Note that as morph channels are not orthogonal (several channels can morph the same verts, floating point imprecisions add over time)  This technique is MUCH faster as we don't have to fully rebuild the mesh from all morph channels everytime a morph channel changes!
		}
		//###IMPROVE14: Do this in C++ as before?? ErosEngine.Morph_ApplyMorphOpToMesh(IntPtr.Zero, _oBodyBase._oSkinMeshMorph._memVerts.P, _oBodyBase._oSkinMeshMorph._memVerts.L.Length, _memMorphDeltaVerts.P, _memMorphDeltaVerts.L.Length / 16, nMorphStrenghDelta);	// Each record is one int (vert#) and one 3D vector = 16 bytes
		return true;			// Mesh has changed
	}
}
//vecMorphFull.x =    -_aMorphDataRecords[4 * nMorphRecord + 1];		// Previous code to convert from left-hand <-> right-hand.  No longer needed now that Blender vector are encoded at the primitive level
//vecMorphFull.y =     _aMorphDataRecords[4 * nMorphRecord + 3];
//vecMorphFull.z =    -_aMorphDataRecords[4 * nMorphRecord + 2];
