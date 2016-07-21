/*###DISCUSSION: PENIS
=== NEXT ===
 * Can't exit!
 * Get entire penis chain position for stroking

=== TODO ===
 * Balls don't fold under penis... give them their own bones?
-? Need to specify dick chain by mass not density for max cloth repell power.
- Collision of dick against body col needs work: Both dick and simulated pants cannot go through each other.
	- We need to tell body col to always include inner thighs to repell penis from legs
	- Body Col generation needs to be smartend up to just one separated cloth?? (or does it do that already)
	- Need to update body col at each frame...  different context then cloth-fit mode but we need to integrate the two!
- Balls tucked under dick: Need to adjust softbody compress to very high so they get squished!
	- Add a short fixed capsule collider at balls so their inner core stays right under penis

=== LATER ===
- Really need to make penis fit to both man and shemale... will need to build a 'bridge' when we get to man.

=== IMPROVE ===
 * Duplicate collider code in C++

=== DESIGN ===

=== IDEAS ===
 * Hide body and breasts renderer to leave only 'dildo' mode!
- Scaling dick up/down during ejaculation pretty cool!!

=== LEARNED ===

=== PROBLEMS ===
 * +++ Problem with vagina softbody not being kept opened by penis outer colliders:
   * A mass or density issue??  A same-size single colliders keeps vagina open perfectly while penis chain eventually fails
   * Get rid of outer penis chain and have a 'vagina expander' capsule??
 * Large 'jump' in penis joints occur when PhysX delta time changes... smooth out?  Go to FixedUpdate??
 *- Penis tip woble... extend last collider?
++ Because head is such a soft body and easily gets out of its collider, this makes the rigid-body-based approach of connecting cum fluid emitter to a bone useless!  We'll have to read vert pos and send it every frame!
-? Increasing capsule radius causes unstability if density is out of wack... auto-adjust density through mass calculations??
++ Tip of penis chain not glueing to softbody??

=== LEARNED ===
- Increasing capsule radius causes unstability if density is out of wack... auto-adjust density through mass calculations??

=== PROBLEMS: ASSETS ===
- Need to make base of penis look good when we must push down into underwear... how??
- Still have bad capping??
- Uretra looks like shit!  Also define mesh better
- Color of knob still off
- ZBrush penis & decimate!!!!

=== PROBLEMS??? ===
- Problem with lag when moving bones in editor... will be of concert with gametime animations??
- Previous design for penis damping was 2D but we're applying 3D now... can recalibrate better??
- Remember the hack with body col deep copy!

=== WISHLIST ===
- Penis mesh: Push in separation to body mesh one more ring... need more room to push penis into body texture
- Penis textures: Can't apply normal map because of no tangents... import code I found
- Currently defining body collider density in relative terms to original face count... we need to avoid placing too many colliders for small clothing... what to do??
-? Statically set dick joints as it's too slow at init??
*/


