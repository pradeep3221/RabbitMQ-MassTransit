FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["rabbitmq.consumer.worker.csproj", "./"]
RUN dotnet restore "rabbitmq.consumer.worker.csproj"
COPY . .
RUN dotnet build "rabbitmq.consumer.worker.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "rabbitmq.consumer.worker.dll"]