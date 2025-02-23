$CakeVersion = "1.3.0"

# Make sure tools folder exists
$PSScriptRoot = $pwd

$ToolPath = Join-Path $PSScriptRoot "tools"
if (!(Test-Path $ToolPath)) {
    Write-Verbose "Creating tools directory..."
    New-Item -Path $ToolPath -Type directory | out-null
}

###########################################################################
# INSTALL CAKE
###########################################################################

Add-Type -AssemblyName System.IO.Compression.FileSystem
Function Unzip
{
    param([string]$zipfile, [string]$outpath)

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}


# Make sure Cake has been installed.
$CakePath = Join-Path $ToolPath "Cake.$CakeVersion/Cake.exe"
if (!(Test-Path $CakePath)) {
    Write-Host "Installing Cake..."
    (New-Object System.Net.WebClient).DownloadFile("https://www.nuget.org/api/v2/package/Cake/$CakeVersion", "$ToolPath\Cake.zip")
    Unzip "$ToolPath\Cake.zip" "$ToolPath/Cake.$CakeVersion"
    Remove-Item "$ToolPath\Cake.zip"
}

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

& "$CakePath" $args
exit $LASTEXITCODE
