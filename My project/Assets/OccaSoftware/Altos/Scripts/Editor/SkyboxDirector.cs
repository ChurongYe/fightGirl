using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OccaSoftware.Altos.Editor
{
    public class SkyboxDirector : EditorWindow
    {
        [MenuItem("GameObject/Skybox Director", false, 15)]
        public static void CreateSkyboxDirector()
        {
            Debug.Log("Setting up Skybox Director...");
            TimeOfDayManager mgr = FindObjectOfType<TimeOfDayManager>();
            if (mgr != null)
            {
                Debug.Log("Skybox Director already exists in scene. Exiting.", mgr);
                return;
            }

            SkyboxData skyboxData = CreateSkyboxDirectorParent();

            CreateSunLamp(skyboxData);
            CreateMoonLamp(skyboxData);
            CreateStars(skyboxData);
            CreateClouds(skyboxData);

            skyboxData.timeOfDayManager.Awake();
        }

        private static T GetFirstReturned<T>()
        {
            string searchQuery = "t:" + typeof(T);
            string firstGUIDFound = AssetDatabase.FindAssets(searchQuery)[0];
            string path = AssetDatabase.GUIDToAssetPath(firstGUIDFound);
            return (T)(object)AssetDatabase.LoadAssetAtPath(path, typeof(T));
        }

        private static SkyboxData CreateSkyboxDirectorParent()
        {
            List<System.Type> types = new List<System.Type>()
            {
                typeof(TimeOfDayManager)
            };

            GameObject skyboxDirector = new GameObject("Skybox Director", types.ToArray());
            TimeOfDayManager timeOfDayManager = skyboxDirector.GetComponent<TimeOfDayManager>();

            // Set Skybox Definition Scriptable Object on Skybox Director
            SkyboxDefinitionScriptableObject skyboxDefinitionObject = GetFirstReturned<SkyboxDefinitionScriptableObject>();
            timeOfDayManager.SetSkyboxDefinition(skyboxDefinitionObject);

            return new SkyboxData
            {
                skyboxDirector = skyboxDirector,
                timeOfDayManager = timeOfDayManager
            };
        }

        private static void CreateSunLamp(SkyboxData skyboxData)
        {
            List<System.Type> types = new List<System.Type>()
            {
                typeof(Light),
                typeof(SunLampObject)
            };

            GameObject sun = new GameObject("Sun Lamp", types.ToArray());
            Light sunLamp = sun.GetComponent<Light>();
            sunLamp.type = LightType.Directional;
            sunLamp.shadows = LightShadows.Soft;
            UnityEngine.Rendering.GraphicsSettings.lightsUseColorTemperature = true;
            UnityEngine.Rendering.GraphicsSettings.lightsUseLinearIntensity = true;
            sunLamp.useColorTemperature = true;
            sunLamp.colorTemperature = 4800f;
            sun.transform.SetParent(skyboxData.skyboxDirector.transform);
            skyboxData.timeOfDayManager.SetSunLamp(sun.GetComponent<Light>());
        }

        private static void CreateMoonLamp(SkyboxData skyboxData)
        {
            List<System.Type> types = new List<System.Type>()
            {
                typeof(Light),
                typeof(MoonObject),
                typeof(UnityEngine.VFX.VisualEffect)
            };

            GameObject moon = new GameObject("Moon Lamp", types.ToArray());
            moon.transform.rotation = Quaternion.Euler(new Vector3(20, -90, 0));
            Light moonLamp = moon.GetComponent<Light>();
            moonLamp.type = LightType.Directional;
            moonLamp.shadows = LightShadows.None;
            moonLamp.useColorTemperature = true;
            moonLamp.colorTemperature = 10000f;


            string moonAsset_guid = AssetDatabase.FindAssets("Moon OS")[0];
            string moonAsset_path = AssetDatabase.GUIDToAssetPath(moonAsset_guid);
            UnityEngine.VFX.VisualEffectAsset moonAsset = (UnityEngine.VFX.VisualEffectAsset)AssetDatabase.LoadAssetAtPath(moonAsset_path, typeof(UnityEngine.VFX.VisualEffectAsset));
            moon.GetComponent<UnityEngine.VFX.VisualEffect>().visualEffectAsset = moonAsset;

            moon.transform.SetParent(skyboxData.skyboxDirector.transform);
            moon.GetComponent<MoonObject>().SetMoonDefinition(GetFirstReturned<MoonDefinitionScriptableObject>());
        }

        private static void CreateStars(SkyboxData skyboxData)
        {
            List<System.Type> types = new List<System.Type>()
            {
                typeof(UnityEngine.VFX.VisualEffect),
                typeof(StarObject)
            };

            GameObject stars = new GameObject("Stars", types.ToArray());
            stars.transform.SetParent(skyboxData.skyboxDirector.transform);
            string starAsset_guid = AssetDatabase.FindAssets("Stars OS")[0];
            string starAsset_path = AssetDatabase.GUIDToAssetPath(starAsset_guid);
            UnityEngine.VFX.VisualEffectAsset starAsset = (UnityEngine.VFX.VisualEffectAsset)AssetDatabase.LoadAssetAtPath(starAsset_path, typeof(UnityEngine.VFX.VisualEffectAsset));
            stars.GetComponent<UnityEngine.VFX.VisualEffect>().visualEffectAsset = starAsset;
            stars.GetComponent<StarObject>().SetStarDefinition(GetFirstReturned<StarDefinitionScriptableObject>());
            stars.GetComponent<StarObject>().Setup();
        }

        private static void CreateClouds(SkyboxData skyboxData)
        {
            List<System.Type> types = new List<System.Type>()
            {
                typeof(VolumetricCloudVolume)
            };
            
            GameObject clouds = new GameObject("Clouds", types.ToArray());
            clouds.transform.SetParent(skyboxData.skyboxDirector.transform);
            clouds.GetComponent<VolumetricCloudVolume>().cloudData = GetFirstReturned<VolumetricCloudsDefinitionScriptableObject>();
        }

        private struct SkyboxData
        {
            public GameObject skyboxDirector;
            public TimeOfDayManager timeOfDayManager;
        }
    }

}
