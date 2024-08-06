#!/bin/bash
readonly START_TIME=$SECONDS
readonly CONFIGURATION=${1:-"Debug"}
readonly OUTPUT_DIR_TESTRESULTS=${2:-$PWD}
readonly BIN_DIR="${PWD}/bin"
readonly TEST_PROJECT_NAME="SelfService.Tests"
readonly INTEGRATION_TEST_NAME="itst"
readonly DB_CONNECTION_TIMEOUT=2
readonly DB_COMMAND_TIMEOUT=2

rm -Rf ${BIN_DIR}
mkdir ${BIN_DIR}

echo -e "\e[35mClosing any previous containers...\e[0m"
docker compose -p ${INTEGRATION_TEST_NAME} down -v

echo -e "\e[35mCopying test dlls...\e[0m"
dotnet publish \
    --configuration ${CONFIGURATION} \
    --output ${BIN_DIR} \
    --no-build \
    --no-restore \
    --nologo \
    --verbosity quiet \
    ../src/${TEST_PROJECT_NAME}/${TEST_PROJECT_NAME}.csproj

echo -e "\e[35mSpinning up containers...\e[0m"
docker compose -p ${INTEGRATION_TEST_NAME} up --build -d

echo -e "\e[35mGetting port of database container...\e[0m"
PORT=$(docker compose -p ${INTEGRATION_TEST_NAME} port database 5432 | awk -F : '{print $2}')

MIGRATION_CONTAINER_NAME=$(docker compose -p ${INTEGRATION_TEST_NAME} ps --all | grep -i 'db-migration' | awk '{print $1}')

echo "container name: ${MIGRATION_CONTAINER_NAME}"

echo -e "\e[35mWaiting for database to be migrated...\e[0m"
migration_result=$(docker wait ${MIGRATION_CONTAINER_NAME})
if [ "$migration_result" != "0" ]
then
   echo -e "\e[31mDatabase migration FAILED!!\e[0m"
   echo -e "\e[33mHere is the logs from the container:\e[0m"
   docker logs ${MIGRATION_CONTAINER_NAME}

   echo ""
   echo -e "\e[33mClosing containers...\e[0m"
   docker compose -p ${INTEGRATION_TEST_NAME} down -v

   exit -1
fi

echo -e "\e[35mRunning tests...\e[0m"
dotnet test \
    --filter Category=Integration \
    --logger 'trx;LogFileName=testresults.trx' \
    --collect 'XPlat Code Coverage' \
    --nologo \
    --verbosity quiet \
    --environment SS_CONNECTION_STRING="User ID=postgres;Password=p;Host=localhost;Port=$PORT;Database=db;Timeout=$DB_CONNECTION_TIMEOUT;Command Timeout=$DB_COMMAND_TIMEOUT;" \
    ${BIN_DIR}/${TEST_PROJECT_NAME}.dll

LAST_EXIT_CODE=$?

echo -e "\e[35mClosing containers...\e[0m"
docker compose -p ${INTEGRATION_TEST_NAME} down -v

echo -e "\e[35mCopying test result...\e[0m"
cp ${PWD}/TestResults/testresults.trx $OUTPUT_DIR_TESTRESULTS/integration_testresults.trx

echo -e "\e[35mCleaning up...\e[0m"
rm -Rf ${BIN_DIR}
rm -Rf ${PWD}/TestResults

# ELAPSED_TIME=$(($SECONDS - $START_TIME))
# echo ""
# echo "Time Elapsed: $ELAPSED_TIME seconds."

if [ "$LAST_EXIT_CODE" == "0" ]
then
   echo -e "\e[32mSuccess!\e[0m"
else
   echo -e "\e[31mTotally FAILED!!\e[0m"
   exit ${LAST_EXIT_CODE}
fi
