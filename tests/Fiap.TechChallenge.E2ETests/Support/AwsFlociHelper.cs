using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using SnsMessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace Fiap.TechChallenge.E2ETests.Support
{
    public class AwsFlociHelper
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly IAmazonSimpleNotificationService _sns;
        private readonly IAmazonSQS _sqs;
        private readonly TestConfiguration _config;

        public AwsFlociHelper()
        {
            _config = TestConfiguration.Current;
            var credentials = new BasicAWSCredentials("test", "test");
            var region = "sa-east-1";

            _dynamoDb = new AmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig
            {
                ServiceURL = _config.FlociUrl,
                AuthenticationRegion = region
            });

            _sns = new AmazonSimpleNotificationServiceClient(credentials, new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = _config.FlociUrl,
                AuthenticationRegion = region
            });

            _sqs = new AmazonSQSClient(credentials, new AmazonSQSConfig
            {
                ServiceURL = _config.FlociUrl,
                AuthenticationRegion = region
            });
        }

        public async Task<bool> TabelaExiste(string nomeTabela)
        {
            try
            {
                var response = await _dynamoDb.ListTablesAsync();
                return response.TableNames.Contains(nomeTabela);
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, AttributeValue>?> ObterOrdemServicoDynamo(Guid osId)
        {
            try
            {
                var response = await _dynamoDb.GetItemAsync(new GetItemRequest
                {
                    TableName = _config.TableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = new AttributeValue { S = $"OS#{osId}" },
                        ["SK"] = new AttributeValue { S = "METADATA" }
                    }
                });

                return response.Item != null && response.Item.Count > 0 ? response.Item : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task PublicarEventoNoSns(string jsonCorpo, string tipoEvento, Guid osId)
        {
            await _sns.PublishAsync(new PublishRequest
            {
                TopicArn = _config.EventosTopicArn,
                Message = jsonCorpo,
                MessageAttributes = new Dictionary<string, SnsMessageAttributeValue>
                {
                    ["tipoEvento"] = new SnsMessageAttributeValue { DataType = "String", StringValue = tipoEvento },
                    ["origem"] = new SnsMessageAttributeValue { DataType = "String", StringValue = "techchallenge.e2e-tests" },
                    ["idOrdemServico"] = new SnsMessageAttributeValue { DataType = "String", StringValue = osId.ToString() }
                }
            });
        }

        public async Task<List<string>> ListarTopicosSns()
        {
            var response = await _sns.ListTopicsAsync();
            return response.Topics.Select(t => t.TopicArn).ToList();
        }

        public async Task<List<string>> ListarFilasSqs()
        {
            var response = await _sqs.ListQueuesAsync(new ListQueuesRequest());
            return response.QueueUrls;
        }
    }
}
