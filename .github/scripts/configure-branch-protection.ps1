#Requires -Version 7.0
<#
.SYNOPSIS
    Configures branch protection on master to require the build-and-test CI check.

.DESCRIPTION
    Run once after the CI workflow has executed at least once on master so GitHub
    recognizes the "build-and-test" status check. Requires GitHub CLI (gh) and
    admin access to the repository.

.EXAMPLE
    pwsh .github/scripts/configure-branch-protection.ps1
#>
[CmdletBinding()]
param(
    [string]$Branch = "master",
    [string]$RequiredCheck = "build-and-test",
    [string]$Repository = "OleksandrShchur/FindIFBot"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) is required. Install from https://cli.github.com/ and run 'gh auth login'."
}

gh auth status | Out-Null

$payload = @{
    required_status_checks = @{
        strict = $true
        contexts = @($RequiredCheck)
    }
    enforce_admins = $false
    required_pull_request_reviews = $null
    restrictions = $null
    required_linear_history = $false
    allow_force_pushes = $false
    allow_deletions = $false
    block_creations = $false
    required_conversation_resolution = $false
} | ConvertTo-Json -Depth 5 -Compress

gh api `
    --method PUT `
    -H "Accept: application/vnd.github+json" `
    "/repos/$Repository/branches/$Branch/protection" `
    --input - <<< $payload

Write-Host "Branch protection enabled on '$Branch' requiring status check '$RequiredCheck'."
