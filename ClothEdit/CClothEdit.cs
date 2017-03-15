/*###DISCUSSION: CClothEdit: In-game cloth cutting via Blender
//=== LAST ===
- Ok basic persistence between cloth cutting and editing... now go the other way, revisit all code, GUI and canvas, automatic progression, etc
- State of cloth cut lost when going to play mode!  Blender instance has to live longer and cover cloth cutting and play mode in Unity + Blender!
- GUI update at every prop too slow.  Have an 'Update' button
- GUI names horrible!
	- Find a way to highlight what node the user is choosing?

//=== NEXT ===

//=== TODO ===

//=== LATER ===

//=== IMPROVE ===

//=== DESIGN ===

//=== IDEAS ===

//=== LEARNED ===

//=== PROBLEMS ===

//=== QUESTIONS ===

//=== WISHLIST ===

*/


/*###DISCUSSION: Cloth editing GUI
=== LAST ===
- This class needed???

=== NEXT ===
- Think of global 'design time' GUI and its underlying classes & mechanisms (e.g. actions for each mode, what to display in property editor, etc)
- Code simple property editor to edit one curve... then work on curve selection
- When easy cloth editing becomes possible improve Blender-side with angle + dist on seam points
- Load and save of cloth curve.
- Clothing recipes... on Blender side only (CCloth sub-class?)

=== TODO ===

=== LATER ===

=== IMPROVE ===

=== DESIGN ===
- Entire 'design time' is one top-level button that switches between play mode and design mode.
- Design time mode always has a top-level menu on the left of body with one 'design time mode' combobox selecting the design mode...
	- and a 'sub combobox' for what to do in that particular design mode.

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===
=== QUESTIONS ===

=== WISHLIST ===
- Need central editing functionality: A central game mode combobox followed by what to edit (e.g. which cloth) and what to sub-edit (e.g. which curve)
	- Detail pane fills up from CObject enumeration from there.
	- Q: What to do about 'commit' button?  How does that flow with top-level editing framework?
	- Q: How to select multiple clothing? (idea: have a slot for all possible clothing with default being nothing populated?)
	- Q: How to save the whole thing?


###LATEST<17>: UV cloth cutting
# BMesh boolean cut can fail sometimes... go to carve and store custom layers differently?
# Merge var names of old and new
# Need to adopt angle + distance on seams... with backside its own different length (and same angle)    
# Currently no base properties of CCloth... do we put those in for easier cloth editing?
    # Specialize CCloth for different recipes like CClothTop, CClothPants, etc?    
# Go for a class for CCurvePt??
# We currently scan seam chains for L/R... given our simplifying assumptions do we keep overhead of right side?
# Need to redo bodysuit to new body.  Redo UVs too.

#- Unity edits CProps of CCloth at the global level (for cloth types, curve types, meta props like nipple X/Y, bra strap pos / width, etc)
#- User can edit only one curve at a time in a parent / child with CCloth / CCurve.  User picks by radio button one only.
    #- User adjusts sliders and Unity renders the cutting curves onto bodysuit (and cuts when user presses 'Cut' button)
#- CCurve has derived superclasses such as CCurveNeck, CCurveLegs, CCurveBottom, CCurveArms, etc which contains its own CObject / CProps for Unity editing / cloth loading/saving
#- Have to remove the old single-side source mesh, cut mesh, etc and its behavior (duplicate before batch cut)
#- Folder positions of cloth stuff... a new parent node?

        ###RESUME<17>: Need angles and dist for the beziers, need to define and update in same function (called everytime user changes anything)
#- Points not deleted on back mesh!!
#- Side curve bezier tedious to adjust as it is dependent on incident angle... make based on that angle??
    #- Should have different lenght possible for each side of seam beziers?  (go for angle + length with diff lenght on each side??)
#- Need to triangulate final 3D mesh
#- Scan through and remove old crap

*/



using UnityEngine;

public class CClothEdit {            // CClothEdit: Helper class hosted by CBodyBase.  Used to stuff all cloth cutting functionality into one file.

	public CBodyBase			_oBodyBase;
	CUICanvas					_oCanvas;				// Canvas that stores our cloth-editing GUI
	CBCloth						_oCloth_Play;				// The play-time cloth.
	CBMeshFlex					_oMeshFlex_Editing;
	public string				_sNameClothEdit;
	public string				_sClothType;
	public string				_sNameClothSrc;
	public string				_sVertGrp_ClothSkinArea;			// Vertex group from body that dictate which part of cloth is skinned as opposed to simulated (to prevent complete cloth moving!)
	string						_sBlenderAccessString_ClothInCollection;
	EClothEditMode				_eClothEditMode;

