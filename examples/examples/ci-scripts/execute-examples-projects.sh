
# Path where packaged worker file (tgz) exists.

PROJECT_NAME=$1
PROJECT_CONFIG=$2
DOTNET_SPARK_VERSION=$3
DOTNET_ALIAS=$4

mkdir -p "$DESTINATION_PATH"
cd $DESTINATION_PATH

echo "PROJECT_NAME ${PROJECT_NAME}"
echo "PROJECT_CONFIG ${PROJECT_CONFIG}"
echo "DOTNET_SPARK_VERSION ${DOTNET_SPARK_VERSION}"
echo "DOTNET_ALIAS ${DOTNET_ALIAS}"

cd $DLL_PATH

spark-submit \
    --class org.apache.spark.deploy.dotnet.DotnetRunner \
    --master local \
    microsoft-spark-2.4.x-$DOTNET_SPARK_VERSION.jar \
    dotnet "/bin/$PROJECT_CONFIG/$DOTNET_ALIAS/$PROJECT_NAME"
