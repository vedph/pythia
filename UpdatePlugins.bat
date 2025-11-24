@echo off
echo COPY PLUGINS TO CLI
echo DEBUG
REM xcopy .\Pythia.Cli.Plugin.Chiron\bin\Debug\net10.0\ .\pythia\bin\Debug\net10.0\plugins\Pythia.Cli.Plugin.Chiron\ /y
xcopy .\Pythia.Cli.Plugin.Standard\bin\Debug\net10.0\ .\pythia\bin\Debug\net10.0\plugins\Pythia.Cli.Plugin.Standard\ /y
xcopy .\Pythia.Cli.Plugin.Udp\bin\Debug\net10.0\ .\pythia\bin\Debug\net10.0\plugins\Pythia.Cli.Plugin.Udp\ /y
xcopy .\Pythia.Cli.Plugin.Xlsx\bin\Debug\net10.0\ .\pythia\bin\Debug\net10.0\plugins\Pythia.Cli.Plugin.Xlsx\ /y
pause

echo RELEASE
REM xcopy .\Pythia.Cli.Plugin.Chiron\bin\Release\net10.0\ .\pythia\bin\Release\net10.0\plugins\Pythia.Cli.Plugin.Chiron\ /y
xcopy .\Pythia.Cli.Plugin.Standard\bin\Release\net10.0\ .\pythia\bin\Release\net10.0\plugins\Pythia.Cli.Plugin.Standard\ /y
xcopy .\Pythia.Cli.Plugin.Udp\bin\Release\net10.0\ .\pythia\bin\Release\net10.0\plugins\Pythia.Cli.Plugin.Udp\ /y
xcopy .\Pythia.Cli.Plugin.Xlsx\bin\Release\net10.0\ .\pythia\bin\Release\net10.0\plugins\Pythia.Cli.Plugin.Xlsx\ /y

pause
