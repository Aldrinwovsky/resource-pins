# Roadmap — Resource Pins

## v1.1 (atual)
- [x] Pins no overlay, canto superior direito (estilo contador de FPS)
- [x] Ícone por recurso na barra de tarefas, visível só durante o uso
- [x] Câmera, microfone, localização e captura de tela (WGC) via ConsentStore
- [x] Estado aceso/apagado com opacidade — só acende em uso ativo real
- [x] Tooltip com o(s) app(s) consumindo o recurso
- [x] Filtro de entradas órfãs do registro (exige processo vivo)
- [x] Ícone de guardião (escudo + olho), menu de bandeja, log de diagnóstico
- [x] Instalador Windows (Inno Setup), self-contained, com autostart opcional

## v2 (planejado)
- [ ] **Bloquear recurso por app** direto do pin. Ponto em aberto: a revogação em `HKLM` exige elevação, então provavelmente será um atalho para as configurações de privacidade do Windows no caso não-elevado.
- [ ] **Histórico de uso** — linha do tempo de quando cada app usou cada recurso.
- [ ] **Notificação** quando um app usa câmera/microfone pela primeira vez.
- [ ] **Configurações na interface** — escolher recursos monitorados, canto da tela, tamanho e opacidade dos pins.
- [ ] **Lista de apps confiáveis** que não acendem o pin (ex.: o software de reunião que você usa o dia todo).

## Investigar
- [ ] **Detecção de captura por DXGI Desktop Duplication** (OBS e afins), hoje invisível para o ConsentStore. Caminhos possíveis: ETW ou heurística por processo. Só vale se der para fazer sem elevação e sem falso positivo — "OBS aberto" não é o mesmo que "OBS gravando".
- [ ] Suporte a múltiplos monitores (pin na tela ativa ou em cada uma).
- [ ] Pins adicionais: acesso a pastas sensíveis (`broadFileSystemAccess`), USB, HID.
