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

//public class CClothEdit : MonoBehaviour {		//###OBS<17>?

//	public CBody		_oBody;
//	public string		_sClothType;
//	public string       _sBlenderInstancePath_CClothEdit;
//	public CBMesh       _oClothSource;

//	public CClothEdit(CBody oBody, string sClothType) {
//		_oBody			= oBody;
//		_sClothType		= sClothType;

//		//        CBody.CBody._aBodies[0].CreateCloth("BodySuit", "_ClothSkinnedArea_Top", "Shirt")      ###One of teh body suits?
//		string sCmd = _oBody._oBodyBase._sBlenderInstancePath_CBodyBase + ".CreateCloth('BodySuit', '_ClothSkinnedArea_Top', '" + _sClothType + "')";          //###DESIGN: Pass in all args from Unity?  Blender determines?
//		CGame.gBL_SendCmd("CBody", sCmd);
//		_sBlenderInstancePath_CClothEdit = "aCloths['" + _sClothType + "']";

//		_oClothSource = CBMesh.Create(null, _oBody._oBodyBase, _sBlenderInstancePath_CClothEdit + ".oMeshClothSource", typeof(CBMesh));
//	}
//}