using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class CPenis_OBS : CBSoft, IObject, IHotSpotMgr {
	//---------------------------------------------------------------------------	MEMBERS
	[HideInInspector]	public 	CObject				_oObjDriver;					// Our client/server object responsible for the penis colliders.  Note that our parent class has related oObj which is the softbody client/server object!
	[HideInInspector]	public 	CPenisTip			_oPenisTip;						// The penis tip object controls cum fluid

	[HideInInspector]	public  float				_nRadiusStart, _nRadiusNow;		// Penis chain radius at start and now
	[HideInInspector]	public  float				_nSegLenStart, _nSegLenNow;		// Penis chain segment lenght at start and now
	
	[HideInInspector]	public  int					_nNumSegments = 9;				// Number of linked capsules that provide the inner rigid body core to 'bend' the penis.  Around 8-12 is reasonable ###TUNE

	[HideInInspector]	public  Transform			_oNodePenisRootBone;			// The 'bone' we connect to that will represent the 3D position of the penis root in PhysX.  (We must remain at origin as our verts are set by PhysX in global space with zero rotation)
	[HideInInspector]	public  Transform			_oNodePenisScaleDampCenter;		// The child of _oNodePenisRootBone that holds the 3D position of the 'damping center' to reduce scaling of penis near its base so its mesh remains attached to main body mesh
	[HideInInspector]	public  Vector3				_vecPenisRootBoneStartPos;		// The position of our penis root bone at start.  Used to return to this start position after the conclusion of the fitting procedure between penis and CBClothUnderwear
								Vector3				_vecPenisBase;					// The 3D position of where the penis base is at (basically the base of the shaft)
						public	float				_nScaleDampSizeStart, _nScaleDampSizeEnd;	// The size of the 2D circle centered at vecPenisScaleDampCenter where border scaling is applied to dampen scaling near the border


	public const string C_PathPenisBoneParent = "Bones/chest/abdomen/hip/PenisRoot";		//###DESIGN! Body2 and man difference root of penis?  ###DESIGN! ###OPT ###CHECK: Sub node Penis-Base needed once things work well??
	


	public CPenis_OBS() {
		_nRangeTetraPinHunt = 0.012f;           //15###TUNE!!: Sensitive & important!		//###CHECK: Why the sudden range increase requirement???
		_SoftBodyDetailLevel = 15;				//###OPT!!!: Reduce if quality can be maintained at base
	}

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();

		_oObjDriver = new CObject(this, _oBody._nBodyID, typeof(EPenis), "Penis", "Penis");		//###IMPROVE: Name of soft body to GUI

		//=== Get the penis collider coordinates from Blender so that we can create the PhysX string of capsule colliders that perform the real movement / collision of the penis (by driving along the slave soft body)
		string sResult = CGame.gBL_SendCmd("Penis", "gBL_Penis_CalcColliders('" + _sNameBlenderMesh + "')");
		string[] aStringParts = sResult.Split(',');     // Format of comma-separated string from Penis_CalcColliders() is: Penis Radius, Penis Length, Base.y (base 'height'), Base.z (base forward/back), ScaleDampCenter.y, ScaleDampCenter.z, ScaleDampSizeStart, ScaleDampSizeEnd
		_nRadiusStart = Single.Parse(aStringParts[0]);
		float nLengthStart = Single.Parse(aStringParts[1]);
		_nSegLenStart = nLengthStart / _nNumSegments;
		_vecPenisBase.y = Single.Parse(aStringParts[2]);
		_vecPenisBase.z = Single.Parse(aStringParts[3]);		//###NOTE ###IMPORTANT: Cloth repell power a tricky balancing act... see notes in C++ dll for this call!
		Vector3 vecPenisScaleDampCenter = new Vector3(0, Single.Parse(aStringParts[4]), Single.Parse(aStringParts[5]));
		_nScaleDampSizeStart = Single.Parse(aStringParts[6]);
		_nScaleDampSizeEnd   = Single.Parse(aStringParts[7]);
		_oNodePenisRootBone = _oBody._oBodyRootGO.transform.FindChild(C_PathPenisBoneParent);
		_oNodePenisRootBone.position = _vecPenisBase;
		_oNodePenisScaleDampCenter = _oNodePenisRootBone.FindChild("PenisScaleDampCenter");
		_oNodePenisScaleDampCenter.position = vecPenisScaleDampCenter;
		_oNodePenisRootBone.rotation = Quaternion.identity;
		_vecPenisRootBoneStartPos = _oNodePenisRootBone.localPosition;		// Remember start position of penis root bone for CBClothUnderwear fitting procedure.

		_eColGroup = EColGroups.eLayerPenisI;
		//####DEV!!!!!!! base.OnStart(oBody);

		_oObjDriver._hObject = ErosEngine.Penis_Create("Penis", _oObjDriver.GetNumProps(), _oBody._nBodyID, _nNumSegments, _vecPenisBase, _vecPenisBase + new Vector3(0, 0, nLengthStart), transform.rotation, _nRadiusStart / 3, _nRadiusStart, 1);     //###TUNE!!

		//###DESIGN: Most interesting properties in this secondary-name?  Can shift softbody to 2nd???
		_oObjDriver.PropGroupBegin("", "", true);
		_oObjDriver.PropAdd(EPenis.PenisScale,			"Penis Scale",				0.75f,	0.75f, 1.0f,	"", CProp.Local | CProp.Hide);	//###DESIGN??: Unnatural fit to have some these properties with 'collider'?  Create a 3rd object???
		_oObjDriver.PropAdd(EPenis.BaseLeftRight,		"Base Left/Right",			0,		-90,	90,		"");
		_oObjDriver.PropAdd(EPenis.BaseUpDown,			"Base Up/Down",				-40,	-90,	90,		"");
		_oObjDriver.PropAdd(EPenis.ShaftLeftRight,		"Shaft Left/Right",			0,		-10,	10,		"");
		_oObjDriver.PropAdd(EPenis.ShaftUpDown,			"Shaft Up/Down",			-4,		-15,	30,		"");
		_oObjDriver.PropAdd(EPenis.DriveStrength,		"Drive Strength Internal",	5f,		0.05f,	10,		"", CProp.Hide);	// Real (hidden) drive strenght
		_oObjDriver.PropAdd(EPenis.DriveStrengthMax,	"Drive Strength",			100,	0,		100,	"", CProp.Local);	// Drive strength we control
		_oObjDriver.PropAdd(EPenis.AngularDamping,		"Angular Damping",			1,		0,		1,		"");
		_oObjDriver.PropAdd(EPenis.LinearDamping,		"Linear Damping",			1,		0,		1,		"");

		_oObjDriver.PropAdd(EPenis.Mass,				"Mass",						0.3f,	0.001f,	1,		"");	//####MOD: Was 1 (too high!) (was fixing shimmer during cum)
		//_oObjDriver.PropAdd(EPenis.Density,			"Density",					1,		0.01f,	10,		"");
		//_oObjDriver.PropAdd(EPenis.DriveDamping,		"Drive Damping",			0,		0,		1,		"");
		_oObjDriver.FinishInitialization();

		ErosEngine.Object_GoOnline(_oObjDriver._hObject, _oObj._hObject);		// Note the important passing of a PhysX2 softbody handle to our PhysX3 penis colliders!

		_oPenisTip = CUtility.FindOrCreateNode(gameObject, "PenisTip", typeof(CPenisTip)) as CPenisTip;
		_oPenisTip.transform.position = _vecPenisBase;		//###HACK
		_oPenisTip.OnAwake(this);
		_oPenisTip.OnStart();		//###IMPROVE: Combine awake & start?

		_oObj.PropSet(ESoftBody.SolverIterations, 1);			//###OPT!!!!! Expensive but reduces shimmer of soft body when cumming.  Can be removed in other ways??
		_oObj.PropSet(ESoftBody.SoftBody_Damping, 1.0f);	//###TUNE
		_oObj.PropSet(ESoftBody.StretchingStiffness, 0.7f);	//###TUNE
		_oObj.PropSet(ESoftBody.SoftBody_Gravity, -2);		//###TEMP? ###HACK? Set stiff gravity so balls are pulled downward as a cheap fix as we don't yet collide against shaft and legs
	}  

	public override void OnDestroy() {
		ErosEngine.Penis_Destroy(_oObjDriver._hObject);
		base.OnDestroy();
	}

	public override void OnSimulatePre() {
		base.OnSimulatePre();

		//=== Calculate physical penis size from the current percentage stored in global gameplay property ===
		CProp oPropPenisSizePercent = CGame.INSTANCE._oObj.PropFind(EGamePlay.PenisSize);
		CProp oPropPenisScale = _oObjDriver.PropFind(EPenis.PenisScale);
		float nPenisScale = oPropPenisScale._nMin + oPropPenisScale._nMinMaxRange * oPropPenisSizePercent._nValueLocal / 100.0f;
		_oObjDriver.PropSet(EPenis.PenisScale, nPenisScale);
		_nRadiusNow	= _nRadiusStart * nPenisScale;		// Calculate the current penis radius and segment lenght for this frame.  Vagina guide track needs this to open at right size and penis tip needs to properly position fluid emitter
		_nSegLenNow			= _nSegLenStart * nPenisScale;

		//=== Calculate physical penis erection from the current percentage stored in global gameplay property ===	###DESIGN ###SIMPLIFY: This drive strength complexity really required??? Try to simplify!!
		CProp oPropPenisErectionPercent = CGame.INSTANCE._oObj.PropFind(EGamePlay.PenisErectionMax);
		CProp oPropDriveStrength = _oObjDriver.PropFind(EPenis.DriveStrength);
		float nDriveStrengthMax = _oObjDriver.PropGet(EPenis.DriveStrengthMax) / 100;
		float nDriveStrength = oPropDriveStrength._nMin + oPropDriveStrength._nMinMaxRange * oPropPenisErectionPercent._nValueLocal / 100.0f * nDriveStrengthMax;
		_oObjDriver.PropSet(EPenis.DriveStrength, nDriveStrength);

		ErosEngine.Penis_Update(_oObjDriver._hObject, _oNodePenisRootBone.position, _oNodePenisRootBone.rotation, _oPenisTip._memTransformPhysX.P, nPenisScale, CGame.INSTANCE._bPenisInVagina);

		_oPenisTip.OnSimulatePre();
	}
	public override void OnSimulatePost() {
		float nPenisScale = _oObjDriver.PropGet(EPenis.PenisScale);
		if (nPenisScale != 1) {
			Vector3 vecPenisBaseNow = _oNodePenisRootBone.position;
			Vector3 vecPenisScaleDampCenter = _oNodePenisScaleDampCenter.position;
			float nScaleDampSizeRange = _nScaleDampSizeEnd - _nScaleDampSizeStart;
			float nPenisScaleLessOne = (nPenisScale - 1);

			int nVerts = _memVerts.L.Length;
			for (int nVert = 0; nVert < nVerts; nVert++) {				//###OPT!!!!!: Expensive operation that really should be done in C++!  Costs about 1ms per frame!!
				Vector3 vecVert = _memVerts.L[nVert];
				float nDistFromScaleDampCenter = Vector3.Distance(vecVert, vecPenisScaleDampCenter);		// The distance of this 2D vert from the damping center... how close our 2D pos is to this 2D pos determines damping of scaling near penis mount.
				float nDistFromScaleDampCenterLessStart = nDistFromScaleDampCenter - _nScaleDampSizeStart;

				float nScaleThisVert;
				if (nDistFromScaleDampCenterLessStart < 0) {						// If this vert's 2D position (viewed fror left or right) is in the inner range of the scale damp center we don't modify it at all.
					nScaleThisVert = 1;
				} else if (nDistFromScaleDampCenterLessStart < nScaleDampSizeRange) {		// If this vert's 2D position (viewed from left or right) is in the outer range of the scale damp center we scale down its scaling to taper scaling in the ranged area
					nScaleThisVert = 1 + Mathf.Sin(G.C_PiDiv2 * nDistFromScaleDampCenterLessStart / nScaleDampSizeRange) * nPenisScaleLessOne;	// Perform normal-like smoothing on curve for best blending look
				} else {															// If this vert's 2D position (viewed from left or right) is outside the range of the scale damp center we scale it full strength
					nScaleThisVert = nPenisScale;
				}

				if (nScaleThisVert != 1) {
					Vector3 vecFromBase = vecVert - vecPenisBaseNow;
					vecFromBase *= nScaleThisVert;
					vecVert = vecPenisBaseNow + vecFromBase;
					_memVerts.L[nVert] = vecVert;
				}
			}
		}
		base.OnSimulatePost();
	}


	//--------------------------------------------------------------------------	COBJECT CALLBACK EVENTS


	//--------------------------------------------------------------------------	IHotspot interface

	//public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }

	//public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {		//###DESIGN? Currently an interface call... but if only GUI interface occurs through CObject just have cursor directly invoke the GUI_Create() method??
	//	//if (eHotSpotEvent == EHotSpotEvent.ContextMenu)	###NOTE: Done in penis tip...  keep?
	//	//	_oHotSpot.WndPopup_Create(new CObject[] { _oObjDriver, oObj });
	//}

}
