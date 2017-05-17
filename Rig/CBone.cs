/*###DISCUSSION: CBone
=== LAST ===

=== NEXT ===

=== TODO ===
- Port the old config joint driver to new code.
- Port the old pin system to new code
- Old code had actors defining proper limits!  No longer there!
	- Maybe a central generated code function is best / safest?

=== LATER ===

=== IMPROVE ===

=== NEEDS ===
- Need to automatically configure its D6 configurable joint to parent
- Need to add facilities later on to guild Flex softbody particles (e.g. finger bone guiding finger particles, etc)

=== DESIGN ===

=== IDEAS ===
- Could CBone be of any help for 'fake bones' such as Vagina triangulation pins, etc?

=== LEARNED ===

=== PROBLEMS ===

=== QUESTIONS ===
- Do we still need to drive arm bones to 'suggest poses' or can we find way to make pin do all the work (perhaps taking longer route?)

=== WISHLIST ===
- Have one bone rig for both men and women!
	- Then how do we manage different bone chains? (e.g. penis and vagina)
*/

using UnityEngine;


public class CBoneRot {		
	public CBone		_oBone;					// Our parent bone.  Manages / owns this bone rotation definition

	public char			_chAxis;				// Axis of rotation about our owning bone.  Must be 'X', 'Y' or 'Z'
	public char			_chAxisDAZ;				// Axis of rotation about our owning bone in the DAZ domain.  (Enables us to test proper bone angles by importing raw DAZ pose dumps that contain DAZ-domain bone rotation axis)
	public bool			_bAxisNegated;			// If true _chAxis is negated instead of positive
	public byte			_nAxis;					// 0 for X, 1 for Y, 2 for Z
	public string		_sNameRotation;
	public float		_nMin;
	public float		_nMax;
	public float		_nValue;				//###DESIGN20: Keep?

	public CBoneRot(CBone oBone, string sRotationSerialization) {		// Creates a bone rotation from a Blender-provided 'serialization string' statically stored in CBone
		_oBone = oBone;
		sRotationSerialization = "[" + sRotationSerialization + "]";			//###WEAK20: SplitCommaSeparatedPythonListOutput() expects a string wrapped by Python's '[' / ']'.  Wrap our csv string for it to work without clipping our parameters
		string[] aRotationsParams = CUtility.SplitCommaSeparatedPythonListOutput(sRotationSerialization);
		string sAxisAndDirection = aRotationsParams[0];				// Looks like 'X+', 'Z-', 'Y+', etc: the axis of rotation and wheter it is positive or negated
		_chAxis				= sAxisAndDirection[0];
		_bAxisNegated		= (sAxisAndDirection[1] == '-');	// Flags if axis is negated or not
		_nAxis				= (byte)(_chAxis - 'X');			// Break down X,Y,Z axis character into axis number (0 = X, 1 = Y, 2 = Z)
		_chAxisDAZ			= aRotationsParams[1][0];
		_sNameRotation		= aRotationsParams[2];
		_nMin				= float.Parse(aRotationsParams[3]);
		_nMax				= float.Parse(aRotationsParams[4]);
		if (_chAxis == 'Z')										//###WEAK: It appears from observation that all Z rotations are meant to go the other direction... Verify this!  Is this caused because Blender (& DAZ) are Right-handed while Unity is left-handed??
			_bAxisNegated = !_bAxisNegated;
		Debug.LogFormat("-CBoneRot {0}  '{1}'   {2} - {3} ", sAxisAndDirection, _sNameRotation, _nMin, _nMax);
	}
}


public class CBone : MonoBehaviour {
	public Quaternion	_quatBoneRotationStartup;   // The bone rotation at game startup.  Used to rotate from that starting position at each rotation for precision
	public CBoneRot[]	_aBoneRots;                 // Our collection of three-possible rotations where 0 = X, 1 = Y, 2 = Z.  Always of size three if a bone rotation is owned by this bone  (Created at gametime from 
	public string[]		_aBoneRots_Serialized;      // Serialized form of what is expanded in _aBoneRots.  Stored during static creation of CBone objects during Blender update of bones
	public string		_sRotOrder;                 // DAZ-provided order to apply rotation for DAZ pose loading (looks like 'XYZ', 'ZXY', 'YZX', etc)  Essential to apply DAZ poses as DAZ intended but of no consequence to our gameplay as bones are moved via PhysX D6 joints.  We parse through the order of this string at every rotation to apply them in the proper order

