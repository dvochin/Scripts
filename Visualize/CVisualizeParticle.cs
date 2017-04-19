using UnityEngine;

public class CVisualizeParticle : MonoBehaviour {      // CDebugSoftBodyParticle: Renders debug geometry for a soft body particles
    public int _ParticleID;								//###IMPROVE: Add particle grabbing to this class? (to get away from buggy Flex mouse grabber implementation!)
    public bool _Selected;
    CVisualizeSoftBody _oVisSoftBody;
	static Color32 s_oColor_Startup  = new Color32(0, 255, 255, 255);		// Startup = cyan
	static Color32 s_oColor_Selected = new Color32(0, 0, 255, 255);			// Selected = blue
	static Color32 s_oColor_Pinned   = new Color32(128, 128, 128, 255);		// Pinned (infinite mass) = grey


    public void Initialize(CVisualizeSoftBody oVisSoftBody, int nParticleID) {
        _oVisSoftBody = oVisSoftBody;
        _ParticleID = nParticleID;
        name = string.Format("Par{0}", _ParticleID);
        transform.SetParent(_oVisSoftBody.transform);
        transform.localScale = _oVisSoftBody._vecSizeParticles;
        gameObject.SetActive(true);
		SetColorAndSize();
    }

    public void ToggleSelect() {
        _Selected = !_Selected;
        if (_Selected)
            Debug.LogFormat("Debug Particle {0} selected", _ParticleID);			//###IMPROVE: Dump helpful stats about this particle?
        else
            Debug.LogFormat("Debug Particle {0} unselected", _ParticleID);
		SetColorAndSize();
    }

	void SetColorAndSize() {
		if (_Selected) {
			GetComponent<MeshRenderer>().material.color = CVisualizeParticle.s_oColor_Selected;
            transform.localScale = 2.0f * _oVisSoftBody._vecSizeParticles;
		} else if (_oVisSoftBody._oFlexParticles.m_particles[_ParticleID].invMass == 0) { 
			GetComponent<MeshRenderer>().material.color = CVisualizeParticle.s_oColor_Pinned;
            transform.localScale = 2.0f * _oVisSoftBody._vecSizeParticles;
		} else {
			GetComponent<MeshRenderer>().material.color = CVisualizeParticle.s_oColor_Startup;
            transform.localScale = _oVisSoftBody._vecSizeParticles;
		}
	}
}
