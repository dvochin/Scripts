using UnityEngine;
using System;
using System.Collections.Generic;


public class CSurfaceMesh : CBSkinBaked {               // CSurfaceMesh: A mesh collider created from baked mesh created every frame from a reduced-geometry penis.  Used to open vagina via its raycasting approach to penetration
    MeshCollider            _oMeshCollider;             //###DESIGN: Relationship between CSurfaceMesh and derived CFlexTriCol is awkward and convoluted.  Redo inheritance??
    public CSurfacePath     _oSurfacePath;
    public CSurfaceArea[]   _aSurfaceAreas;             // The collection of 'defined body surface areas'.  Defines areas like 'belly' or 'breast-bottom', 'thigh-upper-front', etc
    public byte[]           _aSurfaceAreaVerts;         // Map surface body collider verts to meaningful body areas.  Each vert's area is a lookup into _aSurfaceAreas

    public override void OnDeserializeFromBlender(params object[] aExtraArgs) {
        base.OnDeserializeFromBlender(aExtraArgs);
    }

    public void InitializeSurfaceMesh() {               
        gameObject.layer = G.C_Layer_BodySurface;       //###IMPROVE: Set from arg?
        _oMeshCollider = CUtility.FindOrCreateComponent(gameObject, typeof(MeshCollider)) as MeshCollider;
        _oMeshCollider.sharedMesh = Baking_GetBakedSkinnedMesh();
        _oMeshCollider.material = CGame._oPhysMat_Friction_Lowest;      //###CHECK ###DEV27

        //=== Serialize and populate CSurfaceArea, then develop raycast to display text on surface area ===
        CByteArray oBA_SurfaceAreas = new CByteArray("'CBody'", _oBodyBase._sBlenderInstancePath_CBodyBase + ".oBody.aSurfaceAreas.Unity_GetBytes()");
        int nSurfaceAreas = oBA_SurfaceAreas.ReadByte();
        _aSurfaceAreas = new CSurfaceArea[nSurfaceAreas];
        for (int nSurfaceArea = 0; nSurfaceArea < nSurfaceAreas; nSurfaceArea++)
            _aSurfaceAreas[nSurfaceArea] = new CSurfaceArea(oBA_SurfaceAreas.ReadString());

        //=== Receive the aSurfaceAreaVerts array Blender created to map surface body collider verts to meaningful body areas like 'belly' or 'breast-bottom', etc
        List<byte> aSurfaceAreaVerts = CByteArray.GetArray_BYTE("'CBody'", _oBodyBase._sBlenderInstancePath_CBodyBase + ".oBody.aSurfaceAreaVerts.Unity_GetBytes()");
        _aSurfaceAreaVerts = aSurfaceAreaVerts.ToArray();
    }

    public override void OnSimulate() {
        base.OnSimulate();
        if (_oMeshCollider)
            _oMeshCollider.sharedMesh = Baking_GetBakedSkinnedMesh();
    }

    public void SurfacePath_CreateNewPath() {
        if (_oSurfacePath == null)
            _oSurfacePath = new CSurfacePath(this);
        else
            _oSurfacePath.CreateNewPath();
    }

    public void SurfacePath_AddPoint(Ray oRay) {
        RaycastHit oRayHit;
        bool bHit = Physics.Raycast(oRay, out oRayHit, float.MaxValue, 1 << gameObject.layer);
        if (bHit) {
            Collider oColFound = oRayHit.collider;
            Transform oMarkerRayT = GameObject.Find("(DEV)/DEV_Marker_BodyColPath_Ray_HACK").transform;       //#DEV26: Keep?
            oMarkerRayT.position = oRayHit.point;
            if (oRayHit.triangleIndex != -1) {
                _oSurfacePath.AddPoint(oRayHit.triangleIndex, oRayHit.barycentricCoordinate);
            } else {
                CUtility.ThrowException("Surface Path got a hit with no triangle index!!");
            }
        }
    }

    public void SurfacePath_BeginPlayback() {
        _oSurfacePath._bPlayback = true;
        //CGame._aBodyBases[0]._oActor_ArmL._oJoint_Extremity.angularXMotion = ConfigurableJointMotion.Free;
        //CGame._aBodyBases[0]._oActor_ArmL._oJoint_Extremity.angularYMotion = ConfigurableJointMotion.Free;
        CGame._aBodyBases[0]._oActor_ArmL._oObj.Set("Pinned", 1);
    }


    void Update() {
        if (_oMeshCollider) {           //#DEV26: ###WEAK:!! Dual purpose... implement better!
            if (_oSurfacePath != null) {
                _oSurfacePath.UpdatePoints();           //#DEV26: ###TEMP?
                if (_oSurfacePath._bPlayback && (CGame._nFrameCount % 10) == 0)
                    _oSurfacePath.PlaybackNext();
            }
        }
    }
}



