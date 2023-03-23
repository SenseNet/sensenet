# Install sensenet on local Docker with Powershell

This article describes how to install sensenet in a docker environment on your local machine. It is a Powershell script that will install sensenet from Docker images. It will create a Docker network and start the necessary containers.

## Prerequisites
Before you begin, please take a look at the prerequisites.

### Docker 

The installer uses the docker cli and works with docker containers. You have to have docker installed on your machine.

By default the installer uses the publicly available sensenet Docker images.

### Powershell 

As this installer is built of Powershell scripts you have to have Powershell installed on your machine. 

The installer is built of multiple scripts. By default Windows will ask confirmation before running every script, so it is recommended to set the _ExecutionPolicy_ for the current process:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass 
```

This change on process scope will only affect the current powershell session. To learn more see the documentation on [execution policy](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy?view=powershell-7.3#example-6-set-the-execution-policy-for-the-current-powershell-session).
	
### dotnet cli OR a valid certificate

Sensenet services will use the `snapp.pfx` certificate file from under `./temp/certificates` folder. If it is not present the installer will create a developer certificate in this folder and it uses dotnet cli for this. On linux the dotnet `--trust` switch won't work. so you have to either create or trust the created certificate manually.

### git cli (in case of source install)
By default the installer uses the publicly available sensenet Docker images. In case you want to create the Docker images from source code, you will have to have the _git cli_ installed. The _CreateImages_ switch will download the necessary git repositories in order to create Docker images from sensenet service solutions on-the-fly and it uses the git cli for this.

## Simple install

The script is preconfigured so you don't have to know every switch if you just want to try it out. With the preconfigured settings the installer will create an IdentityServer, an MS SQL Server and a sensenet application container. Neither will have mounted volumes so the necessary files will be either copied into the containers or created inside them.

To install sensenet, execute the following script:

```powershell
.\install-sensenet.ps1 
```

After the script completes, your repository will be available on the service url `https://localhost:51016`. You can log in with the following credentials by visiting [https://admin.sensenet.com](https://admin.test.sensenet.com/?repoUrl=https%3A%2F%2Flocalhost%3A51016) :

```text
username: admin
password: admin
```

## Parameters
You can customize the installation with the following parameters:

### InMemPlatform 

By default the installer will create a sensenet repository that uses an MS SQL database, but there is an in memory sensenet repository that does not need a physical database. This version will start an identity server and a sensenet application container only.

```powershell
.\install-sensenet.ps1 -InMemPlatform
```

### CreateImages

The `-CreateImages` switch will download the source code of the necessary repositories from GitHub and create the appropriate Docker images for sensenet services. For example if you executed the installer with the `-SearchService` switch, it will create the appropriate sensenet application Docker image and the search service image as well, and so on. These temporary folders will be created under the `./temp` folder and the images will be created on the host Docker registry.

```powershell
.\install-sensenet.ps1 -CreateImages
```

### SearchService

Sample sensenet set for NLB environments. Indexing is not handled by the sensenet application but by a separate search service. It will start the following containers:

- IdentityServer
- MS SQL Server
- Search service
- sensenet application
- RabbitMQ service for messaging

### HostDb with DataSource, SqlUser and SqlPsw

You can try this sensenet demo with your local database on the host machine. The `-HostDb` switch will initiate this mode. However you will need a SQL user on your MS SQL Server at least with database creation permission. The script will automatically use the host name as datasource. If your MS SQL Server name is different from your host name, you will have to set it with the `-DataSource` parameter. You can also set the `-SqlUser` and `-SqlPsw` parameters when running the installer otherwise it will ask for these at run time. For example:

```powershell
.\install-sensenet.ps1 -DataSource MyMachineHostName\Sql2019 -HostDb -SqlUser testuserfordockerdemo -SqlPsw Ultr4Secur3P4ssw0rd 
```
### UseVolume and VolumeBasePath

This is a bit advanced switch as Docker for Windows with linux containers may have a problem with the default setup. By default containers will be created **without volume bind**, so it should work on every system. But it is recommended to use volumes with an actual sensenet project in containers. This switch shows how to bind those volumes for sensenet services. However with default settings it may only work on a linux host. If you use Docker for Windows you may have to set the folder path to bind and this path should be on linux. It is because Windows and linux file systems handle file locks differently and the Lucene engine from the linux containers will not be able to lock the necessary files on a Windows file system.

While on linux it is enough to call 

```powershell
.\install-sensenet.ps1 -UseVolume
```

on Windows it should be something like this:

```powershell
.\install-sensenet.ps1 -UseVolume -VolumeBasePath /var/lib/docker/volumes
```

> The example above will bind the WSL paths with the containers.

### OpenInBrowser

With this switch the installer will open the admin ui when the repository is created.

### Uninstall

This switch is responsible for cleanup after an installation. You should use the same switches as with the install to remove the same resources.

### DryRun

This switch can be used to see what processes would be executed but without actually running them.

### Verbose

The output is reduced by default to decrease the amount of install information. With the `-Verbose` switch all the additional technical information will be shown. For example the actual Docker command is shown that can be useful if you need to customize the installation.