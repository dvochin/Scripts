/*###DISCUSSION: June 2017
=== LAST ===

=== NEXT ===

=== TODO ===

=== LATER ===

=== IMPROVE ===

=== DESIGN ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== OPTIMIZATIONS ===

=== QUESTIONS ===

=== WISHLIST ===

*/
using UnityEngine;

public class G : MonoBehaviour {					// Global static class to store global constants
	//---------------------------------------------------------------------------	Constants to represent GUI choices for now...
	public const string C_NameBaseCharacter_HACK		= "BodyA";				//###HACK!!!!

	//---------------------------------------------------------------------------	Constants for the name suffix appended to Blender mesh.  Must match Blender!
	//###TODO!!! Update / sync these with Blender!		###OBS
	public const string	C_NameSuffix_Morph      = "_Morph";			// Suffix applied to the mesh used as the source skinned mesh of the character.  It remains untouched
	public const string	C_NameSuffix_BodySkin     	= "_BodySkin";			// Suffix applied to the mesh used as the skinned body during normal gameplay
	//public const string	C_NameSuffix_BodyCol        = "_BodyCol";           // Suffix applied to the coarsely-decimated body meshes that form the basis of collider speres for the creation of capsules used by PhysX to repel cloth and fluid
	//public const string C_NameSuffix_BodyColCloth   = "_BodyColCloth";		// Suffix applied to the coarse body collider optimized to repel cloth (e.g. torso made of about 50 triangles)
	public const string	C_NameSuffix_BodyRim		= "_BodyRim";			// Suffix applied to the 'reduced skinned mesh' that only have the 'rim polygons' to service fast skinning in Client with 'BakeMesh()'
	//public const string	C_NameSuffix_ClothEdit		= "_ClothEdit";			// Suffix applied to the temporary cloth mesh object currently being processed for display by the game Morph mode
	public const string	C_NameSuffix_ClothFit     	= "_ClothFit";			// Suffix applied to the mesh currently used for PhysX fitting in Cloth Fit game mode (has no border)
	public const string C_NameSuffix_ClothSkinned   = "_ClothSkinned";      // Suffix applied to part of a cloth that is skinned to its owning body (e.g. is not PhysX cloth-simulated)
	public const string C_NameSuffix_ClothSimulated = "_ClothSimulated";    // Suffix applied to part of a cloth that is PhysX simulated (e.g. is not skinned to its owning body)
	public const string	C_NameSuffix_ClothFinal     = "_ClothFinal";		// Suffix applied to the mesh currently used to construct the final game character (has quality borders)
	public const string	C_NameSuffix_Face			= "_Face";				// Suffix applied to the mesh currently used to construct the final game character (has quality borders)
	//	public const string	C_NameSuffix_Detach         = "_Detach_";   // Suffix applied in important Client.GameMode_PrepBodyForPlay() to any part of the composite character mesh that is separated for processing by SoftBody or Cloth simulation.  (e.g. Breasts & clothing, penis, vaginaL/R)            

	public const string	C_NamePrefix_CutterAsMesh = "CutterAsMesh";      // Prefix given to meshes created from curves in gBL_GetCutterAsMesh() that converts a curve into a mesh for Client rendering.  Also has a node folder of the same name in ()


	public const int 	C_Layer_NoCollision			= 08;				
	public const int 	C_Layer_NoCollisionSelf		= 09;				
	public const int 	C_Layer_HarnessSphereliders	= 29;

	public const uint 	C_LayerMask_NoCollision		= 0x00000100;
	public const uint 	C_LayerMask_NoCollisionSelf	= 0x00000200;
	public const uint 	C_LayerMask_HarnessSphereliders	= 0x20000000;					//###NOTE: Can't convert (2 << C_Layer_CutterPlane) easily to their mask (weird compilere errors) so keep these in sync manually!

	public const int	C_FrameRate = 25;							//###TODO: Push this value in all contexts needing framerate... including PhysX!!
	public const byte	C_FileVersion_CurveDefinition = 1;			// The file version of pose serialization.  Magic number pushed as first byte of every curve definition file
	public const byte	C_FileVersion_Pose = 1;						// Version of pose files we can load and save
	public const bool	C_DisplayOnCheckIfChanged = false;			// Controls verbose output on debug log when 'superpublic' values change.
	public const float	C_PiDiv2 = Mathf.PI / 2;
	
	//---------------------------------------------------------------------------	MAGIC NUMBERS: Must match Blender.
	public const ushort C_MagicNo_EndOfFlatGroup = 65535;			// We indicate the 'end of a 'flattened group' with this invalid vertID indicating the end of the current group (for efficient serialization of variable-sized groups)
	public const ushort C_MagicNo_EndOfArray = 12345;

	//---------------------------------------------------------------------------	GUI-Related
	public const int	C_Gui_WidgetsHeight		= 24;		// Height of iGUI widgets  ###DESIGN? Can't resize even though we do font!  Because of sliders?? ###BUG! Changing this to another value will cause property groups to no longer scale properly!  (Constant needs to be pushed into some important iGUI property)
	public const int	C_Gui_WidgetsWidth		= 300;		//###HACK! Bar doesn't follow relative layout!
	public const float	C_Gui_LabelWidth		= 0.5f;		// Fraction of GUI panel width used for label (versus content such as slider or combo box)

	//---------------------------------------------------------------------------	Variables stored in 'PlayerPref'
	public const string	C_PlayerPref_MachineID	= "MachineID";		// The MAC addressed used when registring this computer to 'GameKey'
	//public const string	C_PlayerPref_GameKey	= "GameKey";		// The numeric gamekey entered by the user during activation and read at every game init
}
