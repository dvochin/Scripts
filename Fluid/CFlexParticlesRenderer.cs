using UnityEngine;

//###MODFLEX [ExecuteInEditMode]
public class CFlexParticlesRenderer : MonoBehaviour
{
    public uFlex.FlexParticles m_flexParticles;

    public Color m_color = Color.white;
 //   public float m_size = 0.002f;				//###MODFLEX: Crashes when we have tons of tiny particles initializing this renderer!
	//public float m_radius = 0.001f;
     
    public float m_minDensity = 0.0f;       //#DEV26: Important?
 //   public bool m_showDensity = false;

    private Material m_material;

    private ComputeBuffer m_posBuffer;
    private ComputeBuffer m_colorBuffer;
    private ComputeBuffer m_quadVerticesBuffer;

    CFluidRendererPacker _oFluidRendererPacker;     // Convenience reference to the fluid particle packer global to the fluid and game

    void Awake() {
        enabled = false;
    }

    public void DoStart() {
        if (m_flexParticles == null)
            m_flexParticles = GetComponent<uFlex.FlexParticles>();

        _oFluidRendererPacker = CGame._oFlexParamsFluid._oFluidRendererPacker;

        //###MODFLEX: Disable Particles Renderer on flex objects that currently have no particles
        if (m_flexParticles == null) { 
			Debug.LogWarningFormat("WARNING: Disabling FlexParticlesRenderer on object '{0}' as it has no FlexParticles component!!", gameObject.name);
			this.enabled = false;
			return;
		}
		if (m_flexParticles.m_maxParticlesCount == 0) {
			Debug.LogWarningFormat("WARNING: Disabling FlexParticlesRenderer on object '{0}' as it has no particles!", gameObject.name);
			this.enabled = false;
			return;
		}

	    m_posBuffer   = new ComputeBuffer(_oFluidRendererPacker._nFluidParticlesAllocatedForRenderer, 16);
        m_colorBuffer = new ComputeBuffer(_oFluidRendererPacker._nFluidParticlesAllocatedForRenderer, 16);

        m_posBuffer.SetData(_oFluidRendererPacker._aParticlesPacked, 0, 0, _oFluidRendererPacker._nParticlesPackedThisFrame);
        m_colorBuffer.SetData(_oFluidRendererPacker._aColorsPacked, 0, 0, _oFluidRendererPacker._nParticlesPackedThisFrame);

        m_quadVerticesBuffer = new ComputeBuffer(6, 16);
        m_quadVerticesBuffer.SetData(new[] {
            new Vector4(-0.5f, 0.5f),
            new Vector4(0.5f, 0.5f),
            new Vector4(0.5f, -0.5f),
            new Vector4(0.5f, -0.5f),
            new Vector4(-0.5f, -0.5f),
            new Vector4(-0.5f, 0.5f),
        });

        m_material = new Material(Shader.Find("uFlex/SpherePointsSpritesShader"));
     
        m_material.hideFlags = HideFlags.DontSave;

        m_material.SetBuffer("buf_Positions", m_posBuffer);
        m_material.SetBuffer("buf_Colors", m_colorBuffer);
        m_material.SetBuffer("buf_Vertices", m_quadVerticesBuffer);

        enabled = true;
    }

    public CFlexParticlesRenderer DoDestroy() {
        DestroyImmediate(m_material);
        ReleaseBuffers();
        GameObject.Destroy(this);
        return null;                // Return a convenience null so caller can destroy and update its reference in one line
    }

    void OnRenderObject() {
        m_posBuffer.SetData(_oFluidRendererPacker._aParticlesPacked, 0, 0, _oFluidRendererPacker._nParticlesPackedThisFrame);
        m_colorBuffer.SetData(_oFluidRendererPacker._aColorsPacked, 0, 0, _oFluidRendererPacker._nParticlesPackedThisFrame);

        m_material.SetFloat("_PointRadius", CGame._oFlexParamsFluid._nFluidParticleRadius);
        m_material.SetFloat("_PointScale", CGame._oFlexParamsFluid._nFluidParticleDiameter);
        m_material.SetFloat("_MinDensity", m_minDensity);
        m_material.SetColor("_Color", m_color);

        m_material.SetPass(0);

        Graphics.DrawProcedural(MeshTopology.Triangles, 6, _oFluidRendererPacker._nParticlesPackedThisFrame);
    }

    void ReleaseBuffers() {
        if (m_posBuffer != null)
            m_posBuffer.Release();

        if (m_colorBuffer != null)
            m_colorBuffer.Release();

        if (m_quadVerticesBuffer != null)
            m_quadVerticesBuffer.Release();
    }

    void OnApplicationQuit() {
        DoDestroy();
    }
}


//   Debug.Log("OnRenderObject");
//if (Application.isPlaying)			//###MODFLEX: Shader broken?  Because of VR??
//{
//    m_posBuffer.SetData(_oFluidRendererPacker._aParticlesPacked);
//}
//else
//{
//    Vector4[] tmpPos = new Vector4[_oFluidRendererPacker._nParticlesPacked];
//    for (int i = 0; i < _oFluidRendererPacker._nParticlesPacked; i++)
//    {
//        //tmpPos[i] = transform.TransformPoint(_oFluidRendererPacker._aParticlesPacked[i].pos);
//        tmpPos[i] = _oFluidRendererPacker._aParticlesPacked[i].pos;
//    }
//    m_posBuffer.SetData(tmpPos);
//}       
//if (m_showDensity)
//{
//    for (int i = 0; i < m_flexParticles.m_particlesCount; i++)
//    {
//            m_flexParticles.m_colours[i] = m_flexParticles.m_colour * m_flexParticles.m_densities[i];
//        //  m_flexParticles.m_colours[i] = new Color(m_flexParticles.m_densities[i],0,0,1);
//    }
//}
