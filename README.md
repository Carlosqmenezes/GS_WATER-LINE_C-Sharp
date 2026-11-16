# WaterLine API

API principal do **WaterLine**, a CitizenTech da Global Solution 2026.2 (FIAP), entregue na disciplina **C# Software Development**.

O WaterLine monitora bocas de lobo em São Paulo para reduzir alagamentos. Cada boca de lobo tem um ESP32 com dois sensores ultrassônicos: o **externo** detecta lixo tampando a entrada, e o **interno** mede o fundo da caixa e o nível da água. Há também um indicador de chuva. Esta API é o centro do sistema: recebe as leituras, diagnostica, abre chamados, despacha o agente certo mais próximo e gera alertas.

| Camada | Quando | O que a API faz |
| --- | --- | --- |
| 0. Manutenção contínua | O ano todo | Sedimento ou tubo entupido abrem chamado para a equipe de galeria |
| 1. Prevenção | Previsão de chuva forte (ML) | Eleva a prioridade dos chamados ativos e cria chamados preventivos para bocas com histórico de entupimento |
| 2. Alerta precoce | Chovendo e água subindo rápido | Grava um alerta e chama o serviço de alertas |
| 3. Emergência | Chovendo e nível crítico | Grava um alerta de emergência e chama o serviço de alertas (central) |

Todos os dados do seed são **fictícios**: nenhum nome, matrícula ou endereço é real.

---

## Tecnologias

| Item | Versão |
| --- | --- |
| .NET / ASP.NET Core Web API (controllers) | 8.0 (LTS) |
| Entity Framework Core + Npgsql (PostgreSQL) | 8.0.34 / 8.0.11 |
| PostgreSQL (Docker) | 16 (imagem `postgres:16-alpine`) |
| Swagger UI (Swashbuckle.AspNetCore) | 9.0.6 |
| Testes | xUnit 2.9.3 |
| Ferramenta de migrations | `dotnet-ef` 8.0.34 (ferramenta local, em `dotnet-tools.json`) |

## Estrutura da solução

```
WaterLine.Api/
├── WaterLine.sln
├── docker-compose.yml            PostgreSQL para desenvolvimento
├── dotnet-tools.json             versão fixa do dotnet-ef
├── src/
│   ├── WaterLine.Domain/         regras de negócio, sem dependências externas
│   │   ├── Entidades/            BocaDeLobo, Leitura, Agente (abstrata), Gari, EquipeGaleria, Chamado, PrevisaoChuva, Alerta
│   │   ├── Enums/                TipoDiagnostico, TipoChamado, StatusChamado, NivelAlerta
│   │   ├── Interfaces/           IDiagnosticoService, IDespachoService, IAlertaClient, ...
│   │   ├── Servicos/             implementação das regras (diagnóstico, despacho, prevenção, segurança)
│   │   ├── Configuracoes/        parâmetros das regras (lidos do appsettings.json)
│   │   ├── Geo/                  Haversine
│   │   └── Excecoes/             exceções de domínio (viram 404, 409 e 422)
│   └── WaterLine.Api/            camada web e de infraestrutura
│       ├── Controllers/          rotas REST
│       ├── Dtos/                 contratos de entrada e saída, com validação
│       ├── Servicos/             casos de uso (orquestram domínio e banco)
│       ├── Dados/                DbContext, mapeamentos Fluent API, migrations e seed
│       ├── Integracoes/          clientes do serviço de alertas (HTTP ou log)
│       ├── Armazenamento/        gravação das fotos dos chamados
│       ├── Erros/                tratamento global de erros (ProblemDetails)
│       └── Configuracao/         registro de injeção de dependência
└── tests/
    └── WaterLine.Tests/          testes unitários xUnit do domínio
```

Referências entre projetos: `WaterLine.Api → WaterLine.Domain` e `WaterLine.Tests → WaterLine.Domain`.

---

## Como executar

### Pré-requisitos

- **Visual Studio 2022 versão 17.8 ou superior** (ou Visual Studio 2026), com a carga de trabalho **ASP.NET e desenvolvimento Web**. Ela já instala o SDK do .NET 8.
- **Docker Desktop** para Windows, aberto e em execução.
- Porta **5432** livre. Se você já tem um PostgreSQL instalado nessa porta, pare-o ou troque a porta no `docker-compose.yml` e na connection string do `appsettings.Development.json`.

### 1. Subir o banco de dados

No terminal (PowerShell), na pasta raiz do repositório (a que contém o `WaterLine.sln`):

```powershell
docker compose up -d
docker compose ps        # o serviço "postgres" deve aparecer como "healthy"
```

O banco `waterline` fica em `127.0.0.1:5432` (usuário `waterline`, senha `waterline_dev`, **só para desenvolvimento**).

