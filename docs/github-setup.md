# GitHub and continuous integration

The repository root is museum-explorer (the folder containing frontend, backend, cms and .github).

## What CI does

Every push and pull request starts three independent jobs:
- backend: restore, compile and run the .NET tests.
- frontend: install the locked npm dependencies, type-check and build Next.js.
- cms: install the locked dependencies, type-check, build Studio and extract its schema.

You can also start the workflow manually from the Actions tab after pushing it.
The workflow has read-only repository permissions and a 15-minute limit per job.

No SQL Server, live Sanity account, API tokens or GitHub repository secrets are required for these tests. The backend tests use fake content providers or mocked GraphQL responses; Studio uses a build-only project ID. These checks do not replace the live Sanity/SQL smoke test.

This is CI only. It does not publish Sanity content or deploy the application to AWS.

## First push

Create an empty GitHub repository with the intended visibility. Do not initialise it with another README, license or .gitignore. From the museum-explorer root, use the actual repository URL:

```powershell
git remote add origin https://github.com/YOUR-USERNAME/museum-explorer.git
git push -u origin main
```

Authenticate through Git's sign-in flow if prompted. Do not paste access tokens into chat or embed them in the remote URL.

If the remote already contains work, fetch and inspect it before merging; never use a force push to replace it.

## Verify the first run

Open the repository's Actions tab, select Build and test and open the newest run. Confirm backend, frontend and cms are green. For a red job, open the failed step to see its error output. A successful local check does not mean GitHub has run successfully.

## Normal development

```powershell
git switch -c feature/your-change
# Make and test changes.
git add <specific-files>
git commit -m "Describe the change"
git push -u origin feature/your-change
```

Open a pull request to main and inspect its checks before merging.

## Files excluded from Git

.env.local files, installed node_modules, build output, database files, local credentials and tooling state are ignored. The example environment files and lockfiles are included intentionally. .NET user secrets are stored outside the repository.

## Current status

Local Git and CI are prepared. GitHub repository creation, first push and a hosted Actions run still require the chosen repository destination and authenticated access.

The user reported successful SQL Server and Sanity end-to-end verification before this GitHub milestone.
