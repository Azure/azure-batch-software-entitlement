#
# Build native components for Windows
#
param (
    [ValidateSet('x86', 'x64')]
    $platform = "x64", 
    [ValidateSet('Release', 'Debug')]
    $configuration = "Debug"
)

. .\scripts\bootstrap.ps1
invoke-psake -buildFile .\scripts\psake-build.ps1 -taskList Request.$platform, Request.$configuration, Build.Windows
