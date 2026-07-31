using Microsoft.AspNetCore.HttpOverrides;
using MusicRoom.Hubs;
using MusicRoom.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

builder.Services.AddHttpClient<IYouTubeMetadataService, YouTubeMetadataService>();
builder.Services.AddSingleton<IRoomService, RoomService>();
builder.Services.AddSingleton<PlaybackService>();
builder.Services.AddHostedService<RoomBackgroundService>();

var app = builder.Build();

// Required behind Azure App Service's reverse proxy so SignalR sees the real
// scheme/host and WebSocket upgrades negotiate correctly.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapHub<MusicHub>("/musichub");
app.MapFallbackToPage("/_Host");

app.Run();

// Exposed so the test project can spin this app up with WebApplicationFactory later if desired.
public partial class Program { }
