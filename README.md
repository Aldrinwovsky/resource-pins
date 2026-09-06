# Resource Pins

Indicadores de privacidade para Windows: pins minúsculos que mostram, de relance, **quais recursos sensíveis estão em uso agora** — e por qual aplicativo.

![Pins do Resource Pins: câmera e microfone acesos, localização e captura de tela apagados](docs/pins.png)

| Pin | Recurso | Cor quando aceso |
|-----|---------|------------------|
| 📷 | Câmera | Verde |
| 🎤 | Microfone | Vermelho |
| 📍 | Localização | Azul |
| 🖥️ | Captura de tela | Roxo |

Os pins aparecem em dois lugares, e você escolhe quais quer (menu da bandeja):

- **Overlay na tela** — quatro bolinhas de 16px no canto superior direito, estilo contador de FPS. Sempre visíveis: cinza translúcido quando ocioso, coloridas com brilho quando em uso.
- **Barra de tarefas** — um ícone na área de notificação por recurso, que aparece só enquanto aquele recurso está sendo usado.

Passe o mouse em qualquer um dos dois para ver **qual aplicativo** está usando.

## O que ele detecta (e o que não detecta)

Esta é a parte importante, e a maioria das ferramentas parecidas não é honesta sobre ela.

O Windows mantém um registro (`CapabilityAccessManager\ConsentStore`) de quais aplicativos acessam cada recurso. Um app aparece como "em uso agora" quando tem `LastUsedTimeStart > 0` e `LastUsedTimeStop == 0`. O Resource Pins lê exatamente isso, a cada segundo — sem hooks, sem drivers, sem elevação, custo de CPU desprezível.

**Consequência direta:** o app só enxerga o que o Windows registra.

✅ **Detecta bem** — câmera e microfone de praticamente qualquer aplicativo (navegadores, Discord, Teams, OBS, jogos), e compartilhamento de tela feito pela API moderna **Windows.Graphics.Capture**: Teams, Discord, Meet, Chrome/Edge, Ferramenta de Captura.

❌ **Não detecta** — captura de tela feita por APIs de baixo nível como **DXGI Desktop Duplication**, que não passam pelo mecanismo de permissões do Windows. Na prática, isso inclui o **Display Capture do OBS Studio**: o OBS aparece corretamente nos pins de câmera e microfone, mas o pin de captura de tela permanece apagado enquanto ele grava sua tela por esse método. Isso é uma limitação da fonte de dados, não um bug — e não tem conserto sem partir para ETW ou driver.

Traduzindo para o uso real: confie no pin roxo para responder *"alguém está vendo minha tela numa chamada?"*. Não confie nele como detector de gravadores.

**Também vale saber:** o Windows 11 já mostra um indicador próprio de câmera/microfone na bandeja. O que este projeto acrescenta é ver os quatro recursos de uma vez, sempre à vista, sem precisar abrir nada.

## Instalação

Baixe o instalador em [Releases](../../releases) e execute.

O instalador **não é assinado digitalmente** (certificado de code signing custa algumas centenas de dólares por ano, o que não se justifica num utilitário gratuito). O SmartScreen vai exibir "O Windows protegeu o computador" — clique em **Mais informações → Executar assim mesmo**. Se preferir não confiar num binário de terceiro, compile você mesmo: são três arquivos de código e nenhuma dependência externa.

O app é instalado em `%LOCALAPPDATA%\Programs\ResourcePins`, sem exigir privilégios de administrador, e pode ser configurado para iniciar com o Windows.

## Compilar

```
dotnet publish -c Release
```

Requer o [.NET 10 SDK](https://dotnet.microsoft.com/download). A publicação é *self-contained* (win-x64), ou seja, o resultado roda em máquinas sem o .NET instalado.

Para gerar o instalador, com [Inno Setup 6](https://jrsoftware.org/isdl.php):

```
iscc installer.iss
```

## Uso

O ícone do guardião (escudo com olho) fica na bandeja do sistema — no Windows 11 ele nasce na área de ícones ocultos, atrás da setinha `^`; arraste-o para fora se quiser mantê-lo à vista. Clique com o botão direito para:

- **Mostrar na barra de tarefas** — liga/desliga os ícones por recurso
- **Mostrar pins na tela (overlay)** — liga/desliga as bolinhas do canto
- **Modo teste** — acende todos os pins, útil para conferir posicionamento
- **Abrir log de diagnóstico** — registro em `%APPDATA%\ResourcePins\log.txt`

**Limitação de exibição:** jogos em tela cheia *exclusiva* cobrem qualquer overlay comum. Em modo janela ou borderless o overlay funciona normalmente — e os ícones da barra de tarefas continuam valendo de qualquer forma.

## Privacidade

O app lê o registro local e a lista de processos em execução. Não envia nada para lugar nenhum, não faz conexões de rede, não grava histórico de uso. O único arquivo que ele escreve é o log de diagnóstico e as preferências, ambos em `%APPDATA%\ResourcePins`.

## Roadmap

Ver [ROADMAP.md](ROADMAP.md). O item principal da v2 é poder **bloquear** o acesso de um app a um recurso direto pelo pin.

## Licença

MIT — ver [LICENSE](LICENSE).
