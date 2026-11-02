#!/bin/sh
set -eu

echo "🚀 Inicializando recursos AWS locais no Floci (SNS, SQS, DynamoDB, SSM)..."

REGION="sa-east-1"
STAGE="local"
ENDPOINT="http://localhost:4566"

export AWS_ACCESS_KEY_ID="${AWS_ACCESS_KEY_ID:-test}"
export AWS_SECRET_ACCESS_KEY="${AWS_SECRET_ACCESS_KEY:-test}"
export AWS_DEFAULT_REGION="$REGION"

if command -v awslocal >/dev/null 2>&1; then
  AWS_CLI="awslocal --region $REGION"
else
  AWS_CLI="aws --endpoint-url=$ENDPOINT --region $REGION"
fi

# 1. Tabela DynamoDB (Single-Table do os-microservice)
$AWS_CLI dynamodb create-table \
  --table-name "techchallenge-ordem-servico-${STAGE}" \
  --attribute-definitions \
      AttributeName=PK,AttributeType=S \
      AttributeName=SK,AttributeType=S \
      AttributeName=GSI1PK,AttributeType=S \
      AttributeName=GSI1SK,AttributeType=S \
  --key-schema AttributeName=PK,KeyType=HASH AttributeName=SK,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --global-secondary-indexes "[
    {
      \"IndexName\": \"GSI1\",
      \"KeySchema\": [
        {\"AttributeName\": \"GSI1PK\", \"KeyType\": \"HASH\"},
        {\"AttributeName\": \"GSI1SK\", \"KeyType\": \"RANGE\"}
      ],
      \"Projection\": {\"ProjectionType\": \"ALL\"}
    }
  ]" >/dev/null 2>&1 || true

# 2. Tópico SNS Central da Saga Coreografada
TOPIC_ARN=$($AWS_CLI sns create-topic \
  --name "techchallenge-eventos-${STAGE}" \
  --query 'TopicArn' --output text)

echo "✅ Tópico SNS criado: $TOPIC_ARN"

# 3. Filas SQS por Microsserviço
for QUEUE in \
  "techchallenge-ordem-servico-compensacao-${STAGE}" \
  "techchallenge-os-eventos-${STAGE}" \
  "techchallenge-execucao-eventos-${STAGE}" \
  "techchallenge-billing-eventos-${STAGE}"; do
  $AWS_CLI sqs create-queue --queue-name "${QUEUE}-dlq" >/dev/null
  $AWS_CLI sqs create-queue --queue-name "$QUEUE" >/dev/null
  echo "✅ Fila SQS criada: $QUEUE (+ DLQ)"
done

# 4. Assinaturas SNS -> SQS com FilterPolicy por tipoEvento

# 4.1. Fila do os-microservice
OS_QUEUE_ARN="arn:aws:sqs:${REGION}:000000000000:techchallenge-os-eventos-${STAGE}"
$AWS_CLI sns subscribe \
  --topic-arn "$TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$OS_QUEUE_ARN" \
  --attributes '{
    "FilterPolicy": "{\"tipoEvento\":[\"DiagnosticoIniciadoEvent\",\"DiagnosticoTecnicoConcluidoEvent\",\"ProducaoIniciadaEvent\",\"ProducaoFinalizadaEvent\",\"ProducaoCanceladaEvent\",\"PagamentoConfirmadoEvent\",\"PagamentoRecusadoEvent\",\"PagamentoRecusado\",\"PagamentoCanceladoCompensacaoEvent\",\"PagamentoEstornadoEvent\",\"OrcamentoReprovado\",\"OrcamentoReprovadoEvent\",\"OrdemServicoCancelada\",\"OrdemServicoCanceladaEvent\",\"ReservaEstoqueFalhou\",\"ReservaEstoqueFalhouEvent\"]}"
  }' >/dev/null

# 4.2. Fila do execution-service
EXEC_QUEUE_ARN="arn:aws:sqs:${REGION}:000000000000:techchallenge-execucao-eventos-${STAGE}"
$AWS_CLI sns subscribe \
  --topic-arn "$TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$EXEC_QUEUE_ARN" \
  --attributes '{
    "FilterPolicy": "{\"tipoEvento\":[\"OrdemServicoAbertaEvent\",\"OrdemServicoCriada\",\"DiagnosticoRejeitadoEvent\",\"OrdemServicoLiberadaParaExecucaoEvent\",\"OrdemServicoCanceladaEvent\",\"OrdemServicoCancelada\"]}"
  }' >/dev/null

# 4.3. Fila do billing-service
BILL_QUEUE_ARN="arn:aws:sqs:${REGION}:000000000000:techchallenge-billing-eventos-${STAGE}"
$AWS_CLI sns subscribe \
  --topic-arn "$TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$BILL_QUEUE_ARN" \
  --attributes '{
    "FilterPolicy": "{\"tipoEvento\":[\"DiagnosticoFinalizadoEvent\",\"ProducaoCanceladaEvent\",\"OrdemServicoCanceladaEvent\",\"OrdemServicoCancelada\"]}"
  }' >/dev/null

# 5. Parâmetro SSM de Segredo JWT Compartilhado
$AWS_CLI ssm put-parameter \
  --name "/techchallenge/prod/jwt_secret" \
  --value "techchallenge_jwt_secret_super_secure_key_1234567890!" \
  --type "SecureString" \
  --overwrite >/dev/null

echo "🎉 Recursos AWS locais prontos no Floci!"

