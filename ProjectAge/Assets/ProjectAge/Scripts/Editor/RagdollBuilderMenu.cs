using UnityEditor;
using UnityEngine;

namespace ProjectAge.EditorTools
{
    public static class RagdollBuilderMenu
    {
        [MenuItem("ProjectAge/Build Ragdoll On Selection")]
        static void BuildOnSelection()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("[RagdollBuilder] Select a humanoid character root first.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(go, "Build Ragdoll");
            if (RagdollBuilder.Build(go))
                Debug.Log($"[RagdollBuilder] Built ragdoll on '{go.name}'.");
        }

        [MenuItem("ProjectAge/Build Ragdoll On Selection", true)]
        static bool BuildOnSelectionValidate() => Selection.activeGameObject != null;
    }
}
