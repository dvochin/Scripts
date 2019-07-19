using UnityEngine;
using UnityEngine.Profiling;

// Renders screen space fluids to a number of render textures. Needs a camera post-processing effect to compose and draw final effect
public class CFlexFluidRendererPro : MonoBehaviour {
    public uFlex.FlexParticles m_flexParticles;

    //public float m_pointScale = 1.0f;
    //public float m_pointRadius = 1.0f;
    public float m_minDensity = 0.0f;

    public RenderTexture m_colorTexture;
    public RenderTexture m_depthTexture;
    public RenderTexture m_blurredDepthTexture;
    public RenderTexture m_blurredDepthTempTexture;
    public RenderTexture m_thicknessTexture;

    public bool m_blur = true;
    public float m_blurScale = 1f;
    public int m_blurRadius = 10;
    public float m_minDepth = 0.0f;
    public float m_blurDepthFalloff = 100.0f;

    public float m_thickness = 1.0f;
    public float m_softness = 1.0f;

    private Material m_colorMaterial;
    private Material m_depthMaterial;
    private Material m_blurDepthMaterial;
    private Material m_thicknessMaterial;

    private ComputeBuffer m_posBuffer;
    private ComputeBuffer m_densityBuffer;
    private ComputeBuffer m_quadVerticesBuffer;
    //private ComputeBuffer m_colorBuffer;

    CFluidRendererPacker _oFluidRendererPacker;     // Convenience reference to the fluid particle packer global to the fluid and game
    SSF.SSFPro_ComposeFluid _oFluidCameraComposer;

    void Awake() {
        enabled = false;
    }

    public void DoStart() {
        if (m_flexParticles == null)
            m_flexParticles = GetComponent<uFlex.FlexParticles>();

        _oFluidRendererPacker = CGame._oFlexParamsFluid._oFluidRendererPacker;

        m_colorTexture            = Resources.Load<RenderTexture>("SSF_ColorTexture");          //###INFO: Needs these to be moved into a 'Resource' folder for full on-the-spot construction
        m_depthTexture            = Resources.Load<RenderTexture>("SSF_DepthTexture");
        m_blurredDepthTexture     = Resources.Load<RenderTexture>("SSF_BlurredDepthTexture");
        m_blurredDepthTempTexture = Resources.Load<RenderTexture>("SSF_BlurredDepthTempTexture");
        m_thicknessTexture        = Resources.Load<RenderTexture>("SSF_ThicknessTexture");

        m_colorMaterial         = new Material(Shader.Find("ScreenSpaceFluids/SSF_SpherePointsShader"));
        m_depthMaterial         = new Material(Shader.Find("ScreenSpaceFluids/SSFPro_DepthShaderHQ"));
        m_thicknessMaterial     = new Material(Shader.Find("ScreenSpaceFluids/SSFPro_ThicknessShaderHQ"));
        m_blurDepthMaterial     = new Material(Shader.Find("ScreenSpaceFluids/SSF_BlurDepth"));

        m_posBuffer             = new ComputeBuffer(CGame._oFlexParamsFluid._oFluidRendererPacker._nFluidParticlesAllocatedForRenderer, 16);
        m_densityBuffer         = new ComputeBuffer(_oFluidRendererPacker._nFluidParticlesAllocatedForRenderer, 4);
        m_posBuffer.SetData(_oFluidRendererPacker._aParticlesPacked, 0, 0, _oFluidRendererPacker._nParticlesPackedThisFrame);


        m_quadVerticesBuffer = new ComputeBuffer(6, 16);
        m_quadVerticesBuffer.SetData(new[] {
            new Vector4(-0.5f, 0.5f),
            new Vector4(0.5f, 0.5f),
            new Vector4(0.5f, -0.5f),
            new Vector4(0.5f, -0.5f),
            new Vector4(-0.5f, -0.5f),
            new Vector4(-0.5f, 0.5f),
        });

        m_depthMaterial.SetBuffer("buf_Positions", m_posBuffer);
        m_depthMaterial.SetBuffer("buf_Vertices", m_quadVerticesBuffer);

        m_thicknessMaterial.SetBuffer("buf_Positions", m_posBuffer);
        m_thicknessMaterial.SetBuffer("buf_Vertices", m_quadVerticesBuffer);

        //m_colorBuffer           = new ComputeBuffer(m_flexParticles.m_particlesCount, 16);
        //m_colorBuffer.  SetData(_aColors, 0, 0, _oFluidRendererPacker._nParticlesPackedThisFrame);
        //m_thicknessMaterial.SetBuffer("buf_Velocities", m_colorBuffer);
        //m_depthMaterial.SetBuffer("buf_Velocities", m_colorBuffer);

        //=== Add the SSFPro renderer to our camera (done programmatically because of the many VR cameras) ===
        _oFluidCameraComposer = CUtility.FindOrCreateComponent<SSF.SSFPro_ComposeFluid>(Camera.main.gameObject);
        _oFluidCameraComposer.shader = Resources.Load<Shader>("Shaders/SSFPro_ComposeFluidHQ");

        _oFluidCameraComposer.m_colorTexture            = m_colorTexture;
        _oFluidCameraComposer.m_depthTexture            = m_depthTexture;
        _oFluidCameraComposer.m_thicknessTexture        = m_thicknessTexture;
        _oFluidCameraComposer.m_blurredDepthTexture     = m_blurredDepthTexture;
        _oFluidCameraComposer.m_color                   = new Color32(194, 186, 185, 105);       // Cum color ###TUNE
        _oFluidCameraComposer.m_specular                = Color.white;        //###TUNE
        _oFluidCameraComposer.m_shininess               = 100;      //###MOVE:?  To fluid main?
        _oFluidCameraComposer.m_reflection              = 0;
        _oFluidCameraComposer.m_reflectionFalloff       = 10;
        _oFluidCameraComposer.m_reflection              = 0;
        _oFluidCameraComposer.m_indexOfRefraction       = 0.01f;
        _oFluidCameraComposer.m_fresnel                 = 1;
        _oFluidCameraComposer.m_maxDepth                = 0.9999f;
        _oFluidCameraComposer.m_minThickness            = 0;
        _oFluidCameraComposer.m_xFactor                 = 0.001f;
        _oFluidCameraComposer.m_YFactor                 = 0.001f;
        _oFluidCameraComposer.m_shininess               = 100;
        _oFluidCameraComposer.DoStart();

        enabled = true;
    }

