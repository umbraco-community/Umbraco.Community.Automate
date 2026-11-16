using Umbraco.Automate.Core.Settings;

namespace Umbraco.Community.Automate.Pushover.Tests.Fakes;

/// <summary>
/// Connection types need an IEditableModelResolver to construct, but validation never uses it.
/// Explicit members avoid repeating the interface's generic constraint.
/// </summary>
public sealed class UnusedModelResolver : IEditableModelResolver
{
    object IEditableModelResolver.ResolveModel(string alias, Type modelType, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
    TModel IEditableModelResolver.ResolveModel<TModel>(string alias, object? source, EditableModelSchema? schema) => throw new NotSupportedException();
}