> A connection string usa `127.0.0.1`, e não `localhost`, de propósito. Em alguns Windows, outro processo escuta em `::1:5432` (o IPv6 de `localhost`), e a conexão fica presa até dar timeout em vez de chegar ao container.

### 2. Aplicar as migrations

Ainda na pasta raiz:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/WaterLine.Api
```

O comando cria as tabelas `BocasDeLobo`, `Leituras`, `Agentes`, `Chamados`, `PrevisoesChuva` e `Alertas`. No Visual Studio, os mesmos comandos funcionam no terminal integrado (**Exibir > Terminal**).

### 3. Rodar a API no Visual Studio (F5)

1. Abra o arquivo `WaterLine.sln`.
2. No Gerenciador de Soluções, clique com o botão direito em **WaterLine.Api** e escolha **Definir como Projeto de Inicialização**.
3. Confira se o perfil de execução (ao lado do botão verde) é **http**.
4. Aperte **F5**. O navegador abre em **http://localhost:5080/swagger**.

Pela linha de comando, o equivalente é `dotnet run --project src/WaterLine.Api --launch-profile http`.

Na primeira execução em `Development`, o **seed** cria 5 bocas de lobo e 5 agentes fictícios (3 garis e 2 equipes de galeria), todos fora de serviço. O seed só roda com o banco migrado e vazio. Se o banco não estiver no ar, a API sobe mesmo assim e registra o erro no console.

### 4. Rodar os testes

```powershell
dotnet test
```

No Visual Studio: **Teste > Gerenciador de Testes > Executar Todos**.

### Zerar o banco

```powershell
docker compose down -v
docker compose up -d
dotnet tool run dotnet-ef database update --project src/WaterLine.Api
```

---

## Roteiro de teste no Swagger

Os Ids abaixo são os do seed num banco recém-criado: agentes 1 a 3 são garis, 4 e 5 são equipes de galeria, e as bocas de lobo vão de 1 a 5. Confira com `GET /bocas-de-lobo`. A boca de lobo padrão tem 80 cm de profundidade e 50 cm de abertura.

**A. Entrada bloqueada → gari → conclusão com foto**

1. `POST /agentes/1/servico`: o gari 1 entra em serviço perto da BL-0001.
   ```json
   { "emServico": true, "latitude": -23.5502, "longitude": -46.6401 }
   ```
2. `POST /leituras`: lixo tampando a entrada (10 cm é menor que 70% de 50 cm).
   ```json
   { "bocaDeLoboId": 1, "distanciaExternaCm": 10, "distanciaInternaCm": 80, "chovendo": false }
   ```
   Resposta esperada: `"diagnostico": "EntradaBloqueada"`, com um `chamadoAbertoId` e `"agenteDesignadoId": 1`.
3. `GET /chamados/proximos?lat=-23.5502&lng=-46.6401`: o chamado aparece com a distância.
4. `POST /chamados/{id}/aceitar` com `{ "agenteId": 1 }` → 200.
5. `POST /chamados/{id}/concluir` com `agenteId = 1` e duas imagens JPEG ou PNG → **422**, porque o sensor ainda mostra a entrada bloqueada.
6. `POST /leituras` com a entrada livre: `{ "bocaDeLoboId": 1, "distanciaExternaCm": 50, "distanciaInternaCm": 80, "chovendo": false }`.
7. Repita o passo 5 → **200**, status `Concluido`. Sem uma das fotos → **422**.

**B. Chuva: cancelamento por segurança, alerta precoce e emergência**

1. `POST /leituras` com `{ "bocaDeLoboId": 2, "distanciaExternaCm": 10, "distanciaInternaCm": 80, "chovendo": false }`: abre um chamado de gari na BL-0002.
2. `POST /leituras` com `{ "bocaDeLoboId": 2, "distanciaExternaCm": 10, "distanciaInternaCm": 80, "chovendo": true }`: começou a chover, e o chamado aparece em `chamadosCanceladosIds`. Nenhum gari é chamado enquanto chove.
3. `POST /leituras` com `{ "bocaDeLoboId": 2, "distanciaExternaCm": 50, "distanciaInternaCm": 60, "chovendo": true }`: a água subiu 20 cm desde a leitura anterior, então o diagnóstico é `AguaSubindo` e a resposta traz um `alertaId`. A velocidade é calculada pela diferença de `dataHora` entre as leituras; sem `dataHora`, vale a hora do servidor.
4. `POST /leituras` com `{ "bocaDeLoboId": 2, "distanciaExternaCm": 50, "distanciaInternaCm": 8, "chovendo": true }`: 90% de ocupação, então o diagnóstico é `NivelCritico` (alerta de emergência).
5. `GET /alertas`: os dois alertas aparecem, o mais recente primeiro.

**C. Manutenção da equipe de galeria**

- `POST /leituras` com `{ "bocaDeLoboId": 3, "distanciaExternaCm": 60, "distanciaInternaCm": 54, "chovendo": false }`: 40% de ocupação sem chuva, então o diagnóstico é `SedimentoAcumulando` e abre um chamado `Manutencao` para a equipe de galeria.

**D. Previsão de chuva forte**

- `POST /previsoes` com `{ "horizonteHoras": 3, "probabilidade": 0.82, "chuvaForte": true }`: a resposta mostra quantos chamados foram priorizados e os Ids dos preventivos criados.

---

## Endpoints

Todos os endpoints são assíncronos, recebem e devolvem DTOs, e os erros seguem o padrão **ProblemDetails** (RFC 7807).

| Método | Rota | Quem usa | Sucesso | Erros |
| --- | --- | --- | --- | --- |
| POST | `/leituras` | ESP32 | 201 com o diagnóstico e as ações tomadas | 400, 404, 422 (boca inativa) |
| GET | `/bocas-de-lobo?ativa=` | Painel | 200 | — |
| GET | `/bocas-de-lobo/{id}` | Painel | 200 | 404 |
| POST | `/bocas-de-lobo` | Painel | 201 + `Location` | 400, 409 (código repetido) |
| PUT | `/bocas-de-lobo/{id}` | Painel | 200 | 400, 404, 409 |
| DELETE | `/bocas-de-lobo/{id}` | Painel | 204 (exclusão lógica) | 404 |
| GET | `/chamados/proximos?lat=&lng=` | App do gari | 200, ordenado por prioridade e distância | 400 |
| POST | `/chamados/{id}/aceitar` | App do gari | 200 | 400, 404, 409, 422 (fora do raio, fora de serviço ou tipo errado) |
| POST | `/chamados/{id}/concluir` | App do gari (multipart: `agenteId`, `fotoAntes`, `fotoDepois`) | 200 | 400 (foto inválida), 404, 409, 422 (sem foto ou sensor sem confirmar) |
| POST | `/previsoes` | Modelo de ML | 201 com o resumo da prevenção | 400 |
| GET | `/alertas?bocaDeLoboId=&nivel=&limite=` | Painel / central | 200 | 400 |
| POST | `/agentes/{id}/servico` | App do gari | 200 | 400, 404 |

---

## Regras de negócio

Todos os parâmetros ficam no `appsettings.json`, na seção `Regras`, e são validados quando a API sobe: um valor inválido impede a inicialização.

**1. Diagnóstico** (`DiagnosticoService`). Cálculos: `nível = ProfundidadeCm − DistanciaInternaCm` e `percentual = nível ÷ profundidade`.

| Condição | Diagnóstico | Parâmetro |
| --- | --- | --- |
| Chovendo e percentual ≥ 85% | `NivelCritico` | `PercentualNivelCritico` |
| Chovendo e subida ≥ 5 cm/min desde a leitura anterior | `AguaSubindo` | `VelocidadeSubidaCmPorMinuto` |
| Sem chuva e percentual ≥ 60% há mais de 24 h | `TuboEntupido` | `PercentualTuboEntupido`, `HorasAguaParadaTubo` |
| `DistanciaExternaCm` < 70% da largura da abertura | `EntradaBloqueada` | `PercentualEntradaBloqueada` |
| Sem chuva e percentual entre 30% e 60% | `SedimentoAcumulando` | `PercentualSedimentoMinimo` |
| Nenhuma das anteriores | `Normal` | — |

Quando mais de uma condição vale, vence a primeira da tabela.

**2. Abertura de chamado** (`AberturaChamadoService`):
- `EntradaBloqueada` → `Preventivo` (gari);
- `SedimentoAcumulando` → `Manutencao` (galeria);
- `TuboEntupido` → `Urgente` (galeria);
- `AguaSubindo` e `NivelCritico` geram alerta, não chamado.

O serviço não abre um chamado se já existir outro do mesmo tipo aberto ou aceito para a mesma boca de lobo.

**3. Despacho** (`DespachoService`). Entre os agentes em serviço do tipo certo, escolhe o mais próximo pela fórmula de Haversine, dentro de `Despacho:RaioMetros` (300 m). Os chamados são ordenados por prioridade e depois por distância, com LINQ.

**4. Prevenção** (`PrevencaoService`). Com `ChuvaForte = true`, a prioridade de todos os chamados ativos sobe `Prevencao:IncrementoPrioridade` e um chamado `Preventivo` é criado para cada boca de lobo com histórico de entupimento. Histórico de entupimento é o status atual de entupimento ou uma leitura de bloqueio ou de sedimento nos últimos `DiasHistoricoEntupimento` dias.

**5. Segurança do trabalhador** (`SegurancaTrabalhadorService`). Uma leitura com `Chovendo = true` cancela os chamados de gari abertos ou aceitos daquela boca de lobo, e nenhum chamado novo de gari é aberto enquanto chove.

**6. Conclusão** (`Chamado.Concluir`). O chamado só vira `Concluido` com as duas fotos e com a última leitura mostrando a entrada livre. Só conclui o agente que aceitou.

**7. Alertas** (`IAlertaClient`). `AguaSubindo` grava um alerta `AlertaPrecoce`, e `NivelCritico` grava um alerta `Emergencia`. Depois de gravar, a API chama o serviço externo:

| `Alertas:BaseUrl` | Implementação | Comportamento |
| --- | --- | --- |
| Preenchida | `HttpAlertaClient` | `POST {BaseUrl}/alertas` para alerta precoce e `POST {BaseUrl}/alertas/emergencia` para emergência |
| Vazia | `LogAlertaClient` | Só registra o alerta em log |

Falha no serviço externo não derruba a leitura: o alerta continua gravado no banco.

---

## Onde cada critério de avaliação aparece

| Critério | Onde ver |
| --- | --- |
| **POO** | Encapsulamento: setters privados e construtores que validam em `Domain/Entidades`. Herança: `Agente` (abstrata) → `Gari` e `EquipeGaleria`, mapeados em TPH. Polimorfismo: `TiposDeChamadoAtendidos` e `PodeAtender`. Interfaces em `Domain/Interfaces`. |
| **Web API** | Rotas REST em `Controllers/`, DTOs com DataAnnotations e `IValidatableObject` em `Dtos/`, status 200, 201, 204, 400, 404, 409 e 422, Swagger com comentários XML. |
| **EF Core + LINQ** | `WaterLineDbContext`, mapeamentos Fluent API em `Dados/Configuracoes`, relacionamentos 1:N, índices, herança TPH, concorrência otimista (`xmin`), migrations em `Dados/Migrations`. LINQ com `Join`, subconsulta `Contains` e `Distinct` (`PrevisaoAppService`), e ordenações por prioridade e distância (`DespachoService`). |
| **Async/Await** | Todo acesso a banco (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`), arquivo (`CopyToAsync`) e HTTP (`PostAsJsonAsync`) é assíncrono e recebe `CancellationToken`. |
| **Qualidade** | Injeção de dependência centralizada (`Configuracao/InjecaoDependencia.cs`), camadas separadas, tratamento global de erros (`Erros/TratamentoErrosHandler.cs`), opções validadas na inicialização e testes unitários em `tests/`. |