    public CFlexFluidRendererPro DoDestroy() {
        ReleaseBuffers();
        SSF.SSFPro_ComposeFluid oFluidRend = Camera.main.gameObject.GetComponent<SSF.SSFPro_ComposeFluid>();
        if (_oFluidCameraComposer)
            GameObject.Destroy(_oFluidCameraComposer);
        GameObject.Destroy(this);
        return null;                // Return a convenience null so caller can destroy and update its reference in one line
    }


    void OnRenderObject() {
        //=== Draw the textures for this frame ===
        Profiler.BeginSample("X_Fluid_Rend_Draw");

        m_posBuffer.    SetData(_oFluidRendererPacker._aParticlesPacked, 0, 0, _oFluidRendererPacker._nParticlesPackedThisFrame);
        m_densityBuffer.SetData(_oFluidRendererPacker._aDensitiesPacked, 0, 0, _oFluidRendererPacker._nParticlesPackedThisFrame);
        //m_colorBuffer.  SetData(_aColors, 0, 0, _oFluidRendererPacker._nParticlesPackedThisFrame);           //###MOD: 
        // m_material.SetPass(0);

        DrawColors();

        DrawDepth();

        if (m_blur) {
            BlurDepth();
        } else {
            Graphics.Blit(m_depthTexture, m_blurredDepthTexture);           //###OPT: Can remove blit if we never use expensive blur
        }

        DrawThickness();

        //Graphics.DrawProcedural(MeshTopology.Triangles, sphereVertexCount, body.pointsCount);
        Camera cam = Camera.main;
        Graphics.SetRenderTarget(cam.targetTexture);
        Profiler.EndSample();
    }

    void DrawColors() {
        //m_colorMaterial.SetColor("_Color", m_color);
        float nRadius = CGame._oFlexParamsFluid._nFluidParticleRadius * CGame._oFlexParamsFluid._nShaderParticleSizeMult;
        m_colorMaterial.SetFloat("_PointRadius", nRadius);
        m_colorMaterial.SetFloat("_PointScale", nRadius * 2);
        m_colorMaterial.SetBuffer("buf_Positions", m_posBuffer);
        //m_colorMaterial.SetBuffer("buf_Colors", m_colorBuffer);
        m_colorMaterial.SetBuffer("buf_Vertices", m_quadVerticesBuffer);

        Graphics.SetRenderTarget(m_colorTexture);
        GL.Clear(true, true, Color.white);

        m_colorMaterial.SetPass(0);

        Graphics.DrawProcedural(MeshTopology.Triangles, 6, _oFluidRendererPacker._nParticlesPackedThisFrame);
    }

