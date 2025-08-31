using Quartz;
using System;
using System.Threading.Tasks;

namespace QuartzService.Jobs
{
    public class SampleJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Quartz Service is working.");
            return Task.CompletedTask;
        }
    }
}