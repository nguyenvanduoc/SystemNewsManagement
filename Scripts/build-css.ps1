# Dựng lại wwwroot/css/tailwind.min.css cho trang khách.
# Chạy mỗi khi thêm class Tailwind mới vào file .cshtml (trước khi Publish).
# Yêu cầu: Node.js (https://nodejs.org) - lần đầu npx sẽ tự tải tailwindcss 3.4.17.
#   powershell -ExecutionPolicy Bypass -File Scripts\build-css.ps1
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
npx --yes tailwindcss@3.4.17 -c tailwind.config.js -i Styles/tailwind.input.css -o wwwroot/css/tailwind.min.css --minify
Write-Host "Da dung xong wwwroot/css/tailwind.min.css" -ForegroundColor Green
