# Roadmap — Resource Pins

## v1 (atual)
- [x] Pins topmost no canto superior direito (estilo contador de FPS)
- [x] Câmera, microfone, localização, captura de tela via ConsentStore
- [x] Estado aceso/apagado com opacidade (só acende em uso ativo real)
- [x] Tooltip com o(s) app(s) consumindo o recurso
- [x] Ícone de guardião (escudo + olho) na bandeja
- [x] Instalador Windows (Inno Setup) com autostart

## v2 (planejado)
- [ ] **Bloquear recurso por app**: a partir do pin/tooltip, revogar o acesso do app ao recurso (via `ConsentStore` deny / configurações de privacidade do Windows). Exige decidir UX (clique no pin abre lista de apps com botão "bloquear") e privilégio necessário (HKLM pede elevação).
- [ ] Histórico de uso: log de quando cada app usou cada recurso (linha do tempo).
- [ ] Notificação toast quando um app usa câmera/mic pela primeira vez.
- [ ] Configurações: escolher quais pins monitorar, canto da tela, tamanho/opacidade.
- [ ] Lista de apps "confiáveis" que não acendem o pin.

## Ideias (sem compromisso)
- Suporte a múltiplos monitores (pin em cada tela ou na tela ativa).
- Pin de rede/VPN e de acesso a pastas sensíveis (documentsLibrary, broadFileSystemAccess).
