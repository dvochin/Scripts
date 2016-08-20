using UnityEngine;
using System;
using System.Collections.Generic;

public class CFlexToSkinnedMesh : MonoBehaviour {
    // CFlexToSkinnedMesh: Guides select Flex particles (of owning flex instance) to the position of related particles pinned to a skinned mesh through Flex springs.
    // This enables softbodies to follow their parent (skinned) body and simulated cloth to follow their skinned cloth equivalent

    uFlex.FlexParticles         _oFlexParticles;
    uFlex.FlexSprings           _oFlexSprings;
    public CBSkinBaked          _oMeshSoftBodyRim;     // Reference to the SMR of our skinned body.  We bake it every framee into _oMeshBakedSkin
    List<ushort>                _aMapVertsSkinToSim;            // Blender-generated map of which Flex-simulated mesh map to which particle of the skinned mesh guiding the Flex mesh.
    public int                  _nNumMappingsSkinToSim;         // Number of skin-to-sim mappings = extra particles and springs that are responsible for 'driving' the 'skinned portion' of the cloth particles to the corresponding position of the 'skinned particles'
    ushort                      _nStartOfExtraParticles;        // Where in our parent's container our 'extra springs and particles' start...
    ushort                      _nStartOfExtraSprings;

    public void Initialize(ref List<ushort> aMapVertsSkinToSim, CBSkinBaked oMeshSoftBodyRim) {
        //=== Obtain reference to the objects we'll need at every game frame ===
        _aMapVertsSkinToSim = aMapVertsSkinToSim;
        _nNumMappingsSkinToSim = _aMapVertsSkinToSim.Count / 2;         // Each mapping takes two slots in _aMapVertsSkinToSim
        _oMeshSoftBodyRim = oMeshSoftBodyRim;
        _oFlexParticles = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;     // Find or create these necessary compoenent from our parent
        _oFlexSprings   = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexSprings))   as uFlex.FlexSprings;

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
        _oMeshSoftBodyRim.Baking_UpdateBakedMesh(); 
        Vector3[] aVertSkinned = _oMeshSoftBodyRim._oMeshBaked.vertices;

        //=== Update the position of our (master) skinned driving particles so (slave) simulated particle can closely follow ===
        for (int nMapping = 0; nMapping < _nNumMappingsSkinToSim; nMapping++) {
            ushort nMappingX2 = (ushort)(nMapping * 2);
            ushort nParticleSkinned         = _aMapVertsSkinToSim[nMappingX2 + 0];              // The index of the skinned particle in this mapping (sparsely populated)
            ushort nParticleSimulatedPinned = _aMapVertsSkinToSim[nMappingX2 + 1];          // Note that this map was modified in init to 'flatten' the sparesly populated pinned simulated particules
            _oFlexParticles.m_particles[nParticleSimulatedPinned].pos = aVertSkinned[nParticleSkinned];
        }
    }
}



//###OBSOLETE: Failed attempt to push rim mesh into driver!
//public class CFlexSkinnedSpringDriver : MonoBehaviour {
//    // CFlexSkinnedSpringDriver: Guides select Flex particles (of owning flex instance) to the position of related particles pinned to a skinned mesh through Flex springs.
//    // This enables softbodies to follow their parent (skinned) body and simulated cloth to follow their skinned cloth equivalent

//    [HideInInspector] public CBSkinBaked                 _oBSkinBaked_Driver;			// The 'skinned portion' driving Flex particles that drive springs that drive the driven visible mesh.  Invisible to the user. Skinned alongside its owning body.  We use all its verts in a master-spring-slave Flex relationship to force these verts to stay very close to their original position on the body
//    [HideInInspector] public SkinnedMeshRenderer         _oSMR_Driver;
//    //List<ushort>                _aVertsDriver;                  // List of driver particles / verts in driver skinned mesh.  Has a 1:1 relationship with equivalent vert indicies in _aVertsDriven
//    //List<ushort>                _aVertsDriven;                  // List of driver particles / verts in the driven mesh of our owning parent.  Has a 1:1 relationship with equivalent vert indicies in _aVertsDriver

