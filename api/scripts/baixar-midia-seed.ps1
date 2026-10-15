param(
    [string]$Destino = (Join-Path $env:LOCALAPPDATA "NoiseVault\seed-media"),
    [int]$DuracaoVideoSegundos = 75
)

$ErrorActionPreference = "Stop"
$agente = "NoiseVaultSeed/1.0 (seed de desenvolvimento; mailto:dev@noisevault.net)"
$commons = "https://upload.wikimedia.org/wikipedia/commons"
$thumb = "https://thumb.wikimedia.org/wikipedia/commons/thumb"

$fotos = @(
    @{ Nome = "show-1";              Url = "$thumb/c/cc/Metallica_live_at_Download_festival_2023.jpg/1920px-Metallica_live_at_Download_festival_2023.jpg"; Titulo = "Metallica live at Download festival 2023"; Licenca = "CC BY 4.0" }
    @{ Nome = "show-2";              Url = "$commons/9/92/Kreator_Bloodstock.jpg"; Titulo = "Kreator Bloodstock"; Licenca = "CC BY 2.0" }
    @{ Nome = "show-3";              Url = "$commons/2/2b/Slayer_-_band_-_live_2006.jpg"; Titulo = "Slayer - band - live 2006"; Licenca = "CC BY 2.0" }
    @{ Nome = "flyer-pogo";          Url = "$thumb/0/09/Slayer_-_pentagram_-_live_2006.jpg/1920px-Slayer_-_pentagram_-_live_2006.jpg"; Titulo = "Slayer - pentagram - live 2006"; Licenca = "CC BY 2.0" }
    @{ Nome = "backstage";           Url = "$thumb/f/fd/Kreator_%40_Rock_Hard_Festival_2015_04.jpg/1920px-Kreator_%40_Rock_Hard_Festival_2015_04.jpg"; Titulo = "Kreator @ Rock Hard Festival 2015 04"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pista-1";             Url = "$thumb/9/92/Metallica_Live_at_The_O2%2C_London%2C_England%2C_22_October_2017.jpg/1920px-Metallica_Live_at_The_O2%2C_London%2C_England%2C_22_October_2017.jpg"; Titulo = "Metallica Live at The O2, London, 22 October 2017"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pista-2";             Url = "$thumb/b/b0/Slayer_%40_Bogot%C3%A1_2017_%2834331922872%29.jpg/1920px-Slayer_%40_Bogot%C3%A1_2017_%2834331922872%29.jpg"; Titulo = "Slayer @ Bogotá 2017 (34331922872)"; Licenca = "CC BY 2.0" }
    @{ Nome = "archive-1999-1";      Url = "$commons/a/a5/Metallica_live_London_2003-12-19_corrected.jpg"; Titulo = "Metallica live London 2003-12-19 corrected"; Licenca = "Domínio público" }
    @{ Nome = "archive-1999-2";      Url = "$commons/0/08/Kreator_2005.jpg"; Titulo = "Kreator 2005"; Licenca = "Domínio público" }
    @{ Nome = "archive-1999-3";      Url = "$thumb/8/8d/Slayer_-_tom_araya_-_live_2006.jpg/1920px-Slayer_-_tom_araya_-_live_2006.jpg"; Titulo = "Slayer - tom araya - live 2006"; Licenca = "CC BY-SA 2.0" }
    @{ Nome = "archive-1980-ozzy";   Url = "$commons/5/58/Ozzy_Osbourne_Cardiff_1980.jpg"; Titulo = "Ozzy Osbourne Cardiff 1980"; Licenca = "CC BY-SA 2.0" }
    @{ Nome = "archive-2003-1";      Url = "$thumb/7/76/Korzus_-_2011.jpg/1920px-Korzus_-_2011.jpg"; Titulo = "Korzus - 2011"; Licenca = "CC BY 2.0" }
    @{ Nome = "archive-2003-2";      Url = "$commons/1/13/Marcello_Pompeu_-_Korzus.jpg"; Titulo = "Marcello Pompeu - Korzus"; Licenca = "CC BY 2.0" }
    @{ Nome = "archive-2016-1";      Url = "$commons/d/d7/Slayer_%40_Bogot%C3%A1_2017_%2833682177603%29.jpg"; Titulo = "Slayer @ Bogotá 2017 (33682177603)"; Licenca = "CC BY 2.0" }
    @{ Nome = "archive-2016-2";      Url = "$thumb/0/09/Tuska_20130629_-_Kreator_-_07.jpg/1920px-Tuska_20130629_-_Kreator_-_07.jpg"; Titulo = "Tuska 20130629 - Kreator - 07"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "archive-2008-galpao"; Url = "$thumb/b/ba/Metallica_at_Ippodromo_San_Siro%2C_Milan%2C_May_8th%2C_2019.jpg/1920px-Metallica_at_Ippodromo_San_Siro%2C_Milan%2C_May_8th%2C_2019.jpg"; Titulo = "Metallica at Ippodromo San Siro, Milan, May 8th, 2019"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "archive-2011-praca";  Url = "$thumb/3/3c/HF2022Megadeth_2.jpg/1920px-HF2022Megadeth_2.jpg"; Titulo = "HF2022Megadeth 2"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "support-foto-branca"; Url = "$thumb/2/26/Metallica_live_in_Bangalore_%286350326238%29.jpg/1920px-Metallica_live_in_Bangalore_%286350326238%29.jpg"; Titulo = "Metallica live in Bangalore (6350326238)"; Licenca = "CC BY 2.0" }
    @{ Nome = "pool-metallica-bangalore"; Url = "$thumb/8/87/Metallica_live_in_Bangalore_%286349587977%29.jpg/1920px-Metallica_live_in_Bangalore_%286349587977%29.jpg"; Titulo = "Metallica live in Bangalore (6349587977)"; Licenca = "CC BY 2.0" }
    @{ Nome = "pool-slayer-kerry-2006"; Url = "$thumb/0/07/Slayer_-_kerry_king_-_live_2006.jpg/1920px-Slayer_-_kerry_king_-_live_2006.jpg"; Titulo = "Slayer - kerry king - live 2006"; Licenca = "CC BY-SA 2.0" }
    @{ Nome = "pool-slayer-sonisphere"; Url = "$thumb/8/8e/Slayer_Sonisphere_2010_Warszawa.jpg/1920px-Slayer_Sonisphere_2010_Warszawa.jpg"; Titulo = "Slayer Sonisphere 2010 Warszawa"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-slayer-iceland";  Url = "$thumb/9/9e/Slayer_in_Iceland%2C_2018.jpg/1920px-Slayer_in_Iceland%2C_2018.jpg"; Titulo = "Slayer in Iceland, 2018"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-kreator-misje";   Url = "$commons/e/e3/Kreator_by_Christian_Misje.jpg"; Titulo = "Kreator by Christian Misje"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "pool-kreator-live";    Url = "$thumb/5/5d/Kreator_live.JPG/1920px-Kreator_live.JPG"; Titulo = "Kreator live"; Licenca = "Domínio público" }
    @{ Nome = "pool-kreator-tuska-23"; Url = "$thumb/a/ac/Tuska_20130629_-_Kreator_-_23.jpg/1920px-Tuska_20130629_-_Kreator_-_23.jpg"; Titulo = "Tuska 20130629 - Kreator - 23"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "pool-kreator-2024";    Url = "$thumb/3/34/Kreator_2024_2.jpg/1920px-Kreator_2024_2.jpg"; Titulo = "Kreator 2024 2"; Licenca = "CC0" }
    @{ Nome = "pool-kreator-rockhard-02"; Url = "$thumb/c/c7/Kreator_%40_Rock_Hard_Festival_2015_02.jpg/1920px-Kreator_%40_Rock_Hard_Festival_2015_02.jpg"; Titulo = "Kreator @ Rock Hard Festival 2015 02"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-sepultura-olinda-1"; Url = "$thumb/5/5f/Sepultura_Live_at_Olinda_Classic_Hall_2024_02.jpg/1920px-Sepultura_Live_at_Olinda_Classic_Hall_2024_02.jpg"; Titulo = "Sepultura Live at Olinda Classic Hall 2024 02"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-sepultura-olinda-2"; Url = "$thumb/7/76/Sepultura_Live_at_Olinda_Classic_Hall_2024.jpg/1920px-Sepultura_Live_at_Olinda_Classic_Hall_2024.jpg"; Titulo = "Sepultura Live at Olinda Classic Hall 2024"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-sepultura-rockstadt-1"; Url = "$thumb/2/26/Sepultura_-_Rockstadt_Extreme_Fest_2025_%2803.08.2025%29.02.jpg/1920px-Sepultura_-_Rockstadt_Extreme_Fest_2025_%2803.08.2025%29.02.jpg"; Titulo = "Sepultura - Rockstadt Extreme Fest 2025 .02"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-sepultura-rockstadt-2"; Url = "$thumb/f/f3/Sepultura_-_Rockstadt_Extreme_Fest_2025_%2803.08.2025%29.04.jpg/1920px-Sepultura_-_Rockstadt_Extreme_Fest_2025_%2803.08.2025%29.04.jpg"; Titulo = "Sepultura - Rockstadt Extreme Fest 2025 .04"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-sepultura-kisser-2007"; Url = "$thumb/9/92/Metalmania_2007_-_Sepultura_-_Andreas_Kisser_04.jpg/1920px-Metalmania_2007_-_Sepultura_-_Andreas_Kisser_04.jpg"; Titulo = "Metalmania 2007 - Sepultura - Andreas Kisser 04"; Licenca = "CC BY-SA 2.5" }
    @{ Nome = "pool-cavalera-2026";   Url = "$thumb/e/e7/Max_Cavalera_%E2%80%93_Cavalera_Conspiracy%2C_Tons_of_Rock_2026-1.jpg/1920px-Max_Cavalera_%E2%80%93_Cavalera_Conspiracy%2C_Tons_of_Rock_2026-1.jpg"; Titulo = "Max Cavalera - Cavalera Conspiracy, Tons of Rock 2026-1"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-megadeth-metalmania"; Url = "$thumb/8/86/Metalmania_2008_Megadeth_002.jpg/1920px-Metalmania_2008_Megadeth_002.jpg"; Titulo = "Metalmania 2008 Megadeth 002"; Licenca = "CC BY 3.0" }
    @{ Nome = "pool-mustaine-curitiba"; Url = "$thumb/d/d3/Dave_Mustaine_Show_em_Curitiba.jpg/1920px-Dave_Mustaine_Show_em_Curitiba.jpg"; Titulo = "Dave Mustaine Show em Curitiba"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-lemmy-warsaw";    Url = "$commons/e/e3/Lemmy_Kilmister_in_Warsaw_13-06-2006.jpg"; Titulo = "Lemmy Kilmister in Warsaw 13-06-2006"; Licenca = "Domínio público" }
    @{ Nome = "pool-ozzy-1984";       Url = "$commons/4/4d/Ozzy_in_1984.jpg"; Titulo = "Ozzy in 1984"; Licenca = "Domínio público" }
    @{ Nome = "pool-halford-1978";    Url = "$commons/2/26/Rob_Halford_Circa_1978-80.jpg"; Titulo = "Rob Halford Circa 1978-80"; Licenca = "CC0" }
    @{ Nome = "pool-exodus-thrashfest"; Url = "$thumb/1/19/Exodus_Garage_SB_Thrashfest.JPG/1920px-Exodus_Garage_SB_Thrashfest.JPG"; Titulo = "Exodus Garage SB Thrashfest"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "pool-exodus-holt";     Url = "$commons/1/1e/Exodus_-_Holt.jpg"; Titulo = "Exodus - Holt"; Licenca = "CC BY-SA 2.0" }
    @{ Nome = "pool-testament-1";     Url = "$thumb/5/55/Tuska_20130629_-_Testament_-_13.jpg/1920px-Tuska_20130629_-_Testament_-_13.jpg"; Titulo = "Tuska 20130629 - Testament - 13"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "pool-testament-2";     Url = "$thumb/4/40/Tuska_20130629_-_Testament_-_26.jpg/1920px-Tuska_20130629_-_Testament_-_26.jpg"; Titulo = "Tuska 20130629 - Testament - 26"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "pool-overkill-1";      Url = "$thumb/a/a4/Overkill_-_Jalometalli_2008_-_15.JPG/1920px-Overkill_-_Jalometalli_2008_-_15.JPG"; Titulo = "Overkill - Jalometalli 2008 - 15"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "pool-overkill-2";      Url = "$thumb/8/89/Overkill_-_Jalometalli_2008_-_01.JPG/1920px-Overkill_-_Jalometalli_2008_-_01.JPG"; Titulo = "Overkill - Jalometalli 2008 - 01"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "pool-anthrax";         Url = "$commons/0/07/Anthrax_%2828332792682%29.jpg"; Titulo = "Anthrax (28332792682)"; Licenca = "CC BY-SA 2.0" }
    @{ Nome = "pool-krisiun";         Url = "$thumb/b/b5/KRISIUN_EM_A%C3%87%C3%83O_.jpg/1920px-KRISIUN_EM_A%C3%87%C3%83O_.jpg"; Titulo = "KRISIUN EM AÇÃO"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-angra";           Url = "$thumb/1/18/AngraLiveFortaleza.jpg/1920px-AngraLiveFortaleza.jpg"; Titulo = "AngraLiveFortaleza"; Licenca = "CC BY 2.5" }
    @{ Nome = "pool-tormento";        Url = "$thumb/8/88/Banda_de_Thrash_Metal_Tormento.jpg/1920px-Banda_de_Thrash_Metal_Tormento.jpg"; Titulo = "Banda de Thrash Metal Tormento"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-clash-titans";    Url = "$thumb/b/b2/Clash_of_the_titans_%285956122041%29.jpg/1920px-Clash_of_the_titans_%285956122041%29.jpg"; Titulo = "Clash of the titans (5956122041)"; Licenca = "CC BY 2.0" }
    @{ Nome = "pool-crowd-tuska";     Url = "$thumb/7/77/Crowd_Tuska_2019.jpg/1920px-Crowd_Tuska_2019.jpg"; Titulo = "Crowd Tuska 2019"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-mosh-tuska";      Url = "$thumb/3/36/Women_Mosh_Pit_Tuska_2019.jpg/1920px-Women_Mosh_Pit_Tuska_2019.jpg"; Titulo = "Women Mosh Pit Tuska 2019"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "pool-mosh-1995";       Url = "$thumb/c/ca/Moshing_at_Pain_in_the_Grass_-_1995.jpg/1920px-Moshing_at_Pain_in_the_Grass_-_1995.jpg"; Titulo = "Moshing at Pain in the Grass - 1995"; Licenca = "CC BY-SA 3.0" }
)

$retratos = @(
    @{ Nome = "avatar-hetfield";  Url = "$commons/4/4b/James_Hetfield_2017.jpg"; Titulo = "James Hetfield 2017"; Licenca = "CC BY 2.0" }
    @{ Nome = "avatar-hammett";   Url = "$commons/6/6f/Kirk_Hammett_2017.jpg"; Titulo = "Kirk Hammett 2017"; Licenca = "CC BY 2.0" }
    @{ Nome = "avatar-ulrich";    Url = "$thumb/7/75/Lars_Ulrich_2016.jpg/1920px-Lars_Ulrich_2016.jpg"; Titulo = "Lars Ulrich 2016"; Licenca = "CC BY-SA 2.0" }
    @{ Nome = "avatar-araya";     Url = "$thumb/e/ea/Tom_Araya_-_Slayer_-_Riot_Fest_2019.jpg/1920px-Tom_Araya_-_Slayer_-_Riot_Fest_2019.jpg"; Titulo = "Tom Araya - Slayer - Riot Fest 2019"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "avatar-king";      Url = "$commons/0/09/Kerry_King%2C_2006.jpg"; Titulo = "Kerry King, 2006"; Licenca = "CC BY-SA 2.0" }
    @{ Nome = "avatar-mustaine";  Url = "$commons/5/51/Dave_Mustaine_2011_%28cropped%29.jpg"; Titulo = "Dave Mustaine 2011 (cropped)"; Licenca = "CC BY 2.0" }
    @{ Nome = "avatar-cavalera";  Url = "$thumb/d/db/Max_Cavalera_Sb.JPG/1920px-Max_Cavalera_Sb.JPG"; Titulo = "Max Cavalera Sb"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "avatar-kisser";    Url = "$commons/6/6c/Andreas_Kisser.jpg"; Titulo = "Andreas Kisser"; Licenca = "CC BY-SA 2.0" }
    @{ Nome = "avatar-petrozza";  Url = "$thumb/4/40/Mille_Petrozza_%28Kreator%29_au_Motocultor_en_2017.jpg/1920px-Mille_Petrozza_%28Kreator%29_au_Motocultor_en_2017.jpg"; Titulo = "Mille Petrozza (Kreator) au Motocultor en 2017"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "avatar-billy";     Url = "$thumb/c/c1/Chuck_Billy_Gelsenkirchen_RockHard_1019_%28cropped%29.jpg/1920px-Chuck_Billy_Gelsenkirchen_RockHard_1019_%28cropped%29.jpg"; Titulo = "Chuck Billy Gelsenkirchen RockHard 1019 (cropped)"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "avatar-halford";   Url = "$thumb/f/f6/Rob_Halford_au_Hellfest_2018.jpg/1920px-Rob_Halford_au_Hellfest_2018.jpg"; Titulo = "Rob Halford au Hellfest 2018"; Licenca = "CC BY-SA 4.0" }
    @{ Nome = "avatar-lemmy";     Url = "$thumb/5/55/Lemmy_Kilmister.jpg/1920px-Lemmy_Kilmister.jpg"; Titulo = "Lemmy Kilmister"; Licenca = "CC BY 2.0" }
    @{ Nome = "avatar-ozzy";      Url = "$thumb/e/eb/Ozzy_Osbourne.jpg/1920px-Ozzy_Osbourne.jpg"; Titulo = "Ozzy Osbourne"; Licenca = "CC BY-SA 2.0" }
    @{ Nome = "avatar-anselmo";   Url = "$commons/e/e1/Phil_Anselmo_2011_%28cropped%29.jpg"; Titulo = "Phil Anselmo 2011 (cropped)"; Licenca = "CC BY-SA 2.0" }
    @{ Nome = "avatar-dickinson"; Url = "$commons/c/cf/Bruce_Dickinson_2024.jpg"; Titulo = "Bruce Dickinson 2024"; Licenca = "CC0" }
    @{ Nome = "avatar-holt";      Url = "$thumb/4/4e/Gary_Holt_of_Slayer_performing_in_Austin%2C_Texas_2014.jpg/960px-Gary_Holt_of_Slayer_performing_in_Austin%2C_Texas_2014.jpg"; Titulo = "Gary Holt of Slayer performing in Austin, Texas 2014"; Licenca = "CC BY 2.0" }
)

$videos = @(
    @{ Nome = "ensaio-lamina-fria";  Url = "$commons/3/33/KK%27s_Priest_-_Hell_Patrol_%28Judas_Priest_Cover%29_-_14.10.23_-_Live_at_Aria_Complex%2C_Malta_%28Full_Song%29.webm"; Titulo = "KK's Priest - Hell Patrol (Judas Priest Cover) - Live at Aria Complex, Malta"; Licenca = "CC BY 3.0" }
    @{ Nome = "archive-1999-vhs";    Url = "$commons/5/58/Moshing.ogv"; Titulo = "Moshing"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "pool-wall-of-death";  Url = "$commons/0/08/Wall_of_Death_BTW.ogv"; Titulo = "Wall of Death BTW"; Licenca = "CC BY-SA 3.0" }
    @{ Nome = "pool-malevolent";     Url = "$commons/c/c9/Malevolent_Creation_On_The_Stage_-America_-Europe_-DeathMetal_-ThrashMetal_-Concert_-HeavyMetal.webm"; Titulo = "Malevolent Creation On The Stage"; Licenca = "CC BY 3.0" }
)

function Baixar([string]$url, [string]$caminho) {
    if (Test-Path $caminho) {
        Write-Host "ok      $caminho"
        return $false
    }

    Invoke-WebRequest -Uri $url -OutFile $caminho -UseBasicParsing -UserAgent $agente

    if ((Get-Item $caminho).Length -eq 0) {
        Remove-Item $caminho
        throw "Download vazio: $url"
    }

    Write-Host "baixado $caminho"
    Start-Sleep -Milliseconds 800
    return $true
}

foreach ($pasta in "avatars", "photos", "videos", "videos\originais") {
    New-Item -ItemType Directory -Force (Join-Path $Destino $pasta) | Out-Null
}

$esperados = @{}
foreach ($foto in $fotos) {
    $destinoFoto = Join-Path $Destino "photos\$($foto.Nome).jpg"
    $esperados[$destinoFoto] = $true
    Baixar $foto.Url $destinoFoto | Out-Null
}

foreach ($retrato in $retratos) {
    $destinoRetrato = Join-Path $Destino "avatars\$($retrato.Nome).jpg"
    $esperados[$destinoRetrato] = $true
    Baixar $retrato.Url $destinoRetrato | Out-Null
}

foreach ($video in $videos) {
    $original = Join-Path $Destino "videos\originais\$($video.Nome)$([System.IO.Path]::GetExtension($video.Url))"
    $final = Join-Path $Destino "videos\$($video.Nome).mp4"
    $esperados[$final] = $true

    Baixar $video.Url $original | Out-Null

    if (Test-Path $final) {
        Write-Host "ok      $final"
        continue
    }

    & ffmpeg -hide_banner -loglevel error -y -i $original -t $DuracaoVideoSegundos -c:v libx264 -preset veryfast -crf 23 -pix_fmt yuv420p -c:a aac -b:a 128k -movflags +faststart $final
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg falhou ao converter $original" }
    Write-Host "convert $final"
}

foreach ($pasta in "avatars", "photos", "videos") {
    Get-ChildItem (Join-Path $Destino $pasta) -File | Where-Object { -not $esperados.ContainsKey($_.FullName) } | ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Host "removido $($_.FullName) (saiu da lista)"
    }
}

$creditos = Join-Path $Destino "CREDITOS.txt"
$linhas = @("Mídia do seed de desenvolvimento do Noise Vault. Tudo vem do Wikimedia Commons; o título é o nome do arquivo lá.", "")
foreach ($item in ($fotos + $retratos + $videos)) {
    $linhas += "$($item.Nome): $($item.Titulo) — $($item.Licenca)"
}
Set-Content -Path $creditos -Value $linhas -Encoding UTF8

Write-Host ""
Write-Host "Mídia do seed em $Destino (créditos em CREDITOS.txt)"
