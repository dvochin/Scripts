using UnityEngine;
using System.Collections.Generic;

public class CVisualizeShape : MonoBehaviour {      // CVisualizeShape: Renders debug geometry for a SoftBody shape / bone
    public bool			_Selected;
    public int			_ShapeID;
    public Color		_Color;
    CVisualizeFlex      _oVisualizeFlex;
    Dictionary<int, LineRenderer> _mapLines;


    public void Initialize(CVisualizeFlex oVisualizeFlex, int nShapeID) {
        _oVisualizeFlex = oVisualizeFlex;
        _ShapeID = nShapeID;
        _Color = CUtility.GetRandomColor();
        gameObject.SetActive(true);
        name = string.Format("Shape{0}", _ShapeID);
		GetComponent<MeshRenderer>().material.color = CSoftBody._color_ShapeDefault;
        transform.SetParent(_oVisualizeFlex.transform);
    }

	public void Select_Toggle() {
		if (_Selected)
			Select_Clear();
		else
			Select_Set();
	}

	public void Select_Set() { 
        if (_Selected == false) {
            Debug.LogFormat("Debug Shape {0} selected", _ShapeID);
            GetComponent<MeshRenderer>().material.color = _Color;

			_mapLines = new Dictionary<int, LineRenderer>();

            //=== Obtain the beginning and end of where the particle IDs are stored for this shape ===
            int nShapeParticleLookupBegin  = (_ShapeID == 0) ? 0 : _oVisualizeFlex._oFlexShapeMatching.m_shapeOffsets[_ShapeID - 1];
            int nShapeParticleLookupEnd    = _oVisualizeFlex._oFlexShapeMatching.m_shapeOffsets[_ShapeID];

            //=== Iterate through all the particles associated with this shape to draw them ===
            for (int nShapeParticleLookup = nShapeParticleLookupBegin; nShapeParticleLookup < nShapeParticleLookupEnd; nShapeParticleLookup++) {
                int nParticle = _oVisualizeFlex._oFlexShapeMatching.m_shapeIndices[nShapeParticleLookup];
                string sLineKey = "S" + _ShapeID.ToString() + "-P" + nParticle.ToString();      //###IDEA: Make globally unique with bodyID & soft body id?
                LineRenderer oLR = CGame.Line_Add(sLineKey);
				if (nShapeParticleLookup == nShapeParticleLookupBegin)
					oLR.material.color = Color.white;							// Always color the first particle in a shape (usually the 'master particle' that created the shape) as white
				else
					oLR.material.color = _Color;
                oLR.startWidth	= .0050f;							//###TUNE
                oLR.endWidth	= .0001f;                           // Give tiny end size so we can infer direction
				Debug.LogFormat("Shape {0} - Particle {1}", _ShapeID, nParticle);
				if (_mapLines.ContainsKey(nParticle) == false)
					_mapLines.Add(nParticle, oLR);          // Line position update every frame in Update()
				else
					Debug.LogErrorFormat("###ERROR: CVisualizeShape() on shape {0} already had particle {1}", _ShapeID, nParticle);
            }
			_Selected = true;
        }
	}

	public void Select_Clear() { 
        if (_Selected) {
            Debug.LogFormat("Debug Shape {0} unselected", _ShapeID);
			GetComponent<MeshRenderer>().material.color = CSoftBody._color_ShapeDefault;

            if (_mapLines != null) {
                foreach (LineRenderer oLR in _mapLines.Values)
                    GameObject.Destroy(oLR.gameObject);
                _mapLines = null;
            }
			_Selected = false;
        }
    }

    public void DoUpdate() {
        Vector3 vecVisualizerOffset = _oVisualizeFlex._iVisualizeFlex.GetVisualiserOffset();
		transform.position = _oVisualizeFlex._oFlexShapeMatching.m_shapeTranslations[_ShapeID] + vecVisualizerOffset;
		transform.rotation = _oVisualizeFlex._oFlexShapeMatching.m_shapeRotations   [_ShapeID];
        transform.localScale = _oVisualizeFlex._vecSizeShapes;

        if (_mapLines != null) {                // Update the position of all lines we have defined.
            foreach (int nParticle in _mapLines.Keys) {         //###IDEA: Pass the two transforms for each line to CGame and have it update (and auto-destroy) the lines at each frame.
                LineRenderer oLR = _mapLines[nParticle];
                oLR.transform.position = transform.position;
                oLR.SetPosition(0, transform.position);
                oLR.SetPosition(1, _oVisualizeFlex._oFlexParticles.m_particles[nParticle].pos + vecVisualizerOffset);
            }
        }
    }

    void OnDestroy() {
		Select_Clear();
    }

	private void OnTriggerEnter(Collider other) {
		if (other.gameObject.name == "VrWand-Left") {                       //###WEAK: Test of triggers by their node name.  (Them having a custom object would be more robust)
			Select_Set();
		}
	}
	private void OnTriggerExit(Collider other) {				
		if (other.gameObject.name == "VrWand-Left") {       //#Vis: Broken collider
			Select_Clear();
		}
	}
}
