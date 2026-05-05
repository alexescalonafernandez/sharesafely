# ShareSafely operational documentation

These documents capture ShareSafely deployment, infrastructure-as-code (IaC), monitoring, cleanup lifecycle, OIDC bootstrap, teardown/rebuild strategy, and related operational decisions for this dev/demo portfolio project.

## Documentation index
- [Bicep IaC baseline](https://github.com/alexescalonafernandez/sharesafely/blob/main/docs/operations/bicep-iac-baseline.md)
- [GitHub Actions OIDC deployment](https://github.com/alexescalonafernandez/sharesafely/blob/main/docs/operations/github-actions-oidc-deployment.md)
- [Monitoring baseline](https://github.com/alexescalonafernandez/sharesafely/blob/main/docs/operations/monitoring-baseline.md)
- [Bicep modularization](https://github.com/alexescalonafernandez/sharesafely/blob/main/docs/operations/bicep-modularization.md)
- [Blob cleanup lifecycle](https://github.com/alexescalonafernandez/sharesafely/blob/main/docs/operations/blob-cleanup-lifecycle.md)
- [OIDC bootstrap](https://github.com/alexescalonafernandez/sharesafely/blob/main/docs/operations/oidc-bootstrap.md)
- [Teardown and rebuild strategy](https://github.com/alexescalonafernandez/sharesafely/blob/main/docs/operations/teardown-and-rebuild.md)

## Useful repository links
- [README.md](https://github.com/alexescalonafernandez/sharesafely/blob/main/README.md)
- [infra/bicep/main.bicep](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/main.bicep)
- [infra/bicep/dev.bicepparam](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/dev.bicepparam)
- [infra/bicep/modules/storage.bicep](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/modules/storage.bicep)
- [infra/bicep/modules/monitoring.bicep](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/modules/monitoring.bicep)
- [infra/bicep/modules/webapp.bicep](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/modules/webapp.bicep)
- [infra/bicep/modules/rbac.bicep](https://github.com/alexescalonafernandez/sharesafely/blob/main/infra/bicep/modules/rbac.bicep)
- [.github/workflows/deploy-webapp.yml](https://github.com/alexescalonafernandez/sharesafely/blob/main/.github/workflows/deploy-webapp.yml)
- [scripts/azure/bootstrap-oidc.ps1](https://github.com/alexescalonafernandez/sharesafely/blob/main/scripts/azure/bootstrap-oidc.ps1)
- [scripts/azure/validate-oidc-bootstrap.ps1](https://github.com/alexescalonafernandez/sharesafely/blob/main/scripts/azure/validate-oidc-bootstrap.ps1)
- [Issue #8](https://github.com/alexescalonafernandez/sharesafely/issues/8)

> These notes are operational documentation for a dev/demo portfolio project and are not production runbooks.
