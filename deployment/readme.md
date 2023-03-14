# install sensenet demo on local docker with powershell

## prerequisits

- powershell 

Obviously as this installer made with powershell scripts you have to have powershell.
By default the windows will ask confirmation before run every script, so it is recommended to set ExecutionPolicy for the current process, e.g.:
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass 
```
This change on process scope will only affect the current powershell session.

See  [Microsoft documentation about execution policy](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy?view=powershell-7.3#example-6-set-the-execution-policy-for-the-current-powershell-session)
	
- docker 

The installer use docker cli and works with docker containers

- docker images

To install sensenet you will have to have the appropriate docker images to start the proper containers. There is no public images at the moment so we prepared the script to create these images for you on your local docker registry. At first run simply use `-CreateImages` switch with the script.

## simple install

The script is preconfigured so you don't have to know every switch if you just want to try it out. Except `-CreateImages` for the first run, but hopefully soon it won't be necessary either. With the preconfigured settings the installer will create an identityserver, an mssql server and a sensenet application container. Neither will be mount volumes so the necessary files will be either copied into the containers or created inside them. Example: 
```powershell
.\install-sensenet.ps1 
```
note: `-CreateImages` switch is needed for the first run

## switches

- InMemPlatform 

By default the installer will create a sensenet repository that uses mssql database, but sample sensenet projects have a version of in memory sensenet that does not need a database. This version will start an identity server and a sensenet application container only.
```powershell
.\install-sensenet.ps1 -InMemPlatform
```

- CreateImages

The script started with `-CreateImages` switch will download the necessary github repositories and create the appropriate docker images for every service that the specific version set of sensenet will be using. So if the installer started with `-SearchService`, it will create the appropriate sensenet docker image and search service image as well, and so on. These temporary files will be created under `./temp` folder and the images will be created on the host docker registry. It is a necessary step for the first run as no images published yet.
```powershell
.\install-sensenet.ps1 -CreateImages
```

- SearchService

Sample sensenet set for nlb environments. The indexing is not handled by the sensenet application but with a separate search service. It will start four containers: Identityserver, mmsql server, search service, sensenet application and a rabbitmq for the messaging between services.

- HostDb with DataSource, SqlUser and SqlPsw

You can try this sensenet demo with you local database on the host machine. `-HostDb` switch will use this mode. However you will need an sql user on your mssql server at least with database creation permission. The script will autmatically use host name as datasource. If your mssql server name is different from your host name, you will have to set with `-DataSource` parameter. You can set `-SqlUser` and `-SqlPsw` parameters whit the script or the installer will be ask for these at run time. Exmaple:
```powershell
.\install-sensenet.ps1 -DataSource MyMachineHostName\Sql2019 -HostDb -SqlUser testuserfordockerdemo -SqlPsw Ultr4Secur3P4ssw0rd 
```
- UseVolume and VolumeBasePath

It's a bit advanced switch as docker for windows with linux containers may have a problem. By default with this demo containers will be created without volume bind, so it should work on every system. But it is recommended to use volumes with an actual sensenet project in containers. This switch shows how to bind those volumes for sensenet services. However with default settings it may only work on linux host. If you use docker for windows you may have to set the folder path to bind and this path should be on linux. It is because windows and linux file system handle file locks differently and the lucene engine from the linux containers will not be able to lock necessary files on windows file system.

so while on linux it will be enough to call 
```powershell
.\install-sensenet.ps1 -UseVolume
```

on windows it will be something like this:
```powershell
.\install-sensenet.ps1 -UseVolume -VolumeBasePath /var/lib/docker/volumes
```
note: above example will bind WSL paths with the containers

- OpenInBrowser

With this switch the installer will open the admin ui when the repository is created.

- Uninstall

This switch is responsible to clean after an installation. You should use the same switches as with the install to remove the same resources.

- DryRun

This switch can be used to se what process would be executed but without actually run them.

- Verbose

Output is reduced by default to decrease the amount of install infromation. With `-Verbose` switch additional, mostly technical information will be shown. For example the actual docker command is the most useful of them.