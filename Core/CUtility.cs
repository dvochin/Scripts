using UnityEngine;
//using UnityEditor;
using System;
using System.IO;
//using System.Text;
//using System.Collections.Generic;
//using System.Reflection;
//using System.Runtime.Serialization.Formatters.Binary;

#pragma warning disable 162         // "Unreacheable Code Detected"

public class CUtility {         // Collection of static utility functions
    static Color[] _aColorsForDebug = {     //###IMPROVE: Add more RBG colors
            Color.red,
            Color.green,
            Color.blue,
            new Color32(128, 000, 000, 255),     // Dark Red
            new Color32(000, 128, 000, 255),     // Dark Green
            new Color32(000, 000, 128, 255),     // Dark Blue
            Color.cyan,
            Color.magenta,
            Color.yellow,
            new Color32(255, 153, 051, 255),     // Orange
            //Color.gray,
            //Color.white,
        };
    static int _nLastRandomColorProvided = 0;

    #region === Node / Component Creation ===
    public static Component FindOrCreateNode(GameObject oParentGO, string sName, Type oType) {
		if (oParentGO == null)
			CUtility.ThrowException("*E: FindOrCreateNode() called with no parent GameObject!");
		return CUtility.FindOrCreateNode(oParentGO.transform, sName, oType);
	}

	public static Component FindOrCreateNode(Transform oNodeParent, string sName, Type oType) {
		if (oNodeParent == null)
			CUtility.ThrowException("*E: FindOrCreateNode() called with no parent Transform!");
		Transform oChildTran = oNodeParent.Find(sName);
		if (oChildTran == null) {
			GameObject oChildGO = (oType != null) ? new GameObject(sName, oType) : new GameObject(sName);
			oChildTran = oChildGO.transform;
			oChildTran.SetParent(oNodeParent.transform);
		}
		return (oType != null) ? oChildTran.GetComponent(oType) : oChildTran.transform;
	}

	public static Component FindOrCreateComponent(GameObject oGO, Type oType) {
		if (oGO != null) {
			Component oComp = oGO.GetComponent(oType);
			if (oComp == null)
				oComp = oGO.AddComponent(oType);
			return oComp;
		} else {
			CUtility.ThrowException("*Err: FindOrCreateComponent() was called with a null gameObject!");
			return null;
		}
	}
	public static Component FindOrCreateComponent(Transform oNode, Type oType) {
		if (oNode != null) {
			return FindOrCreateComponent(oNode.gameObject, oType);
		} else {
			CUtility.ThrowException("*Err: FindOrCreateComponent() was called with a null transform!");
			return null;
		}
	}

	public static Component FindComponentInParents(Transform oNodeStart, Type oTypeComponent, string sCallingCodeName) {		// Iterate up the parent chain to return the first ancestor with a component of the provided type
		Transform oNode = oNodeStart;
		while (oNode != null) {
			Component oComp = oNode.GetComponent(oTypeComponent);
			if (oComp != null)
				return oComp;
			oNode = oNode.parent;
		}
		if (sCallingCodeName != null)
			CUtility.ThrowException("FindComponentInParents() could not find component " + oTypeComponent + " in " + sCallingCodeName);
		return null;
	}

    public static void DestroyComponent(Component oComponent) {
        if (oComponent == null)
            return;
        UnityEngine.Object.Destroy(oComponent);
    }

