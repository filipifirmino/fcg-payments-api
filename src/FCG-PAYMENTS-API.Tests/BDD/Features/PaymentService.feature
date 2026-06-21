Feature: PaymentService
  Como parte do sistema de pagamentos FCG
  Quero processar pagamentos de pedidos de jogos
  Para garantir que o fluxo de compra seja registrado corretamente

  Scenario: Processar pagamento aprovado
    Given um pedido válido com preço de 59.90
    When o serviço processa o pagamento com resultado aprovado
    Then o Payment deve ter status "Approved"
    And o Amount do Payment deve ser 59.90
    And o Reason do Payment deve ser nulo

  Scenario: Processar pagamento rejeitado
    Given um pedido válido com preço de 120.00
    When o serviço processa o pagamento com resultado rejeitado
    Then o Payment deve ter status "Rejected"
    And o Reason do Payment deve ser "Insufficient funds (simulated)"

  Scenario: Preservar OrderId no Payment
    Given um pedido com OrderId específico
    When o serviço processa o pagamento com resultado aprovado
    Then o Payment deve preservar o OrderId do pedido

  Scenario: Preservar UserId no Payment
    Given um pedido com UserId específico
    When o serviço processa o pagamento com resultado aprovado
    Then o Payment deve preservar o UserId do pedido

  Scenario: Preservar GameId no Payment
    Given um pedido com GameId específico
    When o serviço processa o pagamento com resultado aprovado
    Then o Payment deve preservar o GameId do pedido

  Scenario: Preservar GameTitle no Payment
    Given um pedido com GameTitle "Elden Ring"
    When o serviço processa o pagamento com resultado aprovado
    Then o Payment deve ter GameTitle "Elden Ring"

  Scenario: Preservar UserEmail no Payment
    Given um pedido com UserEmail "jogador@fcg.com"
    When o serviço processa o pagamento com resultado aprovado
    Then o Payment deve ter UserEmail "jogador@fcg.com"

  Scenario: Payment deve ter Id único
    Given um pedido válido com preço de 30.00
    When o serviço processa o pagamento com resultado aprovado
    Then o Payment deve ter um Id não vazio

  Scenario: Repositório deve ser chamado ao processar pagamento
    Given um pedido válido com preço de 45.00
    When o serviço processa o pagamento com resultado aprovado
    Then o repositório deve ter persistido o Payment exatamente uma vez

  Scenario: ProcessedAt deve ser próximo do momento atual
    Given um pedido válido com preço de 75.00
    When o serviço processa o pagamento com resultado aprovado
    Then o ProcessedAt do Payment deve ser próximo do momento atual

  Scenario: Dois pagamentos devem ter Ids diferentes
    Given dois pedidos válidos distintos
    When o serviço processa ambos os pagamentos com resultado aprovado
    Then os dois Payments devem ter Ids diferentes

  Scenario: Strategy de aprovação deve ser consultada ao processar
    Given um pedido válido com preço de 99.90
    When o serviço processa o pagamento com resultado aprovado
    Then a strategy de aprovação deve ter sido consultada exatamente uma vez
