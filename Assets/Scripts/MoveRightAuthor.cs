using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public class MoveRightAuthor : MonoBehaviour
{
    [SerializeField] float metersPerSecond = 2f;
    
    private class Baker : Baker<MoveRightAuthor>
    {
        public override void Bake(MoveRightAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MoveRightData { metersPerSecond = authoring.metersPerSecond });
        }
    }
}

public struct MoveRightData : IComponentData
{
    public float metersPerSecond;
}

partial struct MoveRightSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (ltwRef, moveData) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveRightData>>())
        {
            ltwRef.ValueRW.Position.x += moveData.ValueRO.metersPerSecond * state.WorldUnmanaged.Time.DeltaTime;
        }
    }
}