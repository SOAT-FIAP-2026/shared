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
        public string FlociUrl { get; }
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
            FlociUrl = Configuration["Urls:Floci"] ?? "http://localhost:4566";

            AdminLogin = Configuration["Auth:AdminLogin"] ?? "admin@techchallenge.com";
            AdminSenha = Configuration["Auth:AdminSenha"] ?? "Admin@123";

            TableName = Configuration["Aws:TableName"] ?? "techchallenge-ordem-servico-local";
            EventosTopicArn = Configuration["Aws:EventosTopicArn"] ?? "arn:aws:sns:sa-east-1:000000000000:techchallenge-eventos-local";
        }
    }
}

