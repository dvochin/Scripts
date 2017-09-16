/*###DOCS24: Aug 2017 - Blender code consolidation effort
=== DEV ===

=== NEXT ===

=== TODO ===

=== LATER ===

=== OPTIMIZATIONS ===

=== REMINDERS ===

=== IMPROVE ===

=== NEEDS ===

=== DESIGN ===

=== QUESTIONS ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== WISHLIST ===

*/







/*=== CENTRAL DESIGN OF GAME USER INTERFACE ===
> The following control permutations are possible:
- Right-handed mouse users *must* position their left  hand on the WASD keyboard area and their right hand on the mouse.
- Left -handed mouse users *must* position their right hand on the WASD keyboard area and their left  hand on the mouse. (ie. no remapping of keys to right of keyboard!)
- Right-handed wand  users *must* position their left  hand on the WASD keyboard area and their right hand on the right VR wand.
- Left -handed wand  users *must* position their right hand on the WASD keyboard area and their left  hand on the left  VR wand.
- Note that both hands on the VR wands has LIMITED capabilities.  (Users can only move both pelvis and must invoke mode with a special key = meant to be used for short periods of time)

> ALL game interactions occur via 'one-context-at-a-time' GUIs that is either 1) shown-while-pressed corner label or 2) a full-featured system-modal dialog box that is opened / closed.
- The 'super-basic GUI label' is little more than an immersion-preserving static colored label in the upper-left corner.It providing feedback on how input devices are changing values pertinent to the editing mode. (e.g. 'Editing Penis Erection: 60%')
- The 'full featured panel' is a moveable system-modal dialog box of arbitrary complexity that breaks immersion(obscures a part of the scene) but offer a rich interaction experience.
- There is only ONE GUI ever shown on the screen at one time. (no multiple modeless dialog boxes with counter-intuitive focus issues)
- The super-basic GUI is shown only while the action key is held down.The full-featured panel is opened by double-tapping the action key and closed by the user clicking the 'X' icon
- The full-featured panel reveals the immersion-breaking 2D mouse (in mouse mode) and the 'headset gazing point' (in VR mode)
- Invoking the LMB / Wand Trigger on 'body hotspot areas' opens the full-featured panel to edit the body part.  (In this flow the user may not be aware of the input-device remapping)

> The basic GUI's purpose is as follows:
- It is meant to perform basic editing of body parts without breaking immersion.
- Is meant to be shown only while the action key is held down (e.g. 5-10 seconds)
- Is rendered in the upper-left corner of the screen as a simple un-editable colored label.
- Is rendered only with static controls.  User cannot 'click' or select any of them(but can otherwise edit values via input-device mapping as described below)
- It renders a simple string like 'Editing Breasts', 'Editing Man Position' when no input device changes.
- It renders a simple string like 'Editing Penis Size: [----42%----]', 'Editing Ejaculation Size: [-------80%--]' when user changes a 1D input axis(e.g.mouse wheel)
- It renders a simple 2D square when user changes a 2D input axis(e.g.VR wand joystick, mouse position)
- Can have additional simple static GUI elements drawn below as warranted:  
	- Purple 'beginner tips' that display what can be done in the current context.  (e.g. 'Use mouse wheel to adjust penis size', 'Right-click-drag to move camera target', etc)

> The 'full-featured panel' functions as follows:
- It is created to perform extensive editing of the body part over a longer period of time. (20-60 seconds)
- It can be of considerable complexity with many controls delivering a highly-complex interactive experience.
- Can render additional widgets based on certain modes(e.g.show 'advanced controls', 'developer features', 'property limits', etc)

> Fast input device mapping to the context of the action key.
- Both types of GUIs capture the 'focus' of the mouse and VR wand inputs for extremely rapid and intuitive editing via mouse / joystick / mouse wheel.Some examples:
	- Holding the 'Woman key' (W) and middle-click-dragging the mouse moves her pelvis along the 'pose plane' (Holding a button moves master Genitals bone instead for posing?)
	- Holding the 'Man key' (E) and moving the wand moves his pelvis.  (Pressing grip switchs to 'Genitals' master pin for posing?)
	- Holding the 'Man key' (E) and moving the VR wand joystick around bends the penis in the joystick's direction.
	- Holding the 'Breast key' (B) and moving the mouse wheel up or down grows / shrinks the breasts
	- Holding the spacebar and middle-click-dragging the mouse moves the camera target along the pose plane.
	- Holding the 'Left Hand Key' and mouse-clicking on a breast will move that hand to the breast and fondle it.
	- Holding the 'Right Hand Key', looking at the penis and pressing the VR wand trigger will make the hand stroke the penis where the user is looking.
	- NOTE: When the 'full-featured panel' is shown the same behavior is active.  (This is why we can't grab left-click-drag (moving body parts) / right-click-drag (for camera rotate) so only middle-click-drag is used

> The Command Queue:
- Commands are accumulated in the 'Command Queue' and can be shown with a toggle key in the 'Command Queue Window'
	- This window is the basis of our sequence editor (and a key element to 'Phase I')  (Do we implement it in SmartGWT browser?)

=== TODO ===
- Make a final decision on the highly-limited but super-important VR buttons!!
- Precisely design how the mouse and VR wand operate when no action key is pressed!
- Map the WASD area(is there an editing tool?)
- Determine the meaning of the important keys like spacebar, tab, shift, control, alt, windows, etc
- Can we afford a full-body mapping(six keys alone for left/right/both arms & legs?)


=== Implementation ===
- Use Unity's keyboard functionality so users can change keys!
- It would be useful if user can re-purpose an open panel by clicking on the 'owning body' icon in the upper left
	- These icons look like the male, female, shemale glyphs(blue for men, pink for women, purple for shemale)

=== Questions ===
- What about our current cool VR panel poping up when we raise hand to headset?
- Total input re-direction is amazing in basic-GUI mode but do the demand of rich GUI editing break some of the features of the basic mode?
- We can use mouse-wheel up/down & middle-click-drag in both basic & full-featured GUI but anything else from the mouse??
- VR input-device remapping can simultaneously change a LOT of values!  (6 degrees of freedom + 2 joystick axis + trigger axis + grip axis = 10 axis!!!)
- What do we do with VR buttons during remapping?
- Which method to use when rendering the complex GUI in VR?  In 3D scene?  Floating with headset?
- How do we tie all this with a speech interface?  (Now that this is so fast does it have much value?)
- How does user rotate the camera (right-click-drag)?
	- What about changing camera focus about pose plane... Function keys??  Ctrl + mouse move

=== Ideas ===
- Have all the tutorials as Browser-delivered videos created from a heavily-frame-reduced capture of the game rendering talking points & game view?
- Show keyboard commands in-game by trapping hotspots on a 3d keyboard! (https://archive3d.net/?tag=keyboard)





============================================================================	OLD IDEAS ON KEYBOARD CHOARDS & FSM ACTION PROCESSING = OBSOLETE BUT STILL HAVE SOME VALUE IN HOW SEQUENCER COULD WORK
===== CENTRAL DESIGN OF ACTION INTERFACE ====
- Shown-until-closed 'Modeless Mode': Panel is created while pressing the LMB or wand trigger while hovering over a body hotspot area.  
	- This opens the panel in its modeless mode.  (The user can leave it floating around as long as needed and click its 'X' button when done)
- Shown-while-key-held 'System Global Mode': Panel is created while pressing and HOLDING the appropriate 'body part key' in the WASD area.  The panel is dismissed when its key is released.
	- Clicking the 'pin' icon in the panel's upper right converts the one-and-only temporary panel into a modeless panel that can be moved.  This panel will persist until closed.

> Shown-until-closed modeless panels can also be invoked in the following ways:
- The user can double-tap the panel's key.
- The user can select that panel in a global list of all possible panels that can be shown on all bodies.


The action interface is in charge of:
- Combining everything the codebase can do in an unified and coherent user interface.
- Unifying the steps a user needs to do to invoke an action via keyboard string commands, voice command, GUI panels(both top-level and context sensitive)

Examples:
L B         [Character Select] <Left Hand> On [Qualifier of erogenous locations like 'Breasts', 'Penis', 'Vagina', 'Mouth']
C 9			[Character Select] <Penis> Cum [Qualifier on cum control like 'a little', 'incontrollably', 'precum', etc]
F D[Character Select] <Vagina> Fuck [Qualifier on how to fuck like 'fast', 'slow', 'auto', etc]
F S +		[Character Select] <Penis> Fuck Speed [Amount qualifier like 'Up', 'Down', 'Minimum', 'Maximum', 0-9, etc]
P E L[Character Select] <Penis> Erection [Amount qualifier like 'Up', 'Down', 'Minimum', 'Maximum', 0-9, etc]
P V[Character Select] <Penis> in Vagina
V T[Character Select] <Vagina> Tease

--- Implementation ---
Implemented like a Finite State Machine where each 'state' is an instance of a CActionState and organized as a strict tree(NOT a graph!)
- Each CActionState has one and only one parent to form a strict tree of commands that can be coherently invoked through a chord of keyboard keys, a fixed sequence of spoken words, a command formed from a 'global command helper' GUI, etc.
	- Each of the above is able to do its task from the rigid tree that is formed from the parent / child relationships between the hundreds of CActionState instances.


CActionState oAS_Root           = new CActionState(null, "ROOT")
CActionState oAS_Penis          = new CActionState(oAS_ROOT, "Penis", KeyCode.P)
CActionState oAS_PenisErection  = new CActionState(oAS_Penis, "Erection", KeyCode.E)


--- The four elements of an action ---
1. [Character Select]: The mandatory first element of every action.  Determines exactly which CBody instance carries the command.
	- This can be determine in absolute ways with the 1-9 choice (directly lookup into our _aBodyBase array) or constructed from lookup code like 'Woman', 'Selected' for a more natural-sounding command
2. <Action Object>: The 'body part' on the character that performs the command ('Left Hand', 'Penis', 'Vagina', 'Body', 'Legs')

3. (Action): The context-sensitive action the <Action Object> can do
	- e.g. the leaf in the tree the user chose like 'Right Hand on Breast'
4. [Qualifier]: An optional or mandatory qualifier (depends of #3 action node)
	- Provides context sensitive direction that makes sense to the action node's implementation.  (e.g. 'incontrollably' to the Cum command)
To insert an action into the action framework an 'action builder' must specify all three.

--- Action Builders ---
- Various 'Action Builders' are implemented to construct actions:
	- A 'voice recognition builder' populates the grammar graph with our tree of words to extract the character select, the leaf action node and the optional qualifier
	- A 'global command builder GUI' populates a GUI panel with buttons that enumerate the next expected choice from tree-root to tree-leaf.
		- E.g. selecting the 'Left Hand' button would highlight that button, dim the other choices, scroll the panel to the 'next question' and present buttons to enable the user to choose where the left hand should go ('Penis', 'Breast', etc)
	- A 'context builder' is a version of the 'global command builder GUI' with the [Character Select]

--- Action Objects ---
- Body:
- Erogenous zones: Penis, Vagina, Mouth:

=== Questions ===
- All commands can only influence one character at a time!  How to control an animation involving two bodies?  How to load a pose?

=== Ideas ===
-OBSOLETE: Numeric keypad to uniquely identify each body part?
	|NL: [BODY SEL] |/:	HEAD        |*:				|
	|7:	LEFT ARM    |8:	BOTH ARMS   |9:	RIGHT ARM   |
	|4:		?		|5:	GENITALS    |6:		?		|
	|1:	LEFT LEG    |2:	BOTH LEGS   |3:	RIGHT LEG   |
	|0:	BODY                        |.:				|
	- We use 'Num Lock' to select the current body  (e.g. Num Lock on = character with penis, off = vagina)
- Have user press and hold WASD keys to display context menu of what to do.

=== Notes ===
- Error-checking is performed as the command figures out how to do the user command.  (If for example the user invokes a penis command on a woman character an error is returned instead)
*/






