using UnityEngine;

namespace LolSuperman.Tarea08
{
    [CreateAssetMenu(fileName = "ModelInfo", menuName = "Tarea 08/Información de modelo")]
    public sealed class ModelInfo : ScriptableObject
    {
        [SerializeField] private string modelName;
        [SerializeField, TextArea(3, 6)] private string description;
        [SerializeField] private string category;
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField] private Sprite icon;

        public string ModelName => modelName;
        public string Description => description;
        public string Category => category;
        public int Level => level;
        public Sprite Icon => icon;

        public void SetContent(string displayName, string details, string modelCategory, int modelLevel, Sprite modelIcon)
        {
            modelName = displayName;
            description = details;
            category = modelCategory;
            level = Mathf.Max(1, modelLevel);
            icon = modelIcon;
        }
    }
}
