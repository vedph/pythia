cd .\pythia

dotnet build -c Release
dotnet publish -c Release
compress-archive -path .\bin\Release\net7.0\publish\* -DestinationPath .\bin\Release\net7.0\pythia-any.zip -Force

dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained

compress-archive -path .\bin\Release\net7.0\win-x64\publish\* -DestinationPath .\bin\Release\net7.0\win-x64\pythia-win-x64.zip -Force
compress-archive -path .\bin\Release\net7.0\linux-x64\publish\* -DestinationPath .\bin\Release\net7.0\linux-x64\pythia-linux-x64.zip -Force
compress-archive -path .\bin\Release\net7.0\osx-x64\publish\* -DestinationPath .\bin\Release\net7.0\osx-x64\pythia-osx-x64.zip -Force
