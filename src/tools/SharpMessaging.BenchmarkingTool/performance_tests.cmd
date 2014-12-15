del /y result.csv

SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 400000 -MessageSize 10 -SpawnServer
SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 400000 -MessageSize 100 -SpawnServer
SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 400000 -MessageSize 1000 -SpawnServer
SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 400000 -MessageSize 10000 -SpawnServer

@echo. >> result.csv

SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 1000000 -MessageSize 10 -SpawnServer
SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 1000000 -MessageSize 100 -SpawnServer
SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 1000000 -MessageSize 1000 -SpawnServer


@echo. >> result.csv

SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 400000 -MessageSize 10 -SpawnServer -MessagesPerAck 200
SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 400000 -MessageSize 100 -SpawnServer -MessagesPerAck 200
SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 400000 -MessageSize 1000 -SpawnServer -MessagesPerAck 200
SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 400000 -MessageSize 10000 -SpawnServer -MessagesPerAck 200

@echo. >> result.csv

SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 1000000 -MessageSize 1000 -SpawnServer
SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 1000000 -MessageSize 1000 -SpawnServer -MessagesPerAck 10
SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 1000000 -MessageSize 1000 -SpawnServer -MessagesPerAck 100
SharpMessaging.BenchmarkApp.exe -client localhost:8334 -MessageCount 1000000 -MessageSize 1000 -SpawnServer -MessagesPerAck 200
