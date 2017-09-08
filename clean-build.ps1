#
# Clean out all files from the .\out\ build artifacts directory
#
. .\scripts\bootstrap.ps1
invoke-psake -buildFile .\scripts\psake-build.ps1 -taskList Clean
