using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public class SpinAuthor : MonoBehaviour
{
    [SerializeField] float degreesPerSecond = 20f;
    
    private class Baker : Baker<SpinAuthor>
    {
        public override void Bake(SpinAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SpinData { radiansPerSecond = math.radians(authoring.degreesPerSecond) });
        }
    }
}

public struct SpinData : IComponentData
{
    public float radiansPerSecond;
}

partial struct SpinSystem : ISystem
{
    bool m_IsEnableMode;
    
    public void OnUpdate(ref SystemState state)
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            m_IsEnableMode = !m_IsEnableMode;
            if (m_IsEnableMode)
                foreach (var e in SystemAPI.QueryBuilder().WithAll<SpinData, Prefab>().WithOptions(EntityQueryOptions.IncludePrefab).Build().ToEntityArray(state.WorldUpdateAllocator))
                    state.EntityManager.RemoveComponent<Prefab>(e);
            else
                foreach (var e in SystemAPI.QueryBuilder().WithAll<SpinData>().Build().ToEntityArray(state.WorldUpdateAllocator))
                    state.EntityManager.AddComponent<Prefab>(e);
        }
        
        
        foreach (var (ltwRef, spinData) in SystemAPI.Query<RefRW<LocalToWorld>, RefRO<SpinData>>().WithAll<Prefab>().WithOptions(EntityQueryOptions.IncludePrefab))
        {
            ltwRef.ValueRW.Value = math.mul(ltwRef.ValueRO.Value, float4x4.RotateY(spinData.ValueRO.radiansPerSecond * SystemAPI.Time.DeltaTime));
            Debug.Log((ltwRef.ValueRO.Rotation));
        }
    }
}