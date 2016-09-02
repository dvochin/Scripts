using UnityEngine;

public class CVisualizeParticle : MonoBehaviour {      // CDebugSoftBodyParticle: Renders debug geometry for a soft body particles
    public int _ParticleID;
    public bool _Selected;
    CVisualizeSoftBody _oVisSoftBody;

    public void Initialize(CVisualizeSoftBody oVisSoftBody, int nParticleID) {
        _oVisSoftBody = oVisSoftBody;
        _ParticleID = nParticleID;
        name = string.Format("Par{0}", _ParticleID);
        transform.SetParent(_oVisSoftBody.transform);
        transform.localScale = _oVisSoftBody._vecSizeParticles;
        gameObject.SetActive(true);
    }

    public void ToggleSelect() {
        _Selected = !_Selected;
        if (_Selected) {
            Debug.LogFormat("Debug Particle {0} selected", _ParticleID);
            GetComponent<MeshRenderer>().material.color = new Color32(128, 128, 0, 255);
            transform.localScale *= 2.0f;
        } else {
            Debug.LogFormat("Debug Particle {0} unselected", _ParticleID);
            GetComponent<MeshRenderer>().material.color = Color.yellow;
            transform.localScale = _oVisSoftBody._vecSizeParticles;
        }
    }
}
