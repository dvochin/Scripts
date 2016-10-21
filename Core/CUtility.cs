/*###DISCUSSION: CGame
=== New 64 bit efforts ===
- Fix Blender material paths
- Fix Unity mat paths!
- Sys now functional enough... test stuff for softbody on flex! ;)


- Hanshake mechanism super crappy!  Blender will crash if sending twice!  Can peek?
- Hacked blender path string... needed??
- SoftBody now crap because of NxTetra!!  Really remove and go for Flex?
- Could revisit PhysX3 samples to clean up only what we need.
- Deactivated cuda for fluid... put back in?
- Blender lost path to textures!











=== STRATEGY ===
- Play a bit with breast col density
- Add more breast sliders
- Add full body morph capacity for breasts up/down
- Apply SlaveMesh functionality to cloth collider (half, separate, pair, fix after morph, etc)
- Idea about source body storing its original verts in a layer of any use??

- Work to have the two game modes go seamlessly from one to other
	- Rethink the game mode enums and the flag in CBody
	- Once fully working do we remove the old mode crap?

=== REVIVE ===
- Why are bones below hip copied twice at runtime??
- Sleep of bones reoccuring?
- Can enhance cloth reset with delta pos/rot








=== TODO ===
 * OpenCL out of resources after a while...  FUCK!!
   * Looks like a complete re-init needed FUCK!!
 * Stop fluid when loading pose
 * ++ Have to implement event notification for client... with color codes for common errors such as overstretch, opencl error, low perf, etc
   * ADD TOOLTIPS to explain
 * Change in hand keys???
 * Multiple panels!
 * Redo hand implementation
 * Reset pins not really working!  Torso == Chest??
 * Need stronger pins when pinning arms?
 * +++ Pose should store arm target!  (Pinned currently a nightmare!)
 * Arms damped too much (undamp during load only?)
 * New hand keys?
 * Stop cum when init body
 * Place hands on head would be really nice
 * Save blender when a body is created? (or before closing game??)
 * Get rid of penis tip menu and reroute to sex?  (same with vagina?)
 * Tooltips on all properties
 * ++ Properties don't change for man/woman
 * Have split bone tree... right decision?  Trim extra junk, revisit colliders 
 * Color and shape code hotpots soon!
 * New combo box settings for CProp GUI
 *? Have early blender float properties... convert to string for the clothing?
   * Better would be an enum for the clothes from directory.
 * Improve female ejaculation... set set properties like gravity, emitter size?
 * PhysX33
 * Should we move torso hotspot to neck??  (hard to pick)
 * Auto number precision from bounds
 * Have single property change from holding key
 * Need to square up the many panels!  Dock or keys!
 * Breast collision with arms and rough colliders
 * Blend in fixed animations with slerp
   * Need limiting angles for fixed pins and not for raycast?
 * Will need hand on head, hand brace on bed
   * Raycast on PhysX3 scene?

=== PROBLEMS ===
 * Cum collider still to be disabled if penis in vagina??
 * Two dicks in scene all influenced by keys!
 * Problem with dick init if pleasure event never occurs
 * Scene reload during body init forgets 'inverted'
 * WTF log file in Unity folder????
 * HandleD3DDeviceLost upon cum and game crash in built game!
 * Sex bone super low now!
 * Problem with hand pins loading a new pose if hands were pinned 
 *???? Hand position corrupted on load!!
 * Gizmo hard to select on 2nd time an issue
 * Hand default position way off: Init?
 * Face deleted during body init!
 * Pose loader can be 180 rotated and appear confusing... store rotation in char pose??? (Disabled button)
 * Reload body can cause hotspot exception... last hovered one?
 * Penis tip poke through!
 * Crash entering PhysX2, improve logging
   * Add regular flushes to logging
 * //###BUG: Cursor text size grow up / down with zoom
 * //###BUG!!!!! Problem with CActor and rotation. Euler conversion can't take all angles!!
 * +++ Dlg action click through a big problem!
 * PoseLoad and Save: Watch out for man / woman diff!!
 * Why offset when reset pins???
 * PenisScaleDampCenter off
 * During rebuild hovering hotspots cause trouble.
 *+++ Why is Woman_Face or Woman top node getting deleted???
 * Vagina SB not destroying during rebuild!!
 * ERROR PhysX2: CUDA not available   File(D:\sw\physx\PhysXSDK\2.8.4\trunk\SDKs\Core\Device\RegistryHardwareSelection.cpp) Line(71)
 * BUG: pin cube shows an orientation different than hotspot!
 * Have seen bug not being able to right click on a hotspot... lost in some mode in pose mode??
 * Extra hotspots for obsolete hand targets annoying... remove or fix??
 * Penis softbody appears out of sync with penis... tip goes into body?
 * Single intances of windows!!!
 * Stats dont go to zero when not used over time
 * GUI message will not all show in some res
 * Missing stats
 * PhysX complains about shape set to invalid geometry... Body colliders when idle??
 * Top of breasts for softbody getting an issue... adjust sphere??
 *++++ Penis shake on close to self body a showstopper
 * 888-771-5803 Catherine

=== PROBLEMS: ASSETS ===
 * Man texture too 'yellow'
 * Improve penis texture blend on man
 * Man head texture problem?
 * Chest up/down on man flawed!
 * Problems with man colliders running into each other... really need layers!
 * Improve room with black roof
 * Man and women bone trees have chest collider colliding against arms... need to set to own layer?

=== PROBLEMS??? ===
 * Test sex change with blender init.  that flag still of use???
 * Blender script protection a problem for install??
 * Vagina track not hidden at start?
 * Guide catching cum?

=== IDEAS ===
 * Add pose categories and ordering??
 * Add 'flip' hotkey!
 * A 'special mode' to move some hard-to-get-at hotspots (with them drawn in x-ray)
   * Hide them most of the time (in non pose mode?)
 * Add frame stamp to log entries
 * Hand pose load position... should be remembered so user can load it back!
 * F1-F4 for hand control, F5-F12 for poses
 * Save log files in diff folder?
 * Screen capture feature with output folder... with contest!
 * When cumming in pussy cap cum near entrance!
 * Map mouse button 4-5??
 * sw = Stopwatch.StartNew();
 * Do some body bends with a hotkey pressed!
 * Add additional keys for 1st person cameras!
 * iGUI supports tooltips per combo box entry!
 * Move torso with a quick key?? (Or raise hotspot of chest?
 * ++++ Add extra props from scene options!  And move them with hotspots!!
 * Have Shift+F spread all thighs??
 * Have a 'randomizer' key to enable user to dicate how 'free' character is
 * Feet separation key?
 * Cum guide in vagina & vagina cum?
 * A 'pleasure indicator' when caressing like in the meet & fuck games
 * 'Skip frame' functionality for cum!!
 * Vagina should have 'inverse cum funnel' to guide cum out of opening (doesn't collide with penis) Also put cap
 * Adjust separation between the bodies with one easy value (like penis angle?)
 * Reset pin positions to current with a hotkey (to solve 'stretching problem')
 * Move both legs in one go in 2D (pinned to floor?)

=== INVESTIGATE FEATURES ===
 * Usage for Mesh.Optimize()?
 * Usage for Mesh.Clear()? http://docs.unity3d.com/Documentation/ScriptReference/Mesh.html
 * Mesh.Topology has quads, lines, linestrip, points!
 * Mesh.MarkDynamic http://docs.unity3d.com/Documentation/ScriptReference/Mesh.MarkDynamic.html
+++ http://www.starscenesoftware.com/vectrosity.html for drawing lines!  Has awesome beziers!!

=== WISHLIST ===
 * User lighting control multiplier?
 * Cum in vagina!
 *? Usable full menu for both hands / both bodies, avoid trapping pins, smooth?
 *
=== NexT: Body Col ===
 * Breast collider oriented 90 degrees out???
 * Fluid grid size out of wack critical issue???  (Why stiffness all broken now??)
 * Good stats will become critical going forward... create a new super class on top of CProp & new GUI with link to profiler
 * 
=== PROBLEMS ===
 * FPS appears at start in player
 * WTF is fucking size of box in PhysX2/3!!!
 * When cum falls on penis base colliders make penis shake!
 * Exit when Blender is not there!
 * Gizmo mostly broken: Move will move toward camera!  (Bad rotation at init?)
 * Penis collision exposions... 

 * === TODO ===
 * Very quickly set colliders owned by cloth for faster OnUpdate... check again!!
 * Breasts not skinned!
 * Can't anim at start because cloth flies off!!  Reposition root higher??
 * Bad latency problem... gone with reposition of colliders???
 * 
 * 
 * === JUNK? ===
 * POSING FAST TRACK 
	 * DETERMINE IF WE GROUP PINS!!!
	 * How about this idea with anim curves????  Should it save our info???
	-URGENT!: Now having to photonize CBody but can't have multiple base classes!  what to do?

 * Posing load and save...
	 * Do we have any hierarchy with the pins??  (like ArmL/R belonging to Arm and it to torso...) so we save relative positions... important!!  TEST
	 * We'll need the ability to load a single character... takes quite a bit of time to do one right!
	 * OR... do we store poses as individuals and rely on the pose designer posing the couple / threesome / quad to load and place posed individuals??  BETTER!!
		 * Then the 'point of insertion (3d position where genital goes (
 * Raising body so feet are on the ground...
	 * Is that even an issue?  Do we just snap to pins and everything ok like our pose?
 * Revise decisions on posing:
	 * For first demo, do we create anims from Unity editor or with our gizmos???
	 * Collision groups ok?
	 * Weight of feet?
	 * Soften drive of pins... appear stiff
	 * Drive on thigh open not enough
	 * Knee folding?  Is that to autocrouch?
	 * Need to allow arms to drop...
 * Finger bones might cause more trouble than worth for now...  Just drive finger bones directly?
	 * Or do we abstract all four fingers as two boxes and thumb as one capsule?
	 * 'doing cool things with hands' is going to be difficult... pick first poses where they are tied up.
 * Arm behind head can be achieved with existing hand driving!!
 * Bad bending around the thighs might make cool poses impossible for now!
	 * Possible to iterate through those verts to smooth them out?
		 * Or is it better to add a bone to push them out?
 * Add show/hide pins again.

=== TODO ===
 * Have to harmonize PhysX properties for SoftBody & cloth soon...
	 * Gravity should be applied to some and not others...
	 * Reconnect a GUI to send these properties via reflection like before??
		 * Use our previous GUI slider or adopt iGUI?

=== LATER ===
 * Morphing now much simpler and more powerful with the rewrite for breast needed...

=== CURRENT PLAN TO REVENUE ===
 * Quick load and save of poses: in files through photon or anim curves???
 * Design a few ultra-hot poses by placing pins in Unity editor and saving them.
 * Implement hot animations from them... in anim curves???
 * PENETRATION!!!!

=== DESIGN ===
 * Max allowed time changed to 0.04 from .3333!!!

=== IDEAS ===
 * blendShapes and http://www.faceshift.com/unity/  Better then our solution??  How to import tho? (http://answers.unity3d.com/questions/574775/how-do-i-get-started-with-blend-shapes.html)
 * Properties really working well with client/server, GUI and scripting... worth enhancing with randomization, smooth adjust, animation, GUI control, etc.
 * Reducing density of penis softbody might make it stay in its cage more...
+++ Autofit of what pose is compatible with what other: have pose designer identify vagina angle at idle and range of motion and height...
	+ When user places a dick somewhere, code attempts to find woman poses made for that angle!  Like placing two a capsule in a sort of cone
		+ What to do about the feet tho... place invisible body first and see what collides?
 * Profiler can output to log file and accept external data (hint: C++ timing stats!)
 * IDEA: Constantly sending verts, tris and counts from different contexts... create a 'CMesh' in c++??
 * //Profiler.BeginSample("StatName");		//###LEARN: Custom stats!
 * ++++ Placing all our important objects as 'Update When Offscreen' prevents having to recalc bounds!!

=== LEARNED ===
 * Setting Unity time setting to .01 from .04 makes strobing effect of Fluid much less noticeable!  (But really slows down system!)
	 * However... setting corresponding number in C++ dll had terrible effects... what gives??

=== PROBLEMS ===
+++++ WTF body disapearing after 20 sec sometimes?? (PhysX window frozen when it happens)
	 * Log says CBSkinBaked lost its skinned mesh, but entire body node is gone!!
	 * Probably related to fluid crash... (was with SPH @ 10K)
	 * Seems to happen after 30 sec always!
	 * Could trap on destroy!
+ PhysX screwed up when we exit game and restart: Game doesn't cleanup!!
+++ Remember hack in PhotonHandler!!
 * Weird bug now with right breast more resistant to gravity???
++++ TRY to not scale penis at all frame... what happens?  Can do once in a while??
++++ Bones were all fucked...  reset sex to be less shitty but not exact...  rethink its 15 deg-off ownership of rest of bones!!
 * WTF happened with breasts & vagina being so soft now??
 * BodyA/BodyB getting a bit of pain in the ass in args everywhere... see if we can simplify?
 * Non-full game init missing meshes!
+ Unity needs to know what meshes Blender creates!  (Like panties, etc) for body to build with proper meshes!

=== PROBLEMS: ASSETS ===
 * Seam appears between breasts and armpits now

=== PROBLEMS??? ===
 * Once I saw performance drop to 23fps while the camera movement looked more like 5fps.   Checking profiler, things like CPinSkinned started taking 14ms and skin rim baked 11ms!
	 * After much testing I did a full rebuild all of the C++ dll and performance got back to 84fps?  WTF??  Why would a bad compile of DLL make C# code run much slower????
 * I think physx clock is ticking while game initializes... verify!
 * Massive rename / reorg around "BodyA' has broken tons of stuff... Many gBL calls now require full qualification!
 * Note that PhysX PVD viewer has X inverted!!!

=== WISHLIST ===
 *-- Disable gravity on some softbodies (to increase performance??)
=== WISHLIST ===
 * Desirability of a 'coarse body collider' concept (with legs, arms, etc being approximated with large capsules...
	 * Implications for accurate breasts & penis collisions
	 * 700 capsule collider limit... on any machine??  (Test on laptop)
*/


