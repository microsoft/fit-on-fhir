#!/usr/bin/env pwsh

param
(
    [Parameter(Mandatory=$True)]
    [ValidateNotNullOrEmpty()]
    [string]
    $ResourceGroup,

    [Parameter(Mandatory=$True)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Name,

    [Parameter(Mandatory=$True)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Src
)

$stopwatch = New-Object System.Diagnostics.Stopwatch

$stopwatch.Start()

while ($stopwatch.Elapsed.TotalSeconds -lt 300) {
    az webapp deployment source config-zip --resource-group $ResourceGroup --name $Name --src $Src
    $success = $?
    if ($success) {
        break;
    } else {
        Start-Sleep -s 5
    }
}

$stopwatch.Stop()

if ($success) {
    Write-Host "az webapp deployment source config-zip --resource-group $ResourceGroup --name $Name --src $Src was successful in $($stopwatch.Elapsed.TotalSeconds) seconds."
} else {
    Write-Host "az webapp deployment source config-zip --resource-group $ResourceGroup --name $Name --src $Src timed out after $($stopwatch.Elapsed.TotalSeconds) seconds"
}
