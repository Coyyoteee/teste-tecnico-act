# Controle de movimentações empresariais

## 1. Visão geral

Aplicação para registrar créditos e débitos em uma conta empresarial, consultar saldo e histórico e impedir que débitos deixem o saldo negativo.

O backend segue um fluxo MVC/API enxuto:

```text
HTTP → Controller → Service → Domain → Repository
```

Stack principal:

- .NET 10 e ASP.NET Core Web API;
- React, TypeScript e Vite;
- CSS Modules e Radix UI;
- persistência local em JSON;
- xUnit no backend;
- Vitest e Testing Library no frontend.

## 2. Decisões técnicas

- **MVC enxuto + SOLID:** separa HTTP, casos de uso, regras de negócio e persistência sem adicionar camadas desnecessárias para o tamanho do domínio.
- **API First:** [`docs/openapi.yaml`](docs/openapi.yaml) é a referência do contrato consumido pelo backend e pelo frontend.
- **Movimentações como fonte de verdade:** o saldo não é persistido separadamente; ele é sempre derivado do histórico de créditos e débitos.
- **Persistência JSON:** mantém a solução simples e adequada ao escopo de uma única instância.
- **Consistência:** um `SemaphoreSlim` protege leitura, validação e persistência dentro da mesma região crítica, inclusive sob requisições concorrentes.
- **Monorepo:** frontend, backend e testes permanecem no mesmo repositório.
- **Deploy único:** em produção, o frontend compilado é servido pelo ASP.NET Core junto com a API.

## 3. Pré-requisitos para execução local

- .NET SDK 10;
- Node.js e npm.

Para executar via Docker, esses pré-requisitos não precisam estar instalados localmente.

## 4. Executar em desenvolvimento

Na raiz do repositório, inicie o backend:

```bash
dotnet watch --project ./src/Backend/Challenge.Api.csproj
```

Em outro terminal, inicie o frontend:

```bash
cd ./src/Frontend
npm install
npm run dev
```

O frontend usa URLs relativas `/api/...`; o proxy do Vite encaminha essas requisições para `http://localhost:5204`. Acesse a URL exibida pelo Vite, normalmente [http://localhost:5173](http://localhost:5173).

## 5. Scalar e documentação da API

Em `Development`, a documentação interativa fica disponível na URL do backend em [http://localhost:5204/scalar/v1](http://localhost:5204/scalar/v1). O documento OpenAPI gerado pela aplicação fica em [http://localhost:5204/openapi/v1.json](http://localhost:5204/openapi/v1.json).

Confirme a porta no output do ASP.NET Core ao iniciar a aplicação. Scalar e OpenAPI não são expostos em `Production`.

## 6. Executar testes

Backend, na raiz do repositório:

```bash
dotnet test ./Challenge.sln
```

Frontend:

```bash
cd ./src/Frontend
npm test
```

Build do frontend:

```bash
npm run build
```

## 7. Executar como aplicação final sem Docker

Na raiz do repositório:

```bash
dotnet publish ./src/Backend/Challenge.Api.csproj -c Release -o ./publish
```

Depois, execute a aplicação dentro da pasta publicada:

```bash
cd ./publish
dotnet ./Challenge.Api.dll
```

É importante executar a DLL dentro de `publish`, pois o frontend compilado está em `publish/wwwroot`. Sem configuração adicional de URL, a aplicação normalmente fica disponível em [http://localhost:5000](http://localhost:5000); confirme a URL no output do ASP.NET Core.

## 8. Executar com Docker

Na raiz do repositório, construa a imagem:

```bash
docker build -t act-challenge .
```

Execute o container com um volume para persistência:

```bash
docker run --name act-challenge -p 8080:8080 -v act-challenge-data:/app/data act-challenge
```

Acesse [http://localhost:8080](http://localhost:8080). Frontend e API executam no mesmo container, e não é necessário instalar .NET ou Node.js para usar a imagem. O volume `act-challenge-data` preserva o arquivo JSON de movimentações.

Comandos úteis:

```bash
docker stop act-challenge
docker start act-challenge
docker rm act-challenge
```

Para remover também os dados, somente quando isso for desejado explicitamente:

```bash
docker volume rm act-challenge-data
```

Esse último comando apaga permanentemente a persistência do volume.

## 9. Persistência

O arquivo JSON é criado em runtime e não é versionado. As movimentações são a fonte de verdade, e o saldo é recalculado a partir do histórico.

- Execução local: `src/Backend/data/movements.json` em desenvolvimento, ou `publish/data/movements.json` ao executar a publicação dentro de `publish`.
- Docker: `/app/data/movements.json`, armazenado no volume montado em `/app/data`.

## 10. Estrutura resumida

```text
src/
├── Backend/
└── Frontend/
tests/
docs/
Dockerfile
README.md
AGENTS.md
```

## 11. Limitações deliberadas

Decisões de escopo desta entrega:

- execução em uma única instância;
- sem autenticação;
- sem banco de dados;
- persistência em arquivo local;
- sem paginação inicialmente.

## 12. Validação manual rápida

- [ ] Abrir o frontend.
- [ ] Registrar um crédito.
- [ ] Registrar um débito válido.
- [ ] Tentar registrar um débito maior que o saldo.
- [ ] Verificar a atualização do saldo.
- [ ] Verificar o histórico de movimentações.
- [ ] Reiniciar a aplicação e confirmar a persistência.
- [ ] Em Docker, recriar o container com o mesmo volume e confirmar os dados.
