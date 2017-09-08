#
# Publish the cross-platform parts of the SDK as standalone executables
#
. .\scripts\bootstrap.ps1
invoke-psake -buildFile .\scripts\psake-build.ps1 -taskList Publish.Xplat
