using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Burst;
using Unity.Jobs;

public class SphereCollisionJob : SystemBase
{
    public BuildPhysicsWorld bpw;
    public StepPhysicsWorld spw;
    private EndSimulationEntityCommandBufferSystem es_ecb_Job;

    protected override void OnCreate()
    {
        es_ecb_Job = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        bpw = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
        spw = World.DefaultGameObjectInjectionWorld.GetExistingSystem<StepPhysicsWorld>();
    }

    protected override void OnUpdate()
    {
        JobHandle jh = new ImpactJob()
        {
            groundTag = GetComponentDataFromEntity<GroundComponentData>(),
            sphereTag = GetComponentDataFromEntity<SphereComponentData>(),
            ecb = es_ecb_Job.CreateCommandBuffer()
        }.Schedule(spw.Simulation, ref bpw.PhysicsWorld, Dependency);

        es_ecb_Job.AddJobHandleForProducer(jh);
    }

    [BurstCompile]
    public struct ImpactJob : ICollisionEventsJob
    {
        public ComponentDataFromEntity<GroundComponentData> groundTag;
        public ComponentDataFromEntity<SphereComponentData> sphereTag;
        public EntityCommandBuffer ecb;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.Entities.EntityA;
            Entity entityB = collisionEvent.Entities.EntityB;
            if (entityA != Entity.Null && entityB != Entity.Null)
            {
                bool isBodyASphere = sphereTag.Exists(entityA);
                bool isBodyBSphere = sphereTag.Exists(entityB);
                bool isBodyAGround = groundTag.Exists(entityA);
                bool isBodyBGround = groundTag.Exists(entityB);

                if (isBodyASphere && isBodyBGround)
                {
                    ecb.AddComponent(entityA, new HaveContact { });
                }
                if (isBodyAGround && isBodyBSphere)
                {
                    ecb.AddComponent(entityB, new HaveContact { });
                }
            }
        }
    }
}

public struct HaveContact : IComponentData { }
