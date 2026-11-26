# Escape the Dark

Jogo curto de terror em primeira pessoa para Unity 6000.6.0f1. Investigue nove salas de um hospital abandonado, encontre três fusíveis e instale-os no quadro da recepção para restaurar os circuitos e acionar a saída.

O hospital fica quase todo no escuro. Sem a lanterna, a visibilidade cai bastante; algumas luminárias ainda permitem reconhecer pontos próximos. A recepção tem balcão e cadeiras de espera; a farmácia, uma reserva de emergência; a observação, uma pista de transferências; a enfermaria, um armário com teclado; e a cirurgia, um equipamento que precisa ser isolado. Pertences abandonados, cortinas e fichas curtas contam o que aconteceu durante a evacuação. A criatura surge após o segundo fusível, patrulha, investiga passos e persegue o jogador quando o vê. Correr faz mais barulho; a lanterna acesa permite que ela enxergue de mais longe.

## Objetivos e progressão

Você começa com a lanterna descarregada. Tente ligá-la com **F**: o interruptor faz um clique, e a mensagem indica que falta uma pilha. Procure a reserva na bandeja do balcão da recepção, sob uma pequena luz de emergência, e pressione **E** para pegá-la. A pilha é instalada e a lanterna acende. Essa pilha dura a partida inteira.

Depois disso, o quadro da recepção apresenta um plano de contingência com três circuitos. Encontrar as peças e instalar os fusíveis são ações distintas; a saída só recebe energia após as três instalações.

1. Consulte o quadro e procure a reserva no balcão da **farmácia**.
2. Volte ao quadro para restaurar a **recepção**. Essa ação também abre a passagem de serviço entre a ala central e a manutenção.
3. Leia a ficha de transferência na **observação** e use sua sequência no teclado do armário da **enfermaria**. A pista fica registrada na interface do teclado depois da leitura.
4. Recolha o segundo fusível. A criatura passa a patrulhar o hospital. Instale a peça no quadro para restaurar a enfermaria.
5. Na **cirurgia**, mantenha **E** pressionado por três segundos para isolar o equipamento e retirar a última peça. Soltar a tecla ou desviar o olhar interrompe a operação.
6. Instale a última peça no quadro. Use o atalho para chegar à **manutenção** e mantenha **E** pressionado por dois segundos na porta de saída.

Cada circuito restaura pequenas áreas de iluminação, preservando a necessidade da lanterna. O atalho permite novas rotas de fuga para o jogador e a criatura. Fichas e teclado pausam a partida, incluindo o agente de navegação da criatura. Antes da perseguição, a primeira coleta provoca falhas de luz, um som distante e o deslocamento discreto de uma cortina.

O menu **Escape The Dark > Validar percurso e objetivos** executa um roteiro em Play Mode com interações por raycast e teclado: testa a lanterna sem pilha, coleta a reserva, liga e desliga o feixe, tenta ações fora de ordem, testa códigos incorretos, coleta e instala as peças, verifica a redução da rota pelo atalho e a travessia real de um agente, interrompe e conclui a retirada da última peça, aciona a saída e reinicia a partida, incluindo a pilha inicial. Capturas e resultados ficam em `docs/scenario-review`.

Prévias da progressão: [quadro de emergência](docs/scenario-review/01-quadro.png), [ficha da observação](docs/scenario-review/04-ficha-observacao.png), [armário aberto](docs/scenario-review/06-armario-aberto.png), [equipamento da cirurgia](docs/scenario-review/07-equipamento-cirurgia.png) e [quadro restaurado](docs/scenario-review/08-quadro-restaurado.png).

Prévias da introdução: [recepção sem pilha](docs/scenario-review/00-recepcao-sem-pilha.png), [pilha no balcão](docs/scenario-review/00-pilha-no-balcao.png) e [lanterna funcionando](docs/scenario-review/00-lanterna-com-pilha.png).

Prévias atuais de iluminação: [recepção](docs/lighting-review/01-recepcao.png), [triagem](docs/lighting-review/02-triagem.png), [isolamento](docs/lighting-review/04-isolamento.png), [lanterna ligada](docs/lighting-review/05-criatura.png), [lanterna desligada](docs/lighting-review/06-lanterna-desligada.png) e [perto da parede](docs/lighting-review/08-proximo-da-parede.png).

## Iluminação e lanterna

A lanterna começa sem pilha e só emite luz depois de recolher a reserva da recepção. Não há consumo ou pilhas de reposição durante a partida. O feixe usa uma textura de projeção gerada em memória para formar um núcleo mais intenso e um halo amplo com bordas suaves. A luz tem tonalidade branca levemente quente, queda com a distância e sombras completas. O feixe acompanha o olhar com um atraso curto; perto das paredes, o objeto recua e a origem da luz é mantida do lado do jogador. A lente permanece visível quando apagada, com a emissão desligada. A tecla **F** aciona uma transição curta com um clique mecânico.

