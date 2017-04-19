using UnityEngine;
using System;
using System.Collections.Generic;

public class CPinnedParticles : MonoBehaviour {
    // CPinnedParticles: Guides select Flex particles (of owning flex instance) to the position of related particles pinned to a skinned mesh through Flex springs.
    // This enables softbodies to follow their parent (skinned) body and simulated cloth to follow their skinned cloth equivalent

    uFlex.FlexParticles         _oFlexParticles;                    // Reference to the particles we're adding to (and modifying at runtime)
    uFlex.FlexSprings           _oFlexSprings;                      // Reference to spring component we're modifying to make pinning possible
    public CBSkinBaked          _oMeshSoftBodyPinnedParticles;      // Reference to the SMR of the skinned mesh acting as source for our particles.  We bake it every framee into _oMeshBakedSkin ###DESIGN? Have this class obtain from Blender??
    List<ushort>                _aMapPinnedParticles;               // Blender-generated map of which Flex-simulated mesh map to which particle of the skinned mesh guiding the Flex mesh.
    public int                  _nNumMappingsSkinToSim;             // Number of skin-to-sim mappings = extra particles and springs that are responsible for 'driving' the 'skinned portion' of the cloth particles to the corresponding position of the 'skinned particles'

    public void Initialize(ref List<ushort> aMapPinnedParticles, CBSkinBaked oMeshSoftBodyPinnedParticles) {
        //=== Obtain reference to the objects we'll need at every game frame ===
        _aMapPinnedParticles = aMapPinnedParticles;
        _nNumMappingsSkinToSim = _aMapPinnedParticles.Count / 2;         // Each mapping takes two slots in _aMapPinnedParticles
        _oMeshSoftBodyPinnedParticles = oMeshSoftBodyPinnedParticles;
        _oFlexParticles = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;     // Find or create these necessary compoenent from our parent
        _oFlexSprings   = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexSprings))   as uFlex.FlexSprings;

        //=== Create extra particles and springs for our parent's Flex objects ===
        ushort nStartOfExtraParticles = (ushort)_oFlexParticles.m_particlesCount;             // Remeber the split point between our parent's Flex particles/springs and the new ones we're adding
        ushort nStartOfExtraSprings = (ushort)_oFlexSprings.m_springsCount;
        _oFlexParticles.m_particlesCount    += _nNumMappingsSkinToSim;          // Add just the number of particles and springs we need (have a 1:1 relationship)
        _oFlexSprings.  m_springsCount      += _nNumMappingsSkinToSim;
        Array.Resize<uFlex.Particle>    (ref _oFlexParticles.m_particles,           _oFlexParticles.m_particlesCount);
        Array.Resize<uFlex.Particle>    (ref _oFlexParticles.m_restParticles,       _oFlexParticles.m_particlesCount);
        Array.Resize<bool>              (ref _oFlexParticles.m_particlesActivity,   _oFlexParticles.m_particlesCount);
        Array.Resize<Vector3>           (ref _oFlexParticles.m_velocities,          _oFlexParticles.m_particlesCount);
        Array.Resize<float>             (ref _oFlexParticles.m_densities,           _oFlexParticles.m_particlesCount);
        Array.Resize<Color>             (ref _oFlexParticles.m_colours,             _oFlexParticles.m_particlesCount);
        Array.Resize<int>               (ref _oFlexSprings.m_springIndices,         _oFlexSprings.m_springsCount * 2);
        Array.Resize<float>             (ref _oFlexSprings.m_springRestLengths,     _oFlexSprings.m_springsCount);
        Array.Resize<float>             (ref _oFlexSprings.m_springCoefficients,    _oFlexSprings.m_springsCount);

        //=== Define the extra particles and springs between the skinned portion and the simulated portion ===
        ushort nNextPinnedParticle = nStartOfExtraParticles;
        ushort nNextSpring         = nStartOfExtraSprings;
        for (ushort nMapping = 0; nMapping < _nNumMappingsSkinToSim; nMapping++) {     //###F ###BUG??? Assumes all skinned particles are used?  ###CHECK!
            ushort nMappingX2 = (ushort)(nMapping * 2);
            //ushort nParticleSkinned             = _aMapPinnedParticles[nMappingX2 + 0];              // The index of the skinned particle in this mapping (sparsely populated)
            ushort nParticleSimulatedMoving     = _aMapPinnedParticles[nMappingX2 + 1];              // The index of the simulated particle in this mapping (moves as usual but has a spring to nParticleSimulatedPinned pinned particle)
            ushort nParticleSimulatedPinned     = nNextPinnedParticle++;                            // The ordinal of the (new) pinned Flex-simulated particle is the next ordinal.
            ushort nSpring                      = nNextSpring++;
            _aMapPinnedParticles[nMappingX2 + 1] = nParticleSimulatedPinned;                     // We never need to address the moving particle after this loop... so re-use the map to point to the pinned particle we'll need to move everyframe instead.
            _oFlexParticles.m_restParticles[nParticleSimulatedPinned].invMass = _oFlexParticles.m_particles[nParticleSimulatedPinned].invMass = 0;        // The extra particle is always the skinned one which we drive so it gets infinite mass to not be simulated
            _oFlexParticles.m_particles[nParticleSimulatedPinned].pos = _oFlexParticles.m_particles[nParticleSimulatedMoving].pos;
            _oFlexParticles.m_colours[nParticleSimulatedPinned] = Color.gray;        //###TODO: Standardize Flex colors!
            _oFlexParticles.m_particlesActivity[nParticleSimulatedPinned] = true;
            _oFlexSprings.m_springRestLengths [nSpring] = 0.0f;      // Springs between (master) skinned particle and (slave) simulated one is always zero... (we want simulated particle to stick as close to where it should be!)
            _oFlexSprings.m_springCoefficients[nSpring] = 1.0f;      //###TUNE: Make as stiff as possible for a given Flex iteration count.        ###IMPROVE: Enable game-time setting of this! ###F
            _oFlexSprings.m_springIndices[nSpring * 2 + 0] = nParticleSimulatedPinned;          // Create the link between the two simulated particles (pinned and moving)
            _oFlexSprings.m_springIndices[nSpring * 2 + 1] = nParticleSimulatedMoving;
        }
    }


    public void UpdatePositionsOfPinnedParticles() {         // Called every frame from the context of owning Flex object (soft body or cloth) to update position of guiding particles.
        _oMeshSoftBodyPinnedParticles.Baking_UpdateBakedMesh();     // Bake our skinned mesh (source of position for the pinne particles)
        Vector3[] aVertSkinned = _oMeshSoftBodyPinnedParticles._oMeshBaked.vertices;

        //=== Update the position of our (master) skinned driving particles so (slave) simulated particle can closely follow. This is what makes it possible for a softbody to appear 'pinned' to the surface of its owning skinned body (e.g. breasts to chest, penis to crotch area, etc) ===
        for (int nMapping = 0; nMapping < _nNumMappingsSkinToSim; nMapping++) {
            ushort nMappingX2 = (ushort)(nMapping * 2);
            ushort nParticleSkinned         = _aMapPinnedParticles[nMappingX2 + 0];              // The index of the skinned particle in this mapping (sparsely populated)
            ushort nParticleSimulatedPinned = _aMapPinnedParticles[nMappingX2 + 1];          // Note that this map was modified in init to 'flatten' the sparesly populated pinned simulated particules
            _oFlexParticles.m_particles[nParticleSimulatedPinned].pos = aVertSkinned[nParticleSkinned];
        }
    }
}
