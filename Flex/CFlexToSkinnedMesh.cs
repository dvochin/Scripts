using UnityEngine;
using System;
using System.Collections.Generic;

public class CFlexToSkinnedMesh : MonoBehaviour {
    // CFlexToSkinnedMesh: Guides select Flex particles (of owning flex instance) to the position of related particles pinned to a skinned mesh through Flex springs.
    // This enables softbodies to follow their parent (skinned) body and simulated cloth to follow their skinned cloth equivalent

    uFlex.FlexParticles         _oFlexParticles;
    uFlex.FlexSprings           _oFlexSprings;
    public SkinnedMeshRenderer  _oSkinMeshRend_SkinnedMesh;     // Reference to the SMR of our skinned body.  We bake it every framee into _oMeshBakedSkin
    Mesh                        _oMeshBakedSkin;                // The 'baked' version of the _oSkinMeshRend_SkinnedMesh.  We bake at every frame and to drive each vert's corresponding 'master flex' particle (which in turns its corresponding 'slave flex' particle in the simulated mesh)
    List<ushort>                _aMapVertsSkinToSim;            // Blender-generated map of which Flex-simulated mesh map to which particle of the skinned mesh guiding the Flex mesh.
    public int                  _nNumMappingsSkinToSim;         // Number of skin-to-sim mappings = extra particles and springs that are responsible for 'driving' the 'skinned portion' of the cloth particles to the corresponding position of the 'skinned particles'
    ushort                      _nStartOfExtraParticles;        // Where in our parent's container our 'extra springs and particles' start...
    ushort                      _nStartOfExtraSprings;

