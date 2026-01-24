# HomeOfficeOnAirNotifierService

This is a Windows service that notifies a REST (OpenHAB) endpoint that the camera and/or microphone is in use by any application on the computer this service is running on. 

Note: The Windows service runs as `Local System` service account to be able to query WMI events.

## Configuration
Checkout the git repository and create a `App.config` file of the following form:<br>
```xml
<configuration>
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
    </startup>
	<appSettings>
		<!-- 
        Provide either LoggedOnUserSID or LoggedOnUserSID (preferred) in <appSettings>
        Both can be found in Registry under HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\SessionData' 
        -->
		<add key="LoggedOnSAMUser" value="" />
		<add key="LoggedOnUserSID" value="" />

		<!-- Hardware configuration -->
        <add key="MicrophoneIDInQuestion" value="" />
		
		<!-- Configuration for the Openhab Endpoint -->
		<add key="BaseEndpointUrl" value="http://192.168.100.100/" />
		<add key="MicrophoneEndpointPath" value="/rest/items/Microphone/state" />
		<add key="CameraEndpointPath" value="/rest/items/Camera/state" />
		<add key="BearerHeaderValue" value="" /> <!-- You can generate one in OpenHAB in your User profile (bottom left) -->
	</appSettings>
	<runtime>
		<assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
			<dependentAssembly>
			<assemblyIdentity name="NAudio.Core" publicKeyToken="e279aa5131008a41" culture="neutral" />
			<bindingRedirect oldVersion="0.0.0.0-2.2.1.0" newVersion="2.2.1.0" />
			</dependentAssembly>
			<dependentAssembly>
			<assemblyIdentity name="NAudio.WinMM" publicKeyToken="e279aa5131008a41" culture="neutral" />
			<bindingRedirect oldVersion="0.0.0.0-2.2.1.0" newVersion="2.2.1.0" />
			</dependentAssembly>
			<dependentAssembly>
			<assemblyIdentity name="NAudio.Wasapi" publicKeyToken="e279aa5131008a41" culture="neutral" />
			<bindingRedirect oldVersion="0.0.0.0-2.2.1.0" newVersion="2.2.1.0" />
			</dependentAssembly>
			<dependentAssembly>
			<assemblyIdentity name="Microsoft.Win32.Registry" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
			<bindingRedirect oldVersion="0.0.0.0-5.0.0.0" newVersion="5.0.0.0" />
			</dependentAssembly>
		</assemblyBinding>
		</runtime>
</configuration>
```

## Building
Open Visual Studio and build the Release solution.

## Install Windows Service
Use `Install.ps1` to install the Windows service or `Uninstall.ps1` to uninstall it.
Note: Everyting is logged into `C:\ProgramData\HomeOfficeOnAirNotifierService\output.log`

## Roadmap
- [ ] Check when the microphone is muted in Google Meets (if possible)
- [x] Keep the service running if a configured microphone is currently `NotPresent` (e.g. USB microphone).
- [x] Reload config instead of having the service to restart when the config changes
