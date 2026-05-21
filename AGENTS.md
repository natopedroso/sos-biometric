# SOS Biometric - Instrucoes para agentes

Projeto WinForms de biometria (Digital Persona 4500) usado como modulo externo pelo SOS Electron.

## Objetivo de integracao
Este executavel deve funcionar em modo visual e em modo CLI para automacao pelo Electron.

## Caminho padrao de instalacao
- `C:\\sos-biometric\\biometric.exe`

## Contrato CLI obrigatorio
Comandos:
- `biometric.exe verify --user-id <id> --json`
- `biometric.exe enroll --user-id <id> --json`

Retorno em `stdout` (JSON unico):
```json
{
  "ok": true,
  "status": "success",
  "message": "opcional"
}
```

Status aceitos:
- `success`
- `failed`
- `cancelled`
- `timeout`
- `error`

## Regras de dados
- Persistir template por usuario (chave principal: `user-id`).
- Nao vazar dados sensiveis no log.
- Em erro de leitura/dispositivo, retornar `ok=false` e `status=error` com `message` util.

## Fluxos
### enroll
- Captura e salva template vinculado ao `user-id`.
- Retorna `success` quando gravado.

### verify
- Carrega template do `user-id` e valida digital.
- Retorna `success` quando houver match.

## Compatibilidade
- Se executado sem argumentos, abrir modo visual (template app).
- Se executado com argumentos CLI, finalizar com resposta JSON para consumo do Electron.

## Build e publicacao
- Script recomendado: `build-and-publish.ps1`
- Exemplo: `powershell -ExecutionPolicy Bypass -File .\\build-and-publish.ps1`
- Pre-requisito: Visual Studio Build Tools com suporte a .NET desktop e `MSBuild.exe` disponivel.
