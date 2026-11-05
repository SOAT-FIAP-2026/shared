#!/usr/bin/env bash
set -euo pipefail

echo "🚀 Inicializando recursos AWS locais no Floci (SNS, SQS, DynamoDB, SSM)..."

ENDPOINT="${FLOCI_ENDPOINT:-http://localhost:4566}"
REGION="${AWS_DEFAULT_REGION:-sa-east-1}"
ACCOUNT_ID="000000000000"

auth_header() {
  local service="$1"
  echo "Authorization: AWS4-HMAC-SHA256 Credential=test/20260101/${REGION}/${service}/aws4_request, SignedHeaders=host, Signature=test"
}

# 1. Tabela DynamoDB (Single-Table do os-microservice)
echo "📦 [1/4] Criando tabela DynamoDB: techchallenge-ordem-servico-local..."
curl -sS -X POST "${ENDPOINT}/" \
  -H "Content-Type: application/x-amz-json-1.0" \
  -H "X-Amz-Target: DynamoDB_20120810.CreateTable" \
  -H "$(auth_header dynamodb)" \
  -d '{
    "TableName": "techchallenge-ordem-servico-local",
    "BillingMode": "PAY_PER_REQUEST",
    "AttributeDefinitions": [
      {"AttributeName": "PK", "AttributeType": "S"},
      {"AttributeName": "SK", "AttributeType": "S"},
      {"AttributeName": "GSI1PK", "AttributeType": "S"},
      {"AttributeName": "GSI1SK", "AttributeType": "S"}
    ],
    "KeySchema": [
      {"AttributeName": "PK", "KeyType": "HASH"},
      {"AttributeName": "SK", "KeyType": "RANGE"}
    ],
    "GlobalSecondaryIndexes": [
      {
        "IndexName": "GSI1",
        "KeySchema": [
          {"AttributeName": "GSI1PK", "KeyType": "HASH"},
          {"AttributeName": "GSI1SK", "KeyType": "RANGE"}
        ],
        "Projection": {"ProjectionType": "ALL"}
      }
    ],
    "StreamSpecification": {
      "StreamEnabled": true,
      "StreamViewType": "NEW_IMAGE"
    }
  }' >/dev/null || true

# 2. Tópico SNS Unificado de Eventos de Domínio
echo "📢 [2/4] Criando tópico SNS: techchallenge-eventos-local..."
TOPIC_ARN="arn:aws:sns:${REGION}:${ACCOUNT_ID}:techchallenge-eventos-local"
curl -sS -X POST "${ENDPOINT}/" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "$(auth_header sns)" \
  --data-urlencode "Action=CreateTopic" \
  --data-urlencode "Name=techchallenge-eventos-local" \
  --data-urlencode "Version=2010-03-31" >/dev/null

# 3. Filas SQS e DLQs por Serviço
echo "📫 [3/4] Criando filas SQS e DLQs..."
for QUEUE in \
  "techchallenge-ordem-servico-compensacao-dlq-local" \
  "techchallenge-ordem-servico-compensacao-local" \
  "techchallenge-os-eventos-dlq-local" \
  "techchallenge-os-eventos-local" \
  "techchallenge-execucao-eventos-dlq-local" \
  "techchallenge-execucao-eventos-local" \
  "techchallenge-billing-eventos-dlq-local" \
  "techchallenge-billing-eventos-local"
do
  curl -sS -X POST "${ENDPOINT}/" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -H "$(auth_header sqs)" \
    --data-urlencode "Action=CreateQueue" \
    --data-urlencode "QueueName=${QUEUE}" \
    --data-urlencode "Version=2012-11-05" >/dev/null
done

subscribe_queue_with_filter() {
  local queue_name="$1"
  local filter_json="$2"
  local queue_arn="arn:aws:sqs:${REGION}:${ACCOUNT_ID}:${queue_name}"

  local sub_xml
  sub_xml=$(curl -sS -X POST "${ENDPOINT}/" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -H "$(auth_header sns)" \
    --data-urlencode "Action=Subscribe" \
    --data-urlencode "TopicArn=${TOPIC_ARN}" \
    --data-urlencode "Protocol=sqs" \
    --data-urlencode "Endpoint=${queue_arn}" \
    --data-urlencode "Version=2010-03-31")

  local sub_arn
  sub_arn=$(echo "${sub_xml}" | sed -n 's:.*<SubscriptionArn>\(.*\)</SubscriptionArn>.*:\1:p')

  if [ -n "${sub_arn}" ]; then
    curl -sS -X POST "${ENDPOINT}/" \
      -H "Content-Type: application/x-www-form-urlencoded" \
      -H "$(auth_header sns)" \
      --data-urlencode "Action=SetSubscriptionAttributes" \
      --data-urlencode "SubscriptionArn=${sub_arn}" \
      --data-urlencode "AttributeName=FilterPolicy" \
      --data-urlencode "AttributeValue=${filter_json}" \
      --data-urlencode "Version=2010-03-31" >/dev/null || true
  fi
}

subscribe_queue_with_filter "techchallenge-os-eventos-local" \
  '{"tipoEvento":["DiagnosticoIniciadoEvent","DiagnosticoTecnicoConcluidoEvent","OrcamentoGeradoEvent","ProducaoIniciadaEvent","ProducaoFinalizadaEvent","ProducaoCanceladaEvent","PagamentoConfirmadoEvent","PagamentoRecusadoEvent","PagamentoRecusado","PagamentoCanceladoCompensacaoEvent","PagamentoEstornadoEvent","OrcamentoReprovado","OrdemServicoCancelada","OrdemServicoCanceladaEvent","ReservaEstoqueFalhou"]}'

subscribe_queue_with_filter "techchallenge-execucao-eventos-local" \
  '{"tipoEvento":["OrdemServicoAbertaEvent","OrdemServicoCriada","DiagnosticoRejeitadoEvent","OrdemServicoLiberadaParaExecucaoEvent","OrdemServicoCanceladaEvent","OrdemServicoCancelada"]}'

subscribe_queue_with_filter "techchallenge-billing-eventos-local" \
  '{"tipoEvento":["DiagnosticoTecnicoConcluidoEvent","ProducaoCanceladaEvent","ProducaoCancelada"]}'

# 4. SSM Parameter Store (Segredo JWT compartilhado)
echo "🔐 [4/4] Criando parâmetro SSM /techchallenge/prod/jwt_secret..."
curl -sS -X POST "${ENDPOINT}/" \
  -H "Content-Type: application/x-amz-json-1.1" \
  -H "X-Amz-Target: AmazonSSM.PutParameter" \
  -H "$(auth_header ssm)" \
  -d '{
    "Name": "/techchallenge/prod/jwt_secret",
    "Value": "techchallenge_jwt_secret_super_secure_key_1234567890!",
    "Type": "SecureString",
    "Overwrite": true
  }' >/dev/null || true

echo "✅ Recursos Floci inicializados com sucesso!"
