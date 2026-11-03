using System;
using UnityEngine;

namespace LolSuperman.Tarea08
{
    [DisallowMultipleComponent]
    public sealed class TrackedModel : MonoBehaviour
    {
        [SerializeField] private ModelInfo information;
        public ModelInfo Information => information;
        public event Action<TrackedModel> BecameUnavailable;

        public void SetInformation(ModelInfo value) => information = value;

        private void OnDisable() => BecameUnavailable?.Invoke(this);
    }
}
