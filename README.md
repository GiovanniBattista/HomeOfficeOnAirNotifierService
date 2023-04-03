# HomeOfficeOnAirNotifierService

This is a Windows service that notifies a REST (OpenHAB) endpoint that the camera and/or microphone is in use by any application on the computer this service is running on. 

Note: The Windows service runs as `Local System` service account to be able to query WMI events.

## Configuration
Checkout the git repository and create a `App.config` file of the following form:<br>
```
<configuration>
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
    </startup>
	<appSettings>
    <!-- either -->
		<add key="LoggedOnSAMUser" value="" />
    <!-- or (preferred) -->
		<add key="LoggedOnUserSID" value="" />
		
		<!-- Configuration for the Openhab Endpoint -->
		<add key="BaseEndpointUrl" value="http://192.168.100.100/" />
		<add key="MicrophoneEndpointPath" value="/rest/items/Microphone/state" />
		<add key="CameraEndpointPath" value="/rest/items/Camera/state" />
		<add key="BearerHeaderValue" value="oh." />
	</appSettings>
</configuration>
```

## Building
Open Visual Studio (Code) and build the Release solution.

## Install Windows Service
Use `Install.ps1` to install the Windows service or `Uninstall.ps1` to uninstall it.
