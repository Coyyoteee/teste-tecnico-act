# AGENTS.md

## 1. Contexto do projeto

Este repositório implementa um desafio técnico para controle de movimentações de uma conta empresarial.

A aplicação deve permitir:

- registrar entradas de valores;
- registrar saídas de valores;
- consultar o saldo disponível;
- consultar o histórico de movimentações;
- garantir consistência dos registros;
- impedir que o saldo fique negativo.

A solução deve priorizar:

- funcionamento correto;
- clareza e organização;
- qualidade dos testes;
- coerência das decisões técnicas;
- simplicidade;
- facilidade de manutenção e explicação.

Princípio geral:

> Preferir a solução mais simples que preserve correção, testabilidade, consistência e boa separação de responsabilidades.

Não adicionar complexidade arquitetural sem uma necessidade concreta.

---

# 2. Stack

## Backend

- .NET 10
- ASP.NET Core Web API
- Controllers / MVC
- SOLID de forma pragmática
- TDD
- xUnit
- OpenAPI

## Frontend

- React
- TypeScript
- Vite
- CSS Modules
- Radix UI Primitives
- interface minimalista
- sem Tailwind CSS
- sem biblioteca global de estado inicialmente
- consumo exclusivo da API HTTP

## Persistência

- sem banco de dados;
- arquivo JSON local;
- movimentações como fonte de verdade;
- saldo derivado das movimentações;
- repository abstraindo a persistência.

## Organização e deploy

- um único repositório Git;
- frontend e backend no mesmo repositório;
- desenvolvimento com Vite e ASP.NET Core em processos separados;
- produção com frontend compilado e servido pelo ASP.NET Core;
- uma única unidade de deploy.

---

# 3. Arquitetura

Usar uma arquitetura MVC/API enxuta:

```text
HTTP
 ↓
Controller
 ↓
Service
 ↓
Domain
 ↓
Repository
```

Não implementar Clean Architecture completa.

A separação entre camadas deve existir apenas onde traga clareza real e baixo acoplamento.

## Controllers

Responsabilidades:

- receber requisições HTTP;
- validar formato básico;
- chamar services;
- produzir respostas HTTP.

Não devem:

- calcular saldo;
- acessar arquivos;
- implementar regras de negócio;
- verificar saldo insuficiente diretamente;
- conter lógica de persistência.

## Services

Responsáveis por coordenar casos de uso:

```text
CreateMovement
GetBalance
GetHistory
```

Devem coordenar domínio e repository sem duplicar regras de negócio.

## Domain

Responsável pelas invariantes:

- valores devem ser maiores que zero;
- crédito aumenta saldo;
- débito reduz saldo;
- débito nunca pode gerar saldo negativo.

O domínio deve poder ser testado sem HTTP, filesystem ou ASP.NET Core.

## Repository

Responsável exclusivamente pela persistência.

```text
IMovementRepository
        ↑
JsonMovementRepository
```

---

# 4. O que evitar

Não adicionar sem necessidade objetiva:

- MediatR;
- CQRS;
- AutoMapper;
- Entity Framework;
- Unit of Work;
- Event Bus;
- Domain Events;
- microservices;
- generic repositories;
- Redux;
- Zustand;
- Tailwind CSS;
- design system completo;
- abstrações prematuras;
- múltiplos projetos ou assemblies apenas para simular Clean Architecture.

Se alguma dessas abordagens for introduzida posteriormente, deve existir justificativa técnica concreta.

---

# 5. Estrutura sugerida

