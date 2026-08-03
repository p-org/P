# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Official P toolchain image.
#
# Bundles everything needed to compile and check P programs:
#   - .NET SDK 8.0   (the P compiler / PChecker are implemented in C#)
#   - JDK 17 + Maven (the PEx / PSym checker backends run on the JVM; the P
#                     Java sources target Java 17 -- see Src/PEx/pom.xml)
#   - graphviz       (used to render coverage / state-machine diagrams)
#   - the `p` CLI    (built from this repository and installed as a global
#                     dotnet tool)
#
# Build:   docker build -t p .
# Run:     docker run --rm -it -v "$PWD":/workspace p
# ---------------------------------------------------------------------------

# --- Stage 1: build the P tool from the repository source ------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# The P compiler build runs the ANTLR4 code generator, which shells out to
# `java`, so a JDK is required even just to `dotnet pack` the tool.
RUN apt-get update \
    && apt-get install -y --no-install-recommends openjdk-17-jdk-headless \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

# Pack the `p` command-line tool into a local NuGet package. This mirrors the
# `dotnet pack` step used by the release workflow; the resulting .nupkg lands
# under Bld/Drops/Release/Binaries/ (see Directory.Build.props).
RUN dotnet pack Src/PCompiler/PCommandLine/PCommandLine.csproj -c Release \
    && mkdir -p /nupkg \
    && cp Bld/Drops/Release/Binaries/[Pp].*.nupkg /nupkg/

# --- Stage 2: the toolchain image ------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0

LABEL org.opencontainers.image.title="P" \
      org.opencontainers.image.description="Toolchain image for the P formal modeling language (P compiler, PChecker, PEx). Includes .NET 8, JDK 17, Maven and graphviz." \
      org.opencontainers.image.source="https://github.com/p-org/P" \
      org.opencontainers.image.documentation="https://p-org.github.io/P/" \
      org.opencontainers.image.licenses="MIT"

# Install the JVM toolchain (PEx/PSym backends) and graphviz. openjdk-17 is
# available for both amd64 and arm64 in the Debian repositories used by the
# .NET SDK base image, so this image builds natively on both architectures.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        openjdk-17-jdk-headless \
        maven \
        graphviz \
    && rm -rf /var/lib/apt/lists/*

# The JDK install path is arch-specific in the Debian layout
# (java-17-openjdk-amd64 vs -arm64), so point a stable symlink at whichever
# one this build produced and set JAVA_HOME to that fixed location. This keeps
# JAVA_HOME identical and correct on both amd64 and arm64.
RUN JAVA_BIN="$(readlink -f "$(command -v java)")" \
    && JAVA_DIR="$(dirname "$(dirname "$JAVA_BIN")")" \
    && ln -s "$JAVA_DIR" /usr/lib/jvm/default-jdk
ENV JAVA_HOME=/usr/lib/jvm/default-jdk

# Install the `p` tool built in stage 1 from the local package source.
COPY --from=build /nupkg /tmp/nupkg
RUN dotnet tool install --global --add-source /tmp/nupkg P \
    && rm -rf /tmp/nupkg
ENV PATH="${PATH}:/root/.dotnet/tools"

# Also expose the tools directory to login shells (which re-source
# /etc/profile and would otherwise drop the ENV above).
RUN echo 'export PATH="$PATH:/root/.dotnet/tools"' > /etc/profile.d/dotnet-tools.sh

# Sanity check: fail the build if the CLI is not runnable.
RUN p --help > /dev/null

WORKDIR /workspace
CMD ["bash"]
