using Autofac;
using Autofac.Extras.Ordering;
using FluentValidation;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Pipelines.Plan.Components;

namespace Recyclarr.Pipelines;

internal class PipelineAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ConfiguredCustomFormatProvider>().InstancePerLifetimeScope();

        RegisterPlan(builder);
    }

    private static void RegisterPlan(ContainerBuilder builder)
    {
        builder.RegisterType<PlanBuilder>();
        builder.RegisterType<ExplicitCfGroupValidator>().As<IValidator<CustomFormatGroupConfig>>();

        // ORDER HERE IS IMPORTANT!
        // CF must run before QP (QP references CFs from plan)
        builder
            .RegisterTypes(
                typeof(CustomFormatPlanComponent),
                typeof(QualityProfilePlanComponent),
                typeof(QualitySizePlanComponent),
                typeof(SonarrMediaNamingPlanComponent),
                typeof(RadarrMediaNamingPlanComponent),
                typeof(MediaManagementPlanComponent)
            )
            .As<IPlanComponent>()
            .OrderByRegistration();
    }
}
