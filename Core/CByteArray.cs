using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;

public class CByteArray {							// Unity equivalent of Blender's CByteArray class.  Responsible to efficiently share large BLOBs with compatible serialization between Unity and Blender
	string              _sBlenderAccessString;
	CMemAlloc<byte>		_memBA;
	byte[]				_oBA;
	int					_nPosRead;
	int                 _nLength;

	const ushort C_CByteArray_StreamBegin = 0x0B16;  // Magic numbers stored as unsigned shorts at the head & tail of every serialization to help sanity checks...         (MUST MATCH BLENDER SIDE!)
	const ushort C_CByteArray_StreamEnd   = 0xB00B;

	//---------------------------------------------------------------------------	CTOR
	public CByteArray(string sNameBlenderModule, string sBlenderAccessString) {
		_sBlenderAccessString = sBlenderAccessString;
		_memBA = new CMemAlloc<byte>();
		CGame.gBL_SendCmd_GetMemBuffer(sNameBlenderModule, sBlenderAccessString, ref _memBA);      //###DESIGN#11: Always force Unity to access everything from Blender through CBody?? ###SOON
		_oBA = (byte[])_memBA.L;					// Obtain convenience pointer to beginning of raw memory obtained from Blender
		_nLength = _memBA.L.Length;
		CheckMagicNumber_Begin();					// Ensure stream starts with proper magic number
	}


	//---------------------------------------------------------------------------	STATIC HOMOGENOUS ARRAY RETRIEVAL
	public static List<ushort> GetArray_USHORT(string sNameBlenderModule, string sBlenderAccessString) {        // Deserialize a Blender mesh's previously-created array
		CByteArray oByteArray = new CByteArray(sNameBlenderModule, sBlenderAccessString);
		int nArrayElements = oByteArray.GetLengthPayload() / sizeof(ushort);
		List<ushort> aBlenderArray = new List<ushort>(nArrayElements);
		if (nArrayElements > 0) {
			for (int nArrayElement = 0; nArrayElement  < nArrayElements; nArrayElement++)
				aBlenderArray.Add(oByteArray.ReadUShort());
		} else {
			Debug.LogWarningFormat("###WARNING: CByteArray.GetArray() gets zero-sided array on '{0}'", oByteArray._sBlenderAccessString);
		}
		return aBlenderArray;
	}

	public static List<int> GetArray_INT(string sNameBlenderModule, string sBlenderAccessString) {
		CByteArray oByteArray = new CByteArray(sNameBlenderModule, sBlenderAccessString);
		int nArrayElements = oByteArray.GetLengthPayload() / sizeof(int);
		List<int> aBlenderArray = new List<int>(nArrayElements);
		if (nArrayElements > 0) {
			for (int nArrayElement = 0; nArrayElement  < nArrayElements; nArrayElement++)
				aBlenderArray.Add(oByteArray.ReadInt());
		} else {
			Debug.LogWarningFormat("###WARNING: CByteArray.GetArray() gets zero-sided array on '{0}'", oByteArray._sBlenderAccessString);
		}
		return aBlenderArray;
	}

	public static List<float> GetArray_FLOAT(string sNameBlenderModule, string sBlenderAccessString) {
		CByteArray oByteArray = new CByteArray(sNameBlenderModule, sBlenderAccessString);
		int nArrayElements = oByteArray.GetLengthPayload() / sizeof(float);
		List<float> aBlenderArray = new List<float>(nArrayElements);
		if (nArrayElements > 0) {
			for (int nArrayElement = 0; nArrayElement  < nArrayElements; nArrayElement++)
				aBlenderArray.Add(oByteArray.ReadFloat());
		} else {
			Debug.LogWarningFormat("###WARNING: CByteArray.GetArray() gets zero-sided array on '{0}'", oByteArray._sBlenderAccessString);
		}
		return aBlenderArray;
	}



	//---------------------------------------------------------------------------	PRIMITIVES
	public ushort	ReadUShort()	{ ushort nVal = BitConverter.ToUInt16(_oBA, _nPosRead); _nPosRead+=2; return nVal;  }
	public short	ReadShort()		{ short  nVal = BitConverter.ToInt16 (_oBA, _nPosRead); _nPosRead+=2; return nVal;  }
	public uint		ReadUInt()		{ uint   nVal = BitConverter.ToUInt32(_oBA, _nPosRead); _nPosRead+=4; return nVal;  }
	public int		ReadInt()		{ int    nVal = BitConverter.ToInt32 (_oBA, _nPosRead); _nPosRead+=4; return nVal;  }
	public float	ReadFloat()		{ float  nVal = BitConverter.ToSingle(_oBA, _nPosRead); _nPosRead+=4; return nVal;  }
	public double	ReadDouble()	{ double nVal = BitConverter.ToDouble(_oBA, _nPosRead); _nPosRead+=8; return nVal;  }
	public byte		ReadByte()		{ byte nVal = _oBA[_nPosRead]; _nPosRead++; return nVal; }
	public char		ReadChar()		{ return (char)ReadByte(); }


	//---------------------------------------------------------------------------	COMPOSITES
	public string ReadString() {		// Used to serialize strings packed by struct.pack in Blender Python.  (First byte is string lenght)
		byte nLen = ReadByte();
		StringBuilder strBuilder = new StringBuilder();
		for (byte nChar = 0; nChar < nLen; nChar++)
			strBuilder.Append(ReadChar());			//###IMPROVE: All in one go
		return strBuilder.ToString();
	}
	public Vector3 ReadVector() {
		Vector3 vec;
		vec.x = ReadFloat();
		vec.y = ReadFloat();
		vec.z = ReadFloat();
		return vec;
	}
	public Quaternion ReadQuaternion() {
		Quaternion quat;
		quat.x = ReadFloat();
		quat.y = ReadFloat();
		quat.z = ReadFloat();
		quat.w = ReadFloat();
		return quat;
	}



	//---------------------------------------------------------------------------	UTILITY
	void CheckMagicNumber_Begin() {
		ushort nMagic = ReadUShort();
		if (nMagic != CByteArray.C_CByteArray_StreamBegin)
			CUtility.ThrowException(String.Format("ERROR in CByteArray('{0}').  Invalid transaction beginning magic number!", _sBlenderAccessString));
	}
	public void CheckMagicNumber_End() {
		ushort nMagic = ReadUShort();
		if (nMagic != CByteArray.C_CByteArray_StreamEnd)
			CUtility.ThrowException(String.Format("ERROR in CByteArray('{0}').  Invalid transaction end magic number!", _sBlenderAccessString));
	}
	public int GetLengthPayload() {
		return _nLength - 2 * sizeof(ushort);			// Payload is the raw length from Blender minus the two ushort magic numbers.
	}
}

//###OBS: Abandoned as it's too slow to have to test type every loop iteration
//public List<T> GetTypedArray<T>() {
//	Type oTypeTemplate = typeof(T);
//	int nArrayElements = ReadInt() / System.Runtime.InteropServices.Marshal.SizeOf(oTypeTemplate);      //###LEARN: How to get the sizeof of primitive type!
//	List<T> aBlenderArray = new List<T>(nArrayElements);
//	if (nArrayElements > 0) {
//		for (int nArrayElement = 0; nArrayElement  < nArrayElements; nArrayElement++) {
//			if (oTypeTemplate == typeof(ushort))
//				aBlenderArray.Add((T)ReadUShort());
//		}
//	}
//	CheckMagicNumber_End();
//	return aBlenderArray;
//}