    public static uFlex.FlexParticles CreateFlexObjects(GameObject oGO, uFlex.IFlexProcessor iFlexProcessor, int nParticleCount, uFlex.FlexInteractionType nFlexInterationType, Color oColor) {
        uFlex.FlexParticles oFlexParticles = CUtility.FindOrCreateComponent(oGO, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;
        oFlexParticles.m_maxParticlesCount = oFlexParticles.m_particlesCount = nParticleCount;                             // The non-edge particle are the ones that require driving between skinned and visible mesh.
        oFlexParticles.m_type = uFlex.FlexBodyType.Other;
        oFlexParticles.m_particles = new uFlex.Particle[nParticleCount];
        oFlexParticles.m_restParticles = new uFlex.Particle[nParticleCount];
        oFlexParticles.m_colours = new Color[nParticleCount];
        oFlexParticles.m_velocities = new Vector3[nParticleCount];
        oFlexParticles.m_densities = new float[nParticleCount];
        oFlexParticles.m_particlesActivity = new bool[nParticleCount];
        oFlexParticles.m_colour = oColor;
        oFlexParticles.m_interactionType = nFlexInterationType;				// The simulated particles collide with everything (other than ourselves)
        oFlexParticles.m_collisionGroup = -1;								// Flex runtime will allocate to its own collision group to collide with everything
        oFlexParticles.m_bounds.SetMinMax(new Vector3(-1,-1,-1), new Vector3(1,1,1));        //###CHECK: Better with some reasonable values than zero?

        ////=== Add particle renderer component for debug visualization ===
        //uFlex.FlexParticlesRenderer oFlexPartRend = CUtility.FindOrCreateComponent(oGO, typeof(uFlex.FlexParticlesRenderer)) as uFlex.FlexParticlesRenderer;
        //oFlexPartRend.m_size = CGame.INSTANCE.particleSpacing;
        //oFlexPartRend.m_radius = oFlexPartRend.m_size / 2.0f;
        //oFlexPartRend.enabled = false;           // Hidden by default

		//=== Create Flex Processor so we can update particles ===
		if (iFlexProcessor != null) {
			uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(oGO, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
			oFlexProc._oFlexProcessor = iFlexProcessor;
		}
		return oFlexParticles;
    }
    #endregion

    #region === Find ===
    public static Transform FindNodeByName(Transform oNode, string sNodeName) {
		if (oNode.name == sNodeName)
			return oNode;
		for (int nChild = 0; nChild < oNode.childCount; nChild++) {
			Transform oNodeFound = FindNodeByName(oNode.GetChild(nChild), sNodeName);
			if (oNodeFound != null)
				return oNodeFound;
		}
		return null;
	}

	public static Transform FindChild(Transform oParentT, string sChildPath) {
		Transform oChildT = oParentT.Find(sChildPath);
		if (oChildT == null)
			CUtility.ThrowException(String.Format("FindChild(Parent='{0}', ChildPath='{1}'", oParentT.name, sChildPath));
		return oChildT;
	}

	 public static GameObject FindObject(/*this*/ GameObject oParentGO, string sNameNode) {     //###INFO: 'this' in function parameter is a 'extension method'!  See http://stackoverflow.com/questions/3045242/this-in-function-parameter
		// Finds the node 'sNameNode' in the descendents of 'oParentGO'.  Can find inactive nodes!!
		//object o = oParentGO.GetComponentsInChildren(typeof(Transform), true);		
		object[] aNodesT = oParentGO.GetComponentsInChildren(typeof(Transform), true);		//###INFO: last argument = include inactive!
		 foreach(Transform oNodeT in aNodesT)
			 if(oNodeT.name == sNameNode)
				  return oNodeT.gameObject;
		 return null;
	 }
	#endregion

	#region === Bones ===
	public static Transform TransferBone(Transform oBoneOld, Transform oBoneNewRoot) {		// Returns a transform at the same relative path of a bone (provided by SkinnedMeshRenderer.bones[i]) but rooted at 'oBoneNewRoot'.  Assumes an identically-structured bone tree between the objects (Running this function over all bones of an object  will make it move along the object at 'oBoneNewRoot')
		string sBonePath = "";
		Transform oNodeIterator = oBoneOld;
		while (oNodeIterator.parent.parent != null) {									//###WEAK!!!!: We assume clothing item under body node... will this always be true?? Iterate up the parent chain to construct the relative path all the way to just before the top-level object
			sBonePath = oNodeIterator.name + "/" + sBonePath;
			oNodeIterator = oNodeIterator.parent;
		}
		sBonePath = sBonePath.TrimEnd('/');
		Transform oBoneNew = oBoneNewRoot.Find(sBonePath);
		if (oBoneNew == null)
			CUtility.ThrowException("*Err: TransferBone could not transfer bone '" + sBonePath + "' to new root '" + oBoneNewRoot + "'");
		return oBoneNew;
	}

	//public static void TransferBones(ref SkinnedMeshRenderer oSkinMeshRend, Transform oBoneNewRoot) {	// Transfer all bones of provided skin renderer to a new root
	//	Transform[] aBones = oSkinMeshRend.bones;
	//	for (int nBone = 0; nBone < oSkinMeshRend.bones.Length; nBone++)								// Iterate through all bones of this skinned mesh to remap them to our body's bones.  (This will make clothing skinned mesh 'move along' with body)
	//		aBones[nBone] = TransferBone(aBones[nBone], oBoneNewRoot);
	//	oSkinMeshRend.bones = aBones;
	//	//###CHECK: ###BUG?: In some contexts, rootbone below was already transfered... doesn't occur with clothing... verify!
	//	oSkinMeshRend.rootBone = TransferBone(oSkinMeshRend.rootBone, oBoneNewRoot);	// Also remap the root bone similarly
	//}

	//public static int FindBoneByName(ref SkinnedMeshRenderer oSkinMeshRend, string sBoneName) {
	//	Transform[] aBones = oSkinMeshRend.bones;
	//	for (int nBone = 0; nBone < oSkinMeshRend.bones.Length; nBone++)								// Iterate through all bones of this skinned mesh to remap them to our body's bones.  (This will make clothing skinned mesh 'move along' with body)
	//		if (aBones[nBone].name == sBoneName)
	//			return nBone;
	//	CUtility.ThrowException("FindBoneByName() could not find bone '" + sBoneName + "' in " + oSkinMeshRend.gameObject.name);
	//}

	public static Transform FindSymmetricalBodyNode(GameObject oNodeSrc) {
		// From a node like <BodyName>/Root/Sex/hip/abdomen/chest/lCollar/rShldr/lForeArm/lHand would return node at <BodyName>/Root/Sex/hip/abdomen/chest/rCollar/rShldr/rForeArm/rHand.  Only works on DAZ-based bone structure naming convention!
		// Testing code: Transform oNodeSym = CUtility.FindSymmetricalBodyNode(GameObject.Find("Woman8/Root/Sex/hip/abdomen/chest/lCollar/lShldr/lForeArm/lHand"), "chestUpper");
		char sPrefixThisSide  = oNodeSrc.name[0];
		char sPrefixOtherSide = (sPrefixThisSide == 'l') ? 'r' : 'l';
		string sPathToBranchPoint = "";
		Transform oNodeIterator = oNodeSrc.transform;
		while (oNodeIterator.transform.name[0] == sPrefixThisSide) {		//###CHECK: Reliable way to determine when we're still l/r split??
			sPathToBranchPoint = sPrefixOtherSide + oNodeIterator.name.Substring(1) + "/" + sPathToBranchPoint;
			oNodeIterator = oNodeIterator.parent;
		}
		if (sPathToBranchPoint.Length > 0)
			sPathToBranchPoint = sPathToBranchPoint.Substring(0, sPathToBranchPoint.Length - 1);		// Remove trailing '/'
		Transform oNodeDst = oNodeIterator.Find(sPathToBranchPoint);
		if (oNodeDst == null)
			CUtility.ThrowException("**Err: FindSymmetricalBodyNode() could not find symmetry node for " + oNodeSrc.name);
		return oNodeDst;
	}
	#endregion

	#region === Value Changes ===
	//public static bool CheckIfChanged(ref float nValueNew, ref float nValueOld, float nMin, float nMax, string sMsg) {			// Simple utility function that checks if the values have changed and if so
	//	nValueNew = Mathf.Clamp(nValueNew, nMin, nMax);
	//	if (nValueOld == nValueNew)
	//		return false;
	//	nValueOld = nValueNew;
	//	if (G.C_DisplayOnCheckIfChanged)
	//		Debug.Log("Changed: " + sMsg + "=" + nValueNew + " (from " + nValueOld + ")");
	//	return true;
	//}

	//public static bool CheckIfChanged(bool nValueNew, ref bool nValueOld, string sMsg) {
	//	if (nValueOld == nValueNew)
	//		return false;
	//	nValueOld = nValueNew;
	//	if (G.C_DisplayOnCheckIfChanged)
	//		Debug.Log("Changed: " + sMsg + "=" + nValueNew + " (from " + nValueOld + ")");
	//	return true;
	//}
	#endregion

	#region === Materials ===
	//public static int FindMaterialIndexByMaterialName(ref SkinnedMeshRenderer oSkinMeshRend, string sMaterialName) {
	//	string sMatNamePlusInstancePostFix = sMaterialName + " (Instance)";			//###WEAK? Frequently Unity will INSTANCE material and append this prefix... look into why??
	//	for (int nMat = 0; nMat < oSkinMeshRend.sharedMaterials.Length; nMat++) {
	//		Material oMat = oSkinMeshRend.sharedMaterials[nMat];
	//		if (oMat.name == sMaterialName || oMat.name == sMatNamePlusInstancePostFix)
	//			return nMat;
	//	}
	//	CUtility.ThrowException("FindMaterialIndexByMaterialName() could not find material " + sMaterialName + " on skinned mesh " + oSkinMeshRend.transform.name);
	//}
	//public static void CopyMaterial(Material oMatSrc, ref Material oMatDst) {
	//	oMatDst.CopyPropertiesFromMaterial(oMatSrc);		//###INFO: How to copy a material	//###WEAK: Not transfering material names!
	//	oMatDst.name = oMatSrc.name;
	//}
	#endregion

	#region === Debug Rendering ===
	//public static void BakeSkinnedMeshAndShow(Transform oNodeParent, string sNodeName, ref Mesh oMesh, Material oMat, bool bMakeVisible) {		//###OBS?
	//	//###INFO: This will draw what is baked...  Useful for debugging!	
	//	GameObject oMeshBakedDumpGO = new GameObject(sNodeName, typeof(MeshFilter), typeof(MeshRenderer));
	//	oMeshBakedDumpGO.transform.SetParent(oNodeParent);
	//	oMeshBakedDumpGO.GetComponent<MeshFilter>().mesh = oMesh;
	//	MeshRenderer oMeshRend = oMeshBakedDumpGO.GetComponent<MeshRenderer>();
	//	int nNumMaterials = 25;						// Give plenty of materials so every submesh is drawn
	//	oMeshRend.sharedMaterials = new Material[nNumMaterials];			//###CHECK!
	//	for (int nMat = 0; nMat < nNumMaterials; nMat++)
	//		oMeshRend.sharedMaterials[nMat] = oMat;
	//	oMeshRend.enabled = bMakeVisible;
	//	Debug.Log("BakeSkinnedMeshAndShow() created: " + sNodeName);
	//}

    public static Color GetRandomColor() {
        //int nColorChoice = (int)(UnityEngine.Random.value * _aColorsForDebug.Length);
        int nColorChoice = _nLastRandomColorProvided++;
        if (_nLastRandomColorProvided == _aColorsForDebug.Length)
            _nLastRandomColorProvided = 0;
        return _aColorsForDebug[nColorChoice];
    }
	#endregion

	#region === Reflection ===
	//public static List<FieldInfo> GetSuperPublicFields(object o) {
	//	//=== Returns the 'super public' fields of an object that are controlled programmatically by reflection (e.g. PhysX properties) ===
	//	List<FieldInfo> aFieldInfos = new List<FieldInfo>();
	//	Type oType = o.GetType();
	//	foreach (FieldInfo oFieldInfo in oType.GetFields()) {		//###IMPROVE: Add test for 'public'
	//		if (oFieldInfo.Name.StartsWith("__")) {
	//			aFieldInfos.Add(oFieldInfo);
	//		}
	//	}
	//	return aFieldInfos;
	//}
	#endregion

	#region === Normals ===
	//public static Vector3 CalculateNormal(ref Vector3 vec0, ref Vector3 vec1, ref Vector3 vec2) {
	//	Vector3 vecNormal = Vector3.Cross(vec1 - vec0, vec2 - vec0);
	//	vecNormal *= 1000;						//###INFO: Normalize the vector when too small!!! (A bug?)   LookAt() needs a large enough vector to 'look' so we multiply then normalize
	//	vecNormal.Normalize();
	//	return vecNormal;
	//}
	#endregion

	#region === Serialize Actors ===
	public static void Serialize(Stream oStream, Vector3 vec) {		//###CHECK: Why the heck is Vector3 not serializable!  Three floats!!  A better way???
		if (oStream.CanWrite) {
			oStream.Write(BitConverter.GetBytes(vec.x), 0, 4);
			oStream.Write(BitConverter.GetBytes(vec.y), 0, 4);
			oStream.Write(BitConverter.GetBytes(vec.z), 0, 4);
		}
	}
	public static Vector3 DeserializeVec(Stream oStream) {			//###IMPROVE?: Merge ser/deser into one smart call??
		Vector3 vec;
		byte[] aBuf = new byte[4];
		oStream.Read(aBuf, 0, 4); vec.x = BitConverter.ToSingle(aBuf, 0);
		oStream.Read(aBuf, 0, 4); vec.y = BitConverter.ToSingle(aBuf, 0);
		oStream.Read(aBuf, 0, 4); vec.z = BitConverter.ToSingle(aBuf, 0);
		return vec;
	}

	public static void Serialize(Stream oStream, Quaternion quat) {
		if (oStream.CanWrite) {
			oStream.Write(BitConverter.GetBytes(quat.x), 0, 4);
			oStream.Write(BitConverter.GetBytes(quat.y), 0, 4);
			oStream.Write(BitConverter.GetBytes(quat.z), 0, 4);
			oStream.Write(BitConverter.GetBytes(quat.w), 0, 4);
		}
	}
	public static Quaternion DeserializeQuat(Stream oStream) {
		Quaternion quat;
		byte[] aBuf = new byte[4];
		oStream.Read(aBuf, 0, 4); quat.x = BitConverter.ToSingle(aBuf, 0);
		oStream.Read(aBuf, 0, 4); quat.y = BitConverter.ToSingle(aBuf, 0);
		oStream.Read(aBuf, 0, 4); quat.z = BitConverter.ToSingle(aBuf, 0);
		oStream.Read(aBuf, 0, 4); quat.w = BitConverter.ToSingle(aBuf, 0);
		return quat;
	}

	public static void Serialize(Stream oStream, Transform oNode, bool bSerializeScale) {   //###DESIGN!!! LOCAL???
		if (oStream.CanWrite) {
			Serialize(oStream, oNode.localPosition);
			Serialize(oStream, oNode.localRotation);
			if (bSerializeScale)
				Serialize(oStream, oNode.localScale);
		} else {
			oNode.localPosition = DeserializeVec(oStream);
			oNode.localRotation = DeserializeQuat(oStream);
			if (bSerializeScale)
				oNode.localScale = DeserializeVec(oStream);
			else
				oNode.localScale = new Vector3(1, 1, 1);
		}
	}

	public static float DeserializeFloat(Stream oStream) {  //###IMPROVE: Better way to do this from straight stream??
		byte[] aBuf = new byte[4];                          //###IMPROVE: Static buffer to avoid gc?
		oStream.Read(aBuf, 0, 4);
		return BitConverter.ToSingle(aBuf, 0);
	}
	#endregion

	#region === Serialize Actors ByteArrays (Blender <-> Unity) ###OBS ===
	//public static string BlenderStream_ReadStringPascal(ref byte[] aBytes, ref int nOffset) {		// Used to serialize strings packed by struct.pack in Blender Python.  (First byte is string length)
	//	byte nLen = aBytes[nOffset]; nOffset++;
	//	StringBuilder strBuilder = new StringBuilder();
	//	for (byte nChar = 0; nChar < nLen; nChar++) {
	//		strBuilder.Append((char)aBytes[nOffset]);
	//		nOffset++;
	//	}
	//	return strBuilder.ToString();
	//}
	//public static Vector3 ByteArray_ReadVector(ref byte[] aBytes, ref int nOffset) {		// Used to serialize strings packed by struct.pack in Blender Python.  (First byte is string length)
	//	Vector3 vec;
	//	vec.x = BitConverter.ToSingle(aBytes, nOffset); nOffset += 4;
	//	vec.y = BitConverter.ToSingle(aBytes, nOffset); nOffset += 4;
	//	vec.z = BitConverter.ToSingle(aBytes, nOffset); nOffset += 4;
	//	return vec;
	//}
	//public static Quaternion ByteArray_ReadQuaternion(ref byte[] aBytes, ref int nOffset) {		// Used to serialize strings packed by struct.pack in Blender Python.  (First byte is string length)
	//	Quaternion quat;
	//	quat.x = BitConverter.ToSingle(aBytes, nOffset); nOffset += 4;
	//	quat.y = BitConverter.ToSingle(aBytes, nOffset); nOffset += 4;
	//	quat.z = BitConverter.ToSingle(aBytes, nOffset); nOffset += 4;
	//	quat.w = BitConverter.ToSingle(aBytes, nOffset); nOffset += 4;
	//	return quat;
	//}
	#endregion

	#region === Strings ===
	public static string ConvertCamelCaseToHumanReadableString(string sCamelCase) {		// Returns a string like _MyCamelCaseVariable into "My Camel Case Variable" for GUI display of our internal variable (typically used by reflection)
		string sHumanReadable = "";
		foreach (char ch in sCamelCase) {
			if (ch >= 'A' && ch <= 'Z')
				sHumanReadable += " " + ch;
			else if (ch != '_')
				sHumanReadable += ch;
		}
		return sHumanReadable.Trim();
	}
	public static string[] SplitCommaSeparatedPythonListOutput(string sPythonListOutput) {  // Takes the output of python command 'str(aMyList)' that looks like "['foo', 'bar']" and returns a string array containing "foo" and "bar"
		if (sPythonListOutput.Length < 2) {
			Debug.LogWarning("#Warning: Invalid string length in SplitCommaSeparatedPythonListOutput()");
			return null;
		}
		sPythonListOutput = sPythonListOutput.Substring(1, sPythonListOutput.Length - 2);		// Remove the [ and ] from the python output
		string[] aSeparators = new string[] { ", " };       //###IMPROVE: Remove space after comma to save bandwidth??
		string[] aElements = sPythonListOutput.Split(aSeparators, StringSplitOptions.RemoveEmptyEntries);     // Python str(aMyList) separates each item with comma and a space
		for (int nElement = 0; nElement < aElements.Length; nElement++)			// Remove the single quotes (') that python inserted at the beginning and end of each element
			aElements[nElement] = aElements[nElement].Substring(1, aElements[nElement].Length - 2);
		return aElements;
	}
	#endregion

	#region === Color Space Conversion ===
	//Color col1F = CUtility.Color_HSVtoRGB(135, 20, 213); Color32 col1 = new Color32((byte)(255 * col1F.r), (byte)(255 * col1F.g), (byte)(255 * col1F.b), 0);		//###BUG Conversion not working... negative numbers, WTF???
	//Color col2F = CUtility.Color_HSVtoRGB(02, 20, 213); Color32 col2 = new Color32((byte)(255 * col2F.r), (byte)(255 * col2F.g), (byte)(255 * col2F.b), 0);

   // public static Color Color_HSVtoRGB(float h, float s, float v) {		//###INFO: From http://www.cs.rit.edu/~ncs/color/t_convert.html		
   //     Color calcColour = new Color( 1, 1, 1, 1 );
       
   //     int i = 0;
   //     float f = 0;
   //     float p = 0;
   //     float q = 0;
   //     float t = 0;
       
   //     if ( s == 0 ) {		// achromatic (grey)
   //         calcColour.r = v;
   //         calcColour.g = v;
   //         calcColour.b = v;
   //         return calcColour;
   //     }
       
   //     h /= 60;
   //     i = Mathf.FloorToInt( h );
   //     f = h - i;
   //     p = v * ( 1 - s );
   //     q = v * ( 1 - ( s * f ) );
   //     t = v * ( 1 - ( s * ( 1 - f ) ) );
     
   //     switch( i ) {
   //         case 0 :
   //             calcColour.r = v;
   //             calcColour.g = t;
   //             calcColour.b = p;
			//break;
           
   //         case 1 :
   //             calcColour.r = q;
   //             calcColour.g = v;
   //             calcColour.b = p;
   //         break;
           
   //         case 2 :
   //             calcColour.r = p;
   //             calcColour.g = v;
   //             calcColour.b = t;
   //         break;
           
   //         case 3 :
   //             calcColour.r = p;
   //             calcColour.g = q;
   //             calcColour.b = v;
   //         break;
           
   //         case 4 :
   //             calcColour.r = t;
   //             calcColour.g = p;
   //             calcColour.b = v;
   //         break;
           
   //         default :
   //             calcColour.r = v;
   //             calcColour.g = p;
   //             calcColour.b = q;
   //         break;
   //     }
       
   //     return calcColour;
   // }
     
   // public static Vector3 Color_RGBtoHSV(float r, float g, float b) {
   //     Vector3 calcColour = new Vector3( 0, 1, 0 ); // H, S, V
       
   //     float min = 0;
   //     float max = 0;
   //     float delta = 0;
       
   //     min = Mathf.Min( r, g, b );
   //     max = Mathf.Max( r, g, b );
   //     calcColour.z = max; // V
       
   //     delta = max - min;
       
   //     if ( max != 0 ) {
   //         calcColour.y = delta / max; // S
   //     } else {
   //         calcColour.y = 0; // S		// r = g = b = 0
   //         calcColour.x = -1; // H
   //         return calcColour;
   //     }
       
   //     if ( r == max )
   //         calcColour.x = ( g - b ) / delta; // H
   //     else if ( g == max )
   //         calcColour.x = 2 + ( ( b - r ) / delta ); // H
   //     else
   //         calcColour.x = 4 + ( ( r - g ) / delta ); // H
       
   //     calcColour.x *= 60; // H
   //     if ( calcColour.x < 0 )
   //         calcColour.x += 360;
   //     return calcColour;
   // }
    #endregion
	
    #region === UI ===
    public static void WndPopup_Create(CUICanvas oCanvas, EWndPopupType eWndPopupType, CObject[] aObjects, string sNamePopup, float nX = -1, float nY = -1) {

		oCanvas.CreatePanel("TODO", "TODO", aObjects);			//###DEV21:!!!!!!!


  //      //=== Construct the dialog's content dependent on what type of dialog it is ===
  //      CUIPanel oPanel = CUIPanel.Create(oCanvas);           //####DESIGN!  ####SOON ####CLEANUP?
  //      int nRows = 0;
  //      int nPropGrps = 0;
  //      foreach (CObject oObj in aObjects) {
  //          foreach (CPropGrp oPropGrp in oObj._aPropGrps) {   //###BUG!: Inserts one extra!  Why??
  //              oPropGrp._oUIPanel = oPanel;                    //####IMPROVE ####MOVE??
  //              //////////oPropGrp.CreateWidget(oListBoxContent);
  //              foreach (int nPropID in oPropGrp._aPropIDs) {
  //                  CProp oProp = oObj.PropFind(nPropID);
  //                  nRows += oProp.CreateWidget(oPropGrp);
  //              }
  //              nPropGrps++;
  //          }
  //      }
  //      oCanvas.transform.position = CGame.INSTANCE._oCursor.transform.position;
  //      oCanvas.transform.rotation = Camera.main.transform.rotation;
    }

	public static RaycastHit RaycastToCameraPoint2D(Vector3 vecScreenPoint, uint nLayerMask) {    			
		// Builds a ray from camera point to the viewspace position of the mouse cursor into the 3D scene... Returns the first physics collider it finds in the requested layers
	    Ray oRay = Camera.main.ScreenPointToRay(vecScreenPoint);
	    RaycastHit oRayHit;
	    Physics.Raycast(oRay, out oRayHit, Mathf.Infinity, (int)nLayerMask);		// Casts the ray and get the first game object hit
		return oRayHit;
	}
	#endregion

	#region === DEBUGGING ===
	public static void ThrowException(string sMsg) {
		Debug.LogError("[EXCEPTION] " + sMsg);
		//EditorApplication.isPaused = true;			//###INFO: How to programatically pause game in Unity editor.  (Doesn't work in player)
		Debug.Break();
		Debug.LogError("[PLACE BREAKPOINT HERE]");	//###NOTE: Put breakpoint here to catch all exception and look up stack tree.
	}

	public static void ThrowExceptionF(string sMsg, params object[] aArgs) {		//###INFO: How to accept and process variable arguments!
		sMsg = "[EXCEPTION] " + sMsg;
		//EditorApplication.isPaused = true;
		Debug.LogErrorFormat(sMsg, aArgs);
		Debug.Break();
		Debug.LogError("[PLACE BREAKPOINT HERE]");	//###NOTE: Put breakpoint here to catch all exception and look up stack tree.
	}

	public static void ThrowException(Exception e) {
		Debug.LogException(e);
		//EditorApplication.isPaused = true;		//###IMPROVE: Wrap with ifdef!
		Debug.Break();
		Debug.LogError("[PLACE BREAKPOINT HERE]");	//###NOTE: Put breakpoint here to catch all exception and look up stack tree.
	}

	public static void PauseGame() {
		if (CGame.INSTANCE.D_CanPause)
			Debug.Break();
	}
	#endregion
}


//	public static void CopyComponentTree(GameObject oTreeSrc, GameObject oTreeDst) {
//		Component[] aSrc = oTreeSrc.GetComponentsInChildren<Transform>();		//###WEAK: Assumes trees are of the same topology!!
//		Component[] aDst = oTreeDst.GetComponentsInChildren<Transform>();
//		int nSrc = 0;
//		//for (int nDst = 0; nDst<aDst.Length; nDst++) {
//		//Transform oDst = (Transform)aDst[nDst];
//		foreach (Transform oDst in aDst) {
//			Transform oSrc = (Transform)aSrc[nSrc];		//###BUG???: Why index out of bound exception here??
//			if (oSrc.name == oDst.name) {
//				Debug.Log("CompCopy: " + oSrc.name);
//				oDst.localPosition = oSrc.localPosition;
//				oDst.localRotation = oSrc.localRotation;
//				oDst.localScale = oSrc.localScale;
//				nSrc++;
//			} else {
//				Debug.LogError("ERROR: Unmatching bones.  Src=" + oSrc.name + "  Dst=" + oDst.name);
//			}
//		}
//	}
//	
//	public static Component FindOrCreateComponentFromCopy(GameObject oGO, Type oType, Component oCompSrc) {		//###MOVE?
//		Component oCompDst = CUtility.FindOrCreateComponent(oGO, oType);
//		EditorUtility.CopySerialized(oCompSrc, oCompDst);
//		return oCompDst;
//	}
//
//	public static Component CopyComponent(GameObject oSrc, GameObject oDst, Type oType) {
//		Component oCompSrc = oSrc.GetComponent(oType);			
//		Component oCompDst = FindOrCreateComponentFromCopy(oDst, oType, oCompSrc);
//		return oCompDst;
//	}

//public class Prop : Attribute {					//###OBS???
//	//public string Description { get; set; }			//###INFO: From technique at http://stackoverflow.com/questions/4879521/creating-custom-attribute-in-c-sharp
//	public string Description;
//	public float Min;
//	public float Max;
//}