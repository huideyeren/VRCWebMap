FROM postgres:18-bookworm

ARG TARGETARCH

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        jq \
        unzip \
        util-linux \
    && case "$TARGETARCH" in \
        amd64) aws_arch=x86_64 ;; \
        arm64) aws_arch=aarch64 ;; \
        *) echo "unsupported architecture: $TARGETARCH" >&2; exit 64 ;; \
    esac \
    && curl -fsSLo /tmp/awscliv2.zip \
        "https://awscli.amazonaws.com/awscli-exe-linux-${aws_arch}.zip" \
    && unzip -q /tmp/awscliv2.zip -d /tmp \
    && /tmp/aws/install \
    && rm -rf /tmp/aws /tmp/awscliv2.zip \
    && apt-get purge -y --auto-remove curl unzip \
    && rm -rf /var/lib/apt/lists/*

COPY ops/db-backup/db-backup.sh /usr/local/bin/db-backup
RUN chmod 0755 /usr/local/bin/db-backup

ENTRYPOINT ["db-backup"]
