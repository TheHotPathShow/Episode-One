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

public struct LifeTimeData : IComponentData, IEnableableComponent
{
    public float SecondsLeft;
}

partial struct LifeTimeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (lifeTimeDataRef, isAliveStateRef) in SystemAPI.Query<RefRW<LifeTimeData>, EnabledRefRW<LifeTimeData>>())
        {
            if (lifeTimeDataRef.ValueRO.SecondsLeft > 0)
                lifeTimeDataRef.ValueRW.SecondsLeft -= deltaTime;
            else
                isAliveStateRef.ValueRW = false;
        }
        
        state.EntityManager.DestroyEntity(SystemAPI.QueryBuilder()
            .WithDisabled<LifeTimeData>()
            .Build().ToEntityArray(state.WorldUpdateAllocator));
    }
}