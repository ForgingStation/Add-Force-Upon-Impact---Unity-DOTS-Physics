using Unity.Entities;

[GenerateAuthoringComponent]
public struct SphereComponentData : IComponentData
{
    public float forceAppliedOnImpact;
}
