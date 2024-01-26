using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class SpawnPrefabAuthor : MonoBehaviour
{
    [SerializeField] GameObject prefab;

    private class SpawnPrefabAuthorBaker : Baker<SpawnPrefabAuthor>
    {
        public override void Bake(SpawnPrefabAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            if (authoring.prefab == null)
                return;
            AddComponent(entity, new SpawnPrefabData 
            { 
                prefabToSpawn = GetEntity(authoring.prefab, TransformUsageFlags.Renderable) 
            });
        }
    }
}

public struct SpawnPrefabData : IComponentData
{
    public Entity prefabToSpawn;
}


[UpdateAfter(typeof(TransformSystemGroup))]
partial struct SpawnPrefabSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;
        
        foreach (var (spawnPrefabData, ltw) in SystemAPI.Query<SpawnPrefabData, LocalToWorld>())
        {
            var spawnedEntity = state.EntityManager.Instantiate(spawnPrefabData.prefabToSpawn);
            SystemAPI.SetComponent(spawnedEntity, LocalTransform.FromMatrix(ltw.Value));
        }
        
        foreach (var (spawnPrefabData, ltw) in SystemAPI.Query<SpawnPrefabData, LocalToWorld>())
        {
            var spawnedEntity = state.EntityManager.Instantiate(spawnPrefabData.prefabToSpawn);
            SystemAPI.SetComponent(spawnedEntity, LocalTransform.FromMatrix(ltw.Value));
        }
    }
}

