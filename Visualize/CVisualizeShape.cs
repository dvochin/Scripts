using UnityEngine;
using System.Collections.Generic;

public class CVisualizeShape : MonoBehaviour {      // CVisualizeShape: Renders debug geometry for a SoftBody shape / bone
    public int _ShapeID;
    public bool _Selected;
    public Color _Color;
    CVisualizeSoftBody _oVisSoftBody;
    Dictionary<int, LineRenderer> _mapLines;

    public void Initialize(CVisualizeSoftBody oVisSoftBody, int nShapeID) {
        _oVisSoftBody = oVisSoftBody;
        _ShapeID = nShapeID;
        transform.localScale = _oVisSoftBody._vecSizeShapes;
        _Color = Color.gray;            // Placeholder color until requested the first time.
        gameObject.SetActive(true);
    }

    public void ToggleSelect() {
        _Selected = !_Selected;
        if (_Selected) {
            Debug.LogFormat("Debug Shape {0} selected", _ShapeID);
            transform.localScale *= 2.0f;
            if (_Color == Color.gray)                       // Get a real color on the first activation (that we will keep)
                _Color = CUtility.GetRandomColor();
            GetComponent<MeshRenderer>().material.color = _Color;

            if (_mapLines == null)
                _mapLines = new Dictionary<int, LineRenderer>();

            //=== Obtain the beginning and end of where the particle IDs are stored for this shape ===
            int nShapeParticleLookupBegin  = (_ShapeID == 0) ? 0 : _oVisSoftBody._oFlexShapeMatching.m_shapeOffsets[_ShapeID - 1];
            int nShapeParticleLookupEnd    = _oVisSoftBody._oFlexShapeMatching.m_shapeOffsets[_ShapeID];

            //=== Iterate through all the particles associated with this shape to draw them ===
            for (int nShapeParticleLookup = nShapeParticleLookupBegin; nShapeParticleLookup < nShapeParticleLookupEnd; nShapeParticleLookup++) {
                int nParticle = _oVisSoftBody._oFlexShapeMatching.m_shapeIndices[nShapeParticleLookup];
                string sLineKey = "S" + _ShapeID.ToString() + "-P" + nParticle.ToString();      //###IDEA: Make globally unique with bodyID & soft body id?
                LineRenderer oLR = CGame.Line_Add(sLineKey);
                oLR.material.color = _Color;
                oLR.SetWidth(_oVisSoftBody._SizeParticles/10, _oVisSoftBody._SizeParticles/10);
                _mapLines.Add(nParticle, oLR);          // Line position update every frame in Update()
            }

        } else {
            Debug.LogFormat("Debug Shape {0} unselected", _ShapeID);
            transform.localScale = _oVisSoftBody._vecSizeShapes;
            GetComponent<MeshRenderer>().material.color = Color.green;      //###IMPROVE: Return to start color.

            if (_mapLines != null) {
                foreach (LineRenderer oLR in _mapLines.Values)
                    GameObject.Destroy(oLR.gameObject);
                _mapLines = null;
            }
        }
    }

    void Update() {
        if (_mapLines != null) {                // Update the position of all lines we have defined.
            foreach (int nParticle in _mapLines.Keys) {         //###IDEA: Pass the two transforms for each line to CGame and have it update (and auto-destroy) the lines at each frame.
                LineRenderer oLR = _mapLines[nParticle];
                oLR.transform.position = transform.position;
                oLR.SetPosition(0, transform.position);
                oLR.SetPosition(1, _oVisSoftBody._oFlexParticles.m_particles[nParticle].pos);
            }
        }
    }

    void OnDestroy() {
        //GetComponent<MeshRenderer>().enabled = false;           // Leave object standing?
        if (_mapLines != null) {
            foreach (LineRenderer oLR in _mapLines.Values)      // Destroy the lines we created
                GameObject.Destroy(oLR.gameObject);
        }
    }

    //public virtual void OnDrawGizmos() {
    //    if (_Selected) { 
    //        Gizmos.color = Color.grey;

