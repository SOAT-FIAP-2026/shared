#!/usr/bin/env bash
set -euo pipefail

echo "🚀 Inicializando recursos AWS locais no LocalStack (SNS, SQS, DynamoDB, SSM)..."

REGION="sa-east-1"
STAGE="local"

# 1. Tabela DynamoDB (Single-Table do os-microservice)
awslocal dynamodb create-table \
  --region "$REGION" \
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
  ]" || true

# 2. Tópico SNS Central da Saga Coreografada
TOPIC_ARN=$(awslocal sns create-topic \
  --region "$REGION" \
  --name "techchallenge-eventos-${STAGE}" \
  --query 'TopicArn' --output text)

echo "✅ Tópico SNS criado: $TOPIC_ARN"

# 3. Filas SQS por Microsserviço
for QUEUE in \
  "techchallenge-ordem-servico-compensacao-${STAGE}" \
  "techchallenge-os-eventos-${STAGE}" \
  "techchallenge-execucao-eventos-${STAGE}" \
  "techchallenge-billing-eventos-${STAGE}"; do
  awslocal sqs create-queue --region "$REGION" --queue-name "${QUEUE}-dlq" >/dev/null
  awslocal sqs create-queue --region "$REGION" --queue-name "$QUEUE" >/dev/null
  echo "✅ Fila SQS criada: $QUEUE (+ DLQ)"
done

# 4. Assinaturas SNS -> SQS com FilterPolicy por tipoEvento

# 4.1. Fila do os-microservice
OS_QUEUE_ARN="arn:aws:sqs:${REGION}:000000000000:techchallenge-os-eventos-${STAGE}"
awslocal sns subscribe \
  --region "$REGION" \
  --topic-arn "$TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$OS_QUEUE_ARN" \
  --attributes '{
    "FilterPolicy": "{\"tipoEvento\":[\"DiagnosticoIniciadoEvent\",\"DiagnosticoTecnicoConcluidoEvent\",\"ProducaoIniciadaEvent\",\"ProducaoFinalizadaEvent\",\"ProducaoCanceladaEvent\",\"PagamentoConfirmadoEvent\",\"PagamentoRecusadoEvent\",\"PagamentoRecusado\",\"PagamentoCanceladoCompensacaoEvent\",\"PagamentoEstornadoEvent\",\"OrcamentoReprovado\",\"OrcamentoReprovadoEvent\",\"OrdemServicoCancelada\",\"OrdemServicoCanceladaEvent\",\"ReservaEstoqueFalhou\",\"ReservaEstoqueFalhouEvent\"]}"
  }' >/dev/null

# 4.2. Fila do execution-service
EXEC_QUEUE_ARN="arn:aws:sqs:${REGION}:000000000000:techchallenge-execucao-eventos-${STAGE}"
awslocal sns subscribe \
  --region "$REGION" \
  --topic-arn "$TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$EXEC_QUEUE_ARN" \
  --attributes '{
    "FilterPolicy": "{\"tipoEvento\":[\"OrdemServicoAbertaEvent\",\"OrdemServicoCriada\",\"DiagnosticoRejeitadoEvent\",\"OrdemServicoLiberadaParaExecucaoEvent\",\"OrdemServicoCanceladaEvent\",\"OrdemServicoCancelada\"]}"
  }' >/dev/null

# 4.3. Fila do billing-service
BILL_QUEUE_ARN="arn:aws:sqs:${REGION}:000000000000:techchallenge-billing-eventos-${STAGE}"
awslocal sns subscribe \
  --region "$REGION" \
  --topic-arn "$TOPIC_ARN" \
  --protocol sqs \
  --notification-endpoint "$BILL_QUEUE_ARN" \
  --attributes '{
    "FilterPolicy": "{\"tipoEvento\":[\"DiagnosticoFinalizadoEvent\",\"ProducaoCanceladaEvent\",\"OrdemServicoCanceladaEvent\",\"OrdemServicoCancelada\"]}"
  }' >/dev/null

# 5. Parâmetro SSM de Segredo JWT Compartilhado
awslocal ssm put-parameter \
  --region "$REGION" \
  --name "/techchallenge/prod/jwt_secret" \
  --value "techchallenge_jwt_secret_super_secure_key_1234567890!" \
  --type "SecureString" \
  --overwrite >/dev/null

echo "🎉 Recursos AWS locais prontos no LocalStack!"

