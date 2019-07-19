/*###IMPROVE:
- Add programatic control of panel size?
- Auto-sense widht of bodies to auto-separate.
- Auto-sense what we're separating (e.g. one body, two bodies, etc)
- Implement intimate knowledge of web browsers? (so we can resize them from here?)
- Taper center height adjustment to not quite go to cam level?
*/ 

using UnityEngine;

public class CPaneRot: MonoBehaviour {                  // CPaneRot: Automatically rotates this object to face the camera.  Used for GUI panels (that are child of this object) to always face camera.

    Transform           _oPaneRot_Root;                    // Root rotator = Our own transform.  Always rotate yaw toward camera.
    Transform           _oPaneRot_Center;                  // Center rotator.  Always moved up/down to keep at the camera height.
    Transform           _oPaneRot_PivotL;                  // Left  panel pivot.   Always rotates its yaw to point toward camera (from the perspective of _oPaneRot_SensorL)
    Transform           _oPaneRot_PivotR;                  // Right panel pviot.   Always rotates its yaw to point toward camera (from the perspective of _oPaneRot_SensorR)
    Transform           _oPaneRot_SensorL;                 // Left  panel sensor.  Senses orientation to camera for pivot
    Transform           _oPaneRot_SensorR;                 // Right panel sensor.  Senses orientation to camera for pivot
    public Transform    _oPaneRot_AnchorL;                 // Left  panel anchor.  The parent of the actual pane.
    public Transform    _oPaneRot_AnchorR;                 // Right panel sensor.  The parent of the actual pane.

    public  Vector3      _vecSize_PaneDocked = new Vector3(0.5f, 0.9f, .001f);       // Size of the panes we dock.  We need to know their width so we can properly space the 'sensor' and 'anchor' at start
            Vector3      _vecSize_PaneDocked_COMPARE = new Vector3();
    public  float        _LeftRightDistFromCenter = 0.3f;    // The distance between the left or right anchors from their center.  Enables bodies to fit in the center with the GUI panes rotating around them without clipping anything
            float        _LeftRightDistFromCenter_COMPARE;   // Compare variable to update locations only when changing

    Vector3             _eulRot;                            // Temporary scratch variables to avoid GC.
    Vector3             _vecPos;
    Vector3             _vecPos_Camera;

    void Awake() {
        enabled = false;
    }

    public void DoStart() {
        _oPaneRot_Root = transform;
        _oPaneRot_Center       = _oPaneRot_Root  .Find("CPaneRot_Center");
        _oPaneRot_PivotL       = _oPaneRot_Center.Find("CPaneRot_PivotL");
        _oPaneRot_PivotR       = _oPaneRot_Center.Find("CPaneRot_PivotR");
        _oPaneRot_SensorL      = _oPaneRot_Center.Find("CPaneRot_SensorL");
        _oPaneRot_SensorR      = _oPaneRot_Center.Find("CPaneRot_SensorR");
        _oPaneRot_AnchorL      = _oPaneRot_PivotL.Find("CPaneRot_AnchorL");
        _oPaneRot_AnchorR      = _oPaneRot_PivotR.Find("CPaneRot_AnchorR");
        enabled = true;
    }

    void Update () {                //###OPT: Can run less frequently than every frame?
        _vecPos_Camera = Camera.main.transform.position;

        //=== 1. Update the sensor and anchor children if the pane size has changed ===
        if (_vecSize_PaneDocked.x != _vecSize_PaneDocked_COMPARE.x) {
            float nDistFromPivot = _vecSize_PaneDocked.x / 2;
            _oPaneRot_SensorL.localPosition = new Vector3( nDistFromPivot + _LeftRightDistFromCenter, 0, 0);
            _oPaneRot_SensorR.localPosition = new Vector3(-nDistFromPivot - _LeftRightDistFromCenter, 0, 0);
            _oPaneRot_AnchorL.localPosition = new Vector3( nDistFromPivot, 0, 0);
            _oPaneRot_AnchorR.localPosition = new Vector3(-nDistFromPivot, 0, 0);
            _vecSize_PaneDocked_COMPARE = _vecSize_PaneDocked;
        }

        //=== 2. Orient the root yaw toward the camera.  This will influence the whole rig ===
        _oPaneRot_Root.LookAt(Camera.main.transform);          // Simply orient toward the camera.  Works great! :)
        _eulRot = _oPaneRot_Root.localRotation.eulerAngles;
        _eulRot.x = _eulRot.z = 0;                              // Ditch the roll and pitch to keep only the yaw
        _oPaneRot_Root.rotation = Quaternion.Euler(_eulRot);

        //=== 3. Raise / lower the center to the camera's height ===
        _vecPos = _vecPos_Camera;
        _vecPos.x = _vecPos.z = 0;                              // Ditch the left / right & forward / backward to leave only height
        _oPaneRot_Center.localPosition = _vecPos;

        //=== 4. Adjust the yaw of the left  panel anchors to point toward the camera ===
        if (_LeftRightDistFromCenter_COMPARE != _LeftRightDistFromCenter)
            _oPaneRot_PivotL.localPosition = new Vector3(_LeftRightDistFromCenter, 0, 0);
        _oPaneRot_SensorL.LookAt(Camera.main.transform);       // Orient the anchor toward the camera but rotate the pivot
        _eulRot = _oPaneRot_SensorL.localRotation.eulerAngles;
        _eulRot.x = _eulRot.z = 0;                              // Ditch the roll and pitch to keep only the yaw
        _oPaneRot_PivotL.localRotation = Quaternion.Euler(_eulRot);

        //=== 5. Adjust the yaw of the right panel anchors to point toward the camera ===
        if (_LeftRightDistFromCenter_COMPARE != _LeftRightDistFromCenter)
            _oPaneRot_PivotR.localPosition = new Vector3(-_LeftRightDistFromCenter, 0, 0);
        _oPaneRot_SensorR.LookAt(Camera.main.transform);       // Orient the anchor toward the camera but rotate the pivot
        _eulRot = _oPaneRot_SensorR.localRotation.eulerAngles;
        _eulRot.x = _eulRot.z = 0;                              // Ditch the roll and pitch to keep only the yaw
        _oPaneRot_PivotR.localRotation = Quaternion.Euler(_eulRot);

        _LeftRightDistFromCenter_COMPARE = _LeftRightDistFromCenter;
    }
}
