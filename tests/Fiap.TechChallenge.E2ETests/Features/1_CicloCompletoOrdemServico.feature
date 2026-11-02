# language: pt-BR
Funcionalidade: Ciclo de Vida Completo da Ordem de Serviço
  Como um atendente e mecânico da oficina mecânica
  Quero abrir, diagnosticar, reservar estoque, executar e entregar uma Ordem de Serviço
  Para validar a integração ponta a ponta entre os microsserviços Core, OS e Execução

  Cenário: Abertura, Diagnóstico Técnico, Reserva de Estoque e Entrega da Ordem de Serviço
    Dado que os microsserviços Core, OS e Execução estão operacionais
    E que o usuário autentica como Administrador no Core Service
    E que existe um cliente e um veículo cadastrados no catálogo
    Quando o atendente solicita a abertura de uma nova Ordem de Serviço no OS Service
    Então a Ordem de Serviço é criada com sucesso com status "Recebida"
    E a Ordem de Serviço pode ser consultada no DynamoDB
    Quando o mecânico inclui itens de serviço e peças na Ordem de Serviço
    E o mecânico inicia o diagnóstico no OS Service
    Então a Ordem de Serviço passa para o status "Em Diagnóstico"
    Quando o mecânico finaliza o diagnóstico técnico
    Então a Ordem de Serviço avança para "Aguardando aprovação"
    Quando o cliente aprova o orçamento
    Então a Ordem de Serviço é liberada para execução
    Quando o mecânico inicia e finaliza as tarefas de execução
    Então a Ordem de Serviço atinge o status "Finalizada"
    Quando o cliente confirma a entrega do veículo
    Então a Ordem de Serviço atinge o status final "Entregue"