//    //uFlex.FlexParticles         _oFlexParticles_Driver;         // The Flex particles attached to (our) skinned object.  These drive springs that in turn drive simulated driven Flex particles
//    //uFlex.FlexParticles         _oFlexParticles_Driven;         // The Flex particles attached to (our parent's) simulated object.
//    uFlex.FlexSprings           _oFlexSprings;
//    public int                  _nNumSprings;                  // Number of driver-to-driven particle mappings = extra particles and springs that are responsible for 'driving' the 'skinned portion' of the cloth particles to the corresponding position of the 'skinned particles'
//    uFlex.FlexParticles         _oFlexParticles;
//    List<ushort>                _aMapVertsSkinToSim;
//    int                         _nNumMappingsSkinToSim;
//    int                         _nStartOfExtraParticles;
//    int                         _nStartOfExtraSprings;

//    public static CFlexSkinnedSpringDriver Create(CBody oBody, ref List<ushort> aMapVertsSkinToSim, ref uFlex.FlexParticles oFlexParticles) {
//        //=== Create the (driving) skinned mesh that will be responsible for driving Flex particles that in turn drive Flex springs which in turn drive the slave (simulated) particles of our owner.  This component is added to the new game object craeted below ===
//        CBSkinBaked oBSkinBaked_Driver = (CBSkinBaked)CBSkinBaked.Create(null, oBody, sBlenderInstancePath_DriverSkinnedMesh, typeof(CBSkinBaked));
//        oBSkinBaked_Driver.transform.SetParent(oFlexParticles.transform);        // Parent under our owning node for clarity.
//        CFlexSkinnedSpringDriver oFlexSkinnedSpringDriver = CUtility.FindOrCreateComponent(oBSkinBaked_Driver.gameObject, typeof(CFlexSkinnedSpringDriver)) as CFlexSkinnedSpringDriver;
//        oFlexSkinnedSpringDriver.Initialize(oBody, ref aMapVertsSkinToSim, ref oFlexParticles);
//        return oFlexSkinnedSpringDriver;
//    }

//    public void Initialize(CBody oBody, ref List<ushort> aMapVertsSkinToSim, ref uFlex.FlexParticles oFlexParticles) {
//        //=== Obtain reference to the objects we'll need at every game frame ===
//        _oFlexParticles = oFlexParticles;
//        _aMapVertsSkinToSim = aMapVertsSkinToSim;
//        _nNumMappingsSkinToSim = _aMapVertsSkinToSim.Count / 2;         // Each mapping takes two slots in _aMapVertsSkinToSim
//        ////_oSkinMeshRend_SkinnedMesh = oSkinMeshRend_SkinnedMesh;

//        _oFlexParticles = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;     // Create these necessary components attached to the skinned part of the mesh
//        _oFlexSprings   = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexSprings))   as uFlex.FlexSprings;

//        //=== Create extra particles and springs for our parent's Flex objects ===
//        _nStartOfExtraParticles = _oFlexParticles.m_particlesCount;             // Remeber the split point between our parent's Flex particles/springs and the new ones we're adding
//        _nStartOfExtraSprings   = _oFlexSprings.m_springsCount;
//        _oFlexParticles.m_particlesCount    += _nNumMappingsSkinToSim;          // Add just the number of particles and springs we need (have a 1:1 relationship)
//        _oFlexSprings.  m_springsCount      += _nNumMappingsSkinToSim;
//        Array.Resize<uFlex.Particle>    (ref _oFlexParticles.m_particles,           _oFlexParticles.m_particlesCount);
//        Array.Resize<bool>              (ref _oFlexParticles.m_particlesActivity,   _oFlexParticles.m_particlesCount);
//        Array.Resize<Vector3>           (ref _oFlexParticles.m_velocities,          _oFlexParticles.m_particlesCount);
//        Array.Resize<float>             (ref _oFlexParticles.m_densities,           _oFlexParticles.m_particlesCount);
//        Array.Resize<Color>             (ref _oFlexParticles.m_colours,             _oFlexParticles.m_particlesCount);
//        Array.Resize<int>               (ref _oFlexSprings.m_springIndices,         _oFlexSprings.m_springsCount * 2);
//        Array.Resize<float>             (ref _oFlexSprings.m_springRestLengths,     _oFlexSprings.m_springsCount);
//        Array.Resize<float>             (ref _oFlexSprings.m_springCoefficients,    _oFlexSprings.m_springsCount);

