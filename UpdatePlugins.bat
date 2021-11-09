@echo off
echo COPY PLUGINS TO CLI
xcopy .\Pythia.Cli.Plugin.Standard\bin\Debug\net6.0\ .\pythia\bin\Debug\net6.0\plugins\Pythia.Cli.Plugin.Standard\ /y
xcopy .\Pythia.Cli.Plugin.Xlsx\bin\Debug\net6.0\ .\pythia\bin\Debug\net6.0\plugins\Pythia.Cli.Plugin.Xlsx\ /y

xcopy .\Pythia.Cli.Plugin.Standard\bin\Release\net6.0\ .\pythia\bin\Release\net6.0\plugins\Pythia.Cli.Plugin.Standard\ /y
xcopy .\Pythia.Cli.Plugin.Xlsx\bin\Release\net6.0\ .\pythia\bin\Release\net6.0\plugins\Pythia.Cli.Plugin.Xlsx\ /y

pause