    public void Initialize(ref List<ushort> aMapVertsSkinToSim, SkinnedMeshRenderer oSkinMeshRend_SkinnedMesh) {
        //=== Obtain reference to the objects we'll need at every game frame ===
        _aMapVertsSkinToSim = aMapVertsSkinToSim;
        _nNumMappingsSkinToSim = _aMapVertsSkinToSim.Count / 2;         // Each mapping takes two slots in _aMapVertsSkinToSim
        _oSkinMeshRend_SkinnedMesh = oSkinMeshRend_SkinnedMesh;
        _oFlexParticles = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;     // Find or create these necessary compoenent from our parent
        _oFlexSprings   = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexSprings))   as uFlex.FlexSprings;
        _oMeshBakedSkin = new Mesh();                                       // Create the mesh that will be baked at every frame.

        //=== Create extra particles and springs for our parent's Flex objects ===
        _nStartOfExtraParticles = (ushort)_oFlexParticles.m_particlesCount;             // Remeber the split point between our parent's Flex particles/springs and the new ones we're adding
        _nStartOfExtraSprings   = (ushort)_oFlexSprings.m_springsCount;
        _oFlexParticles.m_particlesCount    += _nNumMappingsSkinToSim;          // Add just the number of particles and springs we need (have a 1:1 relationship)
        _oFlexSprings.  m_springsCount      += _nNumMappingsSkinToSim;
        Array.Resize<uFlex.Particle>    (ref _oFlexParticles.m_particles,           _oFlexParticles.m_particlesCount);
        Array.Resize<bool>              (ref _oFlexParticles.m_particlesActivity,   _oFlexParticles.m_particlesCount);
        Array.Resize<Vector3>           (ref _oFlexParticles.m_velocities,          _oFlexParticles.m_particlesCount);
        Array.Resize<float>             (ref _oFlexParticles.m_densities,           _oFlexParticles.m_particlesCount);
        Array.Resize<Color>             (ref _oFlexParticles.m_colours,             _oFlexParticles.m_particlesCount);
        Array.Resize<int>               (ref _oFlexSprings.m_springIndices,         _oFlexSprings.m_springsCount * 2);
        Array.Resize<float>             (ref _oFlexSprings.m_springRestLengths,     _oFlexSprings.m_springsCount);
        Array.Resize<float>             (ref _oFlexSprings.m_springCoefficients,    _oFlexSprings.m_springsCount);

        //=== Define the extra particles and springs between the skinned portion and the simulated portion ===
        ushort nNextPinnedParticle = _nStartOfExtraParticles;
        ushort nNextSpring         = _nStartOfExtraSprings;
        for (ushort nMapping = 0; nMapping < _nNumMappingsSkinToSim; nMapping++) {     //###F ###BUG??? Assumes all skinned particles are used?  ###CHECK!
            ushort nMappingX2 = (ushort)(nMapping * 2);
            //ushort nParticleSkinned             = _aMapVertsSkinToSim[nMappingX2 + 0];              // The index of the skinned particle in this mapping (sparsely populated)
            ushort nParticleSimulatedMoving     = _aMapVertsSkinToSim[nMappingX2 + 1];              // The index of the simulated particle in this mapping (moves as usual but has a spring to nParticleSimulatedPinned pinned particle)
            ushort nParticleSimulatedPinned     = nNextPinnedParticle++;                            // The ordinal of the (new) pinned Flex-simulated particle is the next ordinal.
            ushort nSpring                      = nNextSpring++;
            _aMapVertsSkinToSim[nMappingX2 + 1] = nParticleSimulatedPinned;                     // We never need to address the moving particle after this loop... so re-use the map to point to the pinned particle we'll need to move everyframe instead.
            _oFlexParticles.m_particles[nParticleSimulatedPinned].invMass = 0;        // The extra particle is always the skinned one which we drive so it gets infinite mass to not be simulated
            _oFlexParticles.m_particles[nParticleSimulatedPinned].pos = _oFlexParticles.m_particles[nParticleSimulatedMoving].pos;
            _oFlexParticles.m_colours[nParticleSimulatedPinned] = Color.gray;        //###TODO: Standardize Flex colors!
            _oFlexParticles.m_particlesActivity[nParticleSimulatedPinned] = true;
            _oFlexSprings.m_springRestLengths [nSpring] = 0.0f;      // Springs between (master) skinned particle and (slave) simulated one is always zero... (we want simulated particle to stick as close to where it should be!)
            _oFlexSprings.m_springCoefficients[nSpring] = 1.0f;      //###TUNE: Make as stiff as possible for a given Flex iteration count.        ###IMPROVE: Enable game-time setting of this! ###F
            _oFlexSprings.m_springIndices[nSpring * 2 + 0] = nParticleSimulatedPinned;          // Create the link between the two simulated particles (pinned and moving)
            _oFlexSprings.m_springIndices[nSpring * 2 + 1] = nParticleSimulatedMoving;
        }
    }

    //###TODO ###DESTRUCTION

    public void UpdateFlexParticleToSkinnedMesh() {         // Call every frame from the context of owning Flex object (soft body or cloth) to update position of guiding particles.
        //=== Bake the skinned portion of the mesh.  We need its verts to pin the 'pinned particles' which in turn move the 'moving particles' toward them via a spring we created in init ===
        _oSkinMeshRend_SkinnedMesh.BakeMesh(_oMeshBakedSkin);
        Vector3[] aVertSkinned = _oMeshBakedSkin.vertices;

        //=== Update the position of our (master) skinned driving particles so (slave) simulated particle can closely follow ===
        for (int nMapping = 0; nMapping < _nNumMappingsSkinToSim; nMapping++) {
            ushort nMappingX2 = (ushort)(nMapping * 2);
            ushort nParticleSkinned         = _aMapVertsSkinToSim[nMappingX2 + 0];              // The index of the skinned particle in this mapping (sparsely populated)
            ushort nParticleSimulatedPinned = _aMapVertsSkinToSim[nMappingX2 + 1];          // Note that this map was modified in init to 'flatten' the sparesly populated pinned simulated particules
            _oFlexParticles.m_particles[nParticleSimulatedPinned].pos = aVertSkinned[nParticleSkinned];
        }
    }
}
