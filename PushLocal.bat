@echo off
echo PRESS ANY KEY TO INSTALL TO LOCAL NUGET FEED
echo Remember to generate the up-to-date package.
c:\exe\nuget add .\Pythia.Core\bin\Debug\Pythia.Core.4.0.1.nupkg -source C:\Projects\_NuGet
pause