//        //=== Define the extra particles and springs between the skinned portion and the simulated portion ===
//        int nNextPinnedParticle = _nStartOfExtraParticles;
//        int nNextSpring         = _nStartOfExtraSprings;
//        for (int nMapping = 0; nMapping < _nNumMappingsSkinToSim; nMapping++) {     //###F ###BUG??? Assumes all skinned particles are used?  ###CHECK!
//            int nMappingX2 = nMapping * 2;
//            //ushort nParticleSkinned             = _aMapVertsSkinToSim[nMappingX2 + 0];              // The index of the skinned particle in this mapping (sparsely populated)
//            int nParticleSimulatedMoving    = _aMapVertsSkinToSim[nMappingX2 + 1];              // The index of the simulated particle in this mapping (moves as usual but has a spring to nParticleSimulatedPinned pinned particle)
//            int nParticleSimulatedPinned    = nNextPinnedParticle++;                            // The ordinal of the (new) pinned Flex-simulated particle is the next ordinal.
//            int nSpring                      = nNextSpring++;
//            _aMapVertsSkinToSim[nMappingX2 + 1] = (ushort)nParticleSimulatedPinned;                     // We never need to address the moving particle after this loop... so re-use the map to point to the pinned particle we'll need to move everyframe instead.
//            _oFlexParticles.m_particles[nParticleSimulatedPinned].invMass = 0;        // The extra particle is always the skinned one which we drive so it gets infinite mass to not be simulated
//            _oFlexParticles.m_particles[nParticleSimulatedPinned].pos = _oFlexParticles.m_particles[nParticleSimulatedMoving].pos;
//            _oFlexParticles.m_colours[nParticleSimulatedPinned] = Color.gray;        //###TODO: Standardize Flex colors!
//            _oFlexParticles.m_particlesActivity[nParticleSimulatedPinned] = true;
//            _oFlexSprings.m_springRestLengths [nSpring] = 0.0f;      // Springs between (master) skinned particle and (slave) simulated one is always zero... (we want simulated particle to stick as close to where it should be!)
//            _oFlexSprings.m_springCoefficients[nSpring] = 1.0f;      //###TUNE: Make as stiff as possible for a given Flex iteration count.        ###IMPROVE: Enable game-time setting of this! ###F
//            _oFlexSprings.m_springIndices[nSpring * 2 + 0] = nParticleSimulatedPinned;          // Create the link between the two simulated particles (pinned and moving)
//            _oFlexSprings.m_springIndices[nSpring * 2 + 1] = nParticleSimulatedMoving;
//        }
//    }

//    //###TODO ###DESTRUCTION

//    public void UpdateFlexParticleToSkinnedMesh() {         // Call every frame from the context of owning Flex object (soft body or cloth) to update position of guiding particles.
//        //=== Bake the skinned portion of the mesh.  We need its verts to pin the 'pinned particles' which in turn move the 'moving particles' toward them via a spring we created in init ===
//        _oBSkinBaked_Driver.Baking_UpdateBakedMesh();
//        Vector3[] aVertSkinned = _oBSkinBaked_Driver._memVerts.L;

