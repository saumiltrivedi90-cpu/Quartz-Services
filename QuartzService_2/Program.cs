using Microsoft.AspNetCore.Mvc;
using Quartz;
using QuartzService_2.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    var jobKey = new JobKey("SampleJob-2");

    q.AddJob<SampleJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("SampleJob-2-trigger")
        .WithCronSchedule("0/30 * * * * ?")); // Default: every 30 sec
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        // Handle WebSocket communication here
    }
    else
    {
        await next();
    }
});

app.MapGet("/health", async (ISchedulerFactory schedulerFactory) =>
{
    var scheduler = await schedulerFactory.GetScheduler();
    return Results.Ok(new
    {
        SchedulerStatus = scheduler.InStandbyMode ? "Standby" : "Running",
        Jobs = await scheduler.GetCurrentlyExecutingJobs()
    });
});

app.MapPost("/start", async (ISchedulerFactory schedulerFactory) =>
{
    var scheduler = await schedulerFactory.GetScheduler();
    await scheduler.Start();
    return Results.Ok("Scheduler started");
});

app.MapPost("/stop", async (ISchedulerFactory schedulerFactory) =>
{
    var scheduler = await schedulerFactory.GetScheduler();
    await scheduler.Standby();
    return Results.Ok("Scheduler stopped");
});

app.MapPost("/update-cron", async (ISchedulerFactory schedulerFactory, [FromBody] string newCron) =>
{
    var scheduler = await schedulerFactory.GetScheduler();
    var triggerKey = new TriggerKey("SampleJob-2-trigger");

    var newTrigger = TriggerBuilder.Create()
        .WithIdentity(triggerKey)
        .WithCronSchedule(newCron)
        .ForJob("SampleJob-2")
        .Build();

    await scheduler.RescheduleJob(triggerKey, newTrigger);
    return Results.Ok($"Cron updated to: {newCron}");
});

app.MapGet("/current-cron", async (ISchedulerFactory schedulerFactory) =>
{
    var scheduler = await schedulerFactory.GetScheduler();
    var triggerKey = new TriggerKey("SampleJob-2-trigger");
    var trigger = await scheduler.GetTrigger(triggerKey) as ICronTrigger;
    if (trigger == null)
    {
        return Results.NotFound("Cron trigger not found.");
    }
    return Results.Ok(new { CronExpression = trigger.CronExpressionString });
});

app.Run();
