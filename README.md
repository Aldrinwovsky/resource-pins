# Resource Pins

Guardião visual de privacidade para Windows. Pins minúsculos no canto superior direito da tela (estilo contador de FPS) mostram, em tempo real, quais recursos sensíveis estão **em uso ativo agora**:

| Pin | Recurso | Cor quando aceso |
|-----|---------|------------------|
| 📷 | Câmera | Verde |
| 🎤 | Microfone | Vermelho |
| 📍 | Localização | Azul |
| 🖥️ | Captura de tela | Roxo |

- **Apagado** (cinza translúcido): recurso ocioso.
- **Aceso** (cor + brilho): algum app está consumindo o recurso NESTE momento. Passe o mouse pra ver qual(is).

## Como funciona

O Windows registra em `HKCU/HKLM\...\CapabilityAccessManager\ConsentStore\<recurso>` quais apps acessam cada recurso. `LastUsedTimeStart > 0` com `LastUsedTimeStop == 0` significa "em uso agora". O app faz polling desse registro a cada 1s — sem hooks, sem drivers, custo ~zero.

O overlay é uma janela WPF topmost, transparente, sem borda, fora do alt-tab (`WS_EX_TOOLWINDOW`) e que nunca rouba foco (`WS_EX_NOACTIVATE`). O topmost é reafirmado a cada tick.

**Limitação conhecida:** jogos em fullscreen *exclusivo* cobrem qualquer overlay comum. Em borderless/janela funciona sempre.

## Build

```
dotnet publish -c Release
```

Requer .NET 10 SDK. Saída em `bin\Release\net10.0-windows\publish\ResourcePins.exe`.

## Instalador

Script Inno Setup em `installer.iss`:

```
iscc installer.iss
```

Gera `dist\ResourcePinsSetup.exe`. O instalador registra o app para iniciar com o Windows (chave Run do registro — visível em Gerenciador de Tarefas → Aplicativos de inicialização).

## Uso

Ícone do guardião (escudo + olho) fica na bandeja do sistema (área de ícones ocultos). Menu de contexto: **Modo teste** (acende todos os pins) e **Sair**.

Ver [ROADMAP.md](ROADMAP.md) para o que vem na v2.
