using Unity.Entities;
using UnityEngine;

public class SpawnPrefabAuthor : MonoBehaviour
{
    [SerializeField] GameObject prefab;

    private class SpawnPrefabAuthorBaker : Baker<SpawnPrefabAuthor>
    {
        public override void Bake(SpawnPrefabAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
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


partial struct SpawnPrefabSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;
        
        foreach (var spawnPrefabData in SystemAPI.Query<SpawnPrefabData>())
        {
            state.EntityManager.Instantiate(spawnPrefabData.prefabToSpawn);
        }
    }
}