```text
/
├── src/
│   ├── Backend/
│   │   ├── Controllers/
│   │   │   ├── MovementsController.cs
│   │   │   └── BalanceController.cs
│   │   │
│   │   ├── Domain/
│   │   │   ├── Account.cs
│   │   │   ├── Movement.cs
│   │   │   └── MovementType.cs
│   │   │
│   │   ├── Services/
│   │   │   ├── IAccountService.cs
│   │   │   └── AccountService.cs
│   │   │
│   │   ├── Persistence/
│   │   │   ├── IMovementRepository.cs
│   │   │   └── JsonMovementRepository.cs
│   │   │
│   │   ├── Contracts/
│   │   │   ├── Requests/
│   │   │   └── Responses/
│   │   │
│   │   ├── Exceptions/
│   │   ├── wwwroot/
│   │   ├── Program.cs
│   │   └── Challenge.Api.csproj
│   │
│   └── Frontend/
│       ├── src/
│       │   ├── components/
│       │   ├── features/
│       │   ├── services/
│       │   ├── styles/
│       │   ├── App.tsx
│       │   └── main.tsx
│       ├── public/
│       ├── package.json
│       ├── vite.config.ts
│       └── tsconfig.json
│
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
│
├── docs/
│   └── openapi.yaml
│
├── data/
│   └── movements.json
│
├── Challenge.sln
├── README.md
└── AGENTS.md
```

O repositório deve permanecer único.

Não separar frontend e backend em repositórios distintos.

---

# 6. API First

O contrato da API deve ser definido primeiro em:

```text
docs/openapi.yaml
```

Esse arquivo é a referência para backend e frontend.

Endpoints mínimos:

```http
POST /api/v1/movements
GET  /api/v1/movements
GET  /api/v1/balance
```

## POST /api/v1/movements

Exemplo:

```json
{
  "type": "credit",
  "amount": 100.00
}
```

Tipos válidos:

```text
credit
debit
```

Regras:

- `amount > 0`;
- `credit` adiciona saldo;
- `debit` reduz saldo;
- débito não pode deixar saldo negativo.

Respostas esperadas:

```text
201 Created
400 Bad Request
409 Conflict
```

Usar `ProblemDetails` para erros HTTP.

## GET /api/v1/balance

Exemplo:

```json
{
  "balance": 150.00
}
```

## GET /api/v1/movements

Retornar o histórico de movimentações.

Preferir ordem cronológica inversa, salvo decisão posterior documentada.

---

# 7. Domínio

## Movement

Uma movimentação deve possuir pelo menos:

```text
Id
Type
Amount
OccurredAt
```

Usar:

```csharp
decimal
```

para valores monetários.

Não utilizar `float` ou `double`.

Usar preferencialmente:

```csharp
DateTimeOffset
```

com persistência em UTC.

## Account

`Account` concentra as regras da conta.

Responsabilidades:

- calcular saldo;
- adicionar crédito;
- realizar débito;
- impedir débito com saldo insuficiente;
- impedir valores inválidos.

Interface conceitual:

```csharp
account.Deposit(amount);
account.Withdraw(amount);
```

---

# 8. Saldo

O saldo não deve ser persistido como uma segunda fonte de verdade.

Ele deve ser derivado das movimentações:

```text
saldo =
    soma dos créditos
    -
    soma dos débitos
```

As movimentações são a fonte de verdade.

Não persistir simultaneamente `Balance` e `Movements` como estados independentes.

---

# 9. Persistência JSON

Implementar:

```text
IMovementRepository
        ↑
JsonMovementRepository
```

Interface mínima sugerida:

```csharp
public interface IMovementRepository
{
    Task<IReadOnlyCollection<Movement>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Movement movement,
        CancellationToken cancellationToken = default);
}
```

Não criar repository genérico.

O contrato pode evoluir apenas se os testes ou casos de uso demonstrarem necessidade.

---

# 10. Consistência e concorrência

A aplicação deve impedir saldo negativo inclusive sob requisições simultâneas.

Cenário crítico:

```text
saldo inicial = 100

requisição A -> débito 80
requisição B -> débito 80
```

Resultado correto:

```text
uma requisição é aceita
uma requisição é rejeitada
saldo final = 20
```

Nunca:

```text
saldo final = -60
```

## Estratégia

Como a aplicação usa arquivo local e uma única instância, usar sincronização em processo, por exemplo:

```csharp
SemaphoreSlim
```

A região crítica deve abranger:

