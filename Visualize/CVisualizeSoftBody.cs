/*###IMPROVE: Component added to every softbody?
- Activated by keys or mouse click?

*/

using UnityEngine;

public class CVisualizeSoftBody : MonoBehaviour {      // CDebugSoftBody: Manages debug geometry for a soft body (shapes and particles)
    [HideInInspector] public uFlex.FlexShapeMatching    _oFlexShapeMatching;
    [HideInInspector] public uFlex.FlexParticles        _oFlexParticles;
    CVisualizeParticle[]            _aVisParticles;
    CVisualizeShape[]               _aVisShapes;
    public float                    _SizeParticles = 0.0005f;  // CGame.INSTANCE.particleSpacing / 2;
    public float                    _SizeShapes = 0.001f;
    [HideInInspector] public Vector3 _vecSizeParticles;
    [HideInInspector] public Vector3 _vecSizeShapes;

    void Start () {
        _oFlexShapeMatching     = GetComponent<uFlex.FlexShapeMatching>();
        _oFlexParticles         = GetComponent<uFlex.FlexParticles>();
        _vecSizeParticles   = new Vector3(_SizeParticles, _SizeParticles, _SizeParticles);
        _vecSizeShapes      = new Vector3(_SizeShapes, _SizeShapes, _SizeShapes);

        //=== Create new nodes to render all particles ===
        _aVisParticles = new CVisualizeParticle[_oFlexParticles.m_particlesCount];
        for (int nParticle = 0; nParticle < _oFlexParticles.m_particlesCount; nParticle++) {
            GameObject oTemplateGO = Resources.Load("Prefabs/CVisualizeParticle", typeof(GameObject)) as GameObject;
            GameObject oParticleGO = Instantiate(oTemplateGO) as GameObject;
            CVisualizeParticle oVisParticle = CUtility.FindOrCreateComponent(oParticleGO, typeof(CVisualizeParticle)) as CVisualizeParticle;
            _aVisParticles[nParticle] = oVisParticle;
            oVisParticle.Initialize(this, nParticle);
        }

        //=== Create new nodes to render all shapes ===
        _aVisShapes = new CVisualizeShape[_oFlexShapeMatching.m_shapesCount];
        for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++){
            GameObject oTemplateGO = Resources.Load("Prefabs/CVisualizeShape", typeof(GameObject)) as GameObject;
            GameObject oParticleGO = Instantiate(oTemplateGO) as GameObject;
            CVisualizeShape oVisShape = CUtility.FindOrCreateComponent(oParticleGO, typeof(CVisualizeShape)) as CVisualizeShape;
            _aVisShapes[nShape] = oVisShape;
            oVisShape.Initialize(this, nShape);
        }

		MeshRenderer oMeshRenderer = GetComponent<MeshRenderer>();
		if (oMeshRenderer != null)			// Hide the soft body renderer out of convenience so we see inside ###IMPROVE: Set transparent?
			oMeshRenderer.enabled = false;           
		uFlex.FlexParticlesRenderer oParRend = GetComponent<uFlex.FlexParticlesRenderer>();
		if (oParRend != null)
			oParRend.enabled = false;
    }

    void Update() {
        //=== Update position of each debug particle visualizer to the current particle position ===
        foreach (CVisualizeParticle oVisParticle in _aVisParticles)
            oVisParticle.transform.position = _oFlexParticles.m_particles[oVisParticle._ParticleID].pos;

        foreach (CVisualizeShape oVisShape in _aVisShapes)
            oVisShape.transform.position = _oFlexShapeMatching.m_shapeTranslations[oVisShape._ShapeID];

        //=== Draw connections between this shape and our related particles upon mouse click ===
        if (Input.GetMouseButtonDown(1)) {
            int nLayerTarget = CCursor.C_Layer_HotSpot;                 //###BUG??? Send this layer mask to derived classes???
            uint nLayerTargetMask = (uint)1 << nLayerTarget;
            Ray oRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit oRayHit;
            Physics.Raycast(oRay, out oRayHit, Mathf.Infinity, (int)nLayerTargetMask);
            if (oRayHit.collider != null) {
                CVisualizeParticle oSelParticle = oRayHit.transform.GetComponent<CVisualizeParticle>();
                if (oSelParticle != null)
                    oSelParticle.ToggleSelect();
                CVisualizeShape oSelShape = oRayHit.transform.GetComponent<CVisualizeShape>();
                if (oSelShape != null)
                    oSelShape.ToggleSelect();
            }
        }
    }

    void OnDestroy() {
        Debug.Log("CVisualizeSoftBody.OnDestroy() cleaning up.");
		if (_aVisParticles != null)
			foreach (CVisualizeParticle oVisParticle in _aVisParticles)
				GameObject.Destroy(oVisParticle.gameObject);
		if (_aVisShapes != null)
			foreach (CVisualizeShape oVisShape in _aVisShapes)
				GameObject.Destroy(oVisShape);          // Destroy component only        ###BUG?: Not owned by us... destroy by owner!
    }

    //public virtual void OnDrawGizmos() {
    //    //Vector3 vecSizeParticles = new Vector3(_SizeGizmo/10, _SizeGizmo/10 _SizeGizmo/10);
    //    Gizmos.color = _ColorBase;
    //    Gizmos.DrawSphere(transform.position, _SizeShapes * _SizeParticles);

    //    //=== Obtain the beginning and end of where the particle IDs are stored for this shape ===
    //    int nShapeParticleLookupBegin  = (_nShapeID == 0) ? 0 : _oFlexShapeMatching.m_shapeOffsets[_nShapeID - 1];
    //    int nShapeParticleLookupEnd    = _oFlexShapeMatching.m_shapeOffsets[_nShapeID];

    //    //=== Iterate through all the particles associated with this shape to draw them ===
    //    for (int nShapeParticleLookup = nShapeParticleLookupBegin; nShapeParticleLookup < nShapeParticleLookupEnd; nShapeParticleLookup++) {
    //        int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeParticleLookup];
    //        //Vector3 vecPosParticleLocal     = _oFlexShapeMatching.m_shapeRestPositions[nParticle];
    //        //Vector3 vecPosParticleGlobal    = transform.localToWorldMatrix.MultiplyPoint(vecPosParticleLocal);    // How to draw shape rest pos instead of current particle position.
    //        Vector3 vecPosParticleGlobal    = _oFlexParticles.m_particles[nParticle].pos;
    //        Gizmos.DrawSphere(vecPosParticleGlobal, _SizeParticles);
    //        if (_DrawLines)
    //            Gizmos.DrawLine(transform.position, vecPosParticleGlobal);
    //    }
    //}
}