	public CBody		_oBody;
	public CBone		_oBoneParent;				// Our parent bone.  Has a 1:1 relationship with the actual transform bone objects we wrap

	static Quaternion C_quatRotBlender2Unity = Quaternion.Euler(-90, 0, 0);				// Quaternion to convert from Blender-domain to Unity domain (so we have our usual Y+ up, Z- Forward from Blender's Z+ up, Y- forward)


	public void DeserializeFromBlenderStaticBoneImportProcedure(ref CByteArray oBA) { 
        Vector3 vecBone  = oBA.ReadVector();              // Bone position itself.

		//=== Read the bone angle as an angle-axis in order to easily traverse Blender-domain to Unity-domain ===
		Vector3 vecRotAxis = oBA.ReadVector();
		float nRotAngle = oBA.ReadFloat();
		Quaternion quatBone = Quaternion.AngleAxis(nRotAngle * 180.0f / Mathf.PI, vecRotAxis);	// Blender sends radians and Unity needs degrees!
		Quaternion quatBoneUnity = quatBone * C_quatRotBlender2Unity;							//###NOTE: Apply the Blender-to-Unity global rotation right off the top so we have our usual Y+ up, Z- Forward from Blender's Z+ up, Y- forward

        Debug.LogFormat("CBone created bone '{0}' under '{1}' with rot:{2:F3},{3:F3},{4:F3},{5:F3} / pos:{6:F3},{7:F3},{8:F3}", gameObject.name, transform.parent.name, quatBoneUnity.x, quatBoneUnity.y, quatBoneUnity.z, quatBoneUnity.w, vecBone.x, vecBone.y, vecBone.z);
        transform.position = vecBone;
        transform.rotation = quatBoneUnity;

		_sRotOrder = oBA.ReadString();

		byte nRotations = oBA.ReadByte();
		if (nRotations > 0) 
			_aBoneRots_Serialized = new string[3];               // We always have this array of size three (0 = X, 1 = Y, 2 = Z) if we have any rotation (have to go in right slot)

		for (int nRotation = 0; nRotation < nRotations; nRotation++) {
			string sRotationSerialization = oBA.ReadString();
			_aBoneRots_Serialized[nRotation] = sRotationSerialization;
		}
	}

	public void Initialize(CBody oBody) {
		_oBody = oBody;
		_aBoneRots = new CBoneRot[3];

		//=== Create our CBoneRot objects from their 'serialized format' (stuffed by Blender into prefab during design-time import procedure) ===
		for (int nRotation = 0; nRotation < _aBoneRots_Serialized.Length; nRotation++) {
			string sRotationSerialization = _aBoneRots_Serialized[nRotation];
			if (sRotationSerialization.Length > 0) { 
				CBoneRot oBoneRot = new CBoneRot(this, sRotationSerialization);
				_aBoneRots[oBoneRot._nAxis] = oBoneRot;
			}
		}

		_quatBoneRotationStartup = transform.localRotation;     // Remeber startup bone rotaton so we start all rotation changes from the startup point.

		//=== Create an debug bone visualizer mesh (to visually show the axis rotations) ===
		if (
			gameObject.name.Contains("hip") ||
			gameObject.name.Contains("pelvis") ||
			gameObject.name.Contains("abdomen") ||
			gameObject.name.Contains("chest") ||
			gameObject.name.Contains("Collar") ||
			gameObject.name.Contains("Shldr") ||
			gameObject.name.Contains("Forearm") ||
			gameObject.name.Contains("Hand") ||
			gameObject.name.Contains("Thigh") ||
			gameObject.name.Contains("Shin") ||
			gameObject.name.Contains("Heel") ||
			gameObject.name.Contains("Thumb") ||
			gameObject.name.Contains("BigToe") ||
			gameObject.name.Contains("Foot")) {
				GameObject oVisResGO	= Resources.Load("Gizmo/Gizmo-Rotate-Unity") as GameObject;
				GameObject oVisGO = GameObject.Instantiate(oVisResGO) as GameObject;
				oVisGO.name = gameObject.name + "-Vis";
				oVisGO.transform.parent = transform;
				oVisGO.transform.localRotation = new Quaternion();
				oVisGO.transform.localPosition = new Vector3();
				oVisGO.transform.localScale = new Vector3(1,1,1);
		}
	}
}
