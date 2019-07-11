
az keyvault secret set -n "AdobeAppID" --vault-name "[your keyvault name]" --value "[your app ID from Adobe]"
az keyvault secret set -n "AdobeAppSecret" --vault-name "[your keyvault name]" --value "[your app secret from Adobe]"
az keyvault secret set -n "AdobeURI" --vault-name "[your keyvault name]" --value "[URI provisioned by Adobe]"
az keyvault secret set -n "EventHubConnection" --vault-name "[your keyvault name]" --value "[EventHub connection string]"
az keyvault secret set -n "IngestControlQueueConnection" --vault-name "[your keyvault name]" --value "[storage connection string]"
