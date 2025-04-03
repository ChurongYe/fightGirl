namespace OccaSoftware.Altos
{
    public static class Helpers
    {
        public static float Remap(float value, float low1, float high1, float low2, float high2)
        {
            return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
        }

        public static void RenderFeatureOnEnable(UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.Scene> action)
        {
            #if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += action;
            #endif

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += action;
        }

        public static void RenderFeatureOnDisable(UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.Scene> action)
        {
            #if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= action;
            #endif

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= action;
        }
    }
}
