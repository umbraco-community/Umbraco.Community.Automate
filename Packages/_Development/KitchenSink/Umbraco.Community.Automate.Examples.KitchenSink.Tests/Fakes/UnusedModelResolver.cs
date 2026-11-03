using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Examples.KitchenSink.Tests.Fakes;

/// <summary>
/// Connection types and triggers need an IEditableModelResolver to construct, but neither
/// ValidateAsync nor MapEvent/CanHandle in these tests uses it. Explicit members avoid repeating
/// the interface's generic constraint.
/// </summary>
public sealed class UnusedModelResolver : IEditableModelResolver
{
    object IEditableModelResolver.ResolveModel(string alias, Type modelType, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
    TModel IEditableModelResolver.ResolveModel<TModel>(string alias, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
}
