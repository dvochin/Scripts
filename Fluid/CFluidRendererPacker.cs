using UnityEngine;

public class CFluidRendererPacker : MonoBehaviour {       // CFluidRendererPacker: Helper to both CFlexFluidRendererPro and the (debug-only) FlexParticlesRenderer.  It 'packs' the showable Flex-simulated particles and appends the 'baked' particles held in the game-global CFluidBaked skinned mesh.
    public int                 _nParticlesFlex;
    public uFlex.FlexParticles _aParticlesFlexSimulated;
    public uFlex.Particle[]    _aParticlesPacked;
    public float[]             _aDensitiesPacked;
    public Color[]             _aColorsPacked;
    public int                  _nFluidParticlesAllocatedForRenderer;
    public int                 _nParticlesPackedThisFrame;          // How many particles have been packed for this frame.

    public void DoStart() {
        _aParticlesFlexSimulated = CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank;
        _nFluidParticlesAllocatedForRenderer = G.C_MaxVertsInMesh + _aParticlesFlexSimulated.m_particlesCount;
        //=== Create the packed particle arrays we'll need to 'pack' the particles we need to draw this frame into a contiguous array ===
        _nParticlesFlex = _aParticlesFlexSimulated.m_particlesCount;
        _aParticlesPacked   = new uFlex.Particle[_nFluidParticlesAllocatedForRenderer];
        _aDensitiesPacked   = new float[_nFluidParticlesAllocatedForRenderer];
        _aColorsPacked      = new Color[_nFluidParticlesAllocatedForRenderer];
    }

    public void DoPackAllFluidParticles() {
        _nParticlesPackedThisFrame = 0;
        int nVertsBaked = 0;

        //=== Draw all of the baked fluid particles ===
        UnityEngine.Profiling.Profiler.BeginSample("X_Fluid_Rend_Baked");
        if (CGame._oFluidBaked) {
            Mesh oMeshBaked = CGame._oFluidBaked.Baking_GetBakedSkinnedMesh();     //###BUGNOW: Baking problems with fake skinned meshes!
            Vector3[] aVerts = oMeshBaked.vertices;
            nVertsBaked = aVerts.Length;
            for (int nVert = 0; nVert < nVertsBaked; nVert++) {
                _aParticlesPacked[_nParticlesPackedThisFrame].pos = aVerts[nVert];
                _aParticlesPacked[_nParticlesPackedThisFrame].invMass = 1;        //###CHECK: Used??
                _aDensitiesPacked[_nParticlesPackedThisFrame] = 1;                //###IDEA: Manipulate this??
                _aColorsPacked   [_nParticlesPackedThisFrame] = CFluidParticleGroup.s_color_BakedToColliders;
                _nParticlesPackedThisFrame++;
                if (_nParticlesPackedThisFrame >= _nFluidParticlesAllocatedForRenderer) {
                    Debug.LogErrorFormat("###ERROR: Too many packed fluid particles in CFluidRendererPacker");
                    return;
                }
            }
        }
        UnityEngine.Profiling.Profiler.EndSample();

        //=== Draw the Flex-simulated real fluid particles that pass our filters ===
        UnityEngine.Profiling.Profiler.BeginSample("X_Fluid_Rend_Simulated");
        float nDensityMax = -1;
        int nNumParticlesDrawn = 0;
        int nNumKinematic = 0;
        int nNumSimulated = 0;
        int nNumZeroDensity = 0;
        float nDensityCutoffMin = CGame._oObj.Get("Fluid_Rend_Density_Min");
        float nDensityCutoffMax = CGame._oObj.Get("Fluid_Rend_Density_Max");

        for (int i = 0; i < _nParticlesFlex; i++) {
            if (_aParticlesFlexSimulated.m_particlesActivity[i]) {
                float nDensity = _aParticlesFlexSimulated.m_densities[i];
                uFlex.Particle oPar = _aParticlesFlexSimulated.m_particles[i];
                bool bIsKinematic = (oPar.invMass == 0);
                if (bIsKinematic) {
                    nNumKinematic++;
                } else {
                    nNumSimulated++;
                    if (nDensityMax < nDensity)
                        nDensityMax = nDensity;
                }
                if (nDensity == 0)
                    nNumZeroDensity++;

                bool bRenderThisParticle = true;
                if (nDensity == 0) {
                    //_aColorsPacked[_nParticlesPackedThisFrame] = Color.yellow;
                    if (CGame.INSTANCE._bFluidRend_DoMoveBadParticles)             //###OPT:! performs debug tests in debug build only
                        oPar.pos.x += CGame.INSTANCE._nFluidDraw_MoveOfBadParticles;
                    if (CGame.INSTANCE._bFluidRend_HideZeroDensity)
                        bRenderThisParticle = false;
                } else if (nDensity < nDensityCutoffMin) {
                    //_aColorsPacked[_nParticlesPackedThisFrame] = G.C_Color_Orange;
                    if (CGame.INSTANCE._bFluidRend_DoMoveBadParticles)
                        oPar.pos.y -= CGame.INSTANCE._nFluidDraw_MoveOfBadParticles;
                }
                else if (nDensity > nDensityCutoffMax) {
                    //_aColorsPacked[_nParticlesPackedThisFrame] = G.C_Color_Purple;
                    if (CGame.INSTANCE._bFluidRend_DoMoveBadParticles)
                        oPar.pos.y += CGame.INSTANCE._nFluidDraw_MoveOfBadParticles;
                }
                else if (nDensity >= nDensityCutoffMin && nDensity <= nDensityCutoffMax) {
                    nNumParticlesDrawn++;
                    _aColorsPacked[_nParticlesPackedThisFrame] = _aParticlesFlexSimulated.m_colours[i];
                }

                if (bRenderThisParticle) { 
                    _aParticlesPacked[_nParticlesPackedThisFrame] = oPar;
                    _aDensitiesPacked[_nParticlesPackedThisFrame] = nDensity;     //###IDEA: Manipulate this??
                    _nParticlesPackedThisFrame++;
                    if (_nParticlesPackedThisFrame >= _nFluidParticlesAllocatedForRenderer) {
                        Debug.LogErrorFormat("###ERROR: Too n  many packed fluid particles in CFluidRendererPacker");
                        return;
                    }
                }
            }
        }
        UnityEngine.Profiling.Profiler.EndSample();
        CGame._aDebugMsgs[(int)EMsg.FluidPack] = string.Format("FlPack:  Drawn={0}  Hiden={1}  #Baked={2}  #Kin={3}  #Sim={4}  #ZD={5}  MaxDens={6:F2}",
            nNumParticlesDrawn, _nParticlesFlex - nNumParticlesDrawn, nVertsBaked, nNumKinematic, nNumSimulated, nNumZeroDensity, nDensityMax);
		
	}
}
