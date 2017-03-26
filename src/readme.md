# Development Notes


## Test Coverage

The PowerShell script `cover.ps1` in the root folder of the repo will generate a coverage report for all of the unit tests in the solution using [OpenCover](https://github.com/OpenCover/opencover).

For OpenCover to work, the projects need to be configured for **full** debugger output, not *portable*. To check/set this: 

* Open the project's properties window
* Switch to the *Build* tab
* At the bottom of the window, press the *Advanced* button
* Under the heading *Output*, change the value for *Debugging Information* to **full**.

