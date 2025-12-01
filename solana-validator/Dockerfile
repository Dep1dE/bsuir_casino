FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive

# Установка зависимостей
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    bzip2 \
    && rm -rf /var/lib/apt/lists/*

# Установка Solana CLI - прямой способ (фиксированная версия для ускорения)
# Используем стабильную версию 1.18.x
ENV SOLANA_VERSION=v1.18.25

RUN mkdir -p /root/.local/share/solana/install/active_release/bin && \
    echo "Скачивание Solana ${SOLANA_VERSION}..." && \
    curl -fL --progress-bar "https://github.com/solana-labs/solana/releases/download/${SOLANA_VERSION}/solana-release-x86_64-unknown-linux-gnu.tar.bz2" | \
    tar -xj -C /tmp && \
    echo "Копирование бинарников..." && \
    find /tmp/solana-release/bin -maxdepth 1 -type f -executable -exec cp {} /root/.local/share/solana/install/active_release/bin/ \; && \
    chmod +x /root/.local/share/solana/install/active_release/bin/* && \
    echo "Проверка установки..." && \
    /root/.local/share/solana/install/active_release/bin/solana --version && \
    /root/.local/share/solana/install/active_release/bin/solana-test-validator --version && \
    rm -rf /tmp/solana-release

# Настройка PATH
ENV PATH="/root/.local/share/solana/install/active_release/bin:${PATH}"

# Создание директории для данных
RUN mkdir -p /root/.local/share/solana

# Экспорт портов
EXPOSE 8899 8900 8001 8002 8003

# Запуск test validator
CMD ["solana-test-validator", "--bind-address", "0.0.0.0", "--rpc-port", "8899", "--ledger", "/root/.local/share/solana", "--faucet-sol", "1000000", "--limit-ledger-size"]

