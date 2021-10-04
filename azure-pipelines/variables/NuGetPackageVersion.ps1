# Just capture the value produced earlier in the pipeline.
& (& "$PSScriptRoot\..\Get-nbgv.ps1") get-version -v NuGetPackageVersion
