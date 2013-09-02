﻿using NzbDrone.Common.Messaging.Tracking;

namespace NzbDrone.Common.Messaging
{
    /// <summary>
    ///   Enables loosely-coupled publication of events.
    /// </summary>
    public interface IMessageAggregator
    {
        void PublishEvent<TEvent>(TEvent @event) where TEvent : class,  IEvent;
        void PublishCommand<TCommand>(TCommand command) where TCommand : class, ICommand;
        void PublishCommand(string commandTypeName);
        TrackedCommand PublishCommandAsync<TCommand>(TCommand command) where TCommand : class, ICommand;
        TrackedCommand PublishCommandAsync(string commandTypeName);
    }
}