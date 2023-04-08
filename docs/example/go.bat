@echo off
set pt=D:\Projects\Pythia\PythiaApi\pythia\bin\Debug\net7.0\pythia.exe
%pt% create-db -d pythia-demo -c
pause
%pt% add-profiles c:\users\dfusi\desktop\pythia\example.json -d pythia-demo
pause
%pt% index example c:\users\dfusi\desktop\pythia\*.xml -o -d pythia-demo -t factory-provider.chiron
pause
%pt% add-profiles c:\users\dfusi\desktop\pythia\example-prod.json -i example -d pythia-demo
pause
%pt% bulk-write c:\users\dfusi\desktop\pythia\bulk -d pythia-demo
pause