//        //=== Update the position of our (master) skinned driving particles so (slave) simulated particle can closely follow ===
//        for (int nMapping = 0; nMapping < _nNumMappingsSkinToSim; nMapping++) {
//            ushort nMappingX2 = (ushort)(nMapping * 2);
//            ushort nParticleSkinned         = _aMapVertsSkinToSim[nMappingX2 + 0];              // The index of the skinned particle in this mapping (sparsely populated)
//            ushort nParticleSimulatedPinned = _aMapVertsSkinToSim[nMappingX2 + 1];          // Note that this map was modified in init to 'flatten' the sparesly populated pinned simulated particules
//            _oFlexParticles.m_particles[nParticleSimulatedPinned].pos = aVertSkinned[nParticleSkinned];
//        }
//    }
//}





















//public class CFlexSkinnedSpringDriver_OBSOLETE : MonoBehaviour {
//    //###OBSOLETE: Old attempt to drive vagina verts directly from triangulated springs: Looked bad because verts get all crunched together

//    // CFlexSkinnedSpringDriver: Guides select Flex particles (of owning flex instance) to the position of related particles pinned to a skinned mesh through Flex springs.
//    // This enables softbodies to follow their parent (skinned) body and simulated cloth to follow their skinned cloth equivalent

//    [HideInInspector] public CBSkinBaked                 _oBSkinBaked_Driver;			// The 'skinned portion' driving Flex particles that drive springs that drive the driven visible mesh.  Invisible to the user. Skinned alongside its owning body.  We use all its verts in a master-spring-slave Flex relationship to force these verts to stay very close to their original position on the body
//    [HideInInspector] public SkinnedMeshRenderer         _oSMR_Driver;
//    List<ushort>                _aVertsDriver;                  // List of driver particles / verts in driver skinned mesh.  Has a 1:1 relationship with equivalent vert indicies in _aVertsDriven
//    List<ushort>                _aVertsDriven;                  // List of driver particles / verts in the driven mesh of our owning parent.  Has a 1:1 relationship with equivalent vert indicies in _aVertsDriver

//    uFlex.FlexParticles         _oFlexParticles_Driver;         // The Flex particles attached to (our) skinned object.  These drive springs that in turn drive simulated driven Flex particles
//    uFlex.FlexParticles         _oFlexParticles_Driven;         // The Flex particles attached to (our parent's) simulated object.
//    uFlex.FlexSprings           _oFlexSprings;
//    public int                  _nNumSprings;                  // Number of driver-to-driven particle mappings = extra particles and springs that are responsible for 'driving' the 'skinned portion' of the cloth particles to the corresponding position of the 'skinned particles'


//    public static CFlexSkinnedSpringDriver_OBSOLETE Create(CBody oBody, string sBlenderInstancePath_DriverSkinnedMesh, ref List<ushort> aVertsDriver, ref List<ushort> aVertsDriven, ref uFlex.FlexParticles oFlexParticles_Driven) {
//        //=== Create the (driving) skinned mesh that will be responsible for driving Flex particles that in turn drive Flex springs which in turn drive the slave (simulated) particles of our owner.  This component is added to the new game object craeted below ===
//        CBSkinBaked oBSkinBaked_Driver = (CBSkinBaked)CBSkinBaked.Create(null, oBody, sBlenderInstancePath_DriverSkinnedMesh, typeof(CBSkinBaked));
//        oBSkinBaked_Driver.transform.SetParent(oFlexParticles_Driven.transform);        // Parent under our owning node for clarity.
//        CFlexSkinnedSpringDriver_OBSOLETE oFlexSkinnedSpringDriver = CUtility.FindOrCreateComponent(oBSkinBaked_Driver.gameObject, typeof(CFlexSkinnedSpringDriver_OBSOLETE)) as CFlexSkinnedSpringDriver_OBSOLETE;
//        oFlexSkinnedSpringDriver.Initialize(oBSkinBaked_Driver, oBody, sBlenderInstancePath_DriverSkinnedMesh, ref aVertsDriver, ref aVertsDriven, ref oFlexParticles_Driven);
//        return oFlexSkinnedSpringDriver;
//    }