using UnityEngine;
using UnityEditor;
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
		Transform oChildTran = oNodeParent.FindChild(sName);
		if (oChildTran == null) {
			GameObject oChildGO = (oType != null) ? new GameObject(sName, oType) : new GameObject(sName);
			oChildTran = oChildGO.transform;
			oChildTran.parent = oNodeParent.transform;
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

    public static uFlex.FlexParticles CreateFlexObjects(GameObject oGO, IFlexProcessor iFlexProcessor, int nParticleCount, uFlex.FlexInteractionType nFlexInterationType, Color oColor) {
        uFlex.FlexParticles oFlexParticles = CUtility.FindOrCreateComponent(oGO, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;
        oFlexParticles.m_particlesCount = nParticleCount;                             // The non-edge particle are the ones that require driving between skinned and visible mesh.
        oFlexParticles.m_type = uFlex.FlexBodyType.Other;
        oFlexParticles.m_particles = new uFlex.Particle[nParticleCount];
        oFlexParticles.m_colours = new Color[nParticleCount];
        oFlexParticles.m_velocities = new Vector3[nParticleCount];
        oFlexParticles.m_densities = new float[nParticleCount];
        oFlexParticles.m_particlesActivity = new bool[nParticleCount];
        oFlexParticles.m_colour = oColor;
        oFlexParticles.m_interactionType = nFlexInterationType;          // The simulated particles collide with everything (other than ourselves)
        oFlexParticles.m_collisionGroup = -1;           // Flex runtime will allocate to its own 'phase' of type m_interactionType 
        oFlexParticles.m_bounds.SetMinMax(new Vector3(-1,-1,-1), new Vector3(1,1,1));        //###CHECK: Better with some reasonable values than zero?

        //=== Add particle renderer component for debug visualization ===
        uFlex.FlexParticlesRenderer oFlexPartRend = CUtility.FindOrCreateComponent(oGO, typeof(uFlex.FlexParticlesRenderer)) as uFlex.FlexParticlesRenderer;
        oFlexPartRend.m_size = CGame.INSTANCE.particleSpacing;
        oFlexPartRend.m_radius = oFlexPartRend.m_size / 2.0f;
        oFlexPartRend.enabled = false;           // Hidden by default

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
		Transform oChildT = oParentT.FindChild(sChildPath);
		if (oChildT == null)
			CUtility.ThrowException(String.Format("FindChild(Parent='{0}', ChildPath='{1}'", oParentT.name, sChildPath));
		return oChildT;
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
		Transform oBoneNew = oBoneNewRoot.FindChild(sBonePath);
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
		Transform oNodeDst = oNodeIterator.FindChild(sPathToBranchPoint);
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
	//	oMatDst.CopyPropertiesFromMaterial(oMatSrc);		//###LEARN: How to copy a material	//###WEAK: Not transfering material names!
	//	oMatDst.name = oMatSrc.name;
	//}
	#endregion

	#region === Debug Rendering ===
	//public static void BakeSkinnedMeshAndShow(Transform oNodeParent, string sNodeName, ref Mesh oMesh, Material oMat, bool bMakeVisible) {		//###OBS?
	//	//###LEARN: This will draw what is baked...  Useful for debugging!	
	//	GameObject oMeshBakedDumpGO = new GameObject(sNodeName, typeof(MeshFilter), typeof(MeshRenderer));
	//	oMeshBakedDumpGO.transform.parent = oNodeParent;
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
	//	vecNormal *= 1000;						//###LEARN: Normalize the vector when too small!!! (A bug?)   LookAt() needs a large enough vector to 'look' so we multiply then normalize
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
	//public static string BlenderStream_ReadStringPascal(ref byte[] aBytes, ref int nOffset) {		// Used to serialize strings packed by struct.pack in Blender Python.  (First byte is string lenght)
	//	byte nLen = aBytes[nOffset]; nOffset++;
	//	StringBuilder strBuilder = new StringBuilder();
	//	for (byte nChar = 0; nChar < nLen; nChar++) {
	//		strBuilder.Append((char)aBytes[nOffset]);
	//		nOffset++;
	//	}
	//	return strBuilder.ToString();
	//}
	//public static Vector3 ByteArray_ReadVector(ref byte[] aBytes, ref int nOffset) {		// Used to serialize strings packed by struct.pack in Blender Python.  (First byte is string lenght)
	//	Vector3 vec;
	//	vec.x = BitConverter.ToSingle(aBytes, nOffset); nOffset += 4;
	//	vec.y = BitConverter.ToSingle(aBytes, nOffset); nOffset += 4;
	//	vec.z = BitConverter.ToSingle(aBytes, nOffset); nOffset += 4;
	//	return vec;
	//}
	//public static Quaternion ByteArray_ReadQuaternion(ref byte[] aBytes, ref int nOffset) {		// Used to serialize strings packed by struct.pack in Blender Python.  (First byte is string lenght)
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

   // public static Color Color_HSVtoRGB(float h, float s, float v) {		//###LEARN: From http://www.cs.rit.edu/~ncs/color/t_convert.html		
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
        //=== Construct the dialog's content dependent on what type of dialog it is ===
        CUIPanel oPanel = CUIPanel.Create(oCanvas);           //####DESIGN!  ####SOON ####CLEANUP?
        int nRows = 0;
        int nPropGrps = 0;
        foreach (CObject oObj in aObjects) {
            foreach (CPropGroup oPropGrp in oObj._aPropGroups) {   //###BUG!: Inserts one extra!  Why??
                oPropGrp._oUIPanel = oPanel;                    //####IMPROVE ####MOVE??
                //////////oPropGrp.CreateWidget(oListBoxContent);
                foreach (int nPropID in oPropGrp._aPropIDs) {
                    CProp oProp = oObj.PropFind(nPropID);
                    nRows += oProp.CreateWidget(oPropGrp);
                }
                nPropGrps++;
            }
        }
        //oCanvas.transform.position = CGame.INSTANCE._oCursor.transform.position;
        //oCanvas.transform.rotation = Camera.main.transform.rotation;
    }
	#endregion

	#region === DEBUGGING ===
	public static void ThrowException(string sMsg) {
		Debug.LogError("[EXCEPTION] " + sMsg);
		EditorApplication.isPaused = true;			//###LEARN: How to programatically pause game in Unity editor.  (Doesn't work in player)
		CUtility.ThrowException(sMsg);				//###LEARN: Put breakpoint here to catch all exception and look up stack tree.
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
//	//public string Description { get; set; }			//###LEARN: From technique at http://stackoverflow.com/questions/4879521/creating-custom-attribute-in-c-sharp
//	public string Description;
//	public float Min;
//	public float Max;
//}