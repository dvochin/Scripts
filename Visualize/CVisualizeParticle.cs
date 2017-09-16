using UnityEngine;

public class CVisualizeParticle : MonoBehaviour {      // CDebugSoftBodyParticle: Renders debug geometry for a soft body particles
    public int _ParticleID;								//###IMPROVE: Add particle grabbing to this class? (to get away from buggy Flex mouse grabber implementation!)
    public bool _Selected;
    CSoftBody _oSoftBody;


    public void Initialize(CSoftBody oSoftBody, int nParticleID) {
        _oSoftBody = oSoftBody;
        _ParticleID = nParticleID;
        name = string.Format("Par{0}", _ParticleID);
        transform.SetParent(_oSoftBody.transform);
        gameObject.SetActive(true);
		DoUpdate();
    }

    public void Select_Set() {
        _Selected = !_Selected;
		DoUpdate();
    }

	public void DoUpdate() {
        transform.position = _oSoftBody._oFlexParticles.m_particles[_ParticleID].pos + _oSoftBody._vecVisualiserOffset;
        transform.localScale = _oSoftBody._vecSizeParticles;

		if (_Selected) {
			GetComponent<MeshRenderer>().material.color = _oSoftBody._color_ParticlesSelected;
            transform.localScale = 2.0f * _oSoftBody._vecSizeParticles;
		} else if (_oSoftBody._oFlexParticles.m_particles[_ParticleID].invMass == 0) { 
			GetComponent<MeshRenderer>().material.color = _oSoftBody._color_ParticlesPinned;
		} else {
            transform.localScale = _oSoftBody._vecSizeParticles;
			//if (_oSoftBody._oFlexParticles.m_colours[_ParticleID] == Color.black)
			//GetComponent<MeshRenderer>().material.color = _oSoftBody._color_ParticlesDefault;
			GetComponent<MeshRenderer>().material.color = _oSoftBody._oFlexParticles.m_colours[_ParticleID];
		}
	}
}