//    public void Initialize(CBSkinBaked oBSkinBaked_Driver, CBody oBody, string sBlenderInstancePath_DriverSkinnedMesh, ref List<ushort> aVertsDriver, ref List<ushort> aVertsDriven, ref uFlex.FlexParticles oFlexParticles_Driven) {
//        _oBSkinBaked_Driver = oBSkinBaked_Driver;
//        _aVertsDriver = aVertsDriver;
//        _aVertsDriven = aVertsDriven;
//        _oFlexParticles_Driven = oFlexParticles_Driven;
//        _oSMR_Driver = _oBSkinBaked_Driver.GetComponent<SkinnedMeshRenderer>();            // Obtain reference to skinned mesh renderer as it is this object that can 'bake' a skinned mesh.
//        _oSMR_Driver.enabled = false;          // Skinned portion invisible to the user.  Only used to guide simulated portion
//        _nNumSprings = _aVertsDriver.Count;

//        //=== Create the driver particles set by our skinned mesh at every frame ===
//        _oFlexParticles_Driver = CUtility.CreateFlexParticles(gameObject, _nNumSprings*3, uFlex.FlexInteractionType.None, Color.grey);       // Driver particles don't collide with anything and are grey.

//        //=== Create Flex springs to physically link the driver particles to the driven ones by a 1:1:1 relationship ===
//        _oFlexSprings = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexSprings)) as uFlex.FlexSprings;        // Create springs between driver and driven particles in 1:1:1 relationship
//        _oFlexSprings._oFlexParticle_Second = _oFlexParticles_Driven;       // Tell our spring instance that the second indices are pertinent to our parent's (driven) Flex particles!
//        _oFlexSprings.m_springsCount = _nNumSprings * 3;
//        _oFlexSprings.m_springIndices     = new int   [_oFlexSprings.m_springsCount * 2];      // Flattened array of from-to mappings
//        _oFlexSprings.m_springCoefficients = new float[_oFlexSprings.m_springsCount];
//        _oFlexSprings.m_springRestLengths  = new float[_oFlexSprings.m_springsCount];

//        //=== Define the particles and springs between the driver skinned particles and the driven simulated ones ===
//        for (ushort nSpring = 0; nSpring < _nNumSprings; nSpring++) {     //###F ###BUG??? Assumes all skinned particles are used?  ###CHECK!
//            ushort nSpringX3 = (ushort)(nSpring * 3);
//            ushort nParticleDriver = aVertsDriver[nSpring];        // The ordinal of the (new) pinned Flex-simulated particle is the next ordinal.
//            ushort nParticleDriven = aVertsDriven[nSpring];        // The index of the simulated particle in this mapping (moves as usual but has a spring to nParticleSimulatedPinned pinned particle)

//            _oFlexParticles_Driven.m_particles[nSpring].invMass = 1;          // Set the mass of the simulated particle ###TODO: Mass!
//            _oFlexParticles_Driven.m_colours[nSpring] = _oFlexParticles_Driven.m_colour;
//            _oFlexParticles_Driven.m_particlesActivity[nSpring] = true;

//            for (ushort nXYZ = 0; nXYZ < 3; nXYZ++) {
//                ushort nSpringRaw = (ushort)(nSpringX3 + nXYZ);
//                _oFlexParticles_Driver.m_particles[nSpringRaw].invMass = 0;          // The extra particle is always the skinned one which we drive so it gets infinite mass to not be simulated
//                _oFlexParticles_Driver.m_colours[nSpringRaw] = _oFlexParticles_Driver.m_colour;
//                _oFlexParticles_Driver.m_particlesActivity[nSpringRaw] = true;
//                _oFlexSprings.m_springRestLengths [nSpringRaw] = 1.0f;      // Springs between (master) skinned particle and (slave) simulated one is always zero... (we want simulated particle to stick as close to where it should be!)
//                _oFlexSprings.m_springCoefficients[nSpringRaw] = 0.002f;      //###TUNE: Make as stiff as possible for a given Flex iteration count.        ###IMPROVE: Enable game-time setting of this! ###F
//                _oFlexSprings.m_springIndices[nSpringRaw * 2 + 0] = nSpringRaw;          // Create the link between the two simulated particles (pinned and moving)
//                _oFlexSprings.m_springIndices[nSpringRaw * 2 + 1] = nSpring;          //###NOTE: This are ofsetted by '_oFlexParticle_Second' as per our modified FlexSpring code.
//            }
//        }
//    }

