# ES.ConexaoSolidaria.Frontend

Frontend web da plataforma **Conexão Solidária**, desenvolvido para a ONG Esperança Solidária como parte do Hackathon POSTECH/FIAP.

Aplicação **Blazor WebAssembly** que consome exclusivamente a API através do **Gateway** (YARP local / Amazon API Gateway em nuvem), com autenticação JWT e controle de acesso por perfil (`GESTOR_ONG` / `DOADOR`) refletido tanto nas rotas quanto no menu de navegação.

---

## Sumário
- [Arquitetura](#arquitetura)
- [Stack Tecnológica](#stack-tecnológica)
- [Páginas e Rotas](#páginas-e-rotas)
- [Autenticação](#autenticação)
- [Como Rodar Localmente](#como-rodar-localmente)
- [Configuração](#configuração)
- [Testes](#testes)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Github Actions](#github-actions)

---

## Arquitetura

O Frontend é uma **SPA Blazor WebAssembly** — todo o código roda no navegador do usuário, não há servidor de aplicação (o Nginx apenas serve os arquivos estáticos publicados). Todas as chamadas de API são feitas diretamente do browser para o **Gateway** (`http://localhost:5006` em ambiente local), que por sua vez roteia para os microsserviços de Usuários e Campanhas/Doações.

O token JWT é armazenado no `localStorage` do navegador (via Blazored.LocalStorage) e anexado manualmente como header `Authorization: Bearer` em cada chamada autenticada — não há cookies nem sessão de servidor.

> O diagrama completo da arquitetura da plataforma está no repositório de infraestrutura — [https://github.com/gmerendi/ES.ConexaoSolidaria.Infra]

## Stack Tecnológica

- **.NET 8** — Blazor **WebAssembly**
- **MudBlazor** — biblioteca de componentes UI (Material Design)
- **Blazored.LocalStorage** — persistência do token/usuário no navegador
- **System.IdentityModel.Tokens.Jwt** — leitura do token JWT no cliente (extrai perfil e expiração reais do token, não confia apenas na resposta da API)
- **Nginx** — serve os arquivos estáticos publicados em produção/Docker, com fallback de SPA
- **Docker** / **Docker Compose**

## Páginas e Rotas

### Públicas
| Rota | Página | Descrição |
|---|---|---|
| `/` | `LandingPage` | Página inicial pública |
| `/login` | `Login` | Autenticação |
| `/cadastro` | `CadastroDoador` | Cadastro público de novo doador |

### Área autenticada (`DOADOR` e `GESTOR_ONG`)
| Rota | Página | Descrição |
|---|---|---|
| `/portal` | `Home` | Dashboard inicial após login |
| `/campanhas` | `Portal/Campanhas` | Lista de campanhas ativas (Painel de Transparência) |
| `/perfil` | `Portal/Perfil` | Dados do usuário logado / alteração de senha |
| `/doacoes` | `Portal/MinhasDoacoes` | Doações do próprio usuário |
| `/doacoes/nova` | `Portal/NovaDoacao` | Realizar uma nova doação |

### Área administrativa (somente `GESTOR_ONG`)
| Rota | Página | Descrição |
|---|---|---|
| `/admin/usuarios` | `Admin/AdminUsuarios` | Gestão de usuários (suspender/ativar/alterar perfil) |
| `/admin/campanhas` | `Admin/GerenciamentoCampanhas` | Criar, editar, cancelar e concluir campanhas |
| `/doacoes/campanha` | `Admin/DoacoesPorCampanha` | Consulta de doações por campanha |
| `/doacoes/usuario` | `Admin/DoacoesPorUsuario` | Consulta de doações por usuário |

> A rota `/counter` (`Counter.razor`) é o resquício do template padrão do Blazor e não faz parte do fluxo da aplicação.

## Autenticação

- O `JwtAuthStateProvider` lê o usuário armazenado no `localStorage` e monta um `ClaimsPrincipal` com as claims de e-mail, perfil (`Role`) e status, extraídas diretamente do token JWT.
- O menu lateral (`NavMenu`) usa `<AuthorizeView Roles="GESTOR_ONG">` para exibir a seção de Administração apenas para gestores — mas, como toda validação client-side, isso é só uma questão de UX: a autorização de verdade acontece nos endpoints protegidos por `[Authorize(Roles=...)]` na API.
- Ao expirar, o token e os dados do usuário são limpos automaticamente do `localStorage`.

## Como Rodar Localmente

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Docker Desktop 4.79.0
- O **Gateway** (`ES.ConexaoSolidaria.Gateway`) já em execução em `http://localhost:5006`, com as APIs de Usuários e Campanhas rodando por trás dele

### Subindo com Docker Compose

```bash
git clone https://github.com/gmerendi/ES.ConexaoSolidaria.Frontend.git
cd ES.ConexaoSolidaria.Frontend

docker compose up -d --build
```

Serviço exposto:

| Serviço | URL/Porta |
|---|---|
| Frontend | http://localhost:5000 |

### Rodando fora do Docker (dev server)

```bash
dotnet restore
dotnet run --project src/Frontend
```

## Configuração

A URL do Gateway é definida em `wwwroot/appsettings.json` (compilada junto com os assets estáticos, já que é uma SPA client-side — não há variável de ambiente injetada em runtime):

```json
{
  "GatewayUrl": "http://localhost:5006"
}
```


## Testes

Este repositório ainda não possui um projeto de testes automatizados.

## Estrutura do Projeto

```
ES.ConexaoSolidaria.Frontend/
├── src/
│   └── Frontend/
│       ├── Auth/              # JwtAuthStateProvider
│       ├── Layout/             # MainLayout, LandingLayout, NavMenu
│       ├── Models/              # DTOs de Auth, Campanhas, Doações, Common
│       ├── Pages/
│       │   ├── Auth/            # Login, CadastroDoador
│       │   ├── Portal/          # Campanhas, MinhasDoacoes, NovaDoacao, Perfil
│       │   └── Admin/           # AdminUsuarios, GerenciamentoCampanhas, DoacoesPorCampanha/Usuario
│       ├── Services/            # AuthService, UsuarioService, CampanhaAdminService, CampanhaPublicaService, DoacaoService
│       ├── Shared/               # Componentes reutilizáveis (CampanhaCard, DadosUsuarioRow, RedirectToLogin)
│       ├── wwwroot/              # Assets estáticos e appsettings.json (GatewayUrl)
│       └── Program.cs
├── nginx.conf                    # Configuração do Nginx (serve estáticos + fallback SPA)
├── docker-compose.yml
└── ES.ConexaoSolidaria.Frontend.slnx
```

Projeto desenvolvido para o Hackathon **POSTECH** — grupo 1.

## Github Actions

Esse repositorio não possui Github Action.
