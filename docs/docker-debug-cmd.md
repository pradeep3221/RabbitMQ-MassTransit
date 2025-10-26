## Docker Debug

Docker Compose `build` and `run`  
location:`E:\DotNetWorld\2025Projects\EDA\Messaging\RabbitMQ+MassTransit\RabbitMQMassTransitSamples\docker-compose.yml`  

Run the apps
```
docker-compose up -d

docker-compose ps

docker compose stop
docker compose down
```

rebuild and start all 
```
* docker-compose up -d --build
```


Rebuild and run only rabbitmq-producer-api
```
docker-compose build rabbitmq-producer-api && docker-compose up -d rabbitmq-producer-api
```

## SQLDocker Debug
link: https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver17&tabs=cli&pivots=cs1-bash 

```SQLDocker Debug
// this is in appsettings.json and when in docker network
"DefaultConnection": "Server=sqlserver-masstransit,1433;Database=OutboxDb;User=sa;Password=Admin@123;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"
	- Server=127.0.0.1,1433;
	- Server=localhost,1433;

-- docker-compose exec sqlserver-masstransit /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Admin@123 -Q "CREATE DATABASE OrderDb"

docker-compose exec sqlserver-masstransit /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Admin@123 -Q "SELECT * FROM [OrderDb].[dbo].[__EFMigrationsHistory]"


docker-compose exec sqlserver-masstransit /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Admin@123 -Q "SELECT  GETDATE()"
docker exec -it sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin@123"

docker exec sqlserver-masstransit /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Admin@123 -Q "SELECT @@version"
	"SELECT name FROM sys.databases"

docker exec sqlserver-masstransit /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Admin@123 -d OutboxDb -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'"

"SELECT name FROM sys.databases"



docker exec rabbitmq-producer-api apt install -y curl && docker exec rabbitmq-producer-api curl https://packages.microsoft.com/keys/microsoft.asc | docker exec -i rabbitmq-producer-api apt-key add - && docker exec rabbitmq-producer-api curl https://packages.microsoft.com/config/debian/12/prod.list | docker exec -i rabbitmq-producer-api tee /etc/apt/sources.list.d/mssql-release.list && docker exec rabbitmq-producer-api apt update && docker exec rabbitmq-producer-api ACCEPT_EULA=Y apt install -y mssql-tools18


docker exec rabbitmq-producer-api apt update && docker exec rabbitmq-producer-api apt install -y curl gnupg2 && docker exec rabbitmq-producer-api curl https://packages.microsoft.com/keys/microsoft.asc | docker exec -i rabbitmq-producer-api apt-key add - && docker exec rabbitmq-producer-api curl https://packages.microsoft.com/config/debian/12/prod.list > /etc/apt/sources.list.d/mssql-release.list && docker exec rabbitmq-producer-api apt update && docker exec rabbitmq-producer-api ACCEPT_EULA=Y apt install -y mssql-tools18 unixodbc-dev
```

docker exec rabbitmq-producer-api ping sqlserver-masstransit

docker logs rabbitmq-producer-api -f
docker logs sqlserver-masstransit
docker logs rabbitmq-masstransit

docker network inspect rabbitmqmasstransitsamples_rabbitmq-net





