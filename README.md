

### SharpRDP - Remote Desktop Protocol Console Application for Authenticated Command Execution

```
To compile open the project in Visual Studio and build for release. Two DLLs will be output to the Release directory, you do not need those because the DLLs are in the assembly.
```

```
Regular RDP connection and execution
  SharpRDP.exe computername=target.domain command="C:\Temp\file.exe" username=domain\user password=password
```

```
Exec program as child process of cmd or powershell
  SharpRDP.exe computername=target.domain command="C:\Temp\file.exe" username=domain\user password=password exec=cmd
```
  
```
Use restricted admin mode
  SharpRDP.exe computername=target.domain command="C:\Temp\file.exe"
```
If restricted admin mode is enabled on the target do not specify any credentials and it will use the current user context. Can `PTH` or `make_token` in beacon or `runas /netonly` on a windows system.

All execution starts with the Windows run dialog (Win+R). There will be a registry key created at `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU` with the command that you executed. If you want to remove this you can use: [CleanRunMRU: Get or clear RunMRU values](https://github.com/0xthirteen/CleanRunMRU)

Keep in mind if you execute a program like msbuild (I'm sure there are others) a cmd window will pop up while the process is running. If you do it would probably be best to migrate the process and kill the original. 

The required DLLs are compiled into the assembly and app domain assembly resolve event is used. Because of the size of the DLLs they are compressed and decompressed at runtime (so they could meet beacon's 1MB size limit).

Blog about it found here [SharpRDP](https://0xthirteen.com/2020/01/21/revisiting-remote-desktop-lateral-movement/)