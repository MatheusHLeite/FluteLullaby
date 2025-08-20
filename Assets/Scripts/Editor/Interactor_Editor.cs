#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;

[CustomEditor(typeof(Item_Interactor))] [CanEditMultipleObjects]
public class Interactor_Editor : OdinEditor { 
    public override void OnInspectorGUI() { base.OnInspectorGUI(); }
}
#endif