/*
=== DESIGN ===
--- FUCKING DESIGN ---
- Entire pose has its origin at the base of the man's penis
- The 'penetration point' is mathematically computed from the base of penis with some X,Y angle and a 'roll' from the woman about penis tip axis
	- Penetration can only occur when the softbody penis converges exactly at the penetration point.
		- Soft body particle damping and eventual spring-into-position help make this occur quickly.
	- Once the penis has stabilized (FSM checks), various animations between the man and woman can take place.
		- Some animations are for fucking, other clit rub, rub on top of penis, under penis, etc.
- Woman's legs are moved programmatically according to
- Q: The above is male-centric... is there a need for female-centric animations?  how?

=== IDEAS ===
- You can use Application.RegisterLogCallback to get everything that goes into the unity log.   See http://answers.unity3d.com/questions/232589/is-there-a-way-to-catch-global-application-expceti.html
- Read https://unity3d.com/learn/tutorials/temas/best-practices/assets-objects-and-serialization for ideas on object serialization
- IDEA: Have colliders be defined in Blender and serialized during

=== VRTK LEARNED ===
###INFO: How to disable FUCKING Chaperone system (Dumb cyan circle drawn on floor over every frame): https://www.reddit.com/r/oculus/comments/5jgg0w/how_to_disable_chaperone_in_steam_vr/  (Set 'CollisionBoundsColorGammaA' to 0 in 'Steam/config/steamvr.vrsettings')
VRTK SAMPLES TO STUDY  ###SOURCE: https://github.com/thestonefox/VRTK/blob/master/EXAMPLES.md
- 011_Camera_HeadSetCollisionFading could help figure out fading problem?
- 015_Controller_TouchpadAxisControl has a cute car remotely controlled...
- 016_Controller_HapticRumble for vibration feedback
- 017_CameraRig_TouchpadWalking for joystick cam movement
- 018_CameraRig_FramesPerSecondCounter has easy info on HMD, FPS and illustrates FPS needs
- 019_Controller_InteractingWithPointer shows how to interact remotely via pointer
- 025_Controls_Overview has various 3D widgets being controlled (not Unity GUI)
- 029_Controller_Tooltips has button tooltips on controller... they appear/dissapear when in a certain cone of vision.  Cone calcs useful?
+ 030_Controls_RadialTouchpadMenu: Very neat radial menu that fades in/out.  Can go on controllers or objects!  Fade is neat!
+ 031_CameraRig_HeadsetGazePointer: Shows how to determine what headset is looking at.  Also has a remote-control sphere that gazes and teleports!
+ 032_Controller_CustomControllerModel shows how to have own mesh for hands 
++034_Controls_InteractingWithUnityUI: extensive sample with useful features: Drag from panel could be fun!  Also scrolly for 2D usage
- 035_Controller_OpacityAndHighlighting has controller being transluscent with labels for buttons used
+ 040_Controls_Panel_Menu: Shows a panel when grabbing but unclear how to use widgets!
Quaternion quatRotLookToBoneStart = Quaternion.RotateTowards(quatRotLook, _oBoneT.rotation, float.MaxValue);		//###INFO: This call will gradually rotate from a to b (slerp like)

=== PROBLEMS ===
- Problem with depth of cursor and GUI... when far we get greater depth... Is this a problem between the overlay cam and main one?  (Or just for VR / Space Navigator??)
	- Add various 'cursor depth traps' around major body parts so cursor has a max depth?
	- Or... use body PhysX colliders?  BETTER! -> map to cursors??

=== OLD PROBLEMS??? ===
- Mouse-build GUI / CURSORS... still relevant?
	- Rethink anchor points now that we can clip a little bit.
	- Can't select a widget without unselecting first one
	- Move canvas center or owning pin?
	   - Finalize pin
	- Slider grabber bigger
	- Mesh show / hide shows extra softbodies, rim, collider body, sb skinned, etc
	- Why are pins so small now?
	- Cursor hit buggy as shit... redo!

=== WISHLIST ===
- Would be nice to have SB auto-reset during teleports!
*/




/*###DOCSxx: Aug 2017 - Description
=== DEV ===

=== NEXT ===

=== TODO ===

=== LATER ===

=== OPTIMIZATIONS ===

=== REMINDERS ===

=== IMPROVE ===

=== NEEDS ===

=== DESIGN ===

=== QUESTIONS ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== WISHLIST ===

*/
