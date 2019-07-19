//#define D_ShowTriggerDebugRenderers

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTrigger : MonoBehaviour {
    CBone               _oBoneParent;             // Reference to the D6 joint of our parent.  We manipulate this when a collider enters our trigger
    MeshRenderer        _oMeshRenderer;
    float               _Size = 0.02f;

    public static CTrigger Create(CBone oBoneParent, float nSize, Vector3 vecOffset) {
        CTrigger oTrigger = CUtility.InstantiatePrefab<CTrigger>("Prefabs/CTrigger", "Trig-" + oBoneParent.name, oBoneParent.transform);
        oTrigger.Initialize(oBoneParent, nSize, vecOffset);
        return oTrigger;
    }

    void Initialize(CBone oBoneParent, float nSize, Vector3 vecOffset) {
        _oBoneParent        = oBoneParent;
        _Size               = nSize;
#if D_ShowTriggerDebugRenderers
        _oMeshRenderer      = GetComponent<MeshRenderer>();
        _oMeshRenderer.enabled = true;
#endif
        transform.localScale = new Vector3(_Size, _Size, _Size);
        transform.localPosition = vecOffset;
    }

    void OnTriggerEnter(Collider oCol) {
#if D_ShowTriggerDebugRenderers
        _oMeshRenderer.material.color = G.C_Color_RedTrans;
#endif
        _oBoneParent.OnChildTriggerEnter(this, oCol);
    }
    void OnTriggerExit(Collider oCol) {
#if D_ShowTriggerDebugRenderers
        _oMeshRenderer.material.color = G.C_Color_YellowTrans;
#endif
        _oBoneParent.OnChildTriggerExit(this, oCol);
    }
}
