using Server.Helpers;
using Server.Interfaces;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<GlobalExceptionFilter>());
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IApiService, ApiService>();
builder.Services.AddSingleton<IWorkingWithJsonService, WorkingWithJsonService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
