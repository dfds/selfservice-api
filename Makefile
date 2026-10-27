CONFIGURATION=Debug
ROOT_DIR=${PWD}
OUTPUT_DIR=${PWD}/.output
OUTPUT_DIR_APP=$(OUTPUT_DIR)/app
OUTPUT_DIR_DB=$(OUTPUT_DIR)/db
OUTPUT_DIR_TESTRESULTS=$(OUTPUT_DIR)/testresults
APP_PROJECT=SelfService/SelfService.csproj
APP_IMAGE_NAME=selfservice-api/app
DB_IMAGE_NAME=selfservice-api/dbmigrations

init: clean restore build tests

clean:
	@-rm -Rf $(OUTPUT_DIR)
	@mkdir $(OUTPUT_DIR)
	@mkdir $(OUTPUT_DIR_APP)
	@mkdir $(OUTPUT_DIR_DB)
	@mkdir $(OUTPUT_DIR_TESTRESULTS)
	@cd src && dotnet clean \
		--configuration $(CONFIGURATION) \
		--nologo \
		-v q \
		$(APP_PROJECT)

restore:
	@cd src && dotnet restore

build:
	@cd src && dotnet build \
		--configuration $(CONFIGURATION) \
		--no-restore \
		--nologo \
		-v q

unittest: unittests

unittests: build
	@cd src && dotnet test \
		--configuration $(CONFIGURATION) \
		--no-restore \
		--no-build \
		--logger "trx;LogFileName=testresults.trx" \
		--filter Category!=Integration \
		--collect "XPlat Code Coverage" \
		--nologo \
		-v normal

integrationtest: integrationtests

integrationtests: build
	@cd integrationtest && bash ./run.sh $(CONFIGURATION) $(OUTPUT_DIR_TESTRESULTS)

test: tests

tests : build unittests integrationtests

publish:
	@cd src && dotnet publish \
		--configuration $(CONFIGURATION) \
		--output $(OUTPUT_DIR_APP) \
		--no-build \
		--no-restore \
		--nologo \
		-v q \
		$(APP_PROJECT)

appcontainer:
	@docker build -t ${APP_IMAGE_NAME} .

migrationcontainer:
	@docker build -t ${DB_IMAGE_NAME} ./db

containers: appcontainer migrationcontainer

ci: clean restore build test publish containers

dev:
	@cd src && dotnet watch --project $(APP_PROJECT) run -- --configuration $(CONFIGURATION)

run:
	@cd src && dotnet run --configuration $(CONFIGURATION) --project $(APP_PROJECT)

# Like `run`, but merges the launch profile's environmentVariables and applicationUrl
# with the shell environment; variables already set in the shell take precedence.
# `dotnet run` alone either lets the profile override the shell or ignores it
# (--no-launch-profile). LAUNCH_PROFILE selects the profile (default: the first one).
LAUNCH_SETTINGS=SelfService/Properties/launchSettings.json
run-with-env: SHELL := /bin/bash
run-with-env:
	@set -euo pipefail; cd src; \
	command -v jq >/dev/null || { echo "jq is required" >&2; exit 1; }; \
	profile="$${LAUNCH_PROFILE:-$$(jq -r '.profiles | keys_unsorted[0]' $(LAUNCH_SETTINGS))}"; \
	jq -e --arg p "$$profile" '.profiles[$$p]' $(LAUNCH_SETTINGS) >/dev/null \
		|| { echo "launch profile '$$profile' not found in $(LAUNCH_SETTINGS)" >&2; exit 1; }; \
	while IFS= read -r -d '' key && IFS= read -r -d '' value; do \
		if [[ -z "$${!key+x}" ]]; then export "$$key=$$value"; fi; \
	done < <(jq -j --arg p "$$profile" \
		'.profiles[$$p].environmentVariables // {} | to_entries[] | .key, "\u0000", (.value | tostring), "\u0000"' \
		$(LAUNCH_SETTINGS)); \
	if [[ -z "$${ASPNETCORE_URLS+x}" ]]; then \
		url="$$(jq -r --arg p "$$profile" '.profiles[$$p].applicationUrl // empty' $(LAUNCH_SETTINGS))"; \
		if [[ -n "$$url" ]]; then export ASPNETCORE_URLS="$$url"; fi; \
	fi; \
	exec dotnet run --configuration $(CONFIGURATION) --project $(APP_PROJECT) --no-launch-profile

newmigration:
	@read -p "Enter name of migration (e.g. add-table-for-xxx):" REPLY; \
	   echo "-- $(shell date +'%4Y-%m-%d %H:%M:%S') : $$REPLY" > db/migrations/$(shell date +'%4Y%m%d%H%M%S')_$$REPLY.sql

nm: newmigration

clean-restore-build: clean restore build
setup-pre-commit-hook:
	@sh ./setup-pre-commit-hook.sh

pre-commit-hook:
	@sh ./.git/hooks/pre-commit
