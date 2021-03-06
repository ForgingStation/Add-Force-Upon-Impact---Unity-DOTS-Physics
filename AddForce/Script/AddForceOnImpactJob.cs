using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Extensions;
using Unity.Physics;
using Unity.Transforms;
using Unity.Collections;

public class AddForceOnImpactJob : SystemBase
{
    public NativeArray<float3> impactParameters;
    private EndSimulationEntityCommandBufferSystem es_ecb;

    protected override void OnCreate()
    {
        impactParameters = new NativeArray<float3>(2,Allocator.Persistent);
        es_ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = es_ecb.CreateCommandBuffer();
        Entities.WithoutBurst().ForEach((Entity e, ref HaveContact hc, 
            ref SphereComponentData scd, ref Translation trans) =>
        {
            impactParameters[0] = new float3(scd.forceAppliedOnImpact, 0, 0);
            impactParameters[1] = trans.Value;
            //ecb.DestroyEntity(e);
        }).Run();

        var parallelECB = es_ecb.CreateCommandBuffer().ToConcurrent();
        NativeArray<float3> impactParametersForJob = impactParameters;
        float deltaTime = Time.DeltaTime;
        Entities
            .WithNone<ForceAppliedTag>()
            .WithReadOnly(impactParametersForJob)
            .ForEach((Entity e, int entityInQueryIndex, ref WallComponentData wcd, 
            ref PhysicsVelocity pv, ref PhysicsMass pm, ref Translation trans) =>
        {
            if (!impactParametersForJob[0].Equals(float3.zero))
            {
                wcd.forceImpluseTime = wcd.forceImpluseTime - deltaTime;
                float3 force = impactParametersForJob[0].x / (trans.Value - impactParametersForJob[1]);
                //float3 force = trans.Value - impactParametersForJob[1];
                //float3 force = impactParametersForJob[1] - trans.Value;
                //force = force * impactParametersForJob[0].x;
                ComponentExtensions.ApplyLinearImpulse(ref pv, pm, force);
                if (wcd.forceImpluseTime <= 0)
                {
                    parallelECB.AddComponent(entityInQueryIndex, e, new ForceAppliedTag { });
                }
            }
        }).ScheduleParallel();

        es_ecb.AddJobHandleForProducer(Dependency);
    }

    protected override void OnDestroy()
    {
        impactParameters.Dispose();
    }
}
public struct ForceAppliedTag : IComponentData { }
