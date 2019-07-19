using UnityEngine;

public class CVisualizeParticle : MonoBehaviour {      // CDebugSoftBodyParticle: Renders debug geometry for a soft body particles
    public int _ParticleID;								//###IMPROVE: Add particle grabbing to this class? (to get away from buggy Flex mouse grabber implementation!)
    public bool _Selected;
    CVisualizeFlex _oVisualizeFlex;


    public void Initialize(CVisualizeFlex oVisualizeFlex, int nParticleID) {
        _oVisualizeFlex = oVisualizeFlex;
        _ParticleID = nParticleID;
        name = string.Format("Par{0}", _ParticleID);
        transform.SetParent(_oVisualizeFlex.transform);
        gameObject.SetActive(true);
		DoUpdate();
    }

    public void Select_Set() {
        _Selected = !_Selected;
		DoUpdate();
    }

	public void DoUpdate() {
        transform.position = _oVisualizeFlex._oFlexParticles.m_particles[_ParticleID].pos + _oVisualizeFlex._iVisualizeFlex.GetVisualiserOffset();
        transform.localScale = _oVisualizeFlex._vecSizeParticles;

		if (_Selected) {
			GetComponent<MeshRenderer>().material.color = CSoftBody._color_ParticlesSelected;
            transform.localScale = 2.0f * _oVisualizeFlex._vecSizeParticles;
		} else if (_oVisualizeFlex._oFlexParticles.m_particles[_ParticleID].invMass == 0) { 
			GetComponent<MeshRenderer>().material.color = CSoftBody._color_ParticlesPinned;
		} else {
            transform.localScale = _oVisualizeFlex._vecSizeParticles;
			//if (_oSoftBody._oFlexParticles.m_colours[_ParticleID] == Color.black)
			//GetComponent<MeshRenderer>().material.color = _oSoftBody._color_ParticlesDefault;
			GetComponent<MeshRenderer>().material.color = _oVisualizeFlex._oFlexParticles.m_colours[_ParticleID];
		}
	}
}
