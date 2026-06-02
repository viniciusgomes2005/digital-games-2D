# Yokai Ascent

Link do video SPRINT 3:

Yokai Ascent e um platformer 2D vertical em URP. A protagonista Asa sobe uma caverna dominada por yokais ate alcancar o sol, usando pulos, ataques e a energia Yokai para desbloquear combos mais fortes.

## Controles

PC/Editor:
- `A` / seta esquerda: mover para a esquerda
- `D` / seta direita: mover para a direita
- `S`: abaixar/descer plataforma, quando a cena suportar
- `Espaco`: pular / pulo duplo
- `J`: atacar

Mobile/WebGL touch:
- Botao esquerdo: mover para a esquerda enquanto pressionado
- Botao direito: mover para a direita enquanto pressionado
- Botao `Pular`: pulo com toque unico
- Botao `Atacar`: ataque com toque unico
- Botao `v`: entrada de baixo enquanto pressionado
- Botao `II`: pausa

## Mecanicas principais

- Platformer vertical com camera seguindo a Asa.
- Inimigo yokai/sapo com patrulha, ataque e estado de dano.
- Barra de energia Yokai sincronizada com progressao.
- Ataque 1 sempre disponivel.
- Combo 2 desbloqueia quando a energia Yokai passa do primeiro limiar.
- Combo 3 desbloqueia quando a energia Yokai passa do segundo limiar.
- Ataque aereo preservado, com mergulho durante o golpe.
- Telas de vitoria e derrota podem ser conectadas ao `GameStateController`.

## Adaptacao mobile

Scripts adicionados:
- `Assets/Scripts/MobileInputController.cs`: centraliza input touch.
- `Assets/Scripts/MobileButton.cs`: botoes touch com feedback visual.
- `Assets/Scripts/MobileControlsBootstrap.cs`: cria a camada `MobileControls` em runtime quando necessario.
- `Assets/Scripts/MobileRuntimeInitializer.cs`: instala os controladores mobile na cena `Main`.
- `Assets/Scripts/PauseMenuController.cs`: pausa, continuar, reiniciar fase e voltar ao menu.
- `Assets/Scripts/GameStateController.cs`: estados Playing, Victory e Defeat, respawn por queda e botoes de fim de jogo.
- `Assets/Scripts/GameStateTrigger.cs`: trigger generico para vitoria, derrota ou respawn.
- `Assets/Scripts/RespawnController.cs`: checkpoints e zonas de respawn por trigger.
- `Assets/Scripts/MobilePerformanceSettings.cs`: FPS alvo, particulas e audio 2D para musica.

Scripts adaptados:
- `PlayerController`: agora combina teclado e `MobileInputController` para movimento, pulo e entrada de baixo.
- `PlayerCombat`: agora combina tecla `J` e botao mobile de ataque.
- `MenuController`: garante `Time.timeScale = 1` antes de carregar a cena.
- `YokaiEnergyBarController`: limites de particulas mais seguros para mobile.

## Como testar no Editor

1. Abra a cena `Assets/Scenes/Menu.unity`.
2. Aperte Play.
3. Clique em `PLAY`.
4. Na cena `Main`, teste teclado e botoes touch na tela.
5. Segure esquerda/direita para mover Asa, toque em `Pular` e `Atacar`.
6. Toque em `II` para pausar, continuar, reiniciar ou voltar ao menu.
7. Para testar respawn, faca Asa cair abaixo do limite configurado no `GameStateController`.

## Ajustes manuais recomendados no Unity Inspector

- Confirmar que `Menu` e `Main` estao em Build Profiles/Scenes In Build.
- Se ja existirem imagens finais de vitoria/derrota, arraste-as para `victoryPanel` e `defeatPanel` do `GameStateController`.
- Criar um GameObject `RespawnPoint` no inicio da fase e usar `RespawnController` com `setCheckpointOnStart`.
- Adicionar `GameStateTrigger` no collider do sol/fim da fase com resultado `Victory`.
- Adicionar `GameStateTrigger` em uma DeathZone, se a equipe preferir derrota por collider em vez de respawn por `deathY`.
- Ajustar `deathY` no `GameStateController` para ficar abaixo da ultima plataforma visivel.
- Refinar posicao, tamanho e alpha dos botoes em `Canvas/MobileControls`, se preferir salvar a UI na cena em vez da criacao runtime.
- Conferir o Canvas Scaler: Scale With Screen Size, Reference Resolution 1920 x 1080, Match 0.5.
- Conferir `AudioSource` de musica com Spatial Blend 0.
- Revisar compressao de imagens grandes em Android/WebGL.

## Build Android/APK

1. Instale Android Build Support no Unity Hub, se ainda nao estiver instalado.
2. Em File > Build Profiles/Build Settings, selecione Android e use Switch Platform.
3. Em Player Settings, confira:
   - Product Name: `Yokai Ascent`
   - Orientation: Landscape
   - Package Name: `com.yokaiascent.game`
   - Minimum API Level compativel com a disciplina/dispositivo
4. Confirme as cenas `Menu` e `Main` no build.
5. Gere o APK em Build.
6. Teste em um celular real antes da entrega.

## Build WebGL mobile para itch.io

1. Em Build Profiles/Build Settings, selecione WebGL e use Switch Platform.
2. Use resolucao landscape/mobile, por exemplo 960 x 540.
3. Gere a build WebGL.
4. Zipe com `index.html`, `Build/` e `TemplateData/` na raiz do zip.
5. No itch.io, marque o arquivo WebGL como playable in browser.
6. Anexe o APK como arquivo separado.

## Creditos

Integrantes:
- Preencher nomes do grupo.

Assets:
- Preservar e completar fontes dos sprites, tiles, efeitos, imagens de vitoria/derrota e outros assets externos.

Musica e audio:
- Preencher creditos da trilha e efeitos sonoros.

Imagens/IA:
- Registrar ferramentas, prompts ou fontes, se imagens geradas por IA tiverem sido usadas.

Link itch.io:
- Preencher quando a pagina estiver publicada.
