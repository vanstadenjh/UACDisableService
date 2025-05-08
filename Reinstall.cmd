set SERVICE_NAME=UACDisableService
set EXECUTABLE_PATH="C:\DEV\Repos\UACDisableService\UACDisableService\bin\Debug\UACDisableService.exe"

echo Stopping the service...
net stop %SERVICE_NAME%

echo Uninstalling the service...
sc delete %SERVICE_NAME%

echo Installing the service...
sc create %SERVICE_NAME% binPath= %EXECUTABLE_PATH% start= auto DisplayName= "UACOverride"

echo Starting the service...
net start %SERVICE_NAME%

cmd /k