using UnityEngine;

public interface IVisualizeFlex {
    uFlex.FlexParticles         GetFlexParticles();
    uFlex.FlexShapeMatching     GetFlexShapeMatching();
    Vector3                     GetVisualiserOffset();
	float						GetSizeParticles_Mult();
	float						GetSizeShapes_Mult();
}

public class CVisualizeFlex : MonoBehaviour {
    public IVisualizeFlex              _iVisualizeFlex;
	public CVisualizeShape[]           _aVisShapes;
	public CVisualizeParticle[]		    _aVisParticles;
    public uFlex.FlexParticles         _oFlexParticles;
    public uFlex.FlexShapeMatching     _oFlexShapeMatching;
    public Vector3                     _vecSizeParticles;
	public Vector3                     _vecSizeShapes;

	public static CVisualizeFlex Create(GameObject oGO, IVisualizeFlex iVisualizeFlex) {
        CVisualizeFlex oVisualizeFlex = CUtility.FindOrCreateComponent<CVisualizeFlex>(oGO);
		oVisualizeFlex.Initialize(iVisualizeFlex);
		return oVisualizeFlex;
	}

    void Initialize(IVisualizeFlex iVisualizeFlex) {
        _iVisualizeFlex = iVisualizeFlex;
        _oFlexParticles     = _iVisualizeFlex.GetFlexParticles();
        _oFlexShapeMatching = _iVisualizeFlex.GetFlexShapeMatching();

		//=== Create new nodes to render all particles ===
		_aVisParticles = new CVisualizeParticle[_oFlexParticles.m_particlesCount];
		for (int nParticle = 0; nParticle < _oFlexParticles.m_particlesCount; nParticle++) {
			//int nParticleType = _aParticleInfo[nParticle] & C_ParticleInfo_Mask_Type;
			GameObject oTemplateGO = Resources.Load("Prefabs/CVisualizeParticle", typeof(GameObject)) as GameObject;
			GameObject oParticleGO = Instantiate(oTemplateGO) as GameObject;
			CVisualizeParticle oVisParticle = CUtility.FindOrCreateComponent(oParticleGO, typeof(CVisualizeParticle)) as CVisualizeParticle;        //###IMPROVE: Set its color based on our type!
			_aVisParticles[nParticle] = oVisParticle;
			oVisParticle.Initialize(this, nParticle);
		}

		//=== Create new nodes to render all shapes ===
        if (_oFlexShapeMatching) {
		    _aVisShapes = new CVisualizeShape[_oFlexShapeMatching.m_shapesCount];
		    for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
			    GameObject oTemplateGO = Resources.Load("Prefabs/CVisualizeShape", typeof(GameObject)) as GameObject;
			    GameObject oParticleGO = Instantiate(oTemplateGO) as GameObject;
			    CVisualizeShape oVisShape = CUtility.FindOrCreateComponent(oParticleGO, typeof(CVisualizeShape)) as CVisualizeShape;
			    _aVisShapes[nShape] = oVisShape;
			    oVisShape.Initialize(this, nShape);
		    }
        }

		//=== Conveniently hide renderers on our gameObject so we can see our shapes ===
		uFlex.FlexParticlesRenderer oParRend = GetComponent<uFlex.FlexParticlesRenderer>();
		if (oParRend != null)
			oParRend.enabled = false;
		SkinnedMeshRenderer oSMR = GetComponent<SkinnedMeshRenderer>();
		if (oSMR)
			oSMR.enabled = false;
		MeshRenderer oMeshRenderer = GetComponent<MeshRenderer>();
		if (oMeshRenderer != null)
			oMeshRenderer.enabled = false;
		//_oBody._oBodySkinnedMesh._oSkinMeshRendNow.enabled = false;		//###IMPROVE: Set body semi transparent when we're invoked
	}
	
	// Update is called once per frame
	void Update () {
		float nSizeParticles	= CGame.particleRadius * _iVisualizeFlex.GetSizeParticles_Mult();
		float nSizeShapes		= CGame.particleRadius * _iVisualizeFlex.GetSizeShapes_Mult();
        _vecSizeParticles.Set(nSizeParticles, nSizeParticles, nSizeParticles);
		_vecSizeShapes.Set(nSizeShapes, nSizeShapes, nSizeShapes);


		//=== Update position of every particle ===
		foreach (CVisualizeParticle oVisParticle in _aVisParticles)
			oVisParticle.DoUpdate();

		//=== Update position & orientation of every shape ===
        if (_aVisShapes != null) {
		    foreach (CVisualizeShape oVisShape in _aVisShapes)
			    oVisShape.DoUpdate();
        }
	}

	public void Visualization_Hide() {                  //###IMPROVE: Un-hide renderers hidden in Show()?
		if (_aVisParticles != null) {
			foreach (CVisualizeParticle oVisParticle in _aVisParticles)
				Destroy(oVisParticle.gameObject);
            if (_aVisShapes != null) {
			    foreach (CVisualizeShape oVisShape in _aVisShapes)
				    Destroy(oVisShape.gameObject);
            }
			_aVisParticles = null;
			_aVisShapes = null;
		}
	}
}
