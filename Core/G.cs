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


	//public const int 	C_Layer_HarnessSphereliders	= 29;
	//public const uint 	C_LayerMask_HarnessSphereliders	= 0x20000000;					//###NOTE: Can't convert (2 << C_Layer_CutterPlane) easily to their mask (weird compilere errors) so keep these in sync manually!

	public const int	C_FrameRate = 25;							//###TODO: Push this value in all contexts needing framerate... including PhysX!!
	public const byte	C_FileVersion_CurveDefinition = 1;			// The file version of pose serialization.  Magic number pushed as first byte of every curve definition file
	public const byte	C_FileVersion_Pose = 1;						// Version of pose files we can load and save
	public const bool	C_DisplayOnCheckIfChanged = false;			// Controls verbose output on debug log when 'superpublic' values change.
	public const float	C_PiDiv2 = Mathf.PI / 2;
    public const int    C_MaxVertsInMesh = 65534;                   // How many verts a Unity mesh can have

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

    public static int       C_Layer_Default             = LayerMask.NameToLayer("Default");             // Layer used for body bones that cannot collide with anything (mostly in torso)
    public static int       C_Layer_UI                  = LayerMask.NameToLayer("UI");
    public static int       C_Layer_NoCollision         = LayerMask.NameToLayer("NoCollision");         // Layer with no collision with anything.
    public static int       C_Layer_BodySurface         = LayerMask.NameToLayer("BodySurface");         // Layer used by PhysX mesh collider to represent the whole body (including soft body parts) for raycasting (used for body surface path creation)
    public static int       C_Layer_Bone1               = LayerMask.NameToLayer("Bone1");               // Layer used for body collider bones.  Three of them are needed due to overlap in body structure (especially around the shoulder area)
    public static int       C_Layer_Bone2               = LayerMask.NameToLayer("Bone2");
    public static int       C_Layer_Bone3               = LayerMask.NameToLayer("Bone3");
    public static int       C_Layer_Hand                = LayerMask.NameToLayer("Hand");                // Hand requires a special layer.  Its PhysX bones must collider with BodySurface so we cannot have the BodySurface mesh collider cover the hand area and had to split the hands into its own BodySurface collider mesh with this layer
    public static int       C_Layer_VrWand              = LayerMask.NameToLayer("VrWand");			    // Layer used ONLY on the left and right VR touch input controllers.
    public static int       C_Layer_Cutter              = LayerMask.NameToLayer("Cutter");              // Layer used to detect only the 'cutter plane' that makes single-plane editing possible    ###OBS:?	public static int       C_Layer_Gizmo               = LayerMask.NameToLayer("Gizmo");               // Layer used to detect only gizmo colliders
    public static int       C_Layer_Gizmo               = LayerMask.NameToLayer("Gizmo");               // Layer used to detect only gizmo colliders
    public static int       C_Layer_HotSpot             = LayerMask.NameToLayer("HotSpot");             // Layer used to detect only hotspots

    
    public static int       C_LayerMask_Default         = 1 << C_Layer_Default;
    public static int		C_LayerMask_UI              = 1 << C_Layer_UI;
    public static int       C_LayerMask_NoCollision     = 1 << C_Layer_NoCollision;
    public static int       C_LayerMask_BodySurface     = 1 << C_Layer_BodySurface;
    public static int       C_LayerMask_Bone1           = 1 << C_Layer_Bone1;
    public static int       C_LayerMask_Bone2           = 1 << C_Layer_Bone2;
    public static int       C_LayerMask_Bone3           = 1 << C_Layer_Bone3;
    public static int       C_LayerMask_Hand            = 1 << C_Layer_Hand;
    public static int       C_LayerMask_VrWand          = 1 << C_Layer_VrWand;
    public static int       C_LayerMask_Cutter          = 1 << C_Layer_Cutter;
	public static int		C_LayerMask_Gizmo 			= 1 << C_Layer_Gizmo;
	public static int		C_LayerMask_HotSpot 		= 1 << C_Layer_HotSpot;

    public static int       C_LayerMask_Bones           = C_LayerMask_Bone1 | C_LayerMask_Bone2 | C_LayerMask_Bone3 | C_LayerMask_Hand;
    public static int       C_LayerMask_BodySurfaces    = C_LayerMask_BodySurface | C_LayerMask_Hand;       //###DESIGN: Keep??
    public static int       C_LayerMask_HeadsetRaycaster= C_LayerMask_BodySurface | C_LayerMask_UI;         // Headset raycaster needs to perform a single raycast against bodies AND UI browsers!

    public static Color     C_Color_YellowTrans         = new Color32(255,255,000,016);      //###IMPROVE: Move all the static color definitions here!
    public static Color     C_Color_RedDark             = new Color32(128,000,000,255);
    public static Color     C_Color_RedTrans            = new Color32(255,000,000,016);
    public static Color     C_Color_BlueDark            = new Color32(000,000,128,255);
    public static Color     C_Color_Orange              = new Color32(255,165,000,255);
    public static Color     C_Color_Purple              = new Color32(216,191,216,255);
}
