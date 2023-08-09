# Self service API

[![Build Status](https://dfds.visualstudio.com/CloudEngineering/_apis/build/status%2Fselfservice-api?branchName=feature%2F1853-create-better-readme)](https://dfds.visualstudio.com/CloudEngineering/_build/latest?definitionId=3113&branchName=feature%2F1853-create-better-readme)

## How to run
The following steps describe how to start an instance of the api on your local machine, where the database will be populated by the data in `db/seed/`.
(run commands from the root of the repo)


1. start dependencies using
```
docker-compose up --build
```
This will start a postgresql server (by default on port 5432) as well as a fake confluent gateway (currently by default on port 5051) and kafka broker (as well as some other stuff)
This may take a minute. If it fails it's usually because the ports are already in use (often by other docker containers you have running).


2. once the above are up and running, run
```
make dev
```
to start the api.

To quickly check if there are any warnings/code issues (not runtime ones), without needing to spin-up all the dependencies, you can do a ```make build```, which returns more warnings than run<br>
To run tests, simply do ``` make tests```
