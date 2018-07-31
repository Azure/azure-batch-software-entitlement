#
# Build native components for Windows
#
param (
    [ValidateSet('Release', 'Debug')]
    $configuration = "Debug"
)

. .\scripts\bootstrap.ps1
invoke-psake -buildFile .\scripts\psake-build.ps1 -taskList Request.$configuration, Build.Windows
