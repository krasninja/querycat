#!/usr/bin/env bash
# Installs the latest QueryCat (qcat) release from https://github.com/krasninja/querycat/releases/latest
#
# Usage: curl -fsSL https://raw.githubusercontent.com/krasninja/querycat/main/scripts/install.sh | bash
#
# - Supported: Linux (x64, arm64), macOS (x64, arm64). Windows: use scripts/install.ps1.
# - Run as root: installs to /usr/local/bin. Otherwise: installs to ~/.local/bin (warns if not on PATH).
# - Installs qcat and qcat-plugins-proxy; re-running upgrades in place.
# - Environment: QCAT_VERSION (default: latest), QCAT_INSTALL_DIR (overrides target directory).
# - Requires: curl or wget, tar.

set -eou pipefail

REPO="krasninja/querycat"
RELEASES_URL="https://github.com/${REPO}/releases"
API_LATEST_URL="https://api.github.com/repos/${REPO}/releases/latest"
RELEASES_ATOM_URL="${RELEASES_URL}.atom"

log() { printf '==> %s\n' "$*" >&2; }
warn() { printf 'warning: %s\n' "$*" >&2; }
die() { printf 'error: %s\n' "$*" >&2; exit 1; }

# --- Downloader detection ---------------------------------------------------

DOWNLOADER=""
if command -v curl > /dev/null 2>&1; then
    DOWNLOADER="curl"
elif command -v wget > /dev/null 2>&1; then
    DOWNLOADER="wget"
fi
[ -n "${DOWNLOADER}" ] || die "Neither curl nor wget found. Install one of them and re-run."
command -v tar > /dev/null 2>&1 || die "tar not found. Install it and re-run."

# Downloads a URL into a file.
download_file() {
    local url="$1" dest="$2"
    if [ "${DOWNLOADER}" = "curl" ]; then
        curl -fL --progress-bar --retry 3 --connect-timeout 15 -o "${dest}" "${url}"
    else
        wget -q -O "${dest}" "${url}"
    fi
}

# Downloads a URL to stdout.
download_stdout() {
    local url="$1"
    if [ "${DOWNLOADER}" = "curl" ]; then
        curl -fsSL --retry 3 --connect-timeout 15 "${url}"
    else
        wget -q -O - "${url}"
    fi
}

# --- Platform detection -----------------------------------------------------

OS=""
ARCH=""
RID=""

detect_platform() {
    local kernel arch
    kernel="$(uname -s)"
    arch="$(uname -m)"
    case "${kernel}" in
        Linux) OS="linux" ;;
        Darwin) OS="osx" ;;
        MINGW* | MSYS* | CYGWIN*)
            die "Windows is not supported by this script. Download the .zip from ${RELEASES_URL}/latest." ;;
        *) die "Unsupported operating system: ${kernel}." ;;
    esac
    case "${arch}" in
        x86_64 | amd64) ARCH="x64" ;;
        aarch64 | arm64) ARCH="arm64" ;;
        *) die "Unsupported CPU architecture: ${arch}. Only x64 and arm64 builds are published." ;;
    esac
    RID="${OS}-${ARCH}"
}

# --- Version resolution -----------------------------------------------------

# Prints the latest release version (without the leading "v") to stdout.
resolve_latest_version() {
    local body tag=""
    # Primary source: GitHub API.
    if body="$(download_stdout "${API_LATEST_URL}" 2> /dev/null)"; then
        tag="$(printf '%s\n' "${body}" \
            | grep -o '"tag_name"[[:space:]]*:[[:space:]]*"[^"]*"' \
            | head -n 1 \
            | sed 's/.*"\([^"]*\)"$/\1/')"
    fi
    # Fallback: releases Atom feed (not affected by the API rate limit).
    if [ -z "${tag}" ] && body="$(download_stdout "${RELEASES_ATOM_URL}" 2> /dev/null)"; then
        tag="$(printf '%s\n' "${body}" \
            | grep -o 'releases/tag/[^"<]*' \
            | head -n 1 \
            | sed 's|releases/tag/||')"
    fi
    [ -n "${tag}" ] || die "Could not determine the latest version. Set QCAT_VERSION explicitly, e.g. QCAT_VERSION=0.19.0 ${0##*/}."
    printf '%s' "${tag#v}"
}

# --- Installation helpers ---------------------------------------------------

# Prints the target directory to stdout.
choose_install_dir() {
    if [ -n "${QCAT_INSTALL_DIR:-}" ]; then
        printf '%s' "${QCAT_INSTALL_DIR}"
    elif [ "$(id -u)" -eq 0 ]; then
        printf '%s' "/usr/local/bin"
    else
        printf '%s' "${HOME:?HOME is not set}/.local/bin"
    fi
}

# Copies the binary via a temporary file and renames it, so that replacing
# a currently running binary is safe ("text file busy" is avoided).
install_binary() {
    local src="$1" dest="$2"
    local tmp="${dest}.new.$$"
    cp "${src}" "${tmp}"
    chmod 0755 "${tmp}"
    mv -f "${tmp}" "${dest}"
}

check_path() {
    local dir="$1"
    case ":${PATH:-}:" in
        *":${dir}:"*) return 0 ;;
    esac
    warn "${dir} is not in your PATH."
    warn "Add it to your shell profile, e.g.: export PATH=\"${dir}:\$PATH\""
    return 0
}

# --- Main -------------------------------------------------------------------

main() {
    detect_platform

    local version="${QCAT_VERSION:-}"
    version="${version#v}"
    if [ -n "${version}" ]; then
        case "${version}" in
            *[!A-Za-z0-9.+_-]*) die "Invalid QCAT_VERSION: ${QCAT_VERSION}." ;;
        esac
    else
        log "Resolving the latest release version..."
        version="$(resolve_latest_version)"
    fi
    local tag="v${version}"
    log "Installing QueryCat ${version} (${RID})"

    local install_dir
    install_dir="$(choose_install_dir)"
    mkdir -p "${install_dir}" || die "Cannot create directory ${install_dir}."
    [ -w "${install_dir}" ] || die "No write permission for ${install_dir}. Re-run with sudo or set QCAT_INSTALL_DIR."

    local tmp_dir
    tmp_dir="$(mktemp -d)"
    # shellcheck disable=SC2064
    trap "rm -rf '${tmp_dir}'" EXIT

    local pkg archive url extract_dir
    for pkg in qcat qcat-plugins-proxy; do
        archive="${tmp_dir}/${pkg}-${version}-${RID}.tar.gz"
        url="${RELEASES_URL}/download/${tag}/${pkg}-${version}-${RID}.tar.gz"
        log "Downloading ${url}"
        download_file "${url}" "${archive}" \
            || die "Download failed: ${url}. Check that release ${tag} exists and provides a ${RID} asset."
        extract_dir="${tmp_dir}/${pkg}"
        mkdir -p "${extract_dir}"
        tar -xzf "${archive}" -C "${extract_dir}"
        [ -f "${extract_dir}/${pkg}" ] || die "Archive ${archive} does not contain the ${pkg} binary."
        install_binary "${extract_dir}/${pkg}" "${install_dir}/${pkg}"
        log "Installed ${install_dir}/${pkg}"
    done

    check_path "${install_dir}"

    log "QueryCat ${version} installed successfully."
    "${install_dir}/qcat" --version || warn "Could not run ${install_dir}/qcat --version."
    log "Try it out: qcat \"SELECT 1\""
}

main "$@"
