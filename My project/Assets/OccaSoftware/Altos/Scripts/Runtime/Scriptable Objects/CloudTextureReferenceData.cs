using System.Collections.Generic;
using UnityEngine;

namespace OccaSoftware.Altos
{
    [CreateAssetMenu(fileName = "TextureReferenceData", menuName = "Skies/TextureReferenceData")]
    public class CloudTextureReferenceData : ScriptableObject
    {
        public List<TextureConfigurationSet> dataset = new List<TextureConfigurationSet>();
    }

    [System.Serializable]
    public class TextureConfigurationSet
    {
        public Texture3D TextureFileReference;
        public TextureIdentifier TextureTypeIdentifier;
        public TextureQuality TextureQualityDefinition;
    }
}
