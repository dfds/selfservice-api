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
to start the api. The default port it gets exposed on is `8080`.

To quickly check if there are any warnings/code issues (not runtime ones), without needing to spin-up all the dependencies, you can do a ```make build```, which returns more warnings than run<br>
To run tests, simply do ``` make tests```.<br>

We currently do not have a dedicated test environment, so if you have a feature that also depends on the frontend, we suggest setting it up locally as well and pointing it to the port where the instance of your api is running. See the [portal's README](link-is-missing) for this.<br>

If your tests pass here and you're getting the wanted behavior in your feature, things should also work in prod, unless some k8s stuff needs to be configured as well. check the wiki or ask someone smart to find out how to do that.

## Branch Workflow

This project uses a lighter version of the classic git master/develop/feature/hotfix workflow. We try to have a 1-1 mapping of features (= feature branches) to issues on [this board](https://github.com/orgs/dfds/projects/25/views/5?filterQuery=milestone%3A%221P%3A+Self-Service+Platform+resuscitation+%2B+Kafka-Janitor+rework%22), and try to keep feature branches as limited in scope as possible. We make PRs into `develop` and have automatic mergeing from develop into `master` every once in a while.

## [good to know about Dockerfile]

the dockerfile of this project currently doesn't build the api, but simply copies things that were built on your machine by `make` into a docker container :snek:.
[11AUG23] - We are in the process of changing this.
