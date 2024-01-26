using Unity.Entities;
using UnityEngine;

public class BeltAuthor : MonoBehaviour
{
    public GameObject beltPrefab;
    public int beltLength = 10;

    private class BeltAuthorBaker : Baker<BeltAuthor>
    {
        public override void Bake(BeltAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            DependsOn(authoring.transform);
            if (authoring.beltPrefab == null || authoring.beltLength <= 0)
                return;
            
            AddComponent(entity, new BeltData
            {
                prefabToSpawn = GetEntity(authoring.beltPrefab, TransformUsageFlags.Renderable),
                beltLength = authoring.beltLength
            });
            AddComponent<BeltSpawnerTag>(entity);
        }
    }
}