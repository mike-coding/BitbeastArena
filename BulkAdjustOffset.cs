using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class BulkAdjustOffset : MonoBehaviour
{
    [Header("New Offset Values")]
    public Vector3 newOffset = new Vector3(0, 0, 0); // Adjust these values based on your notebook's values

    [ContextMenu("Adjust Offsets For All Children")]
    public void AdjustOffsetsForAllChildren()
    {
        // Go through all child objects of this object
        foreach (Transform child in transform)
        {
            child.localPosition = newOffset;
        }
    }

#if UNITY_EDITOR
    [MenuItem("My Tools/Adjust All Scene Prefab Instances")]
    public static void AdjustAllScenePrefabInstances()
    {
        BulkAdjustOffset[] scripts = FindObjectsOfType<BulkAdjustOffset>();
        foreach (BulkAdjustOffset script in scripts)
        {
            script.AdjustOffsetsForAllChildren();
        }
    }

    [MenuItem("My Tools/Adjust All Scene Prefab Instances Parent Z OFFSET")]
    public static void AdjustAllScenePrefabInstancesParentZOffset()
    {
        float zAdjustmentValue = 0.09f; // Set this to whatever value you want the Z position to be adjusted by
        BulkAdjustOffset[] scripts = FindObjectsOfType<BulkAdjustOffset>();
        foreach (BulkAdjustOffset script in scripts)
        {
            SpriteRenderer childSprite = script.gameObject.GetComponentInChildren<SpriteRenderer>();
            if (childSprite && childSprite.sortingLayerName == "UnderWaterObstacles")
            {
                Vector3 parentPos = script.transform.position;
                script.transform.position = new Vector3(parentPos.x, parentPos.y, parentPos.z + zAdjustmentValue);
            }
        }
    }

    [MenuItem("My Tools/Adjust And Remove Collider If UnderWater")]
    public static void AdjustAndRemoveColliderIfUnderWater()
    {
        float zAdjustmentValue = 0.1f; // Set this to whatever value you want the Z position to be adjusted by
        BulkAdjustOffset[] scripts = FindObjectsOfType<BulkAdjustOffset>();
        foreach (BulkAdjustOffset script in scripts)
        {
            SpriteRenderer[] childSprites = script.gameObject.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer childSprite in childSprites)
            {
                if (childSprite.sortingLayerName == "UnderWaterObstacles")
                {
                    // Adjust the Z position
                    Vector3 parentPos = script.transform.position;
                    script.transform.position = new Vector3(parentPos.x, parentPos.y, zAdjustmentValue);

                    // Remove the collider from the child
                    PolygonCollider2D collider = childSprite.GetComponent<PolygonCollider2D>();
                    if (collider)
                    {
                        DestroyImmediate(collider); // Use DestroyImmediate in the editor, else you'd use Destroy in runtime.
                    }
                }
            }
        }
    }

#endif
}
