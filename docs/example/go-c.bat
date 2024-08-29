@echo off
set pt=D:\Projects\Pythia\PythiaApi\pythia\bin\Debug\net8.0\pythia.exe
%pt% create-db -d pythia-demo -c
pause
%pt% add-profiles c:\users\dfusi\desktop\pythia\example-c.json -d pythia-demo
pause
%pt% index example c:\users\dfusi\desktop\pythia\*.xml -o -d pythia-demo -t pythia-factory-provider.chiron
pause
%pt% index-w -d pythia-demo -c date-value=3 -c date_value=3 -x date
pause
%pt% add-profiles c:\users\dfusi\desktop\pythia\example-c-prod.json -i example -d pythia-demo
pause
%pt% bulk-write c:\users\dfusi\desktop\pythia\bulk -d pythia-demo
pause
