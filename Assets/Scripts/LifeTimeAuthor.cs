using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public class LifeTimeAuthor : MonoBehaviour
{
    [SerializeField] float secondsToLive = 2f;
    
    private class Baker : Baker<LifeTimeAuthor>
    {
        public override void Bake(LifeTimeAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new LifeTimeData { SecondsLeft = authoring.secondsToLive });
        }
    }
}

public struct LifeTimeData : IComponentData
{
    public float SecondsLeft;
}

partial struct LifeTimeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var lifetimeDataHandle = SystemAPI.GetComponentTypeHandle<LifeTimeData>();
        foreach (var chunk in SystemAPI.QueryBuilder()
                     .WithAll<LifeTimeData>().Build().ToArchetypeChunkArray(state.WorldUpdateAllocator))
        {
            var lifeTimeData = chunk.GetNativeArray(ref lifetimeDataHandle);
            var entityArray = chunk.GetNativeArray(state.EntityManager.GetEntityTypeHandle());
            for (var i = 0; i < chunk.Count; i++)
            {
                var lifeTime = lifeTimeData[i];
                lifeTime.SecondsLeft -= deltaTime;
                if (lifeTime.SecondsLeft <= 0)
                {
                    state.EntityManager.DestroyEntity(entityArray[i]);
                    lifetimeDataHandle.Update(ref state);
                    lifeTimeData = chunk.GetNativeArray(ref lifetimeDataHandle);
                    entityArray = chunk.GetNativeArray(state.EntityManager.GetEntityTypeHandle());
                }
                else
                    lifeTimeData[i] = lifeTime;
            }
        }
    }
}