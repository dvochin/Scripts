using UnityEngine;

public class CFlexSkinnedBody : uFlex.FlexParticles, IFlexProcessor {       // CFlexSkinned: Renders most of the body as a collection of Flex particles (to repell softbodies, cloth, fluid away from body)

    SkinnedMeshRenderer _oSMR;
    Mesh _oMeshSkinBaked;
    int _nVerts;

    void Awake()			//###LEARN: Start() too late for Flex.  Awake is a must.
    {
        Color color = Color.green;      //###TODO: Colors!

		_oSMR = gameObject.GetComponent<SkinnedMeshRenderer>();
        _oMeshSkinBaked = new Mesh();
        _oSMR.BakeMesh(_oMeshSkinBaked);                  //###OPT!!! Check how expensive this is.  Is there a way for us to move verts & normals straight from skinned mesh from Flex?  (Have not found a way so far)
        _nVerts = _oMeshSkinBaked.vertexCount;

        Vector3[] vertices = _oMeshSkinBaked.vertices;
        int vertexCount = _oMeshSkinBaked.vertexCount;

        m_particlesCount = vertexCount;                         //###IMPROVE<15>: Duplication of CreateFlexObjects(), because we're a subclass of FlexParticles... rethink this?
		m_particles = new uFlex.Particle[vertexCount];
        m_colours = new Color[vertexCount];
        m_velocities = new Vector3[vertexCount];
        m_densities = new float[vertexCount];
        m_particlesActivity = new bool[vertexCount];
        m_colour = color;
        m_interactionType = uFlex.FlexInteractionType.SelfCollideAll;
        m_collisionGroup = -1;

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < vertexCount; i++) {
            m_particles[i].pos = vertices[i];
            m_particles[i].invMass = 0;        // These are pinned particles.  They never move from the simulation (we move them to repel clothing, softbody and fluids)
            m_colours[i] = color;
            m_particlesActivity[i] = true;
            min = uFlex.FlexUtils.Min(min, vertices[i]);
            max = uFlex.FlexUtils.Max(max, vertices[i]);
        }
        m_bounds.SetMinMax(min, max);       //###IMPROVE: Use bounds of skinned mesh instead?  Updates itself??

        uFlex.FlexParticlesRenderer partRend = gameObject.AddComponent<uFlex.FlexParticlesRenderer>();
        partRend.m_size = CGame.INSTANCE.particleSpacing;
        partRend.m_radius = partRend.m_size / 2.0f;
        partRend.enabled = false;

        //Material oMat = new Material(Shader.Find("Diffuse"));
        //Texture oTex = Resources.Load("Textures/Woman/A/Torso", typeof(Texture)) as Texture;
        //oMat.mainTexture = oTex;
        //_oSMR.material = oMat;

        uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
        oFlexProc._oFlexProcessor = this;
    }

    public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
        //=== Bake rim skinned mesh and update position of softbody particle pins ===
        _oSMR.BakeMesh(_oMeshSkinBaked);                  //###OPT!!! Check how expensive this is.  Is there a way for us to move verts & normals straight from skinned mesh from Flex?  (Have not found a way so far)
        Vector3[] aVerts = _oMeshSkinBaked.vertices;

        for (int nVert = 0; nVert < _nVerts; nVert++)       //###OPT!!  Expensive loop per frame that is unfortunately required because different memory footprints... :-(
            m_particles[nVert].pos = aVerts[nVert];
    }
}
