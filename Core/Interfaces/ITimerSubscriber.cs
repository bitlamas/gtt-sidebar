using System;
using System.Threading.Tasks;

namespace gtt_sidebar.Core.Interfaces
{
    /// <summary>
    /// Interface for widgets that need periodic timer updates
    /// </summary>
    public interface ITimerSubscriber
    {
        /// <summary>
        /// How often this subscriber wants to be updated
        /// </summary>
        TimeSpan UpdateInterval { get; }

        /// <summary>
        /// Called when it's time for this subscriber to update
        /// </summary>
        Task OnTimerTickAsync();
    }
}