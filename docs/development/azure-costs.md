# Azure cost notes

No Azure resources are deployed by this task. `infra/bicep/main.bicep` is an unvalidated deployment design until `az bicep build` and a review succeed.

The largest expected cost drivers are PostgreSQL Flexible Server compute/storage/backups, Container Apps minimum replicas, Log Analytics ingestion/retention, Service Bus tier, and outbound traffic. Blob Storage and Key Vault are usually secondary at this scale. Exact prices vary by region and date; use the Azure Pricing Calculator immediately before approval.

Cost controls for a future development environment should include one burstable PostgreSQL instance, small Container Apps resources, low maximum replicas, short non-production log retention, local development instead of shared cloud dependencies, budgets/alerts, and a documented teardown owner. Production sizing, high availability, private networking, backup retention, and disaster recovery must be decided from service objectives, not copied from the development defaults.
