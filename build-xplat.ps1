#
# Build the cross-platform parts of the SDK using dotnet.exe
#

. .\scripts\bootstrap.ps1
invoke-psake -buildFile .\scripts\psake-build.ps1 -taskList Build.Xplat
