using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CBrowserInputVR : ZenFulcrum.EmbeddedBrowser.FPSBrowserUI {
    //public RaycastHit _oRayHit_BrowserUI;

    //Ray _oRay = new Ray();

    //new void Start () {
    //    base.Start();
    //    //ZenFulcrum.EmbeddedBrowser.FPSCursorRenderer.SetUpBrowserInput(GetComponent<CBrowser>(), GetComponent<MeshCollider>());
    //}

    protected override Ray LookRay {
        get {
            //_oRay.origin    = CGame._oHeadsetT.transform.position;
            //_oRay.direction = CGame._oHeadsetT.transform.forward;
            return CGame._oHeadsetT._oRayHeadsetVR;
        }
    }

    public override void InputUpdate() {
        //Note: keyEvents gets filled in OnGUI as things happen. InputUpdate get called just before it's read.
        //To get the right events to the right place at the right time, swap the "double buffer" of key events.
        var tmp = keyEvents;
        keyEvents = keyEventsLast;
        keyEventsLast = tmp;
        keyEvents.Clear();

        //Trace mouse from the main camera
        //Ray mouseRay = LookRay;
        //bool bHit = Physics.Raycast(mouseRay, out _oRayHit_BrowserUI, maxDistance, G.C_LayerMask_UI);      //###INFO: Raycast hit.Collider != hit.RigidBody or hit.Transform!!!  WTF?????????

        //if (bHit)
        //    CGame._aDebugMsgs[(int)EMsg.Dev3] = string.Format("BrowserUI: T: '{0}',  Col: '{1}',  Layer: {2},  {3}, {4}", hit.transform.gameObject.name, hit.collider.gameObject.name, hit.transform.gameObject.layer, hit.textureCoord.x, hit.textureCoord.y);
        //else
        //    CGame._aDebugMsgs[(int)EMsg.Dev3] = string.Format("BrowserUI: None");
        CVrHeadset oVrHeadset = CGame._oHeadsetT;

        if (oVrHeadset && oVrHeadset._bHitUI && oVrHeadset._oRayHit.collider.transform == meshCollider.transform) {       // If we hit a collider and this collider is our collider we process this input as our own
            LookOn();
            MouseHasFocus = true;
            KeyboardHasFocus = true;

            //convert ray hit to useful mouse position on page
            var localPoint = oVrHeadset._oRayHit.textureCoord;
            MousePosition = localPoint;

            if (CGame._eGameModeVR == EGameModeVR.None) {
                // When we don't have a real VR headset we pull mouse buttons directly from the mouse itself...
                var buttons = (ZenFulcrum.EmbeddedBrowser.MouseButton)0;
                if (Input.GetMouseButton(0))
                    buttons |= ZenFulcrum.EmbeddedBrowser.MouseButton.Left;
                if (Input.GetMouseButton(1))
                    buttons |= ZenFulcrum.EmbeddedBrowser.MouseButton.Right;
                if (Input.GetMouseButton(2))
                    buttons |= ZenFulcrum.EmbeddedBrowser.MouseButton.Middle;
                MouseButtons = buttons;
                MouseScroll = Input.mouseScrollDelta;
            } else {
                // When we have a real VR headset we grab the mouse button from a wand joystick direction
                bool bWandWebBrowserButtonPressed = CGame._oVrWandL.Input_GetButton_BrowserLeftMouseButton() || CGame._oVrWandR.Input_GetButton_BrowserLeftMouseButton();
                MouseButtons = bWandWebBrowserButtonPressed ? ZenFulcrum.EmbeddedBrowser.MouseButton.Left : 0;
                MouseScroll = Vector2.zero; //###IMPROVE: Feed joystick in some exclusive browser input mode?? Input.mouseScrollDelta;
            }

            //Unity doesn't include events for some keys, so fake it by checking each frame.
            for (int i = 0; i < keysToCheck.Length; i++) {
                if (Input.GetKeyDown(keysToCheck[i])) { //Prepend down, postpend up. We don't know which happened first, but pressing modifiers usually precedes other key presses and releasing tends to follow.
                    keyEventsLast.Insert(0, new Event() { type = EventType.KeyDown, keyCode = keysToCheck[i] });
                } else if (Input.GetKeyUp(keysToCheck[i])) {
                    keyEventsLast.Add(new Event() { type = EventType.KeyUp, keyCode = keysToCheck[i] });
                }
            }
        } else {        //not looking at it.
            MousePosition = new Vector3(0, 0);
            MouseButtons = 0;
            MouseScroll = new Vector2(0, 0);
            MouseHasFocus = false;
            KeyboardHasFocus = false;
            LookOff();
        }
    }
}
