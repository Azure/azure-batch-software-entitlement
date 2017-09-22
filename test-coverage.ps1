#
# Run unit tests and generate a coverage report
#
. .\scripts\bootstrap.ps1
invoke-psake -buildFile .\scripts\psake-build.ps1 -taskList Cover.Tests
