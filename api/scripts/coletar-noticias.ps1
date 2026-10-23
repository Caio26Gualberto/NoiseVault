param(
    [string]$Api = "https://api.noisevault.net",
    [double]$PausaSegundos = 1.5,
    [switch]$Configurar,
    [switch]$Agendar
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$tempoLimiteApi = 60

$Api = $Api.TrimEnd("/")
$pasta = Join-Path $env:LOCALAPPDATA "NoiseVault"
$arquivoCredencial = Join-Path $pasta "noticias-credencial.xml"
$arquivoLog = Join-Path $pasta "noticias.log"
$sitemapUrl = "https://whiplash.net/sitemap_ultimas.xml"
$agente = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36"
$nomeTarefa = "Noise Vault - coletar notícias"
$utf8 = New-Object System.Text.UTF8Encoding $false

New-Item -ItemType Directory -Force $pasta | Out-Null

if ((Test-Path $arquivoLog) -and (Get-Item $arquivoLog).Length -gt 1MB) {
    Move-Item $arquivoLog "$arquivoLog.old" -Force
}

function Escrever-Log([string]$texto) {
    $linha = "{0:yyyy-MM-dd HH:mm:ss} {1}" -f (Get-Date), $texto
    Write-Host $linha
    [IO.File]::AppendAllText($arquivoLog, $linha + "`r`n", $utf8)
}

function Baixar-Whiplash([string]$url) {
    $temporario = [IO.Path]::GetTempFileName()
    try {
        $status = & curl.exe -s --max-time 20 -A $agente `
            -H "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8" `
            -H "Accept-Language: pt-BR,pt;q=0.9" `
            -o $temporario -w "%{http_code}" $url
        $corpo = [IO.File]::ReadAllText($temporario, $utf8)
        $bloqueado = $status -eq "403" -or $status -eq "429" -or $corpo.Contains("Just a moment...")
        return @{ Status = "$status"; Corpo = $corpo; Bloqueado = $bloqueado }
    }
    finally {
        Remove-Item $temporario -ErrorAction SilentlyContinue
    }
}

function Entrar {
    $corpo = @{ email = $credencial.UserName; password = $credencial.GetNetworkCredential().Password } | ConvertTo-Json -Compress
    $resposta = Invoke-RestMethod -Method Post -Uri "$Api/api/auth/login" -ContentType "application/json; charset=utf-8" -Body $utf8.GetBytes($corpo) -TimeoutSec $tempoLimiteApi
    $script:token = $resposta.data.tokens.token
}

function Chamar-Api([string]$caminho, [hashtable]$corpo) {
    $bytes = $utf8.GetBytes(($corpo | ConvertTo-Json -Compress))
    for ($tentativa = 1; ; $tentativa++) {
        try {
            return Invoke-RestMethod -Method Post -Uri "$Api$caminho" -Headers @{ Authorization = "Bearer $script:token" } -ContentType "application/json; charset=utf-8" -Body $bytes -TimeoutSec $tempoLimiteApi
        }
        catch {
            $status = 0
            if ($_.Exception.Response) {
                $status = [int]$_.Exception.Response.StatusCode
                $_.Exception.Response.Dispose()
            }
            if ($status -eq 401 -and $tentativa -eq 1) {
                Entrar
                continue
            }
            if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
                try { return $_.ErrorDetails.Message | ConvertFrom-Json } catch { }
            }
            throw
        }
    }
}

if ($Agendar) {
    $argumentos = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$PSCommandPath`" -Api `"$Api`""
    $acao = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $argumentos
    $deHora = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Hours 1)
    $configuracao = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Hours 1)
    Register-ScheduledTask -TaskName $nomeTarefa -Action $acao -Trigger $deHora -Settings $configuracao -Force | Out-Null
    Write-Host "Tarefa '$nomeTarefa' agendada de hora em hora. Log em $arquivoLog"
    if (-not (Test-Path $arquivoCredencial)) {
        Write-Host "Ainda falta a credencial: rode o script uma vez com -Configurar."
    }
    exit 0
}

try {
    if ($Configurar) {
        $credencial = Get-Credential -Message "Conta do Noise Vault com a permissão News.Import ($Api)"
        $credencial | Export-Clixml -Path $arquivoCredencial
    }
    elseif (-not (Test-Path $arquivoCredencial)) {
        Escrever-Log "Sem credencial salva. Rode uma vez com -Configurar."
        exit 1
    }
    $credencial = Import-Clixml -Path $arquivoCredencial

    $sitemap = Baixar-Whiplash $sitemapUrl
    if ($sitemap.Bloqueado) {
        Escrever-Log "BLOQUEADO no sitemap (HTTP $($sitemap.Status)). Tenta de novo na próxima execução."
        exit 2
    }
    if ($sitemap.Status -ne "200") {
        Escrever-Log "Sitemap indisponível (HTTP $($sitemap.Status))."
        exit 1
    }

    Entrar
    $resposta = Chamar-Api "/api/admin/news/import/pending" @{ sitemap = $sitemap.Corpo }
    if (-not $resposta.isSuccess) {
        Escrever-Log "A API recusou o sitemap: $($resposta.code)"
        exit 1
    }

    $pendentes = @($resposta.data.pending)
    Escrever-Log "Início: links=$($resposta.data.links) pendentes=$($pendentes.Count)"
    $adicionadas = 0
    $falhas = 0
    $bloqueado = $false

    for ($i = 0; $i -lt $pendentes.Count; $i++) {
        $link = $pendentes[$i]
        if ($i -gt 0) { Start-Sleep -Milliseconds ([int]($PausaSegundos * 1000)) }

        $pagina = Baixar-Whiplash $link.url
        if ($pagina.Bloqueado) {
            Escrever-Log "BLOQUEADO na matéria $($link.externalId) (HTTP $($pagina.Status)). Para aqui."
            $bloqueado = $true
            break
        }
        if ($pagina.Status -ne "200") {
            Escrever-Log "Matéria $($link.externalId) indisponível (HTTP $($pagina.Status))."
            $falhas++
            continue
        }

        $importada = Chamar-Api "/api/admin/news/import" @{ url = $link.url; html = $pagina.Corpo }
        if (-not $importada.isSuccess) {
            Escrever-Log "Matéria $($link.externalId) recusada pela API: $($importada.code)"
            $falhas++
        }
        elseif ($importada.data.added) {
            $adicionadas++
        }

        $situacao = if (-not $importada.isSuccess) { "recusada" } elseif ($importada.data.added) { "adicionada" } elseif ($importada.data.alreadyExists) { "já existia" } else { "antiga demais" }
        Write-Host ("[{0}/{1}] {2} {3}" -f ($i + 1), $pendentes.Count, $link.externalId, $situacao)
    }

    Escrever-Log "Fim: links=$($resposta.data.links) pendentes=$($pendentes.Count) adicionadas=$adicionadas falhas=$falhas bloqueado=$bloqueado"
    if ($bloqueado) { exit 2 }
}
catch {
    Escrever-Log "ERRO: $($_.Exception.Message)"
    exit 1
}
