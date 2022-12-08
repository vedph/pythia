@echo off
echo COPY PLUGINS TO CLI
echo DEBUG
xcopy .\Pythia.Cli.Plugin.Standard\bin\Debug\net7.0\ .\pythia\bin\Debug\net7.0\plugins\Pythia.Cli.Plugin.Standard\ /y
xcopy .\Pythia.Cli.Plugin.Udp\bin\Debug\net7.0\ .\pythia\bin\Debug\net7.0\plugins\Pythia.Cli.Plugin.Udp\ /y
xcopy .\Pythia.Cli.Plugin.Xlsx\bin\Debug\net7.0\ .\pythia\bin\Debug\net7.0\plugins\Pythia.Cli.Plugin.Xlsx\ /y
pause

echo RELEASE
xcopy .\Pythia.Cli.Plugin.Standard\bin\Release\net7.0\ .\pythia\bin\Release\net7.0\plugins\Pythia.Cli.Plugin.Standard\ /y
xcopy .\Pythia.Cli.Plugin.Udp\bin\Release\net7.0\ .\pythia\bin\Release\net7.0\plugins\Pythia.Cli.Plugin.Udp\ /y
xcopy .\Pythia.Cli.Plugin.Xlsx\bin\Release\net7.0\ .\pythia\bin\Release\net7.0\plugins\Pythia.Cli.Plugin.Xlsx\ /y

pause
