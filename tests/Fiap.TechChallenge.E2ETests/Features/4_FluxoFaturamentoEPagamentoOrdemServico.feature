# language: pt-BR
Funcionalidade: Faturamento, Orçamento e Pagamento da Ordem de Serviço (Billing Service)
  Como um cliente da oficina mecânica
  Eu quero receber o orçamento calculado pelo Billing Service e realizar o pagamento
  Para que minha Ordem de Serviço tenha o orçamento aprovado e seja liberada para execução

  Contexto:
    Dado que os microsserviços Core, OS, Execução e Billing estão operacionais
    E que o usuário autentica como Administrador no Core Service
    E que existe um cliente e um veículo cadastrados no catálogo

  Cenário: Geração de orçamento, emissão de boleto, confirmação via Webhook e liberação automática da OS
    Dado que existe uma Ordem de Serviço em fase de diagnóstico concluído com serviços e peças
    Quando o orçamento é solicitado no Billing Service utilizando o contrato compartilhado GerarOrcamentoRequest
    Então o orçamento é gravado no PostgreSQL do Billing Service com o valor total calculado corretamente
    E o evento OrcamentoGeradoEvent é publicado no SNS com os atributos canônicos
    Quando o cliente solicita a emissão de boleto no Billing Service via CriarBoletoRequest
    Então o pagamento é registrado com status "Pendente" no PostgreSQL do Billing Service
    Quando o Billing Service recebe a notificação de pagamento aprovado no endpoint de Webhook
    Então o status do pagamento no PostgreSQL do Billing Service é atualizado para "Aprovado"
    E o evento PagamentoConfirmadoEvent é consumido pelo OS Service atualizando a OS no DynamoDB para "ORCAMENTO_APROVADO"
    E o Execution Service recebe automaticamente a liberação via SQS gravando as tarefas com status "NA_FILA"

