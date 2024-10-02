namespace Recyclarr.Settings;

internal record Settings<T>(T Value) : ISettings<T>;
