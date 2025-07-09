@echo off
echo COPY PLUGINS TO CLI
echo DEBUG
xcopy .\Pythia.Cli.Plugin.Chiron\bin\Debug\net9.0\ .\pythia\bin\Debug\net9.0\plugins\Pythia.Cli.Plugin.Chiron\ /y
xcopy .\Pythia.Cli.Plugin.Standard\bin\Debug\net9.0\ .\pythia\bin\Debug\net9.0\plugins\Pythia.Cli.Plugin.Standard\ /y
xcopy .\Pythia.Cli.Plugin.Udp\bin\Debug\net9.0\ .\pythia\bin\Debug\net9.0\plugins\Pythia.Cli.Plugin.Udp\ /y
xcopy .\Pythia.Cli.Plugin.Xlsx\bin\Debug\net9.0\ .\pythia\bin\Debug\net9.0\plugins\Pythia.Cli.Plugin.Xlsx\ /y
pause

echo RELEASE
xcopy .\Pythia.Cli.Plugin.Chiron\bin\Release\net9.0\ .\pythia\bin\Release\net9.0\plugins\Pythia.Cli.Plugin.Chiron\ /y
xcopy .\Pythia.Cli.Plugin.Standard\bin\Release\net9.0\ .\pythia\bin\Release\net9.0\plugins\Pythia.Cli.Plugin.Standard\ /y
xcopy .\Pythia.Cli.Plugin.Udp\bin\Release\net9.0\ .\pythia\bin\Release\net9.0\plugins\Pythia.Cli.Plugin.Udp\ /y
xcopy .\Pythia.Cli.Plugin.Xlsx\bin\Release\net9.0\ .\pythia\bin\Release\net9.0\plugins\Pythia.Cli.Plugin.Xlsx\ /y

pause
