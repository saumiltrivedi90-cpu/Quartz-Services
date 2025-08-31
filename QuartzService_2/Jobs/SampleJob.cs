using Quartz;

namespace QuartzService_2.Jobs
{
    public class SampleJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Quartz Service - 2 is working.");
            return Task.CompletedTask;
        }
    }
}
