# PObserve Commons

Common utilities for PObserve runtime monitoring.

## Publishing to Maven Central

This project is configured to publish to Maven Central using Gradle and GitHub Actions.

### Prerequisites

To publish to Maven Central, you need:

1. Sonatype OSSRH account
2. GPG key for signing the artifacts
3. GitHub repository secrets configured

### Setting Up GitHub Secrets

Add these secrets to your GitHub repository:

- `MAVEN_USERNAME`: Your Sonatype OSSRH username
- `MAVEN_PASSWORD`: Your Sonatype OSSRH password
- `GPG_PRIVATE_KEY`: Your ASCII-armored GPG private key
- `GPG_PASSPHRASE`: Your GPG key passphrase

### Publishing Methods

#### Method 1: Using GitHub Actions

1. Go to the "Actions" tab in your GitHub repository
2. Select the "Publish PObserveCommons to Maven Central" workflow
3. Click "Run workflow"
4. Enter the version number (e.g., "1.0.0")
5. Click "Run workflow" again

The workflow will:
- Check out the code
- Set up Java and GPG
- Build the project
- Publish to Maven Central using your credentials

#### Method 2: Manually Publishing

To publish manually from your local environment:

```bash
# Set environment variables for credentials
export MAVEN_USERNAME=your_sonatype_username
export MAVEN_PASSWORD=your_sonatype_password

# Make sure your GPG key is available to the system

# Execute the publish task
./gradlew publish
```

### Gradle Specifics

The project is configured with:
- Group ID: io.github.p-org
- Artifact ID: PObserveCommons
- Current version: 1.0.0

The publishing configuration is in `build.gradle.kts` and includes:
- Maven POM configuration
- Sonatype OSSRH repository configuration
- GPG signing
- Source and Javadoc JAR generation

### After Publishing

After successful publication:

1. Login to [Sonatype OSSRH](https://s01.oss.sonatype.org/)
2. Navigate to "Staging Repositories"
3. Find your repository
4. Close the repository (may take a few minutes for validation)
5. Once closed successfully, click "Release"

Your artifacts will be synced to Maven Central within a few hours.