```text
lock
 ↓
ler movimentações
 ↓
reconstruir estado da conta
 ↓
validar operação
 ↓
adicionar movimentação
 ↓
persistir
 ↓
unlock
```

Não proteger apenas a escrita.

A validação do saldo e a persistência precisam ocorrer dentro da mesma região crítica.

---

# 11. Escrita segura

Evitar sobrescrever diretamente o arquivo principal.

Preferir:

```text
serializar
 ↓
gravar arquivo temporário
 ↓
substituir arquivo principal
```

A solução é intencionalmente voltada para uma única instância.

Em produção, a evolução natural seria um banco de dados transacional.

---

# 12. Tratamento de erros

Preferir tratamento centralizado.

Mapeamento sugerido:

```text
InvalidAmountException
    -> 400 Bad Request

InsufficientFundsException
    -> 409 Conflict
```

Usar:

```text
application/problem+json
```

Não expor stack traces nem detalhes internos.

---

# 13. TDD

O backend deve seguir TDD sempre que razoável:

```text
Red
 ↓
Green
 ↓
Refactor
```

Para regras relevantes:

1. escrever teste que falha;
2. implementar apenas o necessário;
3. refatorar mantendo os testes verdes.

Priorizar testes de comportamento, não de detalhes internos.

---

# 14. Testes unitários

Cobrir ao menos:

```text
crédito positivo aumenta saldo
débito válido reduz saldo
crédito zero é rejeitado
crédito negativo é rejeitado
débito zero é rejeitado
débito negativo é rejeitado
débito maior que saldo é rejeitado
débito igual ao saldo é permitido
saldo é calculado corretamente a partir do histórico
```

Não perseguir 100% de cobertura apenas como métrica.

---

# 15. Testes de serviço

Testar `AccountService` isoladamente quando isso trouxer clareza.

Usar fake ou mock de repository conforme necessário.

Cobrir:

- criação de crédito;
- criação de débito;
- saldo insuficiente;
- leitura de saldo;
- histórico.

Evitar mocks em excesso.

---

# 16. Testes de integração

Usar preferencialmente:

```text
WebApplicationFactory
```

Cobrir:

```text
POST /api/v1/movements
GET /api/v1/balance
GET /api/v1/movements
```

Criar também teste de concorrência:

```text
Given saldo = 100

When:
    débito 80
    débito 80

executados simultaneamente

Then:
    apenas um débito é confirmado
    saldo final = 20
```

Esse teste é obrigatório por validar diretamente a consistência da solução.

---

# 17. Frontend

O frontend deve ser deliberadamente simples.

Objetivos:

- mostrar saldo;
- registrar entrada;
- registrar saída;
- mostrar histórico;
- demonstrar consumo correto da API.

Sugestão de tela única:

```text
┌─────────────────────────────────┐
│ Saldo disponível                │
│ R$ 1.250,00                     │
├─────────────────────────────────┤
│ Nova movimentação               │
│                                 │
│ [ Entrada | Saída ]             │
│ Valor: [____________]           │
│                  [ Registrar ]  │
├─────────────────────────────────┤
│ Histórico                       │
│                                 │
│ + R$ 500,00     25/08 14:00     │
│ - R$ 100,00     25/08 13:20     │
└─────────────────────────────────┘
```

## Diretrizes

Preferir:

- HTML semântico;
- componentes pequenos;
- estado local;
- hooks do React;
- CSS Modules;
- Radix UI apenas quando houver comportamento complexo.

Evitar:

- Redux;
- Zustand;
- Tailwind;
- styled-components;
- CSS-in-JS;
- design system próprio;
- animações elaboradas;
- abstrações prematuras.

---

# 18. Radix UI

Usar Radix UI como biblioteca de primitives acessíveis e sem estilo.

Radix não deve substituir HTML nativo quando HTML nativo resolver adequadamente.

## Usar HTML nativo para

```text
button
input
form
label
fieldset
table
```

quando não houver necessidade de comportamento adicional.

## Usar Radix para componentes como

- Dialog;
- Select;
- Tooltip;
- Dropdown Menu;
- Tabs;
- Popover;
- componentes que exijam gerenciamento de foco;
- componentes que exijam navegação por teclado;
- overlays e interações acessíveis mais complexas.

Não transformar cada elemento simples em primitive.

O objetivo é usar Radix para comportamento e acessibilidade, enquanto todo o estilo visual permanece sob controle da aplicação.

---

# 19. CSS Modules

Todo estilo específico de componente deve usar CSS Modules.

Exemplo:

```text
MovementForm.tsx
MovementForm.module.css
```

Uso:

```tsx
import styles from './MovementForm.module.css'

export function MovementForm() {
  return (
    <form className={styles.form}>
      ...
    </form>
  )
}
```

Não usar Tailwind CSS.

Não adicionar CSS-in-JS.

Estilos globais devem ser limitados a:

- reset/base;
- tokens;
- tipografia global;
- `body`;
- `:root`.

---

# 20. Design tokens e paleta

A paleta base do projeto é:

```css
:root {
  --background-primary: #002147;

  --text-primary: #FFF5EE;
  --text-secondary: #BEBFC5;

  --accent-primary: #F05A24;

  --status-success: #2ECC71;
  --status-error: #FF4D6D;
  --status-warning: #E6A817;
}
```

Essas cores são a base visual oficial do projeto.

Não alterar os valores sem decisão explícita.

## Uso semântico

```text
--background-primary
    fundo principal da aplicação

--text-primary
    conteúdo textual principal

--text-secondary
    textos auxiliares e informações de menor hierarquia

--accent-primary
    ações primárias, destaques e elementos interativos relevantes

--status-success
    sucesso, crédito e feedback positivo

--status-error
    erro, débito ou feedback destrutivo quando semanticamente apropriado

--status-warning
    avisos e estados de atenção
```

Não depender apenas de cor para comunicar estado.

Sucesso, erro e aviso devem também possuir texto, ícone, label ou outro sinal acessível quando necessário.

---

# 21. Tokens adicionais

Se a implementação exigir cores adicionais, derivar novos tokens de forma centralizada.

Exemplos aceitáveis:

```css
:root {
  --background-secondary: #082B52;
  --background-elevated: #0D345E;

  --border-primary: #31506F;

  --accent-hover: #FF6A32;
}
```

Esses tokens são sugestões e só devem ser adicionados quando houver uso concreto.

Não espalhar valores HEX arbitrários pelos CSS Modules.

Preferir:

```css
.card {
  background: var(--background-secondary);
}
```

em vez de:

```css
.card {
  background: #082B52;
}
```

---

# 22. Acessibilidade visual

A interface deve preservar contraste adequado.

Diretrizes:

- texto principal sempre legível sobre o fundo;
- não usar apenas cor para indicar crédito ou débito;
- estados de foco devem ser visíveis;
- elementos interativos devem funcionar por teclado;
- labels devem estar associados aos campos;
- mensagens de erro devem ser semanticamente identificáveis;
- respeitar comportamento acessível fornecido pelo Radix.

Não remover outline de foco sem fornecer uma alternativa claramente visível.

---

# 23. Limites entre frontend e backend

O frontend nunca é fonte de verdade das regras de negócio.

Mesmo que o cliente impeça visualmente um débito, o backend deve validar novamente.

Frontend:

```text
input
feedback visual
requisição HTTP
renderização
```

Backend:

```text
validação de negócio
consistência
saldo
persistência
```

---

# 24. Desenvolvimento local

Durante desenvolvimento, frontend e backend rodam separadamente.

## Backend

```bash
cd src/Backend
dotnet watch
```

## Frontend

```bash
cd src/Frontend
npm install
npm run dev
```

O frontend deve consumir URLs relativas:

```text
/api/v1/...
```

Não deve conhecer diretamente a porta do backend.

Configurar proxy no Vite:

```ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:7001',
        changeOrigin: true,
        secure: false
      }
    }
  }
})
```

A porta do backend pode variar conforme configuração local.

Evitar hardcode da URL da API no código React.