    //        //=== Obtain the beginning and end of where the particle IDs are stored for this shape ===
    //        int nShapeParticleLookupBegin  = (_ShapeID == 0) ? 0 : _oVisSoftBody._oFlexShapeMatching.m_shapeOffsets[_ShapeID - 1];
    //        int nShapeParticleLookupEnd    = _oVisSoftBody._oFlexShapeMatching.m_shapeOffsets[_ShapeID];

    //        //=== Iterate through all the particles associated with this shape to draw them ===
    //        for (int nShapeParticleLookup = nShapeParticleLookupBegin; nShapeParticleLookup < nShapeParticleLookupEnd; nShapeParticleLookup++) {
    //            int nParticle = _oVisSoftBody._oFlexShapeMatching.m_shapeIndices[nShapeParticleLookup];
    //            //Vector3 vecPosParticleLocal     = _oFlexShapeMatching.m_shapeRestPositions[nParticle];
    //            //Vector3 vecPosParticleGlobal    = transform.localToWorldMatrix.MultiplyPoint(vecPosParticleLocal);    // How to draw shape rest pos instead of current particle position.
    //            Vector3 vecPosParticleGlobal    = _oVisSoftBody._oFlexParticles.m_particles[nParticle].pos;
    //            //Gizmos.DrawSphere(vecPosParticleGlobal, _SizeParticle);
    //            Gizmos.DrawLine(transform.position, vecPosParticleGlobal);
    //        }
    //    }
    //}

    //uFlex.FlexShapeMatching _oFlexShapeMatching;        // Our parent soft body.  Always has shape matching component
    //uFlex.FlexParticles _oFlexParticles;
    //public int _ShapeID;                               // Our shape ID
    //public Color _Color;                                // The color we use for our debug shapes
    //public float _SizeParticle = 0.00025f;  // CGame.INSTANCE.particleSpacing / 2;
    //public float _SizeShape = 0.001f;
    //public bool _DrawLine = true;

    //void Start () {
    //    _oFlexShapeMatching     = transform.parent.GetComponent<uFlex.FlexShapeMatching>();
    //    _oFlexParticles         = transform.parent.GetComponent<uFlex.FlexParticles>();
    //    _ShapeID = int.Parse(name.Substring(10));      // Node is called something like "FlexShape_12".  Get the ID at the end
    //}

    //public virtual void OnDrawGizmos() {
    //    //Vector3 vecSizeParticles = new Vector3(_SizeGizmo/10, _SizeGizmo/10 _SizeGizmo/10);
    //    Gizmos.color = _Color;
    //    Gizmos.DrawSphere(transform.position, _SizeShape);

    //    //=== Obtain the beginning and end of where the particle IDs are stored for this shape ===
    //    int nShapeParticleLookupBegin  = (_ShapeID == 0) ? 0 : _oFlexShapeMatching.m_shapeOffsets[_ShapeID - 1];
    //    int nShapeParticleLookupEnd    = _oFlexShapeMatching.m_shapeOffsets[_ShapeID];

    //    //=== Iterate through all the particles associated with this shape to draw them ===
    //    for (int nShapeParticleLookup = nShapeParticleLookupBegin; nShapeParticleLookup < nShapeParticleLookupEnd; nShapeParticleLookup++) {
    //        int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeParticleLookup];
    //        //Vector3 vecPosParticleLocal     = _oFlexShapeMatching.m_shapeRestPositions[nParticle];
    //        //Vector3 vecPosParticleGlobal    = transform.localToWorldMatrix.MultiplyPoint(vecPosParticleLocal);    // How to draw shape rest pos instead of current particle position.
    //        Vector3 vecPosParticleGlobal    = _oFlexParticles.m_particles[nParticle].pos;
    //        Gizmos.DrawSphere(vecPosParticleGlobal, _SizeParticle);
    //        if (_DrawLine)
    //            Gizmos.DrawLine(transform.position, vecPosParticleGlobal);
    //    }
    //}
}
