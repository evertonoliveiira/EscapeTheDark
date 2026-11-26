# Projeto de Jogo de Terror em Unity

> Este documento preserva o planejamento inicial. A progressão implementada agora exige recolher e instalar três fusíveis no quadro da recepção, resolver o armário da enfermaria com uma pista da observação e isolar o equipamento da cirurgia. O primeiro circuito libera um atalho para a manutenção; a saída precisa ser acionada após as três instalações. Consulte as [regras atuais no README](../README.md#objetivos-e-progressão).

## Nome provisório

**Escape the Dark**

## Descrição

Escape the Dark será um jogo de terror em primeira pessoa desenvolvido na Unity.

A proposta é criar uma experiência curta, com foco em exploração, suspense e perseguição. O jogador deverá explorar um ambiente abandonado, encontrar objetos necessários para liberar a saída e evitar uma criatura que passa a persegui-lo durante a partida.

O projeto será desenvolvido com escopo reduzido, adequado para um trabalho de aula, com duração aproximada de 5 a 10 minutos.

## Inspirações

O jogo terá inspiração em elementos presentes em:

- Slender Man, principalmente na exploração do cenário e coleta de objetos.
- Dark Deception, principalmente nos momentos de perseguição e fuga.
- Jogos de terror em primeira pessoa com foco em ambientação, iluminação e áudio.

A proposta não é reproduzir diretamente esses jogos, mas utilizar algumas de suas mecânicas como referência.

## Objetivo do Jogo

O jogador deverá encontrar **3 fusíveis** espalhados pelo cenário.

Após coletar os três fusíveis, a porta de saída será liberada.

O jogador vence ao conseguir chegar até a saída sem ser alcançado pela criatura.

## Condição de Vitória

- Coletar os 3 fusíveis.
- Liberar a porta de saída.
- Chegar até a saída.

## Condição de Derrota

O jogador perde caso seja alcançado pela criatura durante uma perseguição.

Ao perder, será exibida uma tela de Game Over com a opção de reiniciar a partida.

## Mecânicas Principais

O jogo terá as seguintes mecânicas:

- Movimentação em primeira pessoa.
- Controle de câmera com mouse.
- Corrida.
- Lanterna.
- Interação com objetos.
- Coleta de fusíveis.
- Contador de fusíveis na interface.
- Porta de saída bloqueada.
- Inimigo controlado por inteligência artificial simples.
- Perseguição.
- Eventos de susto.
- Tela de vitória.
- Tela de derrota.

## Cenário

### Direção escolhida para o protótipo

O cenário implementado é um hospital abandonado compacto, com nove áreas conectadas. O Hospital Horror Pack fornece pisos, tetos, paredes e objetos clínicos. As salas têm usos diferentes para reduzir a repetição visual.

A maior parte do hospital fica escura. Com a lanterna desligada, a visibilidade é muito baixa; luzes fracas e instáveis servem apenas como pontos de referência. A lanterna revela os detalhes próximos, mas também facilita que a criatura veja o jogador.

A criatura patrulha o hospital, investiga o som de passos, percebe melhor o jogador correndo e procura por alguns segundos no último local em que o detectou. A captura exige proximidade e linha de visão. O jogo continua com apenas uma criatura para preservar o escopo curto.

O jogo acontecerá em uma instalação abandonada.

O ambiente poderá representar, por exemplo:

- Hospital abandonado.
- Escola abandonada.
- Laboratório.
- Depósito industrial.
- Instalação subterrânea.

O mapa será pequeno e formado principalmente por salas e corredores.

Exemplo de estrutura:

```text
       Sala 1
          |
Entrada - Corredor - Sala 2
          |
       Corredor
       /     \
   Sala 3   Sala 4
       \     /
        Saída
```

O mapa deverá possuir alguns caminhos alternativos para permitir momentos de perseguição e fuga.

## Progressão da Partida

A progressão será dividida de forma simples.

### Início

O jogador começa dentro da instalação e descobre que a saída está bloqueada.

Uma mensagem informa que é necessário restaurar a energia utilizando três fusíveis.

### Primeiro Fusível

O jogador explora o cenário e encontra o primeiro fusível.

Após a coleta, poderá ocorrer um pequeno evento de terror, como:

- Uma porta fechando sozinha.
- Uma luz apagando.
- Um objeto caindo.
- Um som distante.

### Segundo Fusível

Após coletar o segundo fusível, a criatura passa a fazer parte do jogo.

Ela poderá aparecer em um corredor ou em uma sala distante.

A partir desse momento, o jogador poderá ser perseguido.

### Terceiro Fusível

Ao coletar o terceiro fusível, a saída é liberada.

O jogador deverá retornar até a porta de saída enquanto evita o inimigo.

### Final

Ao chegar à saída, o jogador vence a partida.

## Inimigo

O jogo terá apenas um inimigo.

A inteligência artificial será simples para manter o projeto dentro do escopo da atividade.

### Comportamentos

O inimigo poderá possuir os seguintes estados:

- Parado ou desativado no início da partida.
- Patrulha pelo cenário.
- Detecção do jogador.
- Perseguição.
- Retorno à patrulha caso perca o jogador.

A movimentação do inimigo poderá ser implementada utilizando o sistema de **NavMesh** da Unity.

Quando o inimigo alcançar o jogador, a partida termina.

## Sustos e Suspense

Os sustos serão usados de forma limitada para evitar repetição.

O jogo poderá utilizar aproximadamente três eventos principais.

### Exemplo 1

Ao entrar em determinado corredor, uma luz apaga ou começa a piscar.

### Exemplo 2

Após coletar o primeiro fusível, uma porta fecha repentinamente.

### Exemplo 3

Após coletar o segundo fusível, a criatura aparece no final de um corredor e inicia uma perseguição.

Esses eventos poderão ser ativados através de objetos com `BoxCollider` configurados como Trigger.

## Interface

A interface será simples.

Durante a partida será exibido o número de fusíveis coletados.

Exemplo:

```text
Fusíveis: 1 / 3
```

Quando todos forem coletados:

```text
Fusíveis: 3 / 3

Saída liberada!
```

Também poderão ser apresentadas mensagens de interação, como:

```text
[E] Coletar fusível
```

ou:

```text
[E] Abrir porta
```

## Cenas do Projeto

O projeto poderá possuir três cenas principais:

### MainMenu

Menu inicial contendo:

- Jogar.
- Sair.

### Game

Cena principal do jogo.

Contém:

- Jogador.
- Cenário.
- Inimigo.
- Fusíveis.
- Porta de saída.
- Interface.
- Eventos de susto.

### GameOver

Tela exibida quando o jogador é alcançado pela criatura.

Poderá conter:

- Reiniciar.
- Voltar ao menu.

A vitória também poderá ser exibida através de uma tela simples dentro da própria cena ou em uma cena separada.

## Estrutura Inicial de Pastas

```text
Assets
│
├── Animations
│
├── Audio
│   ├── Ambient
│   ├── Enemy
│   └── Scares
│
├── Materials
│
├── Models
│
├── Prefabs
│   ├── Enemy
│   ├── Fuse
│   ├── Doors
│   └── Lights
│
├── Scenes
│   ├── MainMenu
│   ├── Game
│   └── GameOver
│
├── Scripts
│   ├── Player
│   ├── Enemy
│   ├── Interaction
│   └── Game
│
└── UI
```

## Scripts Previstos

Alguns scripts que poderão ser utilizados:

```text
PlayerMovement.cs
PlayerInteraction.cs
Flashlight.cs

EnemyAI.cs

FusePickup.cs
Door.cs
ScareTrigger.cs

GameManager.cs
```

A quantidade de scripts poderá mudar conforme o desenvolvimento.

## Tecnologias

O projeto utilizará:

- Unity.
- C#.
- Unity Input System ou sistema de entrada padrão.
- NavMesh.
- Unity UI.
- Sistema de iluminação da Unity.
- Sistema de áudio da Unity.

## Assets

Para reduzir o tempo de desenvolvimento, poderão ser utilizados assets gratuitos.

Possíveis tipos de assets:

- Corredores.
- Salas.
- Portas.
- Objetos de cenário.
- Materiais.
- Sons ambientes.
- Efeitos sonoros.
- Modelo da criatura.

Os assets utilizados deverão respeitar suas respectivas licenças.

## Direção visual atual

- Hospital abandonado com corredores e salas pequenas.
- Luz ambiente muito baixa; a lanterna revela o cenário e os objetos próximos.
- Poucas luminárias de emergência, com falhas ocasionais.
- Portas entreabertas e criatura vista brevemente antes da perseguição.
- Bordas da tela escurecidas para reforçar a visão limitada.

## Escopo do Projeto

### Incluído

- Um mapa pequeno.
- Um jogador.
- Um inimigo.
- Três itens coletáveis.
- Uma porta de saída.
- Lanterna.
- Corrida.
- Perseguição.
- Eventos de susto.
- Interface básica.
- Vitória e derrota.

### Fora do Escopo

Para manter o projeto simples, não serão implementados:

- Multiplayer.
- Sistema de combate.
- Armas.
- Inventário complexo.
- Crafting.
- Vários tipos de inimigos.
- Geração procedural.
- Sistema complexo de vida.
- Várias fases.
- História extensa.
- Cutscenes complexas.

## Ordem de Desenvolvimento

A implementação poderá seguir esta ordem:

1. Criar o projeto na Unity.
2. Implementar movimentação em primeira pessoa.
3. Criar o mapa básico.
4. Implementar interação com objetos.
5. Criar os três fusíveis.
6. Criar o contador de fusíveis.
7. Criar a porta de saída.
8. Configurar o NavMesh.
9. Adicionar o inimigo.
10. Implementar a perseguição.
11. Criar a condição de derrota.
12. Criar a condição de vitória.
13. Adicionar eventos de susto.
14. Adicionar sons.
15. Ajustar iluminação.
16. Criar o menu principal.
17. Realizar testes e correções.

## Resultado Esperado

Ao final do projeto, deverá existir um jogo curto de terror em primeira pessoa no qual o jogador explora um hospital abandonado, encontra três fusíveis e tenta escapar enquanto é perseguido por uma criatura.

O foco do projeto será demonstrar a utilização de recursos da Unity como movimentação, colisões, interação, interface, iluminação, áudio e inteligência artificial simples utilizando NavMesh.
