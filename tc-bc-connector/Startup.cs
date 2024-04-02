namespace Com.Tradecloud1.BCconnector;

using Com.Tradecloud1.BCconnector.BC.Client;
using Com.Tradecloud1.BCconnector.MS;
using Com.Tradecloud1.BCconnector.TC.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public IConfiguration Configuration { get; }

    private SubscriptionClient sc;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddAzureWebAppDiagnostics();
        });
        services.AddControllers().AddNewtonsoftJson();
        services.AddSingleton<OAuthClient>();
        services.AddSingleton<Config>();
        services.AddSingleton<SubscriptionClient>();
        services.AddSingleton<PurchaseOrderClient>();
        services.AddSingleton<AuthClient>();
        services.AddSingleton<SingleDeliveryOrderClient>();
        services.AddSingleton<PurchaseOrderResponseClient>();
    }

    public async void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime alt, SubscriptionClient sc)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        this.sc = sc;
        alt.ApplicationStopping.Register(Stopping);
        await sc.EnsureSubscription();
    }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private void Stopping()
    {
        sc.Unsubscribe().GetAwaiter().GetResult();
    }
}
