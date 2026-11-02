# language: pt-BR
Funcionalidade: Ciclo de Vida Completo da Ordem de Serviço
  Como um atendente e mecânico da oficina mecânica
  Quero abrir, diagnosticar, orçar, executar tarefas e entregar uma Ordem de Serviço
  Para validar a integração ponta a ponta e a persistência em múltiplos bancos de dados (DynamoDB e PostgreSQL)

  Cenário: Abertura, Diagnóstico Técnico, Orçamento, Execução e Entrega da Ordem de Serviço
    Dado que os microsserviços Core, OS e Execução estão operacionais
    E que o usuário autentica como Administrador no Core Service
    E que existe um cliente e um veículo cadastrados no catálogo
    Quando o atendente solicita a abertura de uma nova Ordem de Serviço no OS Service
    Então a Ordem de Serviço é criada com sucesso com status "Recebida"
    E a Ordem de Serviço é gravada fisicamente no DynamoDB
    E o evento de abertura da OS é integrado com o Execution Service
    E a Ordem de Produção é gravada no PostgreSQL do Execution Service com status "AGUARDANDO_DIAGNOSTICO"
    Quando o mecânico inicia o diagnóstico no Execution Service
    Então o diagnóstico técnico é gravado no PostgreSQL do Execution Service com status "EM_DIAGNOSTICO"
    Quando o mecânico finaliza o diagnóstico técnico no Execution Service
    Então o status no PostgreSQL do Execution Service é atualizado para "DIAGNOSTICO_CONCLUIDO"
    Quando o mecânico inclui itens de serviço e peças na Ordem de Serviço
    E o cliente aprova o orçamento
    Então a Ordem de Serviço é atualizada no DynamoDB com orçamento aprovado
    E o Execution Service recebe a liberação e grava as tarefas no PostgreSQL com status "NA_FILA"
    Quando o mecânico inicia e finaliza as tarefas de execução no Execution Service
    Então a Ordem de Produção é concluída no PostgreSQL do Execution Service
    E a Ordem de Serviço no OS Service atinge o status "Finalizada"
    Quando o cliente confirma a entrega do veículo
    Então a Ordem de Serviço atinge o status final "Entregue" no DynamoDB
