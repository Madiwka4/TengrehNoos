FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /TengrehNoos

COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /TengrehNoos
COPY --from=build-env /TengrehNoos/out .
COPY --from=build-env /TengrehNoos/schema.sql /docker-entrypoint-initdb.d/
ENTRYPOINT ["dotnet", "TengrehNoos.dll"]