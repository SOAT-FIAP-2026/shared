# language: pt-BR
Funcionalidade: Compensação Transacional da Saga de Estoque
  Como sistema distribuído da oficina mecânica
  Quero garantir resiliência e integridade quando ocorrerem falhas de validação ou cancelamento
  Para evitar consistência divergente entre a Ordem de Serviço e o Estoque do Core

  Cenário: Tentativa de inclusão de peça inexistente com rejeição adequada
    Dado que os microsserviços Core e OS estão operacionais
    E que o usuário autentica como Administrador no Core Service
    E que o atendente possui uma Ordem de Serviço aberta
    Quando o atendente tenta incluir uma peça com identificador inexistente
    Então o sistema rejeita a operação com erro de validação
    E o status da Ordem de Serviço permanece inalterado

  Cenário: Compensação assíncrona na OS quando o pagamento é recusado ou cancelado no Billing Service
    Dado que o atendente possui uma Ordem de Serviço aguardando pagamento com peças reservadas no estoque
    Quando o pagamento é recusado no Billing Service disparando PagamentoCanceladoCompensacaoEvent no SNS
    Então a Lambda de compensação do OS Service consome o evento da fila SQS
    E o estoque da peça reservada é estornado no Core Service
    E o status da Ordem de Serviço no DynamoDB é atualizado para "CANCELADA"