	public CClothEdit(CBodyBase oBodyBase, string sNameClothEdit, string sClothType, string sNameClothSrc, string sVertGrp_ClothSkinArea) {
		_oBodyBase			= oBodyBase;
		_sNameClothEdit		= sNameClothEdit;
		_sClothType			= sClothType;
		_sNameClothSrc		= sNameClothSrc;
		_sVertGrp_ClothSkinArea = sVertGrp_ClothSkinArea;
        _sBlenderAccessString_ClothInCollection = ".aCloths['" + _sNameClothEdit + "']";

		//=== Create cloth from Blender for the first time ===
        CGame.gBL_SendCmd("CBody", oBodyBase._sBlenderInstancePath_CBodyBase + ".CreateCloth('" + sNameClothEdit + "', '" + sClothType + "', '" + sNameClothSrc + "')");      // Create the Blender-side entity to service our requests

		//=== Enter editing mode and create the editing cloth at init ===
		_eClothEditMode		= EClothEditMode.Editing;			// We start in editing mode and must be told when to go back and forth to/from play mode
		Create_EditingCloth();

		//=== Create the managing object and related hotspot ===
		CObject _oObj = new CObject(_oMeshFlex_Editing, "Cloth Cutting", "Cloth Cutting");
		_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue_EditingMode;
		for (int nCurve = 0; nCurve < 3; nCurve++) {
			string sBlenderAccessString_Curve = string.Format("{0}{1}.aCurves[{2}].oObj", _oBodyBase._sBlenderInstancePath_CBodyBase, _sBlenderAccessString_ClothInCollection, nCurve);
			CPropGrpBlender oPropGrpBlender = new CPropGrpBlender(_oObj, "", sBlenderAccessString_Curve);
		}

		//=== Create Canvas for GUI for this mode ===
		_oCanvas = CUICanvas.Create(_oBodyBase._oBodyRootGO.transform);				//###CHECK<19> What root??
		_oCanvas.transform.position = new Vector3(0.31f, 1.35f, 0.13f);            //###WEAK<11>: Hardcoded panel placement in code?  Base on a node in a template instead??  ###IMPROVE: Autorotate?
		_oCanvas.CreatePanel("Cloth Cutting", null, _oObj);
	}

	//------------------------------------------------------------------	CREATE / DESTROY
	void Create_EditingCloth() {
		if (_oMeshFlex_Editing == null)
			_oMeshFlex_Editing = CBMeshFlex.CreateForClothEdit(_oBodyBase, _sNameClothEdit);
	}

	void Destroy_EditingCloth() {
		if (_oMeshFlex_Editing != null) {
			GameObject.Destroy(_oMeshFlex_Editing.gameObject);
			_oMeshFlex_Editing = null;
		}
	}

	void Create_PlayCloth() {
		if (_oCloth_Play == null)
			_oCloth_Play = CBCloth.Create(this, _sVertGrp_ClothSkinArea);
	}

	void Destroy_PlayCloth() {
		if (_oCloth_Play != null)
			_oCloth_Play = _oCloth_Play.DoDestroy();
	}

	public CClothEdit DoDestroy() {
		Destroy_EditingCloth();
		Destroy_PlayCloth();
		CGame.gBL_SendCmd("CBody", _oBodyBase._sBlenderInstancePath_CBodyBase + ".DestroyCloth('" + _sNameClothEdit + "')");		// We're completely destroying cloth editing.  Destroy cloth in Blender too.
		return null;				// Return convenient null for assignment to own variable.
	}


	//------------------------------------------------------------------	MODE CHANGE
	public void GameMode_EnterMode_EditCloth() {
		if (_eClothEditMode == EClothEditMode.Editing)
			return;
		Destroy_PlayCloth();
		Create_EditingCloth();
		_eClothEditMode = EClothEditMode.Editing;
	}

	public void GameMode_EnterMode_Play() {
		if (_eClothEditMode == EClothEditMode.Play)
			return;
		Destroy_EditingCloth();
		Create_PlayCloth();
		_eClothEditMode = EClothEditMode.Play;
	}


	//------------------------------------------------------------------	EVENTS
	void Event_PropertyChangedValue_EditingMode(object sender, EventArgs_PropertyValueChanged oArgs) {      // Fired everytime user adjusts a property.
		// 'Bake' the morphing mesh as per the player's morphing parameters into a 'MorphResult' mesh that can be serialized to Unity.  Matches Blender's CBodyBase.UpdateMorphResultMesh()
		Debug.LogFormat("CClothEdit updates on body'{0}' because property '{1}' changed to value {2}", _oBodyBase._sBodyPrefix, oArgs.PropertyName, oArgs.ValueNew);

		Destroy_EditingCloth();			// Destroy and re-create editing cloth so we get the freshly recut mesh from Blender
		Create_EditingCloth();
	}
}

enum EClothEditMode {
	Editing,					// User is editing the cloth mesh (adjusting sliders for cutting curves) to recut mesh.
	Play						// User is in play mode with this cloth (full simulation capabilities)
};
