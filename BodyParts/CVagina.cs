using UnityEngine;
using System.Collections;

public class CVagina : CSoftBody
{
}

///*###DISCUSSION: Vagina
//=== NEXT ===
// * Track rotates very poorly... can be fixed with simple angle adjustment??  Bones screwed up??
// * Cunt looks awful... needs more geometry???  Too stiff on SB?
// * Guide tracked managed by L/R!!!
// * Delete renderer on man
// * Bad values on init (never sets!
// * Add gradual penis growth
// * Need longer track to avoid penis getting stuck
// * Disable collision from cum when inside??  (should still leave limbs enabled!)

//=== TODO ===
// * Add new bone to crotch area to split legs apart... one bone that is scaled ok?  (Or do we need two moved apposite ways (harder to define!)
// * Do we still need VaginaL/R classes?
// * Set final position in Blender and sync!

//=== LATER ===
// * Build a dildo we can fuck with!!

//=== DESIGN ===

//=== IDEAS ===
// * Instead of pinning vagina at surface for half-split point, can go under??  (Or not??)

//=== QUESTIONS ===
// * Angle applied to Sex or Guide track??

//=== PROBLEMS ===
// * Cum collides on tracks
// * Very weird bug with half of vagina not showing up if we invert build!!???
// *+++ Vagina mesh selection in Blender from base body troublesome... find a way to unjag the selection?  Or resort to selection sphere?  Or some fancy bmesh analysis of edges and angles??
// * Having too much skin above slit hurts appearance... reduce to minimum!
//	 * But how?  (Original body has little geometry there!)
// *-- Because vagina body attachments verts can be so far apart, messy to connect to CPinTetra from CPinSkinned:
//	 * Give extra geometry to the skinned mesh?  Connect to tetra by half-verts?

//=== PROBLEMS: ASSETS ===
// * Vagina meshes still have all materials!!
// * Pose of new capping vert important to avoid wasting particles!!
// * Far too much softbody above slit
// * Need to re-UV new vagina in ZBrush and draw in pussy slit.

//=== PROBLEMS??? ===
// *? Corner CPinSkinned of slits grabbing too many tetra verts... how to fix this???
//	* Consider a 'sphere dead zone while looking for CPinTetra??

//=== WISHLIST ===
// * Capping??
// * Move clitoris and anus bones when penetration!
// * We can do better with the strip between the two halves... find a way to do wider on that doesn't kill the UVs

//*/

//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;


//public class CVagina {		// Class to abstract away complexity of managing L/R vagina under one set of properties		//####OBS ####SOON
//	public CSoftBodyVaginaL 		_oSoftBodyVaginaL;
//	public CSoftBodyVaginaR 		_oSoftBodyVaginaR;
//	CBody				_oBody;

//	CCollider 			_oColGuideU,  _oColGuideD,  _oColGuideL,  _oColGuideR;	// The 'guide track' that guide the penis rigid body inner core inside the vagina
//	CCollider			_oColGuideDF, _oColGuideLF, _oColGuideRF;				// The 'funnel' part of the geometry that funnels penis inside vagina (Up funnel absent for easy penis front entry)
//	List<CCollider>		_aColliders = new List<CCollider>();		// Collection of all the above colliders for easier batch processing.

//	//float _nRadiusPenisOld;				// Previously-read value of penis radius.  Used to reconstruct guide track only when needed.

//	const float C_TrackLength = 0.2f;		// The length of the vagina guide track (how far it stick into body)		###BUG: Cannot change this from 0.2 without funnel and track not joining... revisit algorithm
//	const string C_BonePath_Track = "chest/abdomen/hip/sex/VaginaGuideTrack/";		// Where in the bone structure vagina guide track nodes are

//	public Transform _oVaginaCumEmitterT;		// The bone representing where cum is emitted from when female ejaculates.

//	public CVagina(CBody oBody) {
//		_oBody = oBody;		//###WEAK!!! Filling in body's array of softbodies!  Revisit??
//		//###BROKEN
//		//oBody._aSoftBodies.Add(_oSoftBodyVaginaR = (CSoftBodyVaginaR)CBMesh.Create(null, _oBody, _oBody._sNameGameBody, "_Detach_VaginaR", "Client", "gBL_GetMesh", "'SkinInfo'", typeof(CSoftBodyVaginaR)));
//		//oBody._aSoftBodies.Add(_oSoftBodyVaginaL = (CSoftBodyVaginaL)CBMesh.Create(null, _oBody, _oBody._sNameGameBody, "_Detach_VaginaL", "Client", "gBL_GetMesh", "'SkinInfo'", typeof(CSoftBodyVaginaL)));

//		_aColliders.Add(_oColGuideU  = PrepareCollider("VaginaGuideTrackU"));
//		_aColliders.Add(_oColGuideD  = PrepareCollider("VaginaGuideTrackD"));
//		_aColliders.Add(_oColGuideL  = PrepareCollider("VaginaGuideTrackL"));
//		_aColliders.Add(_oColGuideR  = PrepareCollider("VaginaGuideTrackR"));
//		_aColliders.Add(_oColGuideDF = PrepareCollider("VaginaGuideTrackDF"));
//		_aColliders.Add(_oColGuideLF = PrepareCollider("VaginaGuideTrackLF"));
//		_aColliders.Add(_oColGuideRF = PrepareCollider("VaginaGuideTrackRF"));

//		Transform oTrackT = _oBody.FindBone(C_BonePath_Track + "VaginaEntryTrigger");		// Enable the collider of the trigger so it sends entry / exit events.
//		oTrackT.GetComponent<Collider>().enabled = true;

//		VaginaGuideTrack_EnableDisable(false);