## Privacidade do trabalhador

- A localização do agente só é guardada enquanto ele está em serviço e é apagada quando ele sai.
- Os logs registram só Ids. Localização e matrícula nunca vão para o log, e `Agente.ToString()` devolve apenas a função e o Id.
- As respostas da API não devolvem a localização do agente.
- As fotos ficam em `App_Data/fotos`, fora do controle de versão, com nome gerado pelo servidor. A remoção dos metadados EXIF é responsabilidade do módulo de Cybersecurity do projeto.

## Configuração

| Seção | Chave | Padrão |
| --- | --- | --- |
| `ConnectionStrings` | `WaterLine` | definida em `appsettings.Development.json` |
| `Regras:Diagnostico` | `PercentualEntradaBloqueada`, `PercentualSedimentoMinimo`, `PercentualTuboEntupido`, `HorasAguaParadaTubo`, `VelocidadeSubidaCmPorMinuto`, `PercentualNivelCritico` | 70, 30, 60, 24, 5, 85 |
| `Regras:Despacho` | `RaioMetros` | 300 |
| `Regras:Prevencao` | `IncrementoPrioridade`, `DiasHistoricoEntupimento` | 1, 30 |
| `Alertas` | `BaseUrl`, `TimeoutSegundos`, `IntervaloMinimoMinutos` | vazio, 5, 10 |
| `Armazenamento` | `PastaFotos`, `TamanhoMaximoFotoBytes` | `App_Data/fotos`, 5 MB |
| `Swagger` | `Habilitado` | `true` |
| `Seed` | `Habilitado` | `false` (`true` em `Development`) |

Qualquer chave pode ser sobrescrita por variável de ambiente, por exemplo `Alertas__BaseUrl=http://localhost:5090`.

## Limitações conhecidas

- A integração com a central (CGE / Defesa Civil) é uma proposta: o alerta de emergência vai para o serviço de alertas, e o painel faz o papel da central na demonstração.
- Não há autenticação, porque está fora do escopo da disciplina. O `agenteId` é informado na requisição.
- Durante a chuva, o mesmo nível de alerta para a mesma boca de lobo não se repete dentro de `Alertas:IntervaloMinimoMinutos`, para evitar um aviso a cada leitura.
