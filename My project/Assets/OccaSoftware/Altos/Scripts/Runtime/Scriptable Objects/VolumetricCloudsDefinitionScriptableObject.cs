using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccaSoftware.Altos
{
    [CreateAssetMenu(fileName = "Volumetric Cloud Definition", menuName = "Skies/Volumetric Cloud Definition")]
    public class VolumetricCloudsDefinitionScriptableObject : ScriptableObject
    {
        private void OnValidate()
        {
            planetRadius = GetRadiusFromCelestialBodySelection(celestialBodySelection, planetRadius);
            extinctionCoefficient = Mathf.Max(0, extinctionCoefficient);
            maxLightingDistance = Mathf.Max(0, maxLightingDistance);

            weathermapValueRange.x = Mathf.Clamp(weathermapValueRange.x, 0.0f, 1.0f);
            weathermapValueRange.y = Mathf.Clamp(weathermapValueRange.y, 0.0f, 1.0f);
            weathermapValueRange.x = Mathf.Min(weathermapValueRange.x, weathermapValueRange.y);
            weathermapValueRange.y = Mathf.Max(weathermapValueRange.x, weathermapValueRange.y);

            curlTextureInfluence = Mathf.Max(0, curlTextureInfluence);
            curlTextureScale = Mathf.Max(0, curlTextureScale);

            detail1TextureInfluence = Mathf.Clamp(detail1TextureInfluence, 0f, 1f);
            detail1TextureScale = Vector3.Max(Vector3.zero, detail1TextureScale);

            if (baseFalloffSelection == FalloffSelection.Custom)
            {
                baseTextureRGBAInfluence = SetRelativeValues01(ref baseTextureRInfluence, ref baseTextureGInfluence, ref baseTextureBInfluence, ref baseTextureAInfluence);
            }
            else
            {
                baseTextureRGBAInfluence = GetInfluenceFromFalloffSelection(baseFalloffSelection);
            }

            if (detail1FalloffSelection == FalloffSelection.Custom)
            {
                detail1TextureRGBAInfluence = SetRelativeValues01(ref detail1TextureRInfluence, ref detail1TextureGInfluence, ref detail1TextureBInfluence, ref detail1TextureAInfluence);
            }
            else
            {
                detail1TextureRGBAInfluence = GetInfluenceFromFalloffSelection(detail1FalloffSelection);
            }

            if (detail2FalloffSelection == FalloffSelection.Custom)
            {
                detail2TextureRGBAInfluence = SetRelativeValues01(ref detail2TextureRInfluence, ref detail2TextureGInfluence, ref detail2TextureBInfluence, ref detail2TextureAInfluence);
            }
            else
            {
                detail2TextureRGBAInfluence = GetInfluenceFromFalloffSelection(detail2FalloffSelection);
            }



            detail1TextureHeightRemap = ClampVec2_01(detail1TextureHeightRemap);
            detail1TextureHeightRemap = ClampVec2_01(detail1TextureHeightRemap);

            // Cache?
#if UNITY_EDITOR
            GetCloudTextureReferenceData(ref textureReference);

            baseTexture = GetTexture(baseTextureID, baseTextureQuality);
            detail1Texture = GetTexture(detail1TextureID, detail1TextureQuality);
            detail2Texture = GetTexture(detail2TextureID, detail2TextureQuality);
#endif

            if (detail1Texture == null)
                detail1TextureInfluence = 0.0f;

            if (detail2Texture == null)
                detail2TextureInfluence = 0.0f;

            highAltExtinctionCoefficient = Mathf.Max(0, highAltExtinctionCoefficient);

            highAltScale1 = Vector2.Max(Vector2.zero, highAltScale1);
            highAltScale2 = Vector2.Max(Vector2.zero, highAltScale2);
            highAltScale3 = Vector2.Max(Vector2.zero, highAltScale3);

            highAltTexInfluence = SetRelativeValues01(ref highAltStrength1, ref highAltStrength2, ref highAltStrength3);

            renderScale = GetRenderScalingFromRenderScaleSelection(renderScaleSelection, renderScale);

            blueNoise = RoundTo2Decimals(blueNoise);
            renderScale = RoundTo2Decimals(renderScale);
            HGEccentricityBackward = RoundTo2Decimals(HGEccentricityBackward);
            HGEccentricityForward = RoundTo2Decimals(HGEccentricityForward);
            HGBlend = RoundTo2Decimals(HGBlend);
            cloudiness = RoundTo2Decimals(cloudiness);
            cloudinessDensityInfluence = RoundTo2Decimals(cloudinessDensityInfluence);
            heightDensityInfluence = RoundTo2Decimals(heightDensityInfluence);

            detail1TextureInfluence = RoundTo2Decimals(detail1TextureInfluence);
            detail2TextureInfluence = RoundTo2Decimals(detail2TextureInfluence);

            highAltCloudiness = RoundTo2Decimals(highAltCloudiness);
        }

        private void GetCloudTextureReferenceData(ref CloudTextureReferenceData crtd_ref)
        {
#if UNITY_EDITOR
            List<string> guids = UnityEditor.AssetDatabase.FindAssets("t:CloudTextureReferenceData").ToList();

            if (guids.Count <= 0)
            {
                Debug.Log("No Texture Reference Data configurations present in this project. Unable to find Cloud Volume Texture Data. Exiting.");
                return;
            }
            else
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                CloudTextureReferenceData localCTRD = UnityEditor.AssetDatabase.LoadAssetAtPath<CloudTextureReferenceData>(assetPath);

                if (guids.Count > 1)
                    Debug.Log("There are multiple Texture Reference Data configurations present in this project. Defaulted to the first identified dataset: " + localCTRD);

                crtd_ref = localCTRD;
            }
#endif
        }


        private Texture3D GetTexture(TextureIdentifier textureType, TextureQuality qualitySetting)
        {
            if (textureReference == null)
            {
                Debug.Log("Missing 3D Texture Reference Data. Trying to fetch.");
                GetCloudTextureReferenceData(ref textureReference);
                if (textureReference == null)
                {
                    Debug.Log("3D Texture Reference Data not found. Exiting.");
                    return null;
                }
            }

            if (textureType == TextureIdentifier.None)
                return null;

            for (int i = 0; i < textureReference.dataset.Count; i++)
            {
                bool typeMatch = textureReference.dataset[i].TextureTypeIdentifier == textureType ? true : false;
                bool qualityMatch = textureReference.dataset[i].TextureQualityDefinition == qualitySetting ? true : false;

                if (typeMatch && qualityMatch)
                {
                    return textureReference.dataset[i].TextureFileReference;
                }
            }

            Debug.Log($"Requested texture {textureType} - {qualitySetting} quality could not be found in cloud texture reference dataset.", textureReference);
            return null;
        }

        float RoundTo2Decimals(float input)
        {
            return (float)System.Math.Round((double)input, 2);
        }

        Vector2 ClampVec2_01(Vector2 input)
        {
            return Vector2.Min(Vector2.one, Vector2.Max(Vector2.zero, input));
        }

        Vector4 SetRelativeValues01(ref float x, ref float y, ref float z, ref float w)
        {
            Vector4 newIn = new Vector4(x, y, z, w);
            newIn = Vector4.Max(Vector4.zero, newIn);
            newIn = Vector4.Min(Vector4.one, newIn);
            float sum = newIn.x + newIn.y + newIn.z + newIn.w;

            if (sum <= 0f)
                return newIn;

            newIn.x /= sum;
            newIn.y /= sum;
            newIn.z /= sum;
            newIn.w /= sum;

            x = newIn.x;
            y = newIn.y;
            z = newIn.z;
            w = newIn.w;

            return newIn;
        }

        Vector3 SetRelativeValues01(ref float x, ref float y, ref float z)
        {
            Vector3 newIn = new Vector3(x, y, z);
            newIn = Vector3.Max(Vector3.zero, newIn);
            newIn = Vector3.Min(Vector3.one, newIn);

            float sum = newIn.x + newIn.y + newIn.z;

            if (sum <= 0f)
                return newIn;

            newIn.x /= sum;
            newIn.y /= sum;
            newIn.z /= sum;

            x = newIn.x;
            y = newIn.y;
            z = newIn.z;

            return newIn;
        }



        private CloudTextureReferenceData textureReference = null;

        // Move to Editor SO?
        #region Editor States
        public PageSelection pageSelection = PageSelection.Basic;

        public bool lowAltitudeModelingState = false;
        public bool lowAltitudeLightingState = false;
        public bool lowAltitudeWeatherState = false;
        public bool lowAltitudeBaseState = false;
        public bool lowAltitudeDetail1State = false;
        public bool lowAltitudeDetail2State = false;
        public bool lowAltitudeCurlState = false;
        #endregion

        #region Volumetric Basic Setup
        public int stepCount = 32;
        public float blueNoise = 1.0f;

        public Color sunColor = Color.white;
        public float ambientExposure = 1.0f;

        public float HGEccentricityForward = 0.6f;
        public float HGEccentricityBackward = -0.2f;
        public float HGBlend = 0.4f;
        public float HGStrength = 1.0f;

        public CelestialBodySelection celestialBodySelection;
        public int planetRadius = 6378;
        public float cloudLayerHeight = 0.6f;
        public float cloudLayerThickness = 0.6f;
        public float cloudFadeDistance = 30f;
        public float fogPower = 2.0f;

        public RenderScaleSelection renderScaleSelection = RenderScaleSelection.Half;
        public float renderScale = 0.5f;
        public bool renderInSceneView = true;
        public bool taaEnabled = true;
        public float taaBlendFactor = 0.1f;
        public DepthCullOptions depthCullOptions = DepthCullOptions.RenderAsSkybox;
        public bool subpixelJitterEnabled = true;
        #endregion

        #region Low Altitude

        #region Rendering
        public float extinctionCoefficient = 120f;
        public int maxLightingDistance = 2000;
        public float multipleScatteringAmpGain = 0.5f;
        public float multipleScatteringDensityGain = 0.1f;
        public int multipleScatteringOctaves = 3;
        #endregion

        #region Modeling
        public float cloudiness = 0.5f;
        public float distantCoverageDepth = 30f;
        public float distantCoverageAmount = 0.3f;
        public float heightDensityInfluence = 0.6f;
        public float cloudinessDensityInfluence = 0.6f;
        #endregion

        #region Weather
        public WeathermapType weathermapType = WeathermapType.Procedural;
        public Texture2D weathermapTexture = null;
        public Vector2 weathermapVelocity = Vector2.zero;
        public float weathermapScale = 1.0f;
        public Vector2 weathermapValueRange = new Vector2(-1.0f, 1.0f);
        #endregion

        #region Base Volume Model

        public TextureIdentifier baseTextureID = TextureIdentifier.Perlin;
        public TextureQuality baseTextureQuality = TextureQuality.Ultra;
        public Texture3D baseTexture = null;
        public Vector3 baseTextureScale = new Vector3(10f, 10f, 10f);
        public Vector3 baseTextureTimescale = new Vector3(10f, -10f, 0f);

        public FalloffSelection baseFalloffSelection = FalloffSelection.Linear;
        public Vector4 baseTextureRGBAInfluence;
        public float baseCurlAdjustment = 0.1f;

        public float baseTextureRInfluence = 0.5f;
        public float baseTextureGInfluence = 0.25f;
        public float baseTextureBInfluence = 0.25f;
        public float baseTextureAInfluence = 0.125f;
        #endregion

        #region Detail 1 Volume Model
        public TextureIdentifier detail1TextureID = TextureIdentifier.Worley;
        public TextureQuality detail1TextureQuality = TextureQuality.Low;
        public Texture3D detail1Texture = null;
        public float detail1TextureInfluence = 0.2f;
        public Vector3 detail1TextureScale = new Vector3(125f, 125f, 125f);
        public Vector3 detail1TextureTimescale = new Vector3(25f, -50f, 30f);

        public FalloffSelection detail1FalloffSelection = FalloffSelection.Linear;
        public Vector4 detail1TextureRGBAInfluence;
        public float detail1TextureRInfluence = 0.4f;
        public float detail1TextureGInfluence = 0.2f;
        public float detail1TextureBInfluence = 0.2f;
        public float detail1TextureAInfluence = 0.2f;
        public Vector2 detail1TextureHeightRemap = new Vector2(0.0f, 0.3f);
        #endregion

        #region Detail 2 Volume Model
        public TextureIdentifier detail2TextureID = TextureIdentifier.Worley;
        public TextureQuality detail2TextureQuality = TextureQuality.Low;
        public Texture3D detail2Texture = null;
        public float detail2TextureInfluence = 0.2f;
        public Vector3 detail2TextureScale = new Vector3(300f, 300f, 300f);
        public Vector3 detail2TextureTimescale = new Vector3(-103f, -123f, -78f);
        public FalloffSelection detail2FalloffSelection = FalloffSelection.Linear;
        public Vector4 detail2TextureRGBAInfluence;

        public float detail2TextureRInfluence = 0.25f;
        public float detail2TextureGInfluence = 0.25f;
        public float detail2TextureBInfluence = 0.25f;
        public float detail2TextureAInfluence = 0.25f;
        public Vector2 detail2TextureHeightRemap = new Vector2(0f, 0.5f);
        #endregion

        #region Detail Curl 2D Model
        public Texture2D curlTexture;
        public float curlTextureInfluence;
        public float curlTextureScale;
        public float curlTextureTimescale;
        #endregion

        #endregion

        #region High Altitude
        public float highAltExtinctionCoefficient = 0.2f;
        public float highAltCloudiness = 0.5f;

        public Texture2D highAltTex1 = null;
        public Vector2 highAltScale1 = new Vector2(5f, 5f);
        public Vector2 highAltTimescale1 = new Vector2(5f, 5f);
        public float highAltStrength1 = 0.5f;

        public Texture2D highAltTex2 = null;
        public Vector2 highAltScale2 = new Vector2(5f, 5f);
        public Vector2 highAltTimescale2 = new Vector2(5f, 5f);
        public float highAltStrength2 = 0.3f;

        public Texture2D highAltTex3 = null;
        public Vector2 highAltScale3 = new Vector2(5f, 5f);
        public Vector2 highAltTimescale3 = new Vector2(5f, 5f);
        public float highAltStrength3 = 0.2f;

        public Vector3 highAltTexInfluence;
        #endregion

        private Vector4 GetInfluenceFromFalloffSelection(FalloffSelection falloffSelection)
        {
            switch (falloffSelection)
            {
                case FalloffSelection.Linear:
                    return new Vector4(0.4f, 0.3f, 0.2f, 0.1f);
                case FalloffSelection.Quadratic:
                    return new Vector4(0.53f, 0.3f, 0.13f, 0.03f);
                case FalloffSelection.Cubic:
                    return new Vector4(0.64f, 0.27f, 0.08f, 0.01f);
                case FalloffSelection.Exp:
                    return new Vector4(0.35f, 0.27f, 0.21f, 0.17f);
                case FalloffSelection.Div:
                    return new Vector4(0.12f, 0.16f, 0.24f, 0.48f);
                default:
                    return new Vector4(0.5f, 0.25f, 0.125f, 0.0625f);
            }
        }

        private int GetRadiusFromCelestialBodySelection(CelestialBodySelection celestialBodySelection, int currentVal)
        {
            switch (celestialBodySelection)
            {
                case CelestialBodySelection.Earth:
                    return 6378;
                case CelestialBodySelection.Mars:
                    return 3389;
                case CelestialBodySelection.Venus:
                    return 6052;
                case CelestialBodySelection.Luna:
                    return 1737;
                case CelestialBodySelection.Titan:
                    return 2575;
                case CelestialBodySelection.Enceladus:
                    return 252;
                default:
                    return Mathf.Max(0, currentVal);
            }
        }

        private float GetRenderScalingFromRenderScaleSelection(RenderScaleSelection renderScaleSelection, float currentVal)
        {
            switch (renderScaleSelection)
            {
                case RenderScaleSelection.Full:
                    return 1.0f;
                case RenderScaleSelection.Half:
                    return 0.5f;
                case RenderScaleSelection.Quarter:
                    return 0.25f;
                default:
                    return Mathf.Max(0.10f, RoundTo2Decimals(currentVal));

            }
        }
    }

    public enum TextureIdentifier
    {
        None,
        Perlin,
        PerlinWorley,
        Worley
    }

    public enum TextureQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    public enum PageSelection
    {
        Basic,
        LowAltitude,
        HighAltitude
    }

    public enum FalloffSelection
    {
        Linear,
        Quadratic,
        Cubic,
        Exp,
        Div,
        Custom
    }

    public enum CelestialBodySelection
    {
        Earth,
        Mars,
        Venus,
        Luna,
        Titan,
        Enceladus,
        Custom
    }

    public enum RenderScaleSelection
    {
        Full,
        Half,
        Quarter,
        Custom
    }

    public enum DepthCullOptions
    {
        RenderAsSkybox,
        RenderLocal
    }

    public enum WeathermapType
    {
        Procedural,
        Texture
    }

}