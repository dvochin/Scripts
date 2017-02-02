using UnityEngine;
using System;
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
		//###IMPROVE: Rebuild the mesh from non-delta morphs occasionally (to remove floating point imprecisions over time)
		_nMorphStrength = nMorphStrength;
		if (_nMorphStrength == _nMorphStrengthPrevious)
			return false;			// Mesh has not changed.
		float nMorphStrenghDelta = _nMorphStrength - _nMorphStrengthPrevious;
		_nMorphStrengthPrevious = _nMorphStrength;

		Vector3[] aVerts = _oBodyBase._oMeshMorphResult._memVerts.L;            //###DESIGN<14>: Direct access this way??  ###MOVE??
		int nMorphRecords = _aMorphDataRecords.Count / 4;               // Data records consist of 4 floats in order (VertID, DeltaX, DeltaY, DeltaZ)
		Vector3 vecMorphFull;
		Vector3 vecMorphDelta;
		for (int nMorphRecord = 0; nMorphRecord < nMorphRecords; nMorphRecord++) {
			int nVert = (int)	 _aMorphDataRecords[4 * nMorphRecord + 0];
			vecMorphFull.x =	-_aMorphDataRecords[4 * nMorphRecord + 1];	// Obtain the position of the vert at morph value = 1.0
			vecMorphFull.y =	 _aMorphDataRecords[4 * nMorphRecord + 3];	//###NOTE: Note the conversion between the two coordinate space (left-handed versus right-handed)  ###IMPROVE<14>: Add support function?
			vecMorphFull.z =	-_aMorphDataRecords[4 * nMorphRecord + 2];
			vecMorphDelta = vecMorphFull * nMorphStrenghDelta;				// Obtain how much we need to move this vert to take it from the previous morph position to the current one.
			aVerts[nVert] += vecMorphDelta;									// Move this vert from the previous position to the new one.  Note that as morph channels are not orthogonal (several channels can morph the same verts, floating point imprecisions add over time)  This technique is MUCH faster as we don't have to fully rebuild the mesh from all morph channels everytime a morph channel changes!
		}
		//###IMPROVE<14>: Do this in C++ as before?? ErosEngine.Morph_ApplyMorphOpToMesh(IntPtr.Zero, _oBodyBase._oMeshMorphResult._memVerts.P, _oBodyBase._oMeshMorphResult._memVerts.L.Length, _memMorphDeltaVerts.P, _memMorphDeltaVerts.L.Length / 16, nMorphStrenghDelta);	// Each record is one int (vert#) and one 3D vector = 16 bytes
		return true;			// Mesh has changed
	}
}
