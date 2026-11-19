# Builds the ReOMP Mod Manager AppImage on the Ubuntu 22.04 baseline.
# Build context: apps/mod-manager/. See plans/installer.md#linux.
#   docker build --platform linux/amd64 -f installers/linux/Dockerfile -t reomp-linux-builder apps/mod-manager
FROM ubuntu:22.04
SHELL ["/bin/bash", "-o", "pipefail", "-c"]
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential ca-certificates curl file libayatana-appindicator3-dev \
    librsvg2-dev libssl-dev libwebkit2gtk-4.1-dev libxdo-dev patchelf \
    pkg-config wget xz-utils && rm -rf /var/lib/apt/lists/*
RUN cd /tmp && curl -fsSLO https://nodejs.org/dist/v24.18.0/node-v24.18.0-linux-x64.tar.xz \
    && curl -fsSLO https://nodejs.org/dist/v24.18.0/SHASUMS256.txt \
    && grep '  node-v24.18.0-linux-x64.tar.xz$' SHASUMS256.txt | sha256sum -c - \
    && mkdir /opt/node && tar -xJf node-v24.18.0-linux-x64.tar.xz -C /opt/node --strip-components=1
RUN curl -fsSL https://sh.rustup.rs | sh -s -- -y --profile minimal --default-toolchain 1.95.0
ENV PATH="/opt/node/bin:/root/.cargo/bin:$PATH" APPIMAGE_EXTRACT_AND_RUN=1
WORKDIR /src
COPY . .
RUN npm ci
RUN npm run tauri build -- --ci --target x86_64-unknown-linux-gnu --bundles appimage -- --locked
RUN mkdir /out && set -- src-tauri/target/x86_64-unknown-linux-gnu/release/bundle/appimage/*.AppImage \
    && test "$#" -eq 1 && cp "$1" /out/ReOMP.AppImage
