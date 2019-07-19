using UnityEngine;

public class CVrHeadset : MonoBehaviour {
    public Ray          _oRayHeadsetVR = new Ray();
    public RaycastHit   _oRayHit;
    public Transform    _oHeadsetGazeT;                 
    Light               _oHeadsetGazeLight;             // Tiny point light of a child of Headset Gaze.  Used to point 3D body locations to the user in an immersive way
    MeshRenderer        _oHeadsetGazeT_Renderer;
    public bool         _bShowHeadsetGazeCursor;
    public bool         _bHitBody;                      // Raycaster has hit a body this frame
    public bool         _bHitUI;                        // Raycaster has hit a browser GUI this frame

    void Awake() {
        enabled = false;
    }

    public void DoStart() {
        //=== Obtain access to our various mover transforms ===
        _oHeadsetGazeT = CGame.INSTANCE.transform.Find("CVrWand_HeadsetGaze");
        _oHeadsetGazeT_Renderer = _oHeadsetGazeT.GetComponent<MeshRenderer>();
        _oHeadsetGazeLight = _oHeadsetGazeT.GetChild(0).GetComponent<Light>();
        enabled = true;
    }

    void FixedUpdate() {
        _oRayHeadsetVR.origin    = transform.position;
        _oRayHeadsetVR.direction = transform.forward;
        bool bHit = Physics.Raycast(_oRayHeadsetVR, out _oRayHit, float.MaxValue, G.C_LayerMask_HeadsetRaycaster);      //###INFO: Raycast against body colliders AND UI browsers!

        if (bHit) {
            MeshCollider oMeshCollider  = _oRayHit.collider as MeshCollider;
            CSurfaceMesh oSurfaceMesh   = oMeshCollider.GetComponent<CSurfaceMesh>();

            //=== Has a body collider been hit?  Update the 3D cursor for other code to pull from ===
            if (oSurfaceMesh) {
                Mesh oMeshBaked = oSurfaceMesh.Baking_GetBakedSkinnedMesh();

                int[] aTris         = oMeshBaked.triangles;
                Vector3[] aVerts    = oMeshBaked.vertices;
                Vector3[] aNormals  = oMeshBaked.normals;

                int nTriX3 = _oRayHit.triangleIndex * 3;
                Vector3 vecBarycentric = _oRayHit.barycentricCoordinate;

                int nTri0 = aTris[nTriX3 + 0];
                int nTri1 = aTris[nTriX3 + 1];
                int nTri2 = aTris[nTriX3 + 2];
                int nTriSurfaceMax = 0;
                if (oSurfaceMesh._aSurfaceAreaVerts != null) {      //#DEV26 Vr Hack for FlexTriCol
                    int nTriSurface0 = oSurfaceMesh._aSurfaceAreaVerts[nTri0];
                    int nTriSurface1 = oSurfaceMesh._aSurfaceAreaVerts[nTri1];
                    int nTriSurface2 = oSurfaceMesh._aSurfaceAreaVerts[nTri2];
                    nTriSurfaceMax = Mathf.Max(nTriSurface0, nTriSurface1, nTriSurface2);
                }

                Vector3 v0 = aVerts[nTri0];
                Vector3 v1 = aVerts[nTri1];
                Vector3 v2 = aVerts[nTri2];
                Vector3 n0 = aNormals[nTri0];
                Vector3 n1 = aNormals[nTri1];
                Vector3 n2 = aNormals[nTri2];
                Vector3 vecPosition = v0 * vecBarycentric.x + v1 * vecBarycentric.y + v2 * vecBarycentric.z;     //###INFO: From https://docs.unity3d.com/ScriptReference/RaycastHit-barycentricCoordinate.html
                Vector3 vecNormal = -(n0 * vecBarycentric.x + n1 * vecBarycentric.y + n2 * vecBarycentric.z);
                Quaternion quatRotation = Quaternion.LookRotation(vecNormal);

                _oHeadsetGazeT.position = vecPosition;
                _oHeadsetGazeT.rotation = quatRotation;

                _bHitBody = true;

            //=== Has a Web Browser GUI collider been hit?  Update the browser cursor position ===
            } else {
                _bHitUI = true;
            }
        } else {
            _bHitBody = _bHitUI = false;
        }
#if __DEBUG__
        //_oHeadsetGazeT_Renderer.enabled = (bCursorHitsValidSurface && bShowCursor);
#endif
        _oHeadsetGazeLight.enabled = (bHit && _bShowHeadsetGazeCursor);
    }
}
