using Microsoft.Extensions.Configuration;

namespace Fiap.TechChallenge.E2ETests.Support
{
    public class TestConfiguration
    {
        public static TestConfiguration Current { get; } = new();

        public IConfiguration Configuration { get; }

        public string CoreApiUrl { get; }
        public string OsApiUrl { get; }
        public string ExecutionApiUrl { get; }
        public string BillingApiUrl { get; }
        public string FlociUrl { get; }
        public string ExecutionDbConnectionString { get; }
        public string BillingDbConnectionString { get; }
        public string AdminLogin { get; }
        public string AdminSenha { get; }
        public string TableName { get; }
        public string EventosTopicArn { get; }

        public TestConfiguration()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            CoreApiUrl = Configuration["Urls:CoreApi"] ?? "http://localhost:8080";
            OsApiUrl = Configuration["Urls:OsApi"] ?? "http://localhost:8083";
            ExecutionApiUrl = Configuration["Urls:ExecutionApi"] ?? "http://localhost:8082";
            BillingApiUrl = Configuration["Urls:BillingApi"] 
                ?? Configuration["ServiceUrls:BillingApi"] 
                ?? "http://localhost:8081";
            FlociUrl = Configuration["Urls:Floci"] ?? "http://localhost:4566";
            ExecutionDbConnectionString = Configuration["ConnectionStrings:ExecutionDb"] 
                ?? "Host=localhost;Port=5433;Database=techchallenge_execution_db;Username=execution;Password=execution_local_pwd";
            BillingDbConnectionString = Configuration["ConnectionStrings:BillingDb"] 
                ?? "Host=localhost;Port=5434;Database=techchallenge_billing_db;Username=billing;Password=billing_password";

            AdminLogin = Configuration["Auth:AdminLogin"] ?? "admin@techchallenge.com";
            AdminSenha = Configuration["Auth:AdminSenha"] ?? "Admin@123";

            TableName = Configuration["Aws:TableName"] ?? "techchallenge-ordem-servico-local";
            EventosTopicArn = Configuration["Aws:EventosTopicArn"] ?? "arn:aws:sns:sa-east-1:000000000000:techchallenge-eventos-local";
        }
    }
}

