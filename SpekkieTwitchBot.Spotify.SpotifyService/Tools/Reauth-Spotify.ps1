# Re-runs the Spotify authorization-code flow and writes a fresh refresh_token
# into the bot's Settings/Spotify.json. Run this yourself in a normal terminal.

$ErrorActionPreference = 'Stop'

# --- locate Spotify.json exactly the way BotPaths.BaseDir does ---
$baseDir = $env:BOT_BASE_DIR
if ([string]::IsNullOrWhiteSpace($baseDir)) {
    $baseDir = Join-Path ([Environment]::GetFolderPath('Desktop')) 'SpekkieTwitchBot'
}
$authFile = Join-Path $baseDir 'Settings\Spotify.json'

if (-not (Test-Path $authFile)) { throw "Not found: $authFile" }
$auth = Get-Content $authFile -Raw | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($auth.client_id) -or [string]::IsNullOrWhiteSpace($auth.client_secret)) {
    throw "client_id / client_secret missing in $authFile"
}
Write-Host "Using $authFile (client_id ends in ...$($auth.client_id.Substring($auth.client_id.Length-4)))"

# --- must EXACTLY match a Redirect URI registered in your Spotify app dashboard ---
$redirectUri = Read-Host "Redirect URI [https://127.0.0.1:4202/callback]"
if ([string]::IsNullOrWhiteSpace($redirectUri)) { $redirectUri = 'https://127.0.0.1:4202/callback' }

$scopes = @(
    'user-read-private','user-read-email','user-read-playback-state','user-modify-playback-state',
    'playlist-read-private','playlist-read-collaborative','playlist-modify-public','playlist-modify-private',
    'user-library-modify','user-library-read','user-read-currently-playing'
) -join ' '

$authUrl = 'https://accounts.spotify.com/authorize' +
    '?response_type=code' +
    '&client_id='    + [uri]::EscapeDataString($auth.client_id) +
    '&scope='        + [uri]::EscapeDataString($scopes) +
    '&redirect_uri=' + [uri]::EscapeDataString($redirectUri) +
    '&show_dialog=true'

Write-Host ""
Write-Host "Opening browser. Log in as the Spotify account the bot should control, then Agree."
Write-Host "The page will fail to load afterwards - that is expected."
Write-Host "Copy the WHOLE address bar URL (it contains ?code=...) and paste it below."
Write-Host ""
Write-Host $authUrl
Write-Host ""
Start-Process $authUrl

$pasted = Read-Host "Paste the redirect URL (or just the code)"
$code = $pasted.Trim()
if ($code -match '[?&]code=([^&]+)') { $code = [uri]::UnescapeDataString($matches[1]) }
if ($pasted -match '[?&]error=([^&]+)') { throw "Spotify returned error: $($matches[1])" }
if ([string]::IsNullOrWhiteSpace($code)) { throw "No authorization code found." }

# --- exchange code for tokens (Basic auth, same as SpotifyAuthService) ---
$basic = [Convert]::ToBase64String(
    [Text.Encoding]::UTF8.GetBytes("$($auth.client_id):$($auth.client_secret)"))

$body = "grant_type=authorization_code" +
        "&code="         + [uri]::EscapeDataString($code) +
        "&redirect_uri=" + [uri]::EscapeDataString($redirectUri)

try {
    $tok = Invoke-RestMethod -Method Post -Uri 'https://accounts.spotify.com/api/token' `
        -Headers @{ Authorization = "Basic $basic" } `
        -ContentType 'application/x-www-form-urlencoded' -Body $body
} catch {
    $resp = $_.Exception.Response
    if ($resp) {
        $reader = New-Object IO.StreamReader($resp.GetResponseStream())
        Write-Host "Spotify said: $($reader.ReadToEnd())" -ForegroundColor Red
    }
    throw
}

if ([string]::IsNullOrWhiteSpace($tok.refresh_token)) { throw "No refresh_token in response." }

# Codes are single-use, so persist the response immediately - if anything below
# fails, the tokens are still recoverable from this file instead of being lost.
# Deliberately alongside Spotify.json, NOT next to this script: this script lives in the
# repo, and a stray token file there would sit in the working tree waiting to be committed.
$stash = Join-Path (Split-Path $authFile -Parent) 'spotify-tokens-raw.json'
($tok | ConvertTo-Json -Depth 5) | Set-Content $stash -Encoding utf8
Write-Host "Tokens received and stashed at $stash" -ForegroundColor Yellow

# --- back up, then write both tokens back ---
$backup = "$authFile.bak"
Copy-Item $authFile $backup -Force

# -Force so this works whether or not the key already exists in the JSON
# (the file has no "token" key until the first successful auth).
$auth | Add-Member -NotePropertyName 'token'         -NotePropertyValue $tok.access_token  -Force
$auth | Add-Member -NotePropertyName 'refresh_token' -NotePropertyValue $tok.refresh_token -Force

# WriteAllText with a BOM-less encoder - Out-File -Encoding utf8 emits a BOM on PS 5.1.
[IO.File]::WriteAllText($authFile, ($auth | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding($false)))

Write-Host ""
Write-Host "Done. New refresh_token written to $authFile" -ForegroundColor Green
Write-Host "Backup of the old file: $backup"
Write-Host "Restart the bot."

# The stash held the only copy while the write was in flight; it is redundant now.
Remove-Item $stash -Force -ErrorAction SilentlyContinue
