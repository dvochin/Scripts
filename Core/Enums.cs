using UnityEngine;
using System.Collections;

public enum EGameModes {	//### The 'Game Modes': Central mode that changes how nearly everything operates.  Implemented by CGameXXX derivatives which are all owned by CGame singleton
	None,					// No game mode -> used during cleanup & shutdown
	Configure,				// Gameplay mode that doesn't start the animation engine  (characters are fully realized by remain in T-pose).  Splits mesh between skinned and softbody/cloth simulated, sends simulated parts to PhysX, animates the characters for normal gameplay
	Play,					// Normal gameplay.  Splits mesh between skinned and softbody/cloth simulated, sends simulated parts to PhysX, animates the characters for normal gameplay
}
public enum ECollLayer_OBS {
	Default,			// Default collision layer... basically collides with everything except NoCollision
	VaginaSoftBody,		// The soft body vagina
	VaginaOpener,		// The rigid body colliders shaping the vagina
	VaginaGuideTrack,	// The rigid body colliders guiding the penis inside the vagina
	PenisSoftBody,		// The soft body penis
	PenisRigidCore1,	// The rigid body colliders stiffening the penis
	PenisRigidCore2,	// The rigid body colliders that are responsible for collision against vagina softbody
	BreastsSoftBody,	// The soft body breasts
	BodyColliders_OBS,		// The spheres our Unity implementation uses to simulate the body to push the cloth away (Set to not collide with 'SoftBody')
	Fluid,				// All Fluids (cum)
	Clothing,			// Layer contains all clothing
	NoSelfCollision,	// Any object in this group doesn't collide with other objects in this group
	NoCollision			// Any object in this group doesn't collide with anything!
};

public enum EBodyPartType {			// The different body parts that our soft body implementation can process
	Breasts,
	VaginaL,
	VaginaR,
	Penis,
	Anus
};



public enum EBodySex {		//###TODO ###DESIGN: Add the body type extension like WomanA, ManB, etc?
	Man,
	Woman,
	Shemale					//###DESIGN!!! ###PROBLEM: Two different concepts merged into this enum!!  One is the character type (man/woman/shemale) and the other is the Blender source body used (either Penis or Body2) SPLIT UP!!
};

public enum EBodyPose_TEMP {		//###TODO ###DESIGN: Add the body type extension like WomanA, ManB, etc?
	Standing,
//	Squatting,
//	Kneeling,
	KneelingWideOpen,
	ProppedOnBed,
	Development0,
	Development1,
	Development2,
	Development3,
	Development4,
	Development5,
	Development6,
	Development7,
	Development8,
	Development9,
};

public enum EPenisArg {
	SlerpRotation,
	SlerpSpringForce,
	SlerpSpringDamping
};


public enum EGamePlay {
	Pleasure,
	Arousal,
	PoseRootPos,				//###MOVE?
	PenisSize,
	PenisErectionMax,		// Maximum amount of erection possible... multiplies the penis drive strength value for dual control.
	FluidConfig,
};



public enum EActorNode {
	Pinned,			// Note that the top properties on all actors must all have pins, pos, rot
	PosX,
	PosY,
	PosZ,
	RotX,
	RotY,
	RotZ,
	RotW,
	//Height			//###HACK?  A bit out of place for a generic node... fix would be to specialize nodes for base and torso?
};
public enum EActorChest {
	Pinned,			// Note that the top properties on all actors must all have pins, pos, rot
	PosX,
	PosY,
	PosZ,
	RotX,
	RotY,
	RotZ,
	RotW,
	Chest_LeftRight,
	Chest_UpDown,
	Chest_Twist,
};

public enum EActorPelvis {
	Pinned,			// Note that the top properties on all actors must all have pins, pos, rot
	PosX,
	PosY,
	PosZ,
	RotX,
	RotY,
	RotZ,
	RotW,
};

public enum EActorArm {
	Pinned,			// Note that the top properties on all actors must all have pins, pos, rot
	PosX,
	PosY,
	PosZ,
	RotX,
	RotY,
	RotZ,
	RotW,
	HandTarget,
	Hand_UpDown,
	Hand_LeftRight,
	Hand_Twist,
	Fingers_Close,
	Fingers_Spread,
	Fingers_ThumbPose,
//	UserControl					// Special property CKeyHook uses for user control of pushing / pulling hands.
};

public enum EActorLeg {
	Pinned,			// Note that the top properties on all actors must all have pins, pos, rot
	PosX,
	PosY,
	PosZ,
	RotX,
	RotY,
	RotZ,
	RotW,
	Thigh_Spread,
	Thigh_Rotate,
};






public enum EBodyDef {
	Sex,
	ClothingTop,
	ClothingBottom,
	Hair,
	BtnUpdateBody,
	BreastSize				//####DEV
//	BlenderProp_HACK
};

public enum EBodyClothingTop_HACK {		//###HACK!!!! These need to be read from file directory names!!
	None,
	TiedTop,
	Bra1,
	BikiniT1
};

public enum EBodyClothingBottom_HACK {
	None,
	Panties1,
	BikiniB1
};

public enum EBodyHair {
	None,
	Messy,
	TiedUp,
};

public enum EFace {
	EyesClosed,
	MouthOpen,
	BrowInner,
	BrowOuter
};

public enum EPoseRoot {
	Flipped					//###BROKEN: Need to flip load order, not 180!!  
};


public enum EMorphOps {		//####TEMP
	BreastSize
};

