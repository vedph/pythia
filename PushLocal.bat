@echo off
echo PRESS ANY KEY TO INSTALL TO LOCAL NUGET FEED
echo Remember to generate the up-to-date package.

c:\exe\nuget add .\Corpus.Api.Controllers\bin\Debug\Corpus.Api.Controllers.10.1.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Corpus.Api.Models\bin\Debug\Corpus.Api.Models.10.1.0.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Corpus.Core\bin\Debug\Corpus.Core.10.1.0.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Corpus.Core.Plugin\bin\Debug\Corpus.Core.Plugin.10.1.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Corpus.Sql\bin\Debug\Corpus.Sql.10.1.0.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Corpus.Sql.MsSql\bin\Debug\Corpus.Sql.MsSql.10.1.0.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Corpus.Sql.PgSql\bin\Debug\Corpus.Sql.PgSql.10.1.0.nupkg -source C:\Projects\_NuGet

c:\exe\nuget add .\Pythia.Api.Controllers\bin\Debug\Pythia.Api.Controllers.5.1.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Api.Models\bin\Debug\Pythia.Api.Models.5.1.0.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Api.Services\bin\Debug\Pythia.Api.Services.5.1.4.nupkg -source C:\Projects\_NuGet

c:\exe\nuget add .\Pythia.Core\bin\Debug\Pythia.Core.5.1.0.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Cli.Core\bin\Debug\Pythia.Cli.Core.5.1.0.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Cli.Plugin.Standard\bin\Debug\Pythia.Cli.Plugin.Standard.5.1.3.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Core.Plugin\bin\Debug\Pythia.Core.Plugin.5.1.1.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Sql\bin\Debug\Pythia.Sql.5.1.2.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Sql.PgSql\bin\Debug\Pythia.Sql.PgSql.5.1.2.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Udp.Plugin\bin\Debug\Pythia.Udp.Plugin.5.1.0.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Pythia.Xlsx.Plugin\bin\Debug\Pythia.Xlsx.Plugin.5.1.0.nupkg -source C:\Projects\_NuGet
pause
