CONFIGURATION=Debug
ROOT_DIR=${PWD}
OUTPUT_DIR=${PWD}/.output
OUTPUT_DIR_APP=$(OUTPUT_DIR)/app
OUTPUT_DIR_DB=$(OUTPUT_DIR)/db
OUTPUT_DIR_MANIFESTS=$(OUTPUT_DIR)/manifests
OUTPUT_DIR_TESTRESULTS=$(OUTPUT_DIR)/testresults
APP_PROJECT=SelfService/SelfService.csproj
APP_IMAGE_NAME=selfservice-api/app
DB_IMAGE_NAME=selfservice-api/dbmigrations
BUILD_NUMBER?=n/a

init: clean restore build tests

clean:
	@-rm -Rf $(OUTPUT_DIR)
	@mkdir $(OUTPUT_DIR)
	@mkdir $(OUTPUT_DIR_APP)
	@mkdir $(OUTPUT_DIR_DB)
	@mkdir $(OUTPUT_DIR_MANIFESTS)
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

tests: test

test:
	@cd src && dotnet test \
		--configuration $(CONFIGURATION) \
		--no-restore \
		--no-build \
		--logger "trx;LogFileName=testresults.trx" \
		--filter Category!=Integration \
		--collect "XPlat Code Coverage" \
		--nologo \
		-v normal

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

# migrationcontainer:
# 	@docker build -t ${DB_IMAGE_NAME} ./db

containers: appcontainer #migrationcontainer

ci: clean restore build test publish containers

manifests:
	@for m in k8s/*.yml; do envsubst <$$m >$(OUTPUT_DIR_MANIFESTS)/$$(basename $$m); done
	@for m in k8s/*.yml; do envsubst < $$m > $(OUTPUT_DIR_MANIFESTS)/$$(basename $$m); done

deliver:
	@sh ./tools/push-container.sh "$(APP_IMAGE_NAME)" "${BUILD_NUMBER}"
	# @sh ./tools/push-container.sh "$(DB_IMAGE_NAME)" "${BUILD_NUMBER}"

cd: ci manifests deliver

dev:
	@cd src && dotnet watch --project $(APP_PROJECT) run -- --configuration $(CONFIGURATION)

run:
	@cd src && dotnet run --configuration $(CONFIGURATION) --project $(APP_PROJECT)
