# Setup-SecurityEnvironment.ps1
# PowerShell script to configure security environment variables for TeamA.Todo application
# Run this script as Administrator

param(
    [Parameter(Mandatory=$false)]
    [string]$JwtSecret,
    
    [Parameter(Mandatory=$false)]
    [string]$AdminEmail = "admin@todo.com",
    
    [Parameter(Mandatory=$false)]
    [string]$AdminPassword,
    
    [Parameter(Mandatory=$false)]
    [switch]$GenerateSecrets
)

# Function to generate secure random string
function New-SecureRandomString {
    param(
        [int]$Length = 64
    )
    
    Add-Type -AssemblyName System.Web
    return [System.Web.Security.Membership]::GeneratePassword($Length, 16)
}

# Function to validate JWT secret
function Test-JwtSecret {
    param(
        [string]$Secret
    )
    
    if ($Secret.Length -lt 32) {
        Write-Warning "JWT Secret must be at least 32 characters long!"
        return $false
    }
    
    return $true
}

# Function to set environment variable
function Set-EnvironmentVariable {
    param(
        [string]$Name,
        [string]$Value,
        [string]$Target = "Machine"
    )
    
    try {
        [System.Environment]::SetEnvironmentVariable($Name, $Value, $Target)
        Write-Host "✓ Set $Name environment variable" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "Failed to set $Name environment variable: $_"
        return $false
    }
}

# Check if running as administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator. Please restart PowerShell as Administrator and try again."
    exit 1
}

Write-Host "TeamA.Todo Security Environment Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Generate JWT Secret if needed
if ($GenerateSecrets -or [string]::IsNullOrEmpty($JwtSecret)) {
    Write-Host "Generating secure JWT secret..." -ForegroundColor Yellow
    $JwtSecret = New-SecureRandomString -Length 64
    Write-Host "Generated JWT Secret: $JwtSecret" -ForegroundColor Green
    Write-Host ""
    Write-Warning "IMPORTANT: Save this JWT secret in a secure location. You will need it for all application instances."
    Write-Host ""
}

# Validate JWT Secret
if (-not (Test-JwtSecret -Secret $JwtSecret)) {
    Write-Error "Invalid JWT Secret. Please provide a secret at least 32 characters long."
    exit 1
}

# Generate Admin Password if needed
if ($GenerateSecrets -and [string]::IsNullOrEmpty($AdminPassword)) {
    Write-Host "Generating secure admin password..." -ForegroundColor Yellow
    $AdminPassword = New-SecureRandomString -Length 16
    Write-Host "Generated Admin Password: $AdminPassword" -ForegroundColor Green
    Write-Host ""
}

# Display current configuration
Write-Host "Configuration Summary:" -ForegroundColor Cyan
Write-Host "JWT Secret Length: $($JwtSecret.Length) characters" -ForegroundColor White
Write-Host "Admin Email: $AdminEmail" -ForegroundColor White
if (-not [string]::IsNullOrEmpty($AdminPassword)) {
    Write-Host "Admin Password: [PROVIDED]" -ForegroundColor White
} else {
    Write-Host "Admin Password: [WILL BE GENERATED ON FIRST RUN]" -ForegroundColor Yellow
}
Write-Host ""

# Confirm before proceeding
$confirmation = Read-Host "Do you want to set these environment variables? (Y/N)"
if ($confirmation -ne 'Y' -and $confirmation -ne 'y') {
    Write-Host "Setup cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Setting environment variables..." -ForegroundColor Cyan

# Set environment variables
$success = $true

# JWT Secret
if (-not (Set-EnvironmentVariable -Name "JWT_SECRET" -Value $JwtSecret)) {
    $success = $false
}

# Admin Email
if (-not (Set-EnvironmentVariable -Name "ADMIN_EMAIL" -Value $AdminEmail)) {
    $success = $false
}

# Admin Password (if provided)
if (-not [string]::IsNullOrEmpty($AdminPassword)) {
    if (-not (Set-EnvironmentVariable -Name "ADMIN_PASSWORD" -Value $AdminPassword)) {
        $success = $false
    }
}

Write-Host ""

if ($success) {
    Write-Host "✓ Environment variables set successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Restart IIS or your application pool" -ForegroundColor White
    Write-Host "2. Start the TeamA.Todo application" -ForegroundColor White
    Write-Host "3. Login with admin credentials" -ForegroundColor White
    Write-Host "4. Change the admin password immediately" -ForegroundColor White
    
    if (-not [string]::IsNullOrEmpty($AdminPassword)) {
        Write-Host ""
        Write-Warning "Admin Credentials:"
        Write-Warning "Email: $AdminEmail"
        Write-Warning "Password: $AdminPassword"
        Write-Warning "Change this password immediately after first login!"
    }
} else {
    Write-Error "Some environment variables could not be set. Please check the errors above."
    exit 1
}

Write-Host ""
Write-Host "Security setup complete!" -ForegroundColor Green

# Optionally display current environment variables
Write-Host ""
$showCurrent = Read-Host "Would you like to verify the current environment variables? (Y/N)"
if ($showCurrent -eq 'Y' -or $showCurrent -eq 'y') {
    Write-Host ""
    Write-Host "Current Environment Variables:" -ForegroundColor Cyan
    
    $jwtValue = [System.Environment]::GetEnvironmentVariable("JWT_SECRET", "Machine")
    if ($jwtValue) {
        Write-Host "JWT_SECRET: [SET - $($jwtValue.Length) characters]" -ForegroundColor Green
    } else {
        Write-Host "JWT_SECRET: [NOT SET]" -ForegroundColor Red
    }
    
    $emailValue = [System.Environment]::GetEnvironmentVariable("ADMIN_EMAIL", "Machine")
    if ($emailValue) {
        Write-Host "ADMIN_EMAIL: $emailValue" -ForegroundColor Green
    } else {
        Write-Host "ADMIN_EMAIL: [NOT SET]" -ForegroundColor Red
    }
    
    $passwordValue = [System.Environment]::GetEnvironmentVariable("ADMIN_PASSWORD", "Machine")
    if ($passwordValue) {
        Write-Host "ADMIN_PASSWORD: [SET]" -ForegroundColor Green
    } else {
        Write-Host "ADMIN_PASSWORD: [NOT SET - Will be generated on first run]" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Script execution completed." -ForegroundColor Cyan