//    //###TODO ###DESTRUCTION

//    public void UpdateFlexParticleToSkinnedMesh() {         // Call every frame from the context of owning Flex object (soft body or cloth) to update position of guiding particles.
//        //=== Bake the skinned portion of the mesh.  We need its verts to pin the 'pinned particles' which in turn move the 'moving particles' toward them via a spring we created in init ===
//        _oBSkinBaked_Driver.Baking_UpdateBakedMesh();
//        Vector3[] aVertSkinned = _oBSkinBaked_Driver._oMeshBaked.vertices;

//        //=== Update the position of our driver skinned driving particles from the verts of our baked skinned mesh so (slave) simulated particle can follow by their associated Flex springs ===
//        for (int nSpring = 0; nSpring < _nNumSprings; nSpring++) {
//            ushort nSpringX3 = (ushort)(nSpring * 3);
//            ushort nParticleDriver = _aVertsDriver[nSpring];              // The index of the skinned particle in this mapping (sparsely populated)
//            Vector3 vecVert = aVertSkinned[nParticleDriver];
//            _oFlexParticles_Driver.m_particles[nSpringX3 + 0].pos = vecVert;
//            _oFlexParticles_Driver.m_particles[nSpringX3 + 1].pos = vecVert;
//            _oFlexParticles_Driver.m_particles[nSpringX3 + 2].pos = vecVert;
//            _oFlexParticles_Driver.m_particles[nSpringX3 + 0].pos.x += 1.0f;    //###NOW### Make faster and document!
//            _oFlexParticles_Driver.m_particles[nSpringX3 + 1].pos.y += 1.0f;
//            _oFlexParticles_Driver.m_particles[nSpringX3 + 2].pos.z += 1.0f;
//        }
//    }

//    //###OBSOLETE: Single-spring version = Cannot work because of not enough control in one group of colliding against other groups!
//    //public void Initialize(CBSkinBaked oBSkinBaked_Driver, CBody oBody, string sBlenderInstancePath_DriverSkinnedMesh, ref List<ushort> aVertsDriver, ref List<ushort> aVertsDriven, ref uFlex.FlexParticles oFlexParticles_Driven)
//    //{
//    //    _oBSkinBaked_Driver = oBSkinBaked_Driver;
//    //    _aVertsDriver = aVertsDriver;
//    //    _aVertsDriven = aVertsDriven;
//    //    _oFlexParticles_Driven = oFlexParticles_Driven;
//    //    _oSMR_Driver = _oBSkinBaked_Driver.GetComponent<SkinnedMeshRenderer>();            // Obtain reference to skinned mesh renderer as it is this object that can 'bake' a skinned mesh.
//    //    _oSMR_Driver.enabled = false;          // Skinned portion invisible to the user.  Only used to guide simulated portion
//    //    _nNumSprings = _aVertsDriver.Count;

//    //    //=== Create the driver particles set by our skinned mesh at every frame ===
//    //    _oFlexParticles_Driver = CUtility.CreateFlexParticles(gameObject, _nNumSprings, uFlex.FlexInteractionType.None, Color.grey);       // Driver particles don't collide with anything and are grey.