public class CSurfacePoint : IDisposable {
    int                 _nTriX3;
    Vector3             _vecBarycentric;
    public Vector3      _vecPosition;
    public Vector3      _vecNormal;
    public Quaternion   _quatRotation;
    Transform           _oMarkerT;

    public CSurfacePoint(int nTri, Vector3 vecBarycentric) {
        _nTriX3 = nTri * 3;
        _vecBarycentric = vecBarycentric;
        _oMarkerT = CUtility.InstantiatePrefab<Transform>("Prefabs/MarkerS", "SurfacePoint-" + nTri.ToString(), CGame.INSTANCE.transform);
    }

    ~CSurfacePoint() {
        Debug.Log("!!");        //###IMPROVE: Autocleanup marker from here??
    }

    public void Dispose() {
        if (_oMarkerT) {
            GameObject.Destroy(_oMarkerT.gameObject);
            _oMarkerT = null;
        }
    }

    public Vector3 UpdatePosition(ref int[] aTris, ref Vector3[] aVerts, ref Vector3[] aNormals) {
        int nVert0 = aTris[_nTriX3 + 0];
        int nVert1 = aTris[_nTriX3 + 1];
        int nVert2 = aTris[_nTriX3 + 2];
        Vector3 v0 = aVerts[nVert0];
        Vector3 v1 = aVerts[nVert1];
        Vector3 v2 = aVerts[nVert2];
        Vector3 n0 = aNormals[nVert0];
        Vector3 n1 = aNormals[nVert1];
        Vector3 n2 = aNormals[nVert2];
        _vecPosition    =   v0 * _vecBarycentric.x + v1 * _vecBarycentric.y + v2 * _vecBarycentric.z;     //###INFO: From https://docs.unity3d.com/ScriptReference/RaycastHit-barycentricCoordinate.html
        _vecNormal      = -(n0 * _vecBarycentric.x + n1 * _vecBarycentric.y + n2 * _vecBarycentric.z);
        _quatRotation = Quaternion.LookRotation(_vecNormal);
        _oMarkerT.position = _vecPosition;
        _oMarkerT.rotation = _quatRotation;
        return _vecPosition;
    }
}

public class CSurfacePath : IDisposable {
    public CSurfaceMesh             _oSurfaceMesh;
    public List<CSurfacePoint>  _aPoints = new List<CSurfacePoint>();
    public bool                     _bPlayback;
    public int                      _nPlaybackPoint;

    public CSurfacePath(CSurfaceMesh oSurfaceMesh) {
        _oSurfaceMesh = oSurfaceMesh;
    }

    public void Dispose() {
        foreach (CSurfacePoint oPoint in _aPoints)
            oPoint.Dispose();
        _aPoints.Clear();
        _bPlayback = false;
        _nPlaybackPoint = 0;
    }

    public void CreateNewPath() {
        Dispose();
    }

    public void AddPoint(int nTri, Vector3 vecBarycentric) {
        CSurfacePoint oPoint = new CSurfacePoint(nTri, vecBarycentric);
        _aPoints.Add(oPoint);
    }

    public void UpdatePoints() {
        Mesh oMeshBaked = _oSurfaceMesh.Baking_GetBakedSkinnedMesh();
        int[]       aTris       = oMeshBaked.triangles;
        Vector3[]   aVerts      = oMeshBaked.vertices;
        Vector3[]   aNormals    = oMeshBaked.normals;

        foreach (CSurfacePoint oPoint in _aPoints)
            oPoint.UpdatePosition(ref aTris, ref aVerts, ref aNormals);
    }

    public void PlaybackNext() {
        Mesh oMeshBaked = _oSurfaceMesh.Baking_GetBakedSkinnedMesh();
        int[]       aTris       = oMeshBaked.triangles;
        Vector3[]   aVerts      = oMeshBaked.vertices;
        Vector3[]   aNormals    = oMeshBaked.normals;

        CSurfacePoint oPoint = _aPoints[_nPlaybackPoint++];
        oPoint.UpdatePosition(ref aTris, ref aVerts, ref aNormals);

        Transform oArm = CGame._aBodyBases[0]._oActor_ArmL.transform;
        oArm.position = oPoint._vecPosition;
        oArm.rotation = oPoint._quatRotation;

        if (_nPlaybackPoint >= _aPoints.Count)
            _nPlaybackPoint = 0;
    }
}











public class CSurfaceArea {
    public string _sName;

    public CSurfaceArea(string sName) {
        _sName = sName;
    }
}
