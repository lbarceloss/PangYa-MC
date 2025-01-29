# Descrição

Este projeto em C# implementa um gerenciador de processos para detectar e manipular o mutex do jogo PangYa (ProjectG.exe). Ele permite encontrar, fechar e reiniciar a instância do jogo caso um mutex esteja impedindo sua reexecução.

## Principais Funcionalidades

* Permite selecionar e iniciar um executável (.exe) de maneira manual.
* Verifica se o jogo ProjectG.exe está em execução.
* Tenta localizar e fechar o mutex do jogo para evitar conflitos de instâncias.
* Utiliza chamadas nativas do Windows (kernel32.dll, ntdll.dll) para manipular processos e handles.
* Apresenta logs em tempo real com informações sobre o estado do processo e tentativas de fechamento do mutex.

## Como Utilizar

1. Selecione o executável do jogo através do botão correspondente.
2. Clique em Iniciar para iniciar o jogo ou fechar mutexes caso já esteja rodando.
3. Acompanhe os logs para verificar se o mutex foi fechado com sucesso.

## Tecnologia Utilizada

* C# / .NET WPF - Interface gráfica moderna para facilitar a interação.
* Win32 API - Utiliza funções nativas do Windows para acessar informações de processos.
* Manipulação de Handles - Interage diretamente com handles para detectar e encerrar mutexes.

## Aviso Legal

Este projeto foi desenvolvido com finalidades exclusivamente educacionais. Manipular mutexes de processos de terceiros pode ser proibido por Termos de Uso de determinados softwares. Use por sua conta e risco.

## Licença

Este projeto é fornecido "como está", sem garantias. Você pode modificá-lo e adaptá-lo conforme sua necessidade.

## Media
[YouTube](https://youtu.be/-n3hHLfq_qU)
