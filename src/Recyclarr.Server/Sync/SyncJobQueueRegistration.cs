using Quartz;

namespace Recyclarr.Server.Sync;

internal static class SyncJobQueueRegistration
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the Quartz scheduler that queues and runs sync jobs (ADR-024). The queue lives
        /// in memory: after a restart, startup marks unfinished jobs interrupted and they never run
        /// again, so a persisted queue would hold nothing worth keeping. One worker runs one job at
        /// a time. On shutdown the running job is canceled and the host waits for it to record its
        /// outcome.
        /// </summary>
        public void AddSyncJobQueue()
        {
            services.AddQuartz(q =>
            {
                q.UseInMemoryStore();
                q.UseDefaultThreadPool(maxConcurrency: 1);
                q.ConfigureScheduler(options =>
                    options.ShutdownJobInterruption = ShutdownJobInterruption.WhenWaitingForJobs
                );
            });
            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        }
    }
}
