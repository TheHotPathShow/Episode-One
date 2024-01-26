using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[TemporaryBakingType]
public struct BeltData : IComponentData
{
    public Entity prefabToSpawn;
    public int beltLength;
}

[BakingType]
struct BeltStillHereTag : ICleanupComponentData
{
    public Entity mainBeltEntity;
}

[BakingType]
struct BeltSpawnerTag : IComponentData {}

[InternalBufferCapacity(4)]
public struct LinkedEntityGroupFake : ICleanupBufferElementData
{
    public Entity Value;
}

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateInGroup(typeof(PostBakingSystemGroup))]
partial struct BeltBakingSystem : ISystem
{
    NativeList<Entity> m_BeltEntities;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        m_BeltEntities = new NativeList<Entity>(Allocator.Persistent);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Initialize belts
        state.EntityManager.AddComponent<BeltStillHereTag>(
            SystemAPI.QueryBuilder().WithAll<BeltData>().WithNone<BeltStillHereTag>().Build());
        
        // Destroy previous LinkedEntityGroup (fake)
        foreach (var e in SystemAPI.QueryBuilder()
                     .WithAll<BeltStillHereTag, BeltData>()
                     .Build().ToEntityArray(state.WorldUpdateAllocator))
        {
            if (SystemAPI.HasBuffer<LinkedEntityGroupFake>(e))
            {
                state.EntityManager.DestroyEntity(SystemAPI.GetBuffer<LinkedEntityGroupFake>(e).Reinterpret<Entity>().AsNativeArray());
                SystemAPI.GetBuffer<LinkedEntityGroupFake>(e).Clear();
            }
            else
            {
                state.EntityManager.AddBuffer<LinkedEntityGroupFake>(e);
            }
        }
        
        using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        foreach (var (tr, beltData, guid, leg, e) in 
                 SystemAPI.Query<RefRO<TransformAuthoring>, RefRO<BeltData>, RefRO<EntityGuid>, DynamicBuffer<LinkedEntityGroupFake>>().WithAll<BeltStillHereTag>().WithEntityAccess())
        {
            // Spawn belts and make sure they are linked to the main belt entity
            m_BeltEntities.Resize(beltData.ValueRO.beltLength, NativeArrayOptions.UninitializedMemory);
            state.EntityManager.Instantiate(beltData.ValueRO.prefabToSpawn, m_BeltEntities.AsArray());
            leg.AddRange(m_BeltEntities.AsArray().Reinterpret<LinkedEntityGroupFake>());

            var id = 0;
            var guidOrigin = guid.ValueRO;
            for (int i = 0; i < beltData.ValueRO.beltLength; ++i)
            {
                // Re-parent children of the belt to the spawned belt (otherwise will use the prefab as parent)
                var beltChildren = SystemAPI.GetBuffer<LinkedEntityGroup>(m_BeltEntities[i]);
                foreach (var child in beltChildren)
                {
                    SystemAPI.SetComponent(child.Value, new EntityGuid(guidOrigin.OriginatingId, guidOrigin.OriginatingSubId, 42, (uint)(id++)+1));
                    if (SystemAPI.HasComponent<Parent>(child.Value) &&
                        SystemAPI.GetComponent<Parent>(child.Value).Value == beltData.ValueRO.prefabToSpawn)
                    {
                        SystemAPI.SetComponent(child.Value, new Parent { Value = m_BeltEntities[i] });
                    }
                }
                
                // Add the children to the main belt entity
                leg.AddRange(beltChildren.AsNativeArray().Reinterpret<LinkedEntityGroupFake>());
                
                // Set the guid of the belt
                SystemAPI.SetComponent(m_BeltEntities[i], new EntityGuid(guidOrigin.OriginatingId, guidOrigin.OriginatingSubId, 42, (uint)(id++)+1));

                // Set the transform of the belt
                var localPosition = new float3(i, 0, 0);
                SystemAPI.SetComponent(m_BeltEntities[i], new LocalToWorld {Value = float4x4.Translate(localPosition) * tr.ValueRO.LocalToWorld});
                SystemAPI.SetComponent(m_BeltEntities[i], LocalTransform.FromPosition(localPosition + tr.ValueRO.Position));
                if (SystemAPI.HasComponent<TransformAuthoring>(m_BeltEntities[i]))
                {
                    // get the transform authoring component from e as reference
                    var mainTransform = SystemAPI.GetComponent<TransformAuthoring>(e);
                    
                    ref var beltTransformAuthoring = ref SystemAPI.GetComponentRW<TransformAuthoring>(m_BeltEntities[i]).ValueRW;
                    beltTransformAuthoring.LocalToWorld = float4x4.Translate(localPosition) * mainTransform.LocalToWorld;
                    beltTransformAuthoring.Position = localPosition + mainTransform.Position;
                    beltTransformAuthoring.AuthoringParent = e;
                    beltTransformAuthoring.LocalPosition = localPosition + mainTransform.Position;
                    beltTransformAuthoring.LocalRotation = tr.ValueRO.Rotation;
                    beltTransformAuthoring.LocalScale = tr.ValueRO.LocalScale;
                    beltTransformAuthoring.RuntimeParent = e;
                }
            }
        }
        ecb.Playback(state.EntityManager);
        
        // Destroy belt if component / entity is fully removed!
        foreach (var e in SystemAPI.QueryBuilder()
                     .WithAll<BeltStillHereTag>().WithNone<BeltSpawnerTag>()
                     .Build().ToEntityArray(state.WorldUpdateAllocator))
        {
            state.EntityManager.DestroyEntity(SystemAPI.GetBuffer<LinkedEntityGroupFake>(e).Reinterpret<Entity>().AsNativeArray());
            SystemAPI.GetBuffer<LinkedEntityGroupFake>(e).Clear();
        }
        
        // Ensure the clean up component is fully cleaned!
        state.EntityManager.RemoveComponent<BeltStillHereTag>(
            SystemAPI.QueryBuilder().WithAll<BeltStillHereTag>().WithNone<BeltSpawnerTag>().Build());
    }
}