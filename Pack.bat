@echo off
echo BUILD CORPUS PACKAGES
del .\Corpus.Api.Controllers\bin\Debug\*.*nupkg
del .\Corpus.Api.Models\bin\Debug\*.*nupkg
del .\Corpus.Core\bin\Debug\*.*nupkg
del .\Corpus.Core.Plugin\bin\Debug\*.*nupkg
del .\Corpus.Sql\bin\Debug\*.*nupkg
del .\Corpus.Sql.PgSql\bin\Debug\*.*nupkg
del .\Corpus.Sql.MsSql\bin\Debug\*.*nupkg

cd .\Corpus.Api.Controllers
dotnet pack -c Debug --include-symbols
cd..
cd .\Corpus.Api.Models
dotnet pack -c Debug --include-symbols
cd..
cd .\Corpus.Core
dotnet pack -c Debug --include-symbols
cd..
cd .\Corpus.Core.Plugin
dotnet pack -c Debug --include-symbols
cd..
cd .\Corpus.Sql
dotnet pack -c Debug --include-symbols
cd..
cd .\Corpus.Sql.PgSql
dotnet pack -c Debug --include-symbols
cd..
cd .\Corpus.Sql.MsSql
dotnet pack -c Debug --include-symbols
cd..
pause

echo BUILD PYTHIA PACKAGES
del .\Pythia.Api.Controllers\bin\Debug\*.*nupkg
del .\Pythia.Api.Models\bin\Debug\*.*nupkg
del .\Pythia.Api.Services\bin\Debug\*.*nupkg
del .\Pythia.Cli.Core\bin\Debug\*.*nupkg
del .\Pythia.Cli.Plugin.Standard\bin\Debug\*.*nupkg
del .\Pythia.Cli.Plugin.Xlsx\bin\Debug\*.*nupkg
del .\Pythia.Core\bin\Debug\*.*nupkg
del .\Pythia.Core.Plugin\bin\Debug\*.*nupkg
del .\Pythia.Sql\bin\Debug\*.*nupkg
del .\Pythia.Sql.PgSql\bin\Debug\*.*nupkg
del .\Pythia.Xlsx.Plugin\bin\Debug\*.*nupkg

cd .\Pythia.Api.Controllers
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Pythia.Api.Models
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Pythia.Api.Services
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Pythia.Cli.Core
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Pythia.Cli.Plugin.Standard
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Pythia.Cli.Plugin.Xlsx
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Pythia.Core
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Pythia.Core.Plugin
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Pythia.Sql
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Pythia.Sql.PgSql
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Pythia.Xlsx.Plugin
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

cd .\Pythia.Udp.Plugin
dotnet pack -c Debug -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg
cd..

pause
