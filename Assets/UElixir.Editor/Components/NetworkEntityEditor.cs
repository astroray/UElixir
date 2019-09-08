using UElixir;
using UnityEditor;

namespace UElixirEditor
{
    [CustomEditor(typeof(NetworkEntity))]
    public class NetworkEntityEditor : Editor
    {
        private NetworkEntity m_networkEntity;

        private void OnEnable()
        {
            m_networkEntity = target as NetworkEntity;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.LabelField(nameof(NetworkEntity.NetworkId), m_networkEntity.NetworkId.ToString());
        }
    }
}