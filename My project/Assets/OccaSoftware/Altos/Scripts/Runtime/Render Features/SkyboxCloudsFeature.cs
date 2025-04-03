using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OccaSoftware.Altos
{
    public class SkyboxCloudsFeature : ScriptableRendererFeature
    {
        class VolumetricCloudsRenderPass : ScriptableRenderPass
        {
            #region Render Target Handles
            private RenderTargetHandle cloudRenderTarget;
            private RenderTargetHandle cloudTAATarget;
            private RenderTargetHandle cloudUpscaleTarget;
            private RenderTargetHandle cloudMergeTarget;
            #endregion

            #region Input vars
            private const string profilerTag = "Render Volumetric Clouds OS";
            private float renderScale;
            private bool taaEnabled;
            private VolumetricCloudVolume cloudVolume;
            #endregion

            #region Shader Variable References
            private const string mergePassInputTextureShaderReference = "_MERGE_PASS_INPUT_TEX";
            private const string temporalAAHistoricalResultsShaderReference = "_PREVIOUS_TAA_CLOUD_RESULTS";
            #endregion

            #region Texture Names
            private const string cloudRenderTargetName = "_CloudRenderPass_OS";
            private const string upscaleTargetName = "Clouds Low Res Merge Target";
            private const string temporalAATargetName = "_CloudTAAPass_OS";
            private const string sceneMergeTargetName = "_CloudMergePass_OS";
            #endregion

            #region Shader Paths
            private const string upscaleShaderpath = "Shader Graphs/Upscale Pass Shader_OS";
            private const string temporalAntiAliasingShaderpath = "Shader Graphs/Temporal Integration Shader_OS";
            private const string cameraMergeShaderpath = "Shader Graphs/Merge Pass_OS";
            private const string renderpassShaderpath = "Shader Graphs/Volumetric Clouds Renderer_OS";
            #endregion

            #region Materials
            private Material cloudRenderPassMaterial;
            private Material cloudTAAMaterial;
            private Material cloudMergeMaterial;

            private Material upscaleMaterial;
            #endregion

            Matrix4x4 prevViewProj = Matrix4x4.identity;
            Dictionary<Camera, Matrix4x4> prevViewProjPairs;

            Dictionary<Camera, TAACameraData> renderTextures;

            #region Time Management
            float managedTime;
            uint frameCount;
            #endregion

            #region RenderTextureDescriptors
            RenderTextureDescriptor cloudRenderDescriptor;
            #endregion


            class TAACameraData
            {
                private uint lastFrameUsed;
                private RenderTexture renderTexture;
                private string cameraName;

                public TAACameraData(uint lastFrameUsed, RenderTexture renderTexture, string cameraName)
                {
                    LastFrameUsed = lastFrameUsed;
                    RenderTexture = renderTexture;
                    CameraName = cameraName;
                }

                public uint LastFrameUsed
                {
                    get => lastFrameUsed;
                    set => lastFrameUsed = value;
                }

                public RenderTexture RenderTexture
                {
                    get => renderTexture;
                    set => renderTexture = value;
                }

                public string CameraName
                {
                    get => cameraName;
                    set => cameraName = value;
                }
            }

            public VolumetricCloudsRenderPass()
            {
                renderTextures = new Dictionary<Camera, TAACameraData>();
                prevViewProjPairs = new Dictionary<Camera, Matrix4x4>();
                #region Initialize Render Target Handles
                cloudRenderTarget.Init(cloudRenderTargetName);

                cloudUpscaleTarget.Init(upscaleTargetName);

                cloudTAATarget.Init(temporalAATargetName);
                cloudMergeTarget.Init(sceneMergeTargetName);
                #endregion

                #region Create Needed Materials
                if (cloudRenderPassMaterial == null) cloudRenderPassMaterial = CoreUtils.CreateEngineMaterial(renderpassShaderpath);
                if (cloudMergeMaterial == null) cloudMergeMaterial = CoreUtils.CreateEngineMaterial(cameraMergeShaderpath);
                if (cloudTAAMaterial == null) cloudTAAMaterial = CoreUtils.CreateEngineMaterial(temporalAntiAliasingShaderpath);
                if (upscaleMaterial == null) upscaleMaterial = CoreUtils.CreateEngineMaterial(upscaleShaderpath);
                #endregion
            }

            private bool ValidateCloudVolume(VolumetricCloudVolume cloudVolume)
			{
                if (cloudVolume != null && cloudVolume.cloudData != null)
                    return true;

                return false;
			}

            public bool TryGetCloudVolume(out VolumetricCloudVolume cloudVolume)
            {
                if (ValidateCloudVolume(this.cloudVolume))
				{
                    cloudVolume = this.cloudVolume;
                    return true;
				}
				else
				{
                    cloudVolume = FindObjectOfType<VolumetricCloudVolume>();

                    if (ValidateCloudVolume(cloudVolume))
                    {
                        this.cloudVolume = cloudVolume;
                        return true;
                    }

                    return false;
                }
            }

            private void SetRenderVars()
            {
                renderScale = cloudVolume.cloudData.renderScale;
                taaEnabled = cloudVolume.cloudData.taaEnabled;
            }


            void CalculateTime()
            {
                // Get data
                float unityRealtimeSinceStartup = Time.realtimeSinceStartup;
                uint unityFrameCount = (uint)Time.frameCount;

                bool newFrame;
                if (Application.isPlaying)
                {
                    newFrame = frameCount != unityFrameCount;
                    frameCount = unityFrameCount;
                }
                else
                {
                    newFrame = (unityRealtimeSinceStartup - managedTime) > 0.0166f;
                    if (newFrame)
                        frameCount++;
                }

                if (newFrame)
                {
                    managedTime = unityRealtimeSinceStartup;
                }
            }

            //  Returns true if this is the Texture's first frame, false if it already exists
            TAAData SetupTAA(Camera camera, RenderTextureDescriptor descriptor)
            {
                if (renderTextures.TryGetValue(camera, out TAACameraData cameraData))
                {
                    if(IsRenderTextureValid(camera, descriptor, cameraData.RenderTexture))
                        return new TAAData(cameraData.RenderTexture, false);
                }

                return new TAAData(CreateTAARenderTextureAndAddToDictionary(camera, descriptor), true);
            }

            struct TAAData
			{
                public TAAData(RenderTexture renderTexture, bool isFirstFrame)
				{
                    this.renderTexture = renderTexture;
                    this.isFirstFrame = isFirstFrame;
				}
                public RenderTexture renderTexture;
                public bool isFirstFrame;
            }


            bool IsRenderTextureValid(Camera camera, RenderTextureDescriptor descriptor, RenderTexture renderTexture)
			{
                if(renderTexture == null)
				{
                    return false;
				}

                bool rtWrongSize = (renderTexture.width != descriptor.width || renderTexture.height != descriptor.height) ? true : false;
                if (rtWrongSize)
                {
                    return false;
                }

                return true;
            }

            RenderTexture CreateTAARenderTextureAndAddToDictionary(Camera camera, RenderTextureDescriptor descriptor)
            {
                SetupTAARenderTexture(camera, descriptor, out RenderTexture renderTexture);

                if (renderTextures.ContainsKey(camera))
                {
                    if (renderTextures[camera].RenderTexture != null)
                        renderTextures[camera].RenderTexture.Release();

                    renderTextures[camera].RenderTexture = renderTexture;
                }
                else
                {
                    renderTextures.Add(camera, new TAACameraData(frameCount, renderTexture, camera.name));
                }

                return renderTexture;
            }

            void SetupTAARenderTexture(Camera camera, RenderTextureDescriptor descriptor, out RenderTexture renderTexture)
            {
                descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
                renderTexture = new RenderTexture(descriptor);

                RenderTexture activeTexture = RenderTexture.active;
                RenderTexture.active = renderTexture;
                GL.Clear(false, true, new Color(0.0f, 0.0f, 0.0f, 1.0f));
                RenderTexture.active = activeTexture;

                renderTexture.name = camera.name + " TAA History";
                renderTexture.filterMode = FilterMode.Point;
                renderTexture.wrapMode = TextureWrapMode.Clamp;

                renderTexture.Create();
            }

            void CleanupDictionary()
            {
                List<Camera> removeTargets = new List<Camera>();
                foreach (KeyValuePair<Camera, TAACameraData> entry in renderTextures)
                {
                    if (entry.Value.LastFrameUsed < frameCount - 10)
                    {
                        //Debug.Log("Cleaning up unused render texture: " + entry.Value.RenderTexture.name);
                        if (entry.Value.RenderTexture != null)
                            entry.Value.RenderTexture.Release();

                        removeTargets.Add(entry.Key);
                    }
                }

                for (int i = 0; i < removeTargets.Count; i++)
                {
                    renderTextures.Remove(removeTargets[i]);
                }
            }

            Matrix4x4 GetPreviousViewProjection(Camera camera)
            {
                if(prevViewProjPairs.TryGetValue(camera, out Matrix4x4 prevViewProj))
                {
                    return prevViewProj;
                }
                else
                {
                    return Matrix4x4.identity;
                }
            }

            void SetPreviousViewProjection(Camera camera, Matrix4x4 currentViewProjection)
            {
                if (prevViewProjPairs.ContainsKey(camera))
                {
                    prevViewProjPairs[camera] = currentViewProjection;
                }
                else
                {
                    prevViewProjPairs.Add(camera, currentViewProjection);
                }
            }


            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                CloudShaderParamHandler.SetCloudMaterialSettings(cloudVolume, cloudRenderPassMaterial);
                SetRenderVars();

                

                RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
                rtDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;

                #region Configure Cloud Render Pass Target
                cloudRenderDescriptor = cameraTextureDescriptor;
                cloudRenderDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;
                cloudRenderDescriptor.height = (int)(cloudRenderDescriptor.height * renderScale);
                cloudRenderDescriptor.width = (int)(cloudRenderDescriptor.width * renderScale);
                CloudShaderParamHandler.SetRenderScale(renderScale);

                cmd.GetTemporaryRT(cloudRenderTarget.id, cloudRenderDescriptor);
                #endregion

                #region Configure Low Res Cloud Render Pass Target
                cmd.GetTemporaryRT(cloudUpscaleTarget.id, rtDescriptor);
                #endregion

                if (taaEnabled)
                {
                    cmd.GetTemporaryRT(cloudTAATarget.id, cloudRenderDescriptor);
                }

                #region Set up Merge Target
                cmd.GetTemporaryRT(cloudMergeTarget.id, rtDescriptor);
                #endregion
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CalculateTime();
                CleanupDictionary();

                Camera camera = renderingData.cameraData.camera;

                RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;

                Profiler.BeginSample(profilerTag);
                CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

                #region Render Clouds
                // Executes Cloud Target
                CloudShaderParamHandler.SetDepthCulling(cloudVolume, cloudRenderPassMaterial);
                CloudShaderParamHandler.SetDepthCulling(cloudVolume, cloudMergeMaterial);

                Blit(cmd, source, cloudRenderTarget.Identifier(), cloudRenderPassMaterial);

                #endregion

                #region TAA
                if (taaEnabled && cloudTAAMaterial != null)
                {
                    
                    var proj = camera.nonJitteredProjectionMatrix;
                    var view = camera.worldToCameraMatrix;
                    var viewProj = proj * view;

                    cloudTAAMaterial.SetMatrix("_ViewProjM", viewProj);
                    cloudTAAMaterial.SetMatrix("_PrevViewProjM", GetPreviousViewProjection(camera));
                    SetPreviousViewProjection(camera, viewProj);


                    TAAData taaData = SetupTAA(renderingData.cameraData.camera, cloudRenderDescriptor);

                    cloudTAAMaterial.SetTexture(temporalAAHistoricalResultsShaderReference, taaData.renderTexture);

                    if (taaData.isFirstFrame)
                        CloudShaderParamHandler.IgnoreTAAThisFrame(cloudVolume, cloudTAAMaterial, renderScale);
                    else
                        CloudShaderParamHandler.ConfigureTAAParams(cloudVolume, cloudTAAMaterial, renderScale);


                    Blit(cmd, cloudRenderTarget.Identifier(), cloudTAATarget.Identifier(), cloudTAAMaterial);

                    #region Configure TAA History
                    if (renderTextures[camera].RenderTexture == null)
                    {
                        Debug.Log("Cloud Volume Temporal AA Render Texture is missing. Please submit bug report. Missing Texture: " + renderTextures[camera].CameraName + " RT");
                    }
                    else
                    {
                        Blit(cmd, cloudTAATarget.Identifier(), renderTextures[camera].RenderTexture);
                        renderTextures[camera].LastFrameUsed = frameCount;
                    }
                    #endregion
                }
                #endregion

                RenderTargetIdentifier cloudUpscaleSource = taaEnabled ? cloudTAATarget.Identifier() : cloudRenderTarget.Identifier();
                Blit(cmd, cloudUpscaleSource, cloudUpscaleTarget.Identifier(), upscaleMaterial);
                cmd.SetGlobalTexture(mergePassInputTextureShaderReference, cloudUpscaleTarget.Identifier());

                #region Merge with Scene View
                Blit(cmd, source, cloudMergeTarget.Identifier(), cloudMergeMaterial);
                Blit(cmd, cloudMergeTarget.Identifier(), source);
                #endregion

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
                Profiler.EndSample();
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(cloudRenderTarget.id);
                cmd.ReleaseTemporaryRT(cloudUpscaleTarget.id);
                cmd.ReleaseTemporaryRT(cloudTAATarget.id);
                cmd.ReleaseTemporaryRT(cloudMergeTarget.id);
            }
        }


        VolumetricCloudsRenderPass cloudRenderPass;

        #region Excluded Camera Targets
        private const string previewCameraName = "Preview Camera";
        private const string previewSceneCameraName = "Preview Scene Camera";
        #endregion

        private void OnEnable()
        {
            Helpers.RenderFeatureOnEnable(Recreate);
        }
        
        private void OnDisable()
        {
            Helpers.RenderFeatureOnDisable(Recreate);
        }

        private void Recreate(UnityEngine.SceneManagement.Scene current, UnityEngine.SceneManagement.Scene next)
        {
            Create();
        }

        public override void Create()
        {
            cloudRenderPass = new VolumetricCloudsRenderPass();
            cloudRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        private bool ValidateCameraConfiguration(Camera camera, VolumetricCloudVolume cloudVolume)
		{
            if (camera.name == previewCameraName || camera.name == previewSceneCameraName)
                return false;

            if (camera.cameraType == CameraType.SceneView && !cloudVolume.cloudData.renderInSceneView)
                return false;

            if (camera.cameraType == CameraType.Reflection)
                return false;

            return true;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (cloudRenderPass.TryGetCloudVolume(out VolumetricCloudVolume vcv))
			{
				if (ValidateCameraConfiguration(renderingData.cameraData.camera, vcv))
				{
                    renderer.EnqueuePass(cloudRenderPass);
				}
			}
        }



        private class CloudShaderParamHandler
        {
            public static class ShaderParams
            {
                public static int renderScaleShaderReference = Shader.PropertyToID("_CLOUD_RENDER_SCALE");
                public static int depthCullReference = Shader.PropertyToID("_CLOUD_DEPTH_CULL_ON");
                public static int taaBlendFactorReference = Shader.PropertyToID("_TAA_BLEND_FACTOR");
                public static int taaInputRenderScaleReference = Shader.PropertyToID("_TAA_INPUT_TEX_RENDER_SCALE");

                public static class CloudData
                {
                    public static int _CLOUD_STEP_COUNT = Shader.PropertyToID("_CLOUD_STEP_COUNT");
                    public static int _CLOUD_BLUE_NOISE_STRENGTH = Shader.PropertyToID("_CLOUD_BLUE_NOISE_STRENGTH");
                    public static int _CLOUD_BASE_TEX = Shader.PropertyToID("_CLOUD_BASE_TEX");
                    public static int _CLOUD_DETAIL1_TEX = Shader.PropertyToID("_CLOUD_DETAIL1_TEX");
                    public static int _CLOUD_EXTINCTION_COEFFICIENT = Shader.PropertyToID("_CLOUD_EXTINCTION_COEFFICIENT");
                    public static int _CLOUD_COVERAGE = Shader.PropertyToID("_CLOUD_COVERAGE");
                    public static int _CLOUD_SUN_COLOR_MASK = Shader.PropertyToID("_CLOUD_SUN_COLOR_MASK");
                    public static int _CLOUD_LAYER_HEIGHT = Shader.PropertyToID("_CLOUD_LAYER_HEIGHT");
                    public static int _CLOUD_LAYER_THICKNESS = Shader.PropertyToID("_CLOUD_LAYER_THICKNESS");
                    public static int _CLOUD_FADE_DIST = Shader.PropertyToID("_CLOUD_FADE_DIST");
                    public static int _CLOUD_BASE_SCALE = Shader.PropertyToID("_CLOUD_BASE_SCALE");
                    public static int _CLOUD_DETAIL1_SCALE = Shader.PropertyToID("_CLOUD_DETAIL1_SCALE");
                    public static int _CLOUD_DETAIL1_STRENGTH = Shader.PropertyToID("_CLOUD_DETAIL1_STRENGTH");
                    public static int _CLOUD_BASE_TIMESCALE = Shader.PropertyToID("_CLOUD_BASE_TIMESCALE");
                    public static int _CLOUD_DETAIL1_TIMESCALE = Shader.PropertyToID("_CLOUD_DETAIL1_TIMESCALE");
                    public static int _CLOUD_FOG_POWER = Shader.PropertyToID("_CLOUD_FOG_POWER");
                    public static int _CLOUD_MAX_LIGHTING_DIST = Shader.PropertyToID("_CLOUD_MAX_LIGHTING_DIST");
                    public static int _CLOUD_PLANET_RADIUS = Shader.PropertyToID("_CLOUD_PLANET_RADIUS");

                    public static int _CLOUD_CURL_TEX = Shader.PropertyToID("_CLOUD_CURL_TEX");
                    public static int _CLOUD_CURL_SCALE = Shader.PropertyToID("_CLOUD_CURL_SCALE");
                    public static int _CLOUD_CURL_STRENGTH = Shader.PropertyToID("_CLOUD_CURL_STRENGTH");
                    public static int _CLOUD_CURL_TIMESCALE = Shader.PropertyToID("_CLOUD_CURL_TIMESCALE");
                    public static int _CLOUD_CURL_ADJUSTMENT_BASE = Shader.PropertyToID("_CLOUD_CURL_ADJUSTMENT_BASE");

                    public static int _CLOUD_DETAIL2_TEX = Shader.PropertyToID("_CLOUD_DETAIL2_TEX");
                    public static int _CLOUD_DETAIL2_SCALE = Shader.PropertyToID("_CLOUD_DETAIL2_SCALE");
                    public static int _CLOUD_DETAIL2_TIMESCALE = Shader.PropertyToID("_CLOUD_DETAIL2_TIMESCALE");
                    public static int _CLOUD_DETAIL2_STRENGTH = Shader.PropertyToID("_CLOUD_DETAIL2_STRENGTH");

                    public static int _CLOUD_HGFORWARD = Shader.PropertyToID("_CLOUD_HGFORWARD");
                    public static int _CLOUD_HGBACK = Shader.PropertyToID("_CLOUD_HGBACK");
                    public static int _CLOUD_HGBLEND = Shader.PropertyToID("_CLOUD_HGBLEND");
                    public static int _CLOUD_HGSTRENGTH = Shader.PropertyToID("_CLOUD_HGSTRENGTH");

                    public static int _CLOUD_AMBIENT_EXPOSURE = Shader.PropertyToID("_CLOUD_AMBIENT_EXPOSURE");
                    public static int _CLOUD_DISTANT_COVERAGE_START_DEPTH = Shader.PropertyToID("_CLOUD_DISTANT_COVERAGE_START_DEPTH");
                    public static int _CLOUD_DISTANT_CLOUD_COVERAGE = Shader.PropertyToID("_CLOUD_DISTANT_CLOUD_COVERAGE");
                    public static int _CLOUD_DETAIL1_HEIGHT_REMAP = Shader.PropertyToID("_CLOUD_DETAIL1_HEIGHT_REMAP");

                    public static int _CLOUD_DETAIL1_INVERT = Shader.PropertyToID("_CLOUD_DETAIL1_INVERT");
                    public static int _CLOUD_DETAIL2_HEIGHT_REMAP = Shader.PropertyToID("_CLOUD_DETAIL2_HEIGHT_REMAP");
                    public static int _CLOUD_DETAIL2_INVERT = Shader.PropertyToID("_CLOUD_DETAIL2_INVERT");
                    public static int _CLOUD_HEIGHT_DENSITY_INFLUENCE = Shader.PropertyToID("_CLOUD_HEIGHT_DENSITY_INFLUENCE");
                    public static int _CLOUD_COVERAGE_DENSITY_INFLUENCE = Shader.PropertyToID("_CLOUD_COVERAGE_DENSITY_INFLUENCE");

                    public static int _CLOUD_HIGHALT_TEX_1 = Shader.PropertyToID("_CLOUD_HIGHALT_TEX_1");
                    public static int _CLOUD_HIGHALT_TEX_2 = Shader.PropertyToID("_CLOUD_HIGHALT_TEX_2");
                    public static int _CLOUD_HIGHALT_TEX_3 = Shader.PropertyToID("_CLOUD_HIGHALT_TEX_3");

                    public static int _CLOUD_HIGHALT_OFFSET1 = Shader.PropertyToID("_CLOUD_HIGHALT_OFFSET1");
                    public static int _CLOUD_HIGHALT_OFFSET2 = Shader.PropertyToID("_CLOUD_HIGHALT_OFFSET2");
                    public static int _CLOUD_HIGHALT_OFFSET3 = Shader.PropertyToID("_CLOUD_HIGHALT_OFFSET3");
                    public static int _CLOUD_HIGHALT_SCALE1 = Shader.PropertyToID("_CLOUD_HIGHALT_SCALE1");
                    public static int _CLOUD_HIGHALT_SCALE2 = Shader.PropertyToID("_CLOUD_HIGHALT_SCALE2");
                    public static int _CLOUD_HIGHALT_SCALE3 = Shader.PropertyToID("_CLOUD_HIGHALT_SCALE3");
                    public static int _CLOUD_HIGHALT_COVERAGE = Shader.PropertyToID("_CLOUD_HIGHALT_COVERAGE");
                    public static int _CLOUD_HIGHALT_INFLUENCE1 = Shader.PropertyToID("_CLOUD_HIGHALT_INFLUENCE1");
                    public static int _CLOUD_HIGHALT_INFLUENCE2 = Shader.PropertyToID("_CLOUD_HIGHALT_INFLUENCE2");
                    public static int _CLOUD_HIGHALT_INFLUENCE3 = Shader.PropertyToID("_CLOUD_HIGHALT_INFLUENCE3");
                    public static int _CLOUD_BASE_RGBAInfluence = Shader.PropertyToID("_CLOUD_BASE_RGBAInfluence");
                    public static int _CLOUD_DETAIL1_RGBAInfluence = Shader.PropertyToID("_CLOUD_DETAIL1_RGBAInfluence");
                    public static int _CLOUD_DETAIL2_RGBAInfluence = Shader.PropertyToID("_CLOUD_DETAIL2_RGBAInfluence");
                    public static int _CLOUD_HIGHALT_EXTINCTION = Shader.PropertyToID("_CLOUD_HIGHALT_EXTINCTION");

                    public static int _CLOUD_HIGHALT_SHAPE_POWER = Shader.PropertyToID("_CLOUD_HIGHALT_SHAPE_POWER");
                    public static int _CLOUD_SCATTERING_AMPGAIN = Shader.PropertyToID("_CLOUD_SCATTERING_AMPGAIN");
                    public static int _CLOUD_SCATTERING_DENSITYGAIN = Shader.PropertyToID("_CLOUD_SCATTERING_DENSITYGAIN");
                    public static int _CLOUD_SCATTERING_OCTAVES = Shader.PropertyToID("_CLOUD_SCATTERING_OCTAVES");

                    public static int _CLOUD_SUBPIXEL_JITTER_ON = Shader.PropertyToID("_CLOUD_SUBPIXEL_JITTER_ON");
                    public static int _CLOUD_WEATHERMAP_TEX = Shader.PropertyToID("_CLOUD_WEATHERMAP_TEX");
                    public static int _CLOUD_WEATHERMAP_VELOCITY = Shader.PropertyToID("_CLOUD_WEATHERMAP_VELOCITY");
                    public static int _CLOUD_WEATHERMAP_SCALE = Shader.PropertyToID("_CLOUD_WEATHERMAP_SCALE");
                    public static int _CLOUD_WEATHERMAP_VALUE_RANGE = Shader.PropertyToID("_CLOUD_WEATHERMAP_VALUE_RANGE");
                    public static int _USE_CLOUD_WEATHERMAP_TEX = Shader.PropertyToID("_USE_CLOUD_WEATHERMAP_TEX");
                }
            }


            public static void SetRenderScale(float renderScale)
            {
                Shader.SetGlobalFloat(ShaderParams.renderScaleShaderReference, renderScale);
            }

            public static void SetDepthCulling(VolumetricCloudVolume cloudVolume, Material material)
            {
                if (cloudVolume == null)
                    return;

                int renderLocal = 0;
                if (cloudVolume.cloudData.depthCullOptions == DepthCullOptions.RenderLocal)
                    renderLocal = 1;

                material.SetInt(ShaderParams.depthCullReference, renderLocal);
            }

            public static void ConfigureTAAParams(VolumetricCloudVolume cloudVolume, Material material, float renderScale)
            {
                if (cloudVolume == null)
                    return;

                material.SetFloat(ShaderParams.taaInputRenderScaleReference, renderScale);
                material.SetFloat(ShaderParams.taaBlendFactorReference, cloudVolume.cloudData.taaBlendFactor);
            }

            public static void IgnoreTAAThisFrame(VolumetricCloudVolume cloudVolume, Material material, float renderScale)
            {
                if (cloudVolume == null)
                    return;

                material.SetFloat(ShaderParams.taaInputRenderScaleReference, renderScale);
                material.SetFloat(ShaderParams.taaBlendFactorReference, 1f);
            }

            public static void SetCloudMaterialSettings(VolumetricCloudVolume cloudVolume, Material cloudRenderMaterial)
            {
                if (cloudVolume == null)
                    return;

                VolumetricCloudsDefinitionScriptableObject cloudData = cloudVolume.cloudData;

                cloudRenderMaterial.SetFloat(ShaderParams.renderScaleShaderReference, cloudData.renderScale);

                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_AMBIENT_EXPOSURE, cloudData.ambientExposure);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_BASE_RGBAInfluence, cloudData.baseTextureRGBAInfluence);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_BASE_SCALE, cloudData.baseTextureScale);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_BASE_TEX, cloudData.baseTexture);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_BASE_TIMESCALE, cloudData.baseTextureTimescale);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_BLUE_NOISE_STRENGTH, cloudData.blueNoise);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_COVERAGE, cloudData.cloudiness);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_COVERAGE_DENSITY_INFLUENCE, cloudData.cloudinessDensityInfluence);

                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_CURL_SCALE, cloudData.curlTextureScale);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_CURL_STRENGTH, cloudData.curlTextureInfluence);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_CURL_TEX, cloudData.curlTexture);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_CURL_TIMESCALE, cloudData.curlTextureTimescale);

                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_CURL_ADJUSTMENT_BASE, cloudData.baseCurlAdjustment);

                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL1_HEIGHT_REMAP, cloudData.detail1TextureHeightRemap);
                //cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL1_INVERT, cloudData.);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL1_RGBAInfluence, cloudData.detail1TextureRGBAInfluence);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL1_SCALE, cloudData.detail1TextureScale);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_DETAIL1_STRENGTH, cloudData.detail1TextureInfluence);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_DETAIL1_TEX, cloudData.detail1Texture);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL1_TIMESCALE, cloudData.detail1TextureTimescale);

                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL2_HEIGHT_REMAP, cloudData.detail2TextureHeightRemap);
                //cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL2_INVERT, cloudData.);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL2_RGBAInfluence, cloudData.detail2TextureRGBAInfluence);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL2_SCALE, cloudData.detail2TextureScale);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_DETAIL2_STRENGTH, cloudData.detail2TextureInfluence);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_DETAIL2_TEX, cloudData.detail2Texture);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_DETAIL2_TIMESCALE, cloudData.detail2TextureTimescale);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_DISTANT_CLOUD_COVERAGE, cloudData.distantCoverageAmount);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_DISTANT_COVERAGE_START_DEPTH, cloudData.distantCoverageDepth);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_EXTINCTION_COEFFICIENT, cloudData.extinctionCoefficient);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_FADE_DIST, cloudData.cloudFadeDistance);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_FOG_POWER, cloudData.fogPower);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HEIGHT_DENSITY_INFLUENCE, cloudData.heightDensityInfluence);

                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HGFORWARD, cloudData.HGEccentricityForward);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HGBACK, cloudData.HGEccentricityBackward);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HGBLEND, cloudData.HGBlend);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HGSTRENGTH, cloudData.HGStrength);

                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HIGHALT_COVERAGE, cloudData.highAltCloudiness);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HIGHALT_EXTINCTION, cloudData.highAltExtinctionCoefficient);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HIGHALT_INFLUENCE1, cloudData.highAltStrength1);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HIGHALT_INFLUENCE2, cloudData.highAltStrength2);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_HIGHALT_INFLUENCE3, cloudData.highAltStrength3);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_OFFSET1, cloudData.highAltTimescale1);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_OFFSET2, cloudData.highAltTimescale2);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_OFFSET3, cloudData.highAltTimescale3);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_SCALE1, cloudData.highAltScale1);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_SCALE2, cloudData.highAltScale2);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_HIGHALT_SCALE3, cloudData.highAltScale3);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_HIGHALT_TEX_1, cloudData.highAltTex1);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_HIGHALT_TEX_2, cloudData.highAltTex2);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_HIGHALT_TEX_3, cloudData.highAltTex3);


                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_LAYER_HEIGHT, cloudData.cloudLayerHeight);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_LAYER_THICKNESS, cloudData.cloudLayerThickness);
                cloudRenderMaterial.SetInt(ShaderParams.CloudData._CLOUD_MAX_LIGHTING_DIST, cloudData.maxLightingDistance);
                cloudRenderMaterial.SetInt(ShaderParams.CloudData._CLOUD_PLANET_RADIUS, cloudData.planetRadius);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_SCATTERING_AMPGAIN, cloudData.multipleScatteringAmpGain);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_SCATTERING_DENSITYGAIN, cloudData.multipleScatteringDensityGain);
                cloudRenderMaterial.SetInt(ShaderParams.CloudData._CLOUD_SCATTERING_OCTAVES, cloudData.multipleScatteringOctaves);
                cloudRenderMaterial.SetInt(ShaderParams.CloudData._CLOUD_STEP_COUNT, cloudData.stepCount);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_SUN_COLOR_MASK, cloudData.sunColor);

                cloudRenderMaterial.SetInt(ShaderParams.CloudData._CLOUD_SUBPIXEL_JITTER_ON, cloudData.subpixelJitterEnabled == true ? 1 : 0);
                cloudRenderMaterial.SetTexture(ShaderParams.CloudData._CLOUD_WEATHERMAP_TEX, cloudData.weathermapTexture);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_WEATHERMAP_VELOCITY, cloudData.weathermapVelocity);
                cloudRenderMaterial.SetFloat(ShaderParams.CloudData._CLOUD_WEATHERMAP_SCALE, cloudData.weathermapScale);
                cloudRenderMaterial.SetVector(ShaderParams.CloudData._CLOUD_WEATHERMAP_VALUE_RANGE, cloudData.weathermapValueRange);
                cloudRenderMaterial.SetInt(ShaderParams.CloudData._USE_CLOUD_WEATHERMAP_TEX, cloudData.weathermapType == WeathermapType.Texture ? 1 : 0);
            }
        }
    }

}