A exposição da câmera é fixa: desligar a lanterna remove a iluminação do feixe, sem escurecer artificialmente as outras fontes do hospital. A luz e os reflexos do céu da cena padrão foram removidos. Uma luz indireta fria e muito fraca permite distinguir silhuetas e passagens sem a lanterna; poucas luminárias de emergência deixam pequenas áreas visíveis. A lanterna continua necessária para enxergar os detalhes, ler as placas comuns e identificar os fusíveis; apenas a sinalização da saída mantém brilho próprio. Apenas as luminárias com defeito apresentam falhas ocasionais. A lanterna não pisca automaticamente por proximidade da criatura.

O menu **Escape The Dark > Gerar previas de iluminacao** verifica também o interruptor pela tecla F, a exposição fixa, a projeção do feixe e o recuo junto à parede. Compara as nove salas com a mesma câmera e a lanterna ligada/desligada, mede o brilho da imagem renderizada e grava os resultados em `docs/lighting-review/visibility.json`. As capturas `room-01-on.png` a `room-09-off.png` permitem conferir cada comparação. Para testar em uma cópia do projeto e gravar em outro diretório, execute `VisualReview.RunLightingBatch` com o argumento `-lightingReviewOutput` seguido do caminho desejado.

## Apresentação visual

Os nove setores têm placas, cores próprias, faixas nas paredes, tubulação, avisos, infiltrações e prontuários abandonados. Farmácia e manutenção recebem prateleiras de suprimentos; as áreas clínicas têm painéis de atendimento. Arandelas e iluminação localizada ajudam a reconhecer as salas, mantendo o ambiente escuro. A cenografia adicional não adiciona colisões nem altera as rotas do NavMesh.

A câmera suaviza a entrada e a saída do balanço dos passos e apresenta respiração discreta ao parar. A lanterna tem empunhadura detalhada e acompanha o olhar com um pequeno atraso visual; o campo de visão abre levemente durante a corrida. Os controles `cameraMotion` e `flashlightMotion`, no componente `PlayerMotor` durante o Play Mode, permitem reduzir esses efeitos até zero. A criatura usa os clips de repouso e caminhada do próprio pacote, mesclados conforme seu deslocamento real, sem o ciclo de ataques e morte da cena de demonstração. Velocidades, gravidade, detecção e condições de vitória permanecem iguais.

Para conferir o resultado, abra `Game.unity` e pressione **Play**. O menu **Escape The Dark > Gerar previas visuais** executa uma revisão em Play Mode, verifica a ausência de colisores novos na cenografia, a navegação da criatura e a resposta visual à corrida, e grava capturas em `docs/visual-review`. Essa revisão encerra o Play Mode ao terminar.

## Como jogar

1. Abra o projeto na Unity 6000.6.0f1.
2. Para reproduzir os visuais e sons das capturas, siga as instruções de importação abaixo.
3. Abra `Assets/Scenes/Game.unity` e pressione **Play**.
4. Clique em **Jogar**, teste a lanterna com **F** e procure a pilha no balcão da recepção. Depois, examine o quadro de emergência. A criatura aparece após o segundo fusível.

| Tecla | Ação |
| --- | --- |
| WASD | Mover |
| Mouse | Olhar |
| Shift | Correr |
| F | Ligar ou desligar a lanterna |
| E | Recolher a pilha, ler fichas e recolher ou instalar fusíveis |
| E mantido | Isolar o equipamento da cirurgia ou acionar a saída |
| 0–9 / Enter / Backspace | Digitar, confirmar ou corrigir o código do armário |
| E ou Esc em uma ficha | Fechar a leitura |
| Esc | Pausar ou fechar o teclado |
| R | Reiniciar após vitória ou derrota |

## Importar os pacotes da Unity Asset Store

Os arquivos originais dos quatro pacotes são distribuídos pela Unity Asset Store. Adicione os pacotes à sua conta da Unity pelos links abaixo.

- Piso, paredes, camas, caixas médicas e luminárias. [Hospital Horror Pack](https://assetstore.unity.com/packages/3d/environments/pbr-hospital-horror-pack-free-80117)
- Visual da criatura, com materiais convertidos para URP em tempo de execução. [Creep Horror Creature](https://assetstore.unity.com/packages/3d/characters/creatures/creep-horror-creature-244853)
- Passos sobre piso de ladrilho. [Footsteps - Essentials](https://assetstore.unity.com/packages/audio/sound-fx/foley/footsteps-essentials-189879)
- Efeitos de susto e derrota. [FREE Gore Gloom Hits](https://assetstore.unity.com/packages/audio/sound-fx/free-gore-gloom-hits-198113)

No Unity, abra o **Package Manager**, selecione **My Assets** e importe os quatro pacotes para as pastas padrão. Depois, escolha **Escape The Dark > Aplicar assets do hospital** no menu do editor. O comando gera os materiais do piso e das paredes e vincula os modelos e os sons à cena `Game.unity`.

O mapa, os objetos e a navegação são montados em tempo de execução por `Assets/Scripts/GameDirector.cs`. O projeto usa **URP**, **Input System** e **AI Navigation**. A cena já está na lista de cenas para build.