//    //    //=== Create Flex springs to physically link the driver particles to the driven ones by a 1:1:1 relationship ===
//    //    _oFlexSprings = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexSprings)) as uFlex.FlexSprings;        // Create springs between driver and driven particles in 1:1:1 relationship
//    //    _oFlexSprings._oFlexParticle_Second = _oFlexParticles_Driven;       // Tell our spring instance that the second indices are pertinent to our parent's (driven) Flex particles!
//    //    _oFlexSprings.m_springsCount = _nNumSprings;
//    //    _oFlexSprings.m_springIndices = new int[_nNumSprings * 2];      // Flattened array of from-to mappings
//    //    _oFlexSprings.m_springCoefficients = new float[_nNumSprings];
//    //    _oFlexSprings.m_springRestLengths = new float[_nNumSprings];

//    //    //=== Define the particles and springs between the driver skinned particles and the driven simulated ones ===
//    //    for (ushort nSpring = 0; nSpring < _nNumSprings; nSpring++)
//    //    {     //###F ###BUG??? Assumes all skinned particles are used?  ###CHECK!
//    //        ushort nMappingX2 = (ushort)(nSpring * 2);
//    //        ushort nParticleDriver = aVertsDriver[nSpring];        // The ordinal of the (new) pinned Flex-simulated particle is the next ordinal.
//    //        ushort nParticleDriven = aVertsDriven[nSpring];        // The index of the simulated particle in this mapping (moves as usual but has a spring to nParticleSimulatedPinned pinned particle)
//    //        _oFlexParticles_Driver.m_particles[nSpring].invMass = 0;          // The extra particle is always the skinned one which we drive so it gets infinite mass to not be simulated
//    //        _oFlexParticles_Driven.m_particles[nSpring].invMass = 1;          // Set the mass of the simulated particle ###TODO: Mass!
//    //        _oFlexParticles_Driver.m_colours[nSpring] = _oFlexParticles_Driver.m_colour;
//    //        _oFlexParticles_Driven.m_colours[nSpring] = _oFlexParticles_Driven.m_colour;
//    //        _oFlexParticles_Driver.m_particlesActivity[nSpring] = true;
//    //        _oFlexParticles_Driven.m_particlesActivity[nSpring] = true;
//    //        _oFlexSprings.m_springRestLengths[nSpring] = 0.0f;      // Springs between (master) skinned particle and (slave) simulated one is always zero... (we want simulated particle to stick as close to where it should be!)
//    //        _oFlexSprings.m_springCoefficients[nSpring] = 1.0f;      //###TUNE: Make as stiff as possible for a given Flex iteration count.        ###IMPROVE: Enable game-time setting of this! ###F
//    //        _oFlexSprings.m_springIndices[nMappingX2 + 0] = nParticleDriver;          // Create the link between the two simulated particles (pinned and moving)
//    //        _oFlexSprings.m_springIndices[nMappingX2 + 1] = nParticleDriven;          //###NOTE: This are ofsetted by '_oFlexParticle_Second' as per our modified FlexSpring code.
//    //    }
//    //}

//    ////###TODO ###DESTRUCTION

//    //public void UpdateFlexParticleToSkinnedMesh()
//    //{         // Call every frame from the context of owning Flex object (soft body or cloth) to update position of guiding particles.
//    //    //=== Bake the skinned portion of the mesh.  We need its verts to pin the 'pinned particles' which in turn move the 'moving particles' toward them via a spring we created in init ===
//    //    _oBSkinBaked_Driver.Baking_UpdateBakedMesh();
//    //    Vector3[] aVertSkinned = _oBSkinBaked_Driver._oMeshBaked.vertices;

//    //    //=== Update the position of our driver skinned driving particles from the verts of our baked skinned mesh so (slave) simulated particle can follow by their associated Flex springs ===
//    //    for (int nSpring = 0; nSpring < _nNumSprings; nSpring++)
//    //    {
//    //        ushort nParticleDriver = _aVertsDriver[nSpring];              // The index of the skinned particle in this mapping (sparsely populated)
//    //        _oFlexParticles_Driver.m_particles[nSpring].pos = aVertSkinned[nParticleDriver];
//    //    }
//    //}
