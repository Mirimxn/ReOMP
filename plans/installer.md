# ReOMP installer implementation

## Payload and ownership

Install the **ReOMP Mod Manager desktop application** from `apps/mod-manager/`. Tauri builds the React frontend through `beforeBuildCommand` and bundles it into the native application. Windows setup packages the Windows executable and WebView2 prerequisite. The Linux script installs the Linux application and its bundled libraries.

For a player release, build the core mods from the release source as ordinary versioned OMP ZIPs. Stage them alongside the OMP runtime in the Mod Manager's bundled resources. Include the same provider ZIPs in both platform installers. [Mod Manager package handling](mod-manager.md#packages-and-versions)

The Mod Manager owns its first-run configuration, the installed-mod ZIP folder, and all changes to OMP, mods and game files. The installer owns the application files and launcher entries.

Use the existing empty Tauri window as the payload for installer development. Launching that window proves that the application packaged and installed correctly. Use the finished Mod Manager build for player releases.

## Windows

Target Windows 10 and 11 on x86-64. Use Tauri's **NSIS setup executable**, installed for the current user. Use the standard wizard, its Start menu entry and its Windows uninstall registration. Re-running setup installs the new app version. Keep the Tauri identifier `com.reomp.manager` stable across builds.

1. Set `productName` and the window title to `ReOMP Mod Manager` in `apps/mod-manager/src-tauri/tauri.conf.json`. Use its top-level `version` for release names.
2. Add `apps/mod-manager/src-tauri/tauri.windows.conf.json` with this platform override.

   ```json
   {
     "bundle": {
       "active": true,
       "targets": ["nsis"],
       "windows": {
         "nsis": { "installMode": "currentUser" },
         "webviewInstallMode": { "type": "offlineInstaller", "silent": true }
       }
     }
   }
   ```

   The offline WebView2 installer adds roughly 127 MB and lets setup supply the runtime on a machine without internet access. Tauri handles the prerequisite inside its standard installer. [Tauri Windows installer guide](https://v2.tauri.app/distribute/windows-installer/#offline-installer)

3. Build on Windows with Node 24.18.0, Rust 1.95.0 and the Microsoft C++ Build Tools with the Desktop development with C++ workload. From `apps/mod-manager/`, run:

   ```powershell
   rustup toolchain install 1.95.0 --profile minimal --target x86_64-pc-windows-msvc
   npm.cmd ci
   $env:RUSTUP_TOOLCHAIN = '1.95.0'
   npm.cmd run tauri build -- --ci --target x86_64-pc-windows-msvc --bundles nsis -- --locked
   ```

   Check each exit code before proceeding. Take the generated setup executable from `src-tauri/target/x86_64-pc-windows-msvc/release/bundle/nsis/` and name the copy `ReOMP-<version>-windows-x64-setup.exe`. Use the executable produced by this run. [Tauri build command](https://v2.tauri.app/reference/cli/#build)

## Linux

Target mainstream glibc-based desktop distros on x86-64, including SteamOS in Desktop Mode. Build in **Ubuntu 22.04**. It ships glibc 2.35 and has WebKitGTK 4.1 in its package repository. Steam's published floor is glibc 2.31, so use 22.04 as the practical native-app build baseline. [Steam Linux requirements](https://help.steampowered.com/en/faqs/view/107F-BB20-FB5A-1CE4) · [Ubuntu glibc](https://packages.ubuntu.com/jammy/libc6) · [Tauri AppImage baseline](https://v2.tauri.app/distribute/appimage/#limitations)

### Build the payload from Windows

1. Add `apps/mod-manager/src-tauri/tauri.linux.conf.json` with `{"bundle":{"active":true,"targets":["appimage"]}}`.
2. Add `installers/linux/Dockerfile` for Docker Desktop in Linux container mode. Build from the `apps/mod-manager/` context so the image receives only manager source. Add `apps/mod-manager/.dockerignore` for `node_modules/`, `dist/`, `src-tauri/target/` and `src-tauri/gen/`.
3. Use the following build stages. The Node checksum comes from the official version directory. The environment flag lets the bundler run its AppImage tools in the container without a FUSE mount. [Tauri Linux dependencies](https://v2.tauri.app/start/prerequisites/#linux) · [Tauri bundler change log](https://github.com/tauri-apps/tauri/blob/dev/crates/tauri-bundler/CHANGELOG.md)

   ```dockerfile
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
   ```

4. From the repository root, run `docker build --platform linux/amd64 -f installers/linux/Dockerfile -t reomp-linux-builder apps/mod-manager`. After success, use `docker create reomp-linux-builder` to obtain a container ID and `docker cp <container-id>:/out/ReOMP.AppImage <staging-folder>` to copy the payload to Windows. Remove that container with `docker rm <container-id>`. The container is used to produce the payload.
5. Put `ReOMP.AppImage` and `installers/linux/install-reomp.sh` together at the root of `ReOMP-<version>-linux-x64.zip`. If bundling fails, record the first error and keep the Linux artifact pending. Windows packaging proceeds independently.

### Install script

The user extracts the ZIP in Desktop Mode and runs `bash install-reomp.sh`. Implement the script in `installers/linux/`.

1. Find `ReOMP.AppImage` beside the script. Use the data directory `${XDG_DATA_HOME:-$HOME/.local/share}`. Keep application files under `reomp/app/` within it.
2. Copy the AppImage into a temporary directory under that application path, make the copy executable, and run it there with `--appimage-extract`. Verify the extracted `squashfs-root/AppRun` exists and is executable. Move the extracted directory to a new release directory on the same filesystem. [AppImage extraction documentation](https://docs.appimage.org/user-guide/troubleshooting/fuse.html#fallback-if-fuse-can-t-be-made-working)
3. Prepare an `applications/com.reomp.manager.desktop` entry under the data directory. Use `Name=ReOMP Mod Manager`, `Type=Application`, `Terminal=false` and an `Exec` pointing to the absolute path of `reomp/app/current/AppRun`. Escape the path according to the desktop entry specification, including spaces and literal percent signs. Write the entry to a temporary file in `applications/`. [Desktop Entry Exec specification](https://specifications.freedesktop.org/desktop-entry/1.2/exec-variables.html)
4. Save the previous `current` link target, switch it to the new release by renaming a temporary symlink over it with `mv -Tf`, then rename the prepared launcher into place. If publishing the launcher fails, restore the previous link or remove it on a first install. Report the failing step and clean the staging files. Running setup again is the upgrade path.
5. Support `bash install-reomp.sh --uninstall` to remove the app files under `reomp/app/` and its desktop entry. Preserve the manager's own config and archive storage. Keep installer writes inside the current user's app and launcher paths.

## Verification and handoff

On the available Windows machine, install as a standard user, open the app from the Start menu and confirm the expected window loads. Close it, install a second build with a higher version, then check the installed version in Windows Settings and launch from the same menu entry. For the second test build, pass `--config <path-to-test-override.json>` before `-- --locked` with a temporary override containing only a higher `version`. Uninstall through Windows Settings and check that the app entry is removed. Record the Windows version and whether the WebView2 prerequisite actually ran. If Windows policy blocks the setup executable, record the message and mark the install check as blocked.

For the player release, verify on a clean Windows install that the manager opens with the bundled core mods available in its library.

For Linux, record whether the container produced the AppImage and whether the ZIP contains the two specified files. Runtime results come later from people using Linux. Treat the Windows installer, Linux build output and Linux user reports as separate results.