//		_oVaginaCumEmitterT = _oBody.FindBone("chest/abdomen/hip/sex/Vagina-CumEmitter");
//	}

//	CCollider PrepareCollider(string sBoneName) {			// Prepare a simple Unity node representing a pre-sized box and turn it into a kinematic collider for PhysX3
//		string sBonePath = C_BonePath_Track + sBoneName;
//		Transform oTrackT = _oBody.FindBone(sBonePath);
//		oTrackT.gameObject.SetActive(true);

//		Rigidbody oRB = oTrackT.gameObject.AddComponent<Rigidbody>();			//###OPT!!!: Can bypass creation of rigidbody to save load on Unity PhysX??
//		oRB.isKinematic = true;
//		BoxCollider oColBox = oTrackT.gameObject.AddComponent<BoxCollider>();
//		oColBox.enabled = false;
//		CCollider oCol = oTrackT.gameObject.AddComponent<CCollider>();
//		oCol.OnStart(false, true, EColGroups.eLayerVagina);
//		oTrackT.GetComponent<Renderer>().enabled = false;

//		return oCol;
//	}


//	public void OnSimulatePre() {			//###OPT: Run once in a while?

//        //###F ###OBS: Penis / vagina collision now through Flex instead of PhysX colliders!
//		//CPenis oPenis = CGame.INSTANCE._aBodies[0]._oPenis;		//###BUG!!!!! ###HACK!!!: Bad assumption on penis in slot 0?
//		//if (oPenis == null)			//###HACK!!!!: Make radius global??
//		//	return;

//		//float nRadiusPenis = oPenis._nRadiusNow * 1.1f;		// Add a little extra to the vagina opening  so that penis colliders can glide easily	###HACK!!!: Assuming body 0 is man!!!

//		//if (_nRadiusPenisOld != nRadiusPenis) {

//		//	_nRadiusPenisOld = nRadiusPenis;

//		//	_oColGuideU.transform.localPosition = new Vector3(0, 0.1f, -nRadiusPenis);			// Adjust where all four edges of the 'guide corridor' goes so a penis of radius _OpeningRadius fits just in...
//		//	_oColGuideD.transform.localPosition = new Vector3(0, 0.1f, nRadiusPenis);
//		//	_oColGuideL.transform.localPosition = new Vector3(-nRadiusPenis, 0.1f, 0);
//		//	_oColGuideR.transform.localPosition = new Vector3(nRadiusPenis, 0.1f, 0);

//		//	_oColGuideU.SetBoxSize(new Vector3(2 * nRadiusPenis, C_TrackLength, 0.01f));
//		//	_oColGuideD.SetBoxSize(new Vector3(2 * nRadiusPenis, C_TrackLength, 0.01f));
//		//	_oColGuideL.SetBoxSize(new Vector3(0.01f, C_TrackLength, 2 * nRadiusPenis));
//		//	_oColGuideR.SetBoxSize(new Vector3(0.01f, C_TrackLength, 2 * nRadiusPenis));

//		//	float nFunnelSize = 1.5f * nRadiusPenis;		//###TUNE?
//		//	float nSquareRoot2 = Mathf.Sqrt(2);

//		//	//_oColGuideUF.transform.localRotation = Quaternion.Euler(-45, 0, 0);
//		//	_oColGuideDF.transform.localRotation = Quaternion.Euler(45, 0, 0);
//		//	_oColGuideLF.transform.localRotation = Quaternion.Euler(0, 0, -45);
//		//	_oColGuideRF.transform.localRotation = Quaternion.Euler(0, 0, 45);

//		//	//_oColGuideUF.transform.localPosition = new Vector3(0, -nFunnelSize / 2 * nSquareRoot2,  nRadiusPenis + nFunnelSize / 2 * nSquareRoot2);
//		//	_oColGuideDF.transform.localPosition = new Vector3(0, -nFunnelSize / 2 * nSquareRoot2, -nRadiusPenis - nFunnelSize / 2 * nSquareRoot2);
//		//	_oColGuideLF.transform.localPosition = new Vector3(-nRadiusPenis - nFunnelSize / 2 * nSquareRoot2, -nFunnelSize / 2 * nSquareRoot2, 0);
//		//	_oColGuideRF.transform.localPosition = new Vector3( nRadiusPenis + nFunnelSize / 2 * nSquareRoot2, -nFunnelSize / 2 * nSquareRoot2, 0);

//		//	//_oColGuideUF.SetBoxSize(new Vector3(3.5f * nFunnelSize, 2.0f * nFunnelSize, 0));
//		//	_oColGuideDF.SetBoxSize(new Vector3(3.5f * nFunnelSize, 2.0f * nFunnelSize, 0.01f));
//		//	_oColGuideLF.SetBoxSize(new Vector3(0.01f, 2.0f * nFunnelSize, 3.5f * nFunnelSize));
//		//	_oColGuideRF.SetBoxSize(new Vector3(0.01f, 2.0f * nFunnelSize, 3.5f * nFunnelSize));
//		//}

//		//CVagina oVagina = CGame.INSTANCE._oBodyWoman._oVagina;		//###DESIGN: Any value to a menu for vagina??
//		//iGUISmartPrefab_WndPopup.WndPopup_Create(new CObject[] { oVagina._oSoftBodyVaginaL._oObj, oVagina._oSoftBodyVaginaR._oObj }, "Vagina HACK", Input.mousePosition.x, Input.mousePosition.y);
//	}

//	public void VaginaGuideTrack_EnableDisable(bool bEnable) {
//		foreach (CCollider oCol in _aColliders) {
//			oCol.EnableDisable(bEnable);
//		}
//	}
//}
