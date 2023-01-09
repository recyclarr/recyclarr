namespace Recyclarr.TrashLib.Services.System;

public interface IServiceInformation
{
    IObservable<Version?> Version { get; }
}