    void DrawDepth() {
        float nRadius = CGame._oFlexParamsFluid._nFluidParticleRadius * CGame._oFlexParamsFluid._nShaderParticleSizeMult;
        m_depthMaterial.SetFloat("_PointRadius", nRadius);
        m_depthMaterial.SetFloat("_PointScale", nRadius * 2);
        m_depthMaterial.SetFloat("_MinDensity", m_minDensity);

        m_depthMaterial.SetBuffer("buf_Positions", m_posBuffer);
        m_depthMaterial.SetBuffer("buf_Densities", m_densityBuffer);
        m_depthMaterial.SetBuffer("buf_Vertices", m_quadVerticesBuffer);

        Graphics.SetRenderTarget(m_depthTexture);
        GL.Clear(true, true, Color.white);

        m_depthMaterial.SetPass(0);

        Graphics.DrawProcedural(MeshTopology.Triangles, 6, _oFluidRendererPacker._nParticlesPackedThisFrame);
    }

    void BlurDepth() {
        m_blurDepthMaterial.SetTexture("_DepthTex", m_depthTexture);

        m_blurDepthMaterial.SetInt("radius", m_blurRadius);
        //    m_blurDepthMaterial.SetFloat("minDepth", m_minDepth);
        m_blurDepthMaterial.SetFloat("blurDepthFalloff", m_blurDepthFalloff);

        m_blurDepthMaterial.SetTexture("_DepthTex", m_depthTexture);
        //   m_blurDepthMaterial.SetFloat("scaleX", 1.0f / Screen.width);
        m_blurDepthMaterial.SetFloat("scaleX", 1.0f / 1024 * m_blurScale);
        m_blurDepthMaterial.SetFloat("scaleY", 0.0f);
        Graphics.Blit(m_depthTexture, m_blurredDepthTempTexture, m_blurDepthMaterial);

        m_blurDepthMaterial.SetTexture("_DepthTex", m_blurredDepthTempTexture);
        m_blurDepthMaterial.SetFloat("scaleX", 0.0f);
        //   m_blurDepthMaterial.SetFloat("scaleY", 1.0f / Screen.height);
        m_blurDepthMaterial.SetFloat("scaleY", 1.0f / 1024 * m_blurScale);
        Graphics.Blit(m_blurredDepthTempTexture, m_blurredDepthTexture, m_blurDepthMaterial);
    }

    void DrawThickness() {
        float nRadius = CGame._oFlexParamsFluid._nFluidParticleRadius * CGame._oFlexParamsFluid._nShaderParticleSizeMult;
        m_thicknessMaterial.SetFloat("_PointRadius", nRadius);
        m_thicknessMaterial.SetFloat("_PointScale", nRadius * 2);
        m_thicknessMaterial.SetFloat("_MinDensity", m_minDensity);
        m_thicknessMaterial.SetFloat("_Thickness", m_thickness);
        m_thicknessMaterial.SetFloat("_Softness", m_softness);

        m_thicknessMaterial.SetBuffer("buf_Positions", m_posBuffer);
        //m_thicknessMaterial.SetBuffer("buf_Velocities", m_colorBuffer);
        m_thicknessMaterial.SetBuffer("buf_Vertices", m_quadVerticesBuffer);

        Graphics.SetRenderTarget(m_thicknessTexture);
        GL.Clear(true, true, Color.black);

        m_thicknessMaterial.SetPass(0);

        Graphics.DrawProcedural(MeshTopology.Triangles, 6, _oFluidRendererPacker._nParticlesPackedThisFrame);
    }

    void ReleaseBuffers() {
        if (m_posBuffer != null)
            m_posBuffer.Release();

        if (m_densityBuffer != null)
            m_densityBuffer.Release();

        if (m_quadVerticesBuffer != null)
            m_quadVerticesBuffer.Release();

        //if (m_colorBuffer != null)
        //    m_colorBuffer.Release();
    }

    void OnApplicationQuit() {
        DoDestroy();
    }
}
