using System.Collections.Generic;
using UnityEngine;

public class CFluidBaked : CBSkinBaked {            
    // CFluidBaked: Generates one game-global on-the-fly skinned mesh containing only skinned 'baked' fluid particles that move with where they land on the game bodies.  
    CFlexParamsFluid        _oFlexParamsFluid;
    Mesh                    _oMeshSkinnedBasis;
    List<Transform>         _aBones;
    List<Matrix4x4>         _aBindPoses;
    List<BoneWeight>        _aBoneWeights;
    List<Vector3>           _aVertices;
    List<CFluidParticle>    _aFluidParticles_Kinematic = new List<CFluidParticle>();     // The kinematic particles we move at each frame.  These non-simulated move with the attached skinned mesh and are updated at each frame in the Flex scene to repell simulated Flex particles.
    bool                    _bBonesChanged;

    public static CFluidBaked Create(CFlexParamsFluid oFlexParamsFluid) {
        GameObject oFluidBakedGO = new GameObject("CFluidBaked");
        oFluidBakedGO.transform.SetParent(oFlexParamsFluid.transform);
        CFluidBaked oFluidBaked = oFluidBakedGO.AddComponent<CFluidBaked>();
        oFluidBaked.Initialize(oFlexParamsFluid);
        return oFluidBaked;
    }

    void Initialize(CFlexParamsFluid oFlexParamsFluid) {
        base.Initialize();
        _oFlexParamsFluid = oFlexParamsFluid;
        _oSkinMeshRend = CUtility.FindOrCreateComponent<SkinnedMeshRenderer>(gameObject);
        _oSkinMeshRend.updateWhenOffscreen = true;
        _oSkinMeshRend.quality = SkinQuality.Bone4;      //###DESIGN:??? ###OPT:??
        _oMeshSkinnedBasis = new Mesh();

        _aBones         = new List<Transform>();
        _aBindPoses     = new List<Matrix4x4>();
        _aBoneWeights   = new List<BoneWeight>();
        _aVertices      = new List<Vector3>();
    }

    public int RegisterBoneForFluidBaking(Transform oBoneT) {
        _aBones.Add(oBoneT);
        _aBindPoses.Add(oBoneT.worldToLocalMatrix * transform.localToWorldMatrix);
        _bBonesChanged = true;
        return _aBones.Count - 1;           // Return the never-changing ordinal of this pin / bone back to CFluidParticlePin
    }

    public void BakeFluidParticles(ref List<CFluidParticleGroup> aParticlesGroupsToBake) {
        foreach (CFluidParticleGroup oFluidParticleGroup in aParticlesGroupsToBake) {
            foreach (CFluidParticle oFluidParticle in oFluidParticleGroup._aFluidParticles) {
                BoneWeight oBW = new BoneWeight();
                oBW.boneIndex0 = oFluidParticleGroup._aFluidParticlePins[0]._nFluidBaked_BoneOrdinal;
                oBW.boneIndex1 = oFluidParticleGroup._aFluidParticlePins[1]._nFluidBaked_BoneOrdinal;
                oBW.boneIndex2 = oFluidParticleGroup._aFluidParticlePins[2]._nFluidBaked_BoneOrdinal;
                oBW.weight0 = 0.5f;                //###WEAK: Gross approximation... improve by using barycentric info available from raycaster!
                oBW.weight1 = oBW.weight2 = (1f - oBW.weight0) / 2f;        //oBW.weight1 = (1f - oBW.weight0);
                _aBoneWeights.Add(oBW);
                oFluidParticle.BakedFluid_ConvertToKinematic(_aVertices.Count);
                _aVertices.Add(oFluidParticle._vecParticle);
                _aFluidParticles_Kinematic.Add(oFluidParticle);
                CGame._oFlexParamsFluid.STAT_FluidParticlesBaked++;
            }
            oFluidParticleGroup._aFluidParticles.Clear();               //###HACK: Manually clear the group's collection of particle so they are not deleted (we have them now).  Kind of crappy way to transfer control... redo?
        }

        _oMeshSkinnedBasis.vertices     = _aVertices.ToArray();
        _oMeshSkinnedBasis.boneWeights  = _aBoneWeights.ToArray();
        if (_bBonesChanged)
            _oMeshSkinnedBasis.bindposes    = _aBindPoses.ToArray();
        _oMeshSkinnedBasis.RecalculateNormals();
        _oMeshSkinnedBasis.RecalculateBounds();     //###OPT: Needed?

        _oSkinMeshRend.sharedMesh = _oMeshSkinnedBasis;
        if (_bBonesChanged) {
            _oSkinMeshRend.bones = _aBones.ToArray();
            _bBonesChanged = false;
        }
    }

    public void BakedFluid_UpdateKinematicFluidParticles() {
        Mesh oMeshBaked = Baking_GetBakedSkinnedMesh();
        Vector3[] aVertsBakedMesh = oMeshBaked.vertices;
        foreach (CFluidParticle oPar in _aFluidParticles_Kinematic)
            oPar.BakedFluid_UpdateKinematicPosition(ref aVertsBakedMesh);
    }
}


////=== Iterate through all the Fluid Particle Pins and ensure in this frame that we have defined a bone for each of them ===
//CFlexTriCol oFlexTriCol = CGame.GetBody(0)._oFlexTriCol_BodySurface;
//if (oFlexTriCol._mapFluidParticlePins != null) { 
//    foreach (KeyValuePair<ushort, CFluidParticlePin> oPair in oFlexTriCol._mapFluidParticlePins) {
//        CFluidParticlePin oFluidParticlePin = oPair.Value;
//        if (oFluidParticlePin._BoneOrdinal_InFluidBaked_HACK == -1) {               // Only add to our flat list of bones those that have not been added (by this code) in previous iteration
//            oFluidParticlePin._BoneOrdinal_InFluidBaked_HACK = _aBones.Count;
//            _aBones.Add(oFluidParticlePin.transform);
//            _aBindPoses.Add(oFluidParticlePin.transform.worldToLocalMatrix * transform.localToWorldMatrix);
//        }
//    }
//}
