@echo off
echo Start DataHolder projects (Build in Release configuration)?
pause

dotnet build --configuration Release ../CDR.DataHolder.API.Gateway.mTLS
dotnet build --configuration Release ../CDR.DataHolder.IdentityServer
dotnet build --configuration Release ../CDR.DataHolder.Resource.API
dotnet build --configuration Release ../CDR.DataHolder.Manage.API
pause

wt --maximized ^
--title Energy_Gateway_MTLS -d ../CDR.DataHolder.API.Gateway.mTLS dotnet run --no-build; ^
--title Energy_IdentityServer -d ../CDR.DataHolder.IdentityServer dotnet run --no-build; ^
--title Energy_Resource_API -d ../CDR.DataHolder.Resource.API dotnet run --no-build; ^
--title Energy_Manage_API -d ../CDR.DataHolder.Manage.API dotnet run --no-build

pause  