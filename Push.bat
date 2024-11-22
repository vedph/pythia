@echo off
echo PUSH PACKAGES TO NUGET
prompt
set nu=C:\Exe\nuget.exe
set src=-Source https://api.nuget.org/v3/index.json

%nu% push .\Corpus.Api.Controllers\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Corpus.Api.Models\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Corpus.Core\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Corpus.Core.Plugin\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Corpus.Sql\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Corpus.Sql.MsSql\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Corpus.Sql.PgSql\bin\Debug\*.nupkg %src% -SkipDuplicate

%nu% push .\Pythia.Api.Models\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Pythia.Api.Services\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Pythia.Core\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Pythia.Cli.Core\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Pythia.Core.Plugin\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Pythia.Sql\bin\Debug\*.nupkg %src% -SkipDuplicate
%nu% push .\Pythia.Sql.PgSql\bin\Debug\*.nupkg %src% -SkipDuplicate
echo COMPLETED
pause
echo on
