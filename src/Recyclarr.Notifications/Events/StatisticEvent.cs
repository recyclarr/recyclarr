// using MediatR;
//
// namespace Recyclarr.Notifications.Events;
//
// public record StatisticEvent(string Description, string Statistic) : IRequest;
//
// internal class StatisticEventHandler : IPresentableNotification, IRequestHandler<StatisticEvent>
// {
//     public string Category => "Statistics";
//     public string Render() => string.Join('\n', _events.Select(x => $"- {x.Description}: {x.Statistic}"));
//
//     private readonly List<StatisticEvent> _events = [];
//
//     public Task Handle(StatisticEvent request, CancellationToken cancellationToken)
//     {
//         _events.Add(request);
//         return Task.CompletedTask;
//     }
// }
