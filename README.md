# Use Azure Files in an AKS Pod

This example shows you how you can use Azure Files in a AKS Pod.

## Description

Conatiner application often needs acces to external data in an external data volume. Azure files can be used as external data store. In this example we create a volume to an existing Azure File share. 
An ASP.NET Core Api acesses files in that share. After the deployment you can use the swagger UI to access, delete and upload files.

### Create an Azure storage account and a share

To create an Azure storage account and the file share you can use either Azure CLI or Powershell.

``` Azure CLI
#!/bin/bash

# Change these four parameters
AKS_PERS_STORAGE_ACCOUNT_NAME=<your storage account name>
AKS_PERS_RESOURCE_GROUP=<your resource group name>
AKS_PERS_LOCATION=<your resource group location>
AKS_PERS_SHARE_NAME=<your share name>

# Create the Resource Group
az group create --name $AKS_PERS_RESOURCE_GROUP --location $AKS_PERS_LOCATION

# Create the storage account
az storage account create -n $AKS_PERS_STORAGE_ACCOUNT_NAME -g $AKS_PERS_RESOURCE_GROUP -l $AKS_PERS_LOCATION --sku Standard_LRS

# Export the connection string as an environment variable, this is used when creating the Azure file share
export AZURE_STORAGE_CONNECTION_STRING=`az storage account show-connection-string -n $AKS_PERS_STORAGE_ACCOUNT_NAME -g $AKS_PERS_RESOURCE_GROUP -o tsv`

# Create the file share
az storage share create -n $AKS_PERS_SHARE_NAME

# Get storage account key
STORAGE_KEY=$(az storage account keys list --resource-group $AKS_PERS_RESOURCE_GROUP --account-name $AKS_PERS_STORAGE_ACCOUNT_NAME --query "[0].value" -o tsv)

# Echo storage account name and key
echo Storage account name: $AKS_PERS_STORAGE_ACCOUNT_NAME
echo Storage account key: $STORAGE_KEY
```

```Powershell
$resourceGroupName = "MyTemp-RG"
$storageAccountName = "anmocktempaccount"
$location = "westeurope"
$shareName = "myshare"

#Create resource group
$rg = New-AzureRmResourceGroup`
            -Name $resourceGroupName`
            -Location $location
#create storage account
$storage = New-AzureRmStorageAccount`
            -ResourceGroupName $resourceGroupName`
            -Name $storageAccountName`
            -SkuName Standard_LRS`
            -Location $location

#Create file share
New-AzureStorageShare`
            -Name $shareName`
            -Context $storage.Context

#Get storage key
$key = Get-AzureRmStorageAccountKey`
            -ResourceGroupName $resourceGroupName`
            -Name $storageAccountName
$key[0].Value
```

### Create a Kubernetes secrets

Kubernetes needs the credentials to access the file share. We store the credentials in a Kubernetes secrets and refernce it later when we create the deployment. 

``` console
kubectl create secret generic azure-secret --from-literal=azurestorageaccountname=<your storage account name>--from-literal=azurestorageaccountkey=<your storage account key>
```

### Mount file share as volume

In the deployment directory you can see the following yaml file that describes the ![deployment](src/deployment/deployment.yaml) of the example application. Please replace all variables between <> with your values. 

``` yaml
kind: Service
apiVersion: v1
metadata:
  name:  fileapisvc
spec:
  selector:
    app:  fileapi
  type:  LoadBalancer
  ports:
  - name:  http
    port:  80
    targetPort:  80
    protocol: TCP
---
kind: Deployment
apiVersion: extensions/v1beta1
metadata:
  name: fileapibackend
spec:
  replicas: 2
  minReadySeconds: 5
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 1
  template:
    metadata:
      labels:
        app: fileapi
    spec:
      containers:
      - name: fileapi
        image: <your regitsry name>.azurecr.io/fileapi
        ports:
        - containerPort: 80
        volumeMounts:
          - name:  azureshare
            mountPath:  /share
      imagePullSecrets:
        - name:  <your image registry secret>
      volumes:
        - name:  azureshare
          azureFile:
            secretName: azureshare-secret
            shareName: <your Azure file share name>
            readOnly: false
```

### Build the docker image and push it to your azure container registry

Switch to directory /src/fileapi and execute the following steps

``` console
az login
az acr login --name <your regitsry name>
docker build -t fileapi .
docker tag fileapi <your registry>.azurecr.io/fileapi
docker push <your regitsry>.azurecr.io/fileapi
```

Switch to directory src/deployment and apply the deployment.

``` console
kubectl create -f deployment.yaml
```

### Browse the example application

Get the IP address of the example service.

``` console
kubectl get service
```

Copy the IP address, open your browser, paste IP address and add /swagger to see the Swagger UI.