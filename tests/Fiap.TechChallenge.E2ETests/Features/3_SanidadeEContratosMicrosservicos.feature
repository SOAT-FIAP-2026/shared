# language: pt-BR
Funcionalidade: Sanidade e Contratos dos Microsserviços
  Como operador de infraestrutura
  Quero verificar a saúde de todos os serviços e componentes de mensageria
  Para assegurar a disponibilidade da arquitetura distribuída

  Cenário: Todos os microsserviços e emulador Floci respondem saudáveis
    Quando consulto os endpoints de health dos serviços Core, OS, Execution e Billing
    Então todos os serviços retornam status saudável
    E os recursos no Floci (DynamoDB, tópicos SNS e filas SQS) estão acessíveis