---

# 25. Produção e publicação

Em produção, não utilizar um servidor Node/Vite separado.

Fluxo:

```text
React/Vite
   ↓
npm run build
   ↓
arquivos estáticos
   ↓
Backend/wwwroot
   ↓
ASP.NET Core
```

O ASP.NET Core deve servir:

```text
/api/v1/* -> Controllers
/*        -> React
```

Configuração conceitual:

```csharp
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapFallbackToFile("index.html");
```

Preferir configurar o `.csproj` para compilar/copiar o frontend durante:

```bash
dotnet publish
```

e não durante todo `dotnet build`.

A publicação final deve resultar em uma única aplicação.

---

# 26. SOLID pragmático

## SRP

Separar:

```text
HTTP
negócio
persistência
```

## OCP

Não criar extensibilidade imaginária.

## LSP

Evitar hierarquias de herança desnecessárias.

## ISP

Interfaces pequenas e específicas.

## DIP

O negócio não depende diretamente da implementação JSON:

```text
AccountService
      ↓
IMovementRepository
      ↑
JsonMovementRepository
```

---

# 27. Código

Priorizar:

- nomes explícitos;
- métodos pequenos;
- classes com responsabilidades claras;
- dependências explícitas;
- composição em vez de herança quando apropriado;
- `CancellationToken` em operações assíncronas relevantes.

Evitar comentários que apenas repetem o código.

Comentários devem explicar decisões ou restrições não óbvias.

---

# 28. Logging

Adicionar logs apenas onde trouxerem valor.

Exemplos:

- falha ao carregar persistência;
- falha ao gravar persistência;
- erro inesperado;
- inicialização do storage.

Não registrar dados em excesso.

---

# 29. README

O README final deve explicar:

## Visão geral

Descrição curta do problema e da solução.

## Arquitetura

Justificar:

```text
MVC enxuto + SOLID
```

e explicar por que não foi usada Clean Architecture completa:

> O domínio é pequeno e uma estrutura mais complexa aumentaria o custo cognitivo sem benefício proporcional.

Explicar também:

> Frontend e backend permanecem no mesmo repositório. Durante desenvolvimento, Vite e ASP.NET Core executam separadamente para hot reload. Na publicação, o frontend é compilado como arquivos estáticos e servido pelo ASP.NET Core, resultando em uma única aplicação para execução e deploy.

## Frontend

Documentar:

```text
React + Vite + TypeScript
CSS Modules
Radix UI
```

Explicar que Radix é utilizado apenas para primitives com comportamento ou acessibilidade complexa, mantendo HTML nativo para elementos simples.

## Como executar

### Desenvolvimento

- `dotnet watch` em `src/Backend`;
- `npm run dev` em `src/Frontend`;
- explicar o proxy `/api`.

### Produção

- executar `dotnet publish`;
- explicar que o frontend é compilado e servido pelo ASP.NET Core;
- demonstrar que apenas uma aplicação precisa ser iniciada.

## Testes

Documentar como executar os testes.

## API

Referenciar:

```text
docs/openapi.yaml
```

e a documentação Swagger/OpenAPI exposta pela aplicação.

## Persistência

Explicar:

- JSON foi escolhido por simplicidade;
- solução projetada para uma única instância;
- sincronização protege operações concorrentes;
- escrita segura reduz risco de corrupção;
- banco transacional seria a evolução natural em produção.

## Limitações deliberadas

Exemplos:

```text
single instance
filesystem local
sem autenticação
sem paginação inicialmente
sem banco transacional
```

## Evoluções possíveis

Exemplos:

```text
PostgreSQL / SQL Server
autenticação
paginação
idempotência
observabilidade
containerização
persistência distribuída
```

Não implementar essas evoluções sem necessidade.

---

# 30. Ordem de implementação

Seguir preferencialmente:

```text
1. definir OpenAPI
2. criar estrutura do backend
3. escrever testes do domínio
4. implementar domínio
5. criar fake/in-memory repository para testes
6. implementar AccountService
7. implementar JsonMovementRepository
8. criar controllers
9. criar testes de integração
10. criar teste de concorrência
11. criar estrutura React/Vite
12. configurar CSS Modules e tokens globais
13. configurar Radix UI
14. implementar interface minimalista
15. configurar proxy do Vite
16. configurar build do frontend para wwwroot
17. validar execução após dotnet publish
18. revisar OpenAPI
19. escrever README
20. revisão final
```

---

# 31. Critérios de aceite

## Backend

- [ ] é possível registrar crédito;
- [ ] é possível registrar débito;
- [ ] valores zero ou negativos são rejeitados;
- [ ] débito nunca deixa saldo negativo;
- [ ] saldo é derivado das movimentações;
- [ ] histórico é consultável;
- [ ] movimentações sobrevivem ao reinício;
- [ ] concorrência não permite saldo negativo;
- [ ] contrato OpenAPI corresponde à implementação;
- [ ] controllers não contêm regras de negócio;
- [ ] domínio é testável isoladamente;
- [ ] testes unitários cobrem regras principais;
- [ ] testes de integração cobrem endpoints.

## Frontend

- [ ] usa React + Vite + TypeScript;
- [ ] usa CSS Modules;
- [ ] não usa Tailwind;
- [ ] usa Radix apenas quando necessário;
- [ ] usa HTML semântico para elementos simples;
- [ ] utiliza a paleta definida neste arquivo;
- [ ] não espalha cores HEX arbitrárias pelos componentes;
- [ ] possui estados de foco visíveis;
- [ ] não depende apenas de cor para comunicar estado;
- [ ] consome apenas a API;
- [ ] usa URLs relativas `/api/...`;
- [ ] proxy do Vite funciona em desenvolvimento.

## Deploy

- [ ] `dotnet publish` inclui o frontend compilado;
- [ ] ASP.NET Core serve o React em produção;
- [ ] a solução publicada roda como uma única aplicação;
- [ ] README contém instruções claras.

---

# 32. Instruções para agentes de código

Ao trabalhar neste repositório:

1. Leia este arquivo antes de modificar código.
2. Preserve a arquitetura simples.
3. Não introduza frameworks ou padrões adicionais sem necessidade concreta.
4. Não mova regras de negócio para controllers ou frontend.
5. Não persista saldo separadamente das movimentações.
6. Preserve a proteção de concorrência.
7. Antes de alterar regra de negócio, atualize ou crie testes.
8. Execute os testes após mudanças relevantes.
9. Mantenha OpenAPI sincronizado com backend e frontend.
10. Prefira a solução mais simples que preserve requisitos e invariantes.
11. Não faça refatorações amplas fora do escopo atual.
12. Não altere contratos públicos sem atualizar OpenAPI, testes e frontend afetado.
13. Documente decisões técnicas relevantes no README.
14. Não adicione dependências externas quando a biblioteca padrão resolver adequadamente.
15. Preserve o monorepo.
16. Durante desenvolvimento, mantenha Vite e ASP.NET Core como processos separados.
17. Em produção, o ASP.NET Core deve servir o frontend compilado.
18. Não introduza servidor Node separado em produção.
19. Não introduza Tailwind CSS.
20. Use CSS Modules para estilos locais.
21. Use Radix UI apenas para interações que realmente se beneficiem dele.
22. Prefira HTML semântico quando suficiente.
23. Preserve os design tokens definidos neste arquivo.
24. Não adicionar cores arbitrárias diretamente em componentes se um token semântico puder ser utilizado.
25. Não transformar este desafio pequeno em uma demonstração de infraestrutura empresarial.

---

# 33. Princípio geral

A solução deve demonstrar maturidade técnica através de:

```text
simplicidade
+
correção
+
testabilidade
+
consistência
+
acessibilidade
+
decisões justificáveis
```

e não através da quantidade de camadas, padrões ou bibliotecas utilizadas.

Quando houver duas soluções corretas, escolher a que seja mais fácil de compreender, testar, explicar e alterar.
