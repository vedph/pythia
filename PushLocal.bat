@echo off
echo PRESS ANY KEY TO INSTALL TO LOCAL NUGET FEED
echo Remember to generate the up-to-date package.

c:\exe\nuget add .\Pythia.Api.Models\bin\Debug\Pythia.Api.Models.4.1.3.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Api.Services\bin\Debug\Pythia.Api.Services.4.1.5.nupkg -source C:\Projects\_NuGet

c:\exe\nuget add .\Pythia.Core\bin\Debug\Pythia.Core.4.1.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Cli.Core\bin\Debug\Pythia.Cli.Core.4.1.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Cli.Plugin.Standard\bin\Debug\Pythia.Cli.Plugin.Standard.4.1.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Core.Plugin\bin\Debug\Pythia.Core.Plugin.4.1.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Sql\bin\Debug\Pythia.Sql.4.1.3.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Sql.PgSql\bin\Debug\Pythia.Sql.PgSql.4.1.3.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Udp.Plugin\bin\Debug\Pythia.Udp.Plugin.4.1.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Xlsx.Plugin\bin\Debug\Pythia.Xlsx.Plugin.4.1.1.nupkg -source C:\Projects\_NuGet
pause